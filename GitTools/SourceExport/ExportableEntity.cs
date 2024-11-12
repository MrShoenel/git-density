using LibGit2Sharp;
using System;
using Util.Density;


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
        public ExportCommitPair ExportCommit { get; protected set; }


        /// <summary>
        /// Creates a new exportable entity that is based on a pair of commits and is concerned
        /// with exactly one file that was changed (i.e., a single <see cref="TreeEntryChanges"/>).
        /// </summary>
        /// <param name="exportCommit"></param>
        public ExportableEntity(ExportCommitPair exportCommit)
        {
            this.ExportCommit = exportCommit;
        }

        /// <summary>
        /// This is the main property of every exportable item and represents a piece of source
        /// code, such as a line or hunk.
        /// </summary>
        public abstract String Content { get; }
    }
}
