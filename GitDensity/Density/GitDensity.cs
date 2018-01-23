using F23.StringSimilarity.Interfaces;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Util;
using Util.Data.Entities;
using Util.Extensions;
using Util.Logging;
using Util.Metrics;
using Util.Similarity;

namespace GitDensity.Density
{
	/// <summary>
	/// This class does the actual Git-Density analysis for a whole repository.
	/// </summary>
	internal class GitDensity : IDisposable, ISupportsExecutionPolicy
	{
		private static BaseLogger<GitDensity> logger = Program.CreateLogger<GitDensity>();

		public static readonly String[] DefaultFileTypeExtensions =
			new [] { "js", "ts", "java", "cs", "php", "phtml", "php3", "php4", "php5", "xml" };

		public GitHoursSpan GitHoursSpan { get; protected internal set; }

		public Repository Repository { get; protected internal set; }

		public ProgrammingLanguage[] ProgrammingLanguages { get; protected internal set; }

		public Boolean SkipInitialCommit { get; protected internal set; } = false;

		public Boolean SkipMergeCommits { get; protected internal set; } = true;

		public ICollection<String> FileTypeExtensions { get; protected internal set; } = GitDensity.DefaultFileTypeExtensions;

		public DirectoryInfo TempDirectory { get; protected internal set; }

		protected IDictionary<SimilarityMeasurementType, Tuple<PropertyInfo, INormalizedStringDistance>> similarityMeasures;

		private ReadOnlyDictionary<SimilarityMeasurementType, Tuple<PropertyInfo, INormalizedStringDistance>> roSimMeasures;
		public ReadOnlyDictionary<SimilarityMeasurementType, Tuple<PropertyInfo, INormalizedStringDistance>> SimilarityMeasures => this.roSimMeasures;

		public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;

		/// <summary>
		/// Constructs a new Analysis.
		/// </summary>
		/// <param name="gitHoursSpan">Represents the span of commits to be analyzed.</param>
		/// <param name="languages">Programming-languages to analyze.</param>
		/// <param name="skipInitialCommit">Skip the initial commit (the one with no parent)
		/// when conducting the analysis.</param>
		/// <param name="skipMergeCommits">Skip merge-commits during analysis.</param>
		/// <param name="fileTypeExtensions">This list represents files that shall be analyzed.
		/// If null, defaults to <see cref="DefaultFileTypeExtensions"/>.</param>
		/// <param name="tempPath">A temporary path to store intermediate results in.</param>
		public GitDensity(GitHoursSpan gitHoursSpan, IEnumerable<ProgrammingLanguage> languages, Boolean? skipInitialCommit = null, Boolean? skipMergeCommits = null, IEnumerable<String> fileTypeExtensions = null, String tempPath = null)
		{
			this.similarityMeasures = new Dictionary<SimilarityMeasurementType, Tuple<PropertyInfo, INormalizedStringDistance>>();
			this.roSimMeasures = new ReadOnlyDictionary<SimilarityMeasurementType, Tuple<PropertyInfo, INormalizedStringDistance>>(this.similarityMeasures);
			this.GitHoursSpan = gitHoursSpan;
			this.Repository = gitHoursSpan.Repository;
			this.ProgrammingLanguages = languages.ToArray();
			this.TempDirectory = new DirectoryInfo(tempPath ?? Path.GetTempPath());

			if (skipInitialCommit.HasValue)
			{
				this.SkipInitialCommit = skipInitialCommit.Value;
			}
			if (skipMergeCommits.HasValue)
			{
				this.SkipMergeCommits = skipMergeCommits.Value;
			}
			if (fileTypeExtensions is IEnumerable<String> && fileTypeExtensions.Any())
			{
				this.FileTypeExtensions = fileTypeExtensions.ToList(); // Clone the IEnumerable
			}

			logger.LogInformation("Analyzing {0} commits, from {1} to {2} (inclusive)",
				gitHoursSpan.FilteredCommits.Count, gitHoursSpan.SinceAsString, gitHoursSpan.UntilAsString);
		}

