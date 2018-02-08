/// ---------------------------------------------------------------------------------
///
/// Copyright (c) 2018 Sebastian Hönel [sebastian.honel@lnu.se]
///
/// https://github.com/MrShoenel/git-density
///
/// This file is part of the project Util. All files in this project,
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
using System.Collections.Generic;
using System.Linq;

namespace Util.Extensions
{
	public static class IEnumerableExtensions
	{
		/// <summary>
		/// Transforms any struct or object into an <see cref="IEnumerable{T}"/> of it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="anyObject"></param>
		/// <returns></returns>
		public static IEnumerable<T> AsEnumerable<T>(this T anyObject)
		{
			yield return anyObject;
		}

		/// <summary>
		/// Partitions an <see cref="IEnumerable{T}"/> into chunks of same length.
		/// If not enough elements are available for the last chunk (i.e. th number
		/// of available elements is lower than the available amount of elements),
		/// the last chunk will contain fewer elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="partitionSize"></param>
		/// <returns></returns>
		public static IEnumerable<IList<T>> Partition<T>(this IEnumerable<T> items, int partitionSize)
		{
			var i = 0;
			var array = new T[partitionSize];
			foreach (var item in items)
			{
				array[i++] = item;

				if (i == partitionSize)
				{
					yield return array.ToList();
					i = 0;
				}
			}

			if (i > 0)
			{
				yield return array.Take(i).ToList();
			}
		}
	}
}