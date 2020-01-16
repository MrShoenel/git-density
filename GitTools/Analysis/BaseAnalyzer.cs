/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Util;

namespace GitTools.Analysis
{
	/// <summary>
	/// A class to serve as base for all analyzers in the project
	/// <see cref="GitTools"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class BaseAnalyzer<T> :
		IAnalyzer<T>
		where T : IAnalyzedCommit
	{
		protected abstract ILogger<IAnalyzer<T>> Logger { get; }

		/// <summary>
		/// A path or URL to the <see cref="Repository"/> that is being
		/// analyzed.
		/// </summary>
		public String RepoPathOrUrl { get; protected set; }

		/// <summary>
		/// A <see cref="GitCommitSpan"/> that delimits the range of
		/// <see cref="Commit"/>s to analyze.
		/// </summary>
		public GitCommitSpan GitCommitSpan { get; protected set; }

		/// <summary>
		/// Base constructor that initalizes the repo-path and commits-span.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		public BaseAnalyzer(String repoPathOrUrl, GitCommitSpan span)
		{
			this.RepoPathOrUrl = repoPathOrUrl;
			this.GitCommitSpan = span;
		}

		#region ISupportsExecutionPolicy
		/// <summary>
		/// An analyzer may do heavy work and its execution-policy
		/// can be get/set using this property. Defaults to
		/// <see cref="ExecutionPolicy.Parallel"/>.
		/// </summary>
		public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;
		#endregion

		#region IAnalyzer<T>
		/// <summary>
		/// Needs to be overridden in sub-classes.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<T> AnalyzeCommits();
		#endregion
	}
}
