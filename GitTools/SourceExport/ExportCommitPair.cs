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
using GitDensity.Density;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Util.Density;
using CompareOptions = LibGit2Sharp.CompareOptions;


namespace GitTools.SourceExport
{
    /// <summary>
    /// An exportable commit, in the sense of its source code that can be
    /// exported alongside some of its basic properties.
    /// </summary>
    public class ExportCommitPair : CommitPair, IEnumerable<ExportableCommit>, IEnumerable<ExportableFile>, IEnumerable<ExportableHunk>, IEnumerable<ExportableBlock>, IEnumerable<ExportableLine>
    {
        private Lazy<IList<ExportableCommit>> lazyCommits;
        private Lazy<IList<ExportableFile>> lazyFiles;
        private Lazy<IList<ExportableHunk>> lazyHunks;
        private Lazy<IList<ExportableBlock>> lazyBlocks;
        private Lazy<IList<ExportableLine>> lazyLines;

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
        public ExportCommitPair(Repository repo, Commit child, Commit parent = null, CompareOptions compareOptions = null) : base(repo, child, parent, compareOptions)
        {
            this.CompareOptions = compareOptions ?? new CompareOptions();

            this.lazyCommits = new Lazy<IList<ExportableCommit>>(mode: LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Commits().ToList());
            this.lazyFiles = new Lazy<IList<ExportableFile>>(mode: LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Files().ToList());
            this.lazyHunks = new Lazy<IList<ExportableHunk>>(mode: LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Hunks().ToList());
            this.lazyBlocks = new Lazy<IList<ExportableBlock>>(mode: LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Blocks().ToList());
            this.lazyLines = new Lazy<IList<ExportableLine>>(mode: LazyThreadSafetyMode.ExecutionAndPublication, valueFactory: () => this.Lines().ToList());
        }

        /// <summary>
        /// Overridden as to return relevant changes that are added, copied, deleted, or renamed.
        /// </summary>
        public override IReadOnlyList<TreeEntryChanges> RelevantTreeChanges => base.RelevantTreeChanges.Where(rtc => rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Copied || rtc.Status == ChangeKind.Deleted || rtc.Status == ChangeKind.Renamed || rtc.Status == ChangeKind.Modified).OrderBy(rtc => rtc.Path).ToList().AsReadOnly();


        /// <summary>
        /// Note that one pair can only generate a single commit. This method is called
        /// <see cref="Commits"/> and returns an enumerable for reasons of consistency.
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<ExportableCommit> Commits()
        {
            var commit = new ExportableCommit(this);

            uint fileIdx = 0;
            foreach (var rtc in this.RelevantTreeChanges)
            {
                var file = new ExportableFile(commit, rtc, fileIdx);
                var oldPatch = this.Patch[rtc.OldPath];
                var newPatch = this.Patch[rtc.Path];

                // Either one is null or they are both the same. For the purpose of exporting
                // source code for either case, it does not matter. For example, when a file
                // is renamed, we get both changes here: One change is the patch for the removal
                // and one other (separate) change is for the addition (yes, we want both).
                Debug.Assert(((oldPatch is null) ^ (newPatch is null)) || EqualityComparer<PatchEntryChanges>.Default.Equals(oldPatch, newPatch));
                
                var patch = newPatch ?? oldPatch;
                uint hunkIdx = 0;
                foreach (var hunk in Hunk.HunksForPatch(patch))
                {
                    var expoHunk = new ExportableHunk(file: file, hunk: hunk, hunkIdx: hunkIdx++);
                    file.AddHunk(expoHunk);
                }

                commit.AddFile(file);
                fileIdx++;
            }

            yield return commit;
        }


        protected IEnumerable<ExportableFile> Files()
        {
            foreach (var commit in this.lazyCommits.Value)
            {
                foreach (var file in commit)
                {
                    yield return file;
                }
            }
        }

        protected IEnumerable<ExportableHunk> Hunks()
        {
            foreach (var file in this.lazyFiles.Value)
            {
                foreach (var expoHunk in file)
                {
                    yield return expoHunk;
                }
            }
        }

        protected IEnumerable<ExportableBlock> Blocks()
        {
            foreach (var hunk in this.lazyHunks.Value)
            {
                uint blockIdx = 0;
                foreach (var block in hunk)
                {
                    yield return new ExportableBlock(exportableHunk: hunk, textBlock: block, blockIdx: blockIdx++);
                }
            }
        }

        protected IEnumerable<ExportableLine> Lines()
        {
            foreach (var block in this.lazyBlocks.Value)
            {
                foreach (var line in block)
                {
                    yield return new ExportableLine(block, line);

                }

            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new AmbiguousSpecificationException($"You must cast an instance of {nameof(ExportCommitPair)} to an explicit type for it to be enumerable, such as {nameof(IEnumerable<ExportableHunk>)}.");
        }

        IEnumerator<ExportableCommit> IEnumerable<ExportableCommit>.GetEnumerator()
        {
            return this.lazyCommits.Value.AsEnumerable().GetEnumerator();
        }

        IEnumerator<ExportableFile> IEnumerable<ExportableFile>.GetEnumerator()
        {
            return this.lazyFiles.Value.GetEnumerator();
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

        public IEnumerable<ExportableCommit> AsCommits { get => this as IEnumerable<ExportableCommit>; }

        public IEnumerable<ExportableFile> AsFiles { get => this as IEnumerable<ExportableFile>; }

        public IEnumerable<ExportableHunk> AsHunks { get => this as IEnumerable<ExportableHunk>; }

        public IEnumerable<ExportableBlock> AsBlocks { get =>  this as IEnumerable<ExportableBlock>; }

        public IEnumerable<ExportableLine> AsLines { get => this as IEnumerable<ExportableLine>; }
    }
}
