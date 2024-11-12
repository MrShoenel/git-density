/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project GitDensity. All files in this project,
/// if not noted otherwise, are licensed under the GPLv3-license. You will
/// find a copy of this file in the project's root directory.
///
/// Note that the license changed from MIT to GPLv3. In general, the license
/// from the latest public commit applies.
///
/// ---------------------------------------------------------------------------------
///
using GitDensity.Density;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Util.Extensions;
using Util.Metrics;

namespace GitDensity.Similarity
{
	/// <summary>
	/// Used when obtaining a <see cref="TextBlock"/> from a <see cref="Hunk"/>
	/// to either obtain a block presenting the previous/old or the current/new
	/// block of text.
	/// </summary>
	public enum TextBlockType
	{
		Old,
		New
	}


	/// <summary>
	/// In <see cref="Util.Data.Entities.FileBlockType"/> we have previously used
	/// types for added, deleted, and modified. There were only these three types
	/// because we never saved information about blocks that were untouched (context).
	/// Here, we add the fourth kind so that a <see cref="TextBlock"/> can indentify
	/// itself as any of the four kinds.
	/// </summary>
	public enum TextBlockNature : uint
    {
		/// <summary>
		/// An untouched block of lines, usually shown as part of the Hunk (context).
		/// </summary>
		Context = 0u,

		/// <summary>
		/// A block that has one or more lines added, but no lines deleted.
		/// </summary>
		Added = 1u,

		/// <summary>
		/// A block that has one or more lines deleted, but no lines added.
		/// </summary>
		Deleted = 2u,

        /// <summary>
        /// A block that has one or more lines added, directly followed by one or
        /// more lines deleted (i.e., no other lines in between).
        /// </summary>
        Replaced = 3u
	}

	/// <summary>
	/// Represents a block of text where the block consists of lines and their
	/// line numbers.
	/// </summary>
	public class TextBlock : IEquatable<TextBlock>, ICloneable, IEnumerable<Line>
	{
		protected IDictionary<UInt32, Line> linesWithLineNumber;

		private ReadOnlyDictionary<UInt32, Line> linesWithLineNumberReadOnly;

		/// <summary>
		/// A read-only dictionary with lines and their line number.
		/// </summary>
		public IReadOnlyDictionary<UInt32, Line> LinesWithNumber
		{
			get => this.linesWithLineNumberReadOnly;
		}

		#region Count-properties
		public UInt32 LinesAdded => (UInt32)this.LinesWithNumber
			.Count(kv => kv.Value.Type == LineType.Added);

		public UInt32 LinesDeleted => (UInt32)this.LinesWithNumber
			.Count(kv => kv.Value.Type == LineType.Deleted);

		public UInt32 LinesUntouched => (UInt32)this.linesWithLineNumber
			.Count(kv => kv.Value.Type == LineType.Untouched);
		#endregion

		/// <summary>
		/// The current <see cref="TextBlock"/> represented as a single string,
		/// where the lines have been joined by a new-line character (\n).
		/// </summary>
		public String WholeBlock => String.Join("\n",
			this.linesWithLineNumber.OrderBy(kv => kv.Key).Select(kv => kv.Value.String));

		/// <summary>
		/// The current <see cref="TextBlock"/> represented as a single string,
		/// containing only those lines that have <see cref="Line.Type"/> of
		/// type <see cref="LineType.Added"/> or <see cref="LineType.Deleted"/>.
		/// The lines have been joined by a new-line character (\n).
		/// </summary>
		public String WholeBlockWithoutUntouched => String.Join("\n",
			this.linesWithLineNumber.Where(l => l.Value.Type != LineType.Untouched)
			.OrderBy(kv => kv.Key).Select(kv => kv.Value.String));

		/// <summary>
		/// Returns true if no lines were added.
		/// </summary>
		public Boolean IsEmpty => this.linesWithLineNumber.Count == 0;

		/// <summary>
		/// Initializes a new, empty <see cref="TextBlock"/>.
		/// </summary>
		public TextBlock()
		{
			this.linesWithLineNumber = new Dictionary<UInt32, Line>();
			this.linesWithLineNumberReadOnly =
				new ReadOnlyDictionary<UInt32, Line>(this.linesWithLineNumber);
		}

