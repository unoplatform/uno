// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewListAutomationPeer.cpp, tag winui3/release/1.8.4

using DirectUI;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes ToggleSwitch types to Microsoft UI Automation.
/// </summary>
public partial class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, Provider.IToggleProvider
{
	/// <summary>
	/// Initializes a new instance of the ToggleSwitchAutomationPeer class.
	/// </summary>
	/// <param name="owner"></param>
	public ToggleSwitchAutomationPeer(ToggleSwitch owner) : base(owner)
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

	protected override string GetClassNameCore() => nameof(ToggleSwitch);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

	protected override string GetLocalizedControlTypeCore() =>
		DXamlCore.GetCurrentNoCreate()?.GetLocalizedResourceString(nameof(ToggleSwitch)) ?? base.GetLocalizedControlTypeCore();

	protected override string GetNameCore()
	{
		// If the control has an explicitly set UIA Name, use that
		// Otherwise we construct a Name from the Header text and the On/OffContent
		var baseName = base.GetNameCore();
		if (!string.IsNullOrEmpty(baseName))
		{
			return baseName;
		}
		else
		{
			if (Owner is not ToggleSwitch owner)
			{
				return string.Empty;
			}

			var headerText = owner.Header?.ToString() ?? string.Empty;
			var onOffContentText = string.Empty;

			if (owner.IsOn)
			{
				// We only want to include the OnContent if custom content has been provided.
				// The default value of OnContent is the string "On", but including this in the UIA Name adds no value, since this information is
				// already included in the ToggleState. Narrator reads out both the ToggleState and the Name (We don't want it to read "On On ToggleSwitch").
				if (owner.OnContent is { } onContent)
				{
					onOffContentText = onContent.ToString() ?? string.Empty;
				}
			}
			else
			{
				// As above, we only include custom OffContent.
				if (owner.OffContent is { } offContent)
				{
					onOffContentText = offContent.ToString() ?? string.Empty;
				}
			}

			if (!string.IsNullOrEmpty(headerText) && !string.IsNullOrEmpty(onOffContentText))
			{
				// Return the header text followed by the on/off content separated by a space:
				return $"{headerText} {onOffContentText}";
			}
			else if (!string.IsNullOrEmpty(headerText))
			{
				return headerText;
			}
			else
			{
				// onOffContentText might be empty, but that's ok.
				return onOffContentText;
			}
		}
	}

	protected override Point GetClickablePointCore()
	{
		var owner = Owner as ToggleSwitch;
		return owner.AutomationGetClickablePoint();
	}

	/// <summary>
	/// Gets the toggle state of the control.
	/// </summary>
	public ToggleState ToggleState => ((ToggleSwitch)Owner).IsOn ? ToggleState.On : ToggleState.Off;

	/// <summary>
	/// Cycles through the toggle states of a control.
	/// </summary>
	public void Toggle()
	{
		if (IsEnabled())
		{
			((ToggleSwitch)Owner).AutomationPeerToggle();
		}
	}

	internal void RaiseToggleStatePropertyChangedEvent(object pOldValue, object pNewValue)
	{
		var oldValue = ToggleButtonAutomationPeer.ConvertToToggleState(pOldValue);
		var newValue = ToggleButtonAutomationPeer.ConvertToToggleState(pNewValue);

		MUX_ASSERT(oldValue != ToggleState.Indeterminate);
		MUX_ASSERT(newValue != ToggleState.Indeterminate);

		if (oldValue != newValue)
		{
			RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
		}
	}
}
