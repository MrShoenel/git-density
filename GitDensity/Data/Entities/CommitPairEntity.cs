using FluentNHibernate.Mapping;
using GitDensity.Util;
using System;
using System.Collections.Generic;

namespace GitDensity.Data.Entities
{
	public class CommitPairEntity
	{
		public virtual String ID { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual CommitEntity ChildCommit { get; set; }

		public virtual CommitEntity ParentCommit { get; set; }

		public virtual ISet<TreeEntryChangesEntity> TreeEntryChanges { get; set; }

		public virtual ISet<FileBlockEntity> FileBlocks { get; set; }
	}

	public class CommitPairEntityMap : ClassMap<CommitPairEntity>
	{
		public CommitPairEntityMap()
		{
			this.Table(nameof(CommitPairEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Assigned().Length(32); // as guaranteed by CommitPair

			this.HasMany<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Cascade.Lock();
			this.HasMany<FileBlockEntity>(x => x.FileBlocks).Cascade.Lock();
			
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
			this.References<CommitEntity>(x => x.ChildCommit).Not.Nullable();
			this.References<CommitEntity>(x => x.ParentCommit).Not.Nullable();
		}
	}
}
