using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

[Obsolete(
	"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.MenuBarAutomationPeer instead.")]
public partial class MenuBarAutomationPeer : FrameworkElementAutomationPeer
{
	public MenuBarAutomationPeer()
	{
		throw new NotImplementedException(
			"The Microsoft.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.MenuBarAutomationPeer instead.");
	}
}
