/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
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
using GitDensity.Density;
using GitDensity.Similarity;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Util.Density;
using Util.Extensions;

namespace GitDensityTests
{
	[TestClass]
	public class HunkTests
	{
		public static DirectoryInfo SolutionDirectory
			=> new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Locati‌​on)).Parent.Parent;


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

		[TestMethod]
		public void TestHunkGetLines()
		{
			using (var span = new Util.GitCommitSpan(SolutionDirectory.FullName.OpenRepository()))
			{
				// Has additions and deletions, also within comments
				var comm1 = span.Where(c => c.Sha.StartsWith("6f18a54", StringComparison.OrdinalIgnoreCase)).First();
				//// Has mostly additions but also (single-line) comments we wanna remove
				//var comm2 = span.Where(c => c.Sha.StartsWith("f7574a4", StringComparison.OrdinalIgnoreCase)).First();

				var pair = CommitPair.FromChild(comm1, span.Repository);
				// Each change corresponds to a modified file:
				var changes = pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Modified);

				// Let's look at 'IMetricsAnalyzer':
				var patch = pair.Patch[changes.Where(c => c.Path.EndsWith("IMetricsAnalyzer.cs")).First().Path];

				TextBlock.CountLinesInPatch(
					patch, out uint add, out uint del, out uint addNoC, out uint delNoC);

				Assert.AreEqual(7u, add);
				Assert.AreEqual(3u, del);
				Assert.AreEqual(2u, addNoC);
				Assert.AreEqual(1u, delNoC);


				// Now assert that this commit does not have any pure adds/renames:
				changes = pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Renamed);
				TextBlock.CountLinesInAllPatches(
					changes.Select(c => pair.Patch[c.Path]), out add, out del, out addNoC, out delNoC);

				Assert.AreEqual(0u, add);
				Assert.AreEqual(0u, del);
				Assert.AreEqual(0u, addNoC);
				Assert.AreEqual(0u, delNoC);

				// .. also pure deletes:
				changes = pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Deleted);
				Assert.IsTrue(!changes.Any());
				TextBlock.CountLinesInAllPatches(
					changes.Select(c => pair.Patch[c.OldPath]), out add, out del, out addNoC, out delNoC);

				Assert.AreEqual(0u, add);
				Assert.AreEqual(0u, del);
				Assert.AreEqual(0u, addNoC);
				Assert.AreEqual(0u, delNoC);


				//////////////////////////// Let's check a deleted file:
				comm1 = span.Where(c => c.Sha.StartsWith("03e2779", StringComparison.OrdinalIgnoreCase)).First();
				pair = CommitPair.FromChild(comm1, span.Repository);
				changes = pair.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Deleted);

				Assert.AreEqual(3, changes.Count());

				// Per file, only 2 lines are not comments!
				TextBlock.CountLinesInAllPatches(
					changes.Select(c => pair.Patch[c.Path]), out add, out del, out addNoC, out delNoC);

				Assert.AreEqual(0u, add);
				Assert.IsTrue(del >= 90u); // didn't count it, but roughly 3x30
				Assert.AreEqual(0u, addNoC);
				Assert.AreEqual(6u, delNoC);
			}
		}
	}
}