		/// <summary>
		/// Generate a <see cref="TextBlock"/> based on a <see cref="Hunk"/>. The
		/// <see cref="TextBlock"/> is either based on the previous version of the
		/// file in the <see cref="Hunk"/> or the current version.
		/// </summary>
		/// <param name="hunk"></param>
		/// <param name="textBlockType"></param>
		public TextBlock(Hunk hunk, TextBlockType textBlockType) : this()
		{
			var old = textBlockType == TextBlockType.Old;
			var idx = old ? hunk.OldLineStart : hunk.NewLineStart;

			foreach (var line in hunk.Patch.GetLines())
			{
				var firstChar = line.Length == 0 ? 'X' : line[0];
				var added = firstChar == '+';
				var untouched = firstChar != '-' && firstChar != '+';

				if (untouched
						|| (old && firstChar == '-')
						|| (!old && firstChar == '+'))
				{
					// Empty lines added/removed:
					if (line.Length == 1 && !untouched)
					{
						this.AddLine(new Line(
							added ? LineType.Added : LineType.Deleted, idx++, String.Empty));
						continue;
					}

					// Ordinary lines:
					this.AddLine(new Line(
						added ? LineType.Added : (untouched ? LineType.Untouched : LineType.Deleted),
						// remove first character (white or +/-)
						idx++, (line.Length > 1 ? line.Substring(1) : line).TrimEnd()));
				}
			}
		}

		/// <summary>
		/// Adds a line to this <see cref="TextBlock"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this block already
		/// contains a line with the given number.</exception>
		/// <param name="line"></param>
		public void AddLine(Line line)
		{
			if (this.linesWithLineNumber.ContainsKey(line.Number))
			{
				throw new InvalidOperationException($"This {nameof(TextBlock)} already contains a line with number {line.Number}.");
			}

			this.linesWithLineNumber[line.Number] = line;
		}

		public void AddLines(IEnumerable<Line> lines)
		{
			foreach (var line in lines)
			{
				this.AddLine(line);
			}
		}

		/// <summary>
		/// Removes a line from this <see cref="TextBlock"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this block does not
		/// contain the line with the specified line number.</exception>
		/// <param name="lineNumber"></param>
		/// <returns>The removed line as <see cref="Line"/>.</returns>
		public Line RemoveLine(UInt32 lineNumber)
		{
			if (!this.linesWithLineNumber.ContainsKey(lineNumber))
			{
				throw new InvalidOperationException($"This {nameof(TextBlock)} does not contain a line with number {lineNumber}.");
			}

			var line = this.linesWithLineNumber[lineNumber];
			this.linesWithLineNumber.Remove(lineNumber);
			return line;
		}

		/// <summary>
		/// Removes the given lines from this instance, adds them to a new
		/// instance and returns it.
		/// </summary>
		/// <param name="lineNumbers">A list of line numbers to remove.</param>
		/// <returns>A new <see cref="TextBlock"/> consisting of the removed
		/// lines.</returns>
		public TextBlock RemoveLines(IEnumerable<UInt32> lineNumbers)
		{
			var tb = new TextBlock();
			foreach (var lineNumber in lineNumbers)
			{
				tb.AddLine(this.RemoveLine(lineNumber));
			}
			return tb;
		}

		/// <summary>
		/// A wrapper around <see cref="RemoveEmptyLinesAndComments(out TextBlock)"/>
		/// that discards the <see cref="TextBlock"/> that only contains the removed
		/// empty lines and comments. <see cref="RemoveEmptyLinesAndComments(out TextBlock)"/>
		/// for a full documentation.
		/// </summary>
		/// <returns>This (<see cref="TextBlock"/>) for chaining.</returns>
		public TextBlock RemoveEmptyLinesAndComments()
		{
            return this.RemoveEmptyLinesAndComments(out _);
		}

