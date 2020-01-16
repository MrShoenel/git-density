/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
