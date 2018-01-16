/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
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
using GitDensity.Util;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitDensity.Density
{
	/// <summary>
	/// Represents lines added/removed/changed within a diff. Note that a diff
	/// can consist of multiple <see cref="Hunk"/>s for one file. Use
	/// <see cref="HunksForPatch(PatchEntryChanges)"/> to obtain all hunks for
	/// a <see cref="PatchEntryChanges"/> for one file.
	/// </summary>
	internal class Hunk
	{
		/// <summary>
		/// Used to split and analyze a git-diff hunk.
		/// </summary>
		protected internal static readonly Regex HunkSplitRegex =
			new Regex(@"^@@\s+\-([0-9]+),([0-9]+)\s+\+([0-9]+),([0-9]+)\s+@@.*$", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ECMAScript);

		public UInt32 OldLineStart { get; protected internal set; }

		public UInt32 OldNumberOfLines { get; protected internal set; }

		public UInt32 NewLineStart { get; protected internal set; }

		public UInt32 NewNumberOfLines { get; protected internal set; }

		public String SourceFilePath { get; protected internal set; }

		public String TargetFilePath { get; protected internal set; }

		public UInt32 NumberOfLinesAdded { get { return (UInt32)this.lineNumbersAdded.Count; } }

		public UInt32 NumberOfLinesDeleted { get { return (UInt32)this.lineNumbersDeleted.Count; } }
		
		internal String Patch { get; private set; }

		protected IList<UInt32> lineNumbersAdded;

		protected IList<UInt32> lineNumbersDeleted;

		private Hunk(String patch)
		{
			this.Patch = patch;

			this.lineNumbersAdded = new List<UInt32>();
			this.lineNumbersDeleted = new List<UInt32>();
		}

		/// <summary>
		/// Computes the numbers (their index) of lines that have been added
		/// or removed. Note that indexes start with 1, not 0.
		/// It is essential to only call this method _after_ the properties
		/// <see cref="OldLineStart"/> and <see cref="NewLineStart"/> have been
		/// set; otherwise, the computed numbers will be off by these.
		/// </summary>
		/// <returns>This <see cref="Hunk"/> for chaining.</returns>
		private Hunk ComputeLinesAddedAndDeleted()
		{
			var idxOld = this.OldLineStart;
			var idxNew = this.NewLineStart;

			foreach (var line in Patch.GetLines())
			{
				var firstChar = line.Length == 0 ? 'X' : line[0];
				if (firstChar != '+' && firstChar != '-')
				{
					// No line affected
					idxOld++;
					idxNew++;
					continue;
				}
				
				if (firstChar == '-')
				{
					this.lineNumbersDeleted.Add(idxOld++);
				}
				else if (firstChar == '+')
				{
					this.lineNumbersAdded.Add(idxNew++);
				}
				else
				{
					// No addition or deletion
					idxOld++;
					idxNew++;
				}
			}

			return this;
		}

		/// <summary>
		/// Returns an <see cref="IEnumerable{Hunk}"/> containing all hunks
		/// for the given <see cref="PatchEntryChanges"/>.
		/// </summary>
		/// <param name="pec"></param>
		/// <returns></returns>
		public static IEnumerable<Hunk> HunksForPatch(PatchEntryChanges pec, DirectoryInfo pairSourceDirectory, DirectoryInfo pairTargetDirectory)
		{
			var parts = HunkSplitRegex.Split(pec.Patch);

			foreach (var part in parts.Skip(1).Partition(5))
			{
				// The item 0 is just the diff-header.
				// The structure is 4 variables followed by text (the hunk).

				yield return new Hunk(part[4].TrimStart('\n'))
				{
					OldLineStart = UInt32.Parse(part[0]),
					OldNumberOfLines = UInt32.Parse(part[1]),
					NewLineStart = UInt32.Parse(part[2]),
					NewNumberOfLines = UInt32.Parse(part[3]),
					SourceFilePath = Path.Combine(pairSourceDirectory.FullName, pec.OldPath),
					TargetFilePath = Path.Combine(pairTargetDirectory.FullName, pec.Path)
				}.ComputeLinesAddedAndDeleted(); // Important to call having set the props
			}
		}
	}
}
