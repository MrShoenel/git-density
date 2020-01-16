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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitMetrics.QualityAnalyzer.VizzAnalyzer
{
	/// <summary>
	/// This class is the counterpart to the implementation in Java and its JSON-
	/// formatted output. This class is used to deserialize from JSON.
	/// </summary>
	public class JsonOutput
	{
		[JsonProperty(PropertyName = "outputEntityTypes", Required = Required.Always)]
		public IDictionary<String, Int32> OutputEntityTypes { get; set; }
			= new Dictionary<String, Int32>();

		[JsonProperty(PropertyName = "allMetrics", Required = Required.Always)]
		public ICollection<JsonMetrics> AllMetrics { get; set; }
			= new List<JsonMetrics>();

		[JsonProperty(PropertyName = "allMetricsById", Required = Required.Always)]
		public IDictionary<String, String> AllMetricsById { get; set; }
			= new Dictionary<String, String>();

		[JsonProperty(PropertyName = "entities", Required = Required.Always)]
		public ICollection<JsonEntity> Entities { get; set; }
			= new List<JsonEntity>();
	}
}
