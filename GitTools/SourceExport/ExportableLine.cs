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
using System.Collections.Generic;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace GitTools.SourceExport
{
    [JsonObject]
    public class ExportableLine : ExportableBlock, IEnumerable<char>
    {
        [JsonIgnore]
        public Line Line { get; protected set; }

        public ExportableLine(ExportableBlock exportableBlock, Line line) : base(exportableBlock.ExportableHunk, exportableBlock.TextBlock, exportableBlock.BlockIdx)
        {
            this.Line = line;
        }


        [CsvColumn(FieldIndex = 50)]
        [JsonProperty(Order = 50), JsonConverter(typeof(StringEnumConverter))]
        public LineType LineType { get => this.Line.Type; }

        [CsvColumn(FieldIndex = 51)]
        [JsonProperty(Order = 51)]
        public UInt32 LineNumber { get => this.Line.Number; }

        /// <summary>
        /// The returned line starts with a character that indicates wheter it is a context
        /// (untouched) line, or new (+) or deleted (-).
        /// </summary>
        [JsonIgnore]
        public override String ContentInteral { get => this.Line.String; }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return this.Line.String.GetEnumerator();
        }
    }
}
