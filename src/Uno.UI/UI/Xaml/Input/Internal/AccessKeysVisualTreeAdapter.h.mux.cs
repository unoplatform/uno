// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\VisualTreeAdapter.h, tag winui3/release/1.4.3, commit 685d2bf

#if __SKIA__
#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Input;

/// <summary>
/// Abstracts visual tree access for the AccessKeys system.
/// </summary>
internal sealed class AKVisualTreeFinder
{
	private VisualTree? _visualTree;

	internal AKVisualTreeFinder()
	{
	}

	internal void SetVisualTree(VisualTree? tree)
	{
		_visualTree = tree;
	}

	/// <summary>
	/// Gets the children of an element for AccessKey tree walking.
	/// Handles special cases for MenuFlyoutSubItem, TextBlock, RichTextBlock, etc.
	/// </summary>
	internal IReadOnlyList<DependencyObject>? GetChildren(DependencyObject element)
	{
		if (element is MenuFlyoutSubItem subItem)
		{
			return subItem.Items.Cast<DependencyObject>().ToList();
		}
		else if (element is TextBlock textBlock)
		{
			return textBlock.Inlines.Cast<DependencyObject>().ToList();
		}
		else if (element is RichTextBlock richTextBlock)
		{
			return richTextBlock.Blocks.Cast<DependencyObject>().ToList();
		}
		else if (element is UIElement uiElement)
		{
			var count = VisualTreeHelper.GetChildrenCount(uiElement);
			if (count == 0)
			{
				return null;
			}

			var children = new List<DependencyObject>(count);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(uiElement, i);
				if (child is not null)
				{
					children.Add(child);
				}
			}

			return children;
		}
		else if (element is Paragraph paragraph)
		{
			return paragraph.Inlines.Cast<DependencyObject>().ToList();
		}
		else if (element is Span span)
		{
			return span.Inlines.Cast<DependencyObject>().ToList();
		}

		return null;
	}

	internal DependencyObject? GetParent(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return VisualTreeHelper.GetParent(uiElement);
		}
		else if (element is TextElement textElement)
		{
			return textElement.GetContainingFrameworkElement();
		}

		return null;
	}

	internal static bool IsScope(DependencyObject element)
	{
		return AccessKeys.IsAccessKeyScope(element);
	}

	internal DependencyObject? GetScopeOwner(DependencyObject element)
	{
		if (element is MenuFlyoutPresenter menuFlyoutPresenter)
		{
			// MenuFlyout and MenuFlyoutPresenter are special, because logically the MenuFlyout is the parent of
			// MenuFlyoutPresenter but the MenuFlyoutPresenter is a popup, so is not actually a descendant of MenuFlyout.
			// To handle this, we consider all MenuFlyoutPresenters to have their MenuFlyout as their scope owner.
			return GetParentFromMenuFlyoutPresenter(menuFlyoutPresenter);
		}
		else if (element is UIElement)
		{
			return element.GetValue(UIElement.AccessKeyScopeOwnerProperty) as DependencyObject;
		}
		else if (element is TextElement)
		{
			return element.GetValue(TextElement.AccessKeyScopeOwnerProperty) as DependencyObject;
		}

		return null;
	}

	internal void GetAllVisibleRoots(List<DependencyObject> roots)
	{
		if (_visualTree is null)
		{
			return;
		}

		// In WinUI, this returns up to 3 roots: PublicRootVisual, PopupRoot, FullWindowMediaRoot.
		// We replicate that for the Uno visual tree structure.
		if (_visualTree.PublicRootVisual is { } publicRoot)
		{
			roots.Add(publicRoot);
		}

		if (_visualTree.PopupRoot is { } popupRoot)
		{
			roots.Add(popupRoot);
		}

		if (_visualTree.FullWindowMediaRoot is { } fullWindowMediaRoot)
		{
			roots.Add(fullWindowMediaRoot);
		}
	}

	private static DependencyObject? GetParentFromMenuFlyoutPresenter(MenuFlyoutPresenter presenter)
	{
		// Walk up to find the owning MenuFlyout.
		// The MenuFlyoutPresenter's logical parent is the Popup, and the Popup's associated flyout is the MenuFlyout.
		var parent = presenter.GetParentInternal(false /* publicParentsOnly */);
		if (parent is Popup popup)
		{
			// The associated flyout of the popup is the MenuFlyout
			var flyout = popup.AssociatedFlyout;
			if (flyout is not null)
			{
				return flyout;
			}

			// If no associated flyout, try to find parent MenuFlyoutSubItem
			// TODO Uno: FxCallbacks::FlyoutPresenter_GetParentMenuFlyoutSubItem not available
			// This fallback handles the common cases. Nested sub-items may need additional work.
		}

		return null;
	}
}
#endif
