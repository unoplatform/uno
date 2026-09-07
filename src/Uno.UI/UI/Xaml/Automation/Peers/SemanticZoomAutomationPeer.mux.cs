// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference SemanticZoomAutomationPeer_Partial.cpp, commit 3c9c168844

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
