/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Util.Similarity;

namespace Util.Data.Entities
{
	/// <summary>
	/// This entity represents a file in a changeset and the kind of changes that were
	/// made to it (e.g. added, deleted, changed etc.). Therefore, the file's path before
	/// and after the operation is retained. Since a <see cref="TreeEntryChangesEntity"/>
	/// depicts the changes to a file between two commits, it also references the pair
	/// of commits (<see cref="CommitPairEntity"/>).
	/// </summary>
	public class TreeEntryChangesEntity : BaseEntity<TreeEntryChanges>
	{
		/// <summary>
		/// An unsigned, auto-increment ID.
		/// </summary>
		public virtual UInt32 ID { get; set; }

		/// <summary>
		/// Refers to the kind of change made to the file.
		/// </summary>
		[Indexed]
		public virtual ChangeKind Status { get; set; }

		/// <summary>
		/// The path to the file within the repository before the change.
		/// </summary>
		public virtual String PathOld { get; set; }

		/// <summary>
		/// The path to the file within the repository after the change.
		/// </summary>
		public virtual String PathNew { get; set; }

		/// <summary>
		/// The related <see cref="CommitPairEntity"/>.
		/// </summary>
		public virtual CommitPairEntity CommitPair { get; set; }

		/// <summary>
		/// A set of <see cref="FileBlockEntity"/> objects that represent changes to
		/// the file on block-level.
		/// </summary>
		public virtual ISet<FileBlockEntity> FileBlocks { get; set; }
			= new HashSet<FileBlockEntity>();

		/// <summary>
		/// A set of related <see cref="TreeEntryChangesMetricsEntity"/> for this entity.
		/// For each similarity-measurement, we may get different metrics, that is why we
		/// have a set here.
		/// </summary>
		public virtual ISet<TreeEntryChangesMetricsEntity> TreeEntryChangesMetrics { get; set; }
			= new HashSet<TreeEntryChangesMetricsEntity>();

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

			this.HasMany<TreeEntryChangesMetricsEntity>(x => x.TreeEntryChangesMetrics).Cascade.Lock();
			this.HasMany<FileBlockEntity>(x => x.FileBlocks).Cascade.Lock();

			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
		}
	}
}
