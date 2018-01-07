using GitDensity.Util;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Density
{
	internal class GitDensity
	{
		public static readonly String[] DefaultFileTypeExtensions =
			new [] { "js", "ts", "java", "cs", "php", "phtml", "php3", "php4", "php5", "xml" };

		public Repository Repository { get; protected set; }

		public Boolean SkipInitialCommit { get; protected internal set; } = false;

		public Boolean SkipMergeCommits { get; protected internal set; } = true;

		public IEnumerable<String> FileTypeExtensions { get; protected internal set; } = GitDensity.DefaultFileTypeExtensions;

		public GitDensity(Repository repository, Boolean? skipInitialCommit = null, Boolean? skipMergeCommits = null, IEnumerable<String> fileTypeExtensions = null)
		{
			this.Repository = repository;

			if (skipInitialCommit.HasValue)
			{
				this.SkipInitialCommit = skipInitialCommit.Value;
			}
			if (skipMergeCommits.HasValue)
			{
				this.SkipMergeCommits = skipMergeCommits.Value;
			}
			if (fileTypeExtensions is IEnumerable<String> && fileTypeExtensions.Any())
			{
				this.FileTypeExtensions = fileTypeExtensions.Skip(0); // Clone the IEnumerable
			}
		}

		public GitDensityAnalysisResult Analyze()
		{
			var pairs = this.Repository.CommitPairs(this.SkipInitialCommit, this.SkipMergeCommits);

			foreach (var pair in pairs)
			{
				// Now get all TreeChanges with Status Added, Modified, Deleted or Moved.
				var relevantTreeChanges = pair.TreeChanges.Where(tc =>
				{
					return tc.Status == ChangeKind.Added || tc.Status == ChangeKind.Modified
					|| tc.Status == ChangeKind.Deleted || tc.Status == ChangeKind.Renamed;
				});

				// Now for each of the desired file types, get the Hunks and Diffs
				relevantTreeChanges = relevantTreeChanges.Where(rtc =>
				{
					return this.FileTypeExtensions.Any(extension => rtc.Path.EndsWith(extension))
					|| this.FileTypeExtensions.Any(extension => rtc.OldPath.EndsWith(extension));
				});
			}



			throw new NotImplementedException();
		}
	}
}
