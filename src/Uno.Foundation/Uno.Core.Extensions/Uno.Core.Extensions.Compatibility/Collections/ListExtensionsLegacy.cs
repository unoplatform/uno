using System;
using System.Collections.Generic;
using Uno.Collections;

namespace Uno.Core.Collections
{
	internal static class ListExtensionsLegacy
	{

		/// <summary>
		/// Adapts a list of type T into a list of type U
		/// </summary>
		/// <typeparam name="T">The type to adapt.</typeparam>
		/// <typeparam name="U">The target type.</typeparam>
		/// <param name="items">The list to adapt</param>
		/// <param name="from">The function used to adapt a U into a T.</param>
		/// <param name="to">The function used to adapt a T into a U.</param>
		/// <returns>A adapted list of the target type.</returns>
		public static IList<U> Adapt<T, U>(this IList<T> items, Func<U, T> from, Func<T, U> to)
		{
			return new ListAdapter<T, U>(items, from, to);
		}

		public static IList<U> Adapt<T, U>(this IList<T> items)
		{
			return Adapt(items, Funcs<U, T>.Convert, Funcs<T, U>.Convert);
		}
	}
}
