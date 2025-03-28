using System;
using System.Collections;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.MUX.Helpers;

// Uno specific: Most of these methods are now taking DependencyObject parameter instead of FrameworkElement
// to handle native elements like NativeScrollContentPresenter.

internal static class TreeHelper
{
	public static FrameworkElement GetVisualChildByName(DependencyObject parent, string name)
	{
		FrameworkElement child = default;

		var count = VisualTreeHelper.GetChildrenCount(parent);

		for (var i = 0; i < count && child == default; i++)
		{
			var current = VisualTreeHelper.GetChild(parent, i);
			var currentFe = current as FrameworkElement;
			child = currentFe?.Name == name
				? currentFe
				: GetVisualChildByName(current, name);
		}

		return child;
	}

	public static void GetVisualChildrenByType<T>(DependencyObject parent, ref List<T> children) where T : UIElement
	{
		var count = VisualTreeHelper.GetChildrenCount(parent);

		for (var i = 0; i < count; i++)
		{
			var current = VisualTreeHelper.GetChild(parent, i);

			if (current is T child)
			{
				children.Add(child);
			}
			else
			{
				GetVisualChildrenByType(current, ref children);
			}
		}
	}

	public static T GetVisualChildByType<T>(DependencyObject parent) where T : UIElement
	{
		T child = default;

		var count = VisualTreeHelper.GetChildrenCount(parent);

		for (var i = 0; i < count && child == default; i++)
		{
			var current = VisualTreeHelper.GetChild(parent, i);

			child = current is T c
				? c
				: GetVisualChildByType<T>(current);
		}

		return child;
	}


	internal static FrameworkElement GetVisualChildByNameFromOpenPopups(string name, DependencyObject element)
	{
		var popups = GetOpenPopups(element);

		foreach (var popup in popups)
		{
			var popupChild = popup.Child as FrameworkElement;
			if (popupChild.Name == name)
			{
				return popupChild;
			}
			else
			{
				var child = GetVisualChildByName(popupChild, name);
				if (child != null)
				{
					return child;
				}
			}
		}

		return null;
	}

	internal static T GetVisualChildByTypeFromOpenPopups<T>(DependencyObject element)
		where T : FrameworkElement
	{
		var popups = GetOpenPopups(element);

		foreach (var popup in popups)
		{
			var popupChild = (FrameworkElement)popup.Child;

			var result = popupChild as T;
			if (result != null)
			{
				return result;
			}
			result = GetVisualChildByType<T>(popupChild);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}

	internal static IEnumerable<Popup> GetOpenPopups(DependencyObject element)
	{
#if WINAPPSDK
		return VisualTreeHelper.GetOpenPopups(Window.Current);
#else
		return VisualTreeHelper.GetOpenPopupsForXamlRoot(GetXamlRoot(element));
#endif

	}

#if !WINAPPSDK
	internal static XamlRoot GetXamlRoot(DependencyObject obj)
	{
		XamlRoot xamlRoot = default;
		if (obj is UIElement e)
		{
			xamlRoot = e.XamlRoot;
		}
		else if (obj is TextElement te)
		{
			xamlRoot = te.XamlRoot;
		}
		else if (obj is Windows.UI.Xaml.Controls.Primitives.FlyoutBase fb)
		{
			xamlRoot = fb.XamlRoot;
		}
		else
		{
			throw new InvalidOperationException("TreeHelper::GetXamlRoot: Can't find XamlRoot for element");
		}
		return xamlRoot;
	}
#endif
}
