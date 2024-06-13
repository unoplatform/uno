using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyoutSubItem
{
	private void EnterImpl(DependencyObject pNamescopeOwner, EnterParams parameters)
	{

		base.EnterImpl(pNamescopeOwner, parameters);
		MenuFlyout.KeyboardAcceleratorFlyoutItemEnter(this, pNamescopeOwner, MenuFlyoutSubItem.ItemsProperty, parameters);

		return S_OK;
	}

	private void LeaveImpl(DependencyObject pNamescopeOwner, LeaveParams parameters)
	{
		base.LeaveImpl(pNamescopeOwner, parameters);
		MenuFlyout.KeyboardAcceleratorFlyoutItemLeave(this, pNamescopeOwner, MenuFlyoutSubItem.ItemsProperty, parameters);

		return S_OK;
	}
}
