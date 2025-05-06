// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\MenuFlyoutSubItem.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutSubItem
{
	internal void EnterImpl(DependencyObject pNamescopeOwner, EnterParams parameters)
	{
		//base.EnterImpl(pNamescopeOwner, parameters);
		MenuFlyout.KeyboardAcceleratorFlyoutItemEnter(this, pNamescopeOwner, MenuFlyoutSubItem.ItemsProperty, parameters);
	}

	internal void LeaveImpl(DependencyObject pNamescopeOwner, LeaveParams parameters)
	{
		//base.LeaveImpl(pNamescopeOwner, parameters);
		MenuFlyout.KeyboardAcceleratorFlyoutItemLeave(this, pNamescopeOwner, MenuFlyoutSubItem.ItemsProperty, parameters);
	}
}
