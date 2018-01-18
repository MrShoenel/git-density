﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util.Extensions
{
	public static class StringExtensions
	{
		/// <summary>
		/// Returns true if this <see cref="String"/> is empty or only consists of whitespace.
		/// </summary>
		/// <param name="string"></param>
		/// <returns></returns>
		public static Boolean IsEmptyOrWhiteSpace(this string @string)
		{
			return @string.Length == 0 || @string.Trim().Length == 0;
		}

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

		/// <summary>
		/// Returns true, if a <see cref="String"/> represents a valid Http or Https URL.
		/// </summary>
		/// <param name="string"></param>
		/// <returns>True, iff this <see cref="String"/> represents a valid HTTP(s) URL.</returns>
		public static bool IsHttpUrl(this String @string)
		{
			return Uri.TryCreate(@string, UriKind.Absolute, out Uri temp) &&
				(temp.Scheme == Uri.UriSchemeHttp || temp.Scheme == Uri.UriSchemeHttps);
		}

		/// <summary>
		/// Opens a <see cref="Repository"/> from a <see cref="String"/> that represents
		/// an absolute path to folder that is a valid git-directory or that represents a
		/// valid HTTP(S) URL to clone a git-repository from. If the string is a URL, the
		/// repository will be cloned to a temporary directory first.
		/// </summary>
		/// <param name="string">A full path to a local <see cref="Repository"/> or a URL
		/// that can be used to clone the repository to local facilities.</param>
		/// <param name="tempDirectory">If non-null, will be used as temporary path.
		/// Otherwise, <see cref="Path.GetTempPath()"/> will be used.</param>
		/// <returns>A <see cref="Repository"/> that wraps a local git-repository.</returns>
		public static Repository OpenRepository(this String @string, String tempDirectory = null)
		{
			if (@string.IsHttpUrl())
			{
				// We have to clone the repo to a temporary directory first.
				var tempPath = Path.Combine(tempDirectory ?? Path.GetTempPath(), Path.GetRandomFileName());
				Directory.CreateDirectory(tempPath);

				return new Repository(Repository.Clone(@string, tempPath));
			}
			else
			{
				if (!Directory.Exists(@string))
				{
					throw new FileNotFoundException("The directory does not exist.", @string);
				}

				return new Repository(@string);
			}
		}

		/// <summary>
		/// Simple method that prefixes any uppercase letter with an underscore (except
		/// for the first letter).
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static string ToSimpleUnderscoreCase(this string str)
		{
			return string.Concat(str.Select((x, i) => i > 0 && Char.IsUpper(x) ? $"_{x}" : $"{x}"));
		}
	}
}