		/// <summary>
		/// Initializes the to-compute similarity measures that a certain type or entity
		/// uses, annotated by the <see cref="SimilarityTypeAttribute"/>.
		/// </summary>
		/// <param name="fromType"></param>
		/// <param name="similarityMeasurementTypes">If null, will use the keys from <see cref="SimilarityEntity.SmtToPropertyInfo"/>, which is just all implemented measurements by the <see cref="SimilarityEntity"/>.</param>
		/// <returns>This (<see cref="GitDensity"/>) for chaining.</returns>
		public GitDensity InitializeStringSimilarityMeasures(Type fromType, ISet<SimilarityMeasurementType> similarityMeasurementTypes = null)
		{
			this.similarityMeasures.Clear();
			similarityMeasurementTypes = similarityMeasurementTypes == null || similarityMeasurementTypes.Count == 0 ? null : similarityMeasurementTypes;

			var properties = fromType.GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance)
				.Select(prop => new
				{
					Prop = prop,
					Attr = prop.GetCustomAttributes(typeof(SimilarityTypeAttribute), true).Cast<SimilarityTypeAttribute>().FirstOrDefault()
				})
				.Where(prop => prop.Attr is SimilarityTypeAttribute)
				.Where(anon =>
					similarityMeasurementTypes == null || similarityMeasurementTypes.Contains(anon.Attr.TypeEnum));
			
			var emptyArgs = new Object[0];
			var shingleArg = new Object[1];
			Object[] args;

			foreach (var property in properties)
			{
				if (property.Attr.UsesShingles)
				{
					shingleArg[0] = property.Attr.Shingles;
					args = shingleArg;
				}
				else
				{
					args = emptyArgs;
				}

				this.similarityMeasures[property.Attr.TypeEnum] =
					Tuple.Create(property.Prop, (INormalizedStringDistance)
					Activator.CreateInstance(property.Attr.Type, args));
			}

			logger.LogInformation("Initialized {0} normalized string similarity/distance measures: {1}",
				properties.Count(), String.Join(", ", properties.Select(pi => pi.Attr.ToString())));

			return this;
		}

		/// <summary>
		/// Run the entire analysis and return the result, that can be persisted or futher
		/// analyzed.
		/// </summary>
		/// <returns><see cref="GitDensityAnalysisResult"/> that holds the analyzed
		/// <see cref="LibGit2Sharp.Repository"/> as <see cref="RepositoryEntity"/>.</returns>
		public GitDensityAnalysisResult Analyze()
		{
			if (this.similarityMeasures.Count == 0)
			{
				logger.LogWarning($"No similarity-measures have been initialized. This can be done by calling {nameof(InitializeStringSimilarityMeasures)}(Type fromType).");
			}

			logger.LogWarning("Parallel Analysis is: {0}ABLED!",
				this.ExecutionPolicy == ExecutionPolicy.Parallel ? "EN" : "DIS");

			this.TempDirectory.Clear();

			var dirOld = "old";
			var dirNew = "new";
			var repoEntity = this.Repository.AsEntity(this.GitHoursSpan);
			var developers = this.GitHoursSpan.FilteredCommits.GroupByDeveloperAsSignatures(repoEntity);
			repoEntity.AddDevelopers(new HashSet<DeveloperEntity>(developers.Values));
			var commits = this.GitHoursSpan.FilteredCommits.Select(commit =>
				commit.AsEntity(repoEntity, developers[commit.Author]))
				.ToDictionary(commit => commit.HashSHA1, commit => commit);
			var pairs = this.GitHoursSpan.CommitPairs(
				this.SkipInitialCommit, this.SkipMergeCommits).ToList();
			var similarities = new ReadOnlyDictionary<PropertyInfo, INormalizedStringDistance>(this.SimilarityMeasures.Select(kv => kv.Value).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2));
			var similaritiesEmpty = new Dictionary<PropertyInfo, INormalizedStringDistance>();
			var gitHoursAnalysesPerDeveloper = new GitHours.Hours.GitHours(GitHoursSpan)
				.Analyze(repoEntity, includeHourSpans: true)
				.AuthorStats.ToDictionary(
					@as => @as.Developer as DeveloperEntity, @as => @as.HourSpans);

