/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
///
/// ---------------------------------------------------------------------------------
///
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

		[Indexed]
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
