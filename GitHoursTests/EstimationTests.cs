/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHoursTests. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Extensions;
using static GitHours.Hours.GitHours;

namespace GitHoursTests
{
	[TestClass]
	public class EstimationTests
	{
		[TestMethod]
		public void TestEstimatesWithHelper()
		{
			using (var span = new Util.GitCommitSpan(GitHoursAuthorSpanTests.SolutionDirectory.FullName.OpenRepository(), (String)null, (String)null))
			{
				var analysis = new GitHours.Hours.GitHours(span);

				var dates = span.Repository.Commits.Select(c => c.Committer.When.UtcDateTime).ToArray();
				var original = analysis.Estimate(dates);
				var withHelper = analysis.Estimate(dates, out EstimateHelper[] estimates);

				Assert.AreEqual(original, withHelper, 0.000001d);
				Assert.AreEqual(original, estimates.Select(e => e.Hours).Sum(), 0.000001d);
			}
		}
	}
}
