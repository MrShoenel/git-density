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
using System;
using System.Collections.Generic;
using Util;

namespace GitTools.Analysis.SimpleAnalyzer
{
	/// <summary>
	/// An implementation of <see cref="IAnalyzer{T}"/> that maps each
	/// commit to an instance of <see cref="SimpleCommitDetails"/>.
	/// </summary>
	public class SimpleAnalyzer : BaseAnalyzer<SimpleCommitDetails>
	{
		/// <summary>
		/// This is a forwarding constructor that does not do any other
		/// initialization than <see cref="BaseAnalyzer{T}.BaseAnalyzer(string, GitCommitSpan)"/>.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		public SimpleAnalyzer(String repoPathOrUrl, GitCommitSpan span)
			: base(repoPathOrUrl, span)
		{
		}

		/// <summary>
		/// Maps each <see cref="Commit"/> to a <see cref="SimpleCommitDetails"/>.
		/// </summary>
		/// <returns><see cref="IEnumerable{SimpleCommitDetails}"/></returns>
		public override IEnumerable<SimpleCommitDetails> AnalyzeCommits()
		{
			var repo = this.GitCommitSpan.Repository;

			foreach (var commit in this.GitCommitSpan)
			{
				yield return new SimpleCommitDetails(this.RepoPathOrUrl, repo, commit);
			}
		}
	}
}
