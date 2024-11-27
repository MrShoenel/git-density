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
using Iesi.Collections.Generic;
using LibGit2Sharp;
using LINQtoCSV;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents an entire file (<see cref="TreeEntryChanges"/>) to be exported.
    /// </summary>
    [JsonObject]
    public class ExportableFile : ExportableCommit, IEnumerable<ExportableHunk>
    {
        [JsonIgnore]
        public ExportableCommit ExportableCommit { get; protected set; }

        [JsonIgnore]
        protected LinkedHashSet<ExportableHunk> expoHunks;

        /// <summary>
        /// Each single <see cref="TreeEntryChanges"/> refers to a single file that was modified.
        /// </summary>
        [JsonIgnore]
        public TreeEntryChanges TreeChange { get; protected set; }

        public ExportableFile(ExportableCommit exportableCommit, TreeEntryChanges treeChange, uint fileIdx) : base(exportableCommit.ExportCommitPair)
        {
            this.ExportableCommit = exportableCommit;
            this.expoHunks = new LinkedHashSet<ExportableHunk>();
            this.TreeChange = treeChange;
            this.FileIdx = fileIdx;
        }

        /// <summary>
        /// This method needs to be called in order to properly inherit aggregation statistics
        /// from parent entities. For example, the file itself does not know how many files a
        /// commit actually had, so it has to inherit (copy) this from the commit. This method
        /// should be overridden in sub-classes and also called from there.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns><see cref="ExportableFile"/> (this instance).</returns>
        /// <exception cref="ArgumentException"></exception>
        public virtual ExportableEntity CopyAggregateStatisticsFrom(ExportableEntity entity)
        {
            var commit = entity as ExportableCommit;
            if (!(commit is ExportableCommit))
            {
                throw new ArgumentException($"Entity must be of type {nameof(ExportableCommit)}.");
            }
            this.CommitNumberOfAddedFiles = commit.CommitNumberOfAddedFiles;
            this.CommitNumberOfDeletedFiles = commit.CommitNumberOfDeletedFiles;
            this.CommitNumberOfRenamedFiles = commit.CommitNumberOfRenamedFiles;
            this.CommitNumberOfModifiedFiles = commit.CommitNumberOfModifiedFiles;
            return this;
        }

        /// <summary>
        /// The original event that triggered the creation/removal of this entity.
        /// For example, creating a new file or renaming an existing one. We
        /// retain this information because entities such as single lines do not
        /// store this information.
        /// </summary>
        [CsvColumn(FieldIndex = 20)]
        [JsonProperty(Order = 20), JsonConverter(typeof(StringEnumConverter))]
        public ChangeKind TreeChangeIntent { get => this.TreeChange.Status; }

        [CsvColumn(FieldIndex = 21)]
        [JsonProperty(Order = 21)]
        public UInt32 FileIdx { get; protected set; }

        /// <summary>
        /// The relative path (in the repository) of the file that was affected.
        /// </summary>
        [CsvColumn(FieldIndex = 22)]
        [JsonProperty(Order = 22)]
        public String FileName { get => this.TreeChange.Path; }

        /// <summary>
        /// The line-count can only be determined if this is a full file (i.e.,
        /// <see cref="FullCode"/> needs to be true). If true, then this file
        /// consists of a single hunk which makes the file's new number of lines
        /// equal to the hunk's new number of lines.
        /// </summary>
        [CsvColumn(FieldIndex = 23)]
        [JsonProperty(Order = 23)]
        public virtual UInt32? FileNewNumberOfLines { get => this.FullCode ? (UInt32?)this.expoHunks.Single().HunkNewNumberOfLines : null; set => throw new InvalidOperationException(); }

        /// <summary>
        /// The line-count can only be determined if this is a full file (i.e.,
        /// <see cref="FullCode"/> needs to be true). If true, then this file
        /// consists of a single hunk which makes the file's old number of lines
        /// equal to the hunk's old number of lines.
        [CsvColumn(FieldIndex = 24)]
        [JsonProperty(Order = 24)]
        public virtual UInt32? FileOldNumberOfLines { get => this.FullCode ? (UInt32?)this.expoHunks.Single().HunkOldNumberOfLines : null; set => throw new InvalidOperationException(); }

        /// <summary>
        /// Returns the number of hunks in this file.
        /// </summary>
        [CsvColumn(FieldIndex = 25)]
        [JsonProperty(Order = 25)]
        public virtual UInt32 FileNumberOfHunks { get => (UInt32)this.expoHunks.Count; set => throw new InvalidOperationException(); }

        #region overrides
        [CsvColumn(FieldIndex = 15)]
        [JsonProperty(Order = 15)]
        public override UInt32 CommitNumberOfAddedFiles { get; set; }

        [CsvColumn(FieldIndex = 16)]
        [JsonProperty(Order = 16)]
        public override UInt32 CommitNumberOfDeletedFiles { get; set; }

        [CsvColumn(FieldIndex = 17)]
        [JsonProperty(Order = 17)]
        public override UInt32 CommitNumberOfRenamedFiles { get; set; }

        [CsvColumn(FieldIndex = 18)]
        [JsonProperty(Order = 18)]
        public override UInt32 CommitNumberOfModifiedFiles { get; set; }
        #endregion

        /// <summary>
        /// Exporting all changes for one file means that we will just concatenate all
        /// <see cref="expoHunks"/> in the file using two newlines.
        /// </summary>
        public override string ContentInteral => String.Join("\n\n", this.expoHunks.Select(eh => eh.ContentInteral));

        /// <summary>
        /// Add a hunk to this file.
        /// </summary>
        /// <param name="hunk"></param>
        /// <returns></returns>
        public ExportableFile AddHunk(ExportableHunk hunk)
        {
            this.expoHunks.Add(hunk);
            return this;
        }

        public IEnumerator<ExportableHunk> GetEnumerator()
        {
            return this.expoHunks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
