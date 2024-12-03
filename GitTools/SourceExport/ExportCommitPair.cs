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
using Util;
using Util.Density;
using Util.Extensions;
using CompareOptions = LibGit2Sharp.CompareOptions;


namespace GitTools.SourceExport
{
    /// <summary>
    /// An enumeration of reasons as to why an <see cref="ExportableEntity"/> was exported,
    /// with regard to whether it was primarily selected by the <see cref="GitCommitSpan"/>
    /// or whether it was included because it is a parent generation. When exporting sequences
    /// of commits with number of parent generations greater than zero, it is quite common
    /// that a commit is exported because it is both a primary and a parent commit.
    /// </summary>
    public enum ExportReason
    {
        /// <summary>
        /// An entity was directly selected by the <see cref="GitCommitSpan.FilteredCommits"/>.
        /// </summary>
        Primary,

        /// <summary>
        /// An entity was selected as an ancestor of a primary entity and is only exported
        /// because of that reason.
        /// </summary>
        Parent,

        /// <summary>
        /// An entity that was primarily selected by the <see cref="GitCommitSpan.FilteredCommits"/>,
        /// but is also a parent (ancestor) to another entity. For example, when selecting two
        /// consecutive commits as primary commits and expanding parents
        /// (<see cref="ExportCommitPair.ExpandParents(Repository, GitCommitSpan, uint)"/>) with one
        /// generation, then the older of these two commits is both a primary- and a parent commit.
        /// </summary>
        Both
    }

    /// <summary>
    /// An exportable commit, in the sense of its source code that can be
    /// exported alongside some of its basic properties.
    /// </summary>
    public class ExportCommitPair : CommitPair, IEnumerable<ExportableCommit>, IEnumerable<ExportableFile>, IEnumerable<ExportableHunk>, IEnumerable<ExportableBlock>, IEnumerable<ExportableLine>
    {
        private Lazy<Patch> lazyPatchFull;

        /// <summary>
        /// Returns the full-source patch, where context-lines was set to the allowed
        /// maximum. The result of this is a single hunk per file.
        /// </summary>
        public Patch PatchFull { get => this.lazyPatchFull.Value; }

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
        /// Returns true iff the number of context lines in <see cref="CompareOptions"/> has been
        /// set to the maximum allowed value of <see cref="Int32.MaxValue"/>.
        /// </summary>
        public Boolean ExportFullCode {  get => this.CompareOptions.ContextLines == Int32.MaxValue; }

        /// <summary>
        /// Whether or not the child of this pair marks the beginning of a chain.
        /// </summary>
        public ExportReason ExportReason { get; protected set; }

        /// <summary>
        /// An exportable commit always is a pair, because it needs to be compared relative to
        /// some parent commit.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="child"></param>
        /// <param name="parent">The parent commit. If not given, this commit is compared to
        /// the beginning of the repository.</param>
        /// <param name="compareOptions"></param>
        public ExportCommitPair(Repository repo, Commit child, ExportReason includeReason, Commit parent = null, CompareOptions compareOptions = null) : base(repo, child, parent, compareOptions)
        {
            this.ExportReason = includeReason;
            this.CompareOptions = compareOptions ?? new CompareOptions();

            // Creates an extra patch that always holds the full source code.
            // We use this to determine the number of affected lines old/new,
            // so that we can do positional encoding of entities.
            this.lazyPatchFull = new Lazy<Patch>(() =>
            {
                return this.Repository.Diff.Compare<Patch>(oldTree: this.Parent?.Tree, newTree: this.Child.Tree, compareOptions: new CompareOptions() { ContextLines = Int32.MaxValue });
            });

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
                // Let's also set the number of lines old/new:
                var oldPatch_Full = this.PatchFull[rtc.OldPath];
                var newPatch_Full = this.PatchFull[rtc.Path];
                Debug.Assert(((oldPatch_Full is null) ^ (newPatch_Full is null)) || EqualityComparer<PatchEntryChanges>.Default.Equals(oldPatch_Full, newPatch_Full));
                var patch_full = newPatch_Full ?? oldPatch_Full;
                var hunk_full = Hunk.HunksForPatch(patch_full).Single();
                var fileOldNumberOfLines = hunk_full.OldNumberOfLines;
                var fileNewNumberOfLines = hunk_full.NewNumberOfLines;

                var oldPatch = this.Patch[rtc.OldPath];
                var newPatch = this.Patch[rtc.Path];

                // Either one is null or they are both the same. For the purpose of exporting
                // source code for either case, it does not matter. For example, when a file
                // is renamed, we get both changes here: One change is the patch for the removal
                // and one other (separate) change is for the addition (yes, we want both).
                Debug.Assert(((oldPatch is null) ^ (newPatch is null)) || EqualityComparer<PatchEntryChanges>.Default.Equals(oldPatch, newPatch));
                
                var patch = newPatch ?? oldPatch;
                uint hunkIdx = 0;
                var hunks = Hunk.HunksForPatch(patch).ToList();

                var file = new ExportableFile(commit, rtc, fileIdx: fileIdx, fileNewNumberOfLines: fileNewNumberOfLines, fileOldNumberOfLines: fileOldNumberOfLines, fileNumberOfHunks: (uint)hunks.Count);

                foreach (var hunk in hunks)
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
                foreach (var hunk in file)
                {
                    yield return hunk;
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
            return this.lazyCommits.Value.GetEnumerator();
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

        public IEnumerable<ExportableCommit> AsCommits { get => this; }

        public IEnumerable<ExportableFile> AsFiles { get => this; }

        public IEnumerable<ExportableHunk> AsHunks { get => this; }

        public IEnumerable<ExportableBlock> AsBlocks { get =>  this; }

        public IEnumerable<ExportableLine> AsLines { get => this; }


        /// <summary>
        /// Takes all of a <see cref="GitCommitSpan"/>'s filtered commits, which become the 'primary' commits.
        /// For each, gets N parent generations. Then, recursively, returns all <see cref="ExportCommitPair"/>
        /// entities between each commit and its parent. Also make sure to check out <see cref="ExportReason"/>.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="span"></param>
        /// <param name="compareOptions"></param>
        /// <param name="numGenerations"></param>
        /// <param name="allowIncompleteChains">If true, allow to construct incomplete chains, where not all
        /// parent generations are present.</param>
        /// <returns></returns>
        public static IEnumerable<ExportCommitPair> ExpandParents(Repository repo, GitCommitSpan span, CompareOptions compareOptions, UInt32 numGenerations, bool allowIncompleteChains = false)
        {
            var allCommits = span.FilteredCommits.ToHashSet();
            var primaryCommits = allCommits.ToHashSet(); // make a copy

            var allParents = primaryCommits.SelectMany(commit => commit.ParentGenerations(numGenerations: numGenerations, allowIncompleteChains: allowIncompleteChains)).ToHashSet();
            var both = primaryCommits.Intersect(allParents).ToHashSet();

            // Update all commits
            allCommits = primaryCommits.Concat(allParents).ToHashSet();

            return allCommits.SelectMany(commit =>
            {
                var parents = commit.Parents.ToList();
                if (parents.Count == 0)
                {
                    parents.Add(null); // Required to collect pairs with initial repo state (no parent)!
                }

                return parents.Select(parent =>
                {
                    var reason = both.Contains(commit) ? ExportReason.Both : (primaryCommits.Contains(commit) ? ExportReason.Primary : ExportReason.Parent);
                    return new ExportCommitPair(repo: repo, includeReason: reason, child: commit, parent: parent, compareOptions: compareOptions);
                });
            });
        }
    }
}
