namespace Microsoft.UI.Xaml.Controls;

public partial class ListBox
{
	protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		=> new Automation.Peers.ListBoxAutomationPeer(this);
}
