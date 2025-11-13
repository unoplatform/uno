// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TabView.cpp, commit 65718e2813

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Gets the data items held by the selected tabs that are being dragged.
/// </summary>
public sealed partial class TabViewTabTearOutWindowRequestedEventArgs : EventArgs
{
	internal TabViewTabTearOutWindowRequestedEventArgs(object item, UIElement tab)
	{
		Items = [item];
		Tabs = [tab];
	}

	internal TabViewTabTearOutWindowRequestedEventArgs()
	{
	}

	/// <summary>
	/// Gets the data items held by the selected tabs that are being dragged.
	/// </summary>
	public object[] Items { get; }

	/// <summary>
	/// Gets or sets the WindowId for the new window, if the application created one, that will host the torn-out tabs.
	/// </summary>
	public WindowId NewWindowId { get; set; }

	/// <summary>
	/// Gets the selected tabs that are being dragged.
	/// </summary>
	public UIElement[] Tabs { get; }
}
