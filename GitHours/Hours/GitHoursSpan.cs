/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHours. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
///
/// Permission is hereby granted, free of charge, to any person obtaining a
/// copy of this software and associated documentation files (the "Software"),
/// to deal in the Software without restriction, including without limitation
/// the rights to use, copy, modify, merge, publish, distribute, sublicense,
/// and/or sell copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
/// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
///
/// ---------------------------------------------------------------------------------
///
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitHours.Hours
{
	internal class GitHoursSpan
	{
		/// <summary>
		/// Used for <see cref="Since"/> and <see cref="Until"/>. If such date/times are
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

		private Lazy<IList<Commit>> lazyFilteredCommits;
		/// <summary>
		/// Get a collection of commits to consider according to the since/until values.
		/// </summary>
		[JsonIgnore]
		public IList<Commit> FilteredCommits => this.lazyFilteredCommits.Value;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? SinceDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? UntilDateTime { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String SinceCommitSha { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String UntilCommitSha { get; private set; }

		public GitHoursSpan(Repository repository, String sinceDateTimeOrCommitSha = null, String untilDatetimeOrCommitSha = null)
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
					this.SinceDateTime = DateTime.ParseExact(sinceDateTimeOrCommitSha, GitHoursSpan.DateTimeFormat, ic);
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
					this.UntilDateTime = DateTime.ParseExact(untilDatetimeOrCommitSha, GitHoursSpan.DateTimeFormat, ic);
				}
			}

			this.lazyFilteredCommits = new Lazy<IList<Commit>>(() =>
			{
				var orderedOldToNew = this.Repository.Commits.OrderBy(commit => commit.Author.When).ToList();
				var idxSince = orderedOldToNew.FindIndex(commit =>
				{
					if (this.SinceDateTime.HasValue)
					{
						return commit.Author.When.DateTime >= this.SinceDateTime;
					}
					else if (this.SinceCommitSha != null)
					{
						return commit.Sha.StartsWith(this.SinceCommitSha, StringComparison.InvariantCultureIgnoreCase);
					}

					return false;
				});

				var idxUntil = this.UntilDateTime.HasValue ?
					orderedOldToNew.TakeWhile(commit => commit.Author.When.DateTime <= this.UntilDateTime).Count() - 1
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

				return orderedOldToNew.Skip(idxSince).Take(1 + idxUntil - idxSince).ToList();
			});
		}
	}
}
