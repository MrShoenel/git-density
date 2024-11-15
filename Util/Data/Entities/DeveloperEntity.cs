﻿/// ---------------------------------------------------------------------------------
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
using Util.Extensions;
using static Util.Extensions.RepositoryExtensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// A <see cref="DeveloperEntity"/> represents a single developer within a repository.
	/// It refers to all the developer's commits, contributions within them and spent time.
	/// </summary>
	public class DeveloperEntity : BaseEntity<Signature>, IEquatable<DeveloperEntity>
	{
		public virtual UInt32 ID { get; set; }

		public virtual String Name { get; set; }

		public virtual String Email { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual ISet<CommitEntity> Commits { get; set; } = new HashSet<CommitEntity>();

		public virtual ISet<HoursEntity> Hours { get; set; } = new HashSet<HoursEntity>();

		public virtual ISet<TreeEntryContributionEntity> TreeEntryContributions { get; set; }
			= new HashSet<TreeEntryContributionEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		public virtual DeveloperEntity AddCommit(CommitEntity commit)
		{
			lock (this.padLock)
			{
				this.Commits.Add(commit);
				return this;
			}
		}

		public virtual DeveloperEntity AddCommits(IEnumerable<CommitEntity> commits)
		{
			foreach (var commit in commits)
			{
				this.AddCommit(commit);
			}
			return this;
		}

		public virtual DeveloperEntity AddHour(HoursEntity hour)
		{
			lock (this.padLock)
			{
				this.Hours.Add(hour);
				return this;
			}
		}

		public virtual DeveloperEntity AddHours(IEnumerable<HoursEntity> hours)
		{
			foreach (var hour in hours)
			{
				this.AddHour(hour);
			}
			return this;
		}

		public virtual DeveloperEntity AddContribution(TreeEntryContributionEntity contribution)
		{
			lock (padLock)
			{
				this.TreeEntryContributions.Add(contribution);
				return this;
			}
		}

		public virtual DeveloperEntity AddContributions(IEnumerable<TreeEntryContributionEntity> contributions)
		{
			foreach (var contribution in contributions)
			{
				this.AddContribution(contribution);
			}
			return this;
		}

		/// <summary>
		/// Comapres two <see cref="DeveloperEntity"/> objects.
		/// </summary>
		/// <param name="other"></param>
		/// <returns>True, iff the other name or email is equal to this' email or name.</returns>
		public virtual bool Equals(DeveloperEntity other)
		{
			return other is DeveloperEntity && this.Name == other.Name && this.Email == other.Email;
		}

		/// <summary>
		/// Calls <see cref="DeveloperEntity.Equals(DeveloperEntity)"/> by
		/// previously attempting to cast the object to an instance of it.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as DeveloperEntity);
		}

		/// <summary>
		/// Returns a hashcode based on the name and the email. If either of them is null,
		/// will use <see cref="String.Empty"/> for them.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return 31 * (this.Name ?? String.Empty).GetHashCode() ^ (this.Email ?? String.Empty).GetHashCode();
		}
		#endregion
	}

	public class DeveloperEntityMap : ClassMap<DeveloperEntity>
	{
		public DeveloperEntityMap()
		{
			this.Table(nameof(DeveloperEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Name).Not.Nullable();
			this.Map(x => x.Email).Not.Nullable();

			this.HasMany<CommitEntity>(x => x.Commits).Cascade.Lock();
			this.HasMany<HoursEntity>(x => x.Hours).Cascade.Lock();
			this.HasMany<TreeEntryContributionEntity>(x => x.TreeEntryContributions).Cascade.Lock();

			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}

	public class DeveloperWithAlternativeNamesAndEmailsMap : ClassMap<DeveloperWithAlternativeNamesAndEmails>
	{
		public DeveloperWithAlternativeNamesAndEmailsMap()
		{
			this.Table(nameof(DeveloperEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Name).Not.Nullable();
			this.Map(x => x.Email).Not.Nullable();

			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}
}
