/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2017 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
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
	public class DeveloperEntity
	{
		public virtual UInt32 ID { get; set; }

		public virtual String Name { get; set; }

		public virtual String Email { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual ISet<CommitEntity> Commits { get; set; } = new HashSet<CommitEntity>();

		private readonly Object padLock = new Object();

		#region Methods
		public virtual DeveloperEntity AddCommit(CommitEntity commit)
		{
			lock (this.padLock)
			{
				this.Commits.Add(commit);
				return this;
			}
		}

		public virtual DeveloperEntity AddCommits(IEnumerable<CommitEntity> commits)
		{
			foreach (var commit in commits)
			{
				this.AddCommit(commit);
			}
			return this;
		}
		#endregion
	}

	public class DeveloperEntityMap : ClassMap<DeveloperEntity>
	{
		public DeveloperEntityMap()
		{
			this.Table(nameof(DeveloperEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Name).Not.Nullable();
			this.Map(x => x.Email).Not.Nullable();

			this.HasMany<CommitEntity>(x => x.Commits).Cascade.Lock();

			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}
}
