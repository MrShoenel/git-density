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
using System;

namespace GitDensity.Data.Entities
{
	public enum ProjectEntityLanguage
	{
		Java, PHP, C, CSharp
	}

	/// <summary>
	/// Represents an entity from the 'projects' table.
	/// </summary>
	public class ProjectEntity
	{
		public virtual UInt64 AiId { get; set; }
		public virtual UInt64 InternalId { get; set; }
		public virtual String Name { get; set; }
		public virtual ProjectEntityLanguage Language { get; set; }
		public virtual String CloneUrl { get; set; }

		public virtual RepositoryEntity Repository { get; set; }
	}

	/// <summary>
	/// Maps the entity-class <see cref="ProjectEntity"/>.
	/// </summary>
	public class ProjectEntityMap : ClassMap<ProjectEntity>
	{
		public ProjectEntityMap()
		{
			this.Table("projects");
			this.Id(x => x.AiId).Column("AI_ID");
			this.Map(x => x.InternalId).Column("INTERNAL_ID").Index("IDX_INTERNAL_ID");
			this.Map(x => x.Name).Column("NAME");
			this.Map(x => x.Language).Column("LANGUAGE")
				.Index("IDX_LANGUAGE")
				.CustomType<StringEnumMapper<ProjectEntityLanguage>>();
			this.Map(x => x.CloneUrl).Column("CLONE_URL");

			this.HasOne<RepositoryEntity>(x => x.Repository).Cascade.Lock();
		}
	}
}
