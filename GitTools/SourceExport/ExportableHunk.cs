using GitDensity.Density;
using GitDensity.Similarity;
using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Util.Extensions;
using Line = GitDensity.Similarity.Line;


namespace GitTools.SourceExport
{
    /// <summary>
    /// Represents an entire <see cref="GitDensity.Density.Hunk"/> that can be exported
    /// as an entiry. For each changed file, there are one or more hunks. Each hunk can
    /// have one or more blocks of unchanged or changed lines.
    /// </summary>
    public class ExportableHunk : ExportableFile, IEnumerable<TextBlock>
    {
        public ExportableFile ExportableFile { get; protected set; }

        /// <summary>
        /// The encapsulated <see cref="GitDensity.Density.Hunk"/>.
        /// </summary>
        public Hunk Hunk { get; protected set; }


        public ExportableHunk(ExportableFile file, Hunk hunk, uint hunkIdx) : base(file.ExportableCommit, file.TreeChange)
        {
            this.ExportableFile = file;
            this.Hunk = hunk;
            this.HunkIdx = hunkIdx;
        }

        /// <summary>
        /// Uses the <see cref="Hunk"/>'s <see cref="Patch"/> to get its lines first.
        /// Then creates contiguous blocks of text (one or more lines) that can be
        /// assigned to a block of pure <see cref="TextBlockNature"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TextBlock> GetEnumerator()
        {
            var lines = new Queue<String>(this.Hunk.Patch.GetLines(removeEmptyLines: false));
            if (lines.Count == 0)
            {
                return Enumerable.Empty<TextBlock>().GetEnumerator();
            }

            Func<string, LineType> getType = (string line) =>
            {
                var fc = line.Length == 0 ? 'X' : line[0];
                // Anything that's not added or deleted is context. 'X', therefore, is just a placeholder.
                return fc == '+' ? LineType.Added : (fc == '-' ? LineType.Deleted : LineType.Untouched);
            };


            var blocks = new LinkedList<TextBlock>();
            // Create first block to be added.
            var lastBlock = new TextBlock();
            var lastLine = "X";

            var idxOld = this.Hunk.OldLineStart;
            var idxNew = this.Hunk.NewLineStart;
            while (lines.Count > 0)
            {
                var line = lines.Peek();
                var type = getType(line);

                // There is already something in the last block. We need to create contiguous blocks
                // if untouched or added+deleted lines.
                var addDel = type != LineType.Untouched;
                var addDelLast = getType(lastLine) != LineType.Untouched;

                if (!lastBlock.IsEmpty && (addDel ^ addDelLast))
                {
                    // Switch between untouched(context) lines and added/deleted lines -> new Block.
                    blocks.AddLast(lastBlock);
                    lastBlock = new TextBlock();
                    continue;
                }

                line = lines.Dequeue();
                lastLine = line;
                var number = type == LineType.Deleted ? idxOld : idxNew;
                // Line numbers: Deleted increases deleted, added increases added, but untouched
                // (context) increases either!
                idxOld = type == LineType.Untouched || type == LineType.Deleted ? idxOld + 1 : idxOld;
                idxNew = type == LineType.Untouched || type == LineType.Added ? idxNew + 1 : idxNew;

                lastBlock.AddLine(new Line(type: type, number: number, @string: line));
                if (lines.Count == 0)
                {
                    blocks.AddLast(lastBlock);
                }
            }

            return blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        /// <summary>
        /// The zero-based index of the hunk. Hunks within an affected file are ordered
        /// by where the change occurred, top to bottom.
        /// </summary>
        [CsvColumn(FieldIndex = 5)]
        public UInt32 HunkIdx { get; protected internal set; }

        /// <summary>
        /// A concatenation of line numbers that were added.
        /// </summary>
        [CsvColumn(FieldIndex = 6)]
        public String HunkLineNumbersAdded { get => String.Join(",", this.Hunk.LineNumbersAdded); }

        /// <summary>
        /// A concatenation of line numbers that were deleted.
        /// </summary>
        [CsvColumn(FieldIndex = 7)]
        public String HunkLineNumbersDeleted { get => String.Join(",", this.Hunk.LineNumbersDeleted); }

        [CsvColumn(FieldIndex = 8)]
        public UInt32 HunkOldLineStart { get => this.Hunk.OldLineStart; }

        [CsvColumn(FieldIndex = 9)]
        public UInt32 HunkOldNumberOfLines { get => this.Hunk.OldNumberOfLines; }

        [CsvColumn(FieldIndex = 10)]
        public UInt32 HunkNewLineStart { get => this.Hunk.NewLineStart; }

        [CsvColumn(FieldIndex = 11)]
        public UInt32 HunkNewNumberOfLines { get => this.Hunk.NewNumberOfLines; }

        /// <summary>
        /// The entire hunk's content as a string. Each line will have a leading character
        /// (space, +, -) that designates if the line is unchanged, added, or deleted.
        /// </summary>
        [CsvColumn(FieldIndex = 999)]
        public override String Content { get => this.Hunk.Patch; }
    }
}
