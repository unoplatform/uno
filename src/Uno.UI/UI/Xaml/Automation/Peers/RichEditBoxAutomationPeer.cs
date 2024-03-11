// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichEditBoxAutomationPeer_Partial.cpp, tag winui3/release/1.4.2
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RichEditBox types to Microsoft UI Automation.
/// </summary>
public partial class RichEditBoxAutomationPeer : FrameworkElementAutomationPeer
{
	public RichEditBoxAutomationPeer(Controls.RichEditBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(Controls.RichEditBox);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Edit;

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
	{
		var owner = Owner as Controls.RichEditBox;

		//IFC_RETURN(get_Owner(spOwner.GetAddressOf()));

		//IFC_RETURN(TextBoxPlaceholderTextHelper::SetupPlaceholderTextBlockDescribedBy(spOwner));

		//IFC_RETURN(GetAutomationPeerCollection(UIAXcp::APDescribedByProperty, returnValue));
	}
}
