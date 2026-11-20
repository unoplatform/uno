using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class ListBox
{
	protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		=> new Automation.Peers.ListBoxAutomationPeer(this);

	internal override void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
	{
		base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);

		// Selector skips the containers' IsSelected while SelectedIndex drives the change, so sync them
		// here as ListViewBase does.
		SetContainerIsSelected(oldSelectedIndex, false);
		SetContainerIsSelected(newSelectedIndex, true);
	}

	private void SetContainerIsSelected(int index, bool isSelected)
	{
		if (index >= 0 && ContainerFromIndex(index) is SelectorItem container)
		{
			container.IsSelected = isSelected;
		}
	}
}
