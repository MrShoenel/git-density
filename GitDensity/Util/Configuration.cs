using GitDensity.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Util
{
	/// <summary>
	/// An enumeration of programming languages we currently support and have
	/// also mapping for in <see cref="Configuration.LanguagesAndFileExtensions"/>.
	/// The clone detection we used also supports the not yet implemented languages
	/// Cpp, Scala, TypeScript, VB, XML, Pascal, Cobol, ASP, Ruby, Text, JavaScript,
	/// ADA, Python, ST and SQL.
	/// </summary>
	public enum ProgrammingLanguage
	{
		/// <summary>
		/// C
		/// </summary>
		C,

		/// <summary>
		/// C#
		/// </summary>
		CS,

		/// <summary>
		/// Java
		/// </summary>
		Java,

		/// <summary>
		/// PHP
		/// </summary>
		PHP
	}

	public class Configuration
	{
		/// <summary>
		/// A Dictionary that holds for each <see cref="ProgrammingLanguage"/>
		/// a collection of accepted filename extensions. This is important so that only
		/// relevant files are diff'ed or compared later.
		/// </summary>
		public static readonly IReadOnlyDictionary<ProgrammingLanguage, IReadOnlyCollection<String>> LanguagesAndFileExtensions = new ReadOnlyDictionary<ProgrammingLanguage, IReadOnlyCollection<String>>(new Dictionary<ProgrammingLanguage, IReadOnlyCollection<String>> {
			{ ProgrammingLanguage.C, new ReadOnlyCollection<String>(
				new [] { "c", "h" }.ToList()
			)},

			{ ProgrammingLanguage.CS, new ReadOnlyCollection<String>(
				new [] { "cs" }.ToList()
			)},

			{ ProgrammingLanguage.Java, new ReadOnlyCollection<String>(
				new [] { "java" }.ToList()
			)},

			{ ProgrammingLanguage.PHP, new ReadOnlyCollection<String>(
				new [] { "php", "phtml", "php3", "php4", "php5" }.ToList()
			)}
		});

		public const String DefaultFileName = "configuration.json";

		/// <summary>
		/// When writing out a new example, we include the helptext as first property
		/// with the order -2 (because the other properties have an implicit order of
		/// -1).
		/// <see cref="https://stackoverflow.com/a/14035431"/>
		/// </summary>
		[JsonProperty(Required = Required.Default, PropertyName = "//", Order = -2)]
		public String Help { get; set; }

		/// <summary>
		/// The absolute path to the executable for handling clone detection.
		/// This should not contain any arguments, use the other property
		/// for that.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "pathToCloneDetectionBinary")]
		public String PathToCloneDetectionBinary { get; set; }

		/// <summary>
		/// This should contain any args that are passed to the clone-detection.
		/// If a Jar-file is used, than those args should be placed here as
		/// well, e.g. "-jar /path/to/cloneDetection.jar", while the path
		/// should then only point to the Java-binary.
		/// </summary>
		[JsonProperty(Required = Required.Default, PropertyName = "cloneDetectionArgs")]
		public String CloneDetectionArgs { get; set; }

		/// <summary>
		/// Specify the database to use. This application can operate with an
		/// in-memory database as well.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "databaseType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLiteTemp;

		/// <summary>
		/// A nullable string that contains all necessary details to establish a
		/// connection to the selected database. If the selected <see cref="DatabaseType"/>
		/// is equal to <see cref="DatabaseType.SQLiteTemp"/>, then this string may
		/// be null or empty.
		/// </summary>
		[JsonProperty(Required = Required.AllowNull, PropertyName = "databaseConnectionString")]
		public String DatabaseConnectionString { get; set; } = null;

		/// <summary>
		/// An example that is used to create an initial configuration, if
		/// none exists.
		/// </summary>
		public static readonly Configuration Example = new Configuration
		{
			Help = $@"This is the Helptext for this configuration. Launch the program with '--help' to get more help on available switches. Most of the properties you may adjust are boolean, numbers or strings. Some properties require a specific value - those will be listed below:

-> List of supported Database-Types: {{ { String.Join(", ",
									Enum.GetNames(typeof(DatabaseType)).OrderBy(v => v)) } }}",


			PathToCloneDetectionBinary = @"C:\temp\binary.exe",
			// Detect clones of at least one line, ignore identifier names, ignore self-clones,
			// ignore numeric and string literals
			CloneDetectionArgs = "-min 1 -Id -self -Num -Str",
			DatabaseType = DatabaseType.SQLiteTemp,
			DatabaseConnectionString = null
		};
	}
}
