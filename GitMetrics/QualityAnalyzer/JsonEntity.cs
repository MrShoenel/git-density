/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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

namespace GitMetrics.QualityAnalyzer
{
	/// <summary>
	/// Used to deserialize within class <see cref="JsonOutput"/>.
	/// </summary>
	public class JsonEntity
	{
		[JsonProperty(PropertyName = "type", Required = Required.Always)]
		public Int32 Type { get; set; }

		[JsonProperty(PropertyName = "name", Required = Required.Always)]
		public String Name { get; set; } = String.Empty;

		[JsonProperty(PropertyName = "metricsValues", Required = Required.Always)]
		public IDictionary<String, Double> MetricsValues { get; set; }
			= new Dictionary<String, Double>();
	}
}
