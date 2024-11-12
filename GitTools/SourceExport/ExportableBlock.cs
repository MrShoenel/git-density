﻿using GitDensity.Similarity;
using LINQtoCSV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents a contiguous block of one or more lines that were affected in
    /// a <see cref="exportableHunk"/>. Contiguous refers to the <see cref="TextBlockNature"/>
    /// of the block. A hunk may be comprised of multiple interleaved blocks of varying nature.
    /// The nature varies between <see cref="TextBlockNature.Context"/> (i.e., an untouched
    /// block of lines) and changed. Note, however, that a changed block consists of 0 or more
    /// deleted lines, followed by zero or more added lines (but it cannot be empty though).
    /// If added and deleted, the nature will be <see cref="TextBlockNature.Replaced"/>. Otherwise,
    /// it will directly correspond to <see cref="TextBlockNature.Added"/> or <see cref="TextBlockNature.Deleted"/>,
    /// respectively.
    /// </summary>
    public class ExportableBlock : ExportableEntity
    {
        private ExportableHunk exportableHunk;

        /// <summary>
        /// The encapsulated <see cref="TextBlock"/> that holds <see cref="Line"/>s.
        /// </summary>
        public TextBlock TextBlock { get; protected set; }

        /// <summary>
        /// The nature is determined by the (aggregated) nature of the contained
        /// <see cref="TextBlock"/>'s lines.
        /// </summary>
        [CsvColumn(FieldIndex = 5)]
        public TextBlockNature Nature { get; protected set; }

        /// <summary>
        /// Creates a new contiguous exportable block of lines, by taking into account
        /// its parent <see cref="ExportableHunk"/>.
        /// </summary>
        /// <param name="exportableHunk"></param>
        /// <param name="textBlock"></param>
        /// <param name="blockIdx"></param>
        public ExportableBlock(ExportableHunk exportableHunk, TextBlock textBlock, uint blockIdx) : base(exportableHunk.ExportCommit, exportableHunk.TreeChange)
        {
            this.TextBlock = textBlock;
            this.exportableHunk = exportableHunk;
            this.BlockIdx = blockIdx;

            // Let's determine the nature of this block.
            bool added = textBlock.LinesAdded > 0, deleted = textBlock.LinesDeleted > 0, untouched = textBlock.LinesUntouched > 0;
            Debug.Assert(((added || deleted) && !untouched) || (untouched && !added && !deleted));

            this.Nature = added && deleted ? TextBlockNature.Replaced : (added ? TextBlockNature.Added : (deleted ? TextBlockNature.Deleted : TextBlockNature.Context));
        }

        /// <summary>
        /// Essentially, this property is inherited from <see cref="ExportableHunk"/> this
        /// blocks comes from. Therefore, multiple blocks may refer to the same hunk by index.
        /// </summary>
        [CsvColumn(FieldIndex = 6)]
        public UInt32 HunkIdx { get => this.exportableHunk.HunkIdx; }

        /// <summary>
        /// The zero-based index of the block. This is very similar to <see cref="ExportableHunk.HunkIdx"/>,
        /// but on block-level. The order is top to bottom, with the top-most block having an index of zero.
        /// </summary>
        [CsvColumn(FieldIndex = 7)]
        public UInt32 BlockIdx { get; protected set; }

        [CsvColumn(FieldIndex = 8)]
        public String LineNumbersDeleted { get => String.Join(",", this.TextBlock.LinesWithNumber.Where(kv => kv.Value.Type == LineType.Deleted).Select(kv => kv.Key).OrderBy(k => k)); }

        [CsvColumn(FieldIndex = 9)]
        public String LineNumbersAdded { get => String.Join(",", this.TextBlock.LinesWithNumber.Where(kv => kv.Value.Type == LineType.Added).Select(kv => kv.Key).OrderBy(k => k)); }

        [CsvColumn(FieldIndex = 10)]
        public String LineNumbersUntouched { get => String.Join(",", this.TextBlock.LinesWithNumber.Where(kv => kv.Value.Type == LineType.Untouched).Select(kv => kv.Key).OrderBy(k => k)); }


        /// <summary>
        /// Similar to <see cref="exportableHunk"/>, the content here are the raw lines. Each
        /// line starts with a character that designates its nature (i.e., space, +, or -). A
        /// block may also contain empty lines (i.e., one or more whitespace characters).
        /// </summary>
        [CsvColumn(FieldIndex = 999)]
        public override String Content { get => this.TextBlock.WholeBlock; }
    }
}
