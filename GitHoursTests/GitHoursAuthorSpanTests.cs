using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Util.Extensions;

namespace GitHoursTests
{
	[TestClass]
	public class GitHoursAuthorSpanTests
	{
		public static DirectoryInfo SolutionDirectory
			=> new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Locati‌​on)).Parent.Parent.Parent;

		[TestMethod]
		public void TestAggregateHoursStats()
		{
			using (var span = new Util.GitHoursSpan(SolutionDirectory.FullName.OpenRepository(), (String)null, (String)null))
			{
				var analysis = new GitHours.Hours.GitHours(span);
				var result = analysis.Analyze(hoursSpansDetailLevel: GitHours.Hours.HoursSpansDetailLevel.Standard);

				Assert.AreEqual(span.FilteredCommits.Count,
					result.AuthorStats.Select(@as => @as.HourSpans.Count).Sum());

				foreach (var stat in result.AuthorStats)
				{
					Assert.AreEqual(stat.HoursTotalOriginal,
						stat.HourSpans.Select(hs => hs.HoursOriginal).Sum(), 0.000001d);
				}
			}
		}
	}
}
