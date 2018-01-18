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
