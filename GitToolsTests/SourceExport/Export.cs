/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using GitDensity.Similarity;
using GitTools.SourceExport;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Util;
using Util.Extensions;
using Line = GitDensity.Similarity.Line;

namespace GitToolsTests.SourceExport
{
    [TestClass]
    public class Export
    {
        public static DirectoryInfo SolutionDirectory
            => new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Locati‌​on)).Parent.Parent;

        public static Tuple<ExportCommitPair, Repository, GitCommitSpan> GetPair(string sha1, int ctxLines = 3)
        {
            var repo = SolutionDirectory.FullName.OpenRepository();
            var span = new GitCommitSpan(repo, sinceDateTimeOrCommitSha: sha1, untilDatetimeOrCommitSha: sha1);

            var commit = span.FilteredCommits.Single();
            return Tuple.Create(new ExportCommitPair(repo, commit, commit.Parents.First(), new LibGit2Sharp.CompareOptions() { ContextLines = ctxLines }), repo, span);
        }

        /// <summary>
        /// We will open a specific commit and check whether the lines are exported correctly.
        /// </summary>
        [TestMethod]
        public void TestExportLines_8f05()
        {
            var tp = GetPair("8f05");
            var exp = tp.Item1;
            // This commit has only one file, but it has 4 hunks.
            var lines = exp.AsLines.OrderBy(l => l.HunkIdx).ThenBy(l => l.BlockIdx).ThenBy(l => l.LineNumber).ToList();
            foreach (var line in lines)
            {
                // The following should be the same for all lines. But remember,
                // properties inherited from hunks will differ because not all
                // line belong to the same hunk.
                Assert.AreEqual(line.CommitNumberOfModifiedFiles, 1u);
                Assert.AreEqual(line.CommitNumberOfAddedFiles, 0u);
                Assert.AreEqual(line.CommitNumberOfDeletedFiles, 0u);
                Assert.AreEqual(line.CommitNumberOfRenamedFiles, 0u);
                Assert.AreEqual(line.FileNumberOfHunks, 4u);
            }

            Assert.AreEqual(4, lines.GroupBy(l => l.HunkIdx).Count());

            // 7 lines, 3 blocks
            var hunk1 = lines.Where(l => l.HunkIdx == 0).ToList();
            Assert.AreEqual(7, hunk1.Count());
            Assert.AreEqual(3, hunk1.GroupBy(l => l.BlockIdx).Count());
            Assert.IsTrue(hunk1.All(l => l.HunkNumberOfBlocks == 3u));
            Assert.IsTrue(hunk1.Take(3).All(l => l.LineType == LineType.Untouched));
            Assert.IsTrue(hunk1[3].LineType == LineType.Added);
            Assert.IsTrue(hunk1.Skip(4).All(l => l.LineType == LineType.Untouched));

            var hunk3 = lines.Where(l => l.HunkIdx == 2).ToList();
            Assert.AreEqual(6, hunk3.Where(l => l.LineType == LineType.Untouched).Count());
            Assert.AreEqual(1, hunk3.Where(l => l.LineType == LineType.Deleted).Count());
            Assert.AreEqual(9, hunk3.Where(l => l.LineType == LineType.Added).Count());
            Assert.IsTrue(hunk3.All(l => l.HunkNumberOfBlocks == 3u));


            var hunk4 = lines.Where(l => l.HunkIdx == 3).ToList();
            Assert.IsTrue(hunk4.All(l => l.HunkNumberOfBlocks == 13u));

            tp.Item2.Dispose();
            tp.Item3.Dispose();
        }

        [TestMethod]
        public void TestExportBlocks_8f05()
        {
            var tp = GetPair("8f05");
            var exp = tp.Item1;
            var blocks = exp.AsBlocks.OrderBy(b => b.HunkIdx).ThenBy(b => b.BlockIdx).ToList();

            CollectionAssert.AreEqual(blocks.GroupBy(b => b.HunkIdx).Select(grp => grp.Count()).ToList(), new List<int> { 3, 3, 3, 13 });


            // hunk #4 has 13 blocks.
            var hunk4 = blocks.Where(b => b.HunkIdx == 3).ToList();
            Assert.AreEqual((Int32)hunk4[0].HunkNumberOfBlocks, hunk4.Count);
            foreach (var idx in new List<int> { 0, 2, 4, 6, 8, 10, 12 })
            {
                Assert.IsTrue(hunk4[idx].BlockNature == TextBlockNature.Context);
            }
            foreach (var idx in new List<int> { 1, 3, 5, 7, 9, 11 })
            {
                Assert.IsTrue(hunk4[idx].BlockNature == TextBlockNature.Replaced);
            }

            // Let's test the second block:
            var block2 = hunk4[1];
            Assert.AreEqual("291,292,293", block2.BlockLineNumbersDeleted);
            Assert.AreEqual("301,302,303,304,305,306,307", block2.BlockLineNumbersAdded);
            Assert.AreEqual("var allResults = resultsBag.SelectMany(x => x).ToList();", (block2 as IEnumerable<Line>).First().String.Substring(1).Trim());

            tp.Item2.Dispose();
            tp.Item3.Dispose();
        }

        [TestMethod]
        public void TestExportHunks_8f05()
        {
            var tp = GetPair("8f05");
            var exp = tp.Item1;
            var hunks = exp.AsHunks.OrderBy(h => h.HunkIdx).ToList();

            Assert.AreEqual(4, hunks.Count);
            Assert.AreEqual(4u, hunks[0].FileNumberOfHunks);

            var hunk3 = hunks[2];
            Assert.AreEqual("255", hunk3.HunkLineNumbersDeleted);
            Assert.AreEqual("257,258,259,260,261,262,263,264,265", hunk3.HunkLineNumbersAdded);

            Assert.AreEqual(hunk3.HunkOldLineStart, 252u);
            Assert.AreEqual(hunk3.HunkOldNumberOfLines, 7u); // 6 context + 1 deleted
            Assert.AreEqual(hunk3.HunkNewLineStart, 254u);
            Assert.AreEqual(hunk3.HunkNewNumberOfLines, 15u); // 6 context + 9 added


            // Let's also test Hunk-collapsing:
            tp.Item2.Dispose();
            tp.Item3.Dispose();
            tp = GetPair("8f05", ctxLines: Int32.MaxValue);
            exp = tp.Item1;

            Assert.AreEqual(1, exp.AsHunks.Count());

            tp.Item2.Dispose();
            tp.Item3.Dispose();
        }

        [TestMethod]
        public void TestExportFiles_8f05()
        {
            var tp = GetPair("8f05");
            var exp = tp.Item1;
            var files = exp.AsFiles.ToList();

            Assert.AreEqual(1, files.Count);

            var file = files[0];

            Assert.AreEqual(0u, file.FileIdx);
            Assert.AreEqual(ChangeKind.Modified, file.TreeChangeIntent);
            Assert.AreEqual("GitTools/Program.cs", file.FileName);

            tp.Item2.Dispose();
            tp.Item3.Dispose();
        }

        [TestMethod]
        public void TestExportCommits_8f05()
        {
            var tp = GetPair("8f05");
            var exp = tp.Item1;

            var commits = exp.AsCommits.ToList();
            Assert.AreEqual(1, commits.Count);
            var commit = commits[0];

            Assert.AreEqual("8f05cad", commit.ExportCommitPair.Child.ShaShort(7).ToLower());
            Assert.IsFalse(commit.IsInitialCommit);
            Assert.IsFalse(commit.IsMergeCommit);

            tp.Item2.Dispose();
            tp.Item3.Dispose();
        }

        [TestMethod]
        public void TestGenerations_9b70()
        {
            var tp = GetPair("9b70");
            var commit = tp.Item1.Child;

            Assert.IsTrue(commit.ParentGenerations(numGenerations: 0).Count() == 0);

            // In its first generation, it has two parents: ef1db10 and 8f05cad
            var s = commit.ParentGenerations(numGenerations: 1);
            Assert.AreEqual(2, s.Count);
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "ef1db10").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "8f05cad").Count());

            // In its second generation, ef1db has one parent and 8f05 has one parent
            s = commit.ParentGenerations(numGenerations: 2);
            Assert.AreEqual(4, s.Count);
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "ef1db10").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "8f05cad").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "7710093").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "90ae132").Count());

            // In its third generation, 7710093 has one parent and 90ae has two parents!
            // However, one of 90ae's parents is 8f05cad again, so there should be 6 commits in the set!
            s = commit.ParentGenerations(numGenerations: 3);
            Assert.AreEqual(6, s.Count);
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "ef1db10").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "8f05cad").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "7710093").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "90ae132").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "9992e0a").Count());
            Assert.AreEqual(1, s.Where(c => c.ShaShort() == "4b938a6").Count());
        }
    }
}
