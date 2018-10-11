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

namespace GitMetrics.QualityAnalyzer
{
	/// <summary>
	/// To be implemented by various analyzers that can extract software metrics from any
	/// software project.
	/// </summary>
	public interface IMetricsAnalyzer
	{
		/// <summary>
		/// This method analyzes a <see cref="Repository"/> w.r.t. its metrics at a given
		/// <see cref="Commit"/>. If necessary, the analyzer implementation must checkout
		/// the commit (i.e. create a detached head). The analyzer is guaranteed to have
		/// exclusive access to the <see cref="Repository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> to analyze.</param>
		/// <param name="commit">The specific <see cref="Commit"/> to check out.</param>
		/// <returns>An instance of <see cref="IMetricsAnalysisResult"/>.</returns>
		IMetricsAnalysisResult Analyze(Repository repository, Commit commit);
	}
}
