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
using GitDensity.Density;
using System;
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
	/// Represents a block of text where the block consists of lines and their
	/// line numbers.
	/// </summary>
	internal class TextBlock : IEquatable<TextBlock>, ICloneable
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
			return this.RemoveEmptyLinesAndComments(out TextBlock dummy);
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
	}
}
