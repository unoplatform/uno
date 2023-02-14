using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Microsoft.UI.Xaml.Controls;

#if XAMARIN_ANDROID
using _View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using _View = UIKit.UIView;
using UIKit;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
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
	}
}
