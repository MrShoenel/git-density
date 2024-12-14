/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using Iesi.Collections.Generic;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Util.Extensions;
using Util.Logging;

namespace Util
{
    /// <summary>
    /// When using since and/or until in a <see cref="GitCommitSpan"/> or other supporting
    /// types, the <see cref="DateTime"/> can be extracted from either the author or the
    /// commiter, as a <see cref="Commit"/> has two signatures, <see cref="Commit.Author"/>
    /// and <see cref="Commit.Committer"/>.
    /// </summary>
    public enum SinceUntilUseDate
	{
		/// <summary>
		/// Use the <see cref="DateTime"/> of when the commit was authored.
		/// </summary>
		Author = 1,

		/// <summary>
		/// Use the <see cref="DateTime"/> of when then commit was incorporated (merged, rebased,
		/// cherry-picked etc.).
		/// </summary>
		Committer = 2
	}


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
		/// Max-length was increased to 64 characters, to support SHA256 hashes.
		/// </summary>
		public static readonly Regex RegexShaCommitish = new Regex("^([a-f0-9]{3,64})$", RegexOptions.IgnoreCase);

		/// <summary>
		/// The <see cref="Repository"/> to get the <see cref="Commit"/>s from.
		/// </summary>
		[JsonIgnore]
		public Repository Repository { get; private set; }

		private Lazy<LinkedList<Commit>> lazyAllCommits;

		/// <summary>
		/// Gets a collection with all of the wrapped repository's commits (i.e. no
		/// filters or since/until is applied here). The underlying linked list is
		/// ordered by <see cref="LibGit2Sharp.Signature.When"/>'s utc time, oldest
		/// to newest.
		/// </summary>
		[JsonIgnore]
		public LinkedList<Commit> AllCommits => this.lazyAllCommits.Value;

