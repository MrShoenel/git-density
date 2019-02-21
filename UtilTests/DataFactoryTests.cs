/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project UtilTests. All files in this project,
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
using Util.Data;
using Util.Logging;

namespace UtilTests
{
	[TestClass]
	public class DataFactoryTests
	{
		/// <summary>
		/// The point of this test is to validate the integrity and correct
		/// implementation of all entity classes and that the database can
		/// be correctly created, including all constraints, relations etc.
		/// </summary>
		[TestMethod]
		public void TestDataFactory()
		{
			var conf = Util.Configuration.Example;
			conf.DatabaseConnectionString = null;
			conf.DatabaseType = DatabaseType.SQLiteTemp;

			DataFactory.Configure(conf, new ColoredConsoleLogger<DataFactory>());
			using (var tempSess = DataFactory.Instance.OpenSession())
			{
				// If we get here, then no Exception is thrown.
				// Otherwise, the test-method will fail.
			}
		}
	}
}