		/// <summary>
		/// Creates a new <see cref="TextBlock"/> by removing all empty lines and
		/// lines containing single line comments (that begin with two or more
		/// forward slashes). Also removes multi-line comments. Note that this
		/// method keeps those lines that are partial comments. That means that
		/// code that precedes a comment or is located behind the closing characters
		/// of a multi-line comment is kept and that those lines remain in this
		/// <see cref="TextBlock"/> (but with the comment removed from the lines)
		/// and those lines are also part of the new, returned block, with the code
		/// being removed. This method maintains line-numbers.
		/// </summary>
		/// <param name="blockWithEmptiesAndComments">A new <see cref="TextBlock"/>
		/// containing those lines that are empty or contain (partial) comments.</param>
		/// <returns>This (<see cref="TextBlock"/>) for chaining.</returns>
		public TextBlock RemoveEmptyLinesAndComments(out TextBlock blockWithEmptiesAndComments)
		{
			blockWithEmptiesAndComments = new TextBlock();
			var copy = (TextBlock)this.Clone();

			var multiLineCommentStart = "/*";
			var multiLineCommentEnd = "*/";
			
			var isWithinMultilineComment = false;
			foreach (var line in this.linesWithLineNumber.Values)
			{
				var multiStartIdx = line.String.IndexOf(multiLineCommentStart);
				var multiEndIdx = line.String.IndexOf(multiLineCommentEnd);

				if (multiStartIdx >= 0)
				{
					isWithinMultilineComment = true;
					if (multiStartIdx == 0 || line.String.Substring(multiStartIdx).IsEmptyOrWhiteSpace())
					{
						// move entire line
						blockWithEmptiesAndComments.AddLine(copy.RemoveLine(line.Number));
					}
					else if (multiStartIdx > 0)
					{
						copy.RemoveLine(line.Number);
						copy.AddLine(new Line(
							line.Type, line.Number, line.String.Substring(0, multiStartIdx)));
						blockWithEmptiesAndComments.AddLine(new Line(
							line.Type, line.Number, line.String.Substring(multiStartIdx)));
					}

					continue;
				}
				else if (multiEndIdx >= 0)
				{
					isWithinMultilineComment = false;
					// Check if there is code behind the end of the comment
					if (line.String.Length > multiEndIdx + multiLineCommentEnd.Length)
					{
						copy.RemoveLine(line.Number);
						copy.AddLine(new Line(
							line.Type, line.Number, line.String.Substring(multiEndIdx + multiLineCommentEnd.Length)));
						blockWithEmptiesAndComments.AddLine(new Line(
							line.Type, line.Number, line.String.Substring(0, multiEndIdx + multiLineCommentEnd.Length)));
					}
					else
					{
						// No code afterwards, move entire line
						blockWithEmptiesAndComments.AddLine(copy.RemoveLine(line.Number));
					}

					continue;
				}


				if (isWithinMultilineComment)
				{
					// move entire line
					blockWithEmptiesAndComments.AddLine(copy.RemoveLine(line.Number));
				}
				else
				{
					if (line.String.IsEmptyOrWhiteSpace())
					{
						blockWithEmptiesAndComments.AddLine(copy.RemoveLine(line.Number));
					}

					var match = SimpleLoc.MatchSingleLineComment.Match(line.String);
					if (match.Success)
					{
						// Check whether there is code in front of the comment
						if (!match.Groups[1].Value.IsEmptyOrWhiteSpace())
						{
							var offset = match.Groups[1].Index + match.Groups[1].Length;
							var oldLine = copy.RemoveLine(line.Number);

							copy.AddLine(new Line(
								oldLine.Type, oldLine.Number, oldLine.String.Substring(0, offset)));
							blockWithEmptiesAndComments.AddLine(new Line(
								oldLine.Type, oldLine.Number, oldLine.String.Substring(offset)));
						}
						else
						{
							blockWithEmptiesAndComments.AddLine(copy.RemoveLine(line.Number));
						}
					}
				}
			}

			this.linesWithLineNumber.Clear();
			foreach (var line in copy.linesWithLineNumber.Values)
			{
				this.AddLine(line);
			}

			return this;
		}

		/// <summary>
		/// Returns whether this <see cref="TextBlock"/> has a line with the
		/// specified number.
		/// </summary>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		public Boolean HasLineNumber(UInt32 lineNumber)
		{
			return this.linesWithLineNumber.ContainsKey(lineNumber);
		}

		/// <summary>
		/// Returns true, if the other object is an instance of <see cref="TextBlock"/>
		/// and their blocks as string are equal (<see cref="WholeBlock"/>).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TextBlock other)
		{
			return other is TextBlock && this.WholeBlock == other.WholeBlock;
		}

		/// <summary>
		/// Returns an exact copy of this <see cref="TextBlock"/> that contains
		/// equal (cloned) <see cref="Line"/>s.
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			var tb = new TextBlock();
			tb.AddLines(this.LinesWithNumber.Select(kv => kv.Value.Clone() as Line));
			return tb;
		}

