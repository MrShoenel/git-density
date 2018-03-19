/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
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
using System.Text.RegularExpressions;
using Util.Extensions;

namespace Util.Metrics
{
	/// <summary>
	/// A very simple lines of code metric that keeps track of the gross amount
	/// and is able to remove simple single- and multi-line comments as well as
	/// empty lines containing only whitespace.
	/// Please note that matching comments with regular expressions is a very
	/// unsophisticated method, but the results of it should rather good for most
	/// of the code. Unlike a proper lexer, a regex cannot distinguish whether
	/// the detected comment is an actual comment or appears within a string
	/// literal.
	/// http://csse.usc.edu/TECHRPTS/2007/usc-csse-2007-737/usc-csse-2007-737.pdf
	/// 
	/// In the future, we may use a lexer or at least an improved tool for counting
	/// LOC, like sloccount: https://www.dwheeler.com/sloccount/
	/// </summary>
	public class SimpleLoc
	{
		#region Regexes
		/// <summary>
		/// Matches single lines that start with 2 or more forward slashes. Those
		/// lines may contain whitespace before the slashes.
		/// </summary>
		public static readonly Regex MatchSingleLineComment =
			new Regex(@"^(.*?)\/{2,}.*", RegexOptions.Compiled);

		/// <summary>
		/// Matches multi-line comments beginning with '/*' and ending with '*/'
		/// that contain any kind of character.
		/// </summary>
		public static readonly Regex MatchMultiLineComment =
			new Regex(@"\/\*(?:(.|[\r\n])*?)\*\/", RegexOptions.Compiled | RegexOptions.Multiline);
		#endregion

		/// <summary>
		/// The gross amount of lines of code. Counts every line (even empty lines)
		/// within the string. This measurement corresponds to the physical count.
		/// </summary>
		public UInt32 LocGross => this.lazyLocGross.Value;

		/// <summary>
		/// Lines of code without empty lines, single- and multi-line comments.
		/// This measure corresponds more closely to the logical lines of code.
		/// </summary>
		public UInt32 LocNoComments => this.lazyLocNoComments.Value;

		private Lazy<UInt32> lazyLocGross;

		private Lazy<UInt32> lazyLocNoComments;

		/// <summary>
		/// Construct <see cref="SimpleLoc"/> based on an enumerable of lines.
		/// </summary>
		/// <param name="lines"></param>
		public SimpleLoc(IEnumerable<String> lines)
		{
			this.lazyLocGross = new Lazy<uint>(() =>
			{
				return (UInt32)lines.Count();
			});

			this.lazyLocNoComments = new Lazy<uint>(() =>
			{
				return (UInt32)MatchMultiLineComment.Replace(String.Join("\n", lines), String.Empty)
					.Split('\n')
					.Where(line => // should we keep the line?
					{
						if (line.IsEmptyOrWhiteSpace())
						{
							return false;
						}

						var match = MatchSingleLineComment.Match(line);
						if (match.Success && match.Groups[1].Value.IsEmptyOrWhiteSpace())
						{
							return false;
						}

						return true; // keeps single-line comments with code in front of comment
					})
					.Count();
			});
		}
	}
}
