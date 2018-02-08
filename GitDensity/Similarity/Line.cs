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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Similarity
{
	/// <summary>
	/// Represents the type of line in a <see cref="TextBlock"/>. A <see cref="Hunk"/>
	/// usually includes some non-affected (<see cref="LineType.Untouched"/> lines, too.
	/// </summary>
	public enum LineType
	{
		Added,
		Deleted,
		Untouched
	}

	/// <summary>
	/// Represents a single line in a <see cref="TextBlock"/>.
	/// </summary>
	public class Line : ICloneable
	{
		public LineType Type { get; protected internal set; }

		public UInt32 Number { get; protected internal set; }

		public String String { get; protected internal set; }

		public Line(LineType type, UInt32 number, String @string)
		{
			this.Type = type;
			this.Number = number;
			this.String = @string;
		}

		public object Clone()
		{
			return new Line(this.Type, this.Number, this.String);
		}
	}
}
