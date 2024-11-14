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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Defines the possible entity types that metrics can be assigned to. Usually
	/// those various instances appear in the same list and can be differentiated
	/// using this value.
	/// </summary>
	public enum OutputEntityType
	{
		/// <summary>
		/// Refers to the whole analyzed code (could probably be even more than one
		/// project).
		/// </summary>
		Upload = 0,
		/// <summary>
		/// Refers to the one project.
		/// </summary>
		Project = 1,
		/// <summary>
		/// Refers to one file within a project.
		/// </summary>
		File = 2
	}


	/// <summary>
	/// This type of entity stores one measurement of one type for a specific commit
	/// and optionally a specific file within that commit. E.g. if one were to measure
	/// LOC and SIZE for all files, then for each file in a commit, we would store two
	/// <see cref="MetricEntity"/>, one for LOC and one for SIZE. Also, since we are
	/// storing for a specific file, the reference to the <see cref="TreeEntryChange"/>
	/// would not be null.
	/// </summary>
	public class MetricEntity
	{
		public virtual UInt32 ID { get; set; }

		/// <summary>
		/// E.g. project, file etc.
		/// </summary>
		[Indexed]
		public virtual OutputEntityType OutputEntityType { get; set; }

		/// <summary>
		/// The actual numeric value of the obtained metric, like 42 or 0.43.
		/// </summary>
		[Indexed]
		public virtual Double MetricValue { get; set; }

		/// <summary>
		/// Each measurement refers to a <see cref="CommitEntity"/> and is therefore
		/// specific to it.
		/// </summary>
		public virtual CommitEntity Commit { get; set; }

		/// <summary>
		/// Should only refer to a file of <see cref="OutputEntityType"/> is of
		/// type <see cref="OutputEntityType.File"/>. Otherwise, this reference
		/// is null.
		/// </summary>
		public virtual TreeEntryChangesEntity TreeEntryChange { get; set; } = null;

		/// <summary>
		/// What kind of metric, e.g. LOC.
		/// </summary>
		public virtual MetricTypeEntity MetricType { get; set; }
	}

	public class MetricEntityMap : ClassMap<MetricEntity>
	{
		public MetricEntityMap()
		{
			this.Table(nameof(MetricEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.OutputEntityType)
				.CustomType<OutputEntityType>()
				.Not.Nullable();
			this.Map(x => x.MetricValue).Not.Nullable();

			this.References<CommitEntity>(x => x.Commit)
				.Not.Nullable();
			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChange)
				.Nullable();
			this.References<MetricTypeEntity>(x => x.MetricType)
				.Not.Nullable();
		}
	}
}
