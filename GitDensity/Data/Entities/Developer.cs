/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2017 Sebastian Hönel [sebastian.honel@lnu.se]
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
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitDensity.Data.Entities
{
	/// <summary>
	/// Represents a single developer. Note that git distinguishes between author
	/// and committer and has no such notion of a developer. However, in git density,
	/// we assume that author and committer are the same person.
	/// </summary>
	public class Developer : IEquatable<Developer>, IEqualityComparer<Developer>
	{
		public virtual String Name { get; set; }

		public virtual String Email { get; set; }

		public Developer(String name, String email)
		{
			if (String.IsNullOrEmpty(name) || String.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException(nameof(name));
			}
			if (String.IsNullOrEmpty(email) || String.IsNullOrWhiteSpace(email))
			{
				throw new ArgumentNullException(nameof(email));
			}

			this.Name = name;
			this.Email = email;
		}

		public bool Equals(Developer other)
		{
			return other is Developer &&
				(this.Name.ToLower() == other.Name.ToLowerInvariant() || this.Email == other.Email);
		}

		public static ISet<Developer> DevelopersOfRepository(Repository repo)
		{
			throw new NotImplementedException();
		}

		public bool Equals(Developer x, Developer y)
		{
			throw new NotImplementedException();
		}

		public int GetHashCode(Developer obj)
		{
			throw new NotImplementedException();
		}
	}
}
