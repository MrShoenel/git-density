using FluentNHibernate.Mapping;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Data.Entities
{
	public class TreeEntryChangesEntity
	{
		public virtual UInt32 ID { get; set; }

		[Indexed]
		public virtual ChangeKind Status { get; set; }

		public virtual String PathOld { get; set; }

		public virtual String PathNew { get; set; }

		public virtual CommitPairEntity CommitPair { get; set; }
	}

	public class TreeEntryChangesEntityMap : ClassMap<TreeEntryChangesEntity>
	{
		public TreeEntryChangesEntityMap()
		{
			this.Id(x => x.ID).GeneratedBy.Identity();
			this.Map(x => x.Status).CustomType<ChangeKind>().Not.Nullable();
			this.Map(x => x.PathOld).Length(1000);
			this.Map(x => x.PathNew).Length(1000);

			this.References<CommitPairEntity>(x => x.CommitPair).Not.Nullable();
		}
	}
}
