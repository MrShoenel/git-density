using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using FluentNHibernate.Mapping;

namespace GitDensity.Data.Entities
{
	/// <summary>
	/// An entity that can represent the most essential parts of a <see cref="Commit"/>.
	/// </summary>
	public class CommitEntity
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
	}


	public class CommitEntityMap : ClassMap<CommitEntity>
	{
		public CommitEntityMap()
		{
			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.HashSHA1).Not.Nullable().Length(40);
			this.Map(x => x.CommitDate).Not.Nullable();
			this.Map(x => x.IsMergeCommit).Not.Nullable();

			this.References<DeveloperEntity>(x => x.Developer).Not.Nullable();
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
		}
	}
}
