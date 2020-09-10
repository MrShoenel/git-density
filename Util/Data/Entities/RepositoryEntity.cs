/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using FluentNHibernate.Mapping;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Util.Extensions;
using Util.Logging;

namespace Util.Data.Entities
{
	/// <summary>
	/// Represents a <see cref="LibGit2Sharp.Repository"/> that is associated with
	/// a number of developers through <see cref="DeveloperEntity"/> objects.
	/// </summary>
	public class RepositoryEntity : BaseEntity<Repository>
	{
		public virtual UInt32 ID { get; set; }

		[Indexed(Unique = true)]
		public virtual String Url { get; set; }

		[Indexed(Unique = true)]
		public virtual String SinceCommitSha1 { get; set; }

		[Indexed(Unique = true)]
		public virtual String UntilCommitSha1 { get; set; }

		public virtual ISet<DeveloperEntity> Developers { get; set; } = new HashSet<DeveloperEntity>();

		public virtual ISet<CommitEntity> Commits { get; set; }
			= new HashSet<CommitEntity>();

		public virtual ISet<CommitKeywordsEntity> CommitsKeywords { get; set; }
			= new HashSet<CommitKeywordsEntity>();

		public virtual ISet<CommitMetricsStatusEntity> CommitMetricsStatuses { get; set; }
			= new HashSet<CommitMetricsStatusEntity>();

		public virtual ISet<CommitPairEntity> CommitPairs { get; set; } = new HashSet<CommitPairEntity>();

		public virtual ISet<TreeEntryContributionEntity> TreeEntryContributions { get; set; } = new HashSet<TreeEntryContributionEntity>();

		public virtual ProjectEntity Project { get; set; }

		private readonly Object padLock = new Object();

		#region Methods
		public virtual RepositoryEntity AddDeveloper(DeveloperEntity developer)
		{
			lock (this.padLock)
			{
				this.Developers.Add(developer);
				return this;
			}
		}

		public virtual RepositoryEntity AddDevelopers(IEnumerable<DeveloperEntity> developers)
		{
			foreach (var developer in developers)
			{
				this.AddDeveloper(developer);
			}
			return this;
		}

		public virtual RepositoryEntity AddCommit(CommitEntity commit)
		{
			lock (padLock)
			{
				this.Commits.Add(commit);
				return this;
			}
		}

		public virtual RepositoryEntity AddCommits(IEnumerable<CommitEntity> commits)
		{
			foreach (var commit in commits)
			{
				this.AddCommit(commit);
			}
			return this;
		}

		public virtual RepositoryEntity AddCommitKeywords(CommitKeywordsEntity commitKeywords)
		{
			lock (padLock)
			{
				this.CommitsKeywords.Add(commitKeywords);
				return this;
			}
		}

		public virtual RepositoryEntity AddCommitsKeywords(IEnumerable<CommitKeywordsEntity> commitsKeywords)
		{
			foreach (var commitKeywords in commitsKeywords)
			{
				this.AddCommitKeywords(commitKeywords);
			}
			return this;
		}

		public virtual RepositoryEntity AddCommitMetricsStatus(CommitMetricsStatusEntity cmse)
		{
			lock (this.padLock)
			{
				this.CommitMetricsStatuses.Add(cmse);
				return this;
			}
		}

		public virtual RepositoryEntity AddCommitMetricsStatuses(IEnumerable<CommitMetricsStatusEntity> statuses)
		{
			foreach (var status in statuses)
			{
				this.AddCommitMetricsStatus(status);
			}
			return this;
		}

		public virtual RepositoryEntity AddCommitPair(CommitPairEntity commitPair)
		{
			lock (padLock)
			{
				this.CommitPairs.Add(commitPair);
				return this;
			}
		}

		public virtual RepositoryEntity AddCommitPairs(IEnumerable<CommitPairEntity> commitPairs)
		{
			foreach (var commitPair in commitPairs)
			{
				this.AddCommitPair(commitPair);
			}
			return this;
		}

		public virtual RepositoryEntity AddContribution(TreeEntryContributionEntity contribution)
		{
			lock (padLock)
			{
				this.TreeEntryContributions.Add(contribution);
				return this;
			}
		}

		public virtual RepositoryEntity AddContributions(IEnumerable<TreeEntryContributionEntity> contributions)
		{
			foreach (var contribution in contributions)
			{
				this.AddContribution(contribution);
			}
			return this;
		}
		#endregion

