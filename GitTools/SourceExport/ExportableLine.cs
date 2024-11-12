using GitDensity.Similarity;
using LINQtoCSV;
using System;
using System.Collections.Generic;

namespace GitTools.SourceExport
{
    public class ExportableLine : ExportableBlock, IEnumerable<char>
    {
        public Line Line { get; protected set; }

        public ExportableLine(ExportableBlock exportableBlock, Line line) : base(exportableBlock.ExportableHunk, exportableBlock.TextBlock, exportableBlock.BlockIdx)
        {
            this.Line = line;
        }


        [CsvColumn(FieldIndex = 40)]
        public LineType LineType { get => this.Line.Type; }

        [CsvColumn(FieldIndex = 41)]
        public UInt32 LineNumber { get => this.Line.Number; }

        /// <summary>
        /// The returned line starts with a character that indicates wheter it is a context
        /// (untouched) line, or new (+) or deleted (-).
        /// </summary>
        public override String ContentInteral { get => this.Line.String; }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return this.Line.String.GetEnumerator();
        }
    }
}
