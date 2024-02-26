// ******************************************************************
// Copyright � 2015-2018 Uno Platform Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Collections;
using Uno.Disposables;

namespace Uno.Extensions
{
	/// <summary>
	/// Provides Extensions Methods for ICollection.
	/// </summary>
	internal static class CollectionExtensions
	{
		/// <summary>
		/// Adds the items of the specified collection to the end of the ICollection.
		/// </summary>
		/// <typeparam name="T">The type of the items.</typeparam>
		/// <param name="collection">Collection in which to insert items.</param>
		/// <param name="items">The items to add.</param>
		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			items.ForEach(collection.Add);
		}

		/// <summary>
		/// Removes items in a collection that are identified with a predicate.
		/// </summary>
		/// <typeparam name="T">the type of the items</typeparam>
		/// <param name="collection">Collection in which to remove items.</param>
		/// <param name="predicate">The predicate used to identify if a item is to be removed or not.</param>
		/// <returns>Count of removed items</returns>
		public static int Remove<T>(this ICollection<T> collection, Func<T, bool> predicate)
		{
			return collection
				.Where(predicate)
				.ToArray()
				.Where(collection.Remove)
				.Count();
		}

		/// <summary>
		/// Adds an item to a collection if not already in it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="item">Item to add</param>
		/// <returns>True if the item was added, else false.</returns>
		public static bool AddDistinct<T>(this ICollection<T> collection, T item)
		{
			if (collection.Contains(item))
			{
				return false;
			}
			else
			{
				collection.Add(item);
				return true;
			}
		}

		/// <summary>
		/// Projects the specified array to another array.
		/// </summary>
		public static TResult[] SelectToArray<TSource, TResult>(this TSource[] source, Func<TSource, TResult> selector)
		{
			var output = new TResult[source.Length];

			for (int i = 0; i < output.Length; i++)
			{
				output[i] = selector(source[i]);
			}

			return output;
		}

		/// <summary>
		/// Projects the specified collection to an array.
		/// </summary>
		public static TResult[] SelectToArray<TSource, TResult>(this ICollection<TSource> source, Func<TSource, TResult> selector)
		{
			var output = new TResult[source.Count];
			int index = 0;

			foreach (var item in source)
			{
				output[index] = selector(item);

				index++;
			}

			return output;
		}

		/// <summary>
		/// Projects the specified collection to a <see cref="List{T}"/>.
		/// </summary>
		/// <remarks>This method can be useful when the enumeration of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TResult> SelectToList<TSource, TResult>(this ICollection<TSource> source, Func<TSource, TResult> selector)
		{
			var output = new List<TResult>(source.Count);

			foreach (var item in source)
			{
				output.Add(selector(item));
			}

			return output;
		}

		/// <summary>
		/// Projects the specified collection to a <see cref="List{T}"/>.
		/// </summary>
		/// <remarks>This method can be useful when the enumeration of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TResult> SelectToList<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector)
		{
			var output = new List<TResult>(source.Count);

			foreach (var item in source)
			{
				output.Add(selector(item));
			}

			return output;
		}

		/// <summary>
		/// Projects a <see cref="List{T}"/>. to an other <see cref="List{T}"/>.
		/// </summary>
		/// <remarks>This method can be useful when the enumeration of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TResult> SelectToList<TSource, TResult>(this List<TSource> source, Func<TSource, TResult> selector)
		{
			var output = new List<TResult>(source.Count);

			foreach (var item in source)
			{
				output.Add(selector(item));
			}

			return output;
		}

		/// <summary>
		/// Projects the specified <see cref="List{T}"/> to an other <see cref="List{T}"/> with an index.
		/// </summary>
		/// <remarks>This method can be useful when the enumeration of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TResult> SelectToList<TSource, TResult>(this List<TSource> source, Func<TSource, int, TResult> selector)
		{
			var output = new List<TResult>(source.Count);
			int index = 0;

			foreach (var item in source)
			{
				output.Add(selector(item, index++));
			}

			return output;
		}

		/// <summary>
		/// Filters the specified <see cref="List{T}"/> using a predicate.
		/// </summary>
		/// <remarks>This method can be useful when the enumeration of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TSource> WhereToList<TSource>(this List<TSource> source, Func<TSource, bool> selector)
		{
			var output = new List<TSource>(source.Count);

			foreach (var item in source)
			{
				if (selector(item))
				{
					output.Add(item);
				}
			}

			return output;
		}
	}
}
