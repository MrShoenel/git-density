using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using Util.Extensions;

namespace Util.Data.Entities
{
	public class CommitPairEntity
	{
		public virtual String ID { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual CommitEntity ChildCommit { get; set; }

		public virtual CommitEntity ParentCommit { get; set; }

		public virtual ISet<TreeEntryChangesEntity> TreeEntryChanges { get; set; } = new HashSet<TreeEntryChangesEntity>();

		public virtual ISet<FileBlockEntity> FileBlocks { get; set; } = new HashSet<FileBlockEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		public virtual CommitPairEntity AddTreeEntryChanges(TreeEntryChangesEntity tece)
		{
			lock (this.padLock)
			{
				this.TreeEntryChanges.Add(tece);
				return this;
			}
		}

		public virtual CommitPairEntity AddFileBlock(FileBlockEntity fileBlock)
		{
			lock (this.padLock)
			{
				this.FileBlocks.Add(fileBlock);
				return this;
			}
		}

		public virtual CommitPairEntity AddFileBlocks(IEnumerable<FileBlockEntity> fileBlocks)
		{
			foreach (var fileBlock in fileBlocks)
			{
				this.AddFileBlock(fileBlock);
			}
			return this;
		}
		#endregion
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
