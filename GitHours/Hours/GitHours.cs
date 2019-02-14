/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHours. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
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

		public GitCommitSpan GitCommitSpan { get; protected set; }

		/// <summary>
		/// Constructor for the <see cref="GitHours"/> class.
		/// </summary>
		/// <param name="gitCommitSpan">Contains the (time-)span of commits to include.</param>
		/// <param name="maxCommitDiffInMinutes"></param>
		/// <param name="firstCommitAdditionInMinutes"></param>
		public GitHours(GitCommitSpan gitCommitSpan, UInt32? maxCommitDiffInMinutes = null, UInt32? firstCommitAdditionInMinutes = null)
		{
			this.GitCommitSpan = gitCommitSpan;

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
		/// <param name="hoursSpansDetailLevel">Select the designated level of detail for
		/// the hours-spans.</param>
		/// <returns></returns>
		public GitHoursAuthorStat AnalyzeForDeveloper(DeveloperEntity developer, RepositoryEntity repositoryEntity = null, HoursSpansDetailLevel hoursSpansDetailLevel = HoursSpansDetailLevel.Standard)
		{
			var result = this.Analyze(repositoryEntity, hoursSpansDetailLevel);

			return result.AuthorStats.Where(stats => stats.Developer.Equals(developer)).First();
		}

		/// <summary>
		/// Analyzes the <see cref="LibGit2Sharp.Repository"/> and returns the computed hours
		/// for each developer and in total as <see cref="GitHoursAnalysisResult"/>.
		/// </summary>
		/// <param name="repositoryEntity"></param>
		/// <param name="hoursSpansDetailLevel">Select the designated level of detail for
		/// the hours-spans.</param>
		/// <returns></returns>
		public GitHoursAnalysisResult Analyze(RepositoryEntity repositoryEntity = null, HoursSpansDetailLevel hoursSpansDetailLevel = HoursSpansDetailLevel.Standard)
		{
			var commitsByDeveloper = this.GitCommitSpan.FilteredCommits.GroupByDeveloper(repositoryEntity);
			//var commitsByEmail = this.commits.GroupBy(commit => commit.Author?.Email ?? "unknown"); // that's how it's done in the original script
			var authorStats = commitsByDeveloper.Where(commitGroup => commitGroup.Any()).Select(commitGroup =>
			{
				var stats = new GitHoursAuthorStat(commitGroup.Key)
				{
					HoursTotal = this.Estimate(commitGroup.Select(commit => commit.Committer.When.UtcDateTime).ToArray()),
					NumCommits = (UInt32)commitGroup.Count()
				};

				switch (hoursSpansDetailLevel)
				{
					case HoursSpansDetailLevel.Standard:
						stats.HourSpans = GitHoursAuthorSpan.GetHoursSpans(commitGroup, this.Estimate).ToList();
						break;
					case HoursSpansDetailLevel.Detailed:
						stats.HourSpans = GitHoursAuthorSpanDetailed.GetHoursSpans(commitGroup, this.Estimate).Cast<GitHoursAuthorSpan>().ToList();
						break;
				}

				return stats;
			});

			return new GitHoursAnalysisResult
			{
				TotalHours = authorStats.Aggregate(
					0d, (sum, authorStat) => sum + authorStat.HoursTotal),
				TotalCommits = (UInt32)this.GitCommitSpan.FilteredCommits.Count,
				AuthorStats = authorStats.OrderBy(aw => aw.HoursTotal),

				FirstCommitAdditionInMinutes = this.FirstCommitAdditionInMinutes,
				Sha1FirstCommit = this.GitCommitSpan.FilteredCommits.FirstOrDefault()?.Sha,
				Sha1LastCommit = this.GitCommitSpan.FilteredCommits.LastOrDefault()?.Sha,
				MaxCommitDiffInMinutes = this.MaxCommitDiffInMinutes,
				RepositoryPath = this.GitCommitSpan.Repository.Info.WorkingDirectory,
				GitCommitSpan = this.GitCommitSpan
			};
		}

		/// <summary>
		/// Returns the estimated hours for an array of dates as <see cref="DateTime"/>s.
		/// </summary>
		/// <param name="dates"></param>
		/// <returns>An amount of hours as <see cref="Double"/>.</returns>
		protected internal Double Estimate(DateTime[] dates)
		{
			if (dates.Length < 2)
			{
				return 0d;
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

			return totalHours;
		}

		/// <summary>
		/// Returns the estimated hours for an array of dates. The additional out-parameter
		/// will yield an array of detailed estimates as <see cref="EstimateHelper"/> objects.
		/// </summary>
		/// <param name="dates"></param>
		/// <param name="estimates"></param>
		/// <returns>An amount of hours as <see cref="Double"/>.</returns>
		protected internal Double Estimate(DateTime[] dates, out EstimateHelper[] estimates)
		{
			estimates = default(EstimateHelper[]);

			if (dates.Length < 2)
			{
				return 0d;
			}

			estimates = new EstimateHelper[dates.Length - 1];
			var sortedDates = dates.OrderBy(d => d).ToArray();
			var allButLast = sortedDates.Reverse().Skip(1).Reverse().ToList();

			for (var i = 0; i < allButLast.Count; i++)
			{
				var nextDate = sortedDates[i + 1];
				var diffInMinutes = (nextDate - sortedDates[i]).TotalMinutes;
				var isSessionInitial = diffInMinutes > this.MaxCommitDiffInMinutes;

				estimates[i] = new EstimateHelper
				{
					Hours = (isSessionInitial ?
						(double)this.FirstCommitAdditionInMinutes : diffInMinutes) / 60d,
					IsInitialEstimate = isSessionInitial
				};
			}

			return estimates.Select(e => e.Hours).Sum();
		}

		internal class EstimateHelper
		{
			public Double Hours { get; set; }

			public Boolean IsInitialEstimate { get; set; }
		}
	}
}
