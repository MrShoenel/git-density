using CommandLine;
using CommandLine.Text;
using GitDensity.Util;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			var repo = new LibGit2Sharp.Repository(@"C:\repos\__dummies\merge-test");
			using (repo)
			{
				var allCommits = repo.Commits.ToArray();
				for (var i = 0; i < allCommits.Length - 1; i++)
				{
					CC.YellowLine("Compare parent {0} with child {1}", allCommits[i].Id, allCommits[i + 1].Id);
					CC.SetInitialColors();
					var patch = repo.Diff.Compare<Patch>(allCommits[i + 1].Tree, allCommits[i].Tree);
					Console.WriteLine(patch);
				}


				//var fo2o = repo.Commits.Skip(3).First().Parents.ToList();
				//var tuplesParentWithChild = new List<Tuple<Commit, Commit>>();

				//foreach (var commit in repo.Commits)
				//{
				//	foreach (var parent in commit.Parents)
				//	{
				//		tuplesParentWithChild.Add(Tuple.Create(commit, parent));
				//	}
				//}

				//foreach (var tuple in tuplesParentWithChild)
				//{
				//	CC.YellowLine("Compare parent {0} with child {1}", tuple.Item1.Id, tuple.Item2.Id);
				//	CC.SetInitialColors();
				//	var patch = repo.Diff.Compare<Patch>(tuple.Item2.Tree, tuple.Item1.Tree);
				//	Console.WriteLine(patch);
				//}





				//var queue = repo.Commits.Reverse().Skip(1).ToArray(); // skip initial commit and oldest first
				//var tuplesPrevNextCommit = new List<Tuple<Commit, Commit>>();
				//for (var i = 0; i < queue.Length - 1; i++)
				//{
				//	tuplesPrevNextCommit.Add(Tuple.Create(queue[i], queue[i + 1]));
				//}

				//foreach (var tuple in tuplesPrevNextCommit)
				//{
				//	var diff = repo.Diff.Compare<Patch>(tuple.Item1.Tree, tuple.Item2.Tree);
				//}



				var difffff = repo.Diff.Compare<Patch>(repo.Commits.Last().Tree, repo.Commits.Skip(2).First().Tree);
				foreach (var file in difffff)
				{

				}

				List<Commit> CommitList = new List<Commit>();
				foreach (LogEntry entry in repo.Commits.QueryBy("class.js").ToList())
				{
					CommitList.Add(entry.Commit);
				}
				CommitList.Add(null); // Added to show correct initial add

				int ChangeDesired = 0; // Change difference desired
				while (ChangeDesired + 1 < CommitList.Count)
				{
					var repoDifferences = repo.Diff.Compare<Patch>((Equals(CommitList[ChangeDesired + 1], null)) ? null : CommitList[ChangeDesired + 1].Tree, (Equals(CommitList[ChangeDesired], null)) ? null : CommitList[ChangeDesired].Tree);
					PatchEntryChanges file = null;
					try { file = repoDifferences.First(e => e.Path == "class.js"); }
					catch { } // If the file has been renamed in the past- this search will fail
					if (!Equals(file, null))
					{
						Console.WriteLine(file.Patch);
					}
					ChangeDesired++;
				}
			}
			LibGit2Sharp.Commands.Checkout(repo, repo.Branches["master"]);
			var bran = repo.Branches.Skip(1).First();
			var foo = bran.Commits.ToList();
			var comm = repo.Commits.Skip(10).First();
			var parr = comm.Parents.ToList();
			var tree = comm.Tree.ToList();

			repo.Reset(LibGit2Sharp.ResetMode.Hard, comm);

			repo.CheckoutPaths(comm.Sha, new String[] { "./" }, new LibGit2Sharp.CheckoutOptions {
				CheckoutModifiers = LibGit2Sharp.CheckoutModifiers.Force,
				CheckoutNotifyFlags = LibGit2Sharp.CheckoutNotifyFlags.None
			});


			CC.TransparentLine();
			var options = new CommandLineOptions();
			if (Parser.Default.ParseArguments(args, options))
			{
				if (options.ShowHelp)
				{
					CC.Line(options.GetUsage());
					Environment.Exit(0);
				}
				else if (options.WriteExampeConfig)
				{
					var path = Path.Combine(WorkingDirOfExecutable, "configuration.json");
					CC.YellowLine("Writing example to file: {0}", path);
					CC.TransparentLine();

					File.WriteAllText(path,
						JsonConvert.SerializeObject(Util.Configuration.Example, Formatting.Indented));
					Environment.Exit(0);
				}


				// Now let's read the configuration:
				CC.TransparentLine();
				try
				{
					Program.Configuration = JsonConvert.DeserializeObject<Util.Configuration>(
						File.ReadAllText(Path.Combine(WorkingDirOfExecutable, "configuration.json")));
					CC.YellowLine("Successfully read the configuration.");
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
