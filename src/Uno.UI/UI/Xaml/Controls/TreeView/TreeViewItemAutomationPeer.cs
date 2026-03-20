// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewItemAutomationPeer.cpp, tag winui3/release/1.8.4

using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewItem = Microsoft.UI.Xaml.Controls.TreeViewItem;
using TreeViewNode = Microsoft.UI.Xaml.Controls.TreeViewNode;
using TreeViewSelectionMode = Microsoft.UI.Xaml.Controls.TreeViewSelectionMode;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes TreeViewItem types to Microsoft UI Automation.
/// </summary>
public partial class TreeViewItemAutomationPeer : ListViewItemAutomationPeer, IExpandCollapseProvider, ISelectionItemProvider
{
	/// <summary>
	/// Initializes a new instance of the TreeViewItemAutomationPeer class.
	/// </summary>
	/// <param name="owner">The TreeViewItem control instance to create the peer for.</param>
	public TreeViewItemAutomationPeer(TreeViewItem owner) : base(owner)
	{
	}

	// IExpandCollapseProvider

	/// <summary>
	/// Gets a value indicating the expanded or collapsed state of the associated TreeViewItem.
	/// </summary>
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

	/// <summary>
	/// Collapses the associated TreeViewItem.
	/// </summary>
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

	/// <summary>
	/// Expands the associated TreeViewItem.
	/// </summary>
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
	bool ISelectionItemProvider.IsSelected
	{
		get
		{
			var treeViewItem = (TreeViewItem)Owner;
			return treeViewItem.IsSelectedInternal;
		}
	}

	IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
	{
		get
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
	}

	void ISelectionItemProvider.AddToSelection()
	{
		UpdateSelection(true);
	}

	void ISelectionItemProvider.RemoveFromSelection()
	{
		UpdateSelection(false);
	}

	void ISelectionItemProvider.Select()
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
