/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
						session.Insert(this.Repository.Project);
					}
				}

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
					//session.Insert(tree.TreeEntryChangesMetrics);
					//numEntities++;

					foreach (var metricsEntity in tree.TreeEntryChangesMetrics)
					{
						session.Insert(metricsEntity);
						numEntities++;
					}

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

				foreach (var contribution in this.Repository.TreeEntryContributions)
				{
					session.Insert(contribution);
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
					"`- Similarities:\t\t{9}\n" + fullLine +
					"\t\t\t\t{10} total\n" + fullLine,
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
					numEntities
				);
			}
		}
	}
}
