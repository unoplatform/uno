// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PersonPictureAutomationPeer.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class PersonPictureAutomationPeer : FrameworkElementAutomationPeer
{
	public PersonPictureAutomationPeer(PersonPicture owner) : base(owner)
	{
	}

	//IAutomationPeerOverrides

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

	protected override string GetClassNameCore() => nameof(PersonPicture);
}
