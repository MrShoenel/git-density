/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
///
/// ---------------------------------------------------------------------------------
///
using F23.StringSimilarity;
using F23.StringSimilarity.Interfaces;
using FluentNHibernate.Mapping;
using System;
using System.Linq;
using Util.Extensions;
using Util.Similarity;

namespace Util.Data.Entities
{
	/// <summary>
	/// This attribute is intended to simplify string similarity measure usages. It
	/// supports the annotation of a specific type of string similarity measure and
	/// an optional shingle-parameter (if supported by the type).
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class SimilarityTypeAttribute : Attribute
	{
		private static readonly Type similarityInterfaceType =
			typeof(INormalizedStringSimilarity);

		private static readonly Type distanceInterfaceType =
			typeof(INormalizedStringDistance);

		/// <summary>
		/// The <see cref="System.Type"/> of the similarity measure.
		/// </summary>
		public Type Type { get; private set; }

		private Int32 shingleParameter;

		/// <summary>
		/// Returns true, iff the shingles were specified with a value greater than 0.
		/// </summary>
		public Boolean UsesShingles { get { return this.shingleParameter >= 0; } }

		/// <summary>
		/// The amount of shingles, the similarity measure uses. Throws an
		/// <see cref="InvalidOperationException"/> if obtained for a type,
		/// that does not support shingles.
		/// Use <see cref="UsesShingles"/> to obtain a value that indicates
		/// whether this instance uses shingles or not.
		/// </summary>
		public Int32 Shingles
		{
			private set { this.shingleParameter = value; }

			get
			{
				if (this.shingleParameter < 0)
				{
					throw new InvalidOperationException($"This type ({this.Type.FullName}) does not support a shingle-parameter.");
				}

				return this.shingleParameter;
			}
		}

		/// <summary>
		/// The type must implement <see cref="INormalizedStringSimilarity"/>. Some types have
		/// a shingle-parameter, which can optionally be passed.
		/// </summary>
		/// <param name="type">A <see cref="System.Type"/> that is required to
		/// derive from <see cref="INormalizedStringSimilarity"/>.</param>
		/// <param name="shingleParameter">If less than zero, it is ignored.</param>
		public SimilarityTypeAttribute(Type type, Int32 shingleParameter = -1)
		{
			if (!type.GetInterfaces().Any(intf =>
				intf == similarityInterfaceType || intf == distanceInterfaceType))
			{
				throw new TypeInitializationException(nameof(SimilarityTypeAttribute), null);
			}

			this.Type = type;
			this.shingleParameter = shingleParameter;
		}

		/// <summary>
		/// Returns the name of metric and optional shingle-parameter in parentheses.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			var shingleString = this.UsesShingles ? $"({this.Shingles})" : String.Empty;
			return $"{this.Type.Name}{shingleString}";
		}
	}



	/// <summary>
	/// So the setter can be called from within generic code.
	/// </summary>
	public interface IHasSimilarityComparisonType
	{
		SimilarityComparisonType ComparisonType { set; }
	}




	/// <summary>
	/// NormLeven, Jaro, MLCS, NGr(2-6), Cos(2-6), Jacc(2-6), Soren(2-6) = 23
	/// <see cref="http://www.scielo.br/pdf/gmb/v22n3/22n3a24.pdf"/>
	/// </summary>
	public class SimilarityEntity : IHasSimilarityComparisonType
	{
		public virtual UInt32 ID { get; set; }

		public virtual SimilarityComparisonType ComparisonType { get; set; }

		public virtual FileBlockEntity FileBlock { get; set; }

		#region Similarity measures
		[SimilarityType(typeof(NormalizedLevenshtein))]
		public virtual Double NormalizedLevenshtein { get; set; }

		/// <summary>
		/// We use the Jaro-Winkler distance without any alterations to the default parameters.
		/// </summary>
		[SimilarityType(typeof(JaroWinkler))]
		public virtual Double JaroWinkler { get; set; }

		/// <summary>
		/// Metric Longest Common Subsequence as normalized metric
		/// </summary>
		[SimilarityType(typeof(MetricLCS))]
		public virtual Double MetricLongestCommonSubSeq { get; set; }

		#region Variated similiarity measures
		/// <summary>
		/// The normalized N-Gram distance with shingle-lengths 2 through 6.
		/// 
		/// <note>Kondrak, G., 2005. N-gram similarity and distance. In String processing and information retrieval (pp. 115-126). Springer Berlin/Heidelberg.</note>
		/// </summary>
		[SimilarityType(typeof(NGram), 2)]
		public virtual Double NGram2 { get; set; }
		[SimilarityType(typeof(NGram), 3)]
		public virtual Double NGram3 { get; set; }
		[SimilarityType(typeof(NGram), 4)]
		public virtual Double NGram4 { get; set; }
		[SimilarityType(typeof(NGram), 5)]
		public virtual Double NGram5 { get; set; }
		[SimilarityType(typeof(NGram), 6)]
		public virtual Double NGram6 { get; set; }

		/// <summary>
		/// The Cosine simliarity with shingle-lengths 2 through 6.
		/// 
		/// <note>The similarity between the two strings is the cosine of the angle between these two vectors representation, and is computed as V1 . V2 / (|V1| * |V2|) Distance is computed as 1 - cosine similarity.</note>
		/// </summary>
		[SimilarityType(typeof(Cosine), 2)]
		public virtual Double Cosine2 { get; set; }
		[SimilarityType(typeof(Cosine), 3)]
		public virtual Double Cosine3 { get; set; }
		[SimilarityType(typeof(Cosine), 4)]
		public virtual Double Cosine4 { get; set; }
		[SimilarityType(typeof(Cosine), 5)]
		public virtual Double Cosine5 { get; set; }
		[SimilarityType(typeof(Cosine), 6)]
		public virtual Double Cosine6 { get; set; }

		/// <summary>
		/// The Jaccard-Index with shingle-lengths 2 through 6.
		/// 
		/// <note>Jaccard P (1901) Étude comparative de la distribuition florale dans une portion des Alpes et des Jura.Bull Soc Vandoise Sci Nat 37:547-579.</note>
		/// </summary>
		[SimilarityType(typeof(Jaccard), 2)]
		public virtual Double JaccardIdx2 { get; set; }
		[SimilarityType(typeof(Jaccard), 3)]
		public virtual Double JaccardIdx3 { get; set; }
		[SimilarityType(typeof(Jaccard), 4)]
		public virtual Double JaccardIdx4 { get; set; }
		[SimilarityType(typeof(Jaccard), 5)]
		public virtual Double JaccardIdx5 { get; set; }
		[SimilarityType(typeof(Jaccard), 6)]
		public virtual Double JaccardIdx6 { get; set; }

		/// <summary>
		/// The Sorensen-Dice coefficient with shingle-lengths 2 through 6.
		/// 
		/// <note>Dice LR (1945) Measures of the amount of ecologic association between species.Ecology 26:297-302.
		/// </note>
		/// <note>Sorensen T (1948) A method of establishing groups of equal amplitude in plant sociology based on similarity of species content and its application to analyses of the vegetation on Danish commons.Vidensk Selsk Biol Skr 5:1-34.</note>
		/// </summary>
		[SimilarityType(typeof(SorensenDice), 2)]
		public virtual Double SorensenDice2 { get; set; }
		[SimilarityType(typeof(SorensenDice), 3)]
		public virtual Double SorensenDice3 { get; set; }
		[SimilarityType(typeof(SorensenDice), 4)]
		public virtual Double SorensenDice4 { get; set; }
		[SimilarityType(typeof(SorensenDice), 5)]
		public virtual Double SorensenDice5 { get; set; }
		[SimilarityType(typeof(SorensenDice), 6)]
		public virtual Double SorensenDice6 { get; set; }
		#endregion
		#endregion
	}

	public class SimilarityEntityMapper : ClassMap<SimilarityEntity>
	{
		public SimilarityEntityMapper()
		{
			this.Table(nameof(SimilarityEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.ComparisonType).CustomType<SimilarityComparisonType>().Not.Nullable();

			this.Map(x => x.NormalizedLevenshtein).Not.Nullable();
			this.Map(x => x.JaroWinkler).Not.Nullable();
			this.Map(x => x.MetricLongestCommonSubSeq).Not.Nullable();

			this.Map(x => x.NGram2).Not.Nullable();
			this.Map(x => x.NGram3).Not.Nullable();
			this.Map(x => x.NGram4).Not.Nullable();
			this.Map(x => x.NGram5).Not.Nullable();
			this.Map(x => x.NGram6).Not.Nullable();

			this.Map(x => x.Cosine2).Not.Nullable();
			this.Map(x => x.Cosine3).Not.Nullable();
			this.Map(x => x.Cosine4).Not.Nullable();
			this.Map(x => x.Cosine5).Not.Nullable();
			this.Map(x => x.Cosine6).Not.Nullable();

			this.Map(x => x.JaccardIdx2).Not.Nullable();
			this.Map(x => x.JaccardIdx3).Not.Nullable();
			this.Map(x => x.JaccardIdx4).Not.Nullable();
			this.Map(x => x.JaccardIdx5).Not.Nullable();
			this.Map(x => x.JaccardIdx6).Not.Nullable();

			this.Map(x => x.SorensenDice2).Not.Nullable();
			this.Map(x => x.SorensenDice3).Not.Nullable();
			this.Map(x => x.SorensenDice4).Not.Nullable();
			this.Map(x => x.SorensenDice5).Not.Nullable();
			this.Map(x => x.SorensenDice6).Not.Nullable();

			this.References<FileBlockEntity>(x => x.FileBlock).Not.Nullable();
		}
	}
}
