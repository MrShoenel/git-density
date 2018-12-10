/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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

namespace GitTools.Analysis
{
	/// <summary>
	/// Represents basic details about one <see cref="Commit"/> in a
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
		/// Keeps a reference to the <see cref="Repository"/>.
		/// </summary>
		protected readonly Repository repository;

		/// <summary>
		/// Keeps a reference to the <see cref="Commit"/>.
		/// </summary>
		protected readonly Commit commit;

		/// <summary>
		/// Initializes a new <see cref="SimpleCommitDetails"/> from a given
		/// <see cref="Repository"/> and <see cref="Commit"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="repository"></param>
		/// <param name="commit"></param>
		public SimpleCommitDetails(String repoPathOrUrl, Repository repository, Commit commit)
		{
			this.RepoPathOrUrl = repoPathOrUrl;
			this.repository = repository;
			this.commit = commit;

			var parents = this.commit.Parents.ToList();

			this.IsInitialCommit = parents.Count == 0 ? 1u : 0u;
			this.IsMergeCommit = parents.Count > 1 ? 1u : 0u;
			this.NumberOfParentCommits = (UInt32)parents.Count;
			this.ParentCommitSHA1s = String.Join(",", parents.Select(c => c.Sha));
		}

		#region fields
		[CsvColumn(FieldIndex = 1)]
		public String SHA1 => this.commit.Sha;

		[CsvColumn(FieldIndex = 2)]
		public String RepoPathOrUrl { get; protected set; }

		[CsvColumn(FieldIndex = 3)]
		public String AuthorName => this.commit.Author.Name;

		[CsvColumn(FieldIndex = 4)]
		public String CommitterName => this.commit.Committer.Name;

		[CsvColumn(FieldIndex = 5, OutputFormat = "yyyy-MM-dd HH:MM:ss")]
		public DateTime AuthorTime => this.commit.Author.When.DateTime;

		[CsvColumn(FieldIndex = 6, OutputFormat = "yyyy-MM-dd HH:MM:ss")]
		public DateTime CommitterTime => this.commit.Committer.When.DateTime;

		[CsvColumn(FieldIndex = 9)]
		public String AuthorEmail => this.commit.Author.Email;

		[CsvColumn(FieldIndex = 10)]
		public String CommitterEmail => this.commit.Committer.Email;

		/// <summary>
		/// This is a boolean field but we use 0/1 for compatibility reasons.
		/// </summary>
		[CsvColumn(FieldIndex = 11)]
		public UInt32 IsInitialCommit { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 12)]
		public UInt32 IsMergeCommit { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 13)]
		public UInt32 NumberOfParentCommits { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 14)]
		public String ParentCommitSHA1s { get; protected internal set; } = String.Empty;

		#region virtual fields
		/// <summary>
		/// If using the <see cref="SimpleAnalyzer.SimpleAnalyzer"/>, this will be
		/// the commit's short message.
		/// </summary>
		[CsvColumn(FieldIndex = 8)]
		public virtual String Message =>
			RegexNewLines.Replace(this.commit.MessageShort, " ").Trim();
		#endregion
		#endregion
	}
}
