using System.Collections.Generic;
using System.Linq;

namespace Util.Extensions
{
	public static class IEnumerableExtensions
	{
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