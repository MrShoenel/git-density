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
	/// <summary>
	/// An entity that can represent the most essential parts of a <see cref="Commit"/>.
	/// </summary>
	public class CommitEntity : IEquatable<CommitEntity>
	{
		public virtual UInt32 ID { get; set; }

		[Indexed(Unique = true)]
		public virtual String HashSHA1 { get; set; }

		[Indexed]
		public virtual DateTime CommitDate { get; set; }

		[Indexed]
		public virtual Boolean IsMergeCommit { get; set; }

		public virtual DeveloperEntity Developer { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual ISet<TreeEntryContributionEntity> TreeEntryContributions { get; set; } = new HashSet<TreeEntryContributionEntity>();

		private readonly Object padLock = new Object();

		public virtual CommitEntity AddContribution(TreeEntryContributionEntity contribution)
		{
			lock (padLock)
			{
				this.TreeEntryContributions.Add(contribution);
				return this;
			}
		}

		public virtual CommitEntity AddContributions(IEnumerable<TreeEntryContributionEntity> contributions)
		{
			foreach (var contribution in contributions)
			{
				this.AddContribution(contribution);
			}
			return this;
		}

		#region equality
		public virtual bool Equals(CommitEntity other)
		{
			return other is CommitEntity && other.HashSHA1 == this.HashSHA1;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as CommitEntity);
		}

		public override int GetHashCode()
		{
			return this.HashSHA1.GetHashCode() * 31;
		}
		#endregion
	}


	public class CommitEntityMap : ClassMap<CommitEntity>
	{
		public CommitEntityMap()
		{
			this.Table(nameof(CommitEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.HashSHA1).Not.Nullable().Length(40);
			this.Map(x => x.CommitDate).Not.Nullable();
			this.Map(x => x.IsMergeCommit).Not.Nullable();

			this.HasMany<TreeEntryContributionEntity>(x => x.TreeEntryContributions).Cascade.Lock();

			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}
}
