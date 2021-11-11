using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Collections;

namespace Uno.Core.Collections
{
	internal static class EnumerableExtensionsLegacy
	{
		public static IEnumerable<Pair<object>> Pair(this IEnumerable xItems, IEnumerable yItems)
		{
			return Pair(xItems.Cast<object>(), yItems.Cast<object>());
		}

		public static IEnumerable<Pair<T>> Pair<T>(this IEnumerable<T> xItems, IEnumerable<T> yItems)
		{
			var xEnumerator = xItems.GetEnumerator();
			var yEnumerator = yItems.GetEnumerator();

			var xSuccess = xEnumerator.MoveNext();
			var ySuccess = yEnumerator.MoveNext();

			while (xSuccess && ySuccess)
			{
				yield return new Pair<T>(xEnumerator.Current, yEnumerator.Current);

				xSuccess = xEnumerator.MoveNext();
				ySuccess = yEnumerator.MoveNext();
			}

			if (xSuccess || ySuccess)
			{
				throw new ArgumentException("xItems && yItems not same length");
			}
		}

		public static IList<T> ToLazyList<T>(this IEnumerable<T> items)
		{
			return new LazyList<T>(items.ToList);
		}
	}
}
