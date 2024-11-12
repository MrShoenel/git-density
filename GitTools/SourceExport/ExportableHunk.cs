using GitDensity.Density;
using GitDensity.Similarity;
using Iesi.Collections.Generic;
using LibGit2Sharp;
using LINQtoCSV;
using NHibernate.Mapping;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Util.Extensions;
using Line = GitDensity.Similarity.Line;


namespace GitTools.SourceExport
{

    /// <summary>
    /// In <see cref="Util.Data.Entities.FileBlockType"/> we have previously used
    /// types for added, deleted, and modified. There were only these three types
    /// because we never saved information about blocks that were untouched (context).
    /// Here, we add the fourth kind so that a <see cref="TextBlock"/> can indentify
    /// itself as any of the four kinds.
    /// </summary>
    public enum TextBlockNature : uint
    {
        /// <summary>
        /// An untouched block of lines, usually shown as part of the Hunk (context).
        /// </summary>
        Context = 0u,

        /// <summary>
        /// A block that has one or more lines added, but no lines deleted.
        /// </summary>
        Added = 1u,

        /// <summary>
        /// A block that has one or more lines deleted, but no lines added.
        /// </summary>
        Deleted = 2u,

        /// <summary>
        /// A block that has one or more lines added, directly followed by one or
        /// more lines deleted (i.e., no other lines in between).
        /// </summary>
        Replaced = 3u
    }


    /// <summary>
    /// We need another kind of text block, because the original <see cref="TextBlock"/>
    /// was not meant for holding old (removed) and new (added) lines simultaneously.
    /// This type here is much less impartial and manages lines and their number by
    /// relying on the <see cref="Line"/>s themselves. The API, otherwise, mimics the
    /// original text block as much as possible.
    /// </summary>
    public class LooseTextBlock : IEnumerable<Line>
    {
        public UInt32 LinesAdded { get => (UInt32)this.lines.Where(l => l.Type == LineType.Added).Count(); }

        public UInt32 LinesDeleted { get => (UInt32)this.lines.Where(l => l.Type == LineType.Deleted).Count(); }

        public UInt32 LinesUntouched { get => (UInt32)this.lines.Where(l => l.Type == LineType.Untouched).Count(); }

        public Boolean IsEmpty { get => this.lines.Count == 0; }

        protected LinkedHashSet<Line> lines;

        public ReadOnlySet<Line> Lines { get; protected set; }

        public LooseTextBlock()
        {
            this.lines = new LinkedHashSet<Line>();
            this.Lines = new ReadOnlySet<Line>(this.lines);
        }

        public LooseTextBlock AddLine(Line line)
        {
            this.lines.Add(line);
            return this;
        }

        public IEnumerator<Line> GetEnumerator()
        {
            return this.Lines.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public String WholeBlock => String.Join("\n",
            this.Lines.OrderBy(l => l.Number).Select(l => l.String));
    }



    /// <summary>
    /// Represents an entire <see cref="GitDensity.Density.Hunk"/> that can be exported
    /// as an entiry. For each changed file, there are one or more hunks. Each hunk can
    /// have one or more blocks of unchanged or changed lines.
    /// </summary>
    public class ExportableHunk : ExportableFile, IEnumerable<LooseTextBlock>
    {
        public ExportableFile ExportableFile { get; protected set; }

        /// <summary>
        /// The encapsulated <see cref="GitDensity.Density.Hunk"/>.
        /// </summary>
        public Hunk Hunk { get; protected set; }


        public ExportableHunk(ExportableFile file, Hunk hunk, uint hunkIdx) : base(file.ExportableCommit, file.TreeChange, file.FileIdx)
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
        public IEnumerator<LooseTextBlock> GetEnumerator()
        {
            var lines = new Queue<String>(this.Hunk.Patch.GetLines(removeEmptyLines: false));
            if (lines.Count == 0)
            {
                return Enumerable.Empty<LooseTextBlock>().GetEnumerator();
            }

            Func<string, LineType> getType = (string line) =>
            {
                var fc = line.Length == 0 ? 'X' : line[0];
                // Anything that's not added or deleted is context. 'X', therefore, is just a placeholder.
                return fc == '+' ? LineType.Added : (fc == '-' ? LineType.Deleted : LineType.Untouched);
            };


            var blocks = new LinkedList<LooseTextBlock>();
            // Create first block to be added.
            var lastBlock = new LooseTextBlock();
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
                    lastBlock = new LooseTextBlock();
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
        public override String ContentInteral { get => this.Hunk.Patch; }
    }
}
