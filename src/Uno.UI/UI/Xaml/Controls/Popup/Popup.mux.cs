// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.h, Popup.cpp

using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	internal virtual void OnClosing(ref bool cancel)
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
}
