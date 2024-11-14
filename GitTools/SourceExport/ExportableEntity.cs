using LibGit2Sharp;
using LINQtoCSV;
using Newtonsoft.Json;
using System;
using Util.Density;
using Util.Extensions;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;


namespace GitTools.SourceExport
{
    /// <summary>
    /// The content value of each <see cref="ExportableEntity"/> can be encoded to
    /// Base64 or JSON. This is useful when exporting CSV, for example. No encoding
    /// should be applied when exporting entities to JSON.
    /// </summary>
    public enum ContentEncoding
    {
        /// <summary>
        /// Do not encode the content value.
        /// </summary>
        Plain,
        /// <summary>
        /// Convert the content to its base-64 representation.
        /// </summary>
        Base64,
        /// <summary>
        /// Convert the content to its JSON representation.
        /// </summary>
        JSON
    }

    /// <summary>
    /// A piece of source code that is exportable (e.g., To JSON or CSV). However, this
    /// class is abstract and requires a concrete type to inherit from it, such as a Hunk,
    /// block, or even a line.
    /// </summary>
    [JsonObject]
    public abstract class ExportableEntity
    {
        /// <summary>
        /// A flag that can be used to export the 
        /// </summary>
        [JsonIgnore]
        public ContentEncoding ContentEncoding { get; set; } = ContentEncoding.Plain;

        /// <summary>
        /// An entity always relates to an <see cref="ExportCommitPair"/>, which is a sub-class of
        /// <see cref="CommitPair"/>. This means there is a child and a parent commit.
        /// </summary>
        [JsonIgnore]
        public ExportCommitPair ExportCommitPair { get; protected set; }


        /// <summary>
        /// Creates a new exportable entity that is based on a pair of commits and is concerned
        /// with exactly one file that was changed (i.e., a single <see cref="TreeEntryChanges"/>).
        /// </summary>
        /// <param name="exportCommit"></param>
        public ExportableEntity(ExportCommitPair exportCommit)
        {
            this.ExportCommitPair = exportCommit;
        }

        /// <summary>
        /// This is the main property of every exportable item and represents a piece of source
        /// code, such as a line or hunk.
        /// </summary>
        [JsonIgnore]
        public abstract String ContentInteral { get; }

        /// <summary>
        /// The content that will be stored as a CSV field. Optionally base64-encodes.
        /// See <see cref="ContentEncoding"/>.
        /// </summary>
        [CsvColumn(FieldIndex = 999)]
        [JsonProperty(Order = 999)]
        public String Content { get => this.ContentEncoding == ContentEncoding.Plain ? this.ContentInteral :
                (this.ContentEncoding == ContentEncoding.Base64 ? this.ContentInteral.ToBase64() : this.ContentInteral.ToJSON());
        }
    }
}
