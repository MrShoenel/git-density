/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// An entity that can represent the most essential parts of a <see cref="Commit"/>.
	/// </summary>
	[DebuggerDisplay("CommitEntity {HashSHA1}")]
	public class CommitEntity : BaseEntity<Commit>, IEquatable<CommitEntity>
	{
		public virtual UInt32 ID { get; set; }

		[Indexed(Unique = true)]
		public virtual String HashSHA1 { get; set; }

		[Indexed]
		public virtual DateTime CommitDate { get; set; }

		[Indexed]
		public virtual Boolean IsMergeCommit { get; set; }

		public virtual DeveloperEntity Developer { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		/// <summary>
		/// Refers to a set of contributions within this commit in the shape of
		/// <see cref="TreeEntryContributionEntity"/> objects.
		/// </summary>
		public virtual ISet<TreeEntryContributionEntity> TreeEntryContributions { get; set; }
			= new HashSet<TreeEntryContributionEntity>();

		/// <summary>
		/// There may be any number of metrics associated with this commit. These
		/// metrics may be specific to the repo/project or a file.
		/// </summary>
		public virtual ISet<MetricEntity> Metrics { get; set; }
			= new HashSet<MetricEntity>();

		private readonly Object padLock = new Object();

		public virtual CommitEntity AddContribution(TreeEntryContributionEntity contribution)
		{
			lock (padLock)
			{
				this.TreeEntryContributions.Add(contribution);
				return this;
			}
		}

		public virtual CommitEntity AddContributions(IEnumerable<TreeEntryContributionEntity> contributions)
		{
			foreach (var contribution in contributions)
			{
				this.AddContribution(contribution);
			}
			return this;
		}

		public virtual CommitEntity AddMetric(MetricEntity metric)
		{
			lock (this.padLock)
			{
				this.Metrics.Add(metric);
				return this;
			}
		}

		public virtual CommitEntity AddMetrics(IEnumerable<MetricEntity> metrics)
		{
			foreach (var metric in metrics)
			{
				this.AddMetric(metric);
			}
			return this;
		}

		#region equality
		public virtual bool Equals(CommitEntity other)
		{
			return other is CommitEntity && other.HashSHA1 == this.HashSHA1;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as CommitEntity);
		}

		public override int GetHashCode()
		{
			return this.HashSHA1.GetHashCode() * 31;
		}
		#endregion
	}


	public class CommitEntityMap : ClassMap<CommitEntity>
	{
		public CommitEntityMap()
		{
			this.Table(nameof(CommitEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.HashSHA1).Not.Nullable().Length(40);
			this.Map(x => x.CommitDate).Not.Nullable();
			this.Map(x => x.IsMergeCommit).Not.Nullable();

			this.HasMany<TreeEntryContributionEntity>(x => x.TreeEntryContributions).Cascade.Lock();
			this.HasMany<MetricEntity>(x => x.Metrics).Cascade.Lock();

			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}
}
