using Iesi.Collections.Generic;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Util.Extensions;

namespace GitTools.SourceExport
{
    public class ExportableCommit : ExportableEntity, IEnumerable<ExportableFile>
    {
        protected LinkedHashSet<ExportableFile> expoFiles;

        public ExportableCommit(ExportCommitPair exportCommit) : base(exportCommit)
        {
            this.expoFiles = new LinkedHashSet<ExportableFile>();
        }

        /// <summary>
        /// The hash of the child commit.
        /// </summary>
        [CsvColumn(FieldIndex = 1)]
        public String SHA1 { get => this.ExportCommit.Child.ShaShort(); }

        /// <summary>
        /// The hash of the parent commit.
        /// </summary>
        [CsvColumn(FieldIndex = 2)]
        public String SHA_Parent { get => this.ExportCommit.Parent?.ShaShort() ?? "(initial)"; }

        public ExportableCommit AddFile(ExportableFile expoFile)
        {
            this.expoFiles.Add(expoFile);
            return this;
        }

        [CsvColumn(FieldIndex = 999)]
        public override string Content => String.Join("\n\n", this.Select(ef =>
        {
            return $"({ef.TreeChangeIntent}) {ef.FileName}:\n-------------\n{ef.Content}";
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
