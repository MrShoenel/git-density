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
using F23.StringSimilarity.Interfaces;

namespace Util.Similarity
{
	/// <summary>
	/// Class that implements <see cref="INormalizedStringDistance"/> and
	/// always returns 1 for the distance and 0 for the similarity. Mainly
	/// used for <see cref="SimilarityMeasurementType.None"/>, i.e. when no
	/// similarity shall be computed.
	/// </summary>
	public class DefaultNoSimilarityMeasurement : INormalizedStringDistance, INormalizedStringSimilarity
	{
		/// <summary>
		/// Always returns 1 and does not do any computations.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns>1</returns>
		public double Distance(string s1, string s2)
		{
			return 1d;
		}

		/// <summary>
		/// Always returns 0 and does not do any computations.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns>0</returns>
		public double Similarity(string s1, string s2)
		{
			return 0d;
		}
	}
}
