// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// Popup.h, Popup.cpp

using Uno.UI.Xaml;

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

#if UNO_HAS_ENHANCED_LIFECYCLE
	internal override void NotifyOfDataContextChange(DataContextChangedParams args)
	{
		var child = Child;

		base.NotifyOfDataContextChange(args);

		if (child is FrameworkElement childAsFE)
		{
			childAsFE.OnAncestorDataContextChanged(args);
		}
	}
#endif
}
