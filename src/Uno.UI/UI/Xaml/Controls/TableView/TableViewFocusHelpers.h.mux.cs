// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewFocusHelpers.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class TableViewDetails
{
	// One step up that crosses popup boundaries. A popup's content (a ComboBox dropdown, a flyout)
	// is not a visual descendant of the element that owns it, so a plain VisualTreeHelper walk from
	// a dropdown item never reaches the editor that opened it.
	internal static DependencyObject? GetPopupAwareParent(
		DependencyObject element,
		IReadOnlyList<Popup>? openPopups)
	{
		if (element is SelectorItem item)
		{
			if (ItemsControl.ItemsControlFromItemContainer(item) is { } itemsControl)
			{
				return itemsControl;
			}
		}

		if (element is FrameworkElement frameworkElement)
		{
			if (frameworkElement.Parent is Popup framePopup)
			{
				return framePopup;
			}
		}

		if (openPopups is not null)
		{
			foreach (var popup in openPopups)
			{
				if (popup is not null && popup.Child == element)
				{
					return popup;
				}
			}
		}

		var parent = VisualTreeHelper.GetParent(element);
		if (parent is null)
		{
			if (element is Popup elementPopup)
			{
				parent = elementPopup.PlacementTarget;
			}
		}
		return parent;
	}

	// True when `candidate` is `ancestor` or sits underneath it, including inside popups the
	// ancestor's content owns. The plain visual walk runs first so the common case stays cheap.
	internal static bool IsWithinElementPopupAware(
		object? candidate,
		DependencyObject? ancestor,
		XamlRoot? xamlRoot)
	{
		if (ancestor is null)
		{
			return false;
		}

		var start = candidate as DependencyObject;
		for (var visualCurrent = start; visualCurrent is not null; visualCurrent = VisualTreeHelper.GetParent(visualCurrent))
		{
			if (visualCurrent == ancestor)
			{
				return true;
			}
		}

		IReadOnlyList<Popup>? openPopups = null;
		if (xamlRoot is not null)
		{
			try
			{
				openPopups = VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot);
			}
			catch (Exception)
			{
				// Best-effort: item containers and popup parents are still crossed without it.
			}
		}

		// Bounded: a popup's PlacementTarget could in principle lead back into its own content.
		var current = start;
		for (int depth = 0; current is not null && depth < 1024; ++depth)
		{
			if (current == ancestor)
			{
				return true;
			}
			current = GetPopupAwareParent(current, openPopups);
		}
		return false;
	}

	// Closes any dropdown open inside `root`. An editor removed with its dropdown open (Enter both
	// opens a closed ComboBox and commits the edit) leaves the dropdown to restore focus to an
	// element that is no longer in the tree, and focus falls back to an arbitrary element.
	internal static void CloseOpenDropDowns(DependencyObject? root, int depth = 0)
	{
		if (root is null || depth > 32)
		{
			return;
		}

		if (root is ComboBox comboBox)
		{
			if (comboBox.IsDropDownOpen)
			{
				comboBox.IsDropDownOpen = false;
			}
			return;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; ++i)
		{
			CloseOpenDropDowns(VisualTreeHelper.GetChild(root, i), depth + 1);
		}
	}
}
