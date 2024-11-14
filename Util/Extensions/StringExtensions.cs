/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2020 Sebastian Hönel [sebastian.honel@lnu.se]
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
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
		/// Returns true if this <see cref="String"/> is either null or matches the criteria of
		/// the method <see cref="IsEmptyOrWhiteSpace(string)"/>.
		/// </summary>
		/// <param name="string"></param>
		/// <returns></returns>
		public static Boolean IsNullOrEmptyOrWhiteSpace(this string @string)
		{
			return @string == null || @string.IsEmptyOrWhiteSpace();
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
		/// Removes all invalid characters from a <see cref="String"/> so that it becomes
		/// a valid file- or directory-name. Spaces are replaced with hyphens.
		/// </summary>
		/// <param name="string"></param>
		/// <returns></returns>
		public static String ToValidFileName(this String @string)
		{
			@string = @string.Trim();
			// Also replace spaces with hyphens
			foreach (char c in Path.GetInvalidFileNameChars().Concat(' '.AsEnumerable()))
			{
				@string = @string.Replace(c, c == ' ' ? '-' : '_');
			}

			return @string;
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
		/// <param name="useRepoName">A custom name to use for the to-clone repository. If
		/// not given, then <see cref="Path.GetRandomFileName"/> will be used.</param>
		/// <param name="pullIfAlreadyExists">If true and the there is a repository in the
		/// designated target path, then the repository is opened and a pull is performed.</param>
		/// <exception cref="InvalidOperationException">Thrown if a custom repository-name
		/// was given, but the resulting target directory including that name would already
		/// exist.</exception>
		/// <returns>A <see cref="Repository"/> that wraps a local git-repository.</returns>
		public static Repository OpenRepository(this String @string, String tempDirectory = null, String useRepoName = null, Boolean pullIfAlreadyExists = false)
		{
			if (@string.IsHttpUrl())
			{
				useRepoName = useRepoName.IsNullOrEmptyOrWhiteSpace() ?
					null : useRepoName.ToValidFileName();

				// We have to clone the repo to a temporary directory first.
				var tempPath = Path.Combine(
					tempDirectory ?? Path.GetTempPath(), useRepoName ?? Path.GetRandomFileName());
				if (!useRepoName.IsNullOrEmptyOrWhiteSpace() && Directory.Exists(tempPath))
				{
					if (pullIfAlreadyExists)
					{
						var repository = new Repository(tempPath);
						Commands.Pull(repository, repository.Commits.First().Author, new PullOptions());
						return repository;
					}
					throw new InvalidOperationException(
						$"There is already a repository cloned in path {tempPath}");
				}
				Directory.CreateDirectory(tempPath);

				return new Repository(Repository.Clone(@string, tempPath));
			}
			else
			{
				@string = Path.GetFullPath(@string);
				// The string -supposedly- represents an accessible local path:
				if (!Directory.Exists(@string))
				{
					throw new FileNotFoundException(
						$"The directory of the repository does not exist: {@string}", @string);
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

		/// <summary>
		/// Hashes this <see cref="String"/> using <see cref="SHA256Managed"/> to a
		/// lowercase hex-representation (string of length 64).
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static String SHA256hex(this String value)
		{
			using (SHA256 hash = SHA256Managed.Create())
			{
				var result = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
				return String.Join(String.Empty, result.Select(b => b.ToString("x2")));
			}
		}


		/// <summary>
		/// Convert a string to its base64-representation using a given encoding.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="encoding">If not given, <see cref="Encoding.UTF8"/> is used
		/// to encode the string.</param>
		/// <returns></returns>
		public static String ToBase64(this string value, Encoding encoding = null)
		{
			var bytes = (encoding ?? Encoding.UTF8).GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Convert a base64-encoded string to its cleartext representation given an encoding.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="encoding">If not given, <see cref="Encoding.UTF8"/> is used
		/// to decode the string.</param>
        /// <returns></returns>
        public static String FromBase64(this String value, Encoding encoding = null)
		{
			return (encoding ?? Encoding.UTF8).GetString(Convert.FromBase64String(value));
		}


		/// <summary>
		/// Convert a string to its JSON-representation. This is useful for escaping strings
		/// when they should be used in contexts such as CSV files.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static String ToJSON(this string value)
		{
			return JsonConvert.SerializeObject(value, formatting: Formatting.None);
		}
	}
}
