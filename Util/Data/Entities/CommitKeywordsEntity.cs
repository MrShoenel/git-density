/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using FluentNHibernate.Mapping;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Util.Extensions;

namespace Util.Data.Entities
{
	/// <summary>
	/// Keywords are detected in a <see cref="Commit.Message"/> or in
	/// <see cref="LibGit2Sharp.Commit.MessageShort"/>, and their presence
	/// is noted in this entity.
	/// Initially, the 20 keywords as defined by Levin and Yehudai are used:
	/// (1) add (2) allow (3) bug (4) chang (5) error (6) fail (7) fix
	/// (8) implement (9) improv (10) issu (11) method (12) new (13) npe
	/// (14) refactor (15) remov (16) report (17) set (18) support
	/// (19) test (20) use
	/// </summary>
	public class CommitKeywordsEntity : BaseEntity<Commit>, IEquatable<CommitKeywordsEntity>
	{
		public virtual UInt32 ID { get; set; }

		public virtual CommitEntity Commit { get; set; }

		public virtual RepositoryEntity Repository { get; set; }

		#region Keywords
		[Keyword("add")]
		public virtual UInt32 KW_add { get; set; } = 0;
		[Keyword("allow")]
		public virtual UInt32 KW_allow { get; set; } = 0;
		[Keyword("bug")]
		public virtual UInt32 KW_bug { get; set; } = 0;
		[Keyword("chang")]
		public virtual UInt32 KW_chang { get; set; } = 0;
		[Keyword("error")]
		public virtual UInt32 KW_error { get; set; } = 0;
		[Keyword("fail")]
		public virtual UInt32 KW_fail { get; set; } = 0;
		[Keyword("fix")]
		public virtual UInt32 KW_fix { get; set; } = 0;


		[Keyword("implement")]
		public virtual UInt32 KW_implement { get; set; } = 0;
		[Keyword("improv")]
		public virtual UInt32 KW_improv { get; set; } = 0;
		[Keyword("issu")]
		public virtual UInt32 KW_issu { get; set; } = 0;
		[Keyword("method")]
		public virtual UInt32 KW_method { get; set; } = 0;
		[Keyword("new")]
		public virtual UInt32 KW_new { get; set; } = 0;
		[Keyword("npe")]
		public virtual UInt32 KW_npe { get; set; } = 0;


		[Keyword("refactor")]
		public virtual UInt32 KW_refactor { get; set; } = 0;
		[Keyword("remov")]
		public virtual UInt32 KW_remov { get; set; } = 0;
		[Keyword("report")]
		public virtual UInt32 KW_report { get; set; } = 0;
		[Keyword("set")]
		public virtual UInt32 KW_set { get; set; } = 0;
		[Keyword("support")]
		public virtual UInt32 KW_support { get; set; } = 0;
		[Keyword("test")]
		public virtual UInt32 KW_test { get; set; } = 0;
		[Keyword("use")]
		public virtual UInt32 KW_use { get; set; } = 0;


		/// <summary>
		/// Collection that provides access to the defined keywords.
		/// </summary>
		public static IImmutableSet<PropertyInfo> KeywordProperties =
			typeof(CommitKeywordsEntity).GetProperties().Where(prop =>
				prop.GetCustomAttributes().Any(ca => ca is KeywordAttribute))
				.ToImmutableHashSet();
		#endregion

		/// <summary>
		/// Used to split the message into words.
		/// </summary>
		public static readonly Regex MessageSplitRegex = new Regex(
			"\\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		#region IEquatable
		public virtual bool Equals(CommitKeywordsEntity other)
		{
			if (!(other is CommitKeywordsEntity))
			{
				return false;
			}

			foreach (var kwp in KeywordProperties)
			{
				if (kwp.GetValue(this) != kwp.GetValue(other))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as CommitKeywordsEntity);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hc = 1337;
				foreach (var kwp in KeywordProperties)
				{
					var temp = (Int32)(UInt32)kwp.GetValue(this);
					if (temp > 0)
					{
						hc *= temp;
					}
				}

				return hc;
			}
		}
		#endregion

