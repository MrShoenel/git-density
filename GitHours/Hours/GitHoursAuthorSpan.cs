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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHours.Hours
{
	internal class GitHoursAuthorSpan
	{
		[JsonIgnore]
		public Commit Since { get; private set; }

		public String SinceId => this.Since.Sha.Substring(0, 8);

		[JsonIgnore]
		public Commit Until { get; private set; }

		public String UntilId => this.Until.Sha.Substring(0, 8);

		public Double Hours { get; private set; }

		private GitHoursAuthorSpan(Commit since, Commit until, double hours)
		{
			this.Since = since;
			this.Until = until;
			this.Hours = hours;
		}

		public static IEnumerable<GitHoursAuthorSpan> GetHoursSpans(IEnumerable<Commit> commitsForDeveloper, Func<DateTime[], Double> estimator)
		{
			var commitsSorted = commitsForDeveloper.OrderBy(commit => commit.Author.When).ToList();

			if (commitsSorted.Count < 2)
			{
				yield break; // Cannot return any span
			}

			var hoursUntilCommit = new Dictionary<Int32, Tuple<Commit, Double>>();
			for (var take = 2; take <= commitsSorted.Count; take++)
			{
				hoursUntilCommit[take] = Tuple.Create(
					commitsSorted[take - 1], estimator(commitsSorted.Take(take).Select(commit => commit.Committer.When.DateTime).ToArray()));
			}

			foreach (var kv in hoursUntilCommit)
			{
				var hours = kv.Value.Item2 - (hoursUntilCommit.ContainsKey(kv.Key - 1) ?  hoursUntilCommit[kv.Key - 1].Item2 : 0);
				yield return new GitHoursAuthorSpan(commitsSorted[kv.Key - 2], kv.Value.Item1, Math.Round(hours, 3));
			}
		}
	}
}
