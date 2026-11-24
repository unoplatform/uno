#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI
{
	internal static partial class ViewExtensions
	{
		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		internal static T? FindFirstChild<T>(this UIElement view, int? childLevelLimit = null, bool includeCurrent = true)
			where T : UIElement
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
		internal static T? FindFirstChild<T>(this UIElement view, Func<T, bool>? selector, int? childLevelLimit = null, bool includeCurrent = true)
			where T : UIElement
		{
			Func<UIElement, bool> childSelector;
			if (selector == null)
			{
				childSelector = child => child is T;
			}
			else
			{
				childSelector = child => child is T t && selector(t);
			}

			if (includeCurrent && childSelector(view))
			{
				return view as T;
			}

			return (T?)VisualTreeHelper.EnumerateRawDescendants(view, childLevelLimit).FirstOrDefault(childSelector);
		}
	}
}
