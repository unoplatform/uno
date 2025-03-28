// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference HyperlinkButtonAutomationPeer_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes HyperlinkButton types to Microsoft UI Automation.
/// </summary>
public partial class HyperlinkButtonAutomationPeer : ButtonBaseAutomationPeer, Provider.IInvokeProvider
{
	public HyperlinkButtonAutomationPeer(HyperlinkButton owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => "Hyperlink";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Hyperlink;

	public void Invoke()
	{
		if (!IsEnabled())
		{
			// UIA_E_ELEMENTNOTENABLED
			throw new ElementNotEnabledException();
		}

		(Owner as HyperlinkButton).AutomationPeerClick();
	}
}
