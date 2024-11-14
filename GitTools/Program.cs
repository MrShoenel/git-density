/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
using GitTools.Prompting;
using GitTools.SourceExport;
using LINQtoCSV;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;
using Util.Data.Entities;
using Util.Extensions;
using Util.Logging;
using CompareOptions = LibGit2Sharp.CompareOptions;


namespace GitTools
{
	internal enum ExportCodeType
	{
		Commits,
		Files,
		Hunks,
		Blocks,
		Lines
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

		private static readonly BaseLogger<Program> logger = CreateLogger<Program>();

		/// <summary>
		/// Main entry point for application.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");

			var options = new CommandLineOptions();
			var optionsParseSuccess = Parser.Default.ParseArguments(args, options);

			if (options.CmdCountKeywords.HasValue && options.CmdCountKeywords.Value)
			{
				try
				{
					Program.Update_Gtools_ex();
					Environment.Exit((int)ExitCodes.OK);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}, {ex.StackTrace}");
					Environment.Exit((int)ExitCodes.OtherError);
				}
			}

			if (optionsParseSuccess)
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
							repo, options.Since, options.Until, options.Limit, sha1IDs, options.SinceUseDate, options.UntilUseDate);
						if (span.SHA1Filter.Count > 0)
						{
							logger.LogTrace($"Using the following commit-IDs for the {nameof(GitCommitSpan)}: {String.Join(", ", span.SHA1Filter)}");
						}

						#region Check for commands
						if (options.CmdCountCommits.HasValue && options.CmdCountCommits.Value)
						{
							logger.LogInformation($"Counting commits between {span.SinceAsString} and {span.UntilAsString}..");

							var commits = span.OrderBy(c => c.Author.When.UtcDateTime).ToList();

							var json = JsonConvert.SerializeObject(new
							{
								commits.Count,
								SHA1s = commits.Select(c => c.ShaShort())
							});

							using (var writer = String.IsNullOrWhiteSpace(options.OutputFile) ?
								Console.Out : File.CreateText(options.OutputFile))
							{
								writer.Write(json);
							}
							logger.LogInformation($"Wrote JSON to {(String.IsNullOrWhiteSpace(options.OutputFile) ? "console" : options.OutputFile)}.");
							Environment.Exit((int)ExitCodes.OK);
						}
						else if (options.CmdGeneratePrompts.HasValue && options.CmdGeneratePrompts.Value)
						{
							if (String.IsNullOrEmpty(options.TempDirectory))
							{
								logger.LogError("You must specify a temporary directory when generating prompts, because an individual output file is generated for each commit.");
								Environment.Exit((int)ExitCodes.UsageInvalid);
							}

							String inputTemplate = null;
							if (String.IsNullOrEmpty(options.CmdGeneratePrompts_Template))
							{
								logger.LogWarning("No input prompt template was given, using the empty default template: __SUMMARY__ \\n __CHANGELIST__.");
								inputTemplate = "__SUMMARY__ \n __CHANGELIST__";
							}
							else
							{
								inputTemplate = File.ReadAllText(path: options.CmdGeneratePrompts_Template);
							}

							using (span)
							{
								if (options.AnalysisType != AnalysisType.Extended)
								{
									logger.LogWarning($"Switching to {nameof(AnalysisType.Extended)} analysis because it is required for generating prompts.");
									options.AnalysisType = AnalysisType.Extended;
								}
								logger.LogInformation($"Generating prompts for Commits with IDs {String.Join(separator: ", ", values: sha1IDs)}..");

								var ea = new ExtendedAnalyzer(repoPathOrUrl: options.RepoPath, span: span, skipSizeAnalysis: false)
								{ ExecutionPolicy = options.ExecutionPolicy };
								var pg = new PromptGenerator(analyzer: ea, template: inputTemplate);

								foreach (var commitPrompt in pg)
								{
									var outputPath = Path.Combine(options.TempDirectory, $"{commitPrompt.CommitPair.Child.Id.Sha.Substring(0, 8)}.txt");
									File.WriteAllText(path: outputPath, contents: commitPrompt.ToString(), encoding: Encoding.UTF8);
								}
								Environment.Exit((int)ExitCodes.OK);
							}
						}
						else if (options.CmdExportCode != null)
						{
							if (!(options.OutputFile is string))
							{
								throw new ArgumentException("This command requires writing to a file.");
                            }

							try
                            {
								using (span)
								{
									var commits = span.FilteredCommits.ToList();
									// For each of the span's commits, we will make pairs of commit and parent.
									// This means, we will create one pair for each parent. Then, these pairs
									// are processed according to the policy and returned.
									var compOptions = new CompareOptions()
									{
										ContextLines = (Int32)options.CmdExport_ContextLines
									};

									var pairs = commits.SelectMany(commit =>
									{
										var parents = commit.Parents.ToList();
										if (parents.Count == 0)
										{
											parents.Add(null);
										}
										return parents.Select(parent => new ExportCommitPair(repo: repo, child: commit, parent: parent, compareOptions: compOptions));
									}).ToList();

									logger.LogInformation($"Found {commits.Count} Commits and {pairs.Count} pairs.");
									logger.LogInformation($"Processing all pairs {(options.ExecutionPolicy == ExecutionPolicy.Linear ? "sequentially" : "in parallel")}.");


									var resultsBag = new ConcurrentBag<IEnumerable<ExportableEntity>>();
									Parallel.ForEach(source: pairs, parallelOptions: new ParallelOptions() {
										MaxDegreeOfParallelism = options.ExecutionPolicy == ExecutionPolicy.Linear ? 1 :
											Math.Min(Environment.ProcessorCount, pairs.Count)
									}, body: pair =>
									{
										if (options.CmdExportCode == ExportCodeType.Commits)
										{
											resultsBag.Add(pair.AsCommits.ToList());
										}
										else if (options.CmdExportCode == ExportCodeType.Files)
										{
											resultsBag.Add(pair.AsFiles.ToList());
										}
										else if (options.CmdExportCode == ExportCodeType.Hunks)
										{
											resultsBag.Add(pair.AsHunks.ToList());
										}
										else if (options.CmdExportCode == ExportCodeType.Blocks)
										{
											resultsBag.Add(pair.AsBlocks.ToList());
										}
										else if (options.CmdExportCode == ExportCodeType.Lines)
										{
											resultsBag.Add(pair.AsLines.ToList());
										}
									});


									// Write the results:
									var allResults = resultsBag.SelectMany(x => x).OrderByDescending(ee => ee.ExportCommitPair.Child.Author.When.UtcDateTime).ToList();
									if (options.OutputFile.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
									{
										allResults.ForEach(r => r.ContentEncoding = options.CmdExport_Encoding);
									}


                                    if (options.CmdExportCode == ExportCodeType.Commits)
                                    {
                                        allResults.Cast<ExportableCommit>().WriteCsvOrJson(options.OutputFile);
                                    }
                                    else if (options.CmdExportCode == ExportCodeType.Files)
                                    {
                                        allResults.Cast<ExportableFile>().WriteCsvOrJson(options.OutputFile);
                                    }
                                    else if (options.CmdExportCode == ExportCodeType.Hunks)
                                    {
                                        allResults.Cast<ExportableHunk>().WriteCsvOrJson(options.OutputFile);
                                    }
                                    else if (options.CmdExportCode == ExportCodeType.Blocks)
                                    {
                                        allResults.Cast<ExportableBlock>().WriteCsvOrJson(options.OutputFile);
                                    }
                                    else if (options.CmdExportCode == ExportCodeType.Lines)
                                    {
                                        allResults.Cast<ExportableLine>().WriteCsvOrJson(options.OutputFile);
                                    }

									logger.LogInformation($"Wrote a total of {allResults.Count} {options.CmdExportCode.ToString()} entities to {options.OutputFile}.");
                                    Environment.Exit((int)ExitCodes.OK);
                                }
                            }
							catch (Exception ex)
							{
								logger.LogError($"An exception occurred: {ex.Message}");
								Environment.Exit((int)ExitCodes.CmdError);
							}
						}
						#endregion


						using (span)
						using (var writer = String.IsNullOrWhiteSpace(options.OutputFile) ?
								Console.Out : File.CreateText(options.OutputFile))
						{
							// Now we extract some info and write it out later.
							var csvc = new CsvContext();
							var outd = new CsvFileDescription
							{
								FirstLineHasColumnNames = true,
								FileCultureInfo = Thread.CurrentThread.CurrentUICulture,
								SeparatorChar = ',',
								QuoteAllFields = true,
								EnforceCsvColumnAttribute = true,
								TextEncoding = System.Text.Encoding.UTF8
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
								csvc.Write(details.Cast<SimpleCommitDetails>().OrderBy(c => c.CommitterTime), writer, outd);
							}
							else if (options.AnalysisType == AnalysisType.Extended)
							{
								csvc.Write(details.Cast<ExtendedCommitDetails>().OrderBy(c => c.CommitterTime), writer, outd);
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


		#region Update Gtools_Ex and add keywords
		internal class Gtools_ex_Keywords
		{
			private readonly CommitKeywordsEntity commitKeywordsEntity;
			public Gtools_ex_Keywords(CommitKeywordsEntity commitKeywordsEntity)
			{
				this.commitKeywordsEntity = commitKeywordsEntity;
			}

			public Boolean HasAny => CommitKeywordsEntity.KeywordProperties.Any(kwp => (UInt32)kwp.GetValue(this.commitKeywordsEntity) > 0u);

			public static String AsSetQuery => String.Join(", ", CommitKeywordsEntity.KeywordProperties.Select(kwp => $"{kwp.Name}=@_{kwp.Name}"));

			public void FillStmt(MySqlCommand stmt)
			{
				foreach (var kwp in CommitKeywordsEntity.KeywordProperties)
				{
					stmt.Parameters.AddWithValue($"@_{kwp.Name}", Math.Min(255u, (UInt32)kwp.GetValue(this.commitKeywordsEntity)));
				}
			}
		}

		internal static void Update_Gtools_ex()
		{
			var con = new MySqlConnection("server=localhost;port=3306;uid=root;pwd=root;database=comm_class;");
			var messages = new Dictionary<String, String>();

			con.Open();
			using (var com = con.CreateCommand())
			{
				//com.CommandType = System.Data.CommandType.Text;
				com.CommandText = "SELECT SHA1, Message From gtools_ex;";
				var reader = com.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						messages[reader.GetString(0)] = reader.GetString(1);
					}
					reader.Close();
				}
			}
				

			using (var trans = con.BeginTransaction(IsolationLevel.Serializable))
			{
				using (var stmt = con.CreateCommand())
				{
					stmt.CommandText = $"UPDATE gtools_ex SET {Gtools_ex_Keywords.AsSetQuery} WHERE SHA1=@sha1;";

					Console.WriteLine($"Messages: {messages.Count}");
					var cnt = 0;
					foreach (var kv in messages)
					{
						cnt++;
						if ((cnt % 500) == 0)
						{
							Console.WriteLine($"Progress is {(100d * cnt / messages.Count).ToString("000.00")} %");
						}

						var gex = new Gtools_ex_Keywords(CommitKeywordsEntity.FromMessage(kv.Value));
						if (!gex.HasAny)
						{
							continue;
						}

						stmt.Parameters.AddWithValue("@sha1", kv.Key);
						gex.FillStmt(stmt);

						stmt.ExecuteNonQuery();
						stmt.Parameters.Clear();
					}
				}

				trans.Commit();
			}
		}
		#endregion
	}


	/// <summary>
	/// Class that represents all options that can be supplied using
	/// the command-line interface of this application.
	/// </summary>
	internal class CommandLineOptions
	{
		[Option('r', "repo-path", Required = true, HelpText = "Absolute path or HTTP(S) URL to a git-repository. If a URL is provided, the repository will be cloned to a temporary folder first, using its defined default branch. Also allows passing in an Internal-ID of a project from the database.")]
		public String RepoPath { get; set; }

		[Option('o', "out-file", Required = false, HelpText = "A path to a file to write the analysis' result to. If left unspecified, output is written to the console.")]
		public String OutputFile { get; set; }

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

		[Option('a', "analysis-type", Required = false, DefaultValue = AnalysisType.Extended, HelpText = "Optional. The type of analysis to run. Allowed values are " + nameof(AnalysisType.Simple) + " and " + nameof(AnalysisType.Extended) + ". The extended analysis extracts all supported properties of any Git-repository.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public AnalysisType AnalysisType { get; set; } = AnalysisType.Extended;

		[Option('k', "skip-size", Required = false, DefaultValue = false, HelpText = "If specified, will skip any size-related measurements in the " + nameof(ExtendedCommitDetails) + ".")]
		public Boolean SkipSizeInExtendedAnalysis { get; set; }

		[Option('e', "exec-policy", Required = false, DefaultValue = ExecutionPolicy.Parallel, HelpText = "Optional. Set the execution policy for the analysis. Allowed values are " + nameof(ExecutionPolicy.Parallel) + " and " + nameof(ExecutionPolicy.Linear) + ". The former is faster while the latter uses only minimal resources.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public ExecutionPolicy ExecutionPolicy { get; set; } = ExecutionPolicy.Parallel;

		[Option('i', "input-ids", Required = false, HelpText = "Optional. A path to a file with SHA1's of commits to analyze (one SHA1 per line). If given, then only those commits will be analyzed and all others will be skipped. Can be used in conjunction with --limit.")]
		public String InputCommitIDs { get; set; }

		[Option("limit", Required = false, HelpText = "Optional. A positive integer to limit the amount of commits analyzed. Can be used in conjunction with any other options (such as -i).")]
		public UInt32? Limit { get; set; }

		[Option('l', "log-level", Required = false, DefaultValue = LogLevel.Information, HelpText = "Optional. The Log-level can be one of (highest/most verbose to lowest/least verbose) Trace, Debug, Information, Warning, Error, Critical, None.")]
		[JsonConverter(typeof(StringEnumConverter))]
		public LogLevel LogLevel { get; set; } = LogLevel.Information;

		#region Command-Options
		[Option("cmd-count-commits", Required = false, HelpText = "Command. Counts the amount of commits as delimited by since/until. Writes a JSON-formatted object to the console, including the commits' IDs.")]
		public Boolean? CmdCountCommits { get; set; }

		[Option("cmd-count-keywords", Required = false, HelpText = "Command. Counts the keywords on messages in entities already existing in table Gtools_Ex and updates each with the amount of keywords found. Writes some progress to the console.")]
		public Boolean? CmdCountKeywords { get; set; }

		[Option("cmd-generate-prompts", Required = false, HelpText = "Command. Generate prompts using a template for the selected commits. These prompts can be used for, e.g., Large Language Models.")]
		public Boolean? CmdGeneratePrompts { get; set; }

        [Option('p', "prompt-template", Required = false, HelpText = "Optional. Option for the command --cmd-generate-prompts. A path to a file that holds a template for generating prompts.")]
        public String CmdGeneratePrompts_Template { get; set; }

        [Option("cmd-export-source", Required = false, HelpText = "Command. Export source code from a repository. If present, needs to be one of " + nameof(ExportCodeType.Commits) + ", " + nameof(ExportCodeType.Files) + ", " + nameof(ExportCodeType.Hunks) + ", " + nameof(ExportCodeType.Blocks) + ", or " + nameof(ExportCodeType.Lines) + ". This will determine the granularity and level of detail that is included for each exported entity. Exports as CSV or JSON.")]
		public ExportCodeType? CmdExportCode { get; set; }

		[Option("content-encoding", Required = false, DefaultValue = ContentEncoding.Plain, HelpText = "Option for the command --cmd-export-source. Sets how the content of entities is encoded when exporting CSV. Must be one of " + nameof(ContentEncoding.Plain) + ", " + nameof(ContentEncoding.Base64) + ", or " + nameof(ContentEncoding.JSON) + ". " + nameof(ContentEncoding.Plain) + " is not recommended for CSV files. When exporting as JSON, this setting is ignored.")]
		public ContentEncoding CmdExport_Encoding { get; set; }

		[Option("context-lines", Required = false, DefaultValue = 3u, HelpText = "Option for the command --cmd-export-source. The number of unchanged lines that define the boundary of a hunk (and to display before and after). If this value is large, then hunks will start to collapse into each other. This option is useful when exporting hunks, files, and commits. E.g., setting it to " + nameof(Int32.MaxValue) + " yields one hunk per file and per commit.")]
		public UInt32 CmdExport_ContextLines { get; set; }
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
