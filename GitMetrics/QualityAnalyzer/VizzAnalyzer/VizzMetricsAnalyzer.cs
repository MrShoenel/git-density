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
using System.Text.RegularExpressions;

namespace GitMetrics.QualityAnalyzer.VizzAnalyzer
{
	public class VizzMetricsAnalyzer : IMetricsAnalyzer
	{
		protected BaseLogger<VizzMetricsAnalyzer> logger = Program.CreateLogger<VizzMetricsAnalyzer>();

		public MetricsAnalyzerConfiguration Configuration { get; set; }

		public RepositoryEntity RepositoryEntity { get; protected set; }

		public Repository OriginalRepository { get; protected set; }

		public CommitEntity CommitEntity { get; protected set; }

		public Commit Commit { get => this.CommitEntity.BaseObject; }

		/// <summary>
		/// The key is taken from <see cref="JsonOutput.AllMetricsById"/> and the
		/// value exists twice, once as score.
		/// </summary>
		public IDictionary<String, MetricTypeEntity> MetricTypes { get; }
			= new Dictionary<String, MetricTypeEntity>();

		protected IDictionary<String, TreeEntryChangesEntity> FQJNTreeEntryChanges
			= new Dictionary<String, TreeEntryChangesEntity>();

		#region Analyze
		/// <summary>
		/// 
		/// </summary>
		/// <param name="repositoryEntity"></param>
		/// <param name="commitEntity"></param>
		/// <returns></returns>
		public MetricsAnalysisResult Analyze(Repository originalRepository, RepositoryEntity repositoryEntity, CommitEntity commitEntity)
		{
			this.OriginalRepository = originalRepository;
			this.RepositoryEntity = repositoryEntity;
			this.CommitEntity = commitEntity;

			Commands.Checkout(this.OriginalRepository, this.Commit);
			var conf = this.Configuration.Configuration;

			var result = new MetricsAnalysisResult
			{
				Commit = commitEntity,
				Repository = repositoryEntity
			};

			this.logger.LogDebug($"Starting metrics extraction at commit {this.Commit.ShaShort()}");
			using (var proc = Process.Start(new ProcessStartInfo {
				FileName = (String)conf["pathToBinary"],
				Arguments = $"{(String)conf["args"]} {this.OriginalRepository.Info.WorkingDirectory}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				WorkingDirectory = this.OriginalRepository.Info.WorkingDirectory,
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

						this.GenerateMetricTypesFromJson(json);
						result.MetricTypes.AddAll(this.MetricTypes.Values);

						result.Metrics.AddAll(this.GetMetricsFromJson(json));

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
		#endregion

		protected void GenerateMetricTypesFromJson(JsonOutput output)
		{
			var set = new HashSet<MetricTypeEntity>(
				output.AllMetrics.SelectMany(jsonM => jsonM.AsMetricTypeWithChildren));
			var dict = set.ToDictionary(mte => mte.MetricName, mte => mte);

			const string lookForScore = " (score)";
			this.MetricTypes.Clear();
			foreach (var kv in output.AllMetricsById)
			{
				var metricName = kv.Value.Replace(lookForScore, "");
				var isScore = kv.Value.EndsWith(lookForScore);

				var dictMT = dict[metricName];
				var metricType = MetricTypeEntity.ForSettings(
					metricName, isScore, dictMT.IsRoot, dictMT.IsPublic, dictMT.Accuracy, forceLookupAccuary: true);

				this.MetricTypes.Add(kv.Key, metricType);
			}
		}

		protected static Regex RegexPathToFQJN = new Regex(@"\\|/", RegexOptions.ECMAScript | RegexOptions.Compiled);

		protected static Regex RegexMatchFileType = new Regex(@"\.[a-z0-9]+$", RegexOptions.ECMAScript | RegexOptions.Compiled | RegexOptions.IgnoreCase);

		protected IEnumerable<MetricEntity> GetMetricsFromJson(JsonOutput output)
		{
			this.PrepareTreeEntryChanges();

			foreach (var entity in output.Entities)
			{
				foreach (var value in entity.MetricsValues)
				{
					yield return new MetricEntity
					{
						Commit = this.CommitEntity,
						MetricType = this.MetricTypes[value.Key],
						MetricValue = value.Value,
						OutputEntityType = entity.Type,
						TreeEntryChange = this.GetTreeEntryChangeForMetricsEntity(entity)
					};
				}
			}
		}

		protected void PrepareTreeEntryChanges()
		{
			this.FQJNTreeEntryChanges.Clear();

			foreach (var tec in this.RepositoryEntity.TreeEntryContributions.Select(tec => tec.TreeEntryChanges))
			{
				// We're always interested in the file as it's now, so use PathNew:
				var path = tec.PathNew;
				// path my look like:
				// 'src\main\java\de\alaoli\games\minecraft\mods\limitedresources\Config.java'
				// .. and we need to transform it into a fully-qualified class-name, like:
				// 'src.main.java.de.alaoli.games.minecraft.mods.limitedresources.Config'
				// .. so that .EndsWith() works.
				var fqjn = RegexPathToFQJN.Replace(path, ".");
				fqjn = RegexMatchFileType.Replace(fqjn, String.Empty);

				this.FQJNTreeEntryChanges[fqjn] = tec;
			}
		}

		protected TreeEntryChangesEntity GetTreeEntryChangeForMetricsEntity(JsonEntity je)
		{
			// A TreeEntryChangesEntity is only returned for type file; all others must be null!
			if (je.Type != OutputEntityType.File)
			{
				return null;
			}

			// It's a file; let's try to match the fully qualified Java name to a
			// TreeEntryChange. This must work, so we use .Single()!
			return this.FQJNTreeEntryChanges.Where(kv => kv.Key.EndsWith(je.Name)).Single().Value;
		}
	}
}
