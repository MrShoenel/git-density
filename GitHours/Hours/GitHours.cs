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
using System;
using System.Linq;
using Util;
using Util.Data.Entities;
using Util.Extensions;

namespace GitHours.Hours
{
	public enum HoursSpansDetailLevel : Int32
	{
		/// <summary>
		/// Choose this if you wish not to include hours-spans in the analysis.
		/// </summary>
		None = 1,
		/// <summary>
		/// Choose this to get information about hours-spans, such as the commit-
		/// pair and the amount of hours in between them.
		/// </summary>
		Standard = 2,
		/// <summary>
		/// Choose this option, to return <see cref="GitHoursAuthorSpanDetailed"/>
		/// entities, rather than <see cref="GitHoursAuthorSpan"/> entities. The
		/// former contain more details w.r.t. initial span states.
		/// </summary>
		Detailed = 3
	}

	/// <summary>
	/// Implements the main functionality from this script:
	/// <see cref="https://github.com/kimmobrunfeldt/git-hours/blob/master/src/index.js"/>
	/// </summary>
	internal class GitHours
	{
		public UInt32 MaxCommitDiffInMinutes { get; protected set; } = 2 * 60;

		public UInt32 FirstCommitAdditionInMinutes { get; protected set; } = 2 * 60;

		public GitHoursSpan GitHoursSpan { get; protected set; }

		/// <summary>
		/// Constructor for the <see cref="GitHours"/> class.
		/// </summary>
		/// <param name="gitHoursSpan">Contains the (time-)span of commits to include.</param>
		/// <param name="maxCommitDiffInMinutes"></param>
		/// <param name="firstCommitAdditionInMinutes"></param>
		public GitHours(GitHoursSpan gitHoursSpan, UInt32? maxCommitDiffInMinutes = null, UInt32? firstCommitAdditionInMinutes = null)
		{
			this.GitHoursSpan = gitHoursSpan;

			if (maxCommitDiffInMinutes.HasValue)
			{
				this.MaxCommitDiffInMinutes = maxCommitDiffInMinutes.Value;
			}
			if (firstCommitAdditionInMinutes.HasValue)
			{
				this.FirstCommitAdditionInMinutes = firstCommitAdditionInMinutes.Value;
			}
		}

		/// <summary>
		/// Conducts a full git-hours analysis and returns the <see cref="GitHoursAuthorStat"/>
		/// for the developer that was supplied.
		/// </summary>
		/// <param name="developer"></param>
		/// <param name="repositoryEntity"></param>
		/// <param name="includeHourSpans">If true, will compute and include the hour-spans.</param>
		/// <returns></returns>
		public GitHoursAuthorStat AnalyzeForDeveloper(DeveloperEntity developer, RepositoryEntity repositoryEntity = null, Boolean includeHourSpans = true)
		{
			var result = this.Analyze(repositoryEntity, includeHourSpans);

			return result.AuthorStats.Where(stats => stats.Developer.Equals(developer)).First();
		}

		/// <summary>
		/// Analyzes the <see cref="LibGit2Sharp.Repository"/> and returns the computed hours
		/// for each developer and in total as <see cref="GitHoursAnalysisResult"/>.
		/// </summary>
		/// <param name="repositoryEntity"></param>
		/// <param name="includeHourSpans">If true, will compute and include the hour-spans.</param>
		/// <returns></returns>
		public GitHoursAnalysisResult Analyze(RepositoryEntity repositoryEntity = null, Boolean includeHourSpans = true)
		{
			var commitsByDeveloper = this.GitHoursSpan.FilteredCommits.GroupByDeveloper(repositoryEntity);
			//var commitsByEmail = this.commits.GroupBy(commit => commit.Author?.Email ?? "unknown"); // that's how it's done in the original script
			var authorStats = commitsByDeveloper.Where(commitGroup => commitGroup.Any()).Select(commitGroup =>
			{
				var stats = new GitHoursAuthorStat(commitGroup.Key)
				{
					HoursTotal = this.Estimate(commitGroup.Select(commit => commit.Committer.When.DateTime).ToArray()),
					NumCommits = (UInt32)commitGroup.Count()
				};

				if (includeHourSpans)
				{
					stats.HourSpans = GitHoursAuthorSpan.GetHoursSpans(commitGroup, this.Estimate).ToList();
					var test = stats.HourSpans.Select(hs => hs.Hours).Sum();
				}

				return stats;
			});

			return new GitHoursAnalysisResult
			{
				TotalHours = Math.Round(authorStats.Aggregate(0d, (sum, authorStat) => sum + authorStat.HoursTotal), 2),
				TotalCommits = (UInt32)this.GitHoursSpan.FilteredCommits.Count,
				AuthorStats = authorStats.OrderBy(aw => aw.HoursTotal),

				FirstCommitAdditionInMinutes = this.FirstCommitAdditionInMinutes,
				Sha1FirstCommit = this.GitHoursSpan.FilteredCommits.FirstOrDefault()?.Sha,
				Sha1LastCommit = this.GitHoursSpan.FilteredCommits.LastOrDefault()?.Sha,
				MaxCommitDiffInMinutes = this.MaxCommitDiffInMinutes,
				RepositoryPath = this.GitHoursSpan.Repository.Info.WorkingDirectory,
				GitHoursSpan = this.GitHoursSpan
			};
		}

		/// <summary>
		/// Returns the estimated hours for an array of dates as <see cref="DateTime"/>s.
		/// </summary>
		/// <param name="dates"></param>
		/// <returns>An amount of hours as <see cref="Double"/>.</returns>
		protected Double Estimate(DateTime[] dates)
		{
			if (dates.Length < 2)
			{
				return 0;
			}

			var sortedDates = dates.OrderBy(d => d).ToArray();
			var allButLast = sortedDates.Reverse().Skip(1).Reverse().ToList();

			var index = 0;
			var totalHours = allButLast.Aggregate(0d, (hours, date) =>
			{
				var nextDate = sortedDates[index + 1];
				index++;
				var diffInMinutes = (nextDate - date).TotalMinutes;

				if (diffInMinutes < this.MaxCommitDiffInMinutes)
				{
					return hours + (double)diffInMinutes / 60d;
				}

				return hours + (double)this.FirstCommitAdditionInMinutes / 60d;
			});

			return Math.Round(totalHours, 2);
		}
	}
}
