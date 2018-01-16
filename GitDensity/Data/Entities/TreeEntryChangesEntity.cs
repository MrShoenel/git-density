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

		public virtual ISet<FileBlockEntity> FileBlocks { get; set; } = new HashSet<FileBlockEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		/// <summary>
		/// Add a <see cref="FileBlockEntity"/> to this <see cref="TreeEntryChangesEntity"/>.
		/// </summary>
		/// <param name="fileBlock"></param>
		/// <returns>This (<see cref="TreeEntryChangesEntity"/>) for chaining.</returns>
		public virtual TreeEntryChangesEntity AddFileBlock(FileBlockEntity fileBlock)
		{
			lock (this.padLock)
			{
				this.FileBlocks.Add(fileBlock);
				return this;
			}
		}

		/// <summary>
		/// Add all <see cref="FileBlockEntity"/>s to this <see cref="TreeEntryChangesEntity"/>.
		/// </summary>
		/// <param name="fileBlocks"></param>
		/// <returns>This (<see cref="TreeEntryChangesEntity"/>) for chaining.</returns>
		public virtual TreeEntryChangesEntity AddFileBlocks(IEnumerable<FileBlockEntity> fileBlocks)
		{
			foreach (var fileBlock in fileBlocks)
			{
				this.AddFileBlock(fileBlock);
			}
			return this;
		}
		#endregion
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
