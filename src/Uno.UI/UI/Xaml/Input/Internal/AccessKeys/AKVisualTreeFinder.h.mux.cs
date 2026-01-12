// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\AccessKeys\inc\VisualTreeAdapter.h, tag winui3/release/1.5.3

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Input.AccessKeys;

/// <summary>
/// Adapts the visual tree for access key navigation.
/// Provides methods to get children, parent, and scope owner of elements.
/// </summary>
internal class AKVisualTreeFinder
{
	private VisualTree? _visualTree;

	internal AKVisualTreeFinder()
	{
	}

	internal AKVisualTreeFinder(VisualTree? visualTree)
	{
		_visualTree = visualTree;
	}

	internal void SetVisualTree(VisualTree? tree)
	{
		_visualTree = tree;
	}

	/// <summary>
	/// Gets the children of an element for access key tree walking.
	/// Handles special cases for text elements and menu items.
	/// </summary>
	internal IList<DependencyObject>? GetChildren(DependencyObject element)
	{
		// Handle special cases for various element types
		if (element is MenuFlyoutSubItem menuFlyoutSubItem)
		{
			// MenuFlyoutSubItem.Items is the collection of children
			return menuFlyoutSubItem.Items.Cast<DependencyObject>().ToList();
		}
		else if (element is TextBlock textBlock)
		{
			// TextBlock.Inlines contains the inline elements
			return ToList(textBlock.Inlines);
		}
		else if (element is RichTextBlock richTextBlock)
		{
			// RichTextBlock.Blocks contains the block elements
			return ToList(richTextBlock.Blocks);
		}
		else if (element is UIElement uiElement)
		{
			// For UIElements, get the visual children
			var children = new List<DependencyObject>();
			var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(uiElement);
			for (int i = 0; i < count; i++)
			{
				var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(uiElement, i);
				if (child is not null)
				{
					children.Add(child);
				}
			}
			return children;
		}
		else if (element is Paragraph paragraph)
		{
			// Paragraph.Inlines contains the inline elements
			return ToList(paragraph.Inlines);
		}
		else if (element is Span span)
		{
			// Span.Inlines contains the inline elements
			return ToList(span.Inlines);
		}

		return null;
	}

	/// <summary>
	/// Gets the parent of an element.
	/// </summary>
	internal DependencyObject? GetParent(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			return Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(uiElement);
		}
		else if (element is TextElement textElement)
		{
			// TextElement uses GetParent extension method
			return textElement.GetParent() as DependencyObject;
		}

		return null;
	}

	/// <summary>
	/// Gets the mentor (logical parent) of an element.
	/// </summary>
	internal DependencyObject? GetMentor(DependencyObject element)
	{
		// In Uno, we can use GetTemplatedParent or similar mechanisms
		if (element is FrameworkElement fe)
		{
			return fe.TemplatedParent as DependencyObject;
		}
		return null;
	}

	/// <summary>
	/// Returns true if the element is an access key scope boundary.
	/// </summary>
	internal static bool IsScope(DependencyObject element)
	{
		return Microsoft.UI.Xaml.Input.AccessKeys.IsAccessKeyScope(element);
	}

	/// <summary>
	/// Gets the access key scope owner for an element.
	/// </summary>
	internal DependencyObject? GetScopeOwner(DependencyObject element)
	{
		// Handle MenuFlyoutPresenter specially
		if (element is MenuFlyoutPresenter menuFlyoutPresenter)
		{
			// MenuFlyout and MenuFlyoutPresenter are special, because logically the MenuFlyout is the parent of MenuFlyoutPresenter
			// but the MenuFlyoutPresenter is a popup, so is not actually a descendant of MenuFlyout. To handle this, we consider
			// all MenuFlyoutPresenters to have their MenuFlyout as their scope owner. This makes the descendants of MenuFlyoutPresenter
			// part of the MenuFlyout's scope (and no other scope), just like we expect.
			return GetParentFromMenuFlyoutPresenter(menuFlyoutPresenter);
		}
		else if (element is UIElement uiElement)
		{
			return uiElement.AccessKeyScopeOwner;
		}
		else if (element is TextElement textElement)
		{
			return textElement.AccessKeyScopeOwner;
		}

		return null;
	}

	/// <summary>
	/// Gets all visible root elements for access key tree walking.
	/// </summary>
	internal void GetAllVisibleRootsNoRef(DependencyObject?[] roots)
	{
		// Initialize all roots to null
		for (int i = 0; i < roots.Length; i++)
		{
			roots[i] = null;
		}

		if (_visualTree is null)
		{
			return;
		}

		// Get the root visual from the visual tree
		var rootElement = _visualTree.RootElement;
		if (rootElement is not null)
		{
			roots[0] = rootElement;
		}

		// TODO UNO: Handle multiple visual roots (e.g., for popups, flyouts)
		// In WinUI, GetAllVisibleRootsNoRef returns the root visual, popup root, and full-window media root.
		// For now, we just return the main root element.
	}

	/// <summary>
	/// Gets the parent MenuFlyout from a MenuFlyoutPresenter.
	/// </summary>
	private DependencyObject? GetParentFromMenuFlyoutPresenter(MenuFlyoutPresenter presenter)
	{
		// The MenuFlyoutPresenter is typically placed inside a Popup.
		// We need to find the associated MenuFlyout.
		var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(presenter);
		if (parent is Popup popup)
		{
			// Check if this popup is associated with a flyout
			var associatedFlyout = popup.AssociatedFlyout;
			if (associatedFlyout is not null)
			{
				return associatedFlyout;
			}
		}

		// Fallback: try to find through logical parent or templated parent
		if (presenter.TemplatedParent is FlyoutBase flyout)
		{
			return flyout;
		}

		return null;
	}

	/// <summary>
	/// Helper to convert an InlineCollection to a list.
	/// </summary>
	private static IList<DependencyObject>? ToList(InlineCollection? collection)
	{
		if (collection is null)
		{
			return null;
		}

		var list = new List<DependencyObject>(collection.Count);
		foreach (var item in collection)
		{
			list.Add(item);
		}
		return list;
	}

	/// <summary>
	/// Helper to convert a BlockCollection to a list.
	/// </summary>
	private static IList<DependencyObject>? ToList(BlockCollection? collection)
	{
		if (collection is null)
		{
			return null;
		}

		var list = new List<DependencyObject>(collection.Count);
		foreach (var item in collection)
		{
			list.Add(item);
		}
		return list;
	}
}
