using System;
using System.Collections.Generic;
using Uno.Collections;

namespace Uno.Core.Collections
{
	public static class CollectionsExtensionsLegacy
	{
		public static ICollection<U> Adapt<T, U>(this ICollection<T> collection)
		{
			return Adapt(collection, Funcs<U, T>.Convert, Funcs<T, U>.Convert);
		}

		/// <summary>
		/// Adapts a collection of type T into a collection of type U
		/// </summary>
		/// <typeparam name="TSource">The type to adapt.</typeparam>
		/// <typeparam name="TTarget">The target type.</typeparam>
		/// <param name="collection">The collection to adapt</param>
		/// <param name="from">The function used to adapt a U into a T.</param>
		/// <param name="to">The function used to adapt a T into a U.</param>
		/// <returns>A adapted collection of the target type.</returns>
		public static ICollection<TTarget> Adapt<TSource, TTarget>(this ICollection<TSource> collection, Func<TTarget, TSource> from, Func<TSource, TTarget> to)
		{
			return new CollectionAdapter<TSource, TTarget>(collection, from, to);
		}
	}
}
