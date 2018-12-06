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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitTools.Analysis.ExtendedAnalyzer
{
	public class ExtendedCommitDetails : SimpleCommitDetails
	{
		public ExtendedCommitDetails(String repoPathOrUrl, Repository repository, Commit commit) : base(repoPathOrUrl, repository, commit)
		{
		}

		[CsvColumn(FieldIndex = 7)]
		public String Message { get; protected internal set; }

		[CsvColumn(FieldIndex = 8)]
		public Boolean IsInitialCommit { get; protected internal set; }

		[CsvColumn(FieldIndex = 9)]
		public Boolean IsMergeCommit { get; protected internal set; }

		[CsvColumn(FieldIndex = 10)]
		public UInt32 NumberOfParentCommits { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 11)]
		public UInt32 NumberOfFilesAdded { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 12)]
		public UInt32 NumberOfLinesAddedByAddedFilesNoComments { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 13)]
		public UInt32 NumberOfLinesAddedByAddedFilesGross { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 14)]
		public UInt32 NumberOfFilesDeleted { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 15)]
		public UInt32 NumberOfLinesDeletedByDeletedFilesNoComments { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 16)]
		public UInt32 NumberOfLinesDeletedByDeletedFilesGross { get; protected internal set; } = 0u;


		[CsvColumn(FieldIndex = 17)]
		public UInt32 NumberOfFilesModified { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 18)]
		public UInt32 NumberOfFilesRenamed { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 19)]
		public UInt32 NumberOfLinesAddedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 20)]
		public UInt32 NumberOfLinesDeletedByModifiedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 21)]
		public UInt32 NumberOfLinesAddedByRenamedFiles { get; protected internal set; } = 0u;

		[CsvColumn(FieldIndex = 22)]
		public UInt32 NumberOfLinesDeletedByRenamedFiles { get; protected internal set; } = 0u;
	}
}
