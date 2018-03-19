/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using F23.StringSimilarity;
using F23.StringSimilarity.Interfaces;
using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Util.Extensions;
using Util.Similarity;
using SMT = Util.Similarity.SimilarityMeasurementType;

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
		/// Similar as <see cref="Type"/>, but as corresponding enum-value. See
		/// <see cref="SimilarityTypeAttribute(SMT, Type, int)"/> for a more in-
		/// depth description.
		/// </summary>
		public SMT TypeEnum { get; private set; }

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
		/// <param name="similarityMeasurementType">A value that uniquely identifies the
		/// represented measurement of similarity and its parameters. For example, if the type
		/// is <see cref="Jaccard"/> and the number of shingles is 6, the value for this parameter
		/// should be <see cref="SMT.Jaccard6"/>.</param>
		/// <param name="type">A <see cref="System.Type"/> that is required to
		/// derive from <see cref="INormalizedStringSimilarity"/>.</param>
		/// <param name="shingleParameter">If less than zero, it is ignored.</param>
		public SimilarityTypeAttribute(SMT similarityMeasurementType, Type type, Int32 shingleParameter = -1)
		{
			if (!type.GetInterfaces().Any(intf =>
				intf == similarityInterfaceType || intf == distanceInterfaceType))
			{
				throw new TypeInitializationException(nameof(SimilarityTypeAttribute), null);
			}

			this.TypeEnum = similarityMeasurementType;
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
		/// <summary>
		/// Each type inheriting from this interface and having computable similarity
		/// properties annotated using the <see cref="SimilarityTypeAttribute"/>,
		/// needs to implement the setter for a type of <see cref="SimilarityComparisonType"/>.
		/// </summary>
		SimilarityComparisonType ComparisonType { set; }

		/// <summary>
		/// Should reflect the value of the TextBlocks' lines added.
		/// </summary>
		UInt32 LinesAdded { set; }

		/// <summary>
		/// Should reflect the value of the TextBlocks' lines deleted.
		/// </summary>
		UInt32 LinesDeleted { set; }

		/// <summary>
		/// Should return the computed similarity for a given measurement.
		/// </summary>
		/// <param name="smt"></param>
		/// <returns></returns>
		Double this[SMT smt] { get; }
	}




	/// <summary>
	/// NormLeven, Jaro, MLCS, NGr(2,4,6), Cos(2,4,6), Jacc(2,4,6), Soren(2,4,6) = 15
	/// <see cref="http://www.scielo.br/pdf/gmb/v22n3/22n3a24.pdf"/>
	/// Currently supports all types defined in <see cref="SMT"/>.
	/// </summary>
	public class SimilarityEntity : IHasSimilarityComparisonType
	{
		private static Lazy<IReadOnlyDictionary<SMT, PropertyInfo>> lazySmtToPropInfo =
			new Lazy<IReadOnlyDictionary<SMT, PropertyInfo>>(() =>
			{
				return new ReadOnlyDictionary<SMT, PropertyInfo>(typeof(SimilarityEntity).GetProperties(BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance).Select(prop
					=> new {
						Prop = prop,
						Attr = prop.GetCustomAttributes(typeof(SimilarityTypeAttribute), true)
									.Cast<SimilarityTypeAttribute>().FirstOrDefault()
					})
					.Where(prop => prop.Attr is SimilarityTypeAttribute)
					.ToDictionary(anon => anon.Attr.TypeEnum, anon => anon.Prop));
			});

		/// <summary>
		/// Returns a dictionary where each <see cref="SMT"/> maps to a <see cref="PropertyInfo"/> of this
		/// class. Note that this class does not necessarily implements all types of <see cref="SMT"/>.
		/// Use <see cref="Implements(SMT)"/> to check whether this class implements a certain type of
		/// similarity measurement.
		/// </summary>
		public static IReadOnlyDictionary<SMT, PropertyInfo> SmtToPropertyInfo => lazySmtToPropInfo.Value;

		/// <summary>
		/// Returns whether this class implements a certain type of similarity measurement (<see cref="SMT"/>).
		/// </summary>
		/// <param name="smt"></param>
		/// <returns></returns>
		public static Boolean Implements(SMT smt) => SmtToPropertyInfo.ContainsKey(smt);

		public virtual UInt32 ID { get; set; }

		[Indexed]
		public virtual SimilarityComparisonType ComparisonType { get; set; }

		public virtual FileBlockEntity FileBlock { get; set; }

		/// <summary>
		/// Note that this property is always a positive integer, because it is never
		/// multiplied by factors from a similarity. It represents an amount.
		/// </summary>
		public virtual UInt32 LinesAdded { get; set; }

		/// <summary>
		/// Note that this property is always a positive integer, because it is never
		/// multiplied by factors from a similarity. It represents an amount.
		/// </summary>
		public virtual UInt32 LinesDeleted { get; set; }

		/// <summary>
		/// Returns the value for the computed similarity measurement.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this class does not implement the
		/// requested measurement type.</exception>
		/// <param name="smt"></param>
		/// <returns></returns>
		public virtual Double this[SMT smt]
		{
			get
			{
				if (!SimilarityEntity.Implements(smt))
				{
					throw new InvalidOperationException(
						$"This class does not implement the similarity measurement type {smt.ToString()}.");
				}

				return (Double)SimilarityEntity.SmtToPropertyInfo[smt].GetValue(this);
			}
		}

		#region No-Similarity measure
		/// <summary>
		/// We use this to support the type <see cref="SMT.None"/>. This property is not
		/// mapped into the DB.
		/// </summary>
		[SimilarityType(SMT.None, typeof(DefaultNoSimilarityMeasurement))]
		public virtual Double NoSimilarity { get; set; }
		#endregion

		#region Similarity measures
		[SimilarityType(SMT.NormalizedLevenshtein, typeof(NormalizedLevenshtein))]
		public virtual Double NormalizedLevenshtein { get; set; }

		/// <summary>
		/// We use the Jaro-Winkler distance without any alterations to the default parameters.
		/// </summary>
		[SimilarityType(SMT.JaroWinkler, typeof(JaroWinkler))]
		public virtual Double JaroWinkler { get; set; }

		/// <summary>
		/// Metric Longest Common Subsequence as normalized metric
		/// </summary>
		[SimilarityType(SMT.MetricLongestCommonSubSequence, typeof(MetricLCS))]
		public virtual Double MetricLongestCommonSubSeq { get; set; }

		#region Variated similiarity measures
		/// <summary>
		/// The normalized N-Gram distance with shingle-lengths 2 through 6.
		/// 
		/// <note>Kondrak, G., 2005. N-gram similarity and distance. In String processing and information retrieval (pp. 115-126). Springer Berlin/Heidelberg.</note>
		/// </summary>
		[SimilarityType(SMT.NGram2, typeof(NGram), 2)]
		public virtual Double NGram2 { get; set; }
		[SimilarityType(SMT.NGram3, typeof(NGram), 3)]
		public virtual Double NGram3 { get; set; }
		[SimilarityType(SMT.NGram4, typeof(NGram), 4)]
		public virtual Double NGram4 { get; set; }
		[SimilarityType(SMT.NGram5, typeof(NGram), 5)]
		public virtual Double NGram5 { get; set; }
		[SimilarityType(SMT.NGram6, typeof(NGram), 6)]
		public virtual Double NGram6 { get; set; }

		/// <summary>
		/// The Cosine simliarity with shingle-lengths 2 through 6.
		/// 
		/// <note>The similarity between the two strings is the cosine of the angle between these two vectors representation, and is computed as V1 . V2 / (|V1| * |V2|) Distance is computed as 1 - cosine similarity.</note>
		/// </summary>
		[SimilarityType(SMT.Cosine2, typeof(Cosine), 2)]
		public virtual Double Cosine2 { get; set; }
		[SimilarityType(SMT.Cosine3, typeof(Cosine), 3)]
		public virtual Double Cosine3 { get; set; }
		[SimilarityType(SMT.Cosine4, typeof(Cosine), 4)]
		public virtual Double Cosine4 { get; set; }
		[SimilarityType(SMT.Cosine5, typeof(Cosine), 5)]
		public virtual Double Cosine5 { get; set; }
		[SimilarityType(SMT.Cosine6, typeof(Cosine), 6)]
		public virtual Double Cosine6 { get; set; }

		/// <summary>
		/// The Jaccard-Index with shingle-lengths 2 through 6.
		/// 
		/// <note>Jaccard P (1901) Étude comparative de la distribuition florale dans une portion des Alpes et des Jura.Bull Soc Vandoise Sci Nat 37:547-579.</note>
		/// </summary>
		[SimilarityType(SMT.Jaccard2, typeof(Jaccard), 2)]
		public virtual Double JaccardIdx2 { get; set; }
		[SimilarityType(SMT.Jaccard3, typeof(Jaccard), 3)]
		public virtual Double JaccardIdx3 { get; set; }
		[SimilarityType(SMT.Jaccard4, typeof(Jaccard), 4)]
		public virtual Double JaccardIdx4 { get; set; }
		[SimilarityType(SMT.Jaccard5, typeof(Jaccard), 5)]
		public virtual Double JaccardIdx5 { get; set; }
		[SimilarityType(SMT.Jaccard6, typeof(Jaccard), 6)]
		public virtual Double JaccardIdx6 { get; set; }

		/// <summary>
		/// The Sorensen-Dice coefficient with shingle-lengths 2 through 6.
		/// 
		/// <note>Dice LR (1945) Measures of the amount of ecologic association between species.Ecology 26:297-302.
		/// </note>
		/// <note>Sorensen T (1948) A method of establishing groups of equal amplitude in plant sociology based on similarity of species content and its application to analyses of the vegetation on Danish commons.Vidensk Selsk Biol Skr 5:1-34.</note>
		/// </summary>
		[SimilarityType(SMT.SorensenDice2, typeof(SorensenDice), 2)]
		public virtual Double SorensenDice2 { get; set; }
		[SimilarityType(SMT.SorensenDice3, typeof(SorensenDice), 3)]
		public virtual Double SorensenDice3 { get; set; }
		[SimilarityType(SMT.SorensenDice4, typeof(SorensenDice), 4)]
		public virtual Double SorensenDice4 { get; set; }
		[SimilarityType(SMT.SorensenDice5, typeof(SorensenDice), 5)]
		public virtual Double SorensenDice5 { get; set; }
		[SimilarityType(SMT.SorensenDice6, typeof(SorensenDice), 6)]
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

			this.Map(x => x.ComparisonType)
				.CustomType<SimilarityComparisonType>().Not.Nullable();
			this.Map(x => x.LinesAdded).Not.Nullable();
			this.Map(x => x.LinesDeleted).Not.Nullable();

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
