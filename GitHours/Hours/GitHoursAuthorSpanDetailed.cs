using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GitHours.Hours.GitHours;

namespace GitHours.Hours
{
	/// <summary>
	/// A delegate to match the signature of <see cref="GitHours.Estimate(DateTime[], out EstimateHelper[])"/>.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="estimates"></param>
	/// <returns></returns>
	internal delegate Double Estimator(DateTime[] input, out EstimateHelper[] estimates);

	internal class GitHoursAuthorSpanDetailed : GitHoursAuthorSpan
	{
		/// <summary>
		/// A property that indicates whether this span represents the very first
		/// span of the developer, computed on a specific sets of commits. I.e.,
		/// returns true, if this span concerns the initial commit of the set,
		/// which is not preceded by any other commit.
		/// </summary>
		public Boolean IsInitialSpan { get; private set; }

		/// <summary>
		/// A property that indicates whether this span represents the initial
		/// span of a session. A new session begins, when between the two commits
		/// <see cref="GitHoursAuthorSpan.Since"/> and <see cref="GitHoursAuthorSpan.Until"/>
		/// more time than <see cref="GitHours.MaxCommitDiffInMinutes"/> has passed.
		/// In that case, the constant value <see cref="GitHours.FirstCommitAdditionInMinutes"/>
		/// is assigned for the hours passed.
		/// </summary>
		public Boolean IsSessionInitialSpan { get; private set; }

		private GitHoursAuthorSpanDetailed(Commit initial, Commit since, Commit until, double hours, Boolean isInitialSpan, Boolean isSessionInitialSpan)
			: base(initial, since, until, hours)
		{
			this.IsInitialSpan = isInitialSpan;
			this.IsSessionInitialSpan = isSessionInitialSpan;
		}
		
		/// <summary>
		/// Similar to <see cref="GitHoursAuthorSpan.GitHoursAuthorSpan(Commit, Commit, Commit, double)"/>,
		/// this function returns <see cref="GitHoursAuthorSpanDetailed"/> entities, that carry
		/// information about the involved estimates as well (like whether an estimate is an initial
		/// estimate or not).
		/// </summary>
		/// <param name="commitsForDeveloper"></param>
		/// <param name="estimator">Must be of type <see cref="Estimator"/>.</param>
		/// <returns>The estimates based on the given commits.</returns>
		public static IEnumerable<GitHoursAuthorSpanDetailed> GetHoursSpans(IEnumerable<Commit> commitsForDeveloper, Estimator estimator)
		{
			var commitsSorted = commitsForDeveloper.OrderBy(commit => commit.Author.When).ToList();

			if (commitsSorted.Count == 0)
			{
				yield break; // Cannot return any span
			}

			// Return a special instance for the first (initial) commit, that does not
			// have a preceding commit:
			yield return new GitHoursAuthorSpanDetailed(
				commitsSorted[0], null, commitsSorted[0], 0d, true, true);

			estimator(
				commitsSorted.Select(commit => commit.Author.When.DateTime).ToArray(),
				out EstimateHelper[] estimates);

			for (int i = 0; i < commitsSorted.Count - 1; i++)
			{
				yield return new GitHoursAuthorSpanDetailed(
					initial: commitsSorted[0],
					since: commitsSorted[i],
					until: commitsSorted[i + 1],
					hours: estimates[i].Hours,
					isInitialSpan: false,
					isSessionInitialSpan: estimates[i].IsInitialEstimate);
			}
		}
	}
}
