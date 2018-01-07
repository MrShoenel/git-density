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
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Globalization;
using CC = GitDensity.Util.ColoredConsole;

namespace GitHours
{
	/// <summary>
	/// Uses command-line options from <see cref="CommandLineOptions"/>. This program
	/// outputs its result as formatted JSON.
	/// </summary>
	class Program
	{
		static void Main(string[] args)
		{
			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options))
			{
				if (options.ShowHelp)
				{
					CC.Line(options.GetUsage());
					Environment.Exit(0);
				}

				try
				{
					using (var repo = new Repository(options.RepoPath))
					{
						var ic = CultureInfo.InvariantCulture;
						var gitHours = new Hours.GitHours(repo, options.MaxCommitDiff, options.FirstCommitAdd,
							options.Since == null ? (DateTime?)null : DateTime.ParseExact(options.Since, Hours.GitHours.DateTimeFormat, ic),
							options.Until == null ? (DateTime?)null : DateTime.ParseExact(options.Until, Hours.GitHours.DateTimeFormat, ic));

						var obj = gitHours.Analyze();
						Console.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
					}
				}
				catch (Exception ex)
				{
					CC.RedLine(ex.Message);
					CC.SetInitialColors();
					Environment.Exit(-1);
				}
			}
			else
			{
				CC.RedLine("The supplied options are not valid.");
				CC.WhiteLine(options.GetUsage());
				CC.SetInitialColors();
				Environment.Exit(-1);
			}
		}
	}


	/// <summary>
	/// Class that represents all options that can be supplied using
	/// the command-line interface of this application.
	/// </summary>
	internal class CommandLineOptions
	{
		[Option('r', "repo-path", Required = true, DefaultValue = null, HelpText = "Absolute path to a git-repository.")]
		public String RepoPath { get; set; }

		[Option('d', "max-commit-diff", Required = false, DefaultValue = 120u, HelpText = "[Optional] Maximum difference in minutes between commits counted to one session")]
		public UInt32 MaxCommitDiff { get; set; }

		[Option('a', "first-commit-add", Required = false, DefaultValue = 120u, HelpText = "[Optional] How many minutes first commit of session should add to total")]
		public UInt32 FirstCommitAdd { get; set; }

		[Option('s', "since", Required = false, DefaultValue = null, HelpText = "[Optional] Analyze data since certain date. The required format is 'yyyy-MM-dd HH:mm'.")]
		public String Since { get; set; }

		[Option('u', "until", Required = false, DefaultValue = null, HelpText = "[Optional] Analyze data until (exclusive) certain date. The required format is 'yyyy-MM-dd HH:mm'.")]
		public String Until { get; set; }

		[Option('h', "help", Required = false, DefaultValue = false, HelpText = "Print this help-text and exit.")]
		public Boolean ShowHelp { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		/// <summary>
		/// Returns a help-text generated using the options of this class.
		/// </summary>
		/// <returns></returns>
		public String GetUsage()
		{
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
			return ht;
		}
	}
}
