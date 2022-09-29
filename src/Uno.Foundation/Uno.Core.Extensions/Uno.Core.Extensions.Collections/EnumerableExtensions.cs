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
		/// Add an item who will be enumerated first before the real enumeration
		/// </summary>
		/// <remarks>
		/// Use .Concat() to inject at the end of the enumeration
		/// </remarks>
		public static IEnumerable<T> PrependEx<T>(this IEnumerable<T> items, T item)
		{
			yield return item;

			foreach (var x in items)
			{
				yield return x;
			}
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

		/// <summary>
		/// Check if all element in the enumeration are disctinct.
		/// </summary>
		public static bool AreDistinct<T>(this IEnumerable<T> items)
		{
			int unfilteredCount = 0, filteredCount = 0;

			// This code enumerate once
			items
				.TakeWhile(_ => unfilteredCount++ == filteredCount)
				.Distinct()
				.ForEach((T _) => filteredCount++);

			return unfilteredCount == filteredCount;
		}

		/// <summary>
		/// Check if all element in the enumeration are distinct.
		/// </summary>
		/// <typeparam name="T">Type of the items</typeparam>
		public static bool AreDistinct<T>(this IEnumerable<T> items, IEqualityComparer<T> comparer)
		{
			int unfilteredCount = 0, filteredCount = 0;

			// This code enumerates once
			items
				.TakeWhile(_ => unfilteredCount++ == filteredCount)
				.Distinct(comparer)
				.ForEach((T _) => filteredCount++);

			return unfilteredCount == filteredCount;
		}

		public static TResult SingleOrDefault<T, TResult>(this IEnumerable<T> items, Func<T, TResult> selector)
		{
			T result = items.SingleOrDefault();

			return Equals(result, default(T)) ? default(TResult) : selector(result);
		}

		public static T MinOrDefault<T>(this IEnumerable<T> items)
		{
			if (items.Any())
			{
				return items.Min();
			}
			else
			{
				return default(T);
			}
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

#if !XAMARIN
		/// <summary>
		/// Create an ObservableCollection for an enumeration.
		/// </summary>
		/// <remarks>
		/// The copy is done synchronously, before this method returns.
		/// </remarks>
		public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerableList)
		{
			if (enumerableList != null)
			{
#if WINDOWS_PHONE
				//create an emtpy observable collection object
				var observableCollection = new ObservableCollection<T>();

				//loop through all the records and add to observable collection object
				foreach (var item in enumerableList)
				{
					observableCollection.Add(item);
				}

				//return the populated observable collection
				return observableCollection;
#else
				return new ObservableCollection<T>(enumerableList);
#endif
			}
			return null;
		}
#endif

		/// <summary>
		/// Prevent null enumeration.
		/// When null, it's replaced with an empty enumeration of the same type.
		/// </summary>
		public static IEnumerable<T> Safe<T>(this IEnumerable<T> items)
		{
			return items ?? Enumerable.Empty<T>();
		}

		/// <summary>
		/// Calculate a Standard Deviation over an enumerator of values.
		/// </summary>
		public static double StdDev(this IEnumerable<double> values)
		{
			// ref: http://warrenseen.com/blog/2006/03/13/how-to-calculate-standard-deviation/ 
			double mean = 0.0;
			double sum = 0.0;
			double stdDev = 0.0;
			int n = 0;

			foreach (double val in values)
			{
				n++;
				double delta = val - mean;
				mean += delta / n;
				sum += delta * (val - mean);
			}

			if (1 < n)
				stdDev = Math.Sqrt(sum / (n - 1));

			return stdDev;
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
		/// Check if all items of an enumerable are equals, using an optional comparer
		/// </summary>
		public static bool AllEquals<T>(this IEnumerable<T> items, IEqualityComparer<T> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;

			T first = default(T);
			bool isFirst = true;
			foreach (var item in items)
			{
				if (isFirst)
				{
					first = item;
					isFirst = false;
					continue;
				}
				if (!comparer.Equals(first, item))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determines whether all elements of a sequence are true.
		/// </summary>
		public static bool AllTrue(this IEnumerable<bool> source)
		{
			return source?.All(b => b) ?? true;
		}

		/// <summary>
		/// Determines whether all elements of a sequence are true.
		/// </summary>
		/// <param name="defaultValue">Default value if source is null or empty</param>
		public static bool AllTrueOrDefault(this IEnumerable<bool> source, bool defaultValue)
		{
			if (source == null)
			{
				return defaultValue;
			}

			var hasValue = false;
			foreach (var b in source)
			{
				hasValue = true;
				if (!b)
				{
					return false;
				}
			}

			return hasValue ? true : defaultValue;
		}

		/// <summary>
		/// Determines whether any element of a sequence is true.
		/// </summary>
		public static bool AnyTrue(this IEnumerable<bool> source)
		{
			return source?.Any(b => b) ?? false;
		}

		/// <summary>
		/// Determines whether any element of a sequence satisfies a condition.
		/// </summary>
		/// <param name="defaultValue">Default value if source is null or empty</param>
		public static bool AnyTrueOrDefault(this IEnumerable<bool> source, bool defaultValue)
		{
			if (source == null)
			{
				return defaultValue;
			}

			var hasValue = false;
			foreach (var b in source)
			{
				hasValue = true;
				if (b)
				{
					return true;
				}
			}

			return hasValue ? false : defaultValue;
		}

		public static IEnumerable<T> GetPage<T>(this IEnumerable<T> source, int page, int perPage)
		{
			return source.Skip(page * perPage).Take(perPage);
		}

		public static bool SafeSequenceEqual<T>(this IEnumerable<T> obj, IEnumerable<T> other)
			where T : class
		{
			return (obj ?? Enumerable.Empty<T>()).SequenceEqual(other ?? Enumerable.Empty<T>());
		}

#if WINDOWS_PHONE && !WINPRT
#error WP7 NOT SUPPORTED ANYMORE - TO DELETE!
		/// <summary>
		/// Merges two sequences by using the specified predicate function.
		/// </summary>
		public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
		{
			if (first == null)
			{
				throw new ArgumentNullException("first");
			}
			if (second == null)
			{
				throw new ArgumentNullException("second");
			}
			if (resultSelector == null)
			{
				throw new ArgumentNullException("resultSelector");
			}

			return InnerZip(first, second, resultSelector);
		}

		private static IEnumerable<TResult> InnerZip<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
		{
			using (var firstEnumerator = first.GetEnumerator())
			{
				using (var secondEnumerator = second.GetEnumerator())
				{
					while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext())
					{
						yield return resultSelector(firstEnumerator.Current, secondEnumerator.Current);
					}
				}
			}
		}
#endif

		/// <summary>
		/// Count number of consecutive equals values
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static long ConsecutiveValueCount<T>(this IEnumerable<T> source)
		{
			var enumerator = source.GetEnumerator();
			long count = 0;

			// Skip if empty
			if (enumerator.MoveNext())
			{
				var originalValue = enumerator.Current;
				++count;

				while (enumerator.MoveNext())
				{
					if (originalValue.Equals(enumerator.Current))
					{
						++count;
					}
					else
					{
						return count;
					}
				}
			}

			return count;
		}

		public static TResult MaxOrDefault<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector, TResult defaultValue = default(TResult))
		{
			var src = source.Safe().ToArray();
			return src.Any()
				? src.Max(selector)
				: defaultValue;
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}"/> with unique keys from an <see cref="IEnumerable{TSource}"/> according to a specified key selector function, and an element selector function.
		/// </summary>
		/// <typeparam name="TSource">Type of the source enumerable</typeparam>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <typeparam name="TValue">Type of the value of the result dictionary</typeparam>
		/// <param name="source">Source enuemrable</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="valueSelector">A transform function to produce a result element value from each element.</param>
		/// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains values of type TElement selected from the input sequence.</returns>
		public static Dictionary<TKey, TValue> ToDictionaryDistinct<TSource, TKey, TValue>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TValue> valueSelector)
		{
			var dictionary = new Dictionary<TKey, TValue>();
			foreach (var item in source)
			{
				var key = keySelector(item);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, valueSelector(item));
				}
			}
			return dictionary;
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}"/> with unique keys from an <see cref="IEnumerable{TSource}"/> according to a specified key selector function, a comparer, and an element selector function.
		/// </summary>
		/// <typeparam name="TSource">Type of the source enumerable</typeparam>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <typeparam name="TValue">Type of the value of the result dictionary</typeparam>
		/// <param name="source">Source enuemrable</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="valueSelector">A transform function to produce a result element value from each element.</param>
		/// <param name="equalityComparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
		/// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains values of type TElement selected from the input sequence.</returns>
		public static Dictionary<TKey, TValue> ToDictionaryDistinct<TSource, TKey, TValue>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TValue> valueSelector,
			IEqualityComparer<TKey> equalityComparer)
		{
			var dictionary = new Dictionary<TKey, TValue>(equalityComparer);
			foreach (var item in source)
			{
				var key = keySelector(item);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, valueSelector(item));
				}
			}
			return dictionary;
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TSource}"/> with unique keys from an <see cref="IEnumerable{TSource}"/> according to a specified key selector function.
		/// </summary>
		/// <typeparam name="TSource">Type of the source enumerable and values of the result dictionary</typeparam>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <param name="source">Source enuemrable</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <returns>A <see cref="Dictionary{TKey,TSource}"/> that contains values of type TElement selected from the input sequence.</returns>
		public static Dictionary<TKey, TSource> ToDictionaryDistinct<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector)
		{
			var dictionary = new Dictionary<TKey, TSource>();
			foreach (var item in source)
			{
				var key = keySelector(item);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, item);
				}
			}
			return dictionary;
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TSource}"/> with unique keys from an <see cref="IEnumerable{TSource}"/> according to a specified key selector function, and a comparer.
		/// </summary>
		/// <typeparam name="TSource">Type of the source enumerable and values of the result dictionary</typeparam>
		/// <typeparam name="TKey">Type of the keys of the result dictionary</typeparam>
		/// <param name="source">Source enuemrable</param>
		/// <param name="keySelector">A function to extract a key from each element.</param>
		/// <param name="equalityComparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
		/// <returns>A <see cref="Dictionary{TKey,TSource}"/> that contains values of type TElement selected from the input sequence.</returns>
		public static Dictionary<TKey, TSource> ToDictionaryDistinct<TSource, TKey>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> equalityComparer)
		{
			var dictionary = new Dictionary<TKey, TSource>(equalityComparer);
			foreach (var item in source)
			{
				var key = keySelector(item);
				if (!dictionary.ContainsKey(key))
				{
					dictionary.Add(key, item);
				}
			}
			return dictionary;
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
		/// Correlates the elements of two sequences based on matching keys, including items without pair from both sides.
		/// </summary>
		/// <typeparam name="T1">The type of the elements of the first sequence.</typeparam>
		/// <typeparam name="T2">The type of the elements of the second sequence.</typeparam>
		/// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="left">The first sequence to join.</param>
		/// <param name="right">The second sequence to join.</param>
		/// <param name="leftKeySelector">A function to extract the join key from each element of the first sequence.</param>
		/// <param name="rightKeySelector">A function to extract the join key from each element of the second sequence.</param>
		/// <param name="projection">A function to create a result element from two elements.</param>
		/// <param name="defaultLeft">The default value to use to invoke <paramref name="projection"/> when there is no matching element in first sequence.</param>
		/// <param name="defaultRight">The default value to use to invoke <paramref name="projection"/> when there is no matching element in second sequence.</param>
		/// <param name="keyComparer">An <see cref="IEqualityComparer{T}"/> to hash and compare keys.</param>
		/// <returns>An <see cref="IEnumerable{T}"/> that has elements of type <typeparamref name="TResult"/> that are obtained by performing a full outer join on two sequences.</returns>
		public static IEnumerable<TResult> FullOuterJoin<T1, T2, TKey, TResult>(
			this IEnumerable<T1> left,
			IEnumerable<T2> right,
			Func<T1, TKey> leftKeySelector,
			Func<T2, TKey> rightKeySelector,
			Func<T1, T2, TResult> projection,
			T1 defaultLeft = default(T1),
			T2 defaultRight = default(T2),
			IEqualityComparer<TKey> keyComparer = null)
		{
			keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;

			var leftKeys = new HashSet<TKey>(keyComparer);
			var rightGroups = right.GroupBy(rightKeySelector, keyComparer).ToDictionary(keyComparer);

			foreach (var leftItem in left)
			{
				var leftKey = leftKeySelector(leftItem);
				leftKeys.Add(leftKey);

				IEnumerable<T2> rightGroup;
				if (rightGroups.TryGetValue(leftKey, out rightGroup))
				{
					foreach (var rightItem in rightGroup)
					{
						yield return projection(leftItem, rightItem);
					}
				}
				else
				{
					yield return projection(leftItem, defaultRight);
				}
			}

			foreach (var rightGroup in rightGroups)
			{
				if (!leftKeys.Contains(rightGroup.Key))
				{
					foreach (var rightItem in rightGroup.Value)
					{
						yield return projection(defaultLeft, rightItem);
					}
				}
			}
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
