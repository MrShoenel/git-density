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
		/// This is a forwarding constructor that does not do any other
		/// initialization than <see cref="BaseAnalyzer{T}.BaseAnalyzer(string, GitCommitSpan)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		public ExtendedAnalyzer(String repoPathOrUrl, GitCommitSpan span)
			: base(repoPathOrUrl, span)
		{
		}

		/// <summary>
		/// This method will use <see cref="CommitPair"/>s for extracting
		/// information about the differences in the <see cref="Tree"/>s.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ExtendedCommitDetails> AnalyzeCommits()
		{
			this.logger.LogInformation("Starting analysis of commits..");
			this.logger.LogWarning("Parallel Analysis is: {0}ABLED!",
				this.ExecutionPolicy == ExecutionPolicy.Parallel ? "EN" : "DIS");

			var repo = this.GitCommitSpan.Repository;
			var bag = new ConcurrentBag<ExtendedCommitDetails>();

			var pairs = this.GitCommitSpan.CommitPairs(
				skipInitialCommit: false,
				skipMergeCommits: false);

			Parallel.ForEach(pairs, new ParallelOptions
			{
				MaxDegreeOfParallelism = this.ExecutionPolicy == ExecutionPolicy.Linear ? 1 : Environment.ProcessorCount
			}, pair =>
			{
				pair.ExecutionPolicy = ExecutionPolicy.Linear; // As we're probably running parallel in outer scope already
				var parents = pair.Child.Parents.ToList();

				var ecd = new ExtendedCommitDetails(this.RepoPathOrUrl, repo, pair.Child)
				{
					IsInitialCommit = pair.Parent is Commit ? false : true,
					IsMergeCommit = parents.Count > 1,
					NumberOfParentCommits = (UInt32)parents.Count,
					MinutesSincePreviousCommit = parents.Count == 0 ? (double?)null :
						Math.Round(
						(pair.Child.Committer.When.DateTime -
							(parents.OrderByDescending(p => p.Committer.When.DateTime).First().Committer.When.DateTime)).TotalMinutes, 4)
				};

				var relevantTreeChanges = pair.TreeChanges.Where(tc =>
				{
					return tc.Mode != Mode.GitLink &&
					(tc.Status == ChangeKind.Added || tc.Status == ChangeKind.Modified
						|| tc.Status == ChangeKind.Deleted || tc.Status == ChangeKind.Renamed);
				});

				// We are interested in how many files were affected by this commit
				// for the kinds added, deleted, modified, renamed
				#region added/deleted
				foreach (var change in relevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Deleted))
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
				foreach (var change in relevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Modified || rtc.Status == ChangeKind.Renamed))
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


				bag.Add(ecd);
			});

			this.logger.LogInformation("Finished analysis of commits.");

			return bag.OrderBy(ecd => ecd.AuthorTime);
		}
	}
}
