// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\SplitMenuFlyoutItem.cpp, commit 5f9e85113

using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class SplitMenuFlyoutItem
{
	// MUX Reference: CSplitMenuFlyoutItem::EnterImpl in SplitMenuFlyoutItem.cpp
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);
		MenuFlyout.KeyboardAcceleratorFlyoutItemEnter(this, this, SplitMenuFlyoutItem.ItemsProperty, @params);
	}

	// MUX Reference: CSplitMenuFlyoutItem::LeaveImpl in SplitMenuFlyoutItem.cpp
	internal override void LeaveImpl(LeaveParams @params)
	{
		base.LeaveImpl(@params);
		MenuFlyout.KeyboardAcceleratorFlyoutItemLeave(this, this, SplitMenuFlyoutItem.ItemsProperty, @params);
	}
}
