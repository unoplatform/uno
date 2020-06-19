using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using ViewGroup = Windows.UI.Xaml.UIElement;
using View = Windows.UI.Xaml.UIElement;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		public static T FindFirstChild<T>(this ViewGroup view, int? childLevelLimit = null, bool includeCurrent = true)
			where T : View
		{
			return view.FindFirstChild<T>(null, childLevelLimit, includeCurrent);
		}

		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="selector">Additional selector for the child</param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		public static T FindFirstChild<T>(this ViewGroup view, Func<T, bool> selector, int? childLevelLimit = null, bool includeCurrent = true)
			where T : View
		{
			Func<View, bool> childSelector;
			if (selector == null)
			{
				childSelector = child => child is T;
			}
			else
			{
				childSelector = child =>
				{
					var t = child as T;
					return t != null && selector(t);
				};
			}

			if (includeCurrent
				&& childSelector(view))
			{
				return view as T;
			}

			var maxDepth = childLevelLimit.HasValue
				? childLevelLimit.Value
				: Int32.MaxValue;

			return (T)view.EnumerateAllChildren(childSelector, maxDepth).FirstOrDefault();
		}
	}
}
