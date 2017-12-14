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
using FluentNHibernate.Cfg.Db;
using GitDensity.Data;
using GitDensity.Util;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GitDensity.Util.RepositoryExtensions;
using CC = GitDensity.Util.ColoredConsole;

namespace GitDensity
{
	class Program
	{
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


		/// <summary>
		/// Main entry point for application.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			//using (var repo = new Repository(@"C:\repos\__dummies\merge-test-octo"))
			//{
			//	var commit = repo.Commits.First();
			//}

			CC.TransparentLine();
			var configFilePath = Path.Combine(WorkingDirOfExecutable, Util.Configuration.DefaultFileName);
			var options = new CommandLineOptions();

			if (Parser.Default.ParseArguments(args, options))
			{
				if (options.ShowHelp)
				{
					CC.Line(options.GetUsage());
					Environment.Exit(0);
				}
				else if (options.WriteExampeConfig || !File.Exists(configFilePath))
				{
					CC.YellowLine("Writing example to file: {0}", configFilePath);
					CC.TransparentLine();

					File.WriteAllText(configFilePath,
						JsonConvert.SerializeObject(Util.Configuration.Example, Formatting.Indented));
					Environment.Exit(0);
				}


				// Now let's read the configuration:
				CC.TransparentLine();
				try
				{
					Program.Configuration = JsonConvert.DeserializeObject<Util.Configuration>(
						File.ReadAllText(configFilePath));
					CC.YellowLine("Successfully read the configuration.");

					DataFactory.Configure(Program.Configuration);
				}
				catch
				{
					CC.RedLine("Error reading the configuration. Perhaps try to generate and derive an example configuration (use '--help').");
					CC.SetInitialColors();
					Environment.Exit(-1);
				}
			}

			CC.SetInitialColors();
		}
	}


	/// <summary>
	/// Class that represents all options that can be supplied using
	/// the command-line interface of this application.
	/// </summary>
	internal class CommandLineOptions
	{
		[Option('s', "skip-clone", Required = false, DefaultValue = false, HelpText = "[Optional] If true, clone-detection will not be used as a similarity-metric. Otherwise, it needs to be properly configured using the file 'configuration.json'.")]
		public Boolean SkipCloneDetection { get; set; }

		[Option('c', "write-config", Required = false, DefaultValue = false, HelpText = "If specified, writes an examplary 'configuration.json' file to the binary's location. Note that this will overwrite a may existing file.")]
		public Boolean WriteExampeConfig { get; set; }

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
