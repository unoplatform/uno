// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollItemAdapter_Partial.cpp, tag winui3/release/1.5.3

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace DirectUI;

internal class ScrollItemAdapter : IScrollItemProvider
{
	private FrameworkElementAutomationPeer m_automationPeer;
	public ScrollItemAdapter(FrameworkElementAutomationPeer automationPeer)
	{
		m_automationPeer = automationPeer;
	}

	public void ScrollIntoView()
	{
		if (m_automationPeer?.Owner is UIElement owner)
		{
			owner.StartBringIntoView(new Microsoft.UI.Xaml.BringIntoViewOptions
			{
				AnimationDesired = false
			});
		}
	}
}
