<<<<<<<< HEAD:src/Uno.UI/UI/Xaml/Controls/MenuFlyout/MenuFlyoutItemBase.cs
﻿using System;
========
#if false
using System;
>>>>>>>> a4379b3ee3 (chore: Start porting WinUI Flyout and Popup):src/Uno.UI/UI/Xaml/Controls/Flyout/Legacy/MenuFlyoutItemBase.cs
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public abstract partial class MenuFlyoutItemBase : Control
	{
		private WeakReference m_wrParentMenuFlyoutPresenter;

		public MenuFlyoutItemBase()
		{

		}

		// Get the parent MenuFlyoutPresenter.
		internal MenuFlyoutPresenter GetParentMenuFlyoutPresenter()
			=> m_wrParentMenuFlyoutPresenter?.Target as MenuFlyoutPresenter;

		// Sets the parent MenuFlyoutPresenter.
		internal void SetParentMenuFlyoutPresenter(MenuFlyoutPresenter pParentMenuFlyoutPresenter)
			=> m_wrParentMenuFlyoutPresenter = new WeakReference(pParentMenuFlyoutPresenter);

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
}
#endif
