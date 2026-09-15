// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\controls\LightDismissOverlay\inc\LightDismissOverlayHelper.h, tag winui3/release/1.7.1, commit 5f27a786ac96c

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal static class LightDismissOverlayHelper
{
	internal static bool IsOverlayVisibleForMode(LightDismissOverlayMode mode)
	{
		bool isOverlayVisible = false;

		if (mode == LightDismissOverlayMode.Auto)
		{
			isOverlayVisible = XboxUtility.IsOnXbox();
		}
		else
		{
			isOverlayVisible = (mode == LightDismissOverlayMode.On);
		}

		return isOverlayVisible;
	}

	// Controls that call this should implement IHasLightDismissOverlay, standing in for
	// the duck-typed get_LightDismissOverlayMode() the C++ template requires.
	internal static bool ResolveIsOverlayVisibleForControl<T>(T control)
		where T : class, IHasLightDismissOverlay
	{
		var overlayMode = control.LightDismissOverlayMode;

		return IsOverlayVisibleForMode(overlayMode);
	}
}
