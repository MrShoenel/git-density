/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHoursTests. All files in this project,
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
