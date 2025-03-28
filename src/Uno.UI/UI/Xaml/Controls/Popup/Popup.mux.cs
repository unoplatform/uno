// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.h, Popup.cpp

namespace Windows.UI.Xaml.Controls.Primitives;

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
}
