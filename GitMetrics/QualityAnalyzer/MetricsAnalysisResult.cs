/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitMetrics. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using LINQtoCSV;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Util.Data.Entities;
using Util.Extensions;

namespace GitMetrics.QualityAnalyzer
{
	/// <summary>
	/// Class to be filled with resulting entities by implementations of
	/// <see cref="IMetricsAnalyzer"/>.
	/// Please note that the actual analyzer must not interconnect the
	/// entities (e.g. adding <see cref="MetricEntity"/> objects to the
	/// e.g. <see cref="RepositoryEntity"/> object. The only exception to
	/// this is referencing the <see cref="MetricTypeEntity"/> from within
	/// each of the <see cref="MetricEntity"/> objects.
	/// </summary>
	public class MetricsAnalysisResult
	{
		/// <summary>
		/// Refers to the <see cref="RepositoryEntity"/> that was analyzed.
		/// </summary>
		public RepositoryEntity Repository { get; set; }

		/// <summary>
		/// Refers to the <see cref="CommitEntity"/> that was analyzed within the repository.
		/// </summary>
		public CommitEntity Commit { get; set; }

		/// <summary>
		/// There is one per <see cref="Commit"/>.
		/// </summary>
		public CommitMetricsStatus CommitMetricsStatus { get; set; }

		/// <summary>
		/// A set of metrics that are used within the obtained metrics. It is recommended
		/// that <see cref="MetricTypeEntity"/> objects are retrieved/created using the
		/// method <see cref="MetricTypeEntity.ForSettings(string, bool, bool, bool, double, bool)"/>.
		/// </summary>
		public IList<MetricTypeEntity> MetricTypes { get; set; }
			= new List<MetricTypeEntity>();

		/// <summary>
		/// An enumerable of metrics as obtained from the <see cref="Repository"/> and the
		/// corresponding <see cref="Commit"/>.
		/// </summary>
		public IList<MetricEntity> Metrics { get; set; }
			= new List<MetricEntity>();

		#region As serializable
		public class MetricsAnalysisResultSerializable
		{
			[JsonIgnore]
			private readonly MetricsAnalysisResult mar;

			[JsonIgnore]
			private readonly MetricEntity metric;
			protected internal MetricsAnalysisResultSerializable(
				MetricsAnalysisResult mar, MetricEntity metric = null)
			{
				this.mar = mar;
				this.metric = metric;
			}

			[CsvColumn(FieldIndex = 1)]
			[JsonProperty(Required = Required.Always, PropertyName = "commitId", Order = 1)]
			public String CommitId => this.mar.Commit.BaseObject.ShaShort(10);

			[CsvColumn(FieldIndex = 2)]
			[JsonProperty(Required = Required.Always, PropertyName = "status", Order = 2)]
			public String Status => this.mar.CommitMetricsStatus.ToString();

			[CsvColumn(FieldIndex = 3, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "scope", Order = 3)]
			public String Scope => this.metric?.OutputEntityType.ToString();

			[CsvColumn(FieldIndex = 4, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "metricName", Order = 4)]
			public String MetricName => this.metric?.MetricType.MetricName;

			[CsvColumn(FieldIndex = 5, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "metricValue", Order = 5)]
			public Double? MetricValue => this.metric?.MetricValue;

			[CsvColumn(FieldIndex = 6, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "isPublic", Order = 6)]
			public Boolean? IsPublic => this.metric?.MetricType.IsPublic;

			[CsvColumn(FieldIndex = 7, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "isRequired", Order = 7)]
			public Boolean? IsScore => this.metric?.MetricType.IsScore;

			[CsvColumn(FieldIndex = 8, CanBeNull = true)]
			[JsonProperty(Required = Required.AllowNull, PropertyName = "isRoot", Order = 8)]
			public Boolean? IsRoot => this.metric?.MetricType.IsRoot;
		}

		protected readonly Lazy<IEnumerable<MetricsAnalysisResultSerializable>> asSerializable;
		public Lazy<IEnumerable<MetricsAnalysisResultSerializable>> AsSerializable =>
			new Lazy<IEnumerable<MetricsAnalysisResultSerializable>>(() => this.asSerializable.Value);

		public MetricsAnalysisResult()
		{
			this.asSerializable = new Lazy<IEnumerable<MetricsAnalysisResultSerializable>>(() =>
			{
				if (this.CommitMetricsStatus == CommitMetricsStatus.OK)
				{
					return this.Metrics.Select(metric =>
					{
						return new MetricsAnalysisResultSerializable(this, metric);
					});
				}
				else
				{
					// If the metrics could not be obtained, return an entity that
					// shows the commit-ID and the status at least.
					return new MetricsAnalysisResultSerializable(this, null).AsEnumerable();
				}
			});
		}
		#endregion
	}
}
