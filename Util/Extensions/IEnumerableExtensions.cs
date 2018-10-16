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

		/// <summary>
		/// Adds all items to this collection and returns it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="coll"></param>
		/// <param name="items"></param>
		/// <returns></returns>
		public static ICollection<T> AddAll<T>(this ICollection<T> coll, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				coll.Add(item);
			}
			return coll;
		}
	}
}