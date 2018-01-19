﻿/// ---------------------------------------------------------------------------------
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

		[JsonProperty(Order = 3)]
		public Double HoursTotal { get; set; }

		[JsonProperty(Order = 4)]
		public UInt32 NumCommits { get; set; }

		[JsonProperty(Order = 7, NullValueHandling = NullValueHandling.Ignore)]
		public IList<GitHoursAuthorSpan> HourSpans { get; set; }
	}
}
