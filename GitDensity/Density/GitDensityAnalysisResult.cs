using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
				session.Insert(this.Repository);
				numEntities++;

				foreach (var dev in this.Repository.Developers)
				{
					session.Insert(dev);
					numEntities++;
				}

				foreach (var commit in this.Repository.Commits)
				{
					session.Insert(commit);
					numEntities++;
				}

				foreach (var hour in this.Repository.Developers.SelectMany(dev => dev.Hours))
				{
					session.Insert(hour);
					numEntities++;
				}

				foreach (var pair in this.Repository.CommitPairs)
				{
					session.Insert(pair);
					numEntities++;
				}

				foreach (var tree in this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges))
				{
					session.Insert(tree);
					numEntities++;
					session.Insert(tree.TreeEntryChangesMetrics);
					numEntities++;

					foreach (var fb in tree.FileBlocks)
					{
						session.Insert(fb);
						numEntities++;
						foreach (var sim in fb.Similarities)
						{
							session.Insert(sim);
							numEntities++;
						}
					}
				}

				logger.LogInformation("Trying to commit transaction..");
				trans.Commit();
				logger.LogInformation("Success! Storing the result took {0}", (DateTime.Now - start));
				logger.LogInformation("The ID of the analyzed Repository has become {0}", this.Repository.ID);

				logger.LogDebug("Stored {0} instances totally, thereof:", numEntities);
				if (logger.IsEnabled(LogLevel.Debug))
				{
					logger.LogDebug(
						"\n`- Repositories:\t\t1\n" +
						"`- Developers:\t\t\t{0}\n" +
						"`- Commits:\t\t\t{1}\n" +
						"`- Git-Hours:\t\t\t{2}\n" +
						"`- CommitPairs:\t\t\t{3}\n" +
						"`- TreeEntryChanges:\t\t{4}\n" +
						"`- TreeEntryChangesMetrics:\t{5}\n" +
						"`- FileBlocks:\t\t\t{6}\n" +
						"`- Similarities:\t\t{7}\n",
						this.Repository.Developers.Count,
						this.Repository.Commits.Count,
						this.Repository.Developers.SelectMany(dev => dev.Hours).Count(),
						this.Repository.CommitPairs.Count,
						this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges).Count(),
						// same:
						this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges).Count(),
						this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges)
							.SelectMany(tec => tec.FileBlocks).Count(),
						this.Repository.CommitPairs.SelectMany(cp => cp.TreeEntryChanges)
							.SelectMany(tec => tec.FileBlocks.SelectMany(fb => fb.Similarities)).Count()
					);
				}
			}

		}
	}
}
