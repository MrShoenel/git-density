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
using GitDensity.Data;
using GitDensity.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Configuration = GitDensity.Util.Configuration;
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
		public static Util.Configuration Configuration { private set; get; }

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
					logger.LogInformation(options.GetUsage());
					Environment.Exit((int)ExitCodes.OK);
				}
				else if (options.WriteExampeConfig || !File.Exists(configFilePath))
				{
					logger.LogCritical("Writing example to file: {0}", configFilePath);

					File.WriteAllText(configFilePath,
						JsonConvert.SerializeObject(Util.Configuration.Example, Formatting.Indented));
					Environment.Exit((int)ExitCodes.OK);
				}


				// Now let's read the configuration and probe the configured DB:
				try
				{
					try
					{
						Program.Configuration = JsonConvert.DeserializeObject<Util.Configuration>(
							File.ReadAllText(configFilePath));
					}
					catch (Exception ex)
					{
						throw new IOException("Error reading the configuration. Perhaps try to generate and derive an example configuration (use '--help')", ex);
					}

					logger.LogInformation("Successfully read the configuration.");

					DataFactory.Configure(Program.Configuration, options.TempDirectory);
					using (var tempSess = DataFactory.Instance.OpenSession())
					{
						logger.LogInformation("Successfully probed the configured database.");
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
						var pair = new Density.CommitPair(repo, repo.Commits.Where(c => c.Sha.StartsWith("85d")).First(), repo.Commits.Where(c => c.Sha.StartsWith("473")).First());
						var patch = repo.Diff.Compare<LibGit2Sharp.Patch>(pair.Parent.Tree, pair.Child.Tree, new String[] { "GitDensity/Program.cs" });
						var hunks = Density.Hunk.HunksForPatch(patch.First()).ToList();

						//var simm = new Similarity.Similarity(hunks.Last(), Enumerable.Empty<GitDensity.Density.CloneDensity.ClonesXmlSet>());

						// Instantiate the Density analysis with the selected programming
						// languages' file extensions and other options from the command line.
						var density = new Density.GitDensity(repo, options.ProgrammingLanguages, options.SkipInitialCommit, options.SkipMergeCommits, Configuration.LanguagesAndFileExtensions.Where(kv => options.ProgrammingLanguages.Contains(kv.Key)).SelectMany(kv => kv.Value), options.TempDirectory);

						density.InitializeStringSimilarityMeasures(typeof(Data.Entities.SimilarityEntity));

						var result = density.Analyze();
















						////var rStatus = repo.RetrieveStatus();
						////var foo = rStatus.ToList();
						////var cmp = repo.Diff.Compare<TreeChanges>(new String[] { "./" });

						////var pair = repo.CommitPairs().Skip(7).First();
						//var pair = repo.CommitPairs(sortOrder: SortOrder.LatestFirst).Skip(1).First();
						//pair.WriteOutTree(pair.TreeChanges, new DirectoryInfo(@"C:\users\admin\desktop\x"), true);
						//var cdw = new CloneDetectionWrapper(new DirectoryInfo(@"C:\users\admin\desktop\x"), ProgrammingLanguage.Java, new DirectoryInfo(options.TempDirectory));
						//var ll = cdw.PerformCloneDetection().ToList();

						//var patch = pair.Patch.Reverse().First();
						//var treec = pair.TreeChanges;

						//var hList = Density.Hunk.HunksForPatch(pair.Patch.Reverse().First()).ToList();
						//var h = new Density.Hunk(patch.Patch);

						//var m = treec.Modified.First();

						//Console.WriteLine(patch);
						//Console.WriteLine(treec);
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

		/// <summary>
		/// To obtains the actual <see cref="ICollection{ProgrammingLanguage}"/>s, use the
		/// property <see cref="ProgrammingLanguages"/>.
		/// </summary>
		[OptionList('p', "prog-langs", ',', Required = true, HelpText = "A comma-separated list of programming languages to examine in the given repository. Other files will be ignored.")]
		public IList<String> LanguagesRaw { get; set; }

		[Option('i', "skip-initial-commit", Required = false, DefaultValue = false, HelpText = "If true, does not analyze the pair that consists of the 2nd and the initial commit to a repository.")]
		public Boolean SkipInitialCommit { get; set; }

		[Option('m', "skip-merge-commits", Required = false, DefaultValue = true, HelpText = "If true, does not analyze pairs where the younger commit is a merge commit.")]
		public Boolean SkipMergeCommits { get; set; }

		[Option('c', "write-config", Required = false, DefaultValue = false, HelpText = "Optional. If specified, writes an examplary 'configuration.json' file to the binary's location. Note that this will overwrite a may existing file.")]
		public Boolean WriteExampeConfig { get; set; }

		[Option('t', "temp-dir", Required = false, HelpText = "Optional. A fully qualified path to a custom temporary directory. If not specified, will use the system's default. Be aware that the directory may be wiped at any point in time.")]
		public String TempDirectory { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest to lowest) Trace, Debug, Information, Warning, Error, Critical, None.")]
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
			var supportedLanguages = String.Join(", ", Configuration.LanguagesAndFileExtensions.OrderBy(kv => kv.Key.ToString()).Select(kv => $"{kv.Key.ToString()} ({String.Join(", ", kv.Value.Select(v => $".{v}"))})"));
			var exitReason = exitCode == ExitCodes.UsageInvalid ?
				"Error: The given parameters are invalid and cannot be parsed. You must not specify unrecognized parameters. Please check the usage below.\n\n" : String.Empty;

			return $"{fullLine}\n{exitReason}{ht}\n\n> Supported programming languages: {supportedLanguages}\n\n> Possible Exit-Codes: {exitCodes}\n\n{fullLine}";
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
