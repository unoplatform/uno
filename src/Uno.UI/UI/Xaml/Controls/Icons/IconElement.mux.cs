// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.cpp

#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls;

partial class IconElement
{
	// TODO Uno: NOT PORTED - CIconElement::Create and CSymbolIcon::Create, the type converters building a SymbolIcon
	// from a symbol name. Uno converts in the XAML generator and in BindingPropertyHelper.FastStringToIconElement.

	//-------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Measures the first (and only) child.
	//
	//-------------------------------------------------------------------------
	protected override Size MeasureOverride(Size availableSize)
	{
		var pChild = GetFirstChildNoAddRef();
		if (pChild is not null)
		{
			// Ensure that our inherited Foreground property is up to date if it hasn't
			// been set locally.
			// TODO Uno specific: Uno propagates inherited properties eagerly, there is no text formatting to pull.

			// Measure the child
			pChild.Measure(availableSize);
			pChild.EnsureLayoutStorage();
			return pChild.DesiredSize;
		}

		return default;
	}

	//-------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Arranges the first (and only) child.
	//
	//-------------------------------------------------------------------------
	protected override Size ArrangeOverride(Size finalSize)
	{
		var pChild = GetFirstChildNoAddRef();
		if (pChild is not null)
		{
			var arrangeRect = new Rect(0, 0, finalSize.Width, finalSize.Height);
			pChild.Arrange(arrangeRect);
		}

		return finalSize;
	}

	private protected void AddChildElement(UIElement pChildElement)
	{
		if (GetFirstChildNoAddRef() is null)
		{
			var backgroundBrush = new SolidColorBrush()
			{
				Color = Color.FromArgb(0, 0, 0, 0) /* Transparent */
			};

			var grid = new Grid()
			{
				Background = backgroundBrush
			};

			VisualTreeHelper.AddChild(this, grid);
		}

		// We should have a child by this point.
		((Grid)GetFirstChildNoAddRef()).Children.Add(pChildElement);
	}

	private protected void RemoveChildElement()
	{
		// If we don't have a child, then there's no element for us to remove.
		if (GetFirstChildNoAddRef() is Grid grid && GetChildElementNoRef() is { } child)
		{
			grid.Children.Remove(child);
		}
	}

	private protected UIElement? GetChildElementNoRef()
	{
		UIElement? pResult = null;

		if (GetFirstChildNoAddRef() is Grid pGrid && pGrid.Children.Count > 0)
		{
			pResult = pGrid.Children[0];
		}

		return pResult;
	}

	//-------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Fetches the value of a specified property owned by this IconElement,
	//      and sets it as the value of a property owned by our first (and only)
	//      child.
	//
	//-------------------------------------------------------------------------
	private protected void UpdateLinkedChildProperty(DependencyProperty parentProperty, DependencyProperty childProperty)
	{
		// Our child elements should all be under a root grid.
		if (GetChildElementNoRef() is { } pChild)
		{
			// Set the value on the child's property
			pChild.SetValue(childProperty, GetValue(parentProperty));
		}
	}
}
