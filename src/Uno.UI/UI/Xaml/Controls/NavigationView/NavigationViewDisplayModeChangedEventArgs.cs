// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewDisplayModeChangedEventArgs.cpp, commit 838a0cc

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the NavigationView.DisplayModeChanged event.
/// </summary>
public sealed partial class NavigationViewDisplayModeChangedEventArgs
{
	internal NavigationViewDisplayModeChangedEventArgs(NavigationViewDisplayMode displayMode) =>
		DisplayMode = displayMode;

	/// <summary>
	/// Gets the new display mode.
	/// </summary>
	public NavigationViewDisplayMode DisplayMode { get; }
}
