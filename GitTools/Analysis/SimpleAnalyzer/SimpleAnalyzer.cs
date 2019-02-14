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
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Util;
using Util.Logging;

namespace GitTools.Analysis.SimpleAnalyzer
{
	/// <summary>
	/// An implementation of <see cref="IAnalyzer{T}"/> that maps each
	/// commit to an instance of <see cref="SimpleCommitDetails"/>.
	/// </summary>
	public class SimpleAnalyzer : BaseAnalyzer<SimpleCommitDetails>
	{
		private readonly BaseLogger<SimpleAnalyzer> logger =
			Program.CreateLogger<SimpleAnalyzer>();

		/// <summary>
		/// Returns a concrete <see cref="BaseLogger{SimpleAnalyzer}"/> for this analyzer.
		/// </summary>
		protected override ILogger<IAnalyzer<SimpleCommitDetails>> Logger => this.logger;

		/// <summary>
		/// This is a forwarding constructor that does not do any other
		/// initialization than <see cref="BaseAnalyzer{T}.BaseAnalyzer(string, GitCommitSpan)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		public SimpleAnalyzer(String repoPathOrUrl, GitCommitSpan span)
			: base(repoPathOrUrl, span)
		{
		}

		/// <summary>
		/// Maps each <see cref="Commit"/> to a <see cref="SimpleCommitDetails"/>.
		/// </summary>
		/// <returns><see cref="IEnumerable{SimpleCommitDetails}"/></returns>
		public override IEnumerable<SimpleCommitDetails> AnalyzeCommits()
		{
			this.Logger.LogInformation("Starting analysis of commits..");
			this.Logger.LogWarning("Parallel Analysis is: {0}ABLED!",
				this.ExecutionPolicy == ExecutionPolicy.Parallel ? "EN" : "DIS");

			var done = 0;
			var total = this.GitCommitSpan.Count();
			var repo = this.GitCommitSpan.Repository;
			var bag = new ConcurrentBag<SimpleCommitDetails>();
			var reporter = new SimpleProgressReporter<SimpleAnalyzer>(this.Logger);

			var po = new ParallelOptions();
			if (this.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				po.MaxDegreeOfParallelism = 1;
			}

			Parallel.ForEach(this.GitCommitSpan, po, commit =>
			{
				String authorLabel, committerLabel;
				this.AuthorAndCommitterNominalForCommit(
					commit, out authorLabel, out committerLabel);

				bag.Add(new SimpleCommitDetails(this.RepoPathOrUrl, repo, commit)
				{
					AuthorNominalLabel = authorLabel,
					CommitterNominalLabel = committerLabel
				});
				reporter.ReportProgress(Interlocked.Increment(ref done), total);
			});

			this.Logger.LogInformation("Finished analysis of commits.");

			return bag.OrderBy(scd => scd.AuthorTime);
		}
	}

	internal class SimpleProgressReporter<T> where T: IAnalyzer<IAnalyzedCommit>
	{
		public static readonly ISet<Int32> DefaultSteps
			= new HashSet<Int32>(Enumerable.Range(1, 20).Select(i => i * 5));

		protected readonly ILogger<IAnalyzer<SimpleCommitDetails>> logger;

		protected readonly ISet<Int32> steps;

		protected readonly SemaphoreSlim semaphoreSlim;

		public SimpleProgressReporter(ILogger<IAnalyzer<SimpleCommitDetails>> logger, ISet<Int32> stepsToProgress = null)
		{
			this.logger = logger;
			this.steps = stepsToProgress ?? DefaultSteps;
			this.semaphoreSlim = new SemaphoreSlim(1, 1);
		}

		public void ReportProgress(int numDone, int numTotal)
		{
			var doneNow = (int)Math.Floor((double)numDone / (double)numTotal * 100d);

			this.semaphoreSlim.Wait();
			if (this.steps.Contains(doneNow))
			{
				this.steps.Remove(doneNow);
				this.logger.LogInformation($"Progress is {doneNow.ToString().PadLeft(3)}% ({numDone.ToString().PadLeft(numTotal.ToString().Length)}/{numTotal} commits)");
			}
			this.semaphoreSlim.Release();
		}
	}
}
