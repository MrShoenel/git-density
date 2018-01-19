using F23.StringSimilarity.Interfaces;
using GitDensity.Density;
using GitDensity.Density.CloneDensity;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Util;
using Util.Data.Entities;
using Util.Metrics;
using Util.Similarity;

namespace GitDensity.Similarity
{
	internal class Similarity<T> : ISupportsExecutionPolicy
		where T : IHasSimilarityComparisonType, new()
	{
		private Lazy<TextBlock> lazyOldTextBlock;
		public TextBlock OldTextBlock { get { return this.lazyOldTextBlock.Value; } }

		private Lazy<TextBlock> lazyNewTextBlock;
		public TextBlock NewTextBlock { get { return this.lazyNewTextBlock.Value; } }

		#region Blocks without comments and empty lines
		private Lazy<TextBlock> lazyOldTextBlockNoComments;
		/// <summary>
		/// Returns the old <see cref="TextBlock"/> without comments or empty lines.
		/// </summary>
		public TextBlock OldTextBlockNoComments => this.lazyOldTextBlockNoComments.Value;

		private Lazy<TextBlock> lazyNewTextBlockNoComments;
		/// <summary>
		/// Returns the new <see cref="TextBlock"/> without comments or empty lines.
		/// </summary>
		public TextBlock NewTextBlockNoComments => this.lazyNewTextBlockNoComments.Value;

		#region clones
		private Lazy<TextBlockHelper> lazyClonesBlockNoComments;

		public UInt32 NumberOfLinesAddedPostCloneDetectionNoComments
			=> this.NewTextBlockNoComments.LinesAdded - this.lazyClonesBlockNoComments.Value.NewPostClone.LinesAdded;

		public UInt32 NumberOfLinesDeletedPostCloneDetectionNoComments
			=> this.NewTextBlockNoComments.LinesDeleted - this.lazyClonesBlockNoComments.Value.NewPostClone.LinesDeleted;
		#endregion

		private Lazy<IDictionary<SimilarityComparisonType, ICollection<T>>> lazySimsNoComments;

		public ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>> SimilaritiesNoComments
			=> new ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>>(this.lazySimsNoComments.Value);
		#endregion

		private Hunk hunk;

		private IEnumerable<ClonesXmlSet> cloneSets;

		private IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures;

		private Lazy<IDictionary<SimilarityComparisonType, ICollection<T>>> lazySims;

		public ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>> Similarities
			=> new ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>>(this.lazySims.Value);

		private Lazy<TextBlockHelper> lazyClonesBlocks;

		public UInt32 NumberOfLinesAddedPostCloneDetection
			=> this.NewTextBlock.LinesAdded - this.lazyClonesBlocks.Value.NewPostClone.LinesAdded;

		public UInt32 NumberOfLinesDeletedPostCloneDetection
			=> this.NewTextBlock.LinesDeleted - this.lazyClonesBlocks.Value.NewPostClone.LinesDeleted;

		public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;

		public Similarity(Hunk hunk, IEnumerable<ClonesXmlSet> cloneSets, IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures)
		{
			this.lazyOldTextBlock = new Lazy<TextBlock>(() =>
				new TextBlock(hunk, TextBlockType.Old));
			this.lazyNewTextBlock = new Lazy<TextBlock>(() =>
				new TextBlock(hunk, TextBlockType.New));

			this.hunk = hunk;
			this.cloneSets = cloneSets;
			this.similarityMeasures = similarityMeasures;
			this.lazySims = new Lazy<IDictionary<SimilarityComparisonType, ICollection<T>>>(() =>
			{
				return this.ComputeSimilarities();
			});

			this.lazyClonesBlocks = new Lazy<TextBlockHelper>(() =>
			{
				return this.ToPostCloneBlockAndClonedLinesBlock(this.OldTextBlock, this.NewTextBlock);
			});

			#region Blocks without comments and empty lines
			this.lazyOldTextBlockNoComments = new Lazy<TextBlock>(() =>
				(this.OldTextBlock.Clone() as TextBlock).RemoveEmptyLinesAndComments());
			this.lazyNewTextBlockNoComments = new Lazy<TextBlock>(() =>
				(this.NewTextBlock.Clone() as TextBlock).RemoveEmptyLinesAndComments());

			this.lazySimsNoComments = new Lazy<IDictionary<SimilarityComparisonType, ICollection<T>>>(() =>
			{
				return this.ComputeSimilaritiesNoComments();
			});

			this.lazyClonesBlockNoComments = new Lazy<TextBlockHelper>(() =>
			{
				return this.ToPostCloneBlockAndClonedLinesBlock(
					this.OldTextBlockNoComments, this.NewTextBlockNoComments);
			});
			#endregion
		}

		private static void ComputeBlockSimilarity(TextBlock oldBlock, TextBlock newBlock, SimilarityComparisonType compType, IDictionary<SimilarityComparisonType, ICollection<T>> dictionary, IDictionary<PropertyInfo, INormalizedStringDistance> simMeasures, ExecutionPolicy execPolicy)
		{
			// populate a new instance of T
			var simEntity = new T
			{
				ComparisonType = compType,
				LinesAdded = newBlock.LinesAdded,
				LinesDeleted = oldBlock.LinesDeleted
			};

			var parallelOptions = new ParallelOptions();
			if (execPolicy == ExecutionPolicy.Linear)
			{
				parallelOptions.MaxDegreeOfParallelism = 1;
			}

			Parallel.ForEach(simMeasures, parallelOptions, simMeasure =>
			{
				Double similarity;
				if (oldBlock.IsEmpty && newBlock.IsEmpty) // Empty & Same; similarity is 100%
				{
					similarity = 1d;
				}
				else if (oldBlock.IsEmpty ^ newBlock.IsEmpty) // One empty, one not: sim is 0%
				{
					similarity = 0d;
				}
				else if (oldBlock.Equals(newBlock)) // A more expensive check
				{
					similarity = 1d;
				}
				else
				{
					// Compute: Both non-empty:
					similarity = 1d - simMeasure.Value.Distance(
						oldBlock.WholeBlockWithoutUntouched, newBlock.WholeBlockWithoutUntouched);
				}

				simMeasure.Key.SetValue(simEntity, Double.IsNaN(similarity) ? 0d : similarity);
			});

			dictionary[compType].Add(simEntity);
		}

		private IDictionary<SimilarityComparisonType, ICollection<T>> ComputeSimilaritiesNoComments()
		{
			var dict = new Dictionary<SimilarityComparisonType, ICollection<T>>
			{
				[SimilarityComparisonType.BlockSimilarityNoComments] = new List<T>(),
				[SimilarityComparisonType.ClonedBlockLinesSimilarityNoComments] = new List<T>(),
				[SimilarityComparisonType.PostCloneBlockSimilarityNoComments] = new List<T>()
			};

			// Compute the TextBlocks related to cloned lines
			foreach (var simMeasure in this.similarityMeasures)
			{
				#region BlockSimilarity
				ComputeBlockSimilarity(this.OldTextBlockNoComments, this.NewTextBlockNoComments, SimilarityComparisonType.BlockSimilarityNoComments, dict, this.similarityMeasures, this.ExecutionPolicy);
				#endregion

				#region Clone similarity
				ComputeBlockSimilarity(
					this.lazyClonesBlockNoComments.Value.OldPostClone,
					this.lazyClonesBlockNoComments.Value.NewPostClone,
					SimilarityComparisonType.PostCloneBlockSimilarityNoComments,
					dict, this.similarityMeasures, this.ExecutionPolicy);

				ComputeBlockSimilarity(
					this.lazyClonesBlockNoComments.Value.OldBlockClonedLines,
					this.lazyClonesBlockNoComments.Value.NewBlockClonedLines,
					SimilarityComparisonType.ClonedBlockLinesSimilarityNoComments,
					dict, this.similarityMeasures, this.ExecutionPolicy);
				#endregion
			}

			return dict;
		}

		private IDictionary<SimilarityComparisonType, ICollection<T>> ComputeSimilarities()
		{
			var dict = new Dictionary<SimilarityComparisonType, ICollection<T>>
			{
				[SimilarityComparisonType.BlockSimilarity] = new List<T>(),
				[SimilarityComparisonType.ClonedBlockLinesSimilarity] = new List<T>(),
				[SimilarityComparisonType.PostCloneBlockSimilarity] = new List<T>()
			};

			// Compute the TextBlocks related to cloned lines
			foreach (var simMeasure in this.similarityMeasures)
			{
				#region BlockSimilarity
				ComputeBlockSimilarity(this.OldTextBlock, this.NewTextBlock, SimilarityComparisonType.BlockSimilarity, dict, this.similarityMeasures, this.ExecutionPolicy);
				#endregion

				#region Clone similarity
				ComputeBlockSimilarity(
					this.lazyClonesBlocks.Value.OldPostClone,
					this.lazyClonesBlocks.Value.NewPostClone,
					SimilarityComparisonType.PostCloneBlockSimilarity,
					dict, this.similarityMeasures, this.ExecutionPolicy);

				ComputeBlockSimilarity(
					this.lazyClonesBlocks.Value.OldBlockClonedLines,
					this.lazyClonesBlocks.Value.NewBlockClonedLines,
					SimilarityComparisonType.ClonedBlockLinesSimilarity,
					dict, this.similarityMeasures, this.ExecutionPolicy);
				#endregion
			}

			return dict;
		}

		private TextBlockHelper ToPostCloneBlockAndClonedLinesBlock(TextBlock oldBlock, TextBlock newBlock)
		{
			var tbh = new TextBlockHelper
			{
				OldPostClone = oldBlock.Clone() as TextBlock,
				NewPostClone = newBlock.Clone() as TextBlock
			};

			var oic = StringComparison.OrdinalIgnoreCase;

			foreach (var set in this.cloneSets)
			{
				var oldSetBlock = set.Blocks
					.Where(b => b.Source.EndsWith(this.hunk.SourceFilePath, oic)).First();
				var newSetBlock = set.Blocks
					.Where(b => b.Source.EndsWith(this.hunk.TargetFilePath, oic)).First();

				foreach (var lineNumber in oldSetBlock.LineNumbers)
				{
					if (tbh.OldPostClone.HasLineNumber(lineNumber) && !tbh.OldBlockClonedLines.HasLineNumber(lineNumber))
					{
						tbh.OldBlockClonedLines.AddLine(tbh.OldPostClone.RemoveLine(lineNumber));
					}
				}

				foreach (var lineNumber in newSetBlock.LineNumbers)
				{
					if (tbh.NewPostClone.HasLineNumber(lineNumber) && !tbh.NewBlockClonedLines.HasLineNumber(lineNumber))
					{
						tbh.NewBlockClonedLines.AddLine(tbh.NewPostClone.RemoveLine(lineNumber));
					}
				}
			}

			return tbh;
		}

		private class TextBlockHelper
		{
			#region SimilarityComparisonType.PostCloneBlockSimilarity
			public TextBlock OldPostClone { get; set; }

			public TextBlock NewPostClone { get; set; }
			#endregion

			#region SimilarityComparisonType.ClonedBlockLinesSimilarity
			public TextBlock OldBlockClonedLines { get; private set; }

			public TextBlock NewBlockClonedLines { get; private set; }
			#endregion

			public TextBlockHelper()
			{
				this.OldPostClone = new TextBlock();
				this.NewPostClone = new TextBlock();
				this.OldBlockClonedLines = new TextBlock();
				this.NewBlockClonedLines = new TextBlock();
			}
		}

		#region Aggregate Similarities to Metrics
		public static TreeEntryChangesMetricsEntity AggregateToMetrics(FileBlockEntity fileBlock, Similarity<T> similarity, SimpleLoc simpleLoc, TreeEntryChangesEntity treeEntryChanges, Boolean addToTreeEntryChanges = true)
		{
			return AggregateToMetrics(new[] { Tuple.Create(fileBlock, similarity) }, simpleLoc, treeEntryChanges, addToTreeEntryChanges);
		}

		public static TreeEntryChangesMetricsEntity AggregateToMetrics(IEnumerable<Tuple<FileBlockEntity, Similarity<T>>> fileBlocks, SimpleLoc simpleLoc, TreeEntryChangesEntity treeEntryChanges, Boolean addToTreeEntryChanges = true)
		{
			var metrics = new TreeEntryChangesMetricsEntity
			{
				TreeEntryChanges = addToTreeEntryChanges ? treeEntryChanges : null,
				LocFileGross = (treeEntryChanges.Status == ChangeKind.Deleted ? -1 : 1)
					* (Int32)simpleLoc.LocGross,
				LocFileNoComments = (treeEntryChanges.Status == ChangeKind.Deleted ? -1 : 1)
					* (Int32)simpleLoc.LocNoComments,

				NumAdded = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.NewTextBlock.LinesAdded).Sum(),
				NumDeleted = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.OldTextBlock.LinesDeleted).Sum(),
				NumAddedNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.NewTextBlockNoComments.LinesAdded).Sum(),
				NumDeletedNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.OldTextBlockNoComments.LinesDeleted).Sum(),

				NumAddedPostCloneDetection = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlocks.Value.NewPostClone.LinesAdded).Sum(),
				NumDeletedPostCloneDetection = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlocks.Value.OldPostClone.LinesDeleted).Sum(),
				NumAddedPostCloneDetectionNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlockNoComments.Value.NewPostClone.LinesAdded).Sum(),
				NumDeletedPostCloneDetectionNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlockNoComments.Value.OldPostClone.LinesDeleted).Sum(),

				NumAddedClonedBlockLines = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlocks.Value.NewBlockClonedLines.LinesAdded).Sum(),
				NumDeletedClonedBlockLines = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlocks.Value.OldBlockClonedLines.LinesDeleted).Sum(),
				NumAddedClonedBlockLinesNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlockNoComments.Value.NewBlockClonedLines.LinesAdded).Sum(),
				NumDeletedClonedBlockLinesNoComments = (UInt32)fileBlocks.Select(fb =>
					(Int32)fb.Item2.lazyClonesBlockNoComments.Value.OldBlockClonedLines.LinesDeleted).Sum()
			};

			return metrics;
		}
		#endregion
	}
}
