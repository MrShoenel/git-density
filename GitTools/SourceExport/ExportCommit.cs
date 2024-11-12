using GitDensity.Density;
using GitDensity.Similarity;
using GitTools.Prompting;
using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Util.Density;
using Util.Extensions;
using CompareOptions = LibGit2Sharp.CompareOptions;
using Line = GitDensity.Similarity.Line;

namespace GitTools.SourceExport
{
    /// <summary>
    /// An exportable commit, in the sense of its source code that can be
    /// exported alongside some of its basic properties.
    /// </summary>
    public class ExportCommit : CommitPair, IEnumerable<ExportableLine>, IEnumerable<ExportableHunk>, IEnumerable<ExportableBlock>
    {
        private Lazy<IList<ExportableLine>> lazyLines;
        private Lazy<IList<ExportableHunk>> lazyHunks;
        private Lazy<IList<ExportableBlock>> lazyBlocks;

        /// <summary>
        /// Options that are used during diff'ing.
        /// </summary>
        public CompareOptions CompareOptions { get; protected set; }

        /// <summary>
        /// An exportable commit always is a pair, because it needs to be compared relative to
        /// some parent commit.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="child"></param>
        /// <param name="parent">The parent commit. If not given, this commit is compared to
        /// the beginning of the repository.</param>
        /// <param name="compareOptions"></param>
        public ExportCommit(Repository repo, Commit child, Commit parent = null, CompareOptions compareOptions = null) : base(repo, child, parent)
        {
            this.CompareOptions = compareOptions ?? new CompareOptions();
            this.lazyHunks = new Lazy<IList<ExportableHunk>>(mode: System.Threading.LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Hunks().ToList());
            this.lazyBlocks = new Lazy<IList<ExportableBlock>>(mode: System.Threading.LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Blocks().ToList());
            this.lazyLines = new Lazy<IList<ExportableLine>>(mode: System.Threading.LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Lines().ToList());
        }

        /// <summary>
        /// Overridden as to return relevant changes that are added, copied, deleted, or renamed.
        /// </summary>
        public override IReadOnlyList<TreeEntryChanges> RelevantTreeChanges => base.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Copied || rtc.Status == ChangeKind.Deleted || rtc.Status == ChangeKind.Renamed || rtc.Status == ChangeKind.Modified).ToList().AsReadOnly();

        protected IEnumerable<ExportableHunk> Hunks()
        {
            foreach (var rtc in this.RelevantTreeChanges)
            {
                var added = rtc.Status == ChangeKind.Added;
                var patch = this.Patch[added ? rtc.Path : rtc.OldPath];

                uint hunkIdx = 0;
                foreach (var hunk in Hunk.HunksForPatch(patch))
                {
                    yield return new ExportableHunk(exportCommit: this, treeChanges: rtc, hunk: hunk, hunkIdx: hunkIdx++);
                }
            }
        }

        protected IEnumerable<ExportableBlock> Blocks()
        {
            foreach (var hunk in this.lazyHunks.Value)
            {
                uint blockIdx = 0;
                foreach (var block in hunk.Blocks())
                {
                    yield return new ExportableBlock(exportableHunk: hunk, textBlock: block, blockIdx: blockIdx++);

                }
            }
        }

        protected IEnumerable<ExportableLine> Lines()
        {
            foreach (var block in this.lazyBlocks.Value)
            {

            }

            //foreach (var rtc in this.RelevantTreeChanges)
            //{
            //    var added = rtc.Status == ChangeKind.Added;
            //    var patch = this.Patch[added ? rtc.Path : rtc.OldPath];

            //    uint hunkIdx = 0;
            //    foreach (var hunk in Hunk.HunksForPatch(patch))
            //    {
            //        var tb = new FullTextBlock(hunk);

            //        foreach (var line in tb)
            //        {
            //            yield return new ExportableLine(exportCommit: this, treeChanges: rtc, line: line, hunkIdx: hunkIdx);
            //        }

            //        hunkIdx++;
            //    }
            //}
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new AmbiguousSpecificationException($"You must cast an instance of {nameof(ExportCommit)} to an explicit type for it to be enumerable, such as {nameof(IEnumerable<ExportableHunk>)}.");
        }

        IEnumerator<ExportableHunk> IEnumerable<ExportableHunk>.GetEnumerator()
        {
            return this.lazyHunks.Value.GetEnumerator();
        }

        public IEnumerator<ExportableBlock> GetEnumerator()
        {
            return this.lazyBlocks.Value.GetEnumerator();
        }

        IEnumerator<ExportableLine> IEnumerable<ExportableLine>.GetEnumerator()
        {
            return this.lazyLines.Value.GetEnumerator();
        }
    }

    


    


    



    public class ExportableLine : ExportableEntity
    {
        protected internal Line line;

        public ExportableLine(ExportCommit exportCommit, TreeEntryChanges treeChanges, Line line, uint hunkIdx) : base(exportCommit, treeChanges)
        {
            this.line = line;
            this.HunkIdx = hunkIdx;
        }

        [CsvColumn(FieldIndex = 5)]
        public UInt32 HunkIdx { get; protected internal set; }


        [CsvColumn(FieldIndex = 6)]
        public LineType LineType { get => this.line.Type; }

        [CsvColumn(FieldIndex = 7)]
        public UInt32 LineNumber { get => this.line.Number; }

        [CsvColumn(FieldIndex = 8)]
        public override String Content { get => this.line.String + $"asd ,;\"; foo"; }


    }
}
