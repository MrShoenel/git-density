/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitTools. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using LINQtoCSV;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Util.Data.Entities;

namespace GitTools.Analysis
{
	/// <summary>
	/// Represents basic details about one <see cref="LibGit2Sharp.Commit"/> in a
	/// <see cref="Repository"/>.
	/// </summary>
	public class SimpleCommitDetails : IAnalyzedCommit
	{
		/// <summary>
		/// To remove newlines from commits' messages.
		/// </summary>
		protected static readonly Regex RegexNewLines =
			new Regex("\r|\n", RegexOptions.ECMAScript | RegexOptions.Compiled);

		/// <summary>
		/// Keeps a reference to the <see cref="LibGit2Sharp.Commit"/>.
		/// </summary>
		public Commit Commit { protected set; get; }

		/// <summary>
		/// Keeps a convenient reference.
		/// </summary>
		protected readonly CommitKeywordsEntity keywords;

		/// <summary>
		/// Initializes a new <see cref="SimpleCommitDetails"/> from a given
		/// <see cref="Repository"/> and <see cref="LibGit2Sharp.Commit"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="commit"></param>
		public SimpleCommitDetails(String repoPathOrUrl, Commit commit)
		{
			this.RepoPathOrUrl = repoPathOrUrl;
			this.Commit = commit;
			this.keywords = CommitKeywordsEntity.FromCommit(commit);

			var parents = this.Commit.Parents.ToList();

			this.IsInitialCommit = parents.Count == 0 ? 1u : 0u;
			this.IsMergeCommit = parents.Count > 1 ? 1u : 0u;
			this.NumberOfParentCommits = (UInt32)parents.Count;
			this.ParentCommitSHA1s = String.Join(",", parents.Select(c => c.Sha));
		}

		#region fields
		[CsvColumn(FieldIndex = 1)]
		public String SHA1 => this.Commit.Sha;

		[CsvColumn(FieldIndex = 2)]
		public String RepoPathOrUrl { get; protected set; }

		[CsvColumn(FieldIndex = 3)]
		public String AuthorName => this.Commit.Author.Name;

		[CsvColumn(FieldIndex = 4)]
		public String CommitterName => this.Commit.Committer.Name;

		[CsvColumn(FieldIndex = 5, OutputFormat = "yyyy-MM-dd HH:MM:ss")]
		public DateTime AuthorTime => this.Commit.Author.When.UtcDateTime;

		[CsvColumn(FieldIndex = 6, OutputFormat = "yyyy-MM-dd HH:MM:ss")]
		public DateTime CommitterTime => this.Commit.Committer.When.UtcDateTime;

		[CsvColumn(FieldIndex = 7)]
		public Int64 AuthorTimeUnixEpochMilliSecs => this.Commit.Author.When.ToUnixTimeMilliseconds();

		[CsvColumn(FieldIndex = 8)]
		public Int64 CommitterTimeUnixEpochMilliSecs => this.Commit.Committer.When.ToUnixTimeMilliseconds();

		[CsvColumn(FieldIndex = 11)]
		public String AuthorEmail => this.Commit.Author.Email;

		[CsvColumn(FieldIndex = 12)]
		public String CommitterEmail => this.Commit.Committer.Email;

		/// <summary>
		/// This is a boolean field but we use 0/1 for compatibility reasons.
		/// </summary>
		[CsvColumn(FieldIndex = 15)]
		public UInt32 IsInitialCommit { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 16)]
		public UInt32 IsMergeCommit { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 17)]
		public UInt32 NumberOfParentCommits { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 18)]
		public String ParentCommitSHA1s { get; protected internal set; } = String.Empty;

		#region Keywords
		[CsvColumn(FieldIndex = 41)]
		public UInt32 KW_add => this.keywords.KW_add;
		[CsvColumn(FieldIndex = 42)]
		public UInt32 KW_allow => this.keywords.KW_allow;
		[CsvColumn(FieldIndex = 43)]
		public UInt32 KW_bug => this.keywords.KW_bug;
		[CsvColumn(FieldIndex = 44)]
		public UInt32 KW_chang => this.keywords.KW_chang;
		[CsvColumn(FieldIndex = 45)]
		public UInt32 KW_error => this.keywords.KW_error;
		[CsvColumn(FieldIndex = 46)]
		public UInt32 KW_fail => this.keywords.KW_fail;
		[CsvColumn(FieldIndex = 47)]
		public UInt32 KW_fix => this.keywords.KW_fix;
		[CsvColumn(FieldIndex = 48)]
		public UInt32 KW_implement => this.keywords.KW_implement;
		[CsvColumn(FieldIndex = 49)]
		public UInt32 KW_improv => this.keywords.KW_improv;
		[CsvColumn(FieldIndex = 50)]
		public UInt32 KW_issu => this.keywords.KW_issu;

		[CsvColumn(FieldIndex = 51)]
		public UInt32 KW_method => this.keywords.KW_method;
		[CsvColumn(FieldIndex = 52)]
		public UInt32 KW_new => this.keywords.KW_new;
		[CsvColumn(FieldIndex = 53)]
		public UInt32 KW_npe => this.keywords.KW_npe;
		[CsvColumn(FieldIndex = 54)]
		public UInt32 KW_refactor => this.keywords.KW_refactor;
		[CsvColumn(FieldIndex = 55)]
		public UInt32 KW_remov => this.keywords.KW_remov;
		[CsvColumn(FieldIndex = 56)]
		public UInt32 KW_report => this.keywords.KW_report;
		[CsvColumn(FieldIndex = 57)]
		public UInt32 KW_set => this.keywords.KW_set;
		[CsvColumn(FieldIndex = 58)]
		public UInt32 KW_support => this.keywords.KW_support;
		[CsvColumn(FieldIndex = 59)]
		public UInt32 KW_test => this.keywords.KW_test;
		[CsvColumn(FieldIndex = 60)]
		public UInt32 KW_use => this.keywords.KW_use;
		#endregion

		#region virtual fields
		/// <summary>
		/// If using the <see cref="SimpleAnalyzer.SimpleAnalyzer"/>, this will be
		/// the commit's short message.
		/// </summary>
		[CsvColumn(FieldIndex = 9)]
		public virtual String Message =>
			RegexNewLines.Replace(this.Commit.MessageShort, " ").Replace('"', ' ').Trim();
		#endregion
		#endregion
	}
}
