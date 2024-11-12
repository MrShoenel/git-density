using Iesi.Collections.Generic;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Util.Extensions;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents an entire commit to be exported.
    /// </summary>
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
