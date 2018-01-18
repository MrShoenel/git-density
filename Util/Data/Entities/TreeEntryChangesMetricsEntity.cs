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
	/// Has a 1-to-1 relation to a <see cref="TreeEntryChangesEntity"/> and holds
	/// (aggregated) metrics for it.
	/// </summary>
	public class TreeEntryChangesMetricsEntity
	{
		public virtual UInt32 ID { get; set; }

		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }

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




		public virtual UInt32 NumAdded { get; set; }

		public virtual UInt32 NumDeleted { get; set; }

		public virtual UInt32 NumAddedNoComments { get; set; }

		public virtual UInt32 NumDeletedNoComments { get; set; }



		public virtual UInt32 NumAddedPostCloneDetection { get; set; }

		public virtual UInt32 NumDeletedPostCloneDetection { get; set; }

		public virtual UInt32 NumAddedPostCloneDetectionNoComments { get; set; }

		public virtual UInt32 NumDeletedPostCloneDetectionNoComments { get; set; }



		public virtual UInt32 NumAddedClonedBlockLines { get; set; }

		public virtual UInt32 NumDeletedClonedBlockLines { get; set; }

		public virtual UInt32 NumAddedClonedBlockLinesNoComments { get; set; }

		public virtual UInt32 NumDeletedClonedBlockLinesNoComments { get; set; }
		#endregion
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

			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Unique();
		}
	}
}
