using FluentNHibernate.Mapping;
using GitDensity.Util;
using System;

namespace GitDensity.Data.Entities
{
	public enum FileBlockType
	{
		Added,
		Deleted
	}

	public class FileBlockEntity
	{
		public virtual UInt32 ID { get; set; }

		public virtual FileBlockType FileBlockType { get; set; }

		public virtual UInt32 OldStart { get; set; }

		public virtual UInt32 OldAmount { get; set; }

		public virtual UInt32 NewStart { get; set; }

		public virtual UInt32 NewAmount { get; set; }

		public virtual UInt32 NumAdded { get; set; }

		public virtual UInt32 NumDeleted { get; set; }

		public virtual UInt32 NumAddedPostCloneDetection { get; set; }

		public virtual UInt32 NumDeletedPostCloneDetection { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }

		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }
	}

	public class FileBlockEntityMap : ClassMap<FileBlockEntity>
	{
		public FileBlockEntityMap()
		{
			this.Table(nameof(FileBlockEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.FileBlockType).CustomType<FileBlockType>().Not.Nullable();
			this.Map(x => x.OldStart).Not.Nullable();
			this.Map(x => x.OldAmount).Not.Nullable();
			this.Map(x => x.NewStart).Not.Nullable();
			this.Map(x => x.NewAmount).Not.Nullable();
			this.Map(x => x.NumAdded).Not.Nullable();
			this.Map(x => x.NumDeleted).Not.Nullable();
			this.Map(x => x.NumAddedPostCloneDetection).Not.Nullable();
			this.Map(x => x.NumDeletedPostCloneDetection).Not.Nullable();

			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Not.Nullable();
		}
	}
}
