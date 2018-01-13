using FluentNHibernate.Mapping;
using GitDensity.Util;
using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitDensity.Data.Entities
{
	public class TreeEntryChangesEntity
	{
		public virtual UInt32 ID { get; set; }

		[Indexed]
		public virtual ChangeKind Status { get; set; }

		public virtual String PathOld { get; set; }

		public virtual String PathNew { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }

		public virtual ISet<FileBlockEntity> FileBlocks { get; set; }
	}

	public class TreeEntryChangesEntityMap : ClassMap<TreeEntryChangesEntity>
	{
		public TreeEntryChangesEntityMap()
		{
			this.Table(nameof(TreeEntryChangesEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Status).CustomType<ChangeKind>().Not.Nullable();
			this.Map(x => x.PathOld).Length(1000);
			this.Map(x => x.PathNew).Length(1000);

			this.HasMany<FileBlockEntity>(x => x.FileBlocks).Cascade.Lock();

			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
		}
	}
}
