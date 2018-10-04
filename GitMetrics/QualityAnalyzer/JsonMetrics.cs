﻿/// ---------------------------------------------------------------------------------
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
	}
}
