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
			using (var span = new Util.GitHoursSpan(GitHoursAuthorSpanTests.SolutionDirectory.FullName.OpenRepository(), (String)null, (String)null))
			{
				var analysis = new GitHours.Hours.GitHours(span);

				var dates = span.Repository.Commits.Select(c => c.Author.When.DateTime).ToArray();
				var original = analysis.Estimate(dates);
				var withHelper = analysis.Estimate(dates, out EstimateHelper[] estimates);

				Assert.AreEqual(original, withHelper, 0.000001d);
				Assert.AreEqual(original, estimates.Select(e => e.Hours).Sum(), 0.000001d);
			}
		}
	}
}
