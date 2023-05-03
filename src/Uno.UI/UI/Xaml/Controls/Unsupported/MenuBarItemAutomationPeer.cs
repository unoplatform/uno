using System;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls;

[Obsolete(
	"The Windows.UI.Xaml.Controls version of this control is not supported. " +
	"Please use Microsoft.UI.Xaml.Controls.MenuBarAutomationPeer instead.")]
public partial class MenuBarItemAutomationPeer : FrameworkElementAutomationPeer
{
	public MenuBarItemAutomationPeer()
	{
		throw new NotImplementedException(
			"The Windows.UI.Xaml.Controls version of this control is not supported. " +
			"Please use Microsoft.UI.Xaml.Controls.MenuBarAutomationPeer instead.");
	}
}
