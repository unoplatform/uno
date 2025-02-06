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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Uno.Collections;
using Uno.Equality;

namespace Uno.Extensions
{
	/// <summary>
	/// Provides Extensions Methods for IList
	/// </summary>
	internal static class ListExtensions
	{
		/// <summary>
		/// Returns a readonly instance of the specified list.
		/// </summary>
		/// <typeparam name="T">The type of the IList</typeparam>
		/// <param name="items">The list</param>
		/// <returns>A readonly instance of the specified list.</returns>
		public static IList<T> AsReadOnly<T>(this IList<T> items)
		{
			return new ReadOnlyCollection<T>(items);
		}

		public static void AddRange(this IList destination, IEnumerable source)
		{
			foreach (var item in source)
			{
				destination.Add(item);
			}
		}


		/// <summary>
		/// Adds the items of the specified collection to the end of the ICollection, but only if they
		/// are not already present.
		/// </summary>
		/// <typeparam name="T">The type of the items.</typeparam>
		/// <param name="list">List in which to insert items.</param>
		/// <param name="items">The items to add.</param>
		public static void AddOrReplaceRange<T>(this IList<T> list, IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				var index = list.IndexOf(item);
				if (index < 0)
				{
					list.Add(item);
				}
				else
				{
					list[index] = item;
				}
			}
		}

		/// <summary>
		/// Adds the items of the specified collection to the end of the ICollection, 
		/// and updates existing items if they are found by the predicate
		/// </summary>
		/// <typeparam name="T">List type</typeparam>
		/// <param name="list"></param>
		/// <param name="items"></param>
		/// <param name="predicate"></param>
		public static void AddOrReplaceRange<T>(this IList<T> list, IEnumerable<T> items, Func<T, T, bool> predicate)
		{
			var enumeratedList = list.ToList();

			foreach (var item in items)
			{
				var index = enumeratedList.FindIndex(i => predicate(i, item));
				if (index < 0)
				{
					list.Add(item);
				}
				else
				{
					list[index] = item;
				}
			}
		}

		/// <summary>
		/// Remove all items after <paramref name="index"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		public static void RemoveAllAt<T>(this List<T> list, int index)
		{
			var count = list.Count - index;
			if (count > 0)
			{
				list.RemoveRange(index, count);
			}
		}

		/// <summary>
		/// Replace some items in a list using a selector
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="selector">Select items that have to be replaced</param>
		/// <param name="replacement">The replacement item</param>
		/// <returns>Number of items replaced</returns>
		public static int Replace<T>(this IList<T> list, Func<T, bool> selector, T replacement)
		{
			var replacedCount = 0;
			for (var i = 0; i < list.Count; i++)
			{
				if (selector(list[i]))
				{
					replacedCount++;
					list[i] = replacement;
				}
			}
			return replacedCount;
		}

		/// <summary>
		/// Creates an <see cref="IEnumerable{T}"/> by enumerating the given list both backwards and forwards, starting at the given index
		/// </summary>
		/// <param name="list">List to enumerate</param>
		/// <param name="startingAt">Index to start enumerating from</param>
		/// <returns></returns>
		public static IEnumerable<T> ToDivergentEnumerable<T>(this IList<T> list, int startingAt)
		{
			if ((list?.Count ?? 0) == 0)
			{
				yield break;
			}

			startingAt = Math.Max(0, Math.Min(list.Count - 1, startingAt));

			yield return list[startingAt];

			var forwardCount = list.Count - 1 - startingAt;
			var backwardCount = startingAt;

			for (int i = 1; i < Math.Max(forwardCount, backwardCount); i++)
			{
				if (i < forwardCount)
				{
					yield return list[startingAt + i];
				}
				if (i < backwardCount)
				{
					yield return list[startingAt - i];
				}
			}
		}

