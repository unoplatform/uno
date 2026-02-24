// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AutoSuggestBoxAutomationPeer_Partial.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes AutoSuggestBox types to Microsoft UI Automation.
/// </summary>
/// <param name="owner"></param>
public partial class AutoSuggestBoxAutomationPeer
	: FrameworkElementAutomationPeer, Provider.IInvokeProvider
{
	public AutoSuggestBoxAutomationPeer(AutoSuggestBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(AutoSuggestBox);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface is PatternInterface.Invoke)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	/// <summary>
	/// Sends a request to submit the auto-suggest query to the AutoSuggestBox associated with the automation peer.
	/// </summary>
	public void Invoke()
		=> (Owner as AutoSuggestBox).ProgrammaticSubmitQuery();
}
