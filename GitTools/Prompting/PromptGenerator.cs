using GitDensity.Density;
using GitDensity.Similarity;
using GitTools.Analysis.ExtendedAnalyzer;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Util.Density;
using Util.Extensions;
using Line = GitDensity.Similarity.Line;

namespace GitTools.Prompting
{
	/// <summary>
	/// This class can take a list of commits and transform them into a summary
	/// of natural text. This summary can be used with Large Language Models.
	/// </summary>
	public class PromptGenerator : IEnumerable<CommitPrompt>
	{
		protected ISet<ExtendedCommitDetails> commitDetails;

		protected IReadOnlyDictionary<Commit, CommitPair> commitPairs;

		public ExtendedAnalyzer Analyzer { protected set; get; }

		/// <summary>
		/// The template that will be used for the prompt of each commit. The template
		/// can have the following placeholders:
		/// - __SUMMARY__: This will be replaced with the summary of a commit.
		/// - __CHANGELIST__: This will be replaced with the list of changes of a commit.
		/// </summary>
		public String Template { protected set; get; }

		public PromptGenerator(ExtendedAnalyzer analyzer, String template)
		{
			this.Analyzer = analyzer;
			this.Template = template;
			this.commitDetails = new HashSet<ExtendedCommitDetails>(analyzer.AnalyzeCommits());
			var pairs = analyzer.GitCommitSpan.CommitPairs(skipInitialCommit: false, skipMergeCommits: true).ToList();

			this.commitPairs = new ReadOnlyDictionary<Commit, CommitPair>(this.commitDetails.Select(cd =>
			{
				// This will and should throw if there is not a 1:1 match.
				var pair = pairs.First(p => p.Child == cd.Commit);
				return new { Key = cd.Commit, Value = pair };
			}).ToDictionary(o => o.Key, o => o.Value));
		}

		public IEnumerator<CommitPrompt> GetEnumerator()
		{
			foreach (var details in this.commitDetails)
			{
				yield return new CommitPrompt(commitDetails: details, commitPair: this.commitPairs[details.Commit], template: this.Template);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}


	public class FullTextBlock : TextBlock
	{
		public FullTextBlock(Hunk hunk)
		{
			var idx = 0;
			foreach (var line in hunk.Patch.GetLines())
			{
				var firstChar = line.Length == 0 ? '_' : line[0];
				var added = firstChar == '+';
				var untouched = firstChar != '-' && firstChar != '+';

				// Let's add all lines.
				if (untouched && line.IsEmptyOrWhiteSpace())
				{
					// Empty, non-changed lines do not provide any extra context.
					continue;
				}
				if (line.Length == 1 && !untouched)
				{
					this.AddLine(new Line(added ? LineType.Added : LineType.Deleted, (UInt32)idx++, String.Empty));
				}
				else
				{
					// Ordinary lines:
					this.AddLine(new Line(
						added ? LineType.Added : (untouched ? LineType.Untouched : LineType.Deleted),
						// remove first character (white or +/-)
						(UInt32)idx++, (line.Length > 1 ? line.Substring(1) : line).Trim()));
				}
			}
		}
	}


	public class CommitPrompt
	{
		public ExtendedCommitDetails CommitDetails { protected set; get; }

		public CommitPair CommitPair { protected set; get; }

		protected Lazy<String> lazySummary, lazyHunkChanges;

		/// <summary>
		/// Returns the summary of all commits as a human-readable string in natural language.
		/// </summary>
		public String Summary => this.lazySummary.Value;

		/// <summary>
		/// Returns a list of changes made to the
		/// </summary>
		public String HunkChanges => this.lazyHunkChanges.Value;

		public String Template { protected set; get; }

		public CommitPrompt(ExtendedCommitDetails commitDetails, CommitPair commitPair, String template)
		{
			this.CommitDetails = commitDetails;
			this.CommitPair = commitPair;
			this.Template = template;
			this.lazySummary = new Lazy<string>(this.GenerateSummary);
			this.lazyHunkChanges = new Lazy<string>(() => String.Join("\n\n", this.GenerateHunkChanges()));
		}

		public override String ToString()
		{
			return this.Template.Replace("__SUMMARY__", this.Summary).Replace("__CHANGELIST__", this.HunkChanges);
		}

		protected IEnumerable<String> GenerateHunkChanges()
		{
			var relevantTreeChanges = this.CommitPair.RelevantTreeChanges.Where(rtc =>
			{
				return rtc.Status == ChangeKind.Added || rtc.Status == ChangeKind.Deleted ||
					rtc.Status == ChangeKind.Modified || rtc.Status == ChangeKind.Renamed;
			}).ToList();

			foreach (var change in relevantTreeChanges)
			{
				var deleted = change.Status == ChangeKind.Deleted;
				var path = deleted ? change.OldPath : change.Path;
				var patch = this.CommitPair.Patch[path];

				var hunkChanges = Hunk.HunksForPatch(patch).Select(hunk =>
				{
					var tb = new FullTextBlock(hunk: hunk);

					return $"\n-----\nChanges in lines {hunk.NewLineStart} to {hunk.NewLineStart + hunk.NewNumberOfLines - 1}:\n-----\n" + String.Join("\n", tb.LinesWithNumber.Values.Select(l => {
							var useChar = l.Type == LineType.Untouched ? ".." : (l.Type == LineType.Added ? "++" : "--");
							return $"[{useChar}] " + l.String + $" [/{useChar}]";
						}));
				});

				yield return $"The markup [..][/..], [++][/++], and [--][/--] encloses an unchanged, added, or removed line, respectively.\n\nIn the file {path}, the following changes were made:\n" + String.Join("\n", hunkChanges);
			}
		}

		protected String GenerateSummary()
		{
			var cd = this.CommitDetails;
			var added = cd.NumberOfLinesAddedByAddedFiles + cd.NumberOfLinesAddedByModifiedFiles + cd.NumberOfLinesAddedByRenamedFiles;
			var removed = cd.NumberOfLinesDeletedByDeletedFiles + cd.NumberOfLinesDeletedByModifiedFiles + cd.NumberOfLinesDeletedByRenamedFiles;

			return $"The message of this commit was: [\"{cd.Commit.Message.Trim()}\"]. " +
				"In this commit, a total of " +
				$"{cd.NumberOfFilesAdded} files were added, {cd.NumberOfFilesDeleted} files were removed, " +
				$"{cd.NumberOfFilesModified} files were modified, and {cd.NumberOfFilesRenamed} files were renamed. " +
				$"In total, {added} lines of code were added, and {removed} lines of code were removed. " +
				$"What follows is a summary for each added/changed/removed/renamed file.\n\n";
		}
	}
}
