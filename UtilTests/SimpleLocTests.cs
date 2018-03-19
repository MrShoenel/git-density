/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project UtilTests. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
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
