using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Util.Metrics
{
	/// <summary>
	/// A very simple lines of code metric that keeps track of the gross amount
	/// and is able to remove simple single- and multi-line comments as well as
	/// empty lines containing only whitespace.
	/// </summary>
	public class SimpleLOC
	{
		#region Regexes
		/// <summary>
		/// Matches single lines that start with 2 or more forward slashes. Those
		/// lines may contain whitespace before the slashes.
		/// </summary>
		public static readonly Regex MatchSingleLineComment =
			new Regex(@"^\s*?\/{2,}.*", RegexOptions.Compiled);

		/// <summary>
		/// Matches multi-line comments beginning with '/*' and ending with '*/'
		/// that contain any kind of character.
		/// </summary>
		public static readonly Regex MatchMultiLineComment =
			new Regex(@"\/\*(?:(.|[\r\n])*?)\*\/", RegexOptions.Compiled | RegexOptions.Multiline);
		#endregion

		protected IEnumerable<String> lines;

		/// <summary>
		/// The gross amount of lines of code. Counts every line (even empty lines)
		/// within the string.
		/// </summary>
		public UInt32 LocGross => this.lazyLocGross.Value;

		/// <summary>
		/// Lines of code without empty lines, single- and multi-line comments.
		/// </summary>
		public UInt32 LocNoComments => this.lazyLocNoComments.Value;

		private Lazy<UInt32> lazyLocGross;

		private Lazy<UInt32> lazyLocNoComments;

		/// <summary>
		/// Construct <see cref="SimpleLOC"/> based on an enumerable of lines.
		/// </summary>
		/// <param name="lines"></param>
		public SimpleLOC(IEnumerable<String> lines)
		{
			this.lines = lines;

			this.lazyLocGross = new Lazy<uint>(() =>
			{
				return (UInt32)this.lines.Count();
			});

			this.lazyLocNoComments = new Lazy<uint>(() =>
			{
				return (UInt32)MatchMultiLineComment.Replace(String.Join("\n", this.lines), String.Empty)
					.Split('\n')
					.Where(line =>
						!(String.IsNullOrWhiteSpace(line.Trim()) || MatchSingleLineComment.IsMatch(line)))
					.Count();
			});
		}
	}
}
