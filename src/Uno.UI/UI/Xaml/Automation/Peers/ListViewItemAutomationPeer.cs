using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ListViewItemAutomationPeer : FrameworkElementAutomationPeer
	{
		public ListViewItemAutomationPeer(ListViewItem owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "ListViewItem";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.ListItem;
		}

		protected override int GetPositionInSetCore()
		{
			var returnValue = base.GetPositionInSetCore();
			if (returnValue == -1)
			{
				returnValue = GetPositionOrSizeOfSetHelper(isSetCount: false);
			}
			return returnValue;
		}

		protected override int GetSizeOfSetCore()
		{
			var returnValue = base.GetSizeOfSetCore();
			if (returnValue == -1)
			{
				returnValue = GetPositionOrSizeOfSetHelper(isSetCount: true);
			}
			return returnValue;
		}

		private int GetPositionOrSizeOfSetHelper(bool isSetCount)
		{
			if (Owner is not ListViewItem owner)
			{
				return -1;
			}

			var listView = ItemsControl.ItemsControlFromItemContainer(owner) as ListViewBase;
			if (listView?.GetOrCreateAutomationPeer() is not ItemsControlAutomationPeer itemsControlAutomationPeer)
			{
				return -1;
			}

			return isSetCount
				? itemsControlAutomationPeer.GetSizeOfSetHelper(owner)
				: itemsControlAutomationPeer.GetPositionInSetHelper(owner);
		}
	}
}
