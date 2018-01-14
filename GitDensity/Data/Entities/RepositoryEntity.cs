/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
using GitDensity.Util;
using System;
using System.Collections.Generic;

namespace GitDensity.Data.Entities
{
	/// <summary>
	/// Represents a <see cref="LibGit2Sharp.Repository"/> that is associated with
	/// a number of developers through <see cref="DeveloperEntity"/> objects.
	/// </summary>
	public class RepositoryEntity
	{
		public virtual UInt32 ID { get; set; }

		[Indexed(Unique = true)]
		public virtual String Url { get; set; }
		
		[Indexed(Unique = true)]
		public virtual String ShaHead { get; set; }

		public virtual ISet<DeveloperEntity> Developers { get; set; }

		public virtual ISet<CommitEntity> Commits { get; set; }

		public virtual ProjectEntity Project { get; set; }

		public RepositoryEntity()
		{
			this.Developers = new HashSet<DeveloperEntity>();
			this.Commits = new HashSet<CommitEntity>();
		}
	}

	public class RepositoryEntityMap : ClassMap<RepositoryEntity>
	{
		public RepositoryEntityMap()
		{
			this.Table(nameof(RepositoryEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Url).Not.Nullable();
			this.Map(x => x.ShaHead).Not.Nullable().Length(40);

			this.HasMany<DeveloperEntity>(x => x.Developers).Cascade.Lock();
			this.HasMany<CommitEntity>(x => x.Commits).Cascade.Lock();

			this.References<ProjectEntity>(x => x.Project).Unique();
		}
	}
}
