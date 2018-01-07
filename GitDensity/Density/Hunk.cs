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
		public static readonly Regex HunkSplitRegex =
			new Regex(@"^@@\s+\-([0-9]+),([0-9]+)\s+\+([0-9]+),([0-9]+)\s+@@", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ECMAScript);

		public Int32 OldLineStart { get; protected internal set; }

		public Int32 OldNumberOfLines { get; protected internal set; }

		public Int32 NewLineStart { get; protected internal set; }

		public Int32 NewNumberOfLines { get; protected internal set; }

		public String TargetFilePath { get; protected internal set; }

		public Int32 NumberOfLinesAdded { get { return this.lineNumbersAdded.Count; } }

		public Int32 NumberOfLinesDeleted { get { return this.lineNumbersDeleted.Count; } }

		protected String patch { get; set; }

		protected IList<Int32> lineNumbersAdded;

		protected IList<Int32> lineNumbersDeleted;

		public Hunk(String patch)
		{
			this.patch = patch;

			this.lineNumbersAdded = new List<Int32>();
			this.lineNumbersDeleted = new List<Int32>();

			var i = 1;
			foreach (var line in patch.GetLines().Where(l => l.Length > 0))
			{
				var firstChar = line[0];
				if (firstChar == '+')
				{
					this.lineNumbersAdded.Add(i);
				}
				else if (firstChar == '-')
				{
					this.lineNumbersDeleted.Add(i);
				}

				i++;
			}
		}

		/// <summary>
		/// Returns an <see cref="IEnumerable{Hunk}"/> containing all hunks
		/// for the given <see cref="PatchEntryChanges"/>.
		/// </summary>
		/// <param name="pec"></param>
		/// <returns></returns>
		public static IEnumerable<Hunk> HunksForPatch(PatchEntryChanges pec)
		{
			var parts = HunkSplitRegex.Split(pec.Patch);

			foreach (var part in parts.Skip(1).Partition(5))
			{
				// The item 0 is just the diff-header.
				// The structure is 4 variables followed by text (the hunk).

				yield return new Hunk(part[4])
				{
					OldLineStart = Int32.Parse(part[0]),
					OldNumberOfLines = Int32.Parse(part[1]),
					NewLineStart = Int32.Parse(part[2]),
					NewNumberOfLines = Int32.Parse(part[3]),
					TargetFilePath = pec.Path
				};
			}
		}
	}
}
