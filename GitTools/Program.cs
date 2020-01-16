/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitTools. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using CommandLine;
using CommandLine.Text;
using GitTools.Analysis;
using GitTools.Analysis.ExtendedAnalyzer;
using GitTools.Analysis.SimpleAnalyzer;
using LINQtoCSV;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Util;
using Util.Data;
using Util.Extensions;
using Util.Logging;

namespace GitTools
{

	internal class Program
	{
		/// <summary>
		/// The current global <see cref="LogLevel"/>.
		/// </summary>
		public static LogLevel LogLevel { get; private set; }

		/// <summary>
		/// Shortcut provider for obtaining equally configured loggers per <see cref="Type"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static BaseLogger<T> CreateLogger<T>()
		{
			return new ColoredConsoleLogger<T>
			{
				LogCurrentScope = true,
				LogCurrentTime = true,
				LogCurrentType = true,
				LogLevel = Program.LogLevel
			};
		}

		private static BaseLogger<Program> logger = CreateLogger<Program>();

		/// <summary>
		/// Main entry point for application.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options))
			{
				#region Initialize, DataFactory, temp-dir etc.
				Program.LogLevel = options.LogLevel;
				logger.LogLevel = options.LogLevel;

				if (options.ShowHelp)
				{
					logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
					logger.LogInformation(options.GetUsage(wasHelpRequested: true));
					Environment.Exit((int)ExitCodes.OK);
				}

				logger.LogWarning("Hello, this is GitTools.");
				logger.LogDebug("You supplied the following arguments: {0}",
					String.Join(", ", args.Select(a => $"'{a}'")));
				logger.LogDebug($"Settings:\n{JsonConvert.SerializeObject(options, Formatting.Indented)}");
				logger.LogWarning("Initializing..");


				// Now let's read the configuration and probe the configured DB:
				try
				{
					// First let's create an actual temp-directory in the folder specified:
					Configuration.TempDirectory = new DirectoryInfo(Path.Combine(
						options.TempDirectory ?? Path.GetTempPath(), nameof(GitTools)));
					if (Configuration.TempDirectory.Exists) { Configuration.TempDirectory.TryDelete(); }
					Configuration.TempDirectory.Create();
					options.TempDirectory = Configuration.TempDirectory.FullName;
					logger.LogDebug("Using temporary directory: {0}", options.TempDirectory);
				}
				catch (Exception ex)
				{
					logger.LogError("Exception caught: {0}", ex.Message);
					logger.LogTrace("Exception trace: {0}", ex.StackTrace);
					Environment.Exit((int)ExitCodes.ConfigError);
				}
				#endregion


				// Now let's try to open the specified repository:
				try
				{
					var repoTempPath = Path.Combine(
						new DirectoryInfo(options.TempDirectory).Parent.FullName,
						$"{nameof(GitTools)}_repos");
					if (!Directory.Exists(repoTempPath))
					{
						Directory.CreateDirectory(repoTempPath);
					}

					logger.LogInformation($"Opening repository {options.RepoPath}..");
					using (var repo = options.RepoPath.OpenRepository(
						repoTempPath, pullIfAlreadyExists: true))
					{
						logger.LogInformation($"Repository is located in {repo.Info.WorkingDirectory}");

						ISet<String> sha1IDs = null;
						if (!String.IsNullOrEmpty(options.InputCommitIDs))
						{
							sha1IDs = new HashSet<String>(
								File.ReadAllLines(Path.GetFullPath(options.InputCommitIDs)).Where(l => l.Length >= 40));
						}

						var span = new GitCommitSpan(
							repo, options.Since, options.Until, options.Limit, sha1IDs);
						if (span.SHA1Filter.Count > 0)
						{
							logger.LogTrace($"Using the following commit-IDs for the {nameof(GitCommitSpan)}: {String.Join(", ", span.SHA1Filter)}");
						}

						#region Check for commands
						if (options.CmdCountCommits)
						{
							logger.LogInformation($"Counting commits between {span.SinceAsString} and {span.UntilAsString}..");

							var commits = span.OrderBy(c => c.Author.When.UtcDateTime).ToList();

							var json = JsonConvert.SerializeObject(new {
								commits.Count,
								SHA1s = commits.Select(c => c.ShaShort())
							});

							using (var writer = File.CreateText(options.OutputFile))
							{
								writer.Write(json);
							}

							logger.LogInformation($"Wrote JSON to {options.OutputFile}");
							Environment.Exit((int)ExitCodes.OK);
						}
						#endregion

						using (var writer = File.CreateText(options.OutputFile))
						{
							// Now we extract some info and write it out later.
							var csvc = new CsvContext();
							var outd = new CsvFileDescription
							{
								FirstLineHasColumnNames = true,
								FileCultureInfo = Thread.CurrentThread.CurrentUICulture,
								SeparatorChar = ',',
								QuoteAllFields = true,
								EnforceCsvColumnAttribute = true
							};

							IAnalyzer<IAnalyzedCommit> analyzer = null;
							switch (options.AnalysisType)
							{
								case AnalysisType.Simple:
									analyzer = new SimpleAnalyzer(options.RepoPath, span);
									break;
								case AnalysisType.Extended:
									analyzer = new ExtendedAnalyzer(options.RepoPath, span, options.SkipSizeInExtendedAnalysis);
									break;
								default:
									throw new Exception($"The {nameof(AnalysisType)} '{options.AnalysisType.ToString()}' is not supported.");
							}

							logger.LogDebug($"Using analyzer: {analyzer.GetType().Name}");

							analyzer.ExecutionPolicy = options.ExecutionPolicy;
							var details = analyzer.AnalyzeCommits().ToList();
							logger.LogInformation("Analysis done, attempting to write to CSV..");
							if (options.AnalysisType == AnalysisType.Simple)
							{
								csvc.Write(details.Cast<SimpleCommitDetails>(), writer, outd);
							}
							else if (options.AnalysisType == AnalysisType.Extended)
							{
								csvc.Write(details.Cast<ExtendedCommitDetails>(), writer, outd);
							}

							logger.LogInformation($"Wrote {details.Count} rows to file {options.OutputFile}.");
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(
						"Cannot open the repository specified by path or URL '{0}'.", options.RepoPath);
					logger.LogError("Exception caught: {0}", ex.Message);
					logger.LogTrace("Exception trace: {0}", ex.StackTrace);
					if (ex is AggregateException)
					{
						foreach (var innerEx in (ex as AggregateException).InnerExceptions)
						{
							logger.LogError("Message: {0}", innerEx.Message);
							logger.LogError("Trace: {0}", innerEx.StackTrace);
						}
					}
					Environment.Exit((int)ExitCodes.RepoInvalid);
				}
			}
			else
			{
				logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
				logger.LogInformation(options.GetUsage(
					ExitCodes.UsageInvalid, wasHelpRequested: options.ShowHelp));
				Environment.Exit((int)ExitCodes.UsageInvalid);
			}

			Environment.Exit((int)ExitCodes.OK);
		}

		/// <summary>
		/// Temporary method used to initialize two new, previously just
		/// set to null, fields.
		/// </summary>
		/// <param name="options"></param>
		[Obsolete("Do not use. This method only initialized the additional fields 'AuthorNominalLabel' and 'CommitterNominalLabel' in an already imported/exported DB.")]
		public static void FixDeveloperNominalLabels(CommandLineOptions options)
		{
			var projects = new[] {
				"https://github.com/apache/camel.git",
				"https://github.com/apache/hbase.git",
				"https://github.com/elastic/elasticsearch.git",
				"https://github.com/JetBrains/kotlin.git",
				"https://github.com/kiegroup/drools.git",
				"https://github.com/orientechnologies/orientdb.git",
				"https://github.com/ReactiveX/RxJava.git",
				"https://github.com/restlet/restlet-framework-java.git",
				"https://github.com/spring-projects/spring-framework.git"
			};

			var config = Util.Configuration.ReadDefault();
			DataFactory.Configure(config,
				Program.CreateLogger<DataFactory>(), new DirectoryInfo(options.TempDirectory).Parent.FullName);
			using (var tempSess = DataFactory.Instance.OpenSession())
			{
				logger.LogDebug("Successfully probed the configured database.");
			}

			foreach (var p in projects)
			{
				using (var repo = p.OpenRepository(options.TempDirectory, null, true))
				{
					var span = new GitCommitSpan(repo, options.Since, options.Until);
					var analyzer = new ExtendedAnalyzer(options.RepoPath, span, skipSizeAnalysis: true);
					analyzer.ExecutionPolicy = ExecutionPolicy.Parallel;

					var commits = new HashSet<ExtendedCommitDetails>(analyzer.AnalyzeCommits());

					using (var sess = DataFactory.Instance.OpenSession())
					{
						var q = sess.CreateSQLQuery("UPDATE gtools_ex SET AuthorNominalLabel = :authorNom, CommitterNominalLabel = :committerNom where SHA1 = :sha");

						foreach (var c in commits)
						{
							q.SetParameter("authorNom", c.AuthorNominalLabel);
							q.SetParameter("committerNom", c.CommitterNominalLabel);
							q.SetParameter("sha", c.SHA1);
							q.ExecuteUpdate();
						}
					}
				}
			}
		}

		/// <summary>
		/// This method was temporarily added and used to correct a value.
		/// It is no longer referenced or used anymore, but kept in code
		/// for reference.
		/// </summary>
		/// <param name="options"></param>
		[Obsolete("Do not use. This method only corrected the field 'MinutesSincePreviousCommit' which was not calculated correctly as it was not using UTC DateTimes before.")]
		public static void FixMinutesSincePrevCommit(CommandLineOptions options)
		{
			var projects = new[] {
				"https://github.com/apache/camel.git",
				"https://github.com/apache/hbase.git",
				"https://github.com/elastic/elasticsearch.git",
				"https://github.com/JetBrains/kotlin.git",
				"https://github.com/kiegroup/drools.git",
				"https://github.com/orientechnologies/orientdb.git",
				"https://github.com/ReactiveX/RxJava.git",
				"https://github.com/restlet/restlet-framework-java.git",
				"https://github.com/spring-projects/spring-framework.git"
			};

			var config = Util.Configuration.ReadDefault();
			DataFactory.Configure(config,
				Program.CreateLogger<DataFactory>(), new DirectoryInfo(options.TempDirectory).Parent.FullName);
			using (var tempSess = DataFactory.Instance.OpenSession())
			{
				logger.LogDebug("Successfully probed the configured database.");
			}

			foreach (var p in projects)
			{
				using (var repo = p.OpenRepository(options.TempDirectory, null, true))
				{
					var span = new GitCommitSpan(repo, options.Since, options.Until);
					var analyzer = new ExtendedAnalyzer(options.RepoPath, span, true);
					analyzer.ExecutionPolicy = ExecutionPolicy.Parallel;

					var commits = new HashSet<ExtendedCommitDetails>(analyzer.AnalyzeCommits());

					using (var sess = DataFactory.Instance.OpenSession())
					{
						var q = sess.CreateSQLQuery("UPDATE gtools_ex SET MinutesSincePreviousCommit = :prev where SHA1 = :sha");

						foreach (var c in commits)
						{
							q.SetParameter("prev", c.MinutesSincePreviousCommit);
							q.SetParameter("sha", c.SHA1);
							q.ExecuteUpdate();
						}
					}
				}
			}
		}
	}


