using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Util.Extensions;

namespace UtilTests
{
	[TestClass]
	public class SimpleLocTests
	{
		public const string SingleLineComments =
			"var a = 1; // foo\n" +
			"// delete this\n" +
			"\t  //this, too"
			;

		[TestMethod]
		public void TestSingleLineComments()
		{
			var simpleLoc = new Util.Metrics.SimpleLoc(SingleLineComments.GetLines(removeEmptyLines: false));

			Assert.AreEqual(3u, simpleLoc.LocGross);
			Assert.AreEqual(1u, simpleLoc.LocNoComments);
		}
	}
}
