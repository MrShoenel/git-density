using GitDensity.Density;
using LibGit2Sharp;
using System.Collections.Generic;
using System.Linq;

namespace GitDensity.Util
{
	public static class RepositoryExtensions
	{
		public enum SortOrder
		{
			LatestFirst,
			OldestFirst
		}

		/// <summary>
		/// Returns pairs of commits as <see cref="CommitPair"/>s. Such a pair can be used
		/// to compute the differences between the selected commits. A pair consists of a
		/// commit and its direct parent.
		/// </summary>
		/// <param name="repo">The <see cref="Repository"/> to pull the commits from.</param>
		/// <param name="skipInitialCommit">If true, then the initial commit will be discarded
		/// and not be included in any pair. This might be useful, because the initial commit
		/// consists only of new files.</param>
		/// <param name="skipMergeCommits">If true, then merge-commits (commits with more than
		/// one parent) will be ignored as children, however, they will appear as parents to
		/// subsequent commits. This setting can be useful to ignore changesets that have already
		/// been analyzed using prior commits.</param>
		/// <param name="sortOrder">If <see cref="SortOrder.OldestFirst"/>, the first pair returned
		/// contains the initial commit (if not skipped) and null as a parent.</param>
		/// <returns>An <see cref="IEnumerable{CommitPair}"/> of pairs of commits.</returns>
		public static IEnumerable<CommitPair> CommitPairs(this Repository repo, bool skipInitialCommit = false, bool skipMergeCommits = true, SortOrder sortOrder = SortOrder.OldestFirst)
		{
			var commits = sortOrder == SortOrder.LatestFirst ? repo.Commits : repo.Commits.Reverse();

			foreach (var commit in commits)
			{
				var parentCount = commit.Parents.Count();

				if (skipInitialCommit && parentCount == 0)
				{
					continue;
				}

				if (skipMergeCommits && parentCount > 1)
				{
					continue;
				}


				// Note that the parent can be null, if initial commit is not skipped.
				yield return new CommitPair(repo, commit, commit.Parents.FirstOrDefault());
			}
		}
	}
}
