using GitDensity.Density;
using LibGit2Sharp;
using System;
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
				yield return new CommitPair(repo.Diff, commit, commit.Parents.FirstOrDefault());
			}
		}
	}
}
