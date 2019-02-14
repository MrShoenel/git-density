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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Util;

namespace GitHours.Hours
{
	/// <summary>
	/// Data-class that holds the full analysis-result for time spent on a
	/// <see cref="LibGit2Sharp.Repository"/> and also per-author stats.
	/// </summary>
	internal class GitHoursAnalysisResult
	{
		/// <summary>
		/// The precise value, not rounded. When serializing to JSON, the other,
		/// prettier property <see cref="TotalHours"/> is used.
		/// </summary>
		[JsonIgnore]
		public Double TotalHoursOriginal { get; private set; }

		[JsonIgnore]
		private Double totalHours;
		/// <summary>
		/// Represents the total amount of hours, rounded to 2 decimal places.
		/// </summary>
		public Double TotalHours
		{
			get => this.totalHours;
			protected internal set
			{
				this.TotalHoursOriginal = value;
				this.totalHours = Math.Round(value, 2);
			}
		}

		public UInt32 TotalCommits { get; protected internal set; }

		public UInt32 MaxCommitDiffInMinutes { get; protected internal set; }

		public UInt32 FirstCommitAdditionInMinutes { get; protected internal set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String Sha1FirstCommit { get; protected internal set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public String Sha1LastCommit { get; protected internal set; }

		public GitCommitSpan GitCommitSpan { get; protected internal set; }

		public String RepositoryPath { get; protected internal set; }

		public IEnumerable<GitHoursAuthorStat> AuthorStats { get; protected internal set; }
	}
}
