// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ToggleMenuFlyoutItemAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
namespace Microsoft.UI.Xaml.Automation.Peers;

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

	public void Toggle()
	{
		if (!IsEnabled())
		{
			throw new ElementNotEnabledException();
		}

		(Owner as Controls.ToggleMenuFlyoutItem).Invoke();
	}

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
			//base.RaisePropertyChangedEvent(AutomationProperties.ToggleStateProperty, oldToggleState, newToggleState);
		}
	}

	// Convert the Boolean in Inspectable to the ToggleState Enum, if the Inspectable is NULL that corresponds to Indeterminate state.
	private ToggleState ConvertToToggleState(object value)
	{
		if (value is bool v)
		{
			return v ? ToggleState.On : ToggleState.Off;
		}
		return ToggleState.Indeterminate;
	}
}
