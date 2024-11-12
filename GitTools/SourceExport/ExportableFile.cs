using Iesi.Collections.Generic;
using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace GitTools.SourceExport
{
    public class ExportableFile : ExportableEntity, IEnumerable<ExportableHunk>
    {
        protected LinkedHashSet<ExportableHunk> expoHunks;

        public ExportableFile(ExportCommitPair exportCommit, TreeEntryChanges treeChange) : base(exportCommit, treeChange)
        {
            this.expoHunks = new LinkedHashSet<ExportableHunk>();
        }

        /// <summary>
        /// Exporting all changes for one file means that we will just concatenate all
        /// <see cref="expoHunks"/> in the file using two newlines.
        /// </summary>
        [CsvColumn(FieldIndex = 999)]
        public override string Content => String.Join("\n\n", this.expoHunks.Select(eh => eh.Content));

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
