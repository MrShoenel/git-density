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
