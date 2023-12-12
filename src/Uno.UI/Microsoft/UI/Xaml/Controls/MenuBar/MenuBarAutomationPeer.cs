// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MenuBarAutomationPeer.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class MenuBarAutomationPeer : FrameworkElementAutomationPeer
	{
		public MenuBarAutomationPeer(MenuBar owner) : base(owner)
		{
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.MenuBar;
		}
	}
}
