using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Density;
using Util.Extensions;



namespace GitTools.SourceExport
{
    /// <summary>
    /// A piece of source code that is exportable (e.g., To JSON or CSV). However, this
    /// class is abstract and requires a concrete type to inherit from it, such as a Hunk,
    /// block, or even a line.
    /// </summary>
    public abstract class ExportableEntity
    {
        /// <summary>
        /// An entity always relates to an <see cref="ExportCommit"/>, which is a sub-class of
        /// <see cref="CommitPair"/>. This means there is a child and a parent commit.
        /// </summary>
        public ExportCommit ExportCommit { get; protected set; }

        /// <summary>
        /// Each single <see cref="TreeEntryChanges"/> refers to a single file that was modified.
        /// </summary>
        public TreeEntryChanges TreeChange { get; protected set; }

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
        /// Creates a new exportable entity that is based on a pair of commits and is concerned
        /// with exactly one file that was changed (i.e., a single <see cref="TreeEntryChanges"/>).
        /// </summary>
        /// <param name="exportCommit"></param>
        /// <param name="treeChanges"></param>
        public ExportableEntity(ExportCommit exportCommit, TreeEntryChanges treeChanges)
        {
            this.ExportCommit = exportCommit;
            this.TreeChange = treeChanges;
        }
    }
}
