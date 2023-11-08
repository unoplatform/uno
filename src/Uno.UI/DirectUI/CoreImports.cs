#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;

namespace DirectUI;

internal static class CoreImports
{
	internal static DependencyObject? FocusManager_GetFirstFocusableElement(DependencyObject searchStart)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(searchStart);
		if (focusManager is null)
		{
			throw new InvalidOperationException("No focus manager found for element.");
		}
		return focusManager.GetFirstFocusableElement(searchStart);
	}
}
