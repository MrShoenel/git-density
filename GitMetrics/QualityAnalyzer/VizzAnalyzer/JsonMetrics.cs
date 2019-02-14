/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Data.Entities;
using Util.Extensions;

namespace GitMetrics.QualityAnalyzer.VizzAnalyzer
{
	/// <summary>
	/// Used to deserialize within class <see cref="JsonOutput"/>.
	/// </summary>
	public class JsonMetrics
	{
		[JsonProperty(PropertyName = "metricName", Required = Required.Always)]
		public String Name { get; set; } = String.Empty;

		[JsonProperty(PropertyName = "metricDescription", Required = Required.Always)]
		public String Description { get; set; } = String.Empty;

		[JsonProperty(PropertyName = "isRoot", Required = Required.Always)]
		public Boolean IsRoot { get; set; }

		[JsonProperty(PropertyName = "isPublic", Required = Required.Always)]
		public Boolean IsPublic { get; set; }

		[JsonProperty(PropertyName = "precision", Required = Required.Always)]
		public Double Precision { get; set; }

		[JsonProperty(PropertyName = "children", Required = Required.Default)]
		public JsonMetrics[] Children { get; set; }
			= new JsonMetrics[0];

		[JsonProperty(PropertyName = "childrenWeights", Required = Required.Default)]
		public Double[] ChildrenWeights { get; set; }
			= new Double[0];

		protected readonly Lazy<MetricTypeEntity> asMetricType;

		public MetricTypeEntity AsMetricType { get => this.asMetricType.Value; }

		public ISet<MetricTypeEntity> AsMetricTypeWithChildren
		{
			get
			{
				var set = new HashSet<MetricTypeEntity>();
				set.Add(this.AsMetricType);
				set.AddAll(this.Children.Select(child => child.AsMetricType));
				return set;
			}
		}

		public JsonMetrics()
		{
			this.asMetricType = new Lazy<MetricTypeEntity>(() =>
			{
				return MetricTypeEntity.ForSettings(
					this.Name,
					// The property actually does not exist in the JSON at this time and the types
					// of metrics are only later inserted and also retrieved by ::ForSettings(..).
					isScore: false,
					isRoot: this.IsRoot,
					isPublic: this.IsPublic,
					accuracy: this.Precision,
					forceLookupAccuary: true);
 			});
		}
	}
}
