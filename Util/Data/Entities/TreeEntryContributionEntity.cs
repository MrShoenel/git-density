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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Util.Similarity;
using FluentNHibernate.Mapping;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// A contribution-entity concerns a single file in a single <see cref="Commit"/>.
	/// It is specific to a single <see cref="DeveloperEntity"/>, <see cref="RepositoryEntity"/>,
	/// <see cref="CommitPairEntity"/>, <see cref="TreeEntryChangesEntity"/>,
	/// <see cref="TreeEntryChangesMetricsEntity"/> and <see cref="SimilarityMeasurementType"/>.
	/// All these give a specific notion of size to the contribution. The 2nd part of the
	/// contribution is a notion of time, so it references an <see cref="HoursEntity"/>.
	/// </summary>
	public class TreeEntryContributionEntity
	{
		public virtual UInt32 ID { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual DeveloperEntity Developer { get; set; }

		public virtual CommitEntity Commit { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }

		public virtual TreeEntryChangesEntity TreeEntryChanges { get; set; }

		public virtual TreeEntryChangesMetricsEntity TreeEntryChangesMetrics { get; set; }

		[Indexed]
		public virtual SimilarityMeasurementType SimilarityMeasurementType { get; set; }

		public static TreeEntryContributionEntity Create(RepositoryEntity repository, DeveloperEntity developer, CommitEntity commit, CommitPairEntity commitPair, TreeEntryChangesEntity treeEntryChanges, TreeEntryChangesMetricsEntity treeEntryChangesMetrics, SimilarityMeasurementType similarityMeasurementType)
		{
			var entity = new TreeEntryContributionEntity
			{
				Repository = repository,
				Developer = developer,
				Commit = commit,
				CommitPair = commitPair,
				TreeEntryChanges = treeEntryChanges,
				TreeEntryChangesMetrics = treeEntryChangesMetrics,
				SimilarityMeasurementType = similarityMeasurementType
			};

			repository.AddContribution(entity);
			developer.AddContribution(entity);
			commit.AddContribution(entity);
			commitPair.AddContribution(entity);

			return entity;
		}
	}

	public class TreeEntryContributionEntityMap : ClassMap<TreeEntryContributionEntity>
	{
		public TreeEntryContributionEntityMap()
		{
			this.Table(nameof(TreeEntryContributionEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.SimilarityMeasurementType)
				.CustomType<SimilarityMeasurementType>().Not.Nullable();

			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<CommitEntity>(x => x.Commit).Not.Nullable();
			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
			this.References<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Not.Nullable();
			this.References<TreeEntryChangesMetricsEntity>(x => x.TreeEntryChangesMetrics).Not.Nullable();
		}
	}
}
