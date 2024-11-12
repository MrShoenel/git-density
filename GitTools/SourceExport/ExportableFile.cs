using Iesi.Collections.Generic;
using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents an entire file (<see cref="TreeEntryChanges"/>) to be exported.
    /// </summary>
    public class ExportableFile : ExportableCommit, IEnumerable<ExportableHunk>
    {
        public ExportableCommit ExportableCommit { get; protected set; }

        protected LinkedHashSet<ExportableHunk> expoHunks;

        /// <summary>
        /// Each single <see cref="TreeEntryChanges"/> refers to a single file that was modified.
        /// </summary>
        public TreeEntryChanges TreeChange { get; protected set; }

        public ExportableFile(ExportableCommit exportableCommit, TreeEntryChanges treeChange) : base(exportableCommit.ExportCommit)
        {
            this.ExportableCommit = exportableCommit;
            this.expoHunks = new LinkedHashSet<ExportableHunk>();
            this.TreeChange = treeChange;
        }

        /// <summary>
        /// The original event that triggered the creation/removal of this entity.
        /// For example, creating a new file or renaming an existing one. We
        /// retain this information because entities such as single lines do not
        /// store this information.
        /// </summary>
        [CsvColumn(FieldIndex = 3)]
        public ChangeKind TreeChangeIntent { get => this.TreeChange.Status; }

        /// <summary>
        /// The relative path (in the repository) of the file that was affected.
        /// </summary>
        [CsvColumn(FieldIndex = 4)]
        public String FileName { get => this.TreeChange.Path; }

        /// <summary>
        /// Exporting all changes for one file means that we will just concatenate all
        /// <see cref="expoHunks"/> in the file using two newlines.
        /// </summary>
        [CsvColumn(FieldIndex = 999)]
        public override string Content => String.Join("\n\n", this.expoHunks.Select(eh => eh.Content));

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
