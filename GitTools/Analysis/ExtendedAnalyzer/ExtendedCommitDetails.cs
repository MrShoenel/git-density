/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
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
		/// Forwards constructor that only calls <see cref="SimpleCommitDetails.SimpleCommitDetails(string, Commit)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="commit"></param>
		public ExtendedCommitDetails(String repoPathOrUrl, Commit commit)
			: base(repoPathOrUrl, commit)
		{
		}

		#region overridden fields
		/// <summary>
		/// Overridden to return the entire commit message, which is potentially multi-line.
		/// Line-breaks are substituted with single spaces.
		/// </summary>
		[CsvColumn(FieldIndex = 8)]
		public override string Message
			=> RegexNewLines.Replace(base.commit.Message, " ").Replace('"', ' ').Trim();
		#endregion

		#region additional fields
		/// <summary>
		/// For initial commits (without parent), this field has a negative value (i.e. -.1)
		/// to make it easier to distinguish from other commits' values.
		/// </summary>
		[CsvColumn(FieldIndex = 7)]
		public double MinutesSincePreviousCommit { get; protected internal set; } = -.1;

		[CsvColumn(FieldIndex = 11)]
		public String AuthorNominalLabel { get; protected internal set; } = String.Empty;

		[CsvColumn(FieldIndex = 12)]
		public String CommitterNominalLabel { get; protected internal set; } = String.Empty;

		[CsvColumn(FieldIndex = 17)]
		public UInt32 NumberOfFilesAdded { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 18)]
		public UInt32 NumberOfFilesAddedNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 19)]
		public UInt32 NumberOfLinesAddedByAddedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 20)]
		public UInt32 NumberOfLinesAddedByAddedFilesNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 21)]
		public UInt32 NumberOfFilesDeleted { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 22)]
		public UInt32 NumberOfFilesDeletedNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 23)]
		public UInt32 NumberOfLinesDeletedByDeletedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 24)]
		public UInt32 NumberOfLinesDeletedByDeletedFilesNet { get; protected internal set; } = 0u;


		[CsvColumn(FieldIndex = 25)]
		public UInt32 NumberOfFilesModified { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 26)]
		public UInt32 NumberOfFilesModifiedNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 27)]
		public UInt32 NumberOfFilesRenamed { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 28)]
		public UInt32 NumberOfFilesRenamedNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 29)]
		public UInt32 NumberOfLinesAddedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 30)]
		public UInt32 NumberOfLinesAddedByModifiedFilesNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 31)]
		public UInt32 NumberOfLinesDeletedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 32)]
		public UInt32 NumberOfLinesDeletedByModifiedFilesNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 33)]
		public UInt32 NumberOfLinesAddedByRenamedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 34)]
		public UInt32 NumberOfLinesAddedByRenamedFilesNet { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 35)]
		public UInt32 NumberOfLinesDeletedByRenamedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 36)]
		public UInt32 NumberOfLinesDeletedByRenamedFilesNet { get; protected internal set; } = 0u;
		#endregion

		#region additional generated fields
		/// <summary>
		/// Returns the ratio between all lines added/removed gross and all lines
		/// added/removed net ([0,1]).
		/// </summary>
		[CsvColumn(FieldIndex = 37)]
		public Double Density
		{
			get
			{
				var netLines = this.NumberOfLinesAddedByAddedFilesNet
					+ this.NumberOfLinesAddedByModifiedFilesNet
					+ this.NumberOfLinesAddedByRenamedFilesNet
					+ this.NumberOfLinesDeletedByDeletedFilesNet
					+ this.NumberOfLinesDeletedByModifiedFilesNet
					+ this.NumberOfLinesDeletedByRenamedFilesNet;
				var grossLines = this.NumberOfLinesAddedByAddedFiles
					+ this.NumberOfLinesAddedByModifiedFiles
					+ this.NumberOfLinesAddedByRenamedFiles
					+ this.NumberOfLinesDeletedByDeletedFiles
					+ this.NumberOfLinesDeletedByModifiedFiles
					+ this.NumberOfLinesDeletedByRenamedFiles;

				return grossLines == 0u ? 0d :
					Math.Round((double)netLines / (double)grossLines, 5);
			}
		}

		/// <summary>
		/// Returns the ratio of files changed (add/del/mov/mod) in a commit and
		/// the amount of files affected net (an affected file is one that had
		/// more than zero lines net added/deleted after).
		/// </summary>
		[CsvColumn(FieldIndex = 38)]
		public Double AffectedFilesRatioNet
		{
			get
			{
				var numAffectedFiles = this.NumberOfFilesAdded + this.NumberOfFilesDeleted +
					this.NumberOfFilesModified + this.NumberOfFilesRenamed;
				var numActualAffectedFiles = this.NumberOfFilesAddedNet + this.NumberOfFilesDeletedNet +
					this.NumberOfFilesModifiedNet + this.NumberOfFilesRenamedNet;

				return numAffectedFiles == 0u ? 0d :
					Math.Round((double)numActualAffectedFiles / (double)numAffectedFiles, 5);
			}
		}
		#endregion
	}
}
