/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitHours. All files in this project,
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
using CommandLine;
using CommandLine.Text;
using GitDensity.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GitHours
{
	internal enum ExitCodes : Int32
	{
		OK = 0,
		RepoInvalid = -2,
		UsageInvalid = -3
	}

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
			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options))
			{
				Program.LogLevel = options.LogLevel;
				logger.LogLevel = options.LogLevel;

				if (options.ShowHelp)
				{
					logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
					logger.LogInformation(options.GetUsage());
					Environment.Exit((int)ExitCodes.OK);
				}

				try
				{
					using (var repo = options.RepoPath.OpenRepository(options.TempDirectory))
					{
						var span = new GitHours.Hours.GitHoursSpan(repo, options.Since, options.Until);
						var ic = CultureInfo.InvariantCulture;
						var gitHours = new Hours.GitHours(span, options.MaxCommitDiff, options.FirstCommitAdd);

						var obj = JsonConvert.SerializeObject(gitHours.Analyze(), Formatting.Indented);
						if (String.IsNullOrEmpty(options.OutputFile))
						{
							Console.Write(obj);
						}
						else
						{
							File.WriteAllText(options.OutputFile, obj);
							logger.LogInformation("Wrote the result to file: {0}", options.OutputFile);
						}
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex.Message);
					Environment.Exit((int)ExitCodes.RepoInvalid);
				}
			}
			else
			{
				logger.LogCurrentScope = logger.LogCurrentTime = logger.LogCurrentType = false;
				logger.LogInformation(options.GetUsage(ExitCodes.UsageInvalid));
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

		[Option('s', "since", Required = false,HelpText = "Optional. Analyze data since a certain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Since { get; set; }

		[Option('u', "until", Required = false, HelpText = "Optional. Analyze data until (inclusive) acertain date or SHA1. The required format for a date/time is 'yyyy-MM-dd HH:mm'. If using a hash, at least 3 characters are required.")]
		public String Until { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default.")]
		public String TempDirectory { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest to lowest) Trace, Debug, Information, Warning, Error, Critical, None.")]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

		[Option('h', "help", Required = false, DefaultValue = false, HelpText = "Print this help-text and exit.")]
		public Boolean ShowHelp { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		/// <summary>
		/// Returns a help-text generated using the options of this class.
		/// </summary>
		/// <returns></returns>
		public String GetUsage(ExitCodes exitCode = ExitCodes.OK)
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

			HelpText.DefaultParsingErrorsHandler(this, ht);
			ht.AddOptions(this);

			var exitCodes = String.Join(", ", Enum.GetValues(typeof(ExitCodes)).Cast<ExitCodes>().Select(ec => $"{ec.ToString()} ({(int)ec})"));
			var exitReason = exitCode == ExitCodes.UsageInvalid ?
				"Error: The given parameters are invalid and cannot be parsed. You must not specify unrecognized parameters. Please check the usage below.\n\n" : String.Empty;

			return $"{fullLine}\n{exitReason}{ht}\n\nPossible Exit-Codes: {exitCodes}\n\n{fullLine}";
		}
	}
}
