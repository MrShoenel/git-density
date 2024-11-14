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
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Represents the type of block that was changed within a file. This could be a new,
	/// deleted or changed block.
	/// </summary>
	public enum FileBlockType
	{
		Added = 1,
		Deleted = 2,
		Modified = 3
	}

	/// <summary>
	/// A <see cref="FileBlockEntity"/> represents a specific set of continouos lines
	/// within a file that changed. This could be added, deleted or changed lines. It
	/// represents how many lines changed and where the change occurred.
	/// </summary>
	public class FileBlockEntity
	{
		#region Columns and virtual entities
		public virtual UInt32 ID { get; set; }

		[Indexed]
		public virtual FileBlockType FileBlockType { get; set; }

		public virtual UInt32 OldStart { get; set; }

		public virtual UInt32 OldAmount { get; set; }

		public virtual UInt32 NewStart { get; set; }

		public virtual UInt32 NewAmount { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }

		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }

		public virtual ISet<SimilarityEntity> Similarities { get; set; }
			= new HashSet<SimilarityEntity>();
		#endregion

		private readonly Object padLock = new Object();

		/// <summary>
		/// Add a <see cref="SimilarityEntity"/> to this <see cref="FileBlockEntity"/>.
		/// </summary>
		/// <param name="similarity"></param>
		/// <returns>This (<see cref="FileBlockEntity"/>) for chaining.</returns>
		public virtual FileBlockEntity AddSimilarity(SimilarityEntity similarity)
		{
			lock (this.padLock)
			{
				similarity.FileBlock = this;
				this.Similarities.Add(similarity);
				return this;
			}
		}

		/// <summary>
		/// Add all <see cref="SimilarityEntity"/>s to this <see cref="FileBlockEntity"/>.
		/// </summary>
		/// <param name="similarities"></param>
		/// <returns>This (<see cref="FileBlockEntity"/>) for chaining.</returns>
		public virtual FileBlockEntity AddSimilarities(IEnumerable<SimilarityEntity> similarities)
		{
			foreach (var similarity in similarities)
			{
				this.AddSimilarity(similarity);
			}
			return this;
		}
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

			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Not.Nullable();

			this.HasMany<SimilarityEntity>(x => x.Similarities).Cascade.Lock();
		}
	}
}
