/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
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
using static Util.Extensions.RepositoryExtensions;

namespace GitHours.Hours
{
	/// <summary>
	/// Data-class that holds, for each author, stats about hours spent and commits submitted.
	/// </summary>
	internal class GitHoursAuthorStat
	{
		public GitHoursAuthorStat(DeveloperWithAlternativeNamesAndEmails developer)
		{
			this.Developer = developer;
		}

		[JsonIgnore]
		public DeveloperWithAlternativeNamesAndEmails Developer { get; private set; }

		[JsonProperty(Order = 1)]
		public String Name { get { return this.Developer.Name; } }

		[JsonProperty(Order = 5)]
		public IReadOnlyCollection<String> AlternativeNames { get { return this.Developer.AlternativeNames; } }

		[JsonProperty(Order = 2)]
		public String Email { get { return this.Developer.Email; } }

		[JsonProperty(Order = 6)]
		public IReadOnlyCollection<String> AlternativeEmails { get { return this.Developer.AlternativeEmails; } }

		/// <summary>
		/// When serializing to JSON, we will use the rounded and prettier looking value
		/// as obtained from <see cref="HoursTotal"/>. This value is only kept for when
		/// the actual, unrounded value is needed.
		/// </summary>
		[JsonIgnore]
		public Double HoursTotalOriginal { get; private set; }

		[JsonIgnore]
		private Double hoursTotal;
		[JsonProperty(Order = 3)]
		public Double HoursTotal
		{
			get => this.hoursTotal;
			set
			{
				this.HoursTotalOriginal = value;
				this.hoursTotal = Math.Round(value, 2);
			}
		}

		[JsonProperty(Order = 4)]
		public UInt32 NumCommits { get; set; }

		[JsonProperty(Order = 7, NullValueHandling = NullValueHandling.Ignore)]
		public IList<GitHoursAuthorSpan> HourSpans { get; set; }
	}
}
