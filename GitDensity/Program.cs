﻿/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2024 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Util;
using Util.Data;
using Util.Data.Entities;
using Util.Extensions;
using Util.Logging;
using Util.Similarity;
using Configuration = Util.Configuration;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GitDensity
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

		/// <summary>
		/// The program's configuration, as read from 'configuration.json'.
		/// </summary>
		public static Configuration Configuration { private set; get; }

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
			var optionsParseSuccess = Parser.Default.ParseArguments(args, options) && options.TryValidate();

			#region Check some commands first
			if (options.ShowHelp)
			{
				logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
				logger.LogInformation(options.GetUsage(wasHelpRequested: true));
				Environment.Exit((int)ExitCodes.OK);
			}
			else if (options.WriteExampeConfig || !File.Exists(Configuration.DefaultConfigFilePath))
			{
				logger.LogCritical("Writing example to file: {0}", Configuration.DefaultConfigFilePath);
				Configuration.WriteDefault();
				Environment.Exit((int)ExitCodes.OK);
			}
			#endregion

			if (optionsParseSuccess)
			{
				#region Initialize, DataFactory, temp-dir etc.
				Program.LogLevel = options.LogLevel;
				logger.LogLevel = options.LogLevel;

				logger.LogWarning("Hello, this is GitDensity.");
				logger.LogDebug("You supplied the following arguments: {0}",
					String.Join(", ", args.Select(a => $"'{a}'")));
				logger.LogDebug($"Settings:\n{JsonConvert.SerializeObject(options, Formatting.Indented)}");
				logger.LogWarning("Initializing..");


				// Now let's read the configuration and probe the configured DB:
				try
				{
					// First let's create an actual temp-directory in the folder specified:
					Configuration.TempDirectory = new DirectoryInfo(Path.Combine(
						options.TempDirectory ?? Path.GetTempPath(), nameof(GitDensity)));
					if (Configuration.TempDirectory.Exists) { Configuration.TempDirectory.TryDelete(); }
					Configuration.TempDirectory.Create();
					options.TempDirectory = Configuration.TempDirectory.FullName;
					logger.LogDebug("Using temporary directory: {0}", options.TempDirectory);

					try
					{
						if (!StringExtensions.IsNullOrEmptyOrWhiteSpace(options.ConfigFile))
						{
							logger.LogWarning($"Using a separate config-file located at {options.ConfigFile}");
							Program.Configuration = Configuration.ReadFromFile(
								Path.GetFullPath(options.ConfigFile));
						}
						else
						{
							Program.Configuration = Configuration.ReadDefault();
						}

						logger.LogDebug("Read the following configuration:\n{0}",
							JsonConvert.SerializeObject(Program.Configuration, Formatting.Indented));
					}
					catch (Exception ex)
					{
						throw new IOException("Error reading the configuration. Perhaps try to generate and derive an example configuration (use '--help')", ex);
					}

					DataFactory.Configure(Program.Configuration,
						Program.CreateLogger<DataFactory>(), new DirectoryInfo(options.TempDirectory).Parent.FullName);
					using (var tempSess = DataFactory.Instance.OpenSession())
					{
						logger.LogDebug("Successfully probed the configured database.");
					}
				}
				catch (Exception ex)
				{
					logger.LogError("Exception caught: {0}", ex.Message);
					logger.LogTrace("Exception trace: {0}", ex.StackTrace);
					Environment.Exit((int)ExitCodes.ConfigError);
				}
				#endregion

				#region check for Commands
				if (options.DeleteRepoId != default(UInt32))
				{
					try
					{
						RepositoryEntity.Delete(options.DeleteRepoId, CreateLogger<RepositoryEntity>());
						Environment.Exit((int)ExitCodes.OK);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, ex.Message);
						Environment.Exit((int)ExitCodes.RepoInvalid);
					}
				}
				#endregion


				// Now let's try to open the specified repository:
				try
				{
					String useRepoName = null;
					ProjectEntity project = null;
					// Check if open from Projects first:
					if (UInt32.TryParse(options.RepoPath, out UInt32 projectId))
					{
						using (var sess = DataFactory.Instance.OpenSession())
						{
							project = sess.QueryOver<ProjectEntity>()
								.Where(p => p.InternalId == projectId).SingleOrDefault();
							if (!(project is ProjectEntity))
							{
								throw new InvalidDataException(
									$"Attempted to fetch non-existing Project by internal ID {projectId}.");
							}

							logger.LogInformation("Analyzing repository from internal project with internal ID {0} and clone-URL {1}", projectId, project.CloneUrl);
							if (!project.WasCorrected)
							{
								logger.LogWarning("The project with internal ID {0} was not previously corrected and may not be working.", projectId);
							}

							if (sess.QueryOver<RepositoryEntity>().Where(r => r.Project == project).List().Any())
							{
								logger.LogWarning("Project with ID {0} already analyzed. Quitting.", project.InternalId);
								Environment.Exit((int)ExitCodes.OK);
								return;
							}

							options.RepoPath = project.CloneUrl;
							useRepoName = $"{project.Name}_{project.Owner ?? project.InternalId.ToString()}";
						}
					}

					var repoTempPath = Path.Combine(
						new DirectoryInfo(options.TempDirectory).Parent.FullName,
						$"{nameof(GitDensity)}_repos");
					if (!Directory.Exists(repoTempPath))
					{
						Directory.CreateDirectory(repoTempPath);
					}

					logger.LogInformation($"Opening repository {options.RepoPath}..");
					using (var repo = options.RepoPath.OpenRepository(
						repoTempPath, useRepoName: useRepoName, pullIfAlreadyExists: true))
					{
						logger.LogInformation($"Repository is located in {repo.Info.WorkingDirectory}");
						var span = new GitCommitSpan(repo, options.Since, options.Until,
							sinceUseDate: options.SinceUseDate, untilUseDate: options.UntilUseDate);

						// Instantiate the Density analysis with the selected programming
						// languages' file extensions and other options from the command line.
						using (var density = new Density.GitDensity(span,
							options.ProgrammingLanguages, options.SkipInitialCommit, options.SkipMergeCommits,
							Configuration.LanguagesAndFileExtensions
								.Where(kv => options.ProgrammingLanguages.Contains(kv.Key))
								.SelectMany(kv => kv.Value),
							options.TempDirectory,
							options.SkipGitHoursAnalysis,
							options.SkipGitMetricsAnalysis))
						{
							density.ExecutionPolicy = options.ExecutionPolicy;

							density.InitializeStringSimilarityMeasures(
								typeof(SimilarityEntity),
								new HashSet<SimilarityMeasurementType>(
									SimilarityMeasurementType.None.AsEnumerable().Concat(
									Configuration.EnabledSimilarityMeasurements
									.Where(kv => kv.Value).Select(kv => kv.Key))));

							density.InitializeHoursTypeEntities(Configuration.HoursTypes);

							var start = DateTime.Now;
							logger.LogWarning("Starting Analysis..");
							var result = density.Analyze();
							logger.LogWarning("Analysis took {0}", DateTime.Now - start);

							result.Repository.Project = project;
							result.PersistToDatabase();
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

			if (!options.NoWait)
			{
				logger.LogInformation("Analysis finished, everything went well.");
				logger.LogInformation("Press a key to exit GitDensity...");
				Console.ReadKey();
			}

			Environment.Exit((int)ExitCodes.OK);
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

		/// <summary>
		/// To obtains the actual <see cref="ICollection{ProgrammingLanguage}"/>s, use the
		/// property <see cref="ProgrammingLanguages"/>.
		/// </summary>
		[OptionList('p', "prog-langs", ',', Required = true, HelpText = "A comma-separated list of programming languages to examine in the given repository. Other files will be ignored.")]
		public IList<String> LanguagesRaw { get; set; }

		[Option('c', "config-file", Required = false, HelpText = "Optional. Absolute path to a valid configuration.json. If not given, uses the configuration.json that is to be found in the same folder as " + nameof(GitDensity) + ".exe.")]
		public String ConfigFile { get; set; }

		[Option('i', "skip-initial-commit", Required = false, DefaultValue = false, HelpText = "If present, does not analyze the pair that consists of the 2nd and the initial commit to a repository.")]
		public Boolean SkipInitialCommit { get; set; }

		/// <summary>
		/// Please note that the default behavior (value) was changed to false, as this
		/// is a boolean option that could otherwise not be switched off.
		/// </summary>
		[Option('m', "skip-merge-commits", Required = false, DefaultValue = false, HelpText = "If present, does not analyze pairs where the younger commit is a merge commit.")]
		public Boolean SkipMergeCommits { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default. Be aware that the directory may be wiped at any point in time.")]
		public String TempDirectory { get; set; }

		[Option('s', "since", Required = false, HelpText = "Optional. Analyze data since a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Since { get; set; }

		[Option("since-use-date", Required = false, DefaultValue = SinceUntilUseDate.Committer, HelpText = "Optional. If using a since-date to delimit the range of commits, it can either be extracted from the " + nameof(SinceUntilUseDate.Author) + " or the " + nameof(SinceUntilUseDate.Committer) + ".")]
		[JsonConverter(typeof(StringEnumConverter))]
		public SinceUntilUseDate SinceUseDate { get; set; }

		[Option('u', "until", Required = false, HelpText = "Optional. Analyze data until (inclusive) a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Until { get; set; }

		[Option("until-use-date", Required = false, DefaultValue = SinceUntilUseDate.Committer, HelpText = "Optional. If using an until-date to delimit the range of commits, it can either be extracted from the " + nameof(SinceUntilUseDate.Author) + " or the " + nameof(SinceUntilUseDate.Committer) + ".")]
		[JsonConverter(typeof(StringEnumConverter))]
		public SinceUntilUseDate UntilUseDate { get; set; }

		[Option('e', "exec-policy", Required = false, DefaultValue = ExecutionPolicy.Parallel, HelpText = "Optional. Set the execution policy for the analysis. Allowed values are " + nameof(ExecutionPolicy.Parallel) + " and " + nameof(ExecutionPolicy.Linear) + ". The former is faster while the latter uses only minimal resources.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public ExecutionPolicy ExecutionPolicy { get; set; }

		[Option('w', "no-wait", Required = false, DefaultValue = false, HelpText = "Optional. If present, then the program will exit after the analysis is finished. Otherwise, it will wait for the user to press a key by default.")]
		public Boolean NoWait { get; set; }

		[Option("skip-git-hours", Required = false, DefaultValue = false, HelpText = "Optional. If present, then time spent using GitHours will not be analyzed or included in the result.")]
		public Boolean SkipGitHoursAnalysis { get; set; } = false;

		[Option("skip-git-metrics", Required = false, DefaultValue = false, HelpText = "Optional. If present, then metrics using GitMetrics will not be analyzed or included in the result.")]
		public Boolean SkipGitMetricsAnalysis { get; set; } = false;

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest/most verbose to lowest/least verbose) Trace, Debug, Information, Warning, Error, Critical, None.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

		#region Command-Options
		[Option("cmd-write-config", Required = false, DefaultValue = false, HelpText = "Command. If present, writes an exemplary 'configuration.json' file to the binary's location. Note that this will overwrite a may existing file. The program will terminate afterwards.")]
		public Boolean WriteExampeConfig { get; set; }

		[Option("cmd-delete-repo-id", Required = false, HelpText = "Command. Removes analysis results for an entire RepositoryEntity and all of its associated entities, then terminates the program.")]
		public UInt32 DeleteRepoId { get; set; }
		#endregion

		[Option('h', "help", Required = false, DefaultValue = false, HelpText = "Print this help-text and exit.")]
		public Boolean ShowHelp { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		/// <summary>
		/// The library cannot parse Lists of Enumeration values, so we have to
		/// use this approach of selected languages.
		/// </summary>
		[JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
		public IReadOnlyCollection<ProgrammingLanguage> ProgrammingLanguages
		{
			get
			{
				var eType = typeof(ProgrammingLanguage);
				Func<String, ProgrammingLanguage> convert = @string =>
					(ProgrammingLanguage)Enum.Parse(eType, @string, ignoreCase: true);
				var list = new List<ProgrammingLanguage>(this.LanguagesRaw.Select(raw => convert(raw)));
				return new ReadOnlyCollection<ProgrammingLanguage>(list);
			}
		}

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
			var supportedLanguages = !wasHelpRequested ? String.Empty : "\n\nSupported programming languages: " + String.Join(", ", Configuration.LanguagesAndFileExtensions.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key.ToString()} ({String.Join(", ", kv.Value.Select(v => $".{v}"))})"));
			var implementedSimilarities = !wasHelpRequested ? String.Empty : "\n\nImplemented similarity measurements: " + String.Join(", ", SimilarityEntity.SmtToPropertyInfo.Keys
				.OrderBy(smt => (int)smt)
				.Select(smt => $"{smt.ToString()} ({(int)smt})")
			);
			var exitReason = exitCode == ExitCodes.UsageInvalid && !wasHelpRequested ?
				"Error: The given parameters are invalid and cannot be parsed. You must not specify unrecognized parameters. Use '-h' or '--help' to get the full usage info.\n\n" : String.Empty;

			return $"{fullLine}\n{exitReason}{ht}{exitCodes}{supportedLanguages}{implementedSimilarities}\n\n{fullLine}";
		}

		/// <summary>
		/// Must be called after having parsed the options.
		/// </summary>
		/// <returns></returns>
		internal bool TryValidate()
		{
			try
			{
				return this.ProgrammingLanguages.Count() > 0;
			}
			catch
			{
				return false;
			}
		}
	}
}
