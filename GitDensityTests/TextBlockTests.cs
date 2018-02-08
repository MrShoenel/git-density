/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensityTests. All files in this project,
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
using GitDensity.Similarity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Extensions;

namespace GitDensityTests
{
	[TestClass]
	public class TextBlockTests
	{
		public const String EmptyLinesAndSingleLineCommentsBlock =
			" asdf text \n" +
			"jo //\n" +
			"\t \n" +
			"// bla\n" +
			" more comments..\n" +
			"\n"
			;

		public const String MixedTextBlock =
			" asdf text \n" +
			"jo //\n" +
			"\t \n" +
			"// bla\n" +
			"asdf /* kk\n" +
			" more comments..\n" +
			" kk*/\n" +
			"oneline/*stuff*/\n"
			;

		[TestMethod]
		public void TestRemoveEmptyLinesAndSingleLineComments()
		{
			var tb = CreateFromString(EmptyLinesAndSingleLineCommentsBlock);
			tb.RemoveEmptyLinesAndComments(out TextBlock newTb);

			Assert.AreEqual(3u, tb.LinesAdded);
			Assert.AreEqual(4u, newTb.LinesAdded);
		}

		[TestMethod]
		public void TestRemoveEmptyLinesAndAllComments()
		{
			var tb = CreateFromString(MixedTextBlock);

			// Pulls out empties and comment-lines
			tb.RemoveEmptyLinesAndComments(out TextBlock tbWithoutEmptyLinesAndComments);

			Assert.AreEqual(4u, tb.LinesAdded);
			Assert.AreEqual(7u, tbWithoutEmptyLinesAndComments.LinesAdded);
		}

		internal static TextBlock CreateFromString(String @string)
		{
			var tb = new TextBlock();
			UInt32 idx = 1;
			foreach (var line in @string.GetLines(removeEmptyLines: false))
			{
				tb.AddLine(new Line(LineType.Added, idx++, line));
			}
			return tb;
		}
	}
}