		private Lazy<LinkedList<Commit>> lazyFilteredCommits;
		/// <summary>
		/// Gets a collection of commits to consider according to the since/until values.
		/// When using this <see cref="GitCommitSpan"/> as <see cref="IEnumerable{Commit}"/>,
		/// an <see cref="IEnumerator{Commit}"/> of the filtered commits is returned.
		/// </summary>
		[JsonIgnore]
		public LinkedList<Commit> FilteredCommits => this.lazyFilteredCommits.Value;

		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 1)]
		public DateTime? SinceDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 3)]
		public DateTime? UntilDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 2)]
		public String SinceCommitSha { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 4)]
		public String UntilCommitSha { get; private set; }

		[JsonProperty(Order = 5)]
		public SinceUntilUseDate SinceUseDate { get; private set; } = SinceUntilUseDate.Committer;

		[JsonProperty(Order = 6)]
		public SinceUntilUseDate UntilUseDate { get; private set; } = SinceUntilUseDate.Committer;

		[JsonIgnore]
		public String SinceAsString =>
			this.SinceDateTime.HasValue ? this.SinceDateTime.ToString() :
				"#" + this.SinceCommitSha.Substring(0, Math.Min(this.SinceCommitSha.Length, 8));

		[JsonIgnore]
		public String UntilAsString =>
			this.UntilDateTime.HasValue ? this.UntilDateTime.ToString() :
				"#" + this.UntilCommitSha.Substring(0, Math.Min(this.UntilCommitSha.Length, 8));

		/// <summary>
		/// Can be set to limit the amount of commits that are returned. When combined
		/// with since/until, this can be a powerful tool to retrieve a certain amount
		/// of commits using offsets. Note that the limit is applied after the filter.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 7)]
		public UInt32? Limit { get; private set; }

		[JsonIgnore]
		private readonly ReadOnlySet<String> sha1Filter;
		/// <summary>
		/// A set of SHA1 that can be set to limit this <see cref="GitCommitSpan"/> even
		/// beyond any since/until constraints. Defaults to an empty set.
		/// </summary>
		[JsonProperty(NullValueHandling = NullValueHandling.Include, Order = 8)]
		public ReadOnlySet<String> SHA1Filter => this.sha1Filter;

		/// <summary>
		/// Constructs a new <see cref="GitCommitSpan"/> using two <see cref="Commit"/>s
		/// to delimit the range. Both <see cref="Commit"/>s will be included in the span.
		/// </summary>
		/// <see cref="GitCommitSpan(Repository, string, string, UInt32?, ISet{String})"/>.
		/// <param name="repository"></param>
		/// <param name="sinceCommit"></param>
		/// <param name="untilCommit"></param>
		/// <param name="limit"></param>
		/// <param name="sha1IDs"></param>
		public GitCommitSpan(Repository repository, Commit sinceCommit, Commit untilCommit, UInt32? limit = null, ISet<String> sha1IDs = null)
			: this(repository, sinceCommit.Sha, untilCommit.Sha, limit, sha1IDs)
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
		/// inclusive start of the span. Defaults to null. If null, will assume
		/// <see cref="DateTime.MinValue"/> for <see cref="SinceDateTime"/>.</param>
		/// <param name="untilDatetimeOrCommitSha">(Partial) SHA1 of commit or parseable
		/// date/time (according to <see cref="DateTimeFormat"/>). This offset is the
		/// inclusive end of the span. Defaults to null. If null, will assume
		/// <see cref="DateTime.MaxValue"/> for <see cref="UntilDateTime"/>.</param>
		/// <param name="limit"></param>
		/// <param name="sha1IDs"></param>
		/// <param name="sinceUseDate"></param>
		/// <param name="untilUseDate"></param>
		public GitCommitSpan(Repository repository, String sinceDateTimeOrCommitSha = null, String untilDatetimeOrCommitSha = null, UInt32? limit = null, ISet<String> sha1IDs = null, SinceUntilUseDate? sinceUseDate = null, SinceUntilUseDate? untilUseDate = null)
		{
			this.Repository = repository;
			this.Limit = limit;
			this.sha1Filter = new ReadOnlySet<String>(new HashSet<String>(
				(sha1IDs ?? Enumerable.Empty<String>()).Select(v => v.Trim().ToLower())));

			if (sinceUseDate.HasValue)
			{
				this.SinceUseDate = sinceUseDate.Value;
			}
			if (untilUseDate.HasValue)
			{
				this.UntilUseDate = untilUseDate.Value;
			}

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
					this.SinceDateTime = DateTime.ParseExact(sinceDateTimeOrCommitSha, GitCommitSpan.DateTimeFormat, ic).ToUniversalTime();
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
					this.UntilDateTime = DateTime.ParseExact(untilDatetimeOrCommitSha, GitCommitSpan.DateTimeFormat, ic).ToUniversalTime();
				}
			}

            var singleCommit = this.SinceCommitSha != null && this.SinceCommitSha == this.UntilCommitSha;

            this.lazyAllCommits = new Lazy<LinkedList<Commit>>(() =>
			{
				return new LinkedList<Commit>(
					this.Repository.GetAllCommits()
						.OrderBy(commit => commit.Committer.When.UtcDateTime));
			});

			this.lazyFilteredCommits = new Lazy<LinkedList<Commit>>(() =>
			{
				IEnumerable<Commit> ie;
				if (this.sha1Filter.Count > 0)
				{
					// Only return the commits used in the filter.
					ie = this.Repository.LookupAny<Commit>(this.sha1Filter);
				}
				else
				{
					ie = this.AllCommits;
				}
				if (this.Limit.HasValue)
				{
					ie = ie.Take((Int32)this.Limit.Value);
				}

				Func<Commit, DateTimeOffset> authorSelector = c => c.Author.When,
					committerSelector = c => c.Committer.When;

				var signatureSinceWhenSelector = this.SinceUseDate == SinceUntilUseDate.Author ?
					authorSelector : committerSelector;
				var signatureUntilWhenSelector = this.UntilUseDate == SinceUntilUseDate.Author ?
					authorSelector : committerSelector;

				var orderedOldToNew = new List<Commit>(ie);

				var temp = orderedOldToNew.Select((commit, idx) => new { C = commit, I = idx }).Where(tpl =>
                {
                    if (this.SinceDateTime.HasValue)
                    {
                        return signatureSinceWhenSelector(tpl.C).UtcDateTime >= this.SinceDateTime;
                    }
                    else if (this.SinceCommitSha != null)
                    {
                        return tpl.C.Sha.StartsWith(this.SinceCommitSha, StringComparison.InvariantCultureIgnoreCase);
                    }

                    return false;
                }).ToList();
				if (singleCommit)
				{
					if (temp.Count == 0)
					{
						throw new ArgumentException($"No single commit with ID {this.SinceCommitSha} was found. Check for typos.");
					}
					else if (temp.Count > 1)
					{
                        throw new InvalidOperationException($"A single commit with ID {this.SinceCommitSha} was requested, but a total of {temp.Count} commits matching this ID were found: {String.Join(", ", temp.Select(tpl => tpl.C.ShaShort(length: 15)))}. You need to provide longer hashes in order to uniquely identify commits.");
                    }
				}

				var idxSince = temp[0].I;
				var idxUntil = this.UntilDateTime.HasValue ?
					orderedOldToNew.TakeWhile(commit => signatureUntilWhenSelector(commit).UtcDateTime <= this.UntilDateTime).Count() - 1
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
		/// Returns an enumerator for <see cref="FilteredCommits"/>. This enumerator may
		/// be further limited if <see cref="SHA1Filter"/> and <see cref="Limit"/> were
		/// applied. Note that any additional limitation will result in fewer elements
		/// returned, respectively.
		/// </summary>
		/// <returns>An <see cref="IEnumerator{Commit}"/></returns>
		public IEnumerator<Commit> GetEnumerator()
		{
			return this.FilteredCommits.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator for <see cref="FilteredCommits"/>. This is a wrapper
		/// and returns the enumerator from <see cref="GetEnumerator"/>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
	}
}
