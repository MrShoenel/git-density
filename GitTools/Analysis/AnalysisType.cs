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
using System;

namespace GitTools.Analysis
{
	/// <summary>
	/// Used as part of <see cref="GitTools.Program"/>.
	/// </summary>
	public enum AnalysisType : Int32
	{
		/// <summary>
		/// Will lead to using the <see cref="SimpleAnalyzer.SimpleAnalyzer"/>.
		/// </summary>
		Simple = 1,

		/// <summary>
		/// Will lead to using the <see cref="ExtendedAnalyzer.ExtendedAnalyzer"/>.
		/// </summary>
		Extended = 2
	}
}
