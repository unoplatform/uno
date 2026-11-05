// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase_partial.cpp, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	// FlyoutBase_partial.cpp:1426
	private void SetPopupLightDismissBehavior()
	{
		_popup.IsLightDismissEnabled = IsLightDismissOverlayEnabled;

		if (m_shouldOverlayPassThroughAllInput)
		{
			var overlayInputPassThroughElement = OverlayInputPassThroughElement;
			if (overlayInputPassThroughElement is null)
			{
				var rootVisual = GetOverlayPassThroughRootVisual();
				if (rootVisual is not null)
				{
					OverlayInputPassThroughElement = rootVisual;
					m_ownsOverlayInputPassThroughElement = true;
				}
			}
		}
		else if (m_ownsOverlayInputPassThroughElement)
		{
			OverlayInputPassThroughElement = null;
		}
	}

	// FlyoutBase_partial.cpp:3938
	private void UpdateStateToShowMode(FlyoutShowMode showMode)
	{
		if (showMode == FlyoutShowMode.Auto)
		{
			showMode = FlyoutShowMode.Standard;
		}

		ShowMode = showMode;

		var oldShouldHideIfPointerMovesAway = m_shouldHideIfPointerMovesAway;

		switch (showMode)
		{
			case FlyoutShowMode.Standard:
				m_shouldTakeFocus = true;
				m_shouldHideIfPointerMovesAway = false;
				m_shouldOverlayPassThroughAllInput = false;
				break;
			case FlyoutShowMode.Transient:
				m_shouldTakeFocus = false;
				m_shouldHideIfPointerMovesAway = false;
				m_shouldOverlayPassThroughAllInput = true;
				break;
			case FlyoutShowMode.TransientWithDismissOnPointerMoveAway:
				m_shouldTakeFocus = false;
				m_shouldHideIfPointerMovesAway = true;
				m_shouldOverlayPassThroughAllInput = true;
				break;
			default:
				global::System.Diagnostics.Debug.Fail("Unsupported FlyoutShowMode");
				break;
		}

		if (_popup is { IsOpen: true })
		{
			SetPopupLightDismissBehavior();
			// Uno's popup bindings configure the overlay brush/visibility, and its panel reads
			// OverlayInputPassThroughElement on each hit test instead of ConfigurePopupOverlay().

			if (m_shouldHideIfPointerMovesAway && !oldShouldHideIfPointerMovesAway)
			{
				// TODO Uno: Native pointer-move-away tracking is not implemented.
				// IFC_RETURN(AddRootVisualPointerMovedHandler());
			}
			else if (!m_shouldHideIfPointerMovesAway && oldShouldHideIfPointerMovesAway)
			{
				// TODO Uno: Native pointer-move-away tracking is not implemented.
				// IFC_RETURN(RemoveRootVisualPointerMovedHandler());
			}
		}
	}
}
