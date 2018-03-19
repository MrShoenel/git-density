/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensityTests. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitDensityTests
{
	[TestClass]
	public class HunkTests
	{
		[TestMethod]
		public void TestPatchSplit()
		{
			var r = GitDensity.Density.Hunk.HunkSplitRegex;

			var normal = "@@ -1,2 +0,40 @@";
			var noNew = "@@ -4,7 +8 @@"; // add new empty file
			var noOld = "@@ -1 +33,1 @@"; // delete file


			var mNormal = r.Match(normal);
			Assert.AreEqual(4, mNormal.Groups.Cast<Group>().Where(grp => grp.Success).Count() - 1);
			Assert.AreEqual("1", mNormal.Groups["oldStart"].Value);
			Assert.AreEqual("2", mNormal.Groups["oldNum"].Value);
			Assert.AreEqual("0", mNormal.Groups["newStart"].Value);
			Assert.AreEqual("40", mNormal.Groups["newNum"].Value);


			var mNoNew = r.Match(noNew);
			Assert.AreEqual(3, mNoNew.Groups.Cast<Group>().Where(grp => grp.Success).Count() - 1);
			Assert.AreEqual("4", mNoNew.Groups["oldStart"].Value);
			Assert.AreEqual("7", mNoNew.Groups["oldNum"].Value);
			Assert.AreEqual("8", mNoNew.Groups["newNum"].Value);

			var mNoOld = r.Match(noOld);
			Assert.AreEqual(3, mNoNew.Groups.Cast<Group>().Where(grp => grp.Success).Count() - 1);
			Assert.AreEqual("1", mNoOld.Groups["oldNum"].Value);
			Assert.AreEqual("33", mNoOld.Groups["newStart"].Value);
			Assert.AreEqual("1", mNoOld.Groups["newNum"].Value);
		}

		[TestMethod]
		public void TestPatchSplitMultiLines()
		{
			var r = GitDensity.Density.Hunk.HunkSplitRegex;

			var line =
				"some garbage like diff e3f4424\n" +
				"@@ -1,2 +0,40 @@ some more stuff\n" + // stuff behind on this line is swallowed
				"bla asdf\n" +
				"+a new line\n" +
				"@@ -15,25 +55,66 @@asdfasdfasdf\n" +
				"-deleted line\n" +
				"yo"
				;

			var hunks = GitDensity.Density.Hunk.SplitPatch(line).ToList();
			Assert.AreEqual(2, hunks.Count);

			Assert.AreEqual(1u, hunks[0].OldLineStart);
			Assert.AreEqual(2u, hunks[0].OldNumberOfLines);
			Assert.AreEqual(0u, hunks[0].NewLineStart);
			Assert.AreEqual(40u, hunks[0].NewNumberOfLines);
			Assert.AreEqual("bla asdf\n" +
				"+a new line\n", hunks[0].Patch);


			Assert.AreEqual(15u, hunks[1].OldLineStart);
			Assert.AreEqual(25u, hunks[1].OldNumberOfLines);
			Assert.AreEqual(55u, hunks[1].NewLineStart);
			Assert.AreEqual(66u, hunks[1].NewNumberOfLines);
			Assert.AreEqual("-deleted line\n" +
				"yo", hunks[1].Patch);
		}
	}
}
