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
			this.logger.LogInformation("Starting analysis of commits..");
			this.logger.LogWarning("Parallel Analysis is: {0}ABLED!",
				this.ExecutionPolicy == ExecutionPolicy.Parallel ? "EN" : "DIS");

			var done = 0;
			var total = this.GitCommitSpan.Count();
			var report = new HashSet<Int32>(Enumerable.Range(1, 10).Select(i => i * 10));
			var repo = this.GitCommitSpan.Repository;
			var bag = new ConcurrentBag<SimpleCommitDetails>();

			Parallel.ForEach(this.GitCommitSpan, new ParallelOptions
			{
				MaxDegreeOfParallelism = this.ExecutionPolicy == ExecutionPolicy.Linear ? 1 : Environment.ProcessorCount
			}, commit =>
			{
				bag.Add(new SimpleCommitDetails(this.RepoPathOrUrl, repo, commit));

				var doneNow = (int)Math.Floor((double)Interlocked.Increment(ref done) / total * 100);
				lock (report)
				{
					if (report.Contains(doneNow))
					{
						report.Remove(doneNow);
						this.logger.LogInformation($"Progress is {doneNow.ToString().PadLeft(3)}% ({done.ToString().PadLeft(total.ToString().Length)}/{total} commits)");
					}
				}
			});

			this.logger.LogInformation("Finished analysis of commits.");

			return bag.OrderBy(scd => scd.AuthorTime);
		}
	}
}
