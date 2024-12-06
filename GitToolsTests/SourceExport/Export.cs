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
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
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
            return Tuple.Create(new ExportCommitPair(repo, commit, ExportReason.Primary, commit.Parents.First(), new LibGit2Sharp.CompareOptions() { ContextLines = ctxLines }), repo, span);
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

        [TestMethod]
        public void TestGenerations_47309_b1880()
        {
            using (var repo = SolutionDirectory.FullName.OpenRepository())
            using (var span = new GitCommitSpan(repo, sinceDateTimeOrCommitSha: "b1880", untilDatetimeOrCommitSha: "47309"))
            {
                var b1880 = span.Where(comm => comm.ShaShort() == "b1880aa").Single();

                Assert.ThrowsException<Exception>(() =>
                {
                    b1880.ParentGenerations(numGenerations: 1u);
                });
                // Should not throw:
                b1880.ParentGenerations(numGenerations: 1u, allowIncompleteChains: true);

                var _473 = span.Where(comm => comm.ShaShort() == "47309bf").Single();
                Assert.AreEqual(b1880, _473.ParentGenerations(numGenerations: 1u).Single());

                Assert.ThrowsException<Exception>(() =>
                {
                    _473.ParentGenerations(numGenerations: 2u);
                });
                // Should not throw:
                _473.ParentGenerations(numGenerations: 2u, allowIncompleteChains: true);
            }
        }

        [TestMethod]
        public void TestGenerations_Reason()
        {
            using (var repo = SolutionDirectory.FullName.OpenRepository())
            {
                using (var span = new GitCommitSpan(repo, sinceDateTimeOrCommitSha: "85d7", untilDatetimeOrCommitSha: "8a54"))
                {
                    var pairs = ExportCommitPair.ExpandParents(repo: repo, span: span, numGenerations: 2u, compareOptions: null);

                    var asCommits = pairs.SelectMany(pair => pair.AsCommits).ToDictionary(kv => $"{kv.SHA1}_{kv.SHA1_Parent}", kv => kv);
                    Assert.AreEqual(5, asCommits.Count);

                    Assert.IsTrue(asCommits["b1880aa_(initial)"].ExportReason == ExportReason.Parent);
                    Assert.IsTrue(asCommits["47309bf_b1880aa"].ExportReason == ExportReason.Parent);
                    Assert.IsTrue(asCommits["85d75c5_47309bf"].ExportReason == ExportReason.Both);
                    Assert.IsTrue(asCommits["c35070a_85d75c5"].ExportReason == ExportReason.Both);
                    Assert.IsTrue(asCommits["8a54cc5_c35070a"].ExportReason == ExportReason.Primary);
                }


                using (var span = new GitCommitSpan(repository: repo))
                {
                    // all commits:
                    var pairs = ExportCommitPair.ExpandParents(repo: repo, span: span, numGenerations: 0, compareOptions: null);

                    Assert.IsTrue(pairs.SelectMany(pair => pair.AsCommits).All(c => c.ExportReason == ExportReason.Primary));
                }


                // Let's do a 3rd, more complex test.
                using (var span = new GitCommitSpan(repository: repo, sinceDateTimeOrCommitSha: "7d94", untilDatetimeOrCommitSha: "66bb"))
                {
                    var pairs = ExportCommitPair.ExpandParents(repo: repo, span: span, numGenerations: 3, compareOptions: null);

                    // That should be 8 commits.
                    var asCommits = pairs.SelectMany(pair => pair.AsCommits).ToList();
                    Func<string, string, ExportReason, bool> get = (hc, hp, er) => asCommits.Where(c => c.SHA1 == hc && c.SHA1_Parent == hp).Single().ExportReason == er;

                    // While the selection is only 8 commits, 7d94 is a merge commit, so it
                    // will appears twice with different parents (8a67 and e0aa)
                    Assert.AreEqual(9, pairs.Count());

                    var par = ExportReason.Parent;
                    var bot = ExportReason.Both;
                    var pri = ExportReason.Primary;

                    Assert.IsTrue(get("66bbe65", "7d94800", pri));

                    Assert.IsTrue(get("7d94800", "e0aa22f", bot));
                    Assert.IsTrue(get("7d94800", "8a67cf3", bot));

                    Assert.IsTrue(get("e0aa22f", "0ef7dcc", par));
                    Assert.IsTrue(get("8a67cf3", "0b52fa8", par));
                    Assert.IsTrue(get("0b52fa8", "f8ce1e1", par));
                    Assert.IsTrue(get("f8ce1e1", "47fbf35", par));
                    Assert.IsTrue(get("0ef7dcc", "17d9155", par));
                    Assert.IsTrue(get("17d9155", "227f070", par));
                }
            }
        }


        [TestMethod]
        public void TestBlockOffsets_0b4cf4()
        {
            var tp = GetPair("0b4cf4");
            var exp = tp.Item1;
            var blocks = exp.AsBlocks.Where(block => block.FileName == "GitTools/SourceExport/ExportableLine.cs").OrderBy(b => b.HunkIdx).ThenBy(b => b.BlockIdx).ToList();

            Assert.IsTrue(blocks.All(b => b.FileIdx == blocks[0].FileIdx));
            Assert.AreEqual(3 + 5 + 7 + 3, blocks.Count);

            tp.Item2.Dispose();
            tp.Item3.Dispose();

            TextBlockNature a = TextBlockNature.Added, c = TextBlockNature.Context, d = TextBlockNature.Deleted, r = TextBlockNature.Replaced;
            var b1 = blocks[0];
            Assert.IsTrue(b1.BlockNature == c && b1.BlockOldLineStart == 19u);
            var b2 = blocks[1];
            // The 2nd block doesn't actually have an old start because it's new.
            // However, by logic, it can only start after 21; so, 22 it is.
            Assert.IsTrue(b2.BlockNature == a && b2.BlockOldLineStart == 22u);
            var b3 = blocks[2];
            // Also 22, because the previous block (added) has an empty set of
            // line numbers before.
            Assert.IsTrue(b3.BlockNature == c && b3.BlockOldLineStart == 22u);


            var b6 = blocks[5]; // 3rd in hunk2
            Assert.IsTrue(b6.BlockNature == c && b6.BlockOldLineStart == 30u && b6.HunkIdx == 1u && b6.BlockIdx == 2);

            var b10 = blocks[9]; // 2nd in hunk3
            Assert.IsTrue(b10.BlockNature == r && b10.BlockOldLineStart == 48u && b10.HunkIdx == 2u && b10.BlockIdx == 1u);
            var b11 = blocks[10];
            Assert.IsTrue(b11.BlockNature == c && b11.BlockOldLineStart == 49u && b11.HunkIdx == 2u && b11.BlockIdx == 2u);
            var b12 = blocks[11];
            Assert.IsTrue(b12.BlockNature == d && b12.BlockOldLineStart == 50u && b12.HunkIdx == 2u && b12.BlockIdx == 3u);
            // This is a purely deleted block, so there is no "new line start". However, in the
            // context of the other new/untouched lines, there were 55 of these before. So, this
            // here block should have a new "start" of 56, even if it's empty in terms of new lines.
            // The next block 13, which is context, also starts at 56.
            Assert.IsTrue(b12.BlockNewLineStart == 56u);

            var b13 = blocks[12];
            Assert.IsTrue(b13.BlockNature == c && b13.BlockOldLineStart == 64u && b13.HunkIdx == 2u && b13.BlockIdx == 4u && b13.BlockNewLineStart == 56u);
            var b14 = blocks[13];
            Assert.IsTrue(b14.BlockNature == r && b14.BlockOldLineStart == 66u && b14.HunkIdx == 2u && b14.BlockIdx == 5u && b14.BlockNewLineStart == 58u);
            var b15 = blocks[14];
            Assert.IsTrue(b15.BlockNature == c && b15.BlockOldLineStart == 68u && b15.HunkIdx == 2u && b15.BlockIdx == 6u && b15.BlockNewLineStart == 59u);
        }
    }
}
