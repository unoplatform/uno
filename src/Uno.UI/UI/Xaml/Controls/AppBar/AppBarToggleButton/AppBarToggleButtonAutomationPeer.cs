// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AppBarToggleButtonAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using DirectUI;
using Microsoft.UI.Xaml.Controls;
using AppBarToggleButton = Microsoft.UI.Xaml.Controls.AppBarToggleButton;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AppBarToggleButton types to Microsoft UI Automation.
/// </summary>
public partial class AppBarToggleButtonAutomationPeer : ToggleButtonAutomationPeer
{
	public AppBarToggleButtonAutomationPeer(AppBarToggleButton owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(AppBarToggleButton);

	protected override string GetNameCore()
	{
		// Note: We are calling FrameworkElementAutomationPeer::GetNameCore here, rather than going through
		// any of our own immediate superclasses, to avoid the logic in ButtonBaseAutomationPeer that will
		// substitute Content for the automation name if the latter is unset -- we want to either get back
		// the actual value of AutomationProperties.Name if it has been set, or null if it hasn't.

		//UNO ONLY: ButtonBaseAutomationPeer doesn't substitue Content for automation name if it is unset
		var returnValue = base.GetNameCore();

		if (string.IsNullOrWhiteSpace(returnValue))
		{
			var owner = GetOwningAppBarToggleButton();
			returnValue = owner.Label;
		}

		return returnValue;
	}

	protected override string GetLocalizedControlTypeCore() =>
		DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_APPBAR_TOGGLEBUTTON");

	protected override string GetAcceleratorKeyCore()
	{
		var returnValue = base.GetAcceleratorKeyCore();

		if (string.IsNullOrWhiteSpace(returnValue))
		{
			// If AutomationProperties.AcceleratorKey hasn't been set, then return the value of our KeyboardAcceleratorTextOverride property.
			var owner = GetOwningAppBarToggleButton();
			var keyboardAcceleratorTextOverride = owner.KeyboardAcceleratorTextOverride;

			returnValue = keyboardAcceleratorTextOverride?.Trim();
		}

		return returnValue;
	}

	protected override bool IsKeyboardFocusableCore()
	{
		var owner = GetOwningAppBarToggleButton();

		var parentCommandBar = CommandBar.FindParentCommandBarForElement(owner);

		if (parentCommandBar != null)
		{
			return AppBarButtonHelpers<AppBarButton>.IsKeyboardFocusable(owner);
		}
		else
		{
			return base.IsKeyboardFocusableCore();
		}
	}

	public new void Toggle()
	{
		var isEnabled = IsEnabled();
		if (!isEnabled)
		{
			throw new ElementNotEnabledException();
		}

		var owner = GetOwningAppBarToggleButton();
		owner.AutomationToggleButtonOnToggle();
	}

	public new ToggleState ToggleState
	{
		get
		{
			var owner = GetOwningAppBarToggleButton();
			var isChecked = owner.IsChecked;

			if (isChecked == null)
			{
				return ToggleState.Indeterminate;
			}
			else
			{
				if (isChecked.Value)
				{
					return ToggleState.On;
				}
				else
				{
					return ToggleState.Off;
				}
			}
		}
	}

	internal void RaiseToggleStatePropertyChangedEvent(bool pOldValue, bool pNewValue)
	{
		var oldValue = AppBarToggleButtonAutomationPeer.ConvertToToggleState(pOldValue);
		var newValue = AppBarToggleButtonAutomationPeer.ConvertToToggleState(pNewValue);

		if (oldValue != newValue)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
		}
	}

	private static new ToggleState ConvertToToggleState(object value)
	{
		var toggleState = ToggleState.Indeterminate;

		if (value is not null)
		{
			var boolValue = (bool)value;
			toggleState = boolValue ? ToggleState.On : ToggleState.Off;
		}

		return toggleState;
	}

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			var owner = GetOwningAppBarToggleButton();
			returnValue = CommandBar.GetPositionInSetStatic(owner);
		}

		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetSizeOfSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			var owner = GetOwningAppBarToggleButton();
			returnValue = CommandBar.GetSizeOfSetStatic(owner);
		}

		return returnValue;
	}

	private AppBarToggleButton GetOwningAppBarToggleButton()
	{
		var owner = Owner;
		return owner as AppBarToggleButton;
	}
}
