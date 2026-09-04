// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\MenuFlyoutSubItem.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutSubItem
{
	// MUX Reference: CMenuFlyoutSubItem::EnterImpl in MenuFlyoutSubItem.cpp
	internal override void EnterImpl(EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);
		MenuFlyout.KeyboardAcceleratorFlyoutItemEnter(this, this, MenuFlyoutSubItem.ItemsProperty, @params);
	}

	// MUX Reference: CMenuFlyoutSubItem::LeaveImpl in MenuFlyoutSubItem.cpp
	internal override void LeaveImpl(LeaveParams @params)
	{
		base.LeaveImpl(@params);
		MenuFlyout.KeyboardAcceleratorFlyoutItemLeave(this, this, MenuFlyoutSubItem.ItemsProperty, @params);
	}
}
