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
