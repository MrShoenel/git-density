/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitTools. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using GitDensity.Density;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Util;
using Util.Density;
using Util.Extensions;
using Util.Logging;
using Util.Metrics;

namespace GitTools.Analysis.ExtendedAnalyzer
{
	/// <summary>
	/// An implementation of <see cref="IAnalyzer{T}"/> that maps each
	/// commit to an instance of <see cref="ExtendedCommitDetails"/>.
	/// </summary>
	public class ExtendedAnalyzer : BaseAnalyzer<ExtendedCommitDetails>
	{
		private readonly BaseLogger<ExtendedAnalyzer> logger =
			Program.CreateLogger<ExtendedAnalyzer>();

		/// <summary>
		/// Concrete logger of type <see cref="BaseLogger{ExtendedAnalyzer}"/>.
		/// </summary>
		protected override ILogger<IAnalyzer<ExtendedCommitDetails>> Logger => this.logger;

		/// <summary>
		/// Can be set to true in the constructor.
		/// </summary>
		public Boolean SkipSizeAnalysis { get; protected set; }

		/// <summary>
		/// This is a forwarding constructor that does not do any other
		/// initialization than <see cref="BaseAnalyzer{T}.BaseAnalyzer(string, GitCommitSpan)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		/// <param name="skipSizeAnalysis"></param>
		public ExtendedAnalyzer(String repoPathOrUrl, GitCommitSpan span, Boolean skipSizeAnalysis)
			: base(repoPathOrUrl, span)
		{
			this.SkipSizeAnalysis = skipSizeAnalysis;
		}

		/// <summary>
		/// This method will use <see cref="CommitPair"/>s for extracting
		/// information about the differences in the <see cref="Tree"/>s.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ExtendedCommitDetails> AnalyzeCommits()
		{
			this.Logger.LogInformation("Starting analysis of commits..");
			this.Logger.LogWarning("Parallel Analysis is: {0}ABLED!",
				this.ExecutionPolicy == ExecutionPolicy.Parallel ? "EN" : "DIS");

			var done = 0;
			var total = this.GitCommitSpan.Count();
			var repo = this.GitCommitSpan.Repository;
			var bag = new ConcurrentBag<ExtendedCommitDetails>();
			var reporter = new SimpleAnalyzer.SimpleProgressReporter<ExtendedAnalyzer>(this.Logger);

			var pairs = this.GitCommitSpan.CommitPairs(
				skipInitialCommit: false,
				skipMergeCommits: false);

			var po = new ParallelOptions();
			if (this.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				po.MaxDegreeOfParallelism = 1;
			}

			Parallel.ForEach(pairs, po, pair =>
			{
				pair.ExecutionPolicy = ExecutionPolicy.Linear; // As we're probably running parallel in outer scope already
				var parents = pair.Child.Parents.ToList();

				var ecd = new ExtendedCommitDetails(this.RepoPathOrUrl, repo, pair.Child)
				{
					MinutesSincePreviousCommit = parents.Count == 0 ? -.1 :
						Math.Round(
						(pair.Child.Committer.When.UtcDateTime -
							(parents.OrderByDescending(p => p.Committer.When.UtcDateTime).First().Committer.When.UtcDateTime)).TotalMinutes, 4)
				};

				// We are interested in how many files were affected by this commit
				// for the kinds added, deleted, modified, renamed
				if (this.SkipSizeAnalysis)
				{
					goto AfterSizes;
				}

				#region added/deleted
				foreach (var change in pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Deleted))
				{
					var added = change.Status == ChangeKind.Added;

					var patchNew = pair.Patch[change.Path];
					var patchOld = pair.Patch[change.OldPath];

					var simpleLoc = new SimpleLoc((added ?
						pair.Child[change.Path] : pair.Parent[change.OldPath]).GetLines());

					if (added)
					{
						ecd.NumberOfFilesAdded++;
						ecd.NumberOfLinesAddedByAddedFilesGross += simpleLoc.LocGross;
						ecd.NumberOfLinesAddedByAddedFilesNoComments += simpleLoc.LocNoComments;
					}
					else
					{
						ecd.NumberOfFilesDeleted++;
						ecd.NumberOfLinesDeletedByDeletedFilesGross += simpleLoc.LocGross;
						ecd.NumberOfLinesDeletedByDeletedFilesNoComments += simpleLoc.LocNoComments;
					}
				}
				#endregion

				#region modified/renamed

				// The following block concerns all changes that represent modifications to
				// two different versions of the same file. The file may have been renamed
				// or moved as well (a so-called non-pure modification).
				foreach (var change in pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Modified || rtc.Status == ChangeKind.Renamed))
				{
					var modified = change.Status == ChangeKind.Modified;
					var patchNew = pair.Patch[change.Path];
					var dummyDirectory = new DirectoryInfo(Path.GetTempPath());
					var hunks = Hunk.HunksForPatch(patchNew, dummyDirectory, dummyDirectory).ToList();
					
					if (modified)
					{
						ecd.NumberOfFilesModified++;
						ecd.NumberOfLinesAddedByModifiedFiles += (UInt32)hunks.Sum(h => h.NumberOfLinesAdded);
						ecd.NumberOfLinesDeletedByModifiedFiles += (UInt32)hunks.Sum(h => h.NumberOfLinesDeleted);
					}
					else
					{
						ecd.NumberOfFilesRenamed++;
						ecd.NumberOfLinesAddedByRenamedFiles += (UInt32)hunks.Sum(h => h.NumberOfLinesAdded);
						ecd.NumberOfLinesDeletedByRenamedFiles += (UInt32)hunks.Sum(h => h.NumberOfLinesDeleted);
					}
				}
				#endregion

			AfterSizes:

				bag.Add(ecd);
				reporter.ReportProgress(Interlocked.Increment(ref done), total);
			});

			this.Logger.LogInformation("Finished analysis of commits.");

			return bag.OrderBy(ecd => ecd.AuthorTime);
		}
	}
}
