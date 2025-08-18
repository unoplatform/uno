// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.h, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the TabDroppedOutside event.
/// </summary>
public sealed partial class TabViewTabDroppedOutsideEventArgs
{
	internal TabViewTabDroppedOutsideEventArgs(object item, TabViewItem tab)
	{
		Item = item;
		Tab = tab;
	}

	/// <summary>
	/// Gets the item that was dropped outside of the TabStrip.
	/// </summary>
	public object Item { get; }

	/// <summary>
	/// Gets the TabViewItem that was dropped outside of the TabStrip.
	/// </summary>
	public TabViewItem Tab { get; }
}
