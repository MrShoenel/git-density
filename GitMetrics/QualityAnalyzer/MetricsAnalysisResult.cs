/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitMetrics. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using System.Collections.Generic;
using Util.Data.Entities;

namespace GitMetrics.QualityAnalyzer
{
	/// <summary>
	/// Class to be filled with resulting entities by implementations of
	/// <see cref="IMetricsAnalyzer"/>.
	/// Please note that the actual analyzer must not interconnect the
	/// entities (e.g. adding <see cref="MetricEntity"/> objects to the
	/// e.g. <see cref="RepositoryEntity"/> object. The only exception to
	/// this is referencing the <see cref="MetricTypeEntity"/> from within
	/// each of the <see cref="MetricEntity"/> objects.
	/// </summary>
	public class MetricsAnalysisResult
	{
		/// <summary>
		/// Refers to the <see cref="RepositoryEntity"/> that was analyzed.
		/// </summary>
		public RepositoryEntity Repository { get; set; }

		/// <summary>
		/// Refers to the <see cref="CommitEntity"/> that was analyzed within the repository.
		/// </summary>
		public CommitEntity Commit { get; set; }

		/// <summary>
		/// There is one per <see cref="Commit"/>.
		/// </summary>
		public CommitMetricsStatus CommitMetricsStatus { get; set; }

		/// <summary>
		/// A set of metrics that are used within the obtained metrics. It is recommended
		/// that <see cref="MetricTypeEntity"/> objects are retrieved/created using the
		/// method <see cref="MetricTypeEntity.ForSettings(string, bool, bool, bool, double, bool)"/>.
		/// </summary>
		public IList<MetricTypeEntity> MetricTypes { get; set; }
			= new List<MetricTypeEntity>();

		/// <summary>
		/// An enumerable of metrics as obtained from the <see cref="Repository"/> and the
		/// corresponding <see cref="Commit"/>.
		/// </summary>
		public IList<MetricEntity> Metrics { get; set; }
			= new List<MetricEntity>();
	}
}
