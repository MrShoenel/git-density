using F23.StringSimilarity.Interfaces;
using GitDensity.Density;
using GitDensity.Density.CloneDensity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Util.Data.Entities;
using Util.Similarity;

namespace GitDensity.Similarity
{
	internal class Similarity<T> where T : IHasSimilarityComparisonType, new()
	{
		private Lazy<TextBlock> lazyOldTextBlock;
		public TextBlock OldTextBlock { get { return this.lazyOldTextBlock.Value; } }

		private Lazy<TextBlock> lazyNewTextBlock;
		public TextBlock NewTextBlock { get { return this.lazyNewTextBlock.Value; } }

		private Hunk hunk;

		private IEnumerable<ClonesXmlSet> cloneSets;

		private IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures;

		private Lazy<IDictionary<SimilarityComparisonType, ICollection<T>>> lazySims;

		public ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>> Similarities
		{
			get => new ReadOnlyDictionary<SimilarityComparisonType, ICollection<T>>(this.lazySims.Value);
		}

		private Lazy<TextBlockHelper> lazyClonesBlocks;

		public UInt32 NumberOfLinesAddedPostCloneDetection
			=> this.NewTextBlock.LinesAdded - this.lazyClonesBlocks.Value.NewPostClone.LinesAdded;

		public UInt32 NumberOfLinesDeletedPostCloneDetection
			=> this.NewTextBlock.LinesDeleted - this.lazyClonesBlocks.Value.NewPostClone.LinesDeleted;

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
		}

		private IDictionary<SimilarityComparisonType, ICollection<T>> ComputeSimilarities()
		{
			var dict = new Dictionary<SimilarityComparisonType, ICollection<T>>
			{
				[SimilarityComparisonType.BlockSimilarity] = new List<T>(),
				[SimilarityComparisonType.ClonedBlockLinesSimilarity] = new List<T>(),
				[SimilarityComparisonType.PostCloneBlockSimilarity] = new List<T>()
			};

			Action<TextBlock, TextBlock, IDictionary<SimilarityComparisonType, ICollection<T>>, SimilarityComparisonType> computeSim =
				(oldBlock, newBlock, dictionary, compType) =>
			{
				// populate a new instance of T
				var t = new T { ComparisonType = compType };

				Parallel.ForEach(this.similarityMeasures,
#if DEBUG
					new ParallelOptions { MaxDegreeOfParallelism = 1 },
#endif
					simMeasure =>
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

					simMeasure.Key.SetValue(t, similarity);
				});

				dictionary[compType].Add(t);
			};

			// Compute the TextBlocks related to cloned lines
			foreach (var simMeasure in this.similarityMeasures)
			{
				#region BlockSimilarity
				computeSim(this.OldTextBlock, this.NewTextBlock,
					dict, SimilarityComparisonType.BlockSimilarity);
				#endregion

				#region Clone similarity
				computeSim(this.lazyClonesBlocks.Value.OldPostClone, this.lazyClonesBlocks.Value.NewPostClone,
					dict, SimilarityComparisonType.PostCloneBlockSimilarity);

				computeSim(this.lazyClonesBlocks.Value.OldBlockClonedLines, this.lazyClonesBlocks.Value.NewBlockClonedLines,
					dict, SimilarityComparisonType.ClonedBlockLinesSimilarity);
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
	}
}
