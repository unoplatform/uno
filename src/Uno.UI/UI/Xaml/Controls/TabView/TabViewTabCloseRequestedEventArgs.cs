// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabView.h, commit 65718e2813

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for a tab close event.
/// </summary>
public sealed partial class TabViewTabCloseRequestedEventArgs
{
	internal TabViewTabCloseRequestedEventArgs(object item, TabViewItem tab)
	{
		Item = item;
		Tab = tab;
	}

	/// <summary>
	/// Gets a value that represents the data context for the tab in which a close is being requested.
	/// </summary>
	public object Item { get; }

	/// <summary>
	/// Gets the tab in which a close is being requested.
	/// </summary>
	public TabViewItem Tab { get; }
}
