/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
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
using GitHours.Hours;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		/// The directory of the currently executing binary/assembly,
		/// i.e. 'GitDensity.exe'.
		/// </summary>
		public static readonly String WorkingDirOfExecutable =
			Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

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
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

			var configFilePath = Path.Combine(WorkingDirOfExecutable, Configuration.DefaultFileName);
			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options) && options.TryValidate())
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
				else if (options.WriteExampeConfig || !File.Exists(configFilePath))
				{
					logger.LogCritical("Writing example to file: {0}", configFilePath);

					File.WriteAllText(configFilePath,
						JsonConvert.SerializeObject(Configuration.Example, Formatting.Indented));
					Environment.Exit((int)ExitCodes.OK);
				}


				logger.LogWarning("Hello, this is GitDensity.");
				logger.LogDebug("You supplied the following arguments: {0}",
					String.Join(", ", args.Select(a => $"'{a}'")));
				logger.LogWarning("Initializing..");


				// Now let's read the configuration and probe the configured DB:
				try
				{
					// First let's create an actual temp-directory in the folder specified:
					var tempDirectory = new DirectoryInfo(Path.Combine(
						options.TempDirectory ?? Path.GetTempPath(), nameof(GitDensity)));
					if (tempDirectory.Exists) { tempDirectory.Delete(true); }
					tempDirectory.Create();
					options.TempDirectory = tempDirectory.FullName;
					logger.LogDebug("Using temporary directory: {0}", options.TempDirectory);

					try
					{
						Program.Configuration = JsonConvert.DeserializeObject<Configuration>(
							File.ReadAllText(configFilePath));
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
					logger.LogWarning("Attempting to delete Repository with ID {0}", options.DeleteRepoId);

					try
					{
						RepositoryEntity.Delete(options.DeleteRepoId);
						Environment.Exit((int)ExitCodes.OK);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, ex.Message);
						Environment.Exit((int)ExitCodes.RepoInvalid);
					}
				}

				if (options.CmdRecomputeGitHours)
				{
					try
					{
						Program.RecomputeGitHours(options);
						Environment.Exit((int)ExitCodes.OK);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, ex.Message);
						Environment.Exit((int)ExitCodes.CmdError);
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
					using (var repo = options.RepoPath.OpenRepository(
						repoTempPath, useRepoName: useRepoName, pullIfAlreadyExists: true))
					{
						var span = new GitCommitSpan(repo, options.Since, options.Until);

						// Instantiate the Density analysis with the selected programming
						// languages' file extensions and other options from the command line.
						using (var density = new Density.GitDensity(span,
							options.ProgrammingLanguages, options.SkipInitialCommit, options.SkipMergeCommits,
							Configuration.LanguagesAndFileExtensions
								.Where(kv => options.ProgrammingLanguages.Contains(kv.Key))
								.SelectMany(kv => kv.Value),
							options.TempDirectory))
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


		/// <summary>
		/// This methods purpose was/is to recompute all configured git-hours for already analyzed
		/// repositories. Currently, this method recomputes all repositories' git-hours. This method
		/// should not be used anymore, unless there are manual changes to the database and new/other
		/// types of configured git-hours (hours-types) and one needs to (re-)compute the hours for
		/// the already analyzed repositories.
		/// </summary>
		/// <param name="options"></param>
		[Obsolete]
		protected internal static void RecomputeGitHours(CommandLineOptions options)
		{
			var parallelOptions = new ParallelOptions();
			if (options.ExecutionPolicy == ExecutionPolicy.Linear)
			{
				parallelOptions.MaxDegreeOfParallelism = 1;
			}

			var toRepoName = new Func<RepositoryEntity, String>(re => $"{re.Project.Name}_{re.Project.Owner}");
			var checkoutPath = $"R:\\{nameof(GitDensity)}_repos";
			var hoursTypesEntities = Configuration.HoursTypes.ToDictionary(
				htc => htc, htc =>
				{
					return HoursTypeEntity.ForSettings(htc.MaxDiff, htc.FirstCommitAdd);
				});

			List<UInt32> repoEntityIds;
			using (var session = DataFactory.Instance.OpenSession())
			{
				repoEntityIds = session.QueryOver<RepositoryEntity>()
					.Where(re => re.Project != null).List().Select(re => re.ID).ToList();
			}

			Parallel.ForEach(repoEntityIds, parallelOptions, repoEntityId =>
			{
				using (var session = DataFactory.Instance.OpenSession())
				{
					var start = DateTime.Now;
					var repoEntity = session.QueryOver<RepositoryEntity>()
						.Where(re => re.ID == repoEntityId).List()[0];

					var useRepoName = toRepoName(repoEntity);
					var checkoutDir = Path.Combine(checkoutPath, useRepoName);

					try
					{
						#region conditional clone
						if (Directory.Exists(checkoutDir))
						{
							logger.LogWarning($"Directory exists, skipping: {checkoutDir}");
						}
						else
						{
							logger.LogInformation("Cloning repository from {0}", repoEntity.Project.CloneUrl);
							LibGit2Sharp.Repository.Clone(repoEntity.Project.CloneUrl, checkoutDir);
						}

						var repo = checkoutDir.OpenRepository(pullIfAlreadyExists: true,
							useRepoName: useRepoName, tempDirectory: checkoutPath);
						#endregion

						#region compute git-hours
						var gitCommitSpan = new GitCommitSpan(repo,
							sinceDateTimeOrCommitSha: repoEntity.SinceCommitSha1,
							untilDatetimeOrCommitSha: repoEntity.UntilCommitSha1);
						var pairs = gitCommitSpan.CommitPairs().ToList();
						var commits = repoEntity.Commits.ToDictionary(c => c.HashSHA1, c => c);

						var developersRaw = gitCommitSpan.FilteredCommits
							.GroupByDeveloperAsSignatures(repoEntity);
						var developers = developersRaw.ToDictionary(
							kv => kv.Key, kv =>
							{
								var foo = repoEntity.Developers.Count(dev => dev.Equals(kv.Value));
								return repoEntity.Developers.Where(dev => dev.Equals(kv.Value)).Single();
							});


						foreach (var pair in pairs)
						{
							var gitHoursAnalysesPerDeveloperAndHoursType =
								new ConcurrentDictionary<HoursTypeConfiguration, Dictionary<DeveloperEntity, IList<GitHoursAuthorSpan>>>();

							// Run all Analysis for each Hours-Type:
							Parallel.ForEach(Program.Configuration.HoursTypes, parallelOptions, hoursType =>
							{
								var addSuccess = gitHoursAnalysesPerDeveloperAndHoursType.TryAdd(hoursType,
									new GitHours.Hours.GitHours(
										gitCommitSpan, hoursType.MaxDiff, hoursType.FirstCommitAdd)
											.Analyze(repoEntity, HoursSpansDetailLevel.Detailed)
											.AuthorStats.ToDictionary(
												@as => @as.Developer as DeveloperEntity, @as => @as.HourSpans));

								if (!addSuccess)
								{
									throw new InvalidOperationException(
										$"Cannot add Hours-Type {hoursType.ToString()}.");
								}
							});


							// Check if Hours-type is already computed:
							var numComputed = session.QueryOver<HoursEntity>()
								.Where(he => he.Developer == developers[pair.Child.Author] && he.CommitUntil == commits[pair.Child.Sha]).RowCount();
							if (numComputed == gitHoursAnalysesPerDeveloperAndHoursType.Keys.Count)
							{
								return; // nothing to do
							}


							foreach (var hoursType in gitHoursAnalysesPerDeveloperAndHoursType.Keys)
							{
								// We need this for every HoursEntity that we use; it is already saved
								var hoursTypeEntity = hoursTypesEntities[hoursType];

								var gitHoursAnalysesPerDeveloper =
									gitHoursAnalysesPerDeveloperAndHoursType[hoursType];
								var developerSpans = gitHoursAnalysesPerDeveloper[developers[pair.Child.Author]]
									// OK because we analyzed with HoursSpansDetailLevel.Detailed
									.Cast<GitHoursAuthorSpanDetailed>().ToList();
								var hoursSpan = developerSpans.Where(hs => hs.Until == pair.Child).Single();


								var hoursEntity = new HoursEntity
								{
									InitialCommit = commits[hoursSpan.InitialCommit.Sha],
									CommitSince = hoursSpan.Since == null ? null : commits[hoursSpan.Since.Sha],
									CommitUntil = commits[hoursSpan.Until.Sha],
									Developer = developers[pair.Child.Author],
									Hours = hoursSpan.Hours,
									HoursTotal = developerSpans.Take(1 + developerSpans.IndexOf(hoursSpan))
										.Select(hs => hs.Hours).Sum(),

									HoursType = hoursTypeEntity,
									IsInitial = hoursSpan.IsInitialSpan,
									IsSessionInitial = hoursSpan.IsSessionInitialSpan
								};
								developers[pair.Child.Author].AddHour(hoursEntity);
							}

							using (var trans = session.BeginTransaction())
							{
								var hoursTemp = developers[pair.Child.Author].Hours.ToList();
								foreach (var hour in hoursTemp)
								{
									session.Save(hour);
								}
								trans.Commit();
							}
						}
						#endregion
					}
					catch (Exception)
					{
						File.AppendAllText(@"c:\users\admin\desktop\failed.txt", $"Repo failed: {repoEntity.ID} (Project: {repoEntity.Project.InternalId})\n");
						logger.LogError($"Repo failed: {repoEntity.ID} (Project: {repoEntity.Project.InternalId})");
					}

					logger.LogInformation("Finished repo in {0}: {1}",
						(DateTime.Now - start).ToString(), checkoutDir);
				}
			});
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

		[Option('i', "skip-initial-commit", Required = false, DefaultValue = false, HelpText = "If present, does not analyze the pair that consists of the 2nd and the initial commit to a repository.")]
		public Boolean SkipInitialCommit { get; set; }

		[Option('m', "skip-merge-commits", Required = false, DefaultValue = true, HelpText = "If present, does not analyze pairs where the younger commit is a merge commit.")]
		public Boolean SkipMergeCommits { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default. Be aware that the directory may be wiped at any point in time.")]
		public String TempDirectory { get; set; }

		[Option('s', "since", Required = false, HelpText = "Optional. Analyze data since a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Since { get; set; }

		[Option('u', "until", Required = false, HelpText = "Optional. Analyze data until (inclusive) a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Until { get; set; }

		[Option('e', "exec-policy", Required = false, DefaultValue = ExecutionPolicy.Parallel, HelpText = "Optional. Set the execution policy for the analysis. Allowed values are " + nameof(ExecutionPolicy.Parallel) + " and " + nameof(ExecutionPolicy.Linear) + ". The former is faster while the latter uses only minimal resources.")]
		public ExecutionPolicy ExecutionPolicy { get; set; }

		[Option('w', "no-wait", Required = false, DefaultValue = false, HelpText = "Optional. If present, then the program will exit after the analysis is finished. Otherwise, it will wait for the user to press a key by default.")]
		public Boolean NoWait { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest/most verbose to lowest/least verbose) Trace, Debug, Information, Warning, Error, Critical, None.")]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

		[Option("cmd-write-config", Required = false, DefaultValue = false, HelpText = "Command. If present, writes an exemplary 'configuration.json' file to the binary's location. Note that this will overwrite a may existing file. The program will terminate afterwards.")]
		public Boolean WriteExampeConfig { get; set; }

		#region Command-Options
		[Option("cmd-delete-repo-id", Required = false, HelpText = "Command. Removes analysis results for an entire RepositoryEntity and all of its associated entities, then terminates the program.")]
		public UInt32 DeleteRepoId { get; set; }

		[Option("cmd-recompute-githours", Required = false, DefaultValue = false, HelpText = "Command. Run the method " + nameof(Program.RecomputeGitHours) + "(). If called, the application will only run this and ignore/do nothing else. This method was intended to recompute git-hours of already analyzed repos and should not be used anymore.")]
		public Boolean CmdRecomputeGitHours { get; set; }
		#endregion

		[Option('h', "help", Required = false, DefaultValue = false, HelpText = "Print this help-text and exit.")]
		public Boolean ShowHelp { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		/// <summary>
		/// The library cannot parse Lists of Enumeration values, so we have to
		/// use this approach of selected languages.
		/// </summary>
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
