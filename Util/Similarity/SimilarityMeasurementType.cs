/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
