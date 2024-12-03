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
using Util.Extensions;
using JsonConverter = Newtonsoft.Json.JsonConverter;
using JsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents an entire commit to be exported.
    /// </summary>
    [JsonObject]
    public class ExportableCommit : ExportableEntity, IEnumerable<ExportableFile>
    {
        [JsonIgnore]
        protected LinkedHashSet<ExportableFile> expoFiles;

        /// <summary>
        /// Can be set to true if full files are exported (i.e., maximum number of content lines
        /// which lead to Hunk-collapse and the inclusion of all lines).
        /// </summary>
        [JsonIgnore]
        public Boolean FullCode { get => this.ExportCommitPair.ExportFullCode; }

        public ExportableCommit(ExportCommitPair exportCommit) : base(exportCommit)
        {
            this.expoFiles = new LinkedHashSet<ExportableFile>();
        }

        /// <summary>
        /// The hash of the child commit.
        /// </summary>
        [CsvColumn(FieldIndex = 1)]
        [JsonProperty(Order = 1)]
        public String SHA1 { get => this.ExportCommitPair.Child.ShaShort(); }

        /// <summary>
        /// The hash of the parent commit.
        /// </summary>
        [CsvColumn(FieldIndex = 2)]
        [JsonProperty(Order = 2)]
        public String SHA1_Parent { get => this.ExportCommitPair.Parent?.ShaShort() ?? "(initial)"; }

        /// <summary>
        /// Whether the child of this pair marks the beginning of a potential chain.
        /// </summary>
        [CsvColumn(FieldIndex = 3)]
        [JsonProperty(Order = 3), JsonConverter(typeof(StringEnumConverter))]
        public ExportReason ExportReason { get => this.ExportCommitPair.ExportReason; }

        [CsvColumn(FieldIndex = 4)]
        [JsonProperty(Order = 4)]
        public String Message { get => this.ExportCommitPair.Child.Message.Trim(); }

        [CsvColumn(FieldIndex = 5)]
        [JsonProperty(Order = 5)]
        public String AuthorName { get => this.ExportCommitPair.Child.Author.Name; }

        [CsvColumn(FieldIndex = 6)]
        [JsonProperty(Order = 6)]
        public String AuthorEmail { get => this.ExportCommitPair.Child.Author.Email; }

        [CsvColumn(FieldIndex = 7)]
        [JsonProperty(Order = 7)]
        public String AuthorTime { get => this.SerializeDateTime(this.AuthorTime_DT); }

        [JsonIgnore]
        public DateTime AuthorTime_DT { get => this.ExportCommitPair.Child.Author.When.UtcDateTime; }

        [CsvColumn(FieldIndex = 8)]
        [JsonProperty(Order = 8)]
        public String CommitterName { get => this.ExportCommitPair.Child.Committer.Name; }

        [CsvColumn(FieldIndex = 9)]
        [JsonProperty(Order = 9)]
        public String CommitterEmail { get => this.ExportCommitPair.Child.Committer.Email; }

        [CsvColumn(FieldIndex = 10)]
        [JsonProperty(Order = 10)]

        public String CommitterTime { get => this.SerializeDateTime(this.CommitterTime_DT); }

        [JsonIgnore]
        public DateTime CommitterTime_DT { get => this.ExportCommitPair.Child.Committer.When.UtcDateTime; }

        [CsvColumn(FieldIndex = 11)]
        [JsonProperty(Order = 11)]
        public Boolean IsInitialCommit { get => this.ExportCommitPair.Child.Parents.Count() == 0; }

        [CsvColumn(FieldIndex = 12)]
        [JsonProperty(Order = 12)]
        public Boolean IsMergeCommit { get => this.ExportCommitPair.Child.Parents.Count() > 1; }

        [CsvColumn(FieldIndex = 13)]
        [JsonProperty(Order = 13)]
        public UInt32 NumberOfParentCommits { get => (uint)this.ExportCommitPair.Child.Parents.Count(); }

        /// <summary>
        /// A map between child and parent commit time as the <see cref="TimeSpan"/> between
        /// both. The key is the parent's SHA1 and the value is in fractional minutes.
        /// </summary>
        [CsvColumn(FieldIndex = 14)]
        [JsonProperty(Order = 14)]
        public Double? DaysSinceParentCommit { get => this.ExportCommitPair.Parent is Commit ? (this.ExportCommitPair.Child.Committer.When.UtcDateTime - this.ExportCommitPair.Parent.Committer.When.UtcDateTime).TotalDays : (Double?)null; }

        /// <summary>
        /// Returns the number files added in this commit.
        /// </summary>
        [CsvColumn(FieldIndex = 15)]
        [JsonProperty(Order = 15)]
        public virtual UInt32 CommitNumberOfAddedFiles { get => (UInt32)this.expoFiles.Where(f => f.TreeChangeIntent == ChangeKind.Added).Count(); }

        /// <summary>
        /// Returns the number of files deleted in this commit.
        /// </summary>
        [CsvColumn(FieldIndex = 16)]
        [JsonProperty(Order = 16)]
        public virtual UInt32 CommitNumberOfDeletedFiles { get => (UInt32)this.expoFiles.Where(f => f.TreeChangeIntent == ChangeKind.Deleted).Count(); }

        /// <summary>
        /// Returns the number of files renamed in this commit.
        /// </summary>
        [CsvColumn(FieldIndex = 17)]
        [JsonProperty(Order = 17)]
        public virtual UInt32 CommitNumberOfRenamedFiles { get => (UInt32)this.expoFiles.Where(f => f.TreeChangeIntent == ChangeKind.Renamed).Count(); }

        /// <summary>
        /// Returns the number of files modified in this commit.
        /// </summary>
        [CsvColumn(FieldIndex = 18)]
        [JsonProperty(Order = 18)]
        public virtual UInt32 CommitNumberOfModifiedFiles { get => (UInt32)this.expoFiles.Where(f => f.TreeChangeIntent == ChangeKind.Modified).Count(); }



        /// <summary>
        /// Add an <see cref="ExportableFile"/> to this commit.
        /// </summary>
        /// <param name="expoFile"></param>
        /// <returns></returns>
        public ExportableCommit AddFile(ExportableFile expoFile)
        {
            this.expoFiles.Add(expoFile);
            return this;
        }

        /// <summary>
        /// Returns a concatenation of all <see cref="ExportableFile"/>s. Each file itself is
        /// a concatenation of its hunks. Here, we prefix each file by its <see cref="ExportableFile.TreeChangeIntent"/>
        /// and its <see cref="ExportableFile.FileName"/> before dumping its content to allow
        /// the files to be separable later on.
        /// </summary>
        public override string ContentInteral => String.Join("\n\n", this.Select(ef =>
        {
            return $"({ef.TreeChangeIntent}) {ef.FileName}:\n-------------\n{ef.ContentInteral}";
        }));

        public IEnumerator<ExportableFile> GetEnumerator()
        {
            return this.expoFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
