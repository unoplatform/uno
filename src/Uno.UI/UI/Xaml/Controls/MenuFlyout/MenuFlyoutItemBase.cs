// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\MenuFlyoutItemBase_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the base class for items in a MenuFlyout control.
/// </summary>
public abstract partial class MenuFlyoutItemBase : Control
{
	private ManagedWeakReference m_wrParentMenuFlyoutPresenter;

	public MenuFlyoutItemBase()
	{
	}

	// Get the parent MenuFlyoutPresenter.
	internal MenuFlyoutPresenter GetParentMenuFlyoutPresenter()
		=> m_wrParentMenuFlyoutPresenter is { IsAlive: true } ? m_wrParentMenuFlyoutPresenter.Target as MenuFlyoutPresenter : null;

	// Sets the parent MenuFlyoutPresenter.
	internal void SetParentMenuFlyoutPresenter(MenuFlyoutPresenter pParentMenuFlyoutPresenter)
		=> m_wrParentMenuFlyoutPresenter = WeakReferencePool.RentWeakReference(this, pParentMenuFlyoutPresenter);

	internal bool GetShouldBeNarrow()
	{
		MenuFlyoutPresenter spPresenter = GetParentMenuFlyoutPresenter();

		var shouldBeNarrow = false;

		if (spPresenter != null)
		{
			MenuFlyout spParentFlyout = spPresenter.GetParentMenuFlyout();

			if (spParentFlyout != null)
			{
				shouldBeNarrow =
					(spParentFlyout.InputDeviceTypeUsedToOpen == FocusInputDeviceKind.Mouse) ||
					(spParentFlyout.InputDeviceTypeUsedToOpen == FocusInputDeviceKind.Pen) ||
					(spParentFlyout.InputDeviceTypeUsedToOpen == FocusInputDeviceKind.Keyboard);
			}
		}

		return shouldBeNarrow;
	}
}
