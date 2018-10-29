/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Util.Data;

namespace Util
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

	/// <summary>
	/// Used in <see cref="Configuration"/>.
	/// </summary>
	[DebuggerDisplay("MaxDiff {MaxDiff}, AddFirst {FirstCommitAdd}")]
	public class HoursTypeConfiguration : IEquatable<HoursTypeConfiguration>
	{
		/// <summary>
		/// The amount of minutes between two commits, before they are considered
		/// belonging to a different session.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "maxDiff")]
		public UInt32 MaxDiff { get; set; }

		/// <summary>
		/// The amount of minutes to add for the first commit of a session.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "firstCommitAdd")]
		public UInt32 FirstCommitAdd { get; set; }

		#region equality
		/// <summary>
		/// Returns true if both of this class' properties are equal.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(HoursTypeConfiguration other)
		{
			return other is HoursTypeConfiguration &&
				other.FirstCommitAdd == this.FirstCommitAdd && other.MaxDiff == this.MaxDiff;
		}

		/// <summary>
		/// Overridden so that two equal instances return the same hash code.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.FirstCommitAdd.GetHashCode() ^ this.MaxDiff.GetHashCode() * 31;
		}

		/// <summary>
		/// Calls <see cref="Equals(HoursTypeConfiguration)"/> by casting the other object
		/// to <see cref="HoursTypeConfiguration"/> using the as-operator.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return this.Equals(obj as HoursTypeConfiguration);
		}
		#endregion
	}

	/// <summary>
	/// Used in <see cref="Configuration"/> for configuring each metrics-analyzer.
	/// </summary>
	public class MetricsAnalyzerConfiguration
	{
		/// <summary>
		/// The name of analyzer's implementation's type, to be referred to by
		/// <see cref="Configuration.UseMetricsAnalyzer"/>. It is recommended to
		/// use the fully qualified name.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "typeName")]
		public String TypeName { get; set; }

		/// <summary>
		/// A set of supported Programming languages the analyzer supports. This list
		/// may be used to select an appropriate analyzer.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "supportedLanguages", ItemConverterType = typeof(StringEnumConverter))]
		public ISet<ProgrammingLanguage> SupportedLanguages { get; set; }
			= new HashSet<ProgrammingLanguage>();

		/// <summary>
		/// A dictionary with settings specific to the analyzer. The values of this
		/// dictionary must be JSON-deserializable, so that for the type
		/// <see cref="IComparable"/> was chosen (implemented by all structs and string).
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "configuration")]
		public IDictionary<String, IComparable> Configuration { get; set; }
			= new Dictionary<String, IComparable>();
	}

	public class Configuration
	{
		/// <summary>
		/// The default name of the serialized configuration.
		/// </summary>
		[JsonIgnore]
		public const String DefaultFileName = "configuration.json";

		/// <summary>
		/// The directory of the currently executing binary/assembly,
		/// i.e. 'GitDensity.exe'.
		/// </summary>
		[JsonIgnore]
		public static readonly String WorkingDirOfExecutable =
			Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		/// <summary>
		/// An absolute path to the default configuration file.
		/// </summary>
		[JsonIgnore]
		public static readonly String DefaultConfigFilePath =
			Path.Combine(WorkingDirOfExecutable, DefaultFileName);

		/// <summary>
		/// This can be set and read from within any of the Git*-applications.
		/// </summary>
		[JsonIgnore]
		public static DirectoryInfo TempDirectory { get; set; }

		/// <summary>
		/// Writes the example (<see cref="Example"/>) to the <see cref="DefaultConfigFilePath"/>.
		/// </summary>
		public static void WriteDefault()
		{
			File.WriteAllText(DefaultConfigFilePath,
				JsonConvert.SerializeObject(Example, Formatting.Indented));
		}

		/// <summary>
		/// Attempts to read the default configuration from the <see cref="DefaultConfigFilePath"/>.
		/// </summary>
		/// <returns>An instance of <see cref="Configuration"/>.</returns>
		public static Configuration ReadDefault()
		{
			return JsonConvert.DeserializeObject<Configuration>(
				File.ReadAllText(DefaultConfigFilePath));
		}

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
		/// A dictionary that holds all <see cref="Similarity.SimilarityMeasurementType"/>s that
		/// shall be enabled (and used) during analysis. Note that may not all are implemented by
		/// the entity currently in use. Such entities must implement <see cref="Data.Entities.IHasSimilarityComparisonType"/>. Use the main application's help-screen
		/// to get those measurements that are currently implemented.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "enabledSimilarityMeasurements")]
		public Dictionary<Similarity.SimilarityMeasurementType, Boolean> EnabledSimilarityMeasurements { get; set; }

		/// <summary>
		/// A list of available implementations of analyzers for metrics.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "metricsAnalyzers")]
		public IList<MetricsAnalyzerConfiguration> MetricsAnalyzers { get; set; }
			= new List<MetricsAnalyzerConfiguration>();

		/// <summary>
		/// The type-name of the analyzer-implementation to use. If this property is missing or set
		/// to null, Git-Metrics may use the first available implementation or tries to select an
		/// appropriate analyzer from <see cref="MetricsAnalyzers"/> based on criteria, such as the
		/// underlying type of the project.
		/// Please note that setting this property to null will not disable the analysis.
		/// </summary>
		[JsonProperty(Required = Required.AllowNull, PropertyName = "useMetricsAnalyzer")]
		public String UseMetricsAnalyzer { get; set; } = null;

		/// <summary>
		/// A set of <see cref="HoursTypeConfiguration"/> objects. For each of these, the git-hours
		/// will be computed.
		/// </summary>
		[JsonProperty(Required = Required.Always, PropertyName = "hoursTypes")]
		public HashSet<HoursTypeConfiguration> HoursTypes { get; set; }

		/// <summary>
		/// An example that is used to create an initial configuration, if
		/// none exists.
		/// </summary>
		public static readonly Configuration Example = new Configuration
		{
			Help = $@"This is the Helptext for this configuration. Launch the program with '--help' to get more help on available switches. Most of the properties you may adjust are boolean, numbers or strings. Some properties require a specific value - those will be listed below:

-> List of supported Programming-Languages: {{ {String.Join(", ", Enum.GetNames(typeof(ProgrammingLanguage)).OrderBy(v => v))} }};

-> List of supported Database-Types: {{ { String.Join(", ",
									Enum.GetNames(typeof(DatabaseType)).OrderBy(v => v)) } }};

-> The hoursTypes is an array of objects, where each object has the property 'maxDiff' and 'firstCommitAdd'. Both properties are in minutes and all combinations must be unique. For each object/configuration, git-hours will be computed.",


			PathToCloneDetectionBinary = @"C:\ProgramData\Oracle\Java\javapath\java.exe",
			// Detect clones of at least one line, ignore identifier names, ignore self-clones,
			// ignore numeric and string literals
			CloneDetectionArgs = "-min 1 -Id -self -Num -Str",
			DatabaseType = DatabaseType.SQLiteTemp,
			DatabaseConnectionString = null,
			MetricsAnalyzers = new List<MetricsAnalyzerConfiguration> {
				new MetricsAnalyzerConfiguration
				{
					TypeName = "GitMetrics.QualityAnalyzer.VizzAnalyzer.VizzMetricsAnalyzer",
					SupportedLanguages = new HashSet<ProgrammingLanguage> {
						ProgrammingLanguage.Java
					},
					Configuration = new Dictionary<String, IComparable> {
						{ "pathToBinary", @"C:\ProgramData\Oracle\Java\javapath\java.exe" },
						{ "args", @"-jar C:\temp\jqa.jar" }

					}
				}
			},
			UseMetricsAnalyzer = null,
			EnabledSimilarityMeasurements = Enum.GetValues(typeof(Similarity.SimilarityMeasurementType))
				.Cast<Similarity.SimilarityMeasurementType>()
				// The following is always implicitly available and enabled, so we exclude it:
				.Where(smt => smt != Similarity.SimilarityMeasurementType.None)
				.ToDictionary(smt => smt, foo => true),
			HoursTypes = new HashSet<HoursTypeConfiguration>
			{
				new HoursTypeConfiguration { MaxDiff = 30, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 60, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 90, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 120, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 150, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 180, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 240, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 300, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 360, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 420, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 480, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 540, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 600, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 660, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 720, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 780, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 840, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 900, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 960, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1080, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1140, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1200, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1260, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1320, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1380, FirstCommitAdd = 120 },
				new HoursTypeConfiguration { MaxDiff = 1440, FirstCommitAdd = 120 }
			}
		};
	}
}