		#region Delete Repository and all its belongings
		public static void Delete(UInt32 repositoryEntityId, BaseLogger<RepositoryEntity> logger)
		{
			logger.LogCritical($"Attempting to delete Repository with ID {repositoryEntityId}..");

			using (var session = DataFactory.Instance.OpenSession())
			{
				var repo = session.QueryOver<RepositoryEntity>()
					.Where(r => r.ID == repositoryEntityId).SingleOrDefault();

				if (!(repo is RepositoryEntity))
				{
					throw new ArgumentException($"There is no Repository with ID {repositoryEntityId}");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var keywords in repo.CommitsKeywords)
					{
						session.Delete(keywords);
					}
					trans.Commit();
					logger.LogWarning("Deleted all commits' keywords.");
				}

				using (var trans = session.BeginTransaction())
				{
					var allCodeMetrics = repo.Commits.SelectMany(commit => commit.Metrics);

					foreach (var codeMetric in allCodeMetrics)
					{
						session.Delete(codeMetric);
					}
					trans.Commit();
					logger.LogWarning("Deleted all code metrics.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var contribution in repo.TreeEntryContributions)
					{
						session.Delete(contribution);
					}
					trans.Commit();
					logger.LogWarning("Deleted all TreeEntry-contributions.");
				}

				using (var trans = session.BeginTransaction())
				{
					var allSimilarities = repo.CommitPairs.SelectMany(cp => cp.TreeEntryChanges.SelectMany(tec => tec.FileBlocks.SelectMany(fb => fb.Similarities)));

					foreach (var sim in allSimilarities)
					{
						session.Delete(sim);
					}
					trans.Commit();
					logger.LogWarning("Deleted all similarity metrics.");
				}

				using (var trans = session.BeginTransaction())
				{
					var allFileBlocks = repo.CommitPairs.SelectMany(cp => cp.TreeEntryChanges.SelectMany(tec => tec.FileBlocks));

					foreach (var fb in allFileBlocks)
					{
						session.Delete(fb);
					}
					trans.Commit();
					logger.LogWarning("Deleted all FileBlocks.");
				}

				using (var trans = session.BeginTransaction())
				{
					var allMetrics = repo.CommitPairs.SelectMany(cp => cp.TreeEntryChanges).SelectMany(tree => tree.TreeEntryChangesMetrics);

					foreach (var metric in allMetrics)
					{
						session.Delete(metric);
					}
					trans.Commit();
					logger.LogWarning("Deleted all TreeEntryChange-metrics.");
				}

				using (var trans = session.BeginTransaction())
				{
					var allTrees = repo.CommitPairs.SelectMany(cp => cp.TreeEntryChanges);

					foreach (var tree in allTrees)
					{
						session.Delete(tree);
					}
					trans.Commit();
					logger.LogWarning("Deleted all TreeEntryChanges.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var commitPair in repo.CommitPairs)
					{
						session.Delete(commitPair);
					}
					trans.Commit();
					logger.LogWarning("Deleted all CommitPairs.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var hours in repo.Developers.SelectMany(dev => dev.Hours))
					{
						session.Delete(hours);
					}
					trans.Commit();
					logger.LogWarning("Deleted all developers' Hours.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var status in repo.CommitMetricsStatuses)
					{
						session.Delete(status);
					}
					trans.Commit();
					logger.LogWarning("Deleted all CommitMetricsStatuses.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var commit in repo.Commits)
					{
						session.Delete(commit);
					}
					trans.Commit();
					logger.LogWarning("Deleted all Commits.");
				}

				using (var trans = session.BeginTransaction())
				{
					foreach (var dev in repo.Developers)
					{
						session.Delete(dev);
					}
					trans.Commit();
					logger.LogWarning("Deleted all Developers.");
				}

				using (var trans = session.BeginTransaction())
				{
					session.Delete(repo);
					trans.Commit();
					logger.LogWarning("Deleted the entire Repository.");
				}
			}
		}
		#endregion
	}

	public class RepositoryEntityMap : ClassMap<RepositoryEntity>
	{
		public RepositoryEntityMap()
		{
			this.Table(nameof(RepositoryEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Url).Not.Nullable();
			this.Map(x => x.SinceCommitSha1).Not.Nullable().Length(40);
			this.Map(x => x.UntilCommitSha1).Not.Nullable().Length(40);

			this.HasMany<DeveloperEntity>(x => x.Developers).Cascade.Lock();
			this.HasMany<CommitEntity>(x => x.Commits).Cascade.Lock();
			this.HasMany<CommitKeywordsEntity>(x => x.CommitsKeywords).Cascade.Lock();
			this.HasMany<CommitPairEntity>(x => x.CommitPairs).Cascade.Lock();
			this.HasMany<TreeEntryContributionEntity>(x => x.TreeEntryContributions).Cascade.Lock();
			this.HasMany<CommitMetricsStatusEntity>(x => x.CommitMetricsStatuses).Cascade.Lock();

			this.References<ProjectEntity>(x => x.Project).Unique();
		}
	}
}
