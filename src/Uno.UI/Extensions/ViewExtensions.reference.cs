using System;
using System.Text;
using Windows.UI.Xaml;
using Uno.Collections;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		internal static MaterializableList<UIElement> GetChildren(this UIElement group) => throw new NotImplementedException();

		internal static FrameworkElement GetTopLevelParent(this UIElement view) => throw new NotImplementedException();

		internal static T FindFirstChild<T>(this FrameworkElement root, bool includeCurrent = true) where T : FrameworkElement
		{
			throw new NotImplementedException();
		}

		internal static T FindFirstChild<T>(this FrameworkElement root, Func<T, bool> selector, bool includeCurrent = true) where T : FrameworkElement
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Displays the visual tree in the vicinity of <paramref name="element"/> for diagnostic purposes.
		/// </summary>
		/// <param name="element">The view to display tree for.</param>
		/// <param name="fromHeight">How many levels above <paramref name="element"/> should be included in the displayed subtree.</param>
		/// <returns>A formatted string representing the visual tree around <paramref name="element"/>.</returns>
		internal static string ShowLocalVisualTree(this UIElement element, int fromHeight = 1000)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Displays all the visual descendants of <paramref name="element"/> for diagnostic purposes.
		/// </summary>
		internal static string ShowDescendants(this UIElement element, StringBuilder sb = null, string spacing = "", UIElement viewOfInterest = null)
		{
			throw new NotImplementedException();
		}
	}
}