		#region Parse for keywords
		/// <summary>
		/// Reads a message and adds keywords detected in it to this
		/// instance.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public virtual CommitKeywordsEntity AddFromMessage(string message)
		{
			var words = MessageSplitRegex.Split(message.ToLower());

			foreach (var kwp in KeywordProperties)
			{
				var kw = kwp.GetCustomAttribute<KeywordAttribute>().Keyword;
				var matchCount = (UInt32)words.Count(w => w.Contains(kw));
				// .. and increase the property's count:

				kwp.SetValue(this, (UInt32)kwp.GetValue(this) + matchCount);
			}

			return this;
		}

		/// <summary>
		/// Adds keyword-counts to this instance as read from a
		/// <see cref="LibGit2Sharp.Commit.Message"/> as well as
		/// from <see cref="LibGit2Sharp.Commit.MessageShort"/>.
		/// </summary>
		/// <param name="commit"></param>
		/// <returns></returns>
		public virtual CommitKeywordsEntity AddFromCommit(Commit commit)
		{
			return this.AddFromMessage(commit.Message);
		}

		/// <summary>
		/// Given a string that represents a message, counts how often a
		/// words matches one of the pre-defined keywords and increases
		/// the counter for each keyword.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public static CommitKeywordsEntity FromMessage(String message)
		{
			return new CommitKeywordsEntity().AddFromMessage(message);
		}

		/// <summary>
		/// Calls <see cref="FromMessage(string)"/> with the commit's
		/// <see cref="Commit.Message"/> and <see cref="Commit.MessageShort"/>.
		/// </summary>
		/// <param name="commit"></param>
		/// <returns></returns>
		public static CommitKeywordsEntity FromCommit(Commit commit)
		{
			return new CommitKeywordsEntity().AddFromCommit(commit);
		}
		#endregion
	}



	public class CommitKeywordsEntityMap : ClassMap<CommitKeywordsEntity>
	{
		public CommitKeywordsEntityMap()
		{
			this.Table(nameof(CommitKeywordsEntity).ToSimpleUnderscoreCase());

			this.Id(x => x.ID).GeneratedBy.Identity();

			this.Map(x => x.KW_add).Not.Nullable().Default("0");
			this.Map(x => x.KW_allow).Not.Nullable().Default("0");
			this.Map(x => x.KW_bug).Not.Nullable().Default("0");
			this.Map(x => x.KW_chang).Not.Nullable().Default("0");
			this.Map(x => x.KW_error).Not.Nullable().Default("0");
			this.Map(x => x.KW_fail).Not.Nullable().Default("0");
			this.Map(x => x.KW_fix).Not.Nullable().Default("0");

			this.Map(x => x.KW_implement).Not.Nullable().Default("0");
			this.Map(x => x.KW_improv).Not.Nullable().Default("0");
			this.Map(x => x.KW_issu).Not.Nullable().Default("0");
			this.Map(x => x.KW_method).Not.Nullable().Default("0");
			this.Map(x => x.KW_new).Not.Nullable().Default("0");
			this.Map(x => x.KW_npe).Not.Nullable().Default("0");

			this.Map(x => x.KW_refactor).Not.Nullable().Default("0");
			this.Map(x => x.KW_remov).Not.Nullable().Default("0");
			this.Map(x => x.KW_report).Not.Nullable().Default("0");
			this.Map(x => x.KW_set).Not.Nullable().Default("0");
			this.Map(x => x.KW_support).Not.Nullable().Default("0");
			this.Map(x => x.KW_test).Not.Nullable().Default("0");
			this.Map(x => x.KW_use).Not.Nullable().Default("0");

			this.References<CommitEntity>(x => x.Commit).Not.Nullable().Cascade.Lock();
			this.References<RepositoryEntity>(x => x.Repository).Not.Nullable().Cascade.Lock();
		}
	}


	/// <summary>
	/// Used only in the class <see cref="CommitKeywordsEntity"/>.
	/// </summary>
	public sealed class KeywordAttribute : Attribute
	{
		/// <summary>
		/// Partial keyword to match words against.
		/// </summary>
		public string Keyword { get; private set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="keyword"></param>
		public KeywordAttribute(string keyword)
		{
			this.Keyword = keyword;
		}
	}
}