		/// <summary>
		/// Using <see cref="TextBlock"/>s, this method counts the net- and gross-amount
		/// of lines added and deleted across all <see cref="Hunk"/>s of a <see cref="PatchEntryChanges"/> entity.
		/// Uses <see cref="RemoveEmptyLinesAndComments()"/> to count the net-affected lines.
		/// </summary>
		/// <param name="pec"></param>
		/// <param name="linesAddedGross">Amount of lines added across all hunks.</param>
		/// <param name="linesDeletedGross">Amount of lines deleted across all hunks.</param>
		/// <param name="linesAddedWithoutEmptyOrComments">Amount of lines added net across
		/// all hunks. This count is equal to or lower than linesAddedGross, as empty lines
		/// and comments are removed.</param>
		/// <param name="linesDeletedWithoutEmptyOrComments">Amount of lines deleted net
		/// across all hunks. This count is equal to or lower than linesDeletedGross, as empty
		/// lines and comments are not considered in the deleted lines.</param>
		public static void CountLinesInPatch(PatchEntryChanges pec,
			out UInt32 linesAddedGross,
			out UInt32 linesDeletedGross,
			out UInt32 linesAddedWithoutEmptyOrComments,
			out UInt32 linesDeletedWithoutEmptyOrComments)
		{
			var hunks = Hunk.HunksForPatch(pec).ToList();

			var tbsAdded = hunks.Select(hunk => new TextBlock(hunk, TextBlockType.New)).ToList();
			var tbsDeleted = hunks.Select(hunk => new TextBlock(hunk, TextBlockType.Old)).ToList();

			linesAddedGross = (UInt32)tbsAdded.Sum(tb => tb.LinesAdded);
			linesDeletedGross = (UInt32)tbsDeleted.Sum(tb => tb.LinesDeleted);

			linesAddedWithoutEmptyOrComments = (UInt32)tbsAdded
				.Select(tb => tb.RemoveEmptyLinesAndComments())
				.Sum(tb => tb.LinesAdded);
			linesDeletedWithoutEmptyOrComments = (UInt32)tbsDeleted
				.Select(tb => tb.RemoveEmptyLinesAndComments())
				.Sum(tb => tb.LinesDeleted);

			hunks.ForEach(hunk => hunk.Clear());
		}

		/// <summary>
		/// Uses <see cref="CountLinesInPatch(PatchEntryChanges, out uint, out uint, out uint, out uint)"/> to aggregate the line-counts for a list of <see cref="PatchEntryChanges"/>.
		/// This is useful if multiple changes are to be considered. This is the case for
		/// e.g. counting all lines across all modified/renamed files. Please see the documentation
		/// for the referenced method.
		/// </summary>
		/// <param name="pecs"></param>
		/// <param name="linesAddedGross"></param>
		/// <param name="linesDeletedGross"></param>
		/// <param name="linesAddedWithoutEmptyOrComments"></param>
		/// <param name="linesDeletedWithoutEmptyOrComments"></param>
		public static void CountLinesInAllPatches(IEnumerable<PatchEntryChanges> pecs,
			out UInt32 linesAddedGross,
			out UInt32 linesDeletedGross,
			out UInt32 linesAddedWithoutEmptyOrComments,
			out UInt32 linesDeletedWithoutEmptyOrComments)
		{
			linesAddedGross = linesDeletedGross = linesAddedWithoutEmptyOrComments = linesDeletedWithoutEmptyOrComments = 0u;

			foreach (var pec in pecs)
			{
				TextBlock.CountLinesInPatch(
					pec, out uint add, out uint del, out uint addNoC, out uint delNoC);
				linesAddedGross += add;
				linesDeletedGross += del;
				linesAddedWithoutEmptyOrComments += addNoC;
				linesDeletedWithoutEmptyOrComments += delNoC;
			}
		}

		/// <summary>
		/// Each <see cref="TextBlock"/> is also an enumerable of <see cref="Line"/> objects.
		/// </summary>
		/// <returns></returns>
        public IEnumerator<Line> GetEnumerator()
        {
			return this.linesWithLineNumberReadOnly.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
			return this.GetEnumerator();
        }
    }
}
