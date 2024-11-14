/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util.Similarity
{
	/// <summary>
	/// An enumerations to fully identify a type of <see cref="F23.StringSimilarity.Interfaces.IStringSimilarity"/>
	/// with its (optional) shingle-parameter.
	/// </summary>
	public enum SimilarityMeasurementType : Int32
	{
		None = 0,

		#region M*N
		NormalizedLevenshtein = 1,
		JaroWinkler = 2,
		MetricLongestCommonSubSequence = 3,

		NGram2 = 4,
		NGram3 = 5,
		NGram4 = 6,
		NGram5 = 7,
		NGram6 = 8,
		#endregion

		#region M+N
		Cosine2 = 9,
		Cosine3 = 10,
		Cosine4 = 11,
		Cosine5 = 12,
		Cosine6 = 13,

		Jaccard2 = 14,
		Jaccard3 = 15,
		Jaccard4 = 16,
		Jaccard5 = 17,
		Jaccard6 = 18,

		SorensenDice2 = 19,
		SorensenDice3 = 20,
		SorensenDice4 = 21,
		SorensenDice5 = 22,
		SorensenDice6 = 23,
		#endregion
	}
}
