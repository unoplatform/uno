﻿using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.MenuBarAutomationPeer instead.")]
public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
{
	public MenuBarItemAutomationPeer()
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.MenuBarAutomationPeer instead.");
	}
}
