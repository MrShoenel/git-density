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
	internal class Similarity
	{
		private Lazy<TextBlock> lazyOldTextBlock;
		public TextBlock OldTextBlock { get { return this.lazyOldTextBlock.Value; } }

		private Lazy<TextBlock> lazyNewTextBlock;
		public TextBlock NewTextBlock { get { return this.lazyNewTextBlock.Value; } }

		private Hunk hunk;

		private IEnumerable<ClonesXmlSet> cloneSets;

		private IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures;

		public Similarity(Hunk hunk, IEnumerable<ClonesXmlSet> cloneSets, IDictionary<PropertyInfo, INormalizedStringDistance> similarityMeasures)
		{
			this.lazyOldTextBlock = new Lazy<TextBlock>(() =>
				new TextBlock(hunk, TextBlockType.Old));
			this.lazyNewTextBlock = new Lazy<TextBlock>(() =>
				new TextBlock(hunk, TextBlockType.New));

			this.hunk = hunk;
			this.cloneSets = cloneSets;
			this.similarityMeasures = similarityMeasures;
		}

		private IDictionary<SimilarityComparisonType, ICollection<T>> Bla<T>() where T : new()
		{
			var dict = new Dictionary<SimilarityComparisonType, ICollection<T>>
			{
				[SimilarityComparisonType.BlockSimilarity] = new List<T>(),
				[SimilarityComparisonType.ClonedBlockLinesSimilarity] = new List<T>(),
				[SimilarityComparisonType.PostCloneBlockSimilarity] = new List<T>()
			};

			#region BlockSimilarity
			foreach (var simMeasure in this.similarityMeasures)
			{
				var similarity = 1d - simMeasure.Value.Distance(
					this.OldTextBlock.WholeBlock, this.NewTextBlock.WholeBlock);

				// populate a new instance of T
				var t = new T();
				simMeasure.Key.SetValue(t, similarity);

				dict[SimilarityComparisonType.BlockSimilarity].Add(t);
			}
			#endregion

			#region Clone similarity

			#endregion

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


			}
			throw new NotImplementedException();
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