		/// <summary>
		/// Creates an <see cref="IEnumerable{T}"/> by enumerating the given list both backwards and forwards, starting at the given index
		/// </summary>
		/// <param name="list">List to enumerate</param>
		/// <param name="startingAt">Index to start enumerating from</param>
		/// <returns></returns>
		public static IEnumerable<T> ToDivergentEnumerable<T>(this IReadOnlyList<T> list, int startingAt)
		{
			if ((list?.Count ?? 0) == 0)
			{
				yield break;
			}

			startingAt = Math.Max(0, Math.Min(list.Count - 1, startingAt));

			yield return list[startingAt];

			var forwardCount = list.Count - 1 - startingAt;
			var backwardCount = startingAt;

			for (int i = 1; i < Math.Max(forwardCount, backwardCount); i++)
			{
				if (i < forwardCount)
				{
					yield return list[startingAt + i];
				}
				if (i < backwardCount)
				{
					yield return list[startingAt - i];
				}
			}
		}

		/// <summary>
		/// Gets whether a list contains a given index
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List to test</param>
		/// <param name="index">Index to search</param>
		/// <returns></returns>
		public static bool ContainsIndex<T>(this IList<T> list, int index)
		{
			return index >= 0 && index < list.Count;
		}

		/// <summary>
		/// Gets whether a list contains a given index
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List to test</param>
		/// <param name="index">Index to search</param>
		/// <returns></returns>
		public static bool ContainsIndex<T>(this IReadOnlyList<T> list, int index)
		{
			return index >= 0 && index < list.Count;
		}

		/// <summary>
		/// Returns the nearest item satisfying the given predicate in the list, starting at the given index
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List to search</param>
		/// <param name="predicate">A function to test each element for a condition</param>
		/// <param name="startingAt">Index to start searching from</param>
		/// <returns></returns>
		public static T FindNearestItem<T>(this IList<T> list, Func<T, bool> predicate, int startingAt = 0)
		{
			return list
				.ToDivergentEnumerable(startingAt)
				.FirstOrDefault(predicate);
		}

		/// <summary>
		/// Returns the nearest item satisfying the given predicate in the list, starting at the given index
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">List to search</param>
		/// <param name="predicate">A function to test each element for a condition</param>
		/// <param name="startingAt">Index to start searching from</param>
		/// <returns></returns>
		public static T FindNearestItem<T>(this IReadOnlyList<T> list, Func<T, bool> predicate, int startingAt = 0)
		{
			return list
				.ToDivergentEnumerable(startingAt)
				.FirstOrDefault(predicate);
		}

		/// <summary>
		/// Determines the index of a specific item in the <see cref="IList"/>.
		/// </summary>
		/// <param name="list">The source list to look into.</param>
		/// <param name="value">The object to locate in the <see cref="IList"/>.</param>
		/// <param name="comparer">The comparer to use to locate the <paramref name="value" />.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public static int IndexOf(this IList list, object value, IEqualityComparer comparer)
		{
			if (comparer == null)
			{
				return list.IndexOf(value);
			}

			for (var i = 0; i < list.Count; i++)
			{
				if (comparer.Equals(value, list[i]))
				{
					return i;
				}
			}

			return -1;
		}

		public static int IndexOf<T>(this IReadOnlyList<T> list, T value) =>
			list.IndexOf(value, EqualityComparer<T>.Default);

		public static int IndexOf<T>(this IReadOnlyList<T> list, T value, IEqualityComparer comparer)
		{
			if (comparer == null)
			{
				comparer = EqualityComparer<T>.Default;
			}

			for (var i = 0; i < list.Count; i++)
			{
				if (comparer.Equals(value, list[i]))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Determines whether two lists are key-equal, using the default <see cref="KeyEqualityComparer"/> for <see cref="IKeyEquatable"/>.
		/// </summary>
		/// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
		/// <param name="first">The first list.</param>
		/// <param name="second">The second list.</param>
		/// <returns>True if the two source lists are of equal length and their corresponding elements are key-equal; otherwise false.</returns>
		public static bool SequenceKeyEqual<T>(this IList<T> first, IList<T> second)
		{
			if (first.Count != second.Count)
			{
				return false;
			}

			for (int i = 0; i < first.Count; i++)
			{
				if (!KeyEqualityComparer.Default.Equals(first[i], second[i]))
				{
					return false;
				}
			}

			return true;
		}
	}
}
