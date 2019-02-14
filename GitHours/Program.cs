/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2019 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHours. All files in this project,
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
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Util;
using Util.Data.Entities;
using Util.Extensions;
using Util.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GitHours
{
	/// <summary>
	/// Uses command-line options from <see cref="CommandLineOptions"/>. This program
	/// outputs its result as formatted JSON.
	/// </summary>
	class Program
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
		/// Main entry point of application git-hours.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options))
			{
				Program.LogLevel = options.LogLevel;
				logger.LogLevel = options.LogLevel;

				if (options.ShowHelp)
				{
					logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
					logger.LogInformation(options.GetUsage(wasHelpRequested: true));
					Environment.Exit((int)ExitCodes.OK);
				}

				// First let's create an actual temp-directory in the folder specified:
				var tempDirectory = new DirectoryInfo(Path.Combine(
					options.TempDirectory ?? Path.GetTempPath(), "GitDensity"));
				if (tempDirectory.Exists) { tempDirectory.Delete(true); }
				tempDirectory.Create();
				options.TempDirectory = tempDirectory.FullName;
				logger.LogDebug("Using temporary directory: {0}", options.TempDirectory);

				Repository repository = null;
				try
				{
					String useRepoName = null;
					if (options.RepoPath.IsHttpUrl())
					{
						var project = ProjectEntity.FromCloneUrl(options.RepoPath);
						useRepoName = !project.Name.IsNullOrEmptyOrWhiteSpace() &&
							!project.Owner.IsNullOrEmptyOrWhiteSpace() ?
							$"{project.Name}_{project.Owner}" : null;
					}

					var repoTempPath = Path.Combine(
						new DirectoryInfo(options.TempDirectory).Parent.FullName, $"GitDensity_repos");
					if (!Directory.Exists(repoTempPath))
					{
						Directory.CreateDirectory(repoTempPath);
					}

					repository = options.RepoPath.OpenRepository
						(repoTempPath, useRepoName: useRepoName);
				}
				catch (Exception ex)
				{
					logger.LogError(ex.Message);
					Environment.Exit((int)ExitCodes.RepoInvalid);
				}


				var outputToConsole = String.IsNullOrEmpty(options.OutputFile);
				if (!outputToConsole)
				{
					logger.LogWarning("Hello, this is GitHours.");
				}
				logger.LogDebug("You supplied the following arguments: {0}",
					String.Join(", ", args.Select(a => $"'{a}'")));


				try
				{
					using (repository)
					using (var span = new GitCommitSpan(repository, options.Since, options.Until))
					{
						var gitHours = new Hours.GitHours(span, options.MaxCommitDiff, options.FirstCommitAdd);

						var start = DateTime.Now;
						logger.LogDebug("Starting Analysis..");
						var obj = JsonConvert.SerializeObject(gitHours.Analyze(
							hoursSpansDetailLevel: options.IncludeHoursSpans ? Hours.HoursSpansDetailLevel.Standard : Hours.HoursSpansDetailLevel.None), Formatting.Indented);
						if (outputToConsole)
						{
							Console.Write(obj);
						}
						else
						{
							File.WriteAllText(options.OutputFile, obj);
							logger.LogInformation("Wrote the result to file: {0}", options.OutputFile);
						}
						logger.LogDebug("Analysis took {0}", DateTime.Now - start);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex.Message);
					Environment.Exit((int)ExitCodes.OtherError);
				}
			}
			else
			{
				logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
				logger.LogInformation(options.GetUsage(
					ExitCodes.UsageInvalid, wasHelpRequested: options.ShowHelp));
				Environment.Exit((int)ExitCodes.UsageInvalid);
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

		[Option('d', "max-commit-diff", Required = false, DefaultValue = 120u, HelpText = "Optional. Maximum difference in minutes between commits counted to one session")]
		public UInt32 MaxCommitDiff { get; set; }

		[Option('a', "first-commit-add", Required = false, DefaultValue = 120u, HelpText = "Optional. How many minutes first commit of session should add to total")]
		public UInt32 FirstCommitAdd { get; set; }

		[Option('o', "output-file", Required = false, HelpText = "Optional. Path to write the result to. If not specified, the result will be printed to std-out (and can be piped to a file manually).")]
		public String OutputFile { get; set; }

		[Option('i', "include-hours-spans", Required = false, DefaultValue = false, HelpText = "Whether or not to include Hours-Spans. If present, then the result will include the amount of hours spent between each pair of commits for each developer.")]
		public Boolean IncludeHoursSpans { get; set; }

		[Option('s', "since", Required = false, HelpText = "Optional. Analyze data since a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Since { get; set; }

		[Option('u', "until", Required = false, HelpText = "Optional. Analyze data until (inclusive) a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Until { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default.")]
		public String TempDirectory { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest/most verbose to lowest/least verbose) Trace, Debug, Information, Warning, Error, Critical, None.")]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

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
