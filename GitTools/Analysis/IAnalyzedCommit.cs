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
using LibGit2Sharp;
using LINQtoCSV;
using System;

namespace GitTools.Analysis
{
	/// <summary>
	/// Base interface for <see cref="Commit"/>s as analyzed by instances
	/// of <see cref="IAnalyzer{T}"/>.
	/// </summary>
	public interface IAnalyzedCommit
	{
		/// <summary>
		/// The commit's SHA1.
		/// </summary>
		[CsvColumn]
		String SHA1 { get; }
	}
}
