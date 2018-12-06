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

namespace GitTools.Analysis
{
	/// <summary>
	/// Represents basic details about one <see cref="Commit"/> in a
	/// <see cref="Repository"/>.
	/// </summary>
	public class SimpleCommitDetails : IAnalyzedCommit
	{
		protected readonly Repository repository;

		protected readonly Commit commit;

		public SimpleCommitDetails(String repoPathOrUrl, Repository repository, Commit commit)
		{
			this.RepoPathOrUrl = repoPathOrUrl;
			this.repository = repository;
			this.commit = commit;
		}

		[CsvColumn(FieldIndex = 1)]
		public String RepoPathOrUrl { get; protected set; }

		[CsvColumn(FieldIndex = 2)]
		public String SHA1 => this.commit.Sha;

		[CsvColumn(FieldIndex = 3)]
		public String AuthorName => this.commit.Author.Name;

		[CsvColumn(FieldIndex = 4)]
		public String CommitterName => this.commit.Committer.Name;

		[CsvColumn(FieldIndex = 5)]
		public DateTime AuthorTime => this.commit.Author.When.DateTime;

		[CsvColumn(FieldIndex = 6)]
		public DateTime CommitterTime => this.commit.Committer.When.DateTime;
	}
}
