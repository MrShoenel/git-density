using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Util
{
	public static class StringExtensions
	{
		/// <summary>
		/// Splits a <see cref="string"/> into lines and returns them as string.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="removeEmptyLines"></param>
		/// <returns></returns>
		public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
		{
			using (var sr = new StringReader(str))
			{
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					if (removeEmptyLines && String.IsNullOrWhiteSpace(line))
					{
						continue;
					}
					yield return line;
				}
			}
		}
	}
}
