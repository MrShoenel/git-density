/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitTools. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using System.Collections.Generic;
using Util;

namespace GitTools.Analysis
{
	/// <summary>
	/// Interface for all commit-analyzers to implement.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IAnalyzer<out T> :
		ISupportsExecutionPolicy where T : IAnalyzedCommit
	{
		/// <summary>
		/// Yields <see cref="IAnalyzedCommit"/> entities once run.
		/// </summary>
		/// <returns></returns>
		IEnumerable<T> AnalyzeCommits();
	}
}
