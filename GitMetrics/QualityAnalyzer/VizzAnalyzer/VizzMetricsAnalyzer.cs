/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using System.IO;

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
				FileName = Path.GetFullPath((String)conf["pathToBinary"]),
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
					this.logger.LogWarning($"The metrics extraction failed at commit {this.Commit.ShaShort()} ({result.CommitMetricsStatus}).");
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

		/// <summary>
		/// From the metrics analysis, we get a JSON-formatted output that contains metrics
		/// for entire project and for each file. This method tries to match each so-called
		/// OutputEntity to a change in the file-tree. Note that this cannot always work, as
		/// the metrics analysis analyzes every while, while not every file necessarily has
		/// to change and thus no <see cref="MetricEntity"/> is returned in that case.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		protected IEnumerable<MetricEntity> GetMetricsFromJson(JsonOutput output)
		{
			this.PrepareTreeEntryChanges();

			foreach (var entity in output.Entities)
			{
				TreeEntryChangesEntity tece = null;
				if (entity.Type == OutputEntityType.File &&
					!this.TryGetTreeEntryChangeForMetricsEntity(out tece, entity))
				{
					continue;
					// It is a file but cannot be matched to a TreeEntryChange,
					// Probably because it wasn't actually changed.
				}

				foreach (var value in entity.MetricsValues)
				{
					yield return new MetricEntity
					{
						Commit = this.CommitEntity,
						MetricType = this.MetricTypes[value.Key],
						MetricValue = value.Value,
						OutputEntityType = entity.Type,
						TreeEntryChange = tece
					};
				}
			}
		}

		/// <summary>
		/// Clears the internal dictionary with fully-qualified Java-name tree-changes and
		/// fills it by normalizing paths with a dot (to match the Java package name).
		/// </summary>
		protected void PrepareTreeEntryChanges()
		{
			this.FQJNTreeEntryChanges.Clear();

			foreach (var tec in this.RepositoryEntity.TreeEntryContributions.Where(tec => tec.Commit.BaseObject == this.Commit).Select(tec => tec.TreeEntryChanges))
			{
				// We're always interested in the file as it's now, so use PathNew:
				var path = tec.PathNew;
				// path may look like:
				// 'src\main\java\de\alaoli\games\minecraft\mods\limitedresources\Config.java'
				// .. and we need to transform it into a fully-qualified class-name, like:
				// 'src.main.java.de.alaoli.games.minecraft.mods.limitedresources.Config'
				// .. so that .EndsWith() works.
				var fqjn = RegexPathToFQJN.Replace(path, ".");
				fqjn = RegexMatchFileType.Replace(fqjn, String.Empty);

				this.FQJNTreeEntryChanges[fqjn] = tec;
			}
		}

		/// <summary>
		/// This method attempts to match the metrics gathered for one file to an actual
		/// change in the git-tree and returns true only on success.
		/// </summary>
		/// <param name="tece"></param>
		/// <param name="je"></param>
		/// <returns></returns>
		protected bool TryGetTreeEntryChangeForMetricsEntity(out TreeEntryChangesEntity tece, JsonEntity je)
		{
			tece = null;
			if (this.FQJNTreeEntryChanges.Count == 0)
			{
				return false;
			}

			// This method may only be used with files.
			if (je.Type != OutputEntityType.File)
			{
				throw new Exception($"This method may only be used with type {OutputEntityType.File}.");
			}

			// VizzAnalyzer special case: Classes that do not declare their package, end up
			// in a virtual package called default.
			if (je.Name.StartsWith("default."))
			{
				// If we get here, try to select exactly one class. Stop if fails.
				var clazzName = je.Name.Substring("default.".Length);
				var defaultList = this.FQJNTreeEntryChanges.Where(kv => kv.Key.EndsWith(clazzName)).ToList();

				if (defaultList.Count == 1)
				{
					tece = defaultList[0].Value;
					return true;
				}

				return false;
			}

			// This is a list of size=1, if that exact file was indeed changed and thus
			// has a corresponding TreeEntryChange. If the size=0, then the file was not
			// changed. If size>1, then we have encountered an ambiguity.
			var temp = this.FQJNTreeEntryChanges.Where(kv => kv.Key.EndsWith(je.Name)).ToList();

			if (temp.Count == 0)
			{
				return false;
			}
			if (temp.Count == 1)
			{
				tece = temp[0].Value;
				return true;
			}

			throw new Exception($"{je.Name} matched more than one TreeEntryChange!");
		}
	}
}
