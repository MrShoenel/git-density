/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Util.Extensions;

namespace Util
{
	/// <summary>
	/// Represents a segment that spans from a point in time or <see cref="Commit"/>
	/// to another point in time or <see cref="Commit"/> and thus represents a range
	/// of <see cref="Commit"/>s.
	/// </summary>
	public class GitCommitSpan : IDisposable, IEnumerable<Commit>
	{
		/// <summary>
		/// Used for parsing the date/time, if given as string. If such date/times are
		/// supplied, they must match this format exactly (24 hours format).
		/// </summary>
		public static readonly String DateTimeFormat = "yyyy-MM-dd HH:mm";

		/// <summary>
		/// <see cref="Regex"/> to detect/parse an SHA1 from a <see cref="Commit"/>.
		/// </summary>
		public static readonly Regex RegexShaCommitish = new Regex("^([a-f0-9]{3,40})$", RegexOptions.IgnoreCase);

		/// <summary>
		/// The <see cref="Repository"/> to get the <see cref="Commit"/>s from.
		/// </summary>
		[JsonIgnore]
		public Repository Repository { get; private set; }

		private Lazy<LinkedList<Commit>> lazyFilteredCommits;
		/// <summary>
		/// Get a collection of commits to consider according to the since/until values.
		/// </summary>
		[JsonIgnore]
		public LinkedList<Commit> FilteredCommits => this.lazyFilteredCommits.Value;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 1)]
		public DateTime? SinceDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 3)]
		public DateTime? UntilDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 2)]
		public String SinceCommitSha { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore, Order = 4)]
		public String UntilCommitSha { get; private set; }

		[JsonIgnore]
		public String SinceAsString =>
			this.SinceDateTime.HasValue ? this.SinceDateTime.ToString() :
				"#" + this.SinceCommitSha.Substring(0, Math.Min(this.SinceCommitSha.Length, 8));

		[JsonIgnore]
		public String UntilAsString =>
			this.UntilDateTime.HasValue ? this.UntilDateTime.ToString() :
				"#" + this.UntilCommitSha.Substring(0, Math.Min(this.UntilCommitSha.Length, 8));

		/// <summary>
		/// Constructs a new <see cref="GitCommitSpan"/> using two <see cref="Commit"/>s
		/// to delimit the range. Both <see cref="Commit"/>s will be included in the span.
		/// </summary>
		/// <see cref="GitCommitSpan(Repository, string, string)"/>.
		/// <param name="repository"></param>
		/// <param name="sinceCommit"></param>
		/// <param name="untilCommit"></param>
		public GitCommitSpan(Repository repository, Commit sinceCommit, Commit untilCommit)
			: this(repository, sinceCommit.Sha, untilCommit.Sha)
		{
		}

		/// <summary>
		/// Constructs a new <see cref="GitCommitSpan"/> using two <see cref="String"/>s,
		/// where each of these can represent a (partial) SHA1 of commit's hash or a properly
		/// formatted date and time.
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="sinceDateTimeOrCommitSha">(Partial) SHA1 of commit or parseable
		/// date/time (according to <see cref="DateTimeFormat"/>). This offset is the
		/// inclusive start of the span.</param>
		/// <param name="untilDatetimeOrCommitSha">(Partial) SHA1 of commit or parseable
		/// date/time (according to <see cref="DateTimeFormat"/>). This offset is the
		/// inclusive end of the span.</param>
		public GitCommitSpan(Repository repository, String sinceDateTimeOrCommitSha = null, String untilDatetimeOrCommitSha = null)
		{
			this.Repository = repository;
			var ic = CultureInfo.InvariantCulture;

			if (sinceDateTimeOrCommitSha == null)
			{
				this.SinceDateTime = DateTime.MinValue;
				this.SinceCommitSha = null;
			}
			else
			{
				if (RegexShaCommitish.IsMatch(sinceDateTimeOrCommitSha))
				{
					this.SinceCommitSha = sinceDateTimeOrCommitSha;
				}
				else
				{
					this.SinceDateTime = DateTime.ParseExact(sinceDateTimeOrCommitSha, GitCommitSpan.DateTimeFormat, ic);
				}
			}

			if (untilDatetimeOrCommitSha == null)
			{
				this.UntilDateTime = DateTime.MaxValue;
				this.UntilCommitSha = null;
			}
			else
			{
				if (RegexShaCommitish.IsMatch(untilDatetimeOrCommitSha))
				{
					this.UntilCommitSha = untilDatetimeOrCommitSha;
				}
				else
				{
					this.UntilDateTime = DateTime.ParseExact(untilDatetimeOrCommitSha, GitCommitSpan.DateTimeFormat, ic);
				}
			}

			this.lazyFilteredCommits = new Lazy<LinkedList<Commit>>(() =>
			{
				var orderedOldToNew = this.Repository.GetAllCommits().OrderBy(commit => commit.Committer.When).ToList();
				var idxSince = orderedOldToNew.FindIndex(commit =>
				{
					if (this.SinceDateTime.HasValue)
					{
						return commit.Committer.When.DateTime >= this.SinceDateTime;
					}
					else if (this.SinceCommitSha != null)
					{
						return commit.Sha.StartsWith(this.SinceCommitSha, StringComparison.InvariantCultureIgnoreCase);
					}

					return false;
				});

				var idxUntil = this.UntilDateTime.HasValue ?
					orderedOldToNew.TakeWhile(commit => commit.Committer.When.DateTime <= this.UntilDateTime).Count() - 1
					:
					orderedOldToNew.FindIndex(commit => commit.Sha.StartsWith(this.UntilCommitSha, StringComparison.InvariantCultureIgnoreCase));

				if (idxSince < 0 || idxUntil < 0)
				{
					throw new IndexOutOfRangeException("Cannot use the supplied Since/Until values as valid delimiters");
				}

				if (idxUntil < idxSince)
				{
					throw new InvalidOperationException($"The parameter for 'until' points to a commit that comes before the one identified by the parameter 'since'.");
				}

				return new LinkedList<Commit>(orderedOldToNew.Skip(idxSince).Take(1 + idxUntil - idxSince));
			});
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.lazyFilteredCommits.Value.Clear();
					this.lazyFilteredCommits = null;
					this.Repository = null;
					this.SinceCommitSha = null;
					this.SinceDateTime = null;
					this.UntilCommitSha = null;
					this.UntilDateTime = null;
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			Dispose(true);

		}
		#endregion

		#region IEnumerable Support
		/// <summary>
		/// Returns an enumerator for <see cref="FilteredCommits"/>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator{Commit}"/></returns>
		public IEnumerator<Commit> GetEnumerator()
		{
			return this.FilteredCommits.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator for <see cref="FilteredCommits"/>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
