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
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
	internal enum ExitCodes : Int32
	{
		OK = 0,
		ConfigError = -1,
		RepoInvalid = -2,
		UsageInvalid = -3
	}

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


				// Now let's try to open the specified repository:
				try
				{
					using (var repo = options.RepoPath.OpenRepository(options.TempDirectory))
					{
						var span = new GitHoursSpan(repo, options.Since, options.Until);

						// Instantiate the Density analysis with the selected programming
						// languages' file extensions and other options from the command line.
						using (var density = new Density.GitDensity(
							span, options.ProgrammingLanguages,
							options.SkipInitialCommit, options.SkipMergeCommits,
							Configuration.LanguagesAndFileExtensions
								.Where(kv => options.ProgrammingLanguages.Contains(kv.Key))
								.SelectMany(kv => kv.Value),
							options.TempDirectory))
						{
							density.ExecutionPolicy = options.ExecutionPolicy;
							density.InitializeStringSimilarityMeasures(
								typeof(Util.Data.Entities.SimilarityEntity),
								new HashSet<SimilarityMeasurementType>(
									SimilarityMeasurementType.None.AsEnumerable().Concat(
									Configuration.EnabledSimilarityMeasurements
									.Where(kv => kv.Value).Select(kv => kv.Key))));

							var start = DateTime.Now;
							logger.LogWarning("Starting Analysis..");
							var result = density.Analyze();
							logger.LogWarning("Analysis took {0}", DateTime.Now - start);

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
		}
	}


	/// <summary>
	/// Class that represents all options that can be supplied using
	/// the command-line interface of this application.
	/// </summary>
	internal class CommandLineOptions
	{
		[Option('r', "repo-path", Required = true, HelpText = "Absolute path or HTTP(S) URL to a git-repository. If a URL is provided, the repository will be cloned to a temporary folder first, using its defined default branch.")]
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

		[Option('c', "write-config", Required = false, DefaultValue = false, HelpText = "Optional. If present, writes an exemplary 'configuration.json' file to the binary's location. Note that this will overwrite a may existing file.")]
		public Boolean WriteExampeConfig { get; set; }

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
			var fullLine = new String('-', Console.WindowWidth);
			var ht = new HelpText
			{
				Heading = HeadingInfo.Default,
				Copyright = CopyrightInfo.Default,
				AdditionalNewLineAfterOption = true,
				AddDashesToOption = true,
				MaximumDisplayWidth = Console.WindowWidth
			};

			if (wasHelpRequested)
			{
				ht.AddOptions(this);
			}
			else
			{
				HelpText.DefaultParsingErrorsHandler(this, ht);
			}
			
			var exitCodes = !wasHelpRequested ? String.Empty : "\n\n> Possible Exit-Codes: " + String.Join(", ", Enum.GetValues(typeof(ExitCodes)).Cast<ExitCodes>()
				.OrderByDescending(e => (int)e).Select(ec => $"{ec.ToString()} ({(int)ec})"));
			var supportedLanguages = !wasHelpRequested ? String.Empty : "\n\n> Supported programming languages: " + String.Join(", ", Configuration.LanguagesAndFileExtensions.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key.ToString()} ({String.Join(", ", kv.Value.Select(v => $".{v}"))})"));
			var implementedSimilarities = !wasHelpRequested ? String.Empty : "\n\n> Implemented similarity measurements: " + String.Join(", ", SimilarityEntity.SmtToPropertyInfo.Keys
				.OrderBy(smt => (int)smt)
				.Select(smt => $"{smt.ToString()} ({(int)smt})")
			);
			var exitReason = exitCode == ExitCodes.UsageInvalid && !wasHelpRequested ?
				"Error: The given parameters are invalid and cannot be parsed. You must not specify unrecognized parameters. Use '-h' or '--help' to get the full usage info.\n\n" : String.Empty;

			return $"{fullLine}\n{exitReason}{ht}{supportedLanguages}{implementedSimilarities}{exitCodes}\n\n{fullLine}";
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
