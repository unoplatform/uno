// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\MenuFlyoutSubItem.cpp, tag winui3/release/1.8.1, commit cd3b7ad0eca

using Microsoft.UI.Xaml.Input;
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

	// Uno specific: Walk sub-items to find keyboard accelerators.
	// In WinUI, this is handled by the core-level TryInvokeKeyboardAccelerator tree walk
	// which can access sub-items directly. In Uno, sub-items are not visual children,
	// so we need to explicitly iterate them here.
	protected override void OnProcessKeyboardAccelerators(ProcessKeyboardAcceleratorEventArgs args)
	{
		base.OnProcessKeyboardAccelerators(args);

		if (args.Handled)
		{
			return;
		}

		if (m_tpItems is not null)
		{
			for (int i = 0; i < m_tpItems.Count; i++)
			{
				MenuFlyoutItemBase spItem = m_tpItems[i];
				spItem.TryInvokeKeyboardAccelerator(args);
				if (args.Handled)
				{
					return;
				}
			}
		}
	}
}
