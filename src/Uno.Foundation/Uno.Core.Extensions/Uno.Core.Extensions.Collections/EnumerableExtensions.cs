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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Collections;
using System.Collections.ObjectModel;

namespace Uno.Extensions
{
	/// <summary>
	/// Provides Extensions Methods for IEnumerable.
	/// </summary>
	internal static partial class EnumerableExtensions
	{
		//[Obsolete("Refactor to use .Do() instead. Will potentially enumerate the source more than once.")]
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<KeyValuePair<int, T>> action)
		{
			return ForEach(items, action);
		}

		//[Obsolete("Refactor to use .Do() instead. Will potentially enumerate the source more than once.")]
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<int, T> action)
		{
			if (items != null)
			{
				var i = 0;

				foreach (var item in items)
				{
					action(i++, item);
				}
			}

			return items;
		}

		//[Obsolete("Refactor your code to avoid this operator. Will potentially enumerate the source more than once.")]
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items)
		{
			if (items != null)
			{
				using (var enumerator = items.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
					}
				}
			}

			return items;
		}

		//[Obsolete("Refactor to use .Do() instead. Will potentially enumerate the source more than once.")]
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> action)
		{
			if (items != null)
			{
				foreach (T item in items)
				{
					action(item);
				}
			}

			return items;
		}

		public static bool None<T>(this IEnumerable<T> items, Func<T, bool> predicate)
		{
			return !items.Any(predicate);
		}

		public static bool None<T>(this IEnumerable<T> source)
		{
			var collectionOfT = source as ICollection<T>;
			if (collectionOfT != null)
			{
				return collectionOfT.Count == 0;
			}

			var collection = source as ICollection;
			if (collection != null)
			{
				return collection.Count == 0;
			}

			using (var enumerator = source.GetEnumerator())
			{
				return !enumerator.MoveNext();
			}
		}

		public static bool Empty<T>(this IEnumerable<T> items)
		{
			var collectionOfT = items as ICollection<T>;
			if (collectionOfT != null)
			{
				return collectionOfT.Count == 0;
			}

			var collection = items as ICollection;
			if (collection != null)
			{
				return collection.Count == 0;
			}

			using (var enumerator = items.GetEnumerator())
			{
				return !enumerator.MoveNext();
			}
		}

		/// <summary>
		/// Append an item at the end of an enumeration
		/// </summary>
		/// <remarks>
		/// Use .Prepend() to inject before the enumeration
		/// </remarks>
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> items, T item)
		{
			foreach (var x in items)
			{
				yield return x;
			}

			yield return item;
		}

		/// <summary>
		/// Exclude some items from an enumeration
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="items">Items to exclude</param>
		/// <returns></returns>
		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] items)
		{
			return Enumerable.Except(source, items);
		}

		/// <summary>
		/// Exclude some items from an enumeration using an equality comparer
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="equalityComparer">Equality comparer to use to compare items between enumerations</param>
		/// <param name="items">Items to exclude</param>
		/// <returns></returns>
		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEqualityComparer<T> equalityComparer, params T[] items)
		{
			return Enumerable.Except(source, items, equalityComparer);
		}

		public static int IndexOf<T>(this IEnumerable<T> items, T item)
		{
			return IndexOf(items, item, EqualityComparer<T>.Default);
		}

		public static int IndexOf<T>(this IEnumerable<T> items, T item, IEqualityComparer<T> comparer)
		{
			return IndexOf(items, item, comparer.Equals);
		}

		public static int IndexOf<T>(this IEnumerable<T> items, T item, Func<T, T, bool> predicate)
		{
			var index = 0;

			foreach (var instance in items)
			{
				if (predicate(item, instance))
				{
					return index;
				}

				++index;
			}

			return -1;
		}

		public static TResult SingleOrDefault<T, TResult>(this IEnumerable<T> items, Func<T, TResult> selector)
		{
			T result = items.SingleOrDefault();

			return Equals(result, default(T)) ? default(TResult) : selector(result);
		}

		public static T MaxOrDefault<T>(this IEnumerable<T> items)
		{
			if (items.Any())
			{
				return items.Max();
			}
			else
			{
				return default(T);
			}
		}

		/// <summary>
		/// Finds an item in the sequence for which a projected value is minimized.
		/// </summary>
		/// <typeparam name="TSource">Sequence type.</typeparam>
		/// <typeparam name="TComparable">Projected value type.</typeparam>
		/// <param name="source">The sequence of items.</param>
		/// <param name="selector">Function which projects the sequence into a comparable value.</param>
		/// <returns>A tuple containing the minimum item and its projected value. If multiple items have the same projected value, this will return the first.</returns>
		public static (TSource Item, TComparable Value) MinBy<TSource, TComparable>(this IEnumerable<TSource> source, Func<TSource, TComparable> selector)
		{
			var comparer = Comparer<TComparable>.Default;

			var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
			{
				throw new InvalidOperationException("Source must contain at least one element.");
			}

			var minItem = enumerator.Current;
			var min = selector(minItem);

			while (enumerator.MoveNext())
			{
				var item = enumerator.Current;
				var value = selector(item);
				if (comparer.Compare(value, min) < 0)
				{
					minItem = item;
					min = value;
				}
			}

			return (minItem, min);
		}

		/// <summary>
		/// Finds an item in the sequence for which a projected value is maximized.
		/// </summary>
		/// <typeparam name="TSource">Sequence type.</typeparam>
		/// <typeparam name="TComparable">Projected value type.</typeparam>
		/// <param name="source">The sequence of items.</param>
		/// <param name="selector">Function which projects the sequence into a comparable value.</param>
		/// <returns>A tuple containing the maximum item and its projected value. If multiple items have the same projected value, this will return the first.</returns>
		public static (TSource Item, TComparable Value) MaxBy<TSource, TComparable>(this IEnumerable<TSource> source, Func<TSource, TComparable> selector)
		{
			var comparer = Comparer<TComparable>.Default;

			var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
			{
				throw new InvalidOperationException("Source must contain at least one element.");
			}

			var maxItem = enumerator.Current;
			var max = selector(maxItem);

			while (enumerator.MoveNext())
			{
				var item = enumerator.Current;
				var value = selector(item);
				if (comparer.Compare(value, max) > 0)
				{
					maxItem = item;
					max = value;
				}
			}

			return (maxItem, max);
		}

		/// <summary>
		/// Takes "before" item and "after" item around the "start" item
		/// </summary>
		public static IEnumerable<T> Range<T>(this IEnumerable<T> collection, int start, int before, int after, bool fixedCount = true)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}

			if (start < 0 || before < 0 || after < 0)
			{
				throw new ArgumentException("Start, before and after must be greater than 0");
			}

			var index = start - before;
			index = index < 0 ? 0 : index;

			if (fixedCount)
			{
				// if the start item is the last we add the number of element we want after, before
				if (start == collection.Count() - 1) index -= after;
			}

			return collection.Skip(index).Take(before + after + 1);
		}

		/// <summary>
		/// Intercept enumerated elements. SEE REMARKS FOR USAGE!
		/// </summary>
		/// <remarks>
		/// This method is not doing the enumeration,
		/// only intercept it when an enumeration occurs.
		/// </remarks>
		public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var item in source)
			{
				action(item);
				yield return item;
			}
		}

		/// <summary>
		/// Remove null values while enumerating
		/// </summary>
		public static IEnumerable<T> Trim<T>(this IEnumerable<T> items)
			where T : class
		{
			return items.Where(item => item != null);
		}

		/// <summary>
		/// Remove null values while enumerating
		/// </summary>
		public static IEnumerable<T> Trim<T>(this IEnumerable<T?> items)
			where T : struct
		{
			return items
				.Where(item => item.HasValue)
				.Select(item => item.Value);
		}

		/// <summary>
		/// Prevent null enumeration.
		/// When null, it's replaced with an empty enumeration of the same type.
		/// </summary>
		public static IEnumerable<T> Safe<T>(this IEnumerable<T> items)
		{
			return items ?? Enumerable.Empty<T>();
		}

		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> enumerable, Func<T, IEnumerable<T>> predicate)
		{
			if (enumerable != null)
			{
				foreach (var e in enumerable)
				{
					yield return e;

					foreach (var t in predicate(e).Flatten(predicate))
					{
						yield return t;
					}
				}
			}
		}

		/// <summary>
		/// Enumerate the item first, followed by items of the predicate
		/// </summary>
		public static IEnumerable<T> Flatten<T>(this T item, Func<T, IEnumerable<T>> predicate)
		{
			if (item != null)
			{
				yield return item;

				foreach (var t in predicate(item).Flatten(predicate))
				{
					yield return t;
				}
			}
		}

		public static IEnumerable<T> Flatten<T>(this T item, Func<T, T> predicate)
		{
			if (item != null)
			{
				yield return item;

				foreach (var t in predicate(item).Flatten(predicate))
				{
					yield return t;
				}
			}
		}

		/// <summary>
		/// Determines whether all elements of a sequence are true.
		/// </summary>
		public static bool AllTrue(this IEnumerable<bool> source)
		{
			return source?.All(b => b) ?? true;
		}

		public static TResult MaxOrDefault<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue = default(TResult))
		{
			var src = source.Safe().ToArray();
			return src.Any()
				? src.Max(selector)
				: defaultValue;
		}

		/// <summary>
		/// Creates a Dictionary&lt;TKey,IEnumerable&lt;TSource&gt;&gt; from an IEnumerable&lt;IGrouping&lt;TSource&gt;&gt;;
		/// </summary>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <typeparam name="TValue">Type of the value of the result dictionary</typeparam>
		public static Dictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groups)
		{
			return groups.ToDictionary(g => g.Key, g => g.AsEnumerable());
		}

		/// <summary>
		/// Creates a Dictionary&lt;TKey,IEnumerable&lt;TSource&gt;&gt; from an IEnumerable&lt;IGrouping&lt;TSource&gt;&gt;;
		/// </summary>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <typeparam name="TValue">Type of the value of the result dictionary</typeparam>
		/// <param name="equalityComparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
		public static Dictionary<TKey, IEnumerable<TValue>> ToDictionary<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groups, IEqualityComparer<TKey> equalityComparer)
		{
			return groups.ToDictionary(g => g.Key, g => g.AsEnumerable(), equalityComparer);
		}

		/// <summary>
		/// A SelectMany that returns a non-generic IEnumerable.
		/// </summary>
		public static IEnumerable SelectManyUntyped<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable> selector)
		{
			foreach (var s in source)
			{
				foreach (var o in selector(s))
				{
					yield return o;
				}
			}
		}

