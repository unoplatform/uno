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
using System.Linq;

namespace Uno.Extensions.Specialized
{
	/// <summary>
	/// Provides Extensions Methods for IEnumerable.
	/// </summary>
	internal static partial class EnumerableExtensions
	{
		public static int Count(this IEnumerable enumerable)
		{
			if (enumerable == null)
			{
				return 0;
			}

			var collection = enumerable as ICollection;
			if (collection != null)
			{
				return collection.Count;
			}

			var enumerator = enumerable.GetEnumerator();
			var count = 0;
			while (enumerator.MoveNext())
			{
				count++;
			}

			return count;
		}

		public static bool Any(this IEnumerable items)
		{
			var collection = items as ICollection;

			if (collection != null)
			{
				return collection.Count > 0;
			}

			var enumerator = items.GetEnumerator();

			return enumerator.MoveNext();
		}

		public static object[] ToObjectArray(this IEnumerable items)
		{
			return items.Cast<object>().ToArray();
		}

		public static int IndexOf(this IEnumerable items, object item)
		{
			if (items == null)
			{
				return -1;
			}

			var list = items as IList;
			if (list != null)
			{
				return list.IndexOf(item);
			}

			var enumerator = items.GetEnumerator();
			for (var i = 0; ; i++)
			{
				if (!enumerator.MoveNext())
				{
					return -1;
				}

				if (enumerator.Current?.Equals(item) ?? item == null)
				{
					return i;
				}
			}
		}

		public static System.Object ElementAt(this IEnumerable items, int position)
		{
			if (items == null)
				return null;

			var itemsList = items as IList;
			if (itemsList != null)
			{
				return itemsList[position];
			}

			var enumerator = items.GetEnumerator();
			for (var i = 0; i <= position; i++)
			{
				enumerator.MoveNext();
			}

			return enumerator.Current;
		}

		public static System.Object ElementAtOrDefault(this IEnumerable items, int position)
		{
			if (items == null)
				return null;

			var itemsList = items as IList;
			if (itemsList != null)
			{
				if (itemsList.Count <= position || position < 0)
				{
					return null;
				}
				return itemsList[position];
			}

			var enumerator = items.GetEnumerator();
			for (var i = 0; i <= position; i++)
			{
				var next = enumerator.MoveNext();
				if (!next)
				{
					return null;
				}
			}

			return enumerator.Current;
		}

		/// <summary>
		/// Apply an action for every item of an enumerable
		/// </summary>
		/// <remarks>
		/// This method allows looping on every item of the source without enumerating it
		/// If enumeration is not a concern, you should avoid using this method if you're doing functional or declarative programming.
		/// </remarks>
		public static void ForEach(this IEnumerable enumerable, Action<object> action)
		{
			var list = enumerable as IList;

			if (list == null)
			{
				foreach (var item in enumerable)
				{
					action(item);
				}
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					action(list[i]);
				}
			}
		}

		public static bool None(this IEnumerable source)
		{
			var list = source as IList;

			if (list == null)
			{
				IEnumerator enumerator = null;
				try
				{
					enumerator = source.GetEnumerator();
					return !enumerator.MoveNext();
				}
				finally
				{
					if (enumerator is IDisposable)
					{
						(enumerator as IDisposable).Dispose();
					}
				}
			}
			else
			{
				return list.Count == 0;
			}
		}

		public static IEnumerable Where(this IEnumerable source, Func<object, bool> predicate)
		{
			foreach (var item in source)
			{
				if (predicate(item))
				{
					yield return item;
				}
			}
		}

		public static bool Contains(this IEnumerable source, object value)
		{
			var asList = source as IList;
			if (asList != null)
			{
				return asList.Contains(value);
			}

			foreach (var item in source)
			{
				if (item?.Equals(value) ?? (value == null))
				{
					return true;
				}
			}

			return false;
		}
	}
}
