// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollItemAdapter_Partial.cpp, tag winui3/release/1.5.3

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;

namespace DirectUI;

internal class ScrollItemAdapter
{
	private FrameworkElementAutomationPeer m_automationPeer;
	public ScrollItemAdapter(FrameworkElementAutomationPeer automationPeer)
	{
		m_automationPeer = automationPeer;
	}

	public void ScrollIntoView()
	{
		if (m_automationPeer is FrameworkElementAutomationPeer automationPeer)
		{
			if (automationPeer.Owner is UIElement owner)
			{
				//UNO TODO: Properly implement ScrollIntoView on ScrollItemAdapter

				//var rect = new Windows.Foundation.Rect();
				//var size = owner.GetRenderSize();

				//rect.X = 0;
				//rect.Y = 0;
				//rect.Width = size.Width;
				//rect.Height = size.Height;

				//owner.BringIntoView(rect, true /*forceIntoView*/, false /*useAnimation*/, false /*skipDuringManipulation*/);
			}
		}
	}
}
