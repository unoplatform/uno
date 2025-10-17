using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Extensions;

internal static class EnumerableExtensions
{
	public static IEnumerable<T> DistinctUntilChanged<T>(this IEnumerable<T> source)
	{
		using var enumerator = source.GetEnumerator();

		T previous;

		if (!enumerator.MoveNext()) yield break;
		yield return (previous = enumerator.Current);
		while (enumerator.MoveNext())
		{
			var current = enumerator.Current;
			if (!EqualityComparer<T>.Default.Equals(previous, current))
			{
				yield return current;
				previous = current;
			}
		}
	}
}
