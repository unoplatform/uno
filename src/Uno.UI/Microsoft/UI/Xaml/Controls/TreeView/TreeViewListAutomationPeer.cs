using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TreeViewListAutomationPeer : SelectorAutomationPeer, ISelectionProvider, IDropTargetProvider
	{
		public TreeViewListAutomationPeer(TreeViewList owner) : base(owner)
		{
		}

		// IItemsControlAutomationPeerOverrides2
		protected override ItemAutomationPeer OnCreateItemAutomationPeer(object item)
		{
			var itemPeer = new TreeViewItemDataAutomationPeer(item, this);
			return itemPeer;
		}

		// DropTargetProvider
		string IDropTargetProvider.DropEffect => ((TreeViewList)Owner).GetDropTargetDropEffect();		

		string[] IDropTargetProvider.DropEffects => throw new NotImplementedException();	

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.DropTarget ||
			   (patternInterface == PatternInterface.Selection && IsMultiselect))
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Tree;
		}

		// ISelectionProvider
		bool ISelectionProvider.CanSelectMultiple => IsMultiselect ? true : base.CanSelectMultiple;		

		bool ISelectionProvider.IsSelectionRequired => IsMultiselect ? false : base.CanSelectMultiple;		

		IRawElementProviderSimple[] ISelectionProvider.GetSelection()
		{
			// The selected items might be collapsed, virtualized, so getting an accurate list of selected items is not possible.
			return Array.Empty<IRawElementProviderSimple>();
		}

		private bool IsMultiselect => ((TreeViewList)Owner).IsMultiselect;
	}
}
