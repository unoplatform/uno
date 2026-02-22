// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichEditBoxAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System.Collections.Generic;
using DirectUI;

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
		TextBoxPlaceholderTextHelper.SetupPlaceholderTextBlockDescribedBy(owner);

		return null; //DT TODO: This will be fixed with FrameworkAP
					 //GetAutomationPeerCollection(AutomationProperties.DescribedByProperty);
	}
}
