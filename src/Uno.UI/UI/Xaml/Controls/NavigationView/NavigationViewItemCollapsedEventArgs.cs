// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemCollapsedEventArgs.cpp, commit 5ebf958

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the NavigationViewItem.Collapsed event.
/// </summary>
public partial class NavigationViewItemCollapsedEventArgs
{
	private readonly NavigationView? m_navigationView = null;
	private object? m_collapsedItem = null;

	internal NavigationViewItemCollapsedEventArgs(NavigationView? navigationView)
	{
		m_navigationView = navigationView;
	}

	/// <summary>
	/// Gets the object that has been collapsed after
	/// the NavigationViewItem.Collapsed event.
	/// </summary>
	public object? CollapsedItem
	{
		get
		{
			if (m_collapsedItem is object collapsedItem)
			{
				return collapsedItem;
			}

			if (m_navigationView is NavigationView nv)
			{
				m_collapsedItem = nv.MenuItemFromContainer(CollapsedItemContainer);
				return m_collapsedItem;
			}

			return null;
		}
	}

	/// <summary>
	/// Gets the container of the object that was collapsed
	/// in the NavigationViewItem.Collapsed event.
	/// </summary>
	public NavigationViewItemBase? CollapsedItemContainer { get; internal set; }
}
