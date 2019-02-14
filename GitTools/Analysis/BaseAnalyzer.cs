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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Util;
using Util.Data.Entities;
using Util.Extensions;
using static Util.Extensions.RepositoryExtensions;
using Signature = LibGit2Sharp.Signature;

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

		private IReadOnlyDictionary<Signature, DeveloperWithAlternativeNamesAndEmails> authorSignatures;

		private IReadOnlyDictionary<Signature, DeveloperWithAlternativeNamesAndEmails> committerSignatures;

		private IReadOnlyDictionary<DeveloperEntity, String> developerNominals;

		/// <summary>
		/// Base constructor that initalizes the repo-path and commits-span.
		/// </summary>
		/// <param name="repoPathOrUrl"></param>
		/// <param name="span"></param>
		public BaseAnalyzer(String repoPathOrUrl, GitCommitSpan span)
		{
			this.RepoPathOrUrl = repoPathOrUrl;
			this.GitCommitSpan = span;
			this.InitializeNominalSignatures();
		}

		#region Nominal Signatures
		/// <summary>
		/// Will map each <see cref="Signature"/> uniquely to a <see cref="DeveloperEntity"/>.
		/// Then, for each entity, a unique ID (within the current repository) is assigned. The
		/// IDs look like Excel columns.
		/// </summary>
		private void InitializeNominalSignatures()
		{
			this.Logger.LogInformation("Initializing developer identities for repository..");

			this.developerNominals = new Dictionary<DeveloperEntity, String>();

			this.authorSignatures = new ReadOnlyDictionary<Signature, DeveloperWithAlternativeNamesAndEmails>(
				this.GitCommitSpan.GroupByDeveloperAsSignatures(
					repository: null, useAuthorAndNotCommitter: true));
			this.committerSignatures = new ReadOnlyDictionary<Signature, DeveloperWithAlternativeNamesAndEmails>(
				this.GitCommitSpan.GroupByDeveloperAsSignatures(
					repository: null, useAuthorAndNotCommitter: false));

			Func<Int32, String> excelColumnFromNumber = num =>
			{
				var colLetters = new LinkedList<Char>();
				while (num > 0)
				{
					var currentLetterNum = (num - 1) % 26;
					var currentLetter = (char)(currentLetterNum + 65);
					colLetters.AddLast(currentLetter);
					num = (num - (currentLetterNum + 1)) / 26;
				}
				return new string(colLetters.Reverse().ToArray()).ToUpper();
			};

			var set = new HashSet<DeveloperEntity>(this.authorSignatures.Select(kv => kv.Value)
				.Concat(this.committerSignatures.Select(kv => kv.Value)));

			this.developerNominals = new ReadOnlyDictionary<DeveloperEntity, String>(
				set.Select((dev, idx) => new {
					Dev = dev,
					Id = idx + 1
				}).ToDictionary(kv => kv.Dev, kv => excelColumnFromNumber(kv.Id))
			);

			this.Logger.LogInformation($"Found {String.Format("{0:n0}", this.authorSignatures.Count + this.committerSignatures.Count)} signatures and mapped them to {String.Format("{0:n0}", this.developerNominals.Count)} distinct developer identities.");
		}

		/// <summary>
		/// Returns a unique nominal ID for a <see cref="Commit.Author"/> and
		/// <see cref="Commit.Committer"/> (<see cref="InitializeNominalSignatures"/>).
		/// </summary>
		/// <param name="commit"></param>
		/// <param name="authorNominal"></param>
		/// <param name="committerNominal"></param>
		protected void AuthorAndCommitterNominalForCommit(Commit commit, out string authorNominal, out string committerNominal)
		{
			// TODO: Refactor this to use the SHA256 and remove obsolete code in InitializeNominalSignatures
			authorNominal = this.developerNominals[this.authorSignatures[commit.Author]];
			committerNominal = this.developerNominals[this.committerSignatures[commit.Committer]];
		}
		#endregion

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
