/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Util;
using Util.Data;
using Util.Data.Entities;
using Util.Logging;

namespace GitDensity.Density
{
	internal class GitDensityAnalysisResult
	{
		private static BaseLogger<GitDensityAnalysisResult> logger = Program.CreateLogger<GitDensityAnalysisResult>();

		public RepositoryEntity Repository { get; protected internal set; }

		public GitDensityAnalysisResult(RepositoryEntity repositoryEntity)
		{
			this.Repository = repositoryEntity;
		}

		public void PersistToDatabase()
		{
			var start = DateTime.Now;
			var numEntities = 0;
			logger.LogInformation("Starting persisting analysis result to database..");

			using (var session = DataFactory.Instance.OpenStatelessSession())
			using (var trans = session.BeginTransaction())
			{
				if (this.Repository.Project is ProjectEntity)
				{
					if (this.Repository.Project.AiId == 0uL)
					{
						logger.LogDebug("Inserting the related Project..");
						session.Insert(this.Repository.Project);
					}
				}

				logger.LogDebug("Inserting the Repository..");
				session.Insert(this.Repository);
				numEntities++;

				logger.LogDebug("Inserting the Developers..");
				foreach (var dev in this.Repository.Developers)
				{
					session.Insert(dev);
					numEntities++;
				}

				logger.LogDebug("Inserting the Commits..");
				foreach (var commit in this.Repository.Commits)
				{
					session.Insert(commit);
					numEntities++;
				}

				logger.LogDebug("Inserting the HoursEntities..");
				foreach (var hour in this.Repository.Developers.SelectMany(dev => dev.Hours))
				{
					session.Insert(hour);
					numEntities++;
				}

				logger.LogDebug("Inserting the CommitPairs..");
				foreach (var pair in this.Repository.CommitPairs)
				{
					session.Insert(pair);
					numEntities++;
				}

				logger.LogDebug("Inserting the TreeEntryChanges, their Metrics, FileBlocks and Similarities (this might take a while)..");
				foreach (var tree in this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges))
				{
					session.Insert(tree);
					numEntities++;

					logger.LogTrace($"Inserting {tree.TreeEntryChangesMetrics.Count} Metrics of Tree {tree.PathNew}..");
					foreach (var metricsEntity in tree.TreeEntryChangesMetrics)
					{
						session.Insert(metricsEntity);
						numEntities++;
					}

					logger.LogTrace($"Inserting {tree.FileBlocks.Count} FileBlocks of Tree {tree.PathNew}..");
					foreach (var fb in tree.FileBlocks)
					{
						session.Insert(fb);
						numEntities++;

						logger.LogTrace($"Inserting {fb.Similarities.Count} Similarities of FileBlock #{fb.ID} of Tree {tree.PathNew}..");
						foreach (var sim in fb.Similarities)
						{
							session.Insert(sim);
							numEntities++;
						}
					}
				}

				logger.LogDebug("Inserting the TreeEntryContributions..");
				foreach (var contribution in this.Repository.TreeEntryContributions)
				{
					session.Insert(contribution);
					numEntities++;
				}

				logger.LogDebug("Inserting the CommitMetricsStatuses..");
				foreach (var metricsStatus in this.Repository.CommitMetricsStatuses)
				{
					session.Insert(metricsStatus);
					numEntities++;
				}

				logger.LogDebug("Inserting the Commits' Metrics..");
				foreach (var metrics in this.Repository.Commits.SelectMany(c => c.Metrics))
				{
					session.Insert(metrics);
					numEntities++;
				}

				logger.LogInformation("Trying to commit transaction..");
				trans.Commit();
				logger.LogInformation("Success! Storing the result took {0}", (DateTime.Now - start));
				logger.LogInformation("The ID of the analyzed Repository has become {0}", this.Repository.ID);

				var fullLine = new String('-', ColoredConsole.WindowWidthSafe);
				logger.LogInformation(
					"Stored {0} instances totally, thereof:\n" +
					$"{fullLine}`- Repositories:\t\t1\n" +
					"`- Developers:\t\t\t{1}\n" +
					"`- Commits:\t\t\t{2}\n" +
					"`- Git-Hours:\t\t\t{3}\n" +
					"`- CommitPairs:\t\t\t{4}\n" +
					"`- TreeEntryChanges:\t\t{5}\n" +
					"`- TreeEntryChangesMetrics:\t{6}\n" +
					"`- TreeEntryContributions:\t{}\n" +
					"`- FileBlocks:\t\t\t{7} ({8} modified)\n" +
					"`- Similarities:\t\t{9}\n" +
					"`- MetricsStatuses:\t\t{10}\n" +
					"`- Metrics:\t\t\t{11}\n" + fullLine +
					"\t\t\t\t{12} total\n" + fullLine,
					numEntities,
					this.Repository.Developers.Count,
					this.Repository.Commits.Count,
					this.Repository.Developers.SelectMany(dev => dev.Hours).Count(),
					this.Repository.CommitPairs.Count,
					this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges).Count(),
					this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges.SelectMany(tec
						=> tec.TreeEntryChangesMetrics)).Count(),
					this.Repository.TreeEntryContributions.Count,
					this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges)
						.SelectMany(tec => tec.FileBlocks).Count(),
					this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges)
						.SelectMany(tec => tec.FileBlocks).Where(fb => fb.FileBlockType == FileBlockType.Modified).Count(),
					this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges)
						.SelectMany(tec => tec.FileBlocks.SelectMany(fb => fb.Similarities)).Count(),
					this.Repository.CommitMetricsStatuses.Count,
					this.Repository.Commits.SelectMany(c => c.Metrics).Count(),
					numEntities
				);
			}
		}
	}
}
