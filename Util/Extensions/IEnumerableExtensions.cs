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
using LINQtoCSV;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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

		/// <summary>
		/// Writes the items as JSON file, each item will become an entry in the
		/// resulting array. The items in question should ideally use annotations,
		/// such as <see cref="JsonArrayAttribute"/> or <see cref="JsonIgnoreAttribute"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="outputFile"></param>
		public static void WriteJson<T>(this IEnumerable<T> items, String outputFile)
		{
			using (var writer = File.CreateText(outputFile))
			{
				writer.Write(JsonConvert.SerializeObject(items, Formatting.Indented));
			}
		}

        /// <summary>
        /// Write the items as CSV file, each item as a row. The items in question
        /// should ideally use annotations, such as <see cref="CsvColumnAttribute"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="writer"></param>
        public static void WriteCsv<T>(this IEnumerable<T> items, TextWriter writer)
		{
			using (writer)
			{
                // Now we extract some info and write it out later.
                var csvc = new CsvContext();
                var outd = new CsvFileDescription
                {
                    FirstLineHasColumnNames = true,
                    FileCultureInfo = Thread.CurrentThread.CurrentUICulture,
                    SeparatorChar = ',',
                    QuoteAllFields = true,
                    EnforceCsvColumnAttribute = true
                };

                csvc.Write(items, writer, outd);
            }
		}

		/// <summary>
		/// Writes the items as CSV file, each item as a row. The items in question
		/// should ideally use annotations, such as <see cref="CsvColumnAttribute"/>.
		/// </summary>
		/// <param name="items"></param>
		/// <param name="outputFile"></param>
		public static void WriteCsv<T>(this IEnumerable<T> items, String outputFile)
		{
			items.WriteCsv(File.CreateText(outputFile));
		}
	}
}