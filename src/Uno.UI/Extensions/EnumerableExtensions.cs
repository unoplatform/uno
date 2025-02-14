using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
using UIKit;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

namespace Uno.UI.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Projects the specified collection to an array.
		/// </summary>
		public static List<TResult> SelectToList<TResult>(this UIElementCollection source, Func<_View, TResult> selector)
		{
			var output = new List<TResult>(source.Count);

			foreach (var item in source)
			{
				output.Add(selector(item));
			}

			return output;
		}

		/// <summary>
		/// ToDictionary that doesn't throw on duplicated key. The first value is kept per key.
		/// </summary>
		public static Dictionary<TKey, TElement> ToDictionaryKeepFirst<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector
		) where TKey : notnull
		{
			var result = new Dictionary<TKey, TElement>();

			foreach (var item in source)
			{
				result.TryAdd(keySelector(item), elementSelector(item));
			}

			return result;
		}

		/// <summary>
		/// ToDictionary that doesn't throw on duplicated key. The last value is kept per key.
		/// </summary>
		public static Dictionary<TKey, TElement> ToDictionaryKeepLast<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector
		) where TKey : notnull
		{
			var result = new Dictionary<TKey, TElement>();

			foreach (var item in source)
			{
				result[keySelector(item)] = elementSelector(item);
			}

			return result;
		}
	}
}
