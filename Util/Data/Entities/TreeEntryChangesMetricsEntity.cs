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
using System;
using Util.Extensions;
using Util.Similarity;

namespace Util.Data.Entities
{
	/// <summary>
	/// Has a 1-to-1 relation to a <see cref="TreeEntryChangesEntity"/> and holds
	/// (aggregated) metrics for it. For each kind of similarity-measurement, one
	/// instance is stored (<see cref="SimilarityMeasurementType"/>).
	/// </summary>
	public class TreeEntryChangesMetricsEntity
	{
		/// <summary>
		/// An unsigned, auto-increment ID.
		/// </summary>
		public virtual UInt32 ID { get; set; }

		/// <summary>
		/// Designates which <see cref="SimilarityMeasurementType"/> has been applied
		/// to this entity.
		/// This property is the reason why, since commit #e9a49, all UInt32 metrics
		/// of numbers of lines have been changed to double. If multiplied by a similarity,
		/// we want to keep the exact amount, instead of rounding.
		/// </summary>
		[Indexed]
		public virtual SimilarityMeasurementType SimilarityMeasurement { get; set; }

		#region LOC
		/// <summary>
		/// Represents the physical LOC of the whole file, this change is about.
		/// Note that this property may be negative, in case of a deletion.
		/// </summary>
		public virtual Int32 LocFileGross { get; set; }
		/// <summary>
		/// Represents the LOC w/o empty lines and comments of the whole file.
		/// Like <see cref="LocFileGross"/>, this property may be negative, too.
		/// </summary>
		public virtual Int32 LocFileNoComments { get; set; }
		#endregion

		#region Aggregated file-blocks' numbers
		public virtual Double NumAdded { get; set; }

		public virtual Double NumDeleted { get; set; }

		public virtual Double NumAddedNoComments { get; set; }

		public virtual Double NumDeletedNoComments { get; set; }



		public virtual Double NumAddedPostCloneDetection { get; set; }

		public virtual Double NumDeletedPostCloneDetection { get; set; }

		public virtual Double NumAddedPostCloneDetectionNoComments { get; set; }

		public virtual Double NumDeletedPostCloneDetectionNoComments { get; set; }



		public virtual Double NumAddedClonedBlockLines { get; set; }

		public virtual Double NumDeletedClonedBlockLines { get; set; }

		public virtual Double NumAddedClonedBlockLinesNoComments { get; set; }

		public virtual Double NumDeletedClonedBlockLinesNoComments { get; set; }
		#endregion
		
		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }
	}

	public class TreeEntryChangesMetricsEntityMap : ClassMap<TreeEntryChangesMetricsEntity>
	{
		public TreeEntryChangesMetricsEntityMap()
		{
			this.Table(nameof(TreeEntryChangesMetricsEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.LocFileGross).Not.Nullable();
			this.Map(x => x.LocFileNoComments).Not.Nullable();

			this.Map(x => x.NumAdded).Not.Nullable();
			this.Map(x => x.NumDeleted).Not.Nullable();
			this.Map(x => x.NumAddedNoComments).Not.Nullable();
			this.Map(x => x.NumDeletedNoComments).Not.Nullable();

			this.Map(x => x.NumAddedPostCloneDetection).Not.Nullable();
			this.Map(x => x.NumDeletedPostCloneDetection).Not.Nullable();
			this.Map(x => x.NumAddedPostCloneDetectionNoComments).Not.Nullable();
			this.Map(x => x.NumDeletedPostCloneDetectionNoComments).Not.Nullable();

			this.Map(x => x.NumAddedClonedBlockLines).Not.Nullable();
			this.Map(x => x.NumDeletedClonedBlockLines).Not.Nullable();
			this.Map(x => x.NumAddedClonedBlockLinesNoComments).Not.Nullable();
			this.Map(x => x.NumDeletedClonedBlockLinesNoComments).Not.Nullable();

			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChanges)
				.Not.Nullable().UniqueKey("UNQ_METRICS");
			this.Map(x => x.SimilarityMeasurement)
				.CustomType<SimilarityMeasurementType>()
				.Not.Nullable().UniqueKey("UNQ_METRICS");
		}
	}
}
