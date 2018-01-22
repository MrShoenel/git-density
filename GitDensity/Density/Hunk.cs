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
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Util.Extensions;

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
			new Regex(@"^@@\s+\-([0-9]+),([0-9]+)\s+\+(?:([0-9]+),)?([0-9]+)\s+@@.*$", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ECMAScript);

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
		/// Sets all referenced objects to null and clears all lists.
		/// </summary>
		/// <returns>This <see cref="Hunk"/> for chaining.</returns>
		public Hunk Clear()
		{
			this.SourceFilePath = null;
			this.TargetFilePath = null;
			this.lineNumbersAdded.Clear();
			this.lineNumbersDeleted.Clear();
			this.Patch = null;
			return this;
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
		/// Returns true if this <see cref="Hunk"/> was created as a result of adding a
		/// new, empty file. This can happen by using <see cref="HunksForPatch(PatchEntryChanges, DirectoryInfo, DirectoryInfo)"/>.
		/// </summary>
		public Boolean RepresentsNewEmptyFile =>
			this.OldLineStart == 0u && this.OldNumberOfLines == 0u && this.NewLineStart == 0u && this.NewNumberOfLines == 0u && this.Patch == String.Empty;

		/// <summary>
		/// Returns an <see cref="IEnumerable{Hunk}"/> containing all hunks
		/// for the given <see cref="PatchEntryChanges"/>.
		/// </summary>
		/// <param name="pec"></param>
		/// <param name="pairSourceDirectory"></param>
		/// <param name="pairTargetDirectory"></param>
		/// <returns></returns>
		public static IEnumerable<Hunk> HunksForPatch(PatchEntryChanges pec, DirectoryInfo pairSourceDirectory, DirectoryInfo pairTargetDirectory)
		{
			if (pec.Mode == Mode.NonExecutableFile && pec.OldMode == Mode.Nonexistent && pec.LinesAdded == 0)
			{
				// This is an empty patch that is usually the result from adding a new, empty file.
				// We will only return one empty Hunk for this case.
				/// <see cref="RepresentsNewEmptyFile"/>
				yield return new Hunk(String.Empty)
				{
					OldLineStart = 0u,
					OldNumberOfLines = 0u,
					NewLineStart = 0u,
					NewNumberOfLines = 0u,
					SourceFilePath = Path.Combine(pairSourceDirectory.FullName, pec.OldPath),
					TargetFilePath = Path.Combine(pairTargetDirectory.FullName, pec.Path)
				};
				yield break;
			}

			var parts = HunkSplitRegex.Split(pec.Patch);

			foreach (var partArr in Hunk.GetParts(parts.Skip(1).ToArray()))
			{
				var isShortPart = partArr.Length < 5; // then the offset for the position in the new file is missing
				
				yield return new Hunk((isShortPart ? partArr[3] : partArr[4]).TrimStart('\n'))
				{
					OldLineStart = UInt32.Parse(partArr[0]),
					OldNumberOfLines = UInt32.Parse(partArr[1]),
					NewLineStart = isShortPart ? 0u : UInt32.Parse(partArr[2]),
					NewNumberOfLines = UInt32.Parse(isShortPart ? partArr[2] : partArr[3]),
					SourceFilePath = Path.Combine(pairSourceDirectory.FullName, pec.OldPath),
					TargetFilePath = Path.Combine(pairTargetDirectory.FullName, pec.Path)
				}.ComputeLinesAddedAndDeleted(); // Important to call having set the props
			}
		}

		/// <summary>
		/// The <see cref="HunkSplitRegex"/> sometimes only splits into 3 numerical parts, not 4.
		/// This is the case, if no new line number is included. This function takes a list of
		/// splits and returns 4- or 5-element long arrays.
		/// </summary>
		/// <param name="rawParts"></param>
		/// <returns></returns>
		public static IEnumerable<String[]> GetParts(String[] rawParts)
		{
			if (rawParts.Length <= 5)
			{
				yield return rawParts;
				yield break;
			}

			var list = new List<String>();

			for (int i = 0; i < rawParts.Length; i++)
			{
				if (list.Count < 3)
				{
					list.Add(rawParts[i]);
				}
				else
				{
					// check if we need to take the 4th part or not
					var numLeft = rawParts.Length - i;
					if (numLeft == 1)
					{
						list.Add(rawParts[i]); // Short part
					}
					else if (numLeft == 2) // Ordinary part
					{
						list.Add(rawParts[i++]);
						list.Add(rawParts[i]);
					}
					else if (numLeft > 2)
					{
						var aIsNumber = Int32.TryParse(rawParts[i + 0], out Int32 dummyA);
						var bIsNumber = Int32.TryParse(rawParts[i + 1], out Int32 dummyB);
						var cIsNumber = Int32.TryParse(rawParts[i + 2], out Int32 dummyC);

						if (!aIsNumber && bIsNumber && cIsNumber)
						{
							list.Add(rawParts[i]);
						}
						else if (aIsNumber && !bIsNumber && cIsNumber)
						{
							list.Add(rawParts[i++]);
							list.Add(rawParts[i]);
						}
						else
						{
							throw new Exception("Cannot parse parts.");
						}
					}

					yield return list.ToArray();
					list.Clear();
				}
			}
		}
	}
}
