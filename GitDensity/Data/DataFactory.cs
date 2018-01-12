/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2017 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the MIT-license.
///
/// ---------------------------------------------------------------------------------
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
/// ---------------------------------------------------------------------------------
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg.Db;
using GitDensity.Data.Entities;
using GitDensity.Util;
using Microsoft.Extensions.Logging;
using NHibernate;
using System;
using System.IO;
using System.Linq;

namespace GitDensity.Data
{
	/// <summary>
	/// Enumeration of supported database-types.
	/// </summary>
	public enum DatabaseType
	{
		MsSQL2000,
		MsSQL2005,
		MsSQL2008,
		MsSQL2012,

		MySQL,

		Oracle9,
		Oracle10,

		PgSQL81,
		PgSQL82,

		SQLite,
		SQLiteTemp
	}


	/// <summary>
	/// Non-instantiable class that provides access to the underlying database
	/// as configured by the read <see cref="Util.Configuration"/>.
	/// </summary>
	public class DataFactory
	{
		private static BaseLogger<DataFactory> logger = Program.CreateLogger<DataFactory>();

		/// <summary>
		/// The static instance to the factory.
		/// </summary>
		public static ISessionFactory Instance
		{
			get
			{
				if (!(lazySessionFactory is Lazy<ISessionFactory>))
				{
					throw new InvalidOperationException("This class was not properly initialized.");
				}

				return lazySessionFactory.Value;
			}
		}

		/// <summary>
		/// Uses the selected database, reads all mapping from this assembly,
		/// creates or updates the schema (if necessary).
		/// </summary>
		private static Lazy<ISessionFactory> lazySessionFactory;

		/// <summary>
		/// Called by <see cref="Program"/>. The public static getter for <see cref="Instance"/>
		/// requires that this method has been called before.
		/// </summary>
		/// <param name="configuration"></param>
		internal static void Configure(Util.Configuration configuration, String tempDirectory = null)
		{
			lazySessionFactory = new Lazy<ISessionFactory>(() =>
			{
				var factory = FluentNHibernate.Cfg.Fluently.Configure()
					.Database(DatabaseTypeWithConnectionStringToConfigurer(configuration, tempDirectory))
					.ExposeConfiguration(config =>
					{
						config.SetInterceptor(new SqlStatementInterceptor());

						var update = new NHibernate.Tool.hbm2ddl.SchemaUpdate(config);
						update.Execute(script: false, doUpdate: true);
						if (update.Exceptions.Count > 0)
						{
							foreach (var ex in update.Exceptions)
							{
								logger.LogError(ex, $"{ex.Message}\nStacktrace:\n{ex.StackTrace}");
							}
							throw new InvalidOperationException("Cannot create/update the DB schema.");
						}
					})
					.Mappings(mappings =>
					{
						mappings.FluentMappings
						.AddFromAssemblyOf<DataFactory>()
						.Conventions.Add(typeof(IndexedConvention));
					})
					.BuildSessionFactory();

				return factory;
			});
		}

		/// <summary>
		/// Used to log queries issued by <see cref="NHibernate"/> when the application's
		/// log-level is set to <see cref="Microsoft.Extensions.Logging.LogLevel.Trace"/>.
		/// </summary>
		internal class SqlStatementInterceptor : EmptyInterceptor
		{
			public override NHibernate.SqlCommand.SqlString OnPrepareStatement(NHibernate.SqlCommand.SqlString sql)
			{
				logger.LogTrace(sql.ToString());
				return sql;
			}
		}

		/// <summary>
		/// Returns the correct type of <see cref="IPersistenceConfigurer"/> for the selected
		/// database-type.
		/// </summary>
		/// <param name="configuration"></param>
		/// <returns></returns>
		private static IPersistenceConfigurer DatabaseTypeWithConnectionStringToConfigurer(Util.Configuration configuration, String tempDirectory = null)
		{
			if (configuration.DatabaseType == DatabaseType.SQLiteTemp)
			{
				return SQLiteConfiguration.Standard.UsingFile(
					$"{Path.Combine(tempDirectory ?? Path.GetTempPath(), Path.GetRandomFileName())}.sqlite");
			}

			var connString = configuration.DatabaseConnectionString;

			if (String.IsNullOrEmpty(connString) || String.IsNullOrWhiteSpace(connString))
			{
				throw new ArgumentException("Configuration's database connection string not valid.");
			}

			switch (configuration.DatabaseType)
			{
				case DatabaseType.MsSQL2000:
					return MsSqlConfiguration.MsSql2000.ConnectionString(connString);
				case DatabaseType.MsSQL2005:
					return MsSqlConfiguration.MsSql2005.ConnectionString(connString);
				case DatabaseType.MsSQL2008:
					return MsSqlConfiguration.MsSql2008.ConnectionString(connString);
				case DatabaseType.MsSQL2012:
					return MsSqlConfiguration.MsSql2012.ConnectionString(connString);

				case DatabaseType.MySQL:
					return MySQLConfiguration.Standard.ConnectionString(connString);

				case DatabaseType.Oracle9:
					return OracleDataClientConfiguration.Oracle9.ConnectionString(connString);
				case DatabaseType.Oracle10:
					return OracleDataClientConfiguration.Oracle10.ConnectionString(connString);

				case DatabaseType.PgSQL81:
					return PostgreSQLConfiguration.PostgreSQL81.ConnectionString(connString);
				case DatabaseType.PgSQL82:
					return PostgreSQLConfiguration.PostgreSQL82.ConnectionString(connString);

				case DatabaseType.SQLite:
					return SQLiteConfiguration.Standard.ConnectionString(connString);

				default:
					throw new NotSupportedException(
						String.Format("Database-type {0} is not currently supported.", configuration.DatabaseType.ToString()));
			}
		}
	}
}
