namespace Microsoft.UI.Xaml.Controls;

public partial class ListBoxItem
{
	protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		=> new Automation.Peers.ListBoxItemAutomationPeer(this);
}
