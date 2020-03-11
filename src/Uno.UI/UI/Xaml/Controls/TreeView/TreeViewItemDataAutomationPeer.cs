using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public class TreeViewItemDataAutomationPeer : ItemAutomationPeer, IExpandCollapseProvider
	{
		public TreeViewItemDataAutomationPeer(object item, ItemsControlAutomationPeer parent)
			: base(item, parent)
		{
		}

		// IExpandCollapseProvider 
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				var peer = GetTreeViewItemAutomationPeer();
				if (peer != null)
				{
					return peer.ExpandCollapseState;
				}
				throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
			}
		}

		public void Collapse()
		{
			var peer = GetTreeViewItemAutomationPeer();
			if (peer != null)
			{
				peer.Collapse();
				return;
			}
			throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
		}

		public void Expand()
		{
			var peer = GetTreeViewItemAutomationPeer();
			if (peer != null)
			{
				peer.Expand();
				return;
			}
			throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		private TreeViewItemAutomationPeer GetTreeViewItemAutomationPeer()
		{
			// ItemsAutomationPeer hold ItemsControlAutomationPeer and Item properties.
			// ItemsControlAutomationPeer -> ItemsControl by ItemsControlAutomationPeer.Owner -> ItemsControl Look up Item to get TreeViewItem -> Get TreeViewItemAutomationPeer
			var itemsControlAutomationPeer = ItemsControlAutomationPeer;
			if (itemsControlAutomationPeer != null)
			{
				var itemsControl = itemsControlAutomationPeer.Owner as ItemsControl;
				if (itemsControl != null)
				{
					var item = itemsControl.ContainerFromItem(Item) as UIElement;
					if (item != null)
					{
						var treeViewItemAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(item) as TreeViewItemAutomationPeer;
						if (treeViewItemAutomationPeer != null)
						{
							return treeViewItemAutomationPeer;
						}
					}
				}
			}
			throw new InvalidOperationException(UIA_E_ELEMENTNOTENABLED);
		}
	}
}
