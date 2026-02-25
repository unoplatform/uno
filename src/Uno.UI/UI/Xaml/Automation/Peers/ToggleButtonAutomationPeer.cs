// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ToggleButtonAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using Microsoft.UI.Xaml.Controls.Primitives;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ToggleButton types to Microsoft UI Automation.
/// </summary>
public partial class ToggleButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IToggleProvider
{
	public ToggleButtonAutomationPeer(ToggleButton owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(ToggleButton);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Button;

	/// <summary>
	/// Gets the toggle state of the control.
	/// </summary>
	public ToggleState ToggleState
		=> ((ToggleButton)Owner).IsChecked switch
		{
			(true) => ToggleState.On,
			(false) => ToggleState.Off,
			_ => ToggleState.Indeterminate,
		};

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	public void Toggle()
	{
		if (IsEnabled())
		{
			((ToggleButton)Owner).AutomationToggleButtonOnToggle();
		}
	}

	internal void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
	{
		var oldValue = ConvertToToggleState(pOldValue);
		var newValue = ConvertToToggleState(pNewValue);

		if (oldValue != newValue)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
		}
	}

	/// <summary>
	/// Convert the Boolean in Inspectable to the ToggleState Enum, if the Inspectable is null
	/// that corresponds to Indeterminate state.
	/// </summary>
	internal static ToggleState ConvertToToggleState(object pValue)
	{
		var pToggleState = ToggleState.Indeterminate;

		if (pValue != null)
		{
			var bValue = (bool)pValue;

			if (bValue)
			{
				pToggleState = ToggleState.On;
			}
			else
			{

				pToggleState = ToggleState.Off;
			}
		}

		return pToggleState;
	}
}
