#nullable disable

// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
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
		/// Adds a new item with the default constructor
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <returns></returns>
		public static T AddNew<T>(this ICollection<T> items)
			where T : new()
		{
			T item = new T();

			items.Add(item);

			return item;
		}

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
		/// Adds an item into the collection and returns an IDisposable which will remove the item when disposed.
		/// </summary>
		/// <typeparam name="T">Type of the items in collection</typeparam>
		/// <param name="collection"></param>
		/// <param name="item">The item to add</param>
		/// <returns>An IDisposable which will remove the item when disposed</returns>
		public static IDisposable DisposableAdd<T>(this ICollection<T> collection, T item)
		{
			collection.Add(item);
			return Disposable.Create(() => collection.Remove(item));
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
		/// Replaces the items in a collection with a new set of items.
		/// </summary>
		/// <typeparam name="T">The type of items.</typeparam>
		/// <param name="collection">The collection who's content will be replaced.</param>
		/// <param name="items">The replacing items.</param>
		public static void ReplaceWith<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			collection.Clear();
			AddRange(collection, items);
		}

		public static IDisposable Subscribe<T>(this ICollection<T> collection, T item)
		{
			collection.Add(item);

			return Disposable.Create(() => collection.Remove(item));
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
		/// Adds an item to a collection if not already in it using an EqualityComparer.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="item">Item to add</param>
		/// <param name="comparer">Equality comparer to use to determine if item is already in the collection</param>
		/// <returns>True if the item was added, else false.</returns>
		public static bool AddDistinct<T>(this ICollection<T> collection, T item, IEqualityComparer<T> comparer)
		{
			return AddDistinct(collection, item, comparer.Equals);
		}

		/// <summary>
		/// Adds an item to a collection if not already in it using a predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="item">Item to add</param>
		/// <param name="predicate">Predicate to use to determine if item is already in the collection</param>
		/// <returns>True if the item was added, else false.</returns>
		public static bool AddDistinct<T>(this ICollection<T> collection, T item, Func<T, T, bool> predicate)
		{
			if (collection.None(collectionItem => predicate(collectionItem, item)))
			{
				collection.Add(item);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Adds to a collection the items of an <see cref="IEnumerable{T}"/> which are not already in collection.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="items">Items to add</param>
		/// <returns>Count of items added</returns>
		public static int AddRangeDistinct<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			return items.Count(collection.AddDistinct);
		}

		/// <summary>
		/// Adds to a collection the items of an <see cref="IEnumerable{T}"/> which are not already in collection using an equlaity comparer.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="items">Items to add</param>
		/// <param name="comparer">Equality comparer to use to determine if an item is already in the collection</param>
		/// <returns>Count of items added</returns>
		public static int AddRangeDistinct<T>(this ICollection<T> collection, IEnumerable<T> items, IEqualityComparer<T> comparer)
		{
			return AddRangeDistinct(collection, items, comparer.Equals);
		}

		/// <summary>
		/// Adds to a collection the items of an <see cref="IEnumerable{T}"/> which are not already in collection using an equlaity comparer.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection"></param>
		/// <param name="items">Items to add</param>
		/// <param name="comparer">Predicate to use to determine if an item is already in the collection</param>
		/// <returns>Count of items added</returns>
		public static int AddRangeDistinct<T>(this ICollection<T> collection, IEnumerable<T> items, Func<T, T, bool> predicate)
		{
			return items.Count(item => collection.AddDistinct(item, predicate));
		}

		public static T FindOrCreate<T>(this ICollection<T> collection, Func<T, bool> predicate, Func<T> factory)
			where T : class
		{
			var value = collection.FirstOrDefault(predicate);

			if (value == null)
			{
				value = factory();
				collection.Add(value);
			}

			return value;
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
		/// Projects the specified array to another array, using the item index.
		/// </summary>
		public static TResult[] SelectToArray<TSource, TResult>(this TSource[] source, Func<TSource, int, TResult> selector)
		{
			var output = new TResult[source.Length];

			for (int i = 0; i < output.Length; i++)
			{
				output[i] = selector(source[i], i);
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
		/// Create an array from a portion of another array, as a faster equivalent of .Skip().Take().ToArray().
		/// </summary>
		public static TSource[] ToRangeArray<TSource>(this TSource[] source, int skip, int take)
		{
			int maxLength = Math.Max(0, source.Length - skip);

			var output = new TSource[Math.Min(take, maxLength)];

			Array.ConstrainedCopy(
				source,
				Math.Min(skip, source.Length - 1),
				output,
				0,
				output.Length
			);

			return output;
		}

		/// <summary>
		/// Projects the specified collection to a <see cref="List{T}"/>.
		/// </summary>
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
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
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
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
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
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
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
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
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
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

		/// <summary>
		/// Create a <see cref="List{T}"/> from a portion of another <see cref="List{T}"/>, as a faster equivalent of .Skip().Take().ToList().
		/// </summary>
		/// <remarks>This method can be useful when the enumeation of the result requires less allocations.(see <see cref="List{T}.Enumerator"/>)</remarks>
		public static List<TSource> ToRangeList<TSource>(this List<TSource> source, int skip, int take)
		{
			int maxLength = Math.Max(0, source.Count - skip);
			int outputLength = Math.Min(take, maxLength);
			var startIndex = Math.Min(skip, source.Count - 1);
			var endIndex = startIndex + outputLength;

			var output = new List<TSource>(outputLength);

			for (int i = startIndex; i < endIndex; i++)
			{
				output.Add(source[i]);
			}

			return output;
		}
	}
}