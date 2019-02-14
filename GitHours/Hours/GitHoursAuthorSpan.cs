/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHours. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHours.Hours
{
	internal class GitHoursAuthorSpan
	{
		/// <summary>
		/// The initial commit for the current developer (not the repository's
		/// initial commit!). This property is included, because it's often handy
		/// to access it easily for the current developer and we do the computation
		/// anyway.
		/// </summary>
		[JsonIgnore]
		public Commit InitialCommit { get; private set; }

		/// <summary>
		/// This property is null when <see cref="Until"/> points to the first
		/// <see cref="Commit"/> (because there cannot be any commit before).
		/// </summary>
		[JsonIgnore]
		public Commit Since { get; private set; }

		/// <summary>
		/// The <see cref="Since"/>-commit's SHA1 hash (8 characters of it). This
		/// property is null, if <see cref="Until"/> points to the initial commit.
		/// </summary>
		public String SinceId => this.Since?.Sha.Substring(0, 8);

		/// <summary>
		/// The end of the span (inclusive).
		/// </summary>
		[JsonIgnore]
		public Commit Until { get; private set; }

		/// <summary>
		/// The SHA1 hash of the commit pointed to by <see cref="Until"/>. 8 characters.
		/// </summary>
		public String UntilId => this.Until.Sha.Substring(0, 8);

		/// <summary>
		/// Holds the original amount, rather then the to-3-decimal places rounded value
		/// represented by <see cref="Hours"/>.
		/// </summary>
		[JsonIgnore]
		public Double HoursOriginal { get; private set; }

		/// <summary>
		/// The amount of hours worked within the span of the two <see cref="Commit"/>s.
		/// Rounded to three decimal places for prettier JSON.
		/// </summary>
		public Double Hours { get; private set; }

		protected GitHoursAuthorSpan(Commit initial, Commit since, Commit until, double hours)
		{
			this.InitialCommit = initial;
			this.Since = since;
			this.Until = until;
			this.Hours = Math.Round(hours, 3);
			this.HoursOriginal = hours;
		}

		public static IEnumerable<GitHoursAuthorSpan> GetHoursSpans(IEnumerable<Commit> commitsForDeveloper, Func<DateTime[], Double> estimator)
		{
			var commitsSorted = commitsForDeveloper.OrderBy(commit => commit.Committer.When).ToList();

			if (commitsSorted.Count == 0)
			{
				yield break; // Cannot return any span
			}

			// Return a special instance for the first (initial) commit, that does not
			// have a preceding commit:
			yield return new GitHoursAuthorSpan(commitsSorted[0], null, commitsSorted[0], 0d);

			var hoursUntilCommit = new Dictionary<Int32, Tuple<Commit, Double>>();
			for (var take = 2; take <= commitsSorted.Count; take++)
			{
				hoursUntilCommit[take] = Tuple.Create(
					commitsSorted[take - 1], estimator(commitsSorted.Take(take).Select(commit => commit.Committer.When.UtcDateTime).ToArray()));
			}

			foreach (var kv in hoursUntilCommit)
			{
				var hours = kv.Value.Item2 - (hoursUntilCommit.ContainsKey(kv.Key - 1) ?  hoursUntilCommit[kv.Key - 1].Item2 : 0);
				yield return new GitHoursAuthorSpan(commitsSorted[0],
					commitsSorted[kv.Key - 2], kv.Value.Item1, hours);
			}
		}
	}
}
