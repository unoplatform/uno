// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference SemanticZoomAutomationPeer_Partial.cpp, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class SemanticZoomAutomationPeer
{
	internal IList<AutomationPeer> GetAutomationPeerChildren(UIElement presenter) =>
		GetAutomationPeersForChildrenOfElement(presenter);

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if ((Owner as Controls.SemanticZoom)?.AutomationGetActivePresenter() is { } activePresenter)
		{
			return GetAutomationPeersForChildrenOfElement(activePresenter);
		}

		return new List<AutomationPeer>();
	}
}