	/// <summary>
	/// Class that represents all options that can be supplied using
	/// the command-line interface of this application.
	/// </summary>
	internal class CommandLineOptions
	{
		[Option('r', "repo-path", Required = true, HelpText = "Absolute path or HTTP(S) URL to a git-repository. If a URL is provided, the repository will be cloned to a temporary folder first, using its defined default branch. Also allows passing in an Internal-ID of a project from the database.")]
		public String RepoPath { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default. Be aware that the directory may be wiped at any point in time.")]
		public String TempDirectory { get; set; }

		[Option('s', "since", Required = false, HelpText = "Optional. Analyze data since a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Since { get; set; }

		[Option('u', "until", Required = false, HelpText = "Optional. Analyze data until (inclusive) a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Until { get; set; }

		[Option('a', "analysis-type", Required = false, DefaultValue = AnalysisType.Extended, HelpText = "Optional. The type of analysis to run. Allowed values are " + nameof(AnalysisType.Simple) + " and " + nameof(AnalysisType.Extended) + ". The extended analysis extracts all supported properties of any Git-repository.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public AnalysisType AnalysisType { get; set; } = AnalysisType.Extended;

		[Option('o', "out-file", Required = true, HelpText = "A path to a file to write the analysis' result to.")]
		public String OutputFile { get; set; }

		[Option('k', "skip-size", Required = false, DefaultValue = false, HelpText = "If specified, will skip any size-related measurements in the " + nameof(ExtendedCommitDetails) + ".")]
		public Boolean SkipSizeInExtendedAnalysis { get; set; }

		[Option('e', "exec-policy", Required = false, DefaultValue = ExecutionPolicy.Parallel, HelpText = "Optional. Set the execution policy for the analysis. Allowed values are " + nameof(ExecutionPolicy.Parallel) + " and " + nameof(ExecutionPolicy.Linear) + ". The former is faster while the latter uses only minimal resources.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;

		[Option('i', "input-ids", Required = false, DefaultValue = null, HelpText = "Optional. A path to a file with SHA1's of commits to analyze (one SHA1 per line). If given, then only those commits will be analyzed and all others will be skipped. Can be used in conjunction with --limit.")]
		public String InputCommitIDs { get; set; }

		[Option("limit", Required = false, DefaultValue = null, HelpText = "Optional. A positive integer to limit the amount of commits analyzed. Can be used in conjunction with any other options (such as -i).")]
		public UInt32? Limit { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest/most verbose to lowest/least verbose) Trace, Debug, Information, Warning, Error, Critical, None.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

		#region Command-Options
		[Option("cmd-count-commits", Required = false, DefaultValue = false, HelpText = "Command. Counts the amount of commits as delimited by since/until. Writes a JSON-formatted object to the console, including the commits' IDs.")]
		public Boolean CmdCountCommits { get; set; }
		#endregion

		[Option('h', "help", Required = false, DefaultValue = false, HelpText = "Print this help-text and exit.")]
		public Boolean ShowHelp { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		/// <summary>
		/// Returns a help-text generated using the options of this class.
		/// </summary>
		/// <returns></returns>
		public String GetUsage(ExitCodes exitCode = ExitCodes.OK, Boolean wasHelpRequested = false)
		{
			var fullLine = new String('-', ColoredConsole.WindowWidthSafe);
			var ht = new HelpText
			{
				Heading = HeadingInfo.Default,
				Copyright = CopyrightInfo.Default,
				AdditionalNewLineAfterOption = true,
				AddDashesToOption = true,
				MaximumDisplayWidth = ColoredConsole.WindowWidthSafe
			};

			if (wasHelpRequested)
			{
				ht.AddOptions(this);
			}
			else
			{
				HelpText.DefaultParsingErrorsHandler(this, ht);
			}

			var exitCodes = !wasHelpRequested ? String.Empty : "\n\nPossible Exit-Codes: " + String.Join(", ", Enum.GetValues(typeof(ExitCodes)).Cast<ExitCodes>()
				.OrderByDescending(e => (int)e).Select(ec => $"{ec.ToString()} ({(int)ec})"));
			var exitReason = exitCode == ExitCodes.UsageInvalid && !wasHelpRequested ?
				"Error: The given parameters are invalid and cannot be parsed. You must not specify unrecognized parameters. Use '-h' or '--help' to get the full usage info.\n\n" : String.Empty;

			return $"{fullLine}\n{exitReason}{ht}{exitCodes}\n\n{fullLine}";
		}
	}
}
