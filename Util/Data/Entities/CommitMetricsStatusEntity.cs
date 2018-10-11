/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// For each commit, we can obtain metrics. For that purpose, the project has to
	/// be built at that commit. This can either work or some error can occur. In case
	/// of an error, we are interested in the kind of error.
	/// </summary>
	public enum CommitMetricsStatus
	{
		/// <summary>
		/// The commit could be built and metrics were obtained ordinarily.
		/// </summary>
		OK = 0,
		/// <summary>
		/// The commit could not be built and metrics could not be obtained.
		/// </summary>
		BuildError = 1,
		/// <summary>
		/// The current project (its repository) does not support building/obtaining metrics.
		/// </summary>
		InvalidProjectType = 2
	}


	/// <summary>
	/// This entity shall be mainly used for joining, so that metrics are only
	/// selected if the status is OK and hence the metrics could be obtained.
	/// </summary>
	public class CommitMetricsStatusEntity
	{
		#region Columns and virtual entities
		public virtual UInt32 ID { get; set; }

		/// <summary>
		/// The status attached for the commit that is related.
		/// </summary>
		[Indexed]
		public virtual CommitMetricsStatus MetricsStatus { get; set; }

		/// <summary>
		/// There can only be one status for each commit.
		/// </summary>
		[Indexed(Unique = true)]
		public virtual CommitEntity Commit { get; set; }
		#endregion
	}

	public class CommitMetricsStatusEntityMap : ClassMap<CommitMetricsStatusEntity>
	{
		public CommitMetricsStatusEntityMap()
		{
			this.Table(nameof(CommitMetricsStatusEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.MetricsStatus)
				.CustomType<CommitMetricsStatus>().Not.Nullable();

			this.References<CommitEntity>(x => x.Commit).Not.Nullable();
		}
	}
}
