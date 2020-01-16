/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Util;
using Util.Data.Entities;

namespace GitMetrics.QualityAnalyzer
{
	/// <summary>
	/// To be implemented by various analyzers that can extract software metrics from any
	/// software project. Each analyzer must implement a parameter-less constructor.
	/// </summary>
	public interface IMetricsAnalyzer
	{
		/// <summary>
		/// This method analyzes a <see cref="Repository"/> w.r.t. its metrics at a given
		/// <see cref="Commit"/>. If necessary, the analyzer implementation must checkout
		/// the commit (i.e. create a detached head). The analyzer is guaranteed to have
		/// exclusive access to the <see cref="Repository"/> (i.e. it is operating on an
		/// exclusive copy or similar).
		/// </summary>
		/// <param name="clonedRepository">The cloned repository.</param>
		/// <param name="originalRepositoryEntity">The <see cref="RepositoryEntity"/> to add all
		/// newly created entities to (please note that the underlying <see cref="Repository"/>)
		/// points to the cloned copy, not the original that the first parameter points to.</param>
		/// <param name="originalCommitEntity">The specific <see cref="CommitEntity"/> to check out.</param>
		/// <returns>An instance of <see cref="MetricsAnalysisResult"/>.</returns>
		MetricsAnalysisResult Analyze(Repository clonedRepository, RepositoryEntity originalRepositoryEntity, CommitEntity originalCommitEntity);

		/// <summary>
		/// To be called by the factory, so that any analyzer can accept configuration.
		/// </summary>
		MetricsAnalyzerConfiguration Configuration { set; }
	}
}
