/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
	public class MetricsAnalysisResult
	{
		/// <summary>
		/// Refers to the <see cref="Repository"/> that was analyzed.
		/// </summary>
		public Repository Repository { get; protected set; }

		/// <summary>
		/// Refers to the <see cref="Commit"/> that was analyzed within the repository.
		/// </summary>
		public Commit Commit { get; protected set; }

		/// <summary>
		/// There is one per <see cref="Commit"/>.
		/// </summary>
		public CommitMetricsStatusEntity CommitMetricsStatus { get; protected set; }

		/// <summary>
		/// A set of metrics that are used within the obtained metrics. It is recommended
		/// that <see cref="MetricTypeEntity"/> objects are retrieved/created using the
		/// method <see cref="MetricTypeEntity.ForSettings(string, bool, bool, bool, double, bool)"/>.
		/// </summary>
		public IList<MetricTypeEntity> MetricTypes { get; protected set; }
			= new List<MetricTypeEntity>();

		/// <summary>
		/// An enumerable of metrics as obtained from the <see cref="Repository"/> and the
		/// corresponding <see cref="Commit"/>.
		/// </summary>
		public IList<MetricEntity> Metrics { get; protected set; }
			= new List<MetricEntity>();
	}
}
