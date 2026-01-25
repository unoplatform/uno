// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ToggleMenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ToggleMenuFlyoutItem types to Microsoft UI Automation.
/// </summary>
public partial class ToggleMenuFlyoutItemAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
{
	public ToggleMenuFlyoutItemAutomationPeer(Controls.ToggleMenuFlyoutItem owner) : base(owner)
	{

	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Toggle)
		{
			return this;
		}
		else
		{
			return base.GetPatternCore(patternInterface);
		}
	}

	protected override string GetClassNameCore() => nameof(Controls.ToggleMenuFlyoutItem);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.MenuItem;

	protected override string GetAcceleratorKeyCore()
	{
		var acceleratorKey = base.GetAcceleratorKeyCore();

		if (string.IsNullOrEmpty(acceleratorKey))
		{
			return (Owner as Controls.ToggleMenuFlyoutItem).KeyboardAcceleratorTextOverride;
		}

		return acceleratorKey;
	}

	protected override int GetPositionInSetCore()
	{
		var returnValue = base.GetPositionInSetCore();

		if (returnValue == -1)
		{
			returnValue = GetPositionInSet();
		}

		return returnValue;
	}

	protected override int GetSizeOfSetCore()
	{
		var returnValue = base.GetSizeOfSetCore();

		if (returnValue == -1)
		{
			returnValue = GetPositionInSet();
		}

		return returnValue;
	}

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	/// <exception cref="ElementNotEnabledException"></exception>
	public void Toggle()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		(Owner as Controls.ToggleMenuFlyoutItem).Invoke();
	}

	/// <summary>
	/// Gets the toggle state of the control.
	/// </summary>
	public ToggleState ToggleState
	{
		get
		{
			var isChecked = (Owner as Controls.ToggleMenuFlyoutItem).IsChecked;

			if (isChecked)
			{
				return ToggleState.On;
			}
			else
			{
				return ToggleState.Off;
			}
		}
	}

	internal void RaisePropertyChangedEvent(object oldValue, object newValue)
	{
		var oldToggleState = ConvertToToggleState(oldValue);
		var newToggleState = ConvertToToggleState(newValue);

		if (oldToggleState != newToggleState)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldToggleState, newToggleState);
		}
	}

	/// <summary>
	/// Convert the Boolean in Inspectable to the ToggleState Enum, if the Inspectable is NULL that corresponds to Indeterminate state.
	/// </summary>
	private ToggleState ConvertToToggleState(object value)
	{
		if (value is bool v)
		{
			return v ? ToggleState.On : ToggleState.Off;
		}
		return ToggleState.Indeterminate;
	}
}