			var pairsDone = 0;
			var parallelOptions = new ParallelOptions();
			if (this.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				parallelOptions.MaxDegreeOfParallelism = 1;
			}
			Parallel.ForEach(pairs, parallelOptions, pair =>
			{
				var numDone = Interlocked.Increment(ref pairsDone);
				logger.LogInformation("Analyzing commit-pair {0} of {1}, ID: {2}",
					numDone, pairs.Count, pair.Id);

				pair.ExecutionPolicy = this.ExecutionPolicy;
				var pairEntity = pair.AsEntity(repoEntity, commits[pair.Child.Sha],
					pair.Parent is Commit ? commits[pair.Parent.Sha] : null); // handle initial commits

				#region GitHours
				var developerSpans = gitHoursAnalysesPerDeveloper[developers[pair.Child.Author]];
				var hoursSpan = developerSpans.Where(hs => hs.Until == pair.Child).Single();
				var hoursEntity = new HoursEntity
				{
					InitialCommit = commits[hoursSpan.InitialCommit.Sha],
					CommitSince = hoursSpan.Since == null ? null : commits[hoursSpan.Since.Sha],
					CommitUntil = commits[hoursSpan.Until.Sha],
					Developer = developers[pair.Child.Author],
					Hours = hoursSpan.Hours,
					HoursTotal = developerSpans.Take(1 + developerSpans.IndexOf(hoursSpan))
						.Select(hs => hs.Hours).Sum()
				};
				developers[pair.Child.Author].AddHour(hoursEntity);
				#endregion

				// Now get all TreeChanges with Status Added, Modified, Deleted or Moved.
				var relevantTreeChanges = pair.TreeChanges.Where(tc =>
				{
					return tc.Mode != Mode.GitLink &&
					(tc.Status == ChangeKind.Added || tc.Status == ChangeKind.Modified
						|| tc.Status == ChangeKind.Deleted || tc.Status == ChangeKind.Renamed);
				});

				// Now for each of the desired file types, get the Hunks and Diffs
				relevantTreeChanges = relevantTreeChanges.Where(rtc =>
				{
					return this.FileTypeExtensions.Any(extension => rtc.Path.EndsWith(extension))
					|| this.FileTypeExtensions.Any(extension => rtc.OldPath.EndsWith(extension));
				});

				var pairDirectory = new DirectoryInfo(Path.Combine(this.TempDirectory.FullName, pair.Id));
				var oldDirectory = new DirectoryInfo(Path.Combine(pairDirectory.FullName, dirOld));
				var newDirectory = new DirectoryInfo(Path.Combine(pairDirectory.FullName, dirNew));
				pairDirectory.Create();
				pair.WriteOutTree(
					relevantTreeChanges, pairDirectory, wipeTargetDirectoryBefore: true, parentDirectoryName: dirOld, childDirectoryName: dirNew);


				// Run the clone detection for each pair and each language. We add all sets
				// to the same list, regardless of their language.
				var cloneSets = new List<CloneDensity.ClonesXmlSet>();
				foreach (var language in this.ProgrammingLanguages)
				{
					var cloneWrapper = new CloneDensity.CloneDetectionWrapper(
						pairDirectory, language, this.TempDirectory);
					cloneSets.AddRange(cloneWrapper.PerformCloneDetection()
						// Filter out relevant sets: those that are concerned with two version
						// of the same file (and therefore have exactly two blocks).
						.Where(set => set.Blocks.Length == 2));
				}

				// The following block concerns all pure ADDs and DELETEs (i.e. the file
				// in the patch did not exists previously or was deleted in the more recent
				// patch). That means, for each such file, exactly one FileBlock exists.
				foreach (var change in relevantTreeChanges.Where(change => change.Status == ChangeKind.Added || change.Status == ChangeKind.Deleted))
				{
					var added = change.Status == ChangeKind.Added;
					var patchNew = pair.Patch[change.Path];
					var patchOld = pair.Patch[change.OldPath];
					var treeChangeEntity = change.AsEntity(pairEntity);
					var hunk = Hunk.HunksForPatch(added ? patchNew : patchOld,
						oldDirectory, newDirectory).Single();
					// We explicitly pass an empty enumerable for the clone-sets, as there possibly
					// cannot be any for new or deleted files. The Similarity is needed for computing
					// the TreeEntryChangesMetrics-entity.
					var similarity = new Similarity.Similarity<SimilarityEntity>(
						hunk, Enumerable.Empty<CloneDensity.ClonesXmlSet>(), similaritiesEmpty);
					similarity.ExecutionPolicy = this.ExecutionPolicy;
					var simpleLoc = new SimpleLoc((added ?
						pair.Child[change.Path] : pair.Parent[change.OldPath]).GetLines());

					var fileBlock = new FileBlockEntity
					{
						CommitPair = pairEntity,
						FileBlockType = added ? FileBlockType.Added : FileBlockType.Deleted,
						TreeEntryChanges = treeChangeEntity,

						NewAmount = added ? (uint)patchNew.LinesAdded : 0u,
						NewStart = added ? 1u : 0u,
						OldAmount = added ? 0u : (uint)patchNew.LinesDeleted,
						OldStart = added ? 0u : 1u
					}; // Note that we do not add any similarities here (no sense for pure adds/deletes)

					#region Metrics entity
					var metricsEntity = Similarity.Similarity<SimilarityEntity>.AggregateToMetrics(
						fileBlock, similarity, simpleLoc, treeChangeEntity, SimilarityMeasurementType.None, true);

					// No need to save; entity is added to all other entities.
					TreeEntryContributionEntity.Create(
						repoEntity, developers[pair.Child.Author], hoursEntity, commits[pair.Child.Sha], pairEntity, treeChangeEntity, metricsEntity, SimilarityMeasurementType.None);
					#endregion

					treeChangeEntity.AddFileBlock(fileBlock);
					pairEntity.AddFileBlock(fileBlock);

					hunk.Clear();
					patchNew.Clear();
					patchOld.Clear();
				}


				// The following block concerns all changes that represent modifications to
				// two different versions of the same file. The file may have been renamed
				// or moved as well (a so-called non-pure modification).
				foreach (var change in relevantTreeChanges.Where(change => change.Status == ChangeKind.Modified || change.Status == ChangeKind.Renamed))
				{
					var treeChangeEntity = change.AsEntity(pairEntity);
					var changePathOld = Path.GetFullPath(
						Path.Combine(pairDirectory.FullName, dirOld, change.OldPath));
					var changePathNew = Path.GetFullPath(
						Path.Combine(pairDirectory.FullName, dirNew, change.Path));

					var relevantSets = cloneSets.Where(set =>
					{
						var oic = StringComparison.OrdinalIgnoreCase;
						var clonePathA = Path.GetFullPath(set.Blocks.First().Source);
						var clonePathB = Path.GetFullPath(set.Blocks.Reverse().First().Source);

						if (clonePathA.Equals(changePathOld, oic))
						{
							return clonePathB.Equals(changePathNew, oic);
						}
						else if (clonePathA.Equals(changePathNew, oic))
						{
							return clonePathB.Equals(changePathOld, oic);
						}

						return false;
					});

					var patchNew = pair.Patch[change.Path];
					var hunks = Hunk.HunksForPatch(patchNew, oldDirectory, newDirectory).ToList();
					// We are only interested in LOC regarding the new file, because the LOC
					// of its previous version are covered in the previous CommitPair.
					var simpleLoc = new SimpleLoc(pair.Child[change.Path].GetLines());

					
					var fileBlockTuples = hunks.Select(hunk =>
					{
						var similarity = new Similarity.Similarity<SimilarityEntity>(
							hunk, relevantSets, similarities);
						similarity.ExecutionPolicy = this.ExecutionPolicy;

						var fileBlock = new FileBlockEntity
						{
							CommitPair = pairEntity,
							FileBlockType = FileBlockType.Modified,
							TreeEntryChanges = treeChangeEntity,

							NewAmount = hunk.NewNumberOfLines,
							NewStart = hunk.NewLineStart,
							OldAmount = hunk.OldNumberOfLines,
							OldStart = hunk.OldNumberOfLines
						};

						fileBlock.AddSimilarities(similarity.Similarities.Select(kv => kv.Value));
						fileBlock.AddSimilarities(similarity.SimilaritiesNoComments.Select(kv => kv.Value));

						return Tuple.Create(fileBlock, similarity);
					}).ToList();


					#region Metrics entity
					foreach (var smt in this.SimilarityMeasures.Keys)
					{
						var metricsEntity = Similarity.Similarity<SimilarityEntity>.AggregateToMetrics(
							fileBlockTuples, simpleLoc, treeChangeEntity, smt, addToTreeEntryChanges: true);

						// No need to save; entity is added to all other entities.
						TreeEntryContributionEntity.Create(
							repoEntity, developers[pair.Child.Author], hoursEntity, commits[pair.Child.Sha], pairEntity, treeChangeEntity, metricsEntity, smt);
					}
					#endregion

					treeChangeEntity.AddFileBlocks(fileBlockTuples.Select(tuple => tuple.Item1));
					pairEntity.AddFileBlocks(fileBlockTuples.Select(tuple => tuple.Item1));

					patchNew.Clear();
					hunks.ForEach(hunk => hunk.Clear());
					hunks.Clear();
					fileBlockTuples.Clear();
				}

				cloneSets.Clear();
				
				pair.Dispose(); // also releases the expensive patches.
			});

			return new GitDensityAnalysisResult(repoEntity);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Does the actual clean up.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.TempDirectory.Clear();
					this.GitHoursSpan.Dispose();
				}

				this.disposedValue = true;
			}
		}
		
		/// <summary>
		/// Cleans up temporary directories used during the analysis.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}
}
