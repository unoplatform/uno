// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LightDismissOverlayHelper.h, tag winui3/release/2.5.1, commit ba3a8d59e

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal static class LightDismissOverlayHelper
{
	internal static bool IsOverlayVisibleForMode(LightDismissOverlayMode mode)
	{
		bool isOverlayVisible;

		if (mode == LightDismissOverlayMode.Auto)
		{
			// TODO Uno: XboxUtility.IsOnXbox(): Uno doesn't run on Xbox.
			isOverlayVisible = false;
		}
		else
		{
			isOverlayVisible = mode == LightDismissOverlayMode.On;
		}

		return isOverlayVisible;
	}
}
