using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Java.Lang.Annotation;
using Uno.UI.Helpers.WinUI;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class TreeViewItemAutomationPeer
	{
		private readonly TreeViewItem _owner;

		public TreeViewItemAutomationPeer(TreeViewItem owner) : base(owner)
		{
		}

		// IExpandCollapseProvider 
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				var targetNode = GetTreeViewNode();
				if (targetNode != null && targetNode.HasChildren)
				{
					if (targetNode.IsExpanded)
					{
						return ExpandCollapseState.Expanded;
					}
					return ExpandCollapseState.Collapsed;
				}
				return ExpandCollapseState.LeafNode;
			}
		}

		public void Collapse()
		{
			var ancestorTreeView = GetParentTreeView();
			if (ancestorTreeView != null)
			{
				var targetNode = GetTreeViewNode();
				if (targetNode != null)
				{
					ancestorTreeView.Collapse(targetNode);
					RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Collapsed);
				}
			}
		}

		public void Expand()
		{
			var ancestorTreeView = GetParentTreeView();
			if (ancestorTreeView != null)
			{
				var targetNode = GetTreeViewNode();
				if (targetNode != null)
				{
					ancestorTreeView.Expand(targetNode);
					RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Expanded);
				}
			}
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse)
			{
				return this;
			}

			var treeView = GetParentTreeView();

			if (treeView != null)
			{
				if (patternInterface == PatternInterface.SelectionItem && treeView.SelectionMode != TreeViewSelectionMode.None)
				{
					return this;
				}
			}

			return base.GetPatternCore(patternInterface);
		}


		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.TreeItem;
		}

		protected override string GetNameCore()
		{
			//Check to see if the item has a defined Automation Name
			string nameHString = base.GetNameCore();

			if (string.IsNullOrEmpty(nameHString))
			{
				var treeViewNode = GetTreeViewNode();
				if (treeViewNode != null)
				{
					nameHString = SharedHelpers.TryGetStringRepresentationFromObject(treeViewNode.Content);
				}

				if (string.IsNullOrEmpty(nameHString))
				{
					nameHString = "TreeViewNode";
				}
			}

			return nameHString;
		}

		protected override string GetClassNameCore()
		{
			return nameof(TreeViewItem);
		}

		// IAutomationPeerOverrides3
		protected override int GetPositionInSetCore()
		{
			ListView ancestorListView = GetParentListView();
			var targetNode = GetTreeViewNode();
			int positionInSet = 0;

			if (ancestorListView != null && targetNode != null)
			{
				var targetParentNode = targetNode.Parent;
				if (targetParentNode != null)
				{
					var position = targetParentNode.Children.IndexOf(targetNode);
					if (position != -1)
					{
						positionInSet = position + 1;
					}
				}
			}

			return positionInSet;
		}

		protected override int GetSizeOfSetCore()
		{
			var ancestorListView = GetParentListView();
			var targetNode = GetTreeViewNode();
			int setSize = 0;

			if (ancestorListView != null && targetNode != null)
			{
				var targetParentNode = targetNode.Parent;
				if (targetParentNode != null)
				{
					var size = targetParentNode.Children.Count;
					setSize = size;
				}
			}

			return setSize;
		}

		protected override int GetLevelCore()
		{
			ListView ancestorListView = GetParentListView();
			var targetNode = GetTreeViewNode();
			int level = -1;

			if (ancestorListView != null && targetNode != null)
			{
				level = targetNode.Depth;
				level++;
			}

			return level;
		}

		internal void RaiseExpandCollapseAutomationEvent(ExpandCollapseState newState)
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				ExpandCollapseState oldState;
				var expandCollapseStateProperty = ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty;

				if (newState == ExpandCollapseState.Expanded)
				{
					oldState = ExpandCollapseState.Collapsed;
				}
				else
				{
					oldState = ExpandCollapseState.Expanded;
				}

				// box_value(oldState) doesn't work here, use ReferenceWithABIRuntimeClassName to make Narrator can unbox it.
				RaisePropertyChangedEvent(expandCollapseStateProperty, oldState, newState);
			}
		}

		// ISelectionItemProvider
		private bool IsSelected
		{
			get
			{
				var treeViewItem = (TreeViewItem)Owner;
				return treeViewItem.IsSelectedInternal;
			}
		}

		private IRawElementProviderSimple SelectionContainer()
		{
			IRawElementProviderSimple provider = null;
			var listView = GetParentListView();
			if (listView != null)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);
				if (peer != null)
				{
					provider = ProviderFromPeer(peer);
				}
			}

			return provider;
		}

		private void AddToSelection()
		{
			UpdateSelection(true);
		}

		private void RemoveFromSelection()
		{
			UpdateSelection(false);
		}

		private void Select()
		{
			UpdateSelection(true);
		}

		private ListView GetParentListView()
		{
			var treeViewItemAncestor = (DependencyObject)Owner;
			ListView ancestorListView = null;

			while (treeViewItemAncestor != null && ancestorListView == null)
			{
				treeViewItemAncestor = VisualTreeHelper.GetParent(treeViewItemAncestor);
				if (treeViewItemAncestor != null)
				{
					ancestorListView = treeViewItemAncestor as ListView;
				}
			}

			return ancestorListView;
		}

		private TreeView GetParentTreeView()
		{
			var treeViewItemAncestor = (DependencyObject)Owner;
			TreeView ancestorTreeView = null;

			while (treeViewItemAncestor != null && ancestorTreeView == null)
			{
				treeViewItemAncestor = VisualTreeHelper.GetParent(treeViewItemAncestor);
				if (treeViewItemAncestor != null)
				{
					ancestorTreeView = treeViewItemAncestor as TreeView;
				}
			}

			return ancestorTreeView;
		}

		private TreeViewNode GetTreeViewNode()
		{
			TreeViewNode targetNode = null;
			var treeview = GetParentTreeView();
			if (treeview != null)
			{
				targetNode = treeview.NodeFromContainer(Owner);
			}
			return targetNode;
		}

		private void UpdateSelection(bool select)
		{
			var treeItem = Owner as TreeViewItem;
			if (treeItem != null)
			{
				var impl = treeItem;
				impl.UpdateSelection(select);
			}
		}
	}
}