#if !XAMARIN
		/// <summary>
		/// Skips the last <paramref name="count"/> items from an enumerable sequence.
		/// </summary>
		/// <typeparam name="T">Type of items</typeparam>
		/// <param name="source">The source enumerable</param>
		/// <param name="count">Count of items to ignore at the end of an enumerable sequence.</param>
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return
				count <= 0
					? source :
				(count == 1
					? Skip1(source)
					// count > 1
					: SkipN(source, count));

			IEnumerable<T> Skip1(IEnumerable<T> src)
			{
				using (var enumerator = src.GetEnumerator())
				{
					if (!enumerator.MoveNext())
					{
						yield break;
					}

					var buffer = enumerator.Current;

					while (enumerator.MoveNext())
					{
						yield return buffer;
						buffer = enumerator.Current;
					}
				}
			}

			IEnumerable<T> SkipN(IEnumerable<T> src, int n)
			{
				using (var enumerator = src.GetEnumerator())
				{
					var buffer = new Queue<T>(n);
					for (var i = 0; i < n; i++)
					{
						if (enumerator.MoveNext())
						{
							buffer.Enqueue(enumerator.Current);
						}
						else
						{
							yield break;
						}
					}

					while (enumerator.MoveNext())
					{
						yield return buffer.Dequeue();
						buffer.Enqueue(enumerator.Current);
					}
				}
			}
		}
#endif
	}
}
