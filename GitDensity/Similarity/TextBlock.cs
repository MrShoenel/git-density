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
using GitDensity.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
	internal class TextBlock : IEquatable<TextBlock>
	{
		protected IDictionary<Int32, Line> linesWithLineNumber;

		private ReadOnlyDictionary<Int32, Line> linesWithLineNumberReadOnly;

		/// <summary>
		/// A read-only dictionary with lines and their line number.
		/// </summary>
		public IReadOnlyDictionary<Int32, Line> LinesWithNumber
		{
			get => this.linesWithLineNumberReadOnly;
		}

		/// <summary>
		/// The current <see cref="TextBlock"/> represented as a single string,
		/// where the lines have been joined by a new-line character (\n).
		/// </summary>
		public String WholeBlock
		{
			get => String.Join("\n", this.linesWithLineNumber.OrderBy(kv => kv.Key)
				.Select(kv => kv.Value.String));
		}

		/// <summary>
		/// Returns true if no lines were added.
		/// </summary>
		public Boolean IsEmpty => this.linesWithLineNumber.Count == 0;

		/// <summary>
		/// Initializes a new, empty <see cref="TextBlock"/>.
		/// </summary>
		public TextBlock()
		{
			this.linesWithLineNumber = new Dictionary<Int32, Line>();
			this.linesWithLineNumberReadOnly =
				new ReadOnlyDictionary<Int32, Line>(this.linesWithLineNumber);
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
						//this.AddLine(idx++, String.Empty);
						continue;
					}

					// Ordinary lines:
					this.AddLine(new Line(
						added ? LineType.Added : (untouched ? LineType.Untouched : LineType.Deleted),
						// remove first two chars (white or +/-)
						idx++, (line.Length > 1 ? line.Substring(2) : line).TrimEnd()));
					//this.AddLine(idx++, // remove first two chars (white or +/-)
					//	(line.Length > 1 ? line.Substring(2) : line).TrimEnd());
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
		public Line RemoveLine(Int32 lineNumber)
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
		public TextBlock RemoveLines(IEnumerable<Int32> lineNumbers)
		{
			var tb = new TextBlock();
			foreach (var lineNumber in lineNumbers)
			{
				tb.AddLine(this.RemoveLine(lineNumber));
			}
			return tb;
		}

		/// <summary>
		/// Returns whether this <see cref="TextBlock"/> has a line with the
		/// specified number.
		/// </summary>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		public Boolean HasLineNumber(Int32 lineNumber)
		{
			return this.linesWithLineNumber.ContainsKey(lineNumber);
		}

		public bool Equals(TextBlock other)
		{
			return other is TextBlock && this.WholeBlock == other.WholeBlock;
		}
	}
}
