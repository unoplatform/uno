// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.h, Popup.cpp

using DirectUI;
using Uno.UI.Xaml.Controls;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	internal void OnClosing(ref bool cancel)
	{
		// If this popup is associated with a flyout, then give it a chance to cancel closing.
		if (AssociatedFlyout is { } flyout)
		{
			cancel = flyout.Hide(canCancel: true);
		}
	}

	internal static FlyoutBase GetClosestFlyoutAncestor(UIElement pUIElement)
	{
		UIElement pNode = pUIElement;

		while (pNode is not null)
		{
			if (pNode is Popup popup)
			{
				if (popup.IsFlyout)
				{
					return popup.AssociatedFlyout;
				}
			}

			pNode = pNode.GetUIElementAdjustedParentInternal(false);
		}

		return null;
	}

	// Based on a combination of any special lightDismissFlags and the state of the core IsLightDismissEnabled property
	// determine whether the supplied reason should result in dismissing the popup
	private bool ShouldDismiss(DismissalTriggerFlags reason)
	{
		bool returnValue = false;

		// No special flags have been set. Defer to IsLightDismissEnabled
		if (m_dismissalTriggerFlags == DismissalTriggerFlags.None)
		{
			returnValue = IsLightDismissEnabled;
		}
		else
		{

			returnValue = ((reason & m_dismissalTriggerFlags) != 0);
		}

		// During a drag and drop operation, the current window might lose
		// focus. If that happens, we don't want to close the popup because it could
		// be the source or the target of the dnd operation.
		if (returnValue && reason == DismissalTriggerFlags.WindowDeactivated)
		{
			returnValue = !DXamlCore.IsWinRTDndOperationInProgress();
		}

		return returnValue;
	}

	bool IBackButtonListener.OnBackButtonPressed()
	{
		var handled = false;
		var shouldDismiss = ShouldDismiss(DismissalTriggerFlags.BackPress);
		if (shouldDismiss)
		{
			IsOpen = false;
			handled = true;
		}

		return handled;
	}
}
