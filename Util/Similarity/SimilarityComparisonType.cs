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
	public enum SimilarityComparisonType
	{
		/// <summary>
		/// Describes the string similarity between two <see cref="ClonesXmlSetBlock"/>s
		/// that have not been modified.
		/// </summary>
		BlockSimilarity = 1,

		/// <summary>
		/// Describes the similarity of two <see cref="ClonesXmlSetBlock"/>s after all
		/// cloned lines have been eliminated in both of them.
		/// </summary>
		PostCloneBlockSimilarity = 2,

		/// <summary>
		/// Describes the similarity between between two <see cref="ClonesXmlSetBlock"/>s
		/// of a <see cref="ClonesXmlSet"/> having exactly two blocks, where each block
		/// points to a different version of the same file. For each <see cref="Hunk"/>
		/// there may be more than one set of two blocks, so this value will represent the
		/// average similarity for all sets.
		/// </summary>
		ClonedBlockLinesSimilarity = 3,

		/// <summary>
		/// Like <see cref="BlockSimilarity"/>, but compares blocks without comments or
		/// empty lines.
		/// </summary>
		BlockSimilarityNoComments = 4,

		/// <summary>
		/// Like <see cref="PostCloneBlockSimilarity"/>, but compares blocks without comments or
		/// empty lines.
		/// </summary>
		PostCloneBlockSimilarityNoComments = 5,

		/// <summary>
		/// Like <see cref="ClonedBlockLinesSimilarity"/>, but compares blocks without comments or
		/// empty lines.
		/// </summary>
		ClonedBlockLinesSimilarityNoComments = 6
	}
}
