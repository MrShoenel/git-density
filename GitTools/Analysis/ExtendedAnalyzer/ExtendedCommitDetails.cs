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

namespace GitTools.Analysis.ExtendedAnalyzer
{
	/// <summary>
	/// Extends the <see cref="SimpleCommitDetails"/> with many more fields
	/// and is produced by the <see cref="ExtendedAnalyzer"/>.
	/// </summary>
	public class ExtendedCommitDetails : SimpleCommitDetails
	{
		/// <summary>
		/// Forwards constructor that only calls <see cref="SimpleCommitDetails.SimpleCommitDetails(string, Repository, Commit)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="repository"></param>
		/// <param name="commit"></param>
		public ExtendedCommitDetails(String repoPathOrUrl, Repository repository, Commit commit) : base(repoPathOrUrl, repository, commit)
		{
		}

		#region overridden fields
		/// <summary>
		/// Overridden to return the entire commit message, which is potentially multi-line.
		/// Line-breaks are substituted with single spaces.
		/// </summary>
		[CsvColumn(FieldIndex = 8)]
		public override string Message
			=> RegexNewLines.Replace(base.commit.Message, " ").Trim();
		#endregion

		#region additional fields
		/// <summary>
		/// For initial commits (without parent), this field is null.
		/// </summary>
		[CsvColumn(FieldIndex = 7, CanBeNull = true)]
		public double? MinutesSincePreviousCommit { get; protected internal set; } = null;

		[CsvColumn(FieldIndex = 9)]
		public String AuthorEmail => this.commit.Author.Email;

		[CsvColumn(FieldIndex = 10)]
		public String CommitterEmail => this.commit.Committer.Email;

		[CsvColumn(FieldIndex = 11)]
		public Boolean IsInitialCommit { get; protected internal set; } = false;

		[CsvColumn(FieldIndex = 12)]
		public Boolean IsMergeCommit { get; protected internal set; } = false;

		[CsvColumn(FieldIndex = 13)]
		public UInt32 NumberOfParentCommits { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 14)]
		public UInt32 NumberOfFilesAdded { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 15)]
		public UInt32 NumberOfLinesAddedByAddedFilesNoComments { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 16)]
		public UInt32 NumberOfLinesAddedByAddedFilesGross { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 17)]
		public UInt32 NumberOfFilesDeleted { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 18)]
		public UInt32 NumberOfLinesDeletedByDeletedFilesNoComments { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 19)]
		public UInt32 NumberOfLinesDeletedByDeletedFilesGross { get; protected internal set; } = 0u;


		[CsvColumn(FieldIndex = 20)]
		public UInt32 NumberOfFilesModified { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 21)]
		public UInt32 NumberOfFilesRenamed { get; protected internal set; } = 0u;
	
		[CsvColumn(FieldIndex = 22)]
		public UInt32 NumberOfLinesAddedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 23)]
		public UInt32 NumberOfLinesDeletedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 24)]
		public UInt32 NumberOfLinesAddedByRenamedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 25)]
		public UInt32 NumberOfLinesDeletedByRenamedFiles { get; protected internal set; } = 0u;
		#endregion
	}
}
