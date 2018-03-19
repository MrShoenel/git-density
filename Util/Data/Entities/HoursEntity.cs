/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Holds the amount of hours worked within two consecutive <see cref="Commit"/>s
	/// of a developer and their total amount of hours worked since the initial commit.
	/// </summary>
	public class HoursEntity
	{
		public virtual UInt32 ID { get; set; }

		/// <summary>
		/// The amount of hours worked during the two consecutive <see cref="Commit"/>s.
		/// </summary>
		public virtual Double Hours { get; set; }

		/// <summary>
		/// The amount of hours worked since the developer's initial commit.
		/// </summary>
		public virtual Double HoursTotal { get; set; }

		/// <summary>
		/// Indicates whether this entity is is the developer's first logging across
		/// the commits that were analyzed for the developer.
		/// </summary>
		[Indexed]
		public virtual Boolean IsInitial { get; set; }

		/// <summary>
		/// Indicates whether this entity is the logging of the first commit within
		/// a session. A new session begins, if the maximum difference between commits
		/// is exceeded in time.
		/// </summary>
		[Indexed]
		public virtual Boolean IsSessionInitial { get; set; }

		/// <summary>
		/// The <see cref="DeveloperEntity"/> this entity belongs to.
		/// </summary>
		public virtual DeveloperEntity Developer { get; set; }

		/// <summary>
		/// Marks the beginning of the computed span (the older of the two consecutive
		/// commits of the current developer). This <see cref="CommitEntity"/> will be
		/// null for the analyzed span where <see cref="CommitUntil"/> points to the
		/// current developer's initial commit (because there possibly cannot another
		/// commit before).
		/// </summary>
		public virtual CommitEntity CommitSince { get; set; }

		/// <summary>
		/// Marks the end of the computed span (the younger of the two consecutive
		/// commits of the current developer).
		/// </summary>
		public virtual CommitEntity CommitUntil { get; set; }

		/// <summary>
		/// Points to the initial <see cref="CommitEntity"/> for the current developer.
		/// That means, that for all <see cref="HoursEntity"/> for the current developer,
		/// this property points to the same <see cref="CommitEntity"/> (because it never
		/// changes).
		/// </summary>
		public virtual CommitEntity InitialCommit { get; set; }

		/// <summary>
		/// Used to reference the type of hour logged.
		/// </summary>
		public virtual HoursTypeEntity HoursType { get; set; }
	}

	public class HoursEntityMap : ClassMap<HoursEntity>
	{
		public HoursEntityMap()
		{
			this.Table(nameof(HoursEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.Hours).Not.Nullable();
			this.Map(x => x.HoursTotal).Not.Nullable();
			this.Map(x => x.IsInitial).Not.Nullable();
			this.Map(x => x.IsSessionInitial).Not.Nullable();

			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<CommitEntity>(x => x.CommitSince).Nullable();
			this.References<CommitEntity>(x => x.CommitUntil).Not.Nullable()
				.UniqueKey("UNQ_HOURS_LOGGING");
			this.References<CommitEntity>(x => x.InitialCommit).Not.Nullable();
			this.References<HoursTypeEntity>(x => x.HoursType).Not.Nullable()
				.UniqueKey("UNQ_HOURS_LOGGING");
		}
	}
}
