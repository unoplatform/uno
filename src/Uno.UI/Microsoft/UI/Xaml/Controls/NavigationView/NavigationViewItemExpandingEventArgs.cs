// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemExpandingEventArgs.cpp, commit 5ebf958

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides event data for the NavigationViewItem.Expanding event.
/// </summary>
public partial class NavigationViewItemExpandingEventArgs
{
	private readonly NavigationView? m_navigationView;
	private object? m_expandingItem = null;

	internal NavigationViewItemExpandingEventArgs(NavigationView? navigationView)
	{
		m_navigationView = navigationView;
	}

	/// <summary>
	/// Gets the object that is expanding after the NavigationViewItem.Expanding event.
	/// </summary>
	public object? ExpandingItem
	{
		get
		{
			if (m_expandingItem is object expandingItem)
			{
				return expandingItem;
			}

			if (m_navigationView is NavigationView nv)
			{
				m_expandingItem = nv.MenuItemFromContainer(ExpandingItemContainer);
				return m_expandingItem;
			}

			return null;
		}
	}

	/// <summary>
	/// Gets the container of the expanding item after a NavigationViewItem.Expanding event.
	/// </summary>
	public NavigationViewItemBase? ExpandingItemContainer { get; internal set; }
}
