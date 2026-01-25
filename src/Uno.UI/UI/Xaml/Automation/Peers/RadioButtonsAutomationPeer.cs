// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RadioButtonsAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RadioButtons types to Microsoft UI Automation.
/// </summary>
public partial class RadioButtonsAutomationPeer : FrameworkElementAutomationPeer
{
	public RadioButtonsAutomationPeer(RadioButtons owner) : base(owner)
	{
	}

	protected override string GetClassNameCore()
		=> nameof(RadioButtons);

	protected override string GetNameCore()
	{
		var name = base.GetNameCore();

		if (string.IsNullOrEmpty(name))
		{
			if (Owner is RadioButtons radioButtons)
			{
				name = SharedHelpers.TryGetStringRepresentationFromObject(radioButtons.Header);
			}
		}

		return name;
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Group;
}
