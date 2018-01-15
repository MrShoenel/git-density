using F23.StringSimilarity.Interfaces;
using GitDensity.Data.Entities;
using GitDensity.Density;
using GitDensity.Density.CloneDensity;
using GitDensity.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Similarity
{
	internal class Similarity<T> where T : new()
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
		}

		private IDictionary<SimilarityComparisonType, ICollection<T>> ComputeSimilarities()
		{
			var dict = new Dictionary<SimilarityComparisonType, ICollection<T>>
			{
				[SimilarityComparisonType.BlockSimilarity] = new List<T>(),
				[SimilarityComparisonType.ClonedBlockLinesSimilarity] = new List<T>(),
				[SimilarityComparisonType.PostCloneBlockSimilarity] = new List<T>()
			};

			Action<TextBlock, TextBlock, ICollection<T>> computeSim = (oldBlock, newBlock, coll) =>
			{
				// populate a new instance of T
				var t = new T();

				foreach (var simMeasure in this.similarityMeasures)
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
							oldBlock.WholeBlock, newBlock.WholeBlock);
					}

					simMeasure.Key.SetValue(t, similarity);
				}

				coll.Add(t);
			};

			foreach (var simMeasure in this.similarityMeasures)
			{
				#region BlockSimilarity
				computeSim(this.OldTextBlock, this.NewTextBlock,
					dict[SimilarityComparisonType.BlockSimilarity]);
				#endregion

				#region Clone similarity
				var tbh = this.ToPostCloneBlockAndClonedLinesBlock(this.OldTextBlock, this.NewTextBlock);
				computeSim(tbh.OldPostClone, tbh.NewPostClone,
					dict[SimilarityComparisonType.PostCloneBlockSimilarity]);

				computeSim(tbh.OldBlockClonedLines, tbh.NewBlockClonedLines,
					dict[SimilarityComparisonType.ClonedBlockLinesSimilarity]);
				#endregion
			}

			return dict;
		}

		private TextBlockHelper ToPostCloneBlockAndClonedLinesBlock(TextBlock oldBlock, TextBlock newBlock)
		{
			var tbh = new TextBlockHelper();

			foreach (var set in this.cloneSets)
			{
				var oldSetBlock = set.Blocks
					.Where(b => b.Source.EndsWith(this.hunk.SourceFilePath)).First();
				var newSetBlock = set.Blocks
					.Where(b => b.Source.EndsWith(this.hunk.TargetFilePath)).First();

				// Fill up the old post-clone block
				tbh.OldPostClone.AddLines(oldBlock.LinesWithNumber.Select(kv => kv.Value));

				foreach (var lineNumber in oldSetBlock.LineNumbers)
				{
					if (tbh.OldPostClone.HasLineNumber(lineNumber))
					{
						tbh.OldBlockClonedLines.AddLine(tbh.OldPostClone.RemoveLine(lineNumber));
					}
				}

				// Fill up the new post-clone block
				tbh.NewPostClone.AddLines(newBlock.LinesWithNumber.Select(kv => kv.Value));

				foreach (var lineNumber in newSetBlock.LineNumbers)
				{
					if (tbh.NewPostClone.HasLineNumber(lineNumber))
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
			public TextBlock OldPostClone { get; private set; }

			public TextBlock NewPostClone { get; private set; }
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
