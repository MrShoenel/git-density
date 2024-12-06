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
using GitDensity.Similarity;
using LINQtoCSV;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JsonConverter = Newtonsoft.Json.JsonConverter;
using JsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents a contiguous block of one or more lines that were affected in
    /// a <see cref="ExportableHunk"/>. Contiguous refers to the <see cref="TextBlockNature"/>
    /// of the block. A hunk may be comprised of multiple interleaved blocks of varying nature.
    /// The nature varies between <see cref="TextBlockNature.Context"/> (i.e., an untouched
    /// block of lines) and changed. Note, however, that a changed block consists of 0 or more
    /// deleted lines, followed by zero or more added lines (but it cannot be empty though).
    /// If added and deleted, the nature will be <see cref="TextBlockNature.Replaced"/>. Otherwise,
    /// it will directly correspond to <see cref="TextBlockNature.Added"/> or <see cref="TextBlockNature.Deleted"/>,
    /// respectively.
    /// </summary>
    [JsonObject]
    public class ExportableBlock : ExportableHunk, IEnumerable<Line>
    {
        /// <summary>
        /// The inherited hunk.
        /// </summary>
        [JsonIgnore]
        public ExportableHunk ExportableHunk { get; protected set; }

        /// <summary>
        /// The encapsulated <see cref="TextBlock"/> that holds <see cref="Line"/>s.
        /// </summary>
        [JsonIgnore]
        public LooseTextBlock TextBlock { get; protected set; }

        /// <summary>
        /// Creates a new contiguous exportable block of lines, by taking into account
        /// its parent <see cref="SourceExport.ExportableHunk"/>.
        /// </summary>
        /// <param name="exportableHunk"></param>
        /// <param name="textBlock"></param>
        /// <param name="blockIdx"></param>
        public ExportableBlock(ExportableHunk exportableHunk, LooseTextBlock textBlock, uint blockIdx) : base(exportableHunk.ExportableFile, exportableHunk.Hunk, hunkIdx: exportableHunk.HunkIdx)
        {
            this.TextBlock = textBlock;
            this.ExportableHunk = exportableHunk;
            this.BlockIdx = blockIdx;

            // Let's determine the nature of this block.
            bool added = textBlock.LinesAdded > 0, deleted = textBlock.LinesDeleted > 0, untouched = textBlock.LinesUntouched > 0;
            Debug.Assert(((added || deleted) && !untouched) || (untouched && !added && !deleted));

            this.BlockNature = added && deleted ? TextBlockNature.Replaced : (added ? TextBlockNature.Added : (deleted ? TextBlockNature.Deleted : TextBlockNature.Context));
        }

        /// <summary>
        /// The nature is determined by the (aggregated) nature of the contained
        /// <see cref="TextBlock"/>'s lines.
        /// </summary>
        [CsvColumn(FieldIndex = 40)]
        [JsonProperty(Order = 40), JsonConverter(typeof(StringEnumConverter))]
        public TextBlockNature BlockNature { get; protected set; }

        /// <summary>
        /// The zero-based index of the block. This is very similar to <see cref="ExportableHunk.HunkIdx"/>,
        /// but on block-level. The order is top to bottom, with the top-most block having an index of zero.
        /// </summary>
        [CsvColumn(FieldIndex = 41)]
        [JsonProperty(Order = 41)]
        public UInt32 BlockIdx { get; protected set; }

        [CsvColumn(FieldIndex = 42)]
        [JsonIgnore]
        public String BlockLineNumbersDeleted { get => String.Join(",", this.BlockLineNumbersDeleted_JSON); }

        [JsonProperty(Order = 42, PropertyName = nameof(BlockLineNumbersDeleted))]
        public IEnumerable<UInt32> BlockLineNumbersDeleted_JSON { get => this.TextBlock.Lines.Where(l => l.Type == LineType.Deleted).OrderBy(l => l.Number).Select(l => l.Number); }

        [CsvColumn(FieldIndex = 43)]
        [JsonIgnore]
        public String BlockLineNumbersAdded { get => String.Join(",", this.BlockLineNumbersAdded_JSON); }

        [JsonProperty(Order = 43, PropertyName = nameof(BlockLineNumbersAdded))]
        public IEnumerable<UInt32> BlockLineNumbersAdded_JSON { get => this.TextBlock.Lines.Where(l => l.Type == LineType.Added).OrderBy(l => l.Number).Select(l => l.Number); }

        [CsvColumn(FieldIndex = 44)]
        [JsonIgnore]
        public String BlockLineNumbersUntouched { get => String.Join(",", this.BlockLineNumbersUntouched_JSON); }

        [JsonProperty(Order = 44, PropertyName = nameof(BlockLineNumbersUntouched))]
        public IEnumerable<UInt32> BlockLineNumbersUntouched_JSON { get => this.TextBlock.Lines.Where(l => l.Type == LineType.Untouched).OrderBy(l => l.Number).Select(l => l.Number); }

        /// <summary>
        /// This is a computed property, so it needs to be virtual.
        /// </summary>
        [CsvColumn(FieldIndex = 45)]
        [JsonProperty(Order = 45)]
        public virtual UInt32 BlockNumberOfLines { get => (UInt32)(this as IEnumerable<Line>).Count(); }

        [CsvColumn(FieldIndex = 46)]
        [JsonProperty(Order = 46)]
        public virtual UInt32 BlockOldLineStart { get => this.ExportableHunk.HunkOldLineStart + this.ExportableHunk.NumberOfOldLinesBefore(ltb: this.TextBlock); }

        [CsvColumn(FieldIndex = 47)]
        [JsonProperty(Order = 47)]
        public virtual UInt32 BlockNewLineStart { get => this.ExportableHunk.HunkNewLineStart + this.ExportableHunk.NumberOfNewLinesBefore(ltb: this.TextBlock); }

        #region override
        /// <summary>
        /// Overridden so we can copy down this value from the encapsulated hunk.
        /// </summary>
        [CsvColumn(FieldIndex = 37)]
        [JsonProperty(Order = 37)]
        public sealed override uint HunkNumberOfBlocks { get => this.ExportableHunk.HunkNumberOfBlocks; }
        #endregion


        /// <summary>
        /// Similar to <see cref="ExportableHunk"/>, the content here are the raw lines. Each
        /// line starts with a character that designates its nature (i.e., space, +, or -). A
        /// block may also contain empty lines (i.e., one or more whitespace characters).
        /// </summary>
        public override String ContentInteral { get => this.TextBlock.WholeBlock; }

        /// <summary>
        /// Allows this block to be enumerated as lines.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Line> GetEnumerator()
        {
            return this.TextBlock.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
