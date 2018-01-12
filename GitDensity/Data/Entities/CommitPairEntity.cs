using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Data.Entities
{
	public class CommitPairEntity
	{
		public virtual String ID { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		public virtual CommitEntity ChildCommit { get; set; }

		public virtual CommitEntity ParentCommit { get; set; }

		public virtual ISet<TreeEntryChangesEntity> TreeEntryChanges { get; set; }
	}

	public class CommitPairEntityMap : ClassMap<CommitPairEntity>
	{
		public CommitPairEntityMap()
		{
			this.Id(x => x.ID).GeneratedBy.Assigned().Length(32); // as guaranteed by CommitPair

			this.HasMany<TreeEntryChangesEntity>(x => x.TreeEntryChanges).Cascade.Lock();
			
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable();
			this.References<CommitEntity>(x => x.ChildCommit).Not.Nullable();
			this.References<CommitEntity>(x => x.ParentCommit).Not.Nullable();
		}
	}
}
