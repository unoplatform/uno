// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LightDismissOverlayHelper.h, LightDismissOverlayHelper.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Provides helper methods for light-dismiss overlay visibility.
/// </summary>
internal static class LightDismissOverlayHelper
{
	/// <summary>
	/// Resolves whether the overlay should be visible for a control.
	/// </summary>
	/// <param name="appBar">The AppBar to check.</param>
	/// <param name="isOverlayVisible">Output: whether the overlay should be visible.</param>
	internal static void ResolveIsOverlayVisibleForControl(AppBar appBar, out bool isOverlayVisible)
	{
		var overlayMode = appBar.LightDismissOverlayMode;

		if (overlayMode == LightDismissOverlayMode.Auto)
		{
			// Auto mode - overlay is visible on Xbox
			isOverlayVisible = SharedHelpers.IsOnXbox();
		}
		else
		{
			// Explicit On or Off
			isOverlayVisible = overlayMode == LightDismissOverlayMode.On;
		}
	}
}
