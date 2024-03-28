using System;

namespace Windows.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.MenuBarAutomationPeer instead.")]
public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
{
	public MenuBarItemAutomationPeer()
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.MenuBarAutomationPeer instead.");
	}
}
