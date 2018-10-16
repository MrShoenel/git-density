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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Util;
using System.Diagnostics;
using Util.Data.Entities;
using Util.Extensions;
using Newtonsoft.Json;
using Util.Logging;
using Microsoft.Extensions.Logging;

namespace GitMetrics.QualityAnalyzer.VizzAnalyzer
{
	public class VizzMetricsAnalyzer : IMetricsAnalyzer
	{
		protected BaseLogger<VizzMetricsAnalyzer> logger = Program.CreateLogger<VizzMetricsAnalyzer>();

		public MetricsAnalyzerConfiguration Configuration { get; set; }

		public MetricsAnalysisResult Analyze(Repository repository, Commit commit)
		{
			Commands.Checkout(repository, commit);
			var conf = this.Configuration.Configuration;

			var result = new MetricsAnalysisResult
			{
				Commit = commit,
				Repository = repository
			};

			this.logger.LogDebug($"Starting metrics extraction at commit {commit.ShaShort()}");
			using (var proc = Process.Start(new ProcessStartInfo {
				FileName = (String)conf["pathToBinary"],
				Arguments = $"{(String)conf["args"]} {repository.Info.WorkingDirectory}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				WorkingDirectory = repository.Info.WorkingDirectory,
				WindowStyle = ProcessWindowStyle.Hidden
			}))
			{
				var outputRaw = proc.StandardOutput.ReadToEnd();
				JsonOutput json = null;
				proc.WaitForExit();

				switch (proc.ExitCode)
				{
					case (int)CommitMetricsStatus.OK:
						result.CommitMetricsStatus = CommitMetricsStatus.OK;
						json = JsonConvert.DeserializeObject<JsonOutput>(outputRaw);
						result.MetricTypes.AddAll(GetMetricTypesFromJson(json));
						result.Metrics.AddAll(GetMetricsFromJson(json));
						this.logger.LogInformation($"Metrics extraction successful: {result.MetricTypes.Count} metrics-types, {result.Metrics.Count} metrics");
						break;
					case (int)CommitMetricsStatus.AnalyzerError:
						result.CommitMetricsStatus = CommitMetricsStatus.AnalyzerError;
						break;
					case (int)CommitMetricsStatus.BuildError:
						result.CommitMetricsStatus = CommitMetricsStatus.BuildError;
						break;
					case (int)CommitMetricsStatus.InvalidProjectType:
						result.CommitMetricsStatus = CommitMetricsStatus.InvalidProjectType;
						break;
					default:
						result.CommitMetricsStatus = CommitMetricsStatus.OtherError;
						break;
				}

				if (result.CommitMetricsStatus != CommitMetricsStatus.OK)
				{
					this.logger.LogWarning($"The metrics extraction failed ({result.CommitMetricsStatus}).");
				}
			}

			return result;
		}

		protected static IEnumerable<MetricTypeEntity> GetMetricTypesFromJson(JsonOutput output)
		{
			throw new NotImplementedException();
		}

		protected static IEnumerable<MetricEntity> GetMetricsFromJson(JsonOutput output)
		{
			throw new NotImplementedException();
		}
	}
}
