#nullable enable
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GroupItem
	{
		private ItemsControl? m_tpItemsControl;

		protected override AutomationPeer OnCreateAutomationPeer()
			=> new GroupItemAutomationPeer(this);

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			m_tpItemsControl = GetTemplateChild("ItemsControl") as ItemsControl;
		}

		internal ItemsControl? GetTemplatedItemsControl() => m_tpItemsControl;
	}
}
