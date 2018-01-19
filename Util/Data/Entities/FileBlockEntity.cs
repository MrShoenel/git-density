using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using Util.Extensions;

namespace Util.Data.Entities
{
	public enum FileBlockType
	{
		Added = 1,
		Deleted = 2,
		Modified = 3
	}

	public class FileBlockEntity
	{
		#region Columns and virtual entities
		public virtual UInt32 ID { get; set; }

		public virtual FileBlockType FileBlockType { get; set; }

		public virtual UInt32 OldStart { get; set; }

		public virtual UInt32 OldAmount { get; set; }

		public virtual UInt32 NewStart { get; set; }

		public virtual UInt32 NewAmount { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }

		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }

		public virtual ISet<SimilarityEntity> Similarities { get; set; } = new HashSet<SimilarityEntity>();
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
