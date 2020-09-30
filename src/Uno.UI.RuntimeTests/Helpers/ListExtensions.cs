using System.Collections.Generic;

namespace Uno.UI.RuntimeTests.Helpers
{
	internal static class ListExtensions
	{
		// Helpers for WinUI code compatibility
		internal static T GetAt<T>(this List<T> list, int index) => list[index];

		internal static T GetAt<T>(this IList<T> list, int index) => list[index];

		internal static T GetAt<T>(this IReadOnlyList<T> list, int index) => list[index];

		internal static void SetAt<T>(this IList<T> list, int index, T value) => list[index] = value;

		internal static void Append<T>(this IList<T> list, T item) => list.Add(item);
	}
}
