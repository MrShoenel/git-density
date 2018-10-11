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
	public interface IMetricsAnalysisResult
	{
		/// <summary>
		/// Refers to the <see cref="Repository"/> that was analyzed.
		/// </summary>
		Repository Repository { get; }

		/// <summary>
		/// Refers to the <see cref="Commit"/> that was analyzed within the repository.
		/// </summary>
		Commit Commit { get; }

		/// <summary>
		/// There is one per <see cref="Commit"/>.
		/// </summary>
		CommitMetricsStatusEntity CommitMetricsStatus { get; }

		/// <summary>
		/// A set of metrics that are used within the obtained metrics. It is recommended
		/// that <see cref="MetricTypeEntity"/> objects are retrieved/created using the
		/// method <see cref="MetricTypeEntity.ForSettings(string, bool, bool, bool, double, bool)"/>.
		/// </summary>
		IEnumerable<MetricTypeEntity> MetricTypes { get; }

		/// <summary>
		/// An enumerable of metrics as obtained from the <see cref="Repository"/> and the
		/// corresponding <see cref="Commit"/>.
		/// </summary>
		IEnumerable<MetricEntity> Metrics { get; }
	}
}
