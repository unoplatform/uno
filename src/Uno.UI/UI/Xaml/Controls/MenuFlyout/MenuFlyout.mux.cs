// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\MenuFlyout.cpp, tag winui3/release/1.5.4, commit 98a60c8

using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	internal static void KeyboardAcceleratorFlyoutItemEnter(
		DependencyObject element,
		DependencyObject pNamescopeOwner,
		DependencyProperty property,
		EnterParams parameters)
	{
		var value = element.GetValue(property);
		if (value is MenuFlyoutItemBaseCollection items)
		{
			// This is a dead enter to register any keyboard accelerators that may be present in the MenuFlyout items
			// to the list of live accelerators
			parameters = new EnterParams { IsForKeyboardAccelerator = true, IsLive = false };
			//params.fSkipNameRegistration = true;
			//params.fUseLayoutRounding = false;
			//params.fCoercedIsEnabled = false;

#if HAS_UNO // Custom implementation: Simulate enter/leave for KAs on all targets
			foreach (MenuFlyoutItemBase item in items)
			{
				if (item.KeyboardAccelerators is KeyboardAcceleratorCollection kac)
				{
					kac.Enter(pNamescopeOwner, parameters);
				}

				if (item is MenuFlyoutSubItem subItem)
				{
					subItem.EnterImpl(pNamescopeOwner, parameters);
				}
			}
#endif
		}
	}

	internal static void KeyboardAcceleratorFlyoutItemLeave(
		DependencyObject element,
		DependencyObject pNamescopeOwner,
		DependencyProperty property,
		LeaveParams parameters)
	{
		var value = element.GetValue(property);
		if (value is MenuFlyoutItemBaseCollection items)
		{
			// This is a dead leave to remove any keyboard accelerators that may be present in the MenuFlyout items
			// from the list of live accelerators
			parameters = new LeaveParams { IsForKeyboardAccelerator = true, IsLive = false };
			//params.fSkipNameRegistration = true;
			//params.fUseLayoutRounding = false;
			//params.fCoercedIsEnabled = false;

#if HAS_UNO // Custom implementation: Simulate enter/leave for KAs on all targets
			foreach (MenuFlyoutItemBase item in items)
			{
				if (item.KeyboardAccelerators is KeyboardAcceleratorCollection kac)
				{
					kac.Leave(pNamescopeOwner, parameters);
				}

				if (item is MenuFlyoutSubItem subItem)
				{
					subItem.LeaveImpl(pNamescopeOwner, parameters);
				}
			}
#endif
		}
	}

	private void EnterImpl(DependencyObject namescopeOwner, EnterParams parameters)
	{
		//base.EnterImpl(pNamescopeOwner, params));
		KeyboardAcceleratorFlyoutItemEnter(this, namescopeOwner, MenuFlyout.ItemsProperty, parameters);
	}

	private void LeaveImpl(DependencyObject namescopeOwner, LeaveParams parameters)
	{
		//base.LeaveImpl(pNamescopeOwner, params);
		KeyboardAcceleratorFlyoutItemLeave(this, namescopeOwner, MenuFlyout.ItemsProperty, parameters);
	}
}
