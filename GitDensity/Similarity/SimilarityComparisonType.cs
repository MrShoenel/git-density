using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitDensity.Density;
using GitDensity.Density.CloneDensity;

namespace GitDensity.Similarity
{
	public enum SimilarityComparisonType
	{
		/// <summary>
		/// Describes the string similarity between two <see cref="ClonesXmlSetBlock"/>s
		/// that have not been modified.
		/// </summary>
		BlockSimilarity,

		/// <summary>
		/// Describes the similarity of two <see cref="ClonesXmlSetBlock"/>s after all
		/// cloned lines have been eliminated in both of them.
		/// </summary>
		PostCloneBlockSimilarity,

		/// <summary>
		/// Describes the similarity between between two <see cref="ClonesXmlSetBlock"/>s
		/// of a <see cref="ClonesXmlSet"/> having exactly two blocks, where each block
		/// points to a different version of the same file. For each <see cref="Hunk"/>
		/// there may be more than one set of two blocks, so this value will represent the
		/// average similarity for all sets.
		/// </summary>
		ClonedBlockLinesSimilarity
	}
}
