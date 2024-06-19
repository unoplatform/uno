// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\ToggleMenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.5.4, commit 98a60c8

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ToggleMenuFlyoutItem types to Microsoft UI Automation.
/// </summary>
public partial class ToggleMenuFlyoutItemAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider
{
	/// <summary>
	/// Initializes a new instance of the ToggleMenuFlyoutItemAutomationPeer class.
	/// </summary>
	/// <param name="owner">The owner element to create for.</param>
	public ToggleMenuFlyoutItemAutomationPeer(ToggleMenuFlyoutItem owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Toggle)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	protected override string GetClassNameCore() => nameof(ToggleMenuFlyoutItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

	protected override string GetAcceleratorKeyCore()
	{
		var returnValue = base.GetAcceleratorKeyCore();

		if (returnValue is null)
		{
			// If AutomationProperties.AcceleratorKey hasn't been set, then return the value of our KeyboardAcceleratorTextOverride property.
			var ownerAsToggleMenuFlyoutItem = (ToggleMenuFlyoutItem)Owner;
			var keyboardAcceleratorTextOverride = ownerAsToggleMenuFlyoutItem.KeyboardAcceleratorTextOverride;
			returnValue = GetTrimmedKeyboardAcceleratorTextOverride(keyboardAcceleratorTextOverride);
		}

		return returnValue;
	}

	protected override int GetPositionInSetCore()
	{
		// First retrieve any valid value being directly set on the container, that value will get precedence.
		var returnValue = base.GetPositionInSetCore();

		// if it still is default value, calculate it ourselves.
		if (returnValue == -1)
		{
			returnValue = MenuFlyoutPresenter.GetPositionInSetHelper((MenuFlyoutItemBase)Owner);
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
			returnValue = MenuFlyoutPresenter.GetSizeOfSetHelper((MenuFlyoutItemBase)Owner);
		}

		return returnValue;
	}

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	public void Toggle()
	{
		var isEnabled = IsEnabled();
		if (!isEnabled)
		{
			throw new InvalidOperationException("Element is not enabled");
		}

		((ToggleMenuFlyoutItem)Owner).Invoke();
	}

	/// <summary>
	/// Gets the toggle state of the control.
	/// </summary>
	public ToggleState ToggleState
	{
		get
		{
			var isChecked = ((ToggleMenuFlyoutItem)Owner).IsChecked;
			return isChecked ? Automation.ToggleState.On : Automation.ToggleState.Off;
		}
	}

	internal void RaiseToggleStatePropertyChangedEvent(object oldValue, object newValue)
	{
		var oldState = ConvertToToggleState(oldValue);
		var newState = ConvertToToggleState(newValue);
		if (oldState != newState)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldState, newState);
		}
	}

	private static ToggleState ConvertToToggleState(object value)
	{
		var state = Automation.ToggleState.Indeterminate;

		if (value is bool boolValue)
		{
			if (boolValue)
			{
				state = Automation.ToggleState.On;
			}
			else
			{
				state = Automation.ToggleState.Off;
			}
		}

		return state;
	}
}
