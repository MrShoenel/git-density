using F23.StringSimilarity.Interfaces;
using GitDensity.Data.Entities;
using GitDensity.Util;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GitDensity.Density
{
	internal class GitDensity : IDisposable
	{
		private static BaseLogger<GitDensity> logger = Program.CreateLogger<GitDensity>();

		public static readonly String[] DefaultFileTypeExtensions =
			new [] { "js", "ts", "java", "cs", "php", "phtml", "php3", "php4", "php5", "xml" };

		public Repository Repository { get; protected internal set; }

		public ProgrammingLanguage[] ProgrammingLanguages { get; protected internal set; }

		public Boolean SkipInitialCommit { get; protected internal set; } = false;

		public Boolean SkipMergeCommits { get; protected internal set; } = true;

		public ICollection<String> FileTypeExtensions { get; protected internal set; } = GitDensity.DefaultFileTypeExtensions;

		public DirectoryInfo TempDirectory { get; protected internal set; }

		protected IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures;

		private ReadOnlyDictionary<PropertyInfo, INormalizedStringDistance> roSimMeasures;
		public ReadOnlyDictionary<PropertyInfo, INormalizedStringDistance> SimilarityMeasures
		{
			get => this.roSimMeasures;
		}

		public GitDensity(Repository repository, IEnumerable<ProgrammingLanguage> languages, Boolean? skipInitialCommit = null, Boolean? skipMergeCommits = null, IEnumerable<String> fileTypeExtensions = null, String tempPath = null)
		{
			this.similarityMeasures = new Dictionary<PropertyInfo, INormalizedStringDistance>();
			this.roSimMeasures = new ReadOnlyDictionary<PropertyInfo, INormalizedStringDistance>(this.similarityMeasures);
			this.Repository = repository;
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
		}

		/// <summary>
		/// Initializes the to-compute similarity measures that a certain type or entity
		/// uses, annotated by the <see cref="SimilarityTypeAttribute"/>.
		/// </summary>
		/// <param name="fromType"></param>
		/// <returns>This (<see cref="GitDensity"/>) for chaining.</returns>
		public GitDensity InitializeStringSimilarityMeasures(Type fromType)
		{
			this.similarityMeasures.Clear();

			var properties = fromType.GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance)
				.Select(prop => new
				{
					Prop = prop,
					Attr = prop.GetCustomAttributes(typeof(SimilarityTypeAttribute), true).Cast<SimilarityTypeAttribute>().FirstOrDefault()
				})
				.Where(prop => prop.Attr is SimilarityTypeAttribute);
			
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

				this.similarityMeasures[property.Prop] = (INormalizedStringDistance)
					Activator.CreateInstance(property.Attr.Type, args);
			}

			logger.LogInformation("Initialized {0} normalized string similarity/distance measures: {1}",
				properties.Count(), String.Join(", ", properties.Select(pi => pi.Attr.ToString())));

			return this;
		}

		public GitDensityAnalysisResult Analyze()
		{
			if (this.similarityMeasures.Count == 0)
			{
				logger.LogWarning($"No similarity-measures have been initialized. This can be done by calling {nameof(InitializeStringSimilarityMeasures)}(Type fromType).");
			}

			this.TempDirectory.Clear();

			var developers = this.Repository.Commits.GroupByDeveloperAsSignatures();
			var repoEntity = this.Repository.AsEntity().AddDevelopers(
				new HashSet<DeveloperEntity>(developers.Values));
			var commits = this.Repository.Commits.Select(commit => {
				return commit.AsEntity(repoEntity, developers[commit.Author]);
			}).ToDictionary(commit => commit.HashSHA1, commit => commit);
			var pairs = this.Repository.CommitPairs(this.SkipInitialCommit, this.SkipMergeCommits);

			Parallel.ForEach(pairs,
#if DEBUG
				new ParallelOptions { MaxDegreeOfParallelism = 1 },
#endif
				pair =>
			{
				var pairEntity = pair.AsEntity(repoEntity, commits[pair.Child.Sha],
					pair.Parent is Commit ? commits[pair.Parent.Sha] : null); // handle initial commits

				// Now get all TreeChanges with Status Added, Modified, Deleted or Moved.
				var relevantTreeChanges = pair.TreeChanges.Where(tc =>
				{
					return tc.Status == ChangeKind.Added || tc.Status == ChangeKind.Modified
					|| tc.Status == ChangeKind.Deleted || tc.Status == ChangeKind.Renamed;
				});

				// Now for each of the desired file types, get the Hunks and Diffs
				relevantTreeChanges = relevantTreeChanges.Where(rtc =>
				{
					return this.FileTypeExtensions.Any(extension => rtc.Path.EndsWith(extension))
					|| this.FileTypeExtensions.Any(extension => rtc.OldPath.EndsWith(extension));
				});

				var dirOld = "old";
				var dirNew = "new";
				var pairDirectory = new DirectoryInfo(Path.Combine(this.TempDirectory.FullName, pair.Id));
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
					var treeChangeEntity = change.AsEntity(pairEntity);

					var patchNew = pair.Patch[change.Path];

					var fileBlock = new FileBlockEntity
					{
						CommitPair = pairEntity,
						FileBlockType = change.Status == ChangeKind.Added ? FileBlockType.Added : FileBlockType.Deleted,
						TreeEntryChanges = treeChangeEntity,

						NewAmount = change.Status == ChangeKind.Added ? (uint)patchNew.LinesAdded : 0u,
						NewStart = change.Status == ChangeKind.Added ? 1u : 0u,
						OldAmount = change.Status == ChangeKind.Added ? 0u : (uint)patchNew.LinesDeleted,
						OldStart = change.Status == ChangeKind.Added ? 0u : 1u,

						NumAdded = change.Status == ChangeKind.Added ? (uint)patchNew.LinesAdded : 0u,
						NumDeleted = change.Status == ChangeKind.Added ? 0u : (uint)patchNew.LinesDeleted,

						NumAddedPostCloneDetection = 0u,
						NumDeletedPostCloneDetection = 0u
					};

					treeChangeEntity.AddFileBlock(fileBlock);
					pairEntity.AddFileBlock(fileBlock);
					pairEntity.AddTreeEntryChanges(treeChangeEntity);
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
					var oldDirectory = new DirectoryInfo(Path.Combine(pairDirectory.FullName, dirOld));
					var newDirectory = new DirectoryInfo(Path.Combine(pairDirectory.FullName, dirNew));
					var hunks = Hunk.HunksForPatch(patchNew, oldDirectory, newDirectory);


					var fileBlocks = hunks.Select(hunk =>
					{
						var similarity = new Similarity.Similarity<SimilarityEntity>(
							hunk, relevantSets, this.SimilarityMeasures);

						return new FileBlockEntity
						{
							CommitPair = pairEntity,
							FileBlockType = FileBlockType.Modified,
							TreeEntryChanges = treeChangeEntity,

							NewAmount = hunk.NewNumberOfLines,
							NewStart = hunk.NewLineStart,
							OldAmount = hunk.OldNumberOfLines,
							OldStart = hunk.OldNumberOfLines,

							NumAdded = hunk.NumberOfLinesAdded,
							NumDeleted = hunk.NumberOfLinesDeleted,

							NumAddedPostCloneDetection = similarity.NumberOfLinesAddedPostCloneDetection,
							NumDeletedPostCloneDetection = similarity.NumberOfLinesDeletedPostCloneDetection
							// Regard the next line which adds all similarities to the FileBlock:
						}.AddSimilarities(similarity.Similarities.SelectMany(kv => kv.Value));
					});

					treeChangeEntity.AddFileBlocks(fileBlocks);
					pairEntity.AddFileBlocks(fileBlocks);
					pairEntity.AddTreeEntryChanges(treeChangeEntity);
				}
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
