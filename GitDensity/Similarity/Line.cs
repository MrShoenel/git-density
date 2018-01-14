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
	public class Line
	{
		public LineType Type { get; protected internal set; }

		public Int32 Number { get; protected internal set; }

		public String String { get; protected internal set; }

		public Line(LineType type, Int32 number, String @string)
		{
			this.Type = type;
			this.Number = number;
			this.String = @string;
		}
	}
}
