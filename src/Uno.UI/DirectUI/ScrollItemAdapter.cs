// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ScrollItemAdapter_Partial.cpp, tag winui3/release/1.5-stable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Uno.UI.DirectUI;

internal class ScrollItemAdapter
{
	private FrameworkElementAutomationPeer m_automationPeer;

	// Set the owner of Adapter
	public void PutOwner(FrameworkElementAutomationPeer automationPeer)
	{
		m_automationPeer = automationPeer;
	}

	public void ScrollIntoView()
	{
		if (m_automationPeer is FrameworkElementAutomationPeer automationPeer)
		{
			if (automationPeer.Owner is UIElement owner)
			{
				var rect = new Windows.Foundation.Rect();
				var size = new Windows.Foundation.Size();

				owner.GetRenderSize(out size);

				rect.X = 0;
				rect.Y = 0;
				rect.Width = size.Width;
				rect.Height = size.Height;

				owner.BringIntoView(rect, true /*forceIntoView*/, false /*useAnimation*/, false /*skipDuringManipulation*/);
			}
		}
	}
}
