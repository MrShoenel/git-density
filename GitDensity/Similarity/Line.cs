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
