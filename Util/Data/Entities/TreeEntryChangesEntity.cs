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
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using Util.Extensions;
using Util.Similarity;

namespace Util.Data.Entities
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

		///// <summary>
		///// For each <see cref="SimilarityMeasurementType"/>, this entity references a
		///// different <see cref="TreeEntryChangesMetricsEntity"/>.
		///// </summary>
		//public virtual IDictionary<SimilarityMeasurementType, TreeEntryChangesMetricsEntity> TreeEntryChangesMetrics { get; set; } = new Dictionary<SimilarityMeasurementType, TreeEntryChangesMetricsEntity>();

		public virtual ISet<TreeEntryChangesMetricsEntity> TreeEntryChangesMetrics { get; set; } = new HashSet<TreeEntryChangesMetricsEntity>();

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
