// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewModel.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal partial class TreeViewViewModel : ObservableVector<object>
{
	private readonly SelectedTreeNodeVector m_selectedNodes;
	private readonly SelectedItemsVector m_selectedItems;
	private readonly Dictionary<object, TreeViewNode> m_itemToNodeMap;

	private TreeViewNode m_originNode;
	private TreeViewList m_TreeViewList;
	private TreeView m_TreeView;

	private bool m_isContentMode;

	private int m_selectionTrackingCounter = 0;
	private readonly List<WeakReference<object>> m_addedSelectedItems = new List<WeakReference<object>>();
	private List<object> m_removedSelectedItems = new List<object>();

	public TreeViewViewModel()
	{
		var selectedNodes = new SelectedTreeNodeVector();
		selectedNodes.SetViewModel(this);
		m_selectedNodes = selectedNodes;

		var selectedItems = new SelectedItemsVector();
		selectedItems.SetViewModel(this);
		m_selectedItems = selectedItems;

		m_itemToNodeMap = new Dictionary<object, TreeViewNode>();
	}

	~TreeViewViewModel()
	{
		var origin = m_originNode;
		if (origin != null)
		{
			origin.ChildrenChanged -= TreeViewNodeVectorChanged;
		}

		ClearEventTokenVectors();
	}

	internal void ExpandNode(TreeViewNode value)
	{
		value.IsExpanded = true;
	}

	internal void CollapseNode(TreeViewNode value)
	{
		value.IsExpanded = false;
	}

	internal void SelectAll()
	{
		try
		{
			BeginSelectionChanges();

			UpdateSelection(m_originNode, TreeNodeSelectionState.Selected);
		}
		finally
		{
			EndSelectionChanges();
		}
	}

	internal void SelectSingleItem(object item)
	{
		try
		{
			BeginSelectionChanges();

			var selectedItems = SelectedItems;
			if (selectedItems.Count > 0)
			{
				selectedItems.Clear();
			}
			if (item != null)
			{
				selectedItems.Add(item);
			}
		}
		finally
		{
			EndSelectionChanges();
		}
	}

	internal void SelectNode(TreeViewNode node, bool isSelected)
	{
		try
		{
			BeginSelectionChanges();

			var selectedNodes = SelectedNodes;
			if (isSelected)
			{
				if (IsInSingleSelectionMode() && selectedNodes.Count > 0)
				{
					selectedNodes.Clear();
				}
				selectedNodes.Add(node);
			}
			else
			{
				int index;
				if ((index = selectedNodes.IndexOf(node)) > -1)
				{
					selectedNodes.RemoveAt(index);
				}
			}
		}
		finally
		{
			EndSelectionChanges();
		}
	}

	internal void SelectByIndex(int index, TreeNodeSelectionState state)
	{
		try
		{
			BeginSelectionChanges();

			var targetNode = GetNodeAt(index);
			UpdateSelection(targetNode, state);
		}
		finally
		{
			EndSelectionChanges();
		}
	}

	internal void BeginSelectionChanges()
	{
		if (!IsInSingleSelectionMode())
		{
			m_selectionTrackingCounter++;
			if (m_selectionTrackingCounter == 1)
			{
				m_addedSelectedItems.Clear();
				m_removedSelectedItems.Clear();
			}
		}
	}

	internal void EndSelectionChanges()
	{
		if (!IsInSingleSelectionMode())
		{
			m_selectionTrackingCounter--;
			if (m_selectionTrackingCounter == 0 &&
				(m_addedSelectedItems.Count > 0 || m_removedSelectedItems.Count > 0))
			{
				var treeView = m_TreeView;

				var added = new List<object>();
				for (int i = 0; i < m_addedSelectedItems.Count; i++)
				{
					var weakReference = m_addedSelectedItems[i];
					if (weakReference.TryGetTarget(out var item))
					{
						added.Add(item);
					}
				}
				var removed = new List<object>();
				for (int i = 0; i < m_removedSelectedItems.Count; i++)
				{
					var item = m_removedSelectedItems[i];
					removed.Add(item);
				}
				treeView.RaiseSelectionChanged(added, removed);
			}
		}
	}

	private object GetAt(int index)
	{
		TreeViewNode node = GetNodeAt(index);
		return IsContentMode ? node.Content : node;
	}

#if false
	private object[] GetMany(int startIndex)
	{
		if (IsContentMode)
		{
			var vector = new List<object>();
			int size = Count;
			for (int i = 0; i < size; i++)
			{
				vector.Add(GetNodeAt(i).Content);
			}
			return vector.Skip(startIndex).ToArray();
		}
		var list = new List<object>();
		for (int i = startIndex; i < Count; i++)
		{
			list.Add(base[i]);
		}
		return list.ToArray();
	}
#endif

	internal TreeViewNode GetNodeAt(int index)
	{
		return (TreeViewNode)base[index];
	}

	private void SetAt(int index, object value)
	{
		var current = (TreeViewNode)base[index];
		base[index] = value;

		TreeViewNode newNode = (TreeViewNode)value;

		var tvnCurrent = current;
		tvnCurrent.ChildrenChanged -= TreeViewNodeVectorChanged;
		tvnCurrent.ExpandedChanged -= TreeViewNodePropertyChanged;

		// Hook up events and replace tokens
		var tvnNewNode = newNode;
		tvnNewNode.ChildrenChanged += TreeViewNodeVectorChanged;
		tvnNewNode.ExpandedChanged += TreeViewNodePropertyChanged;
	}


	public override object this[int index]
	{
		get => GetAt(index);
		set => SetAt(index, value);
	}

	internal void InsertAt(int index, object value)
	{
		base.Insert(index, value);
		TreeViewNode newNode = (TreeViewNode)value;

		// Hook up events and save tokens
		var tvnNewNode = newNode;
		tvnNewNode.ChildrenChanged += TreeViewNodeVectorChanged;
		tvnNewNode.ExpandedChanged += TreeViewNodePropertyChanged;
	}

	public override void Insert(int index, object item) => InsertAt(index, item);

	public override void RemoveAt(int index)
	{
		var current = (TreeViewNode)base[index];
		base.RemoveAt(index);

		// Unhook event handlers
		var tvnCurrent = (TreeViewNode)current;
		tvnCurrent.ChildrenChanged -= TreeViewNodeVectorChanged;
		tvnCurrent.ExpandedChanged -= TreeViewNodePropertyChanged;
	}

	private void Append(object value)
	{
		base.Add(value);
		TreeViewNode newNode = (TreeViewNode)value;

		// Hook up events and save tokens
		var tvnNewNode = newNode;
		tvnNewNode.ChildrenChanged += TreeViewNodeVectorChanged;
		tvnNewNode.ExpandedChanged += TreeViewNodePropertyChanged;
	}

	public override void Add(object item) => Append(item);

	private void RemoveAtEnd()
	{
		var current = (TreeViewNode)base[Count - 1];
		base.RemoveAt(base.Count - 1);

		// unhook events
		var tvnCurrent = current;
		tvnCurrent.ChildrenChanged -= TreeViewNodeVectorChanged;
		tvnCurrent.ExpandedChanged -= TreeViewNodePropertyChanged;
	}

	public override void Clear()
	{
		// Don't call GetVectorInnerImpl().Clear() directly because we need to remove hooked events
		int count = Count;
		while (count != 0)
		{
			RemoveAtEnd();
			count--;
		}
	}

#if false
	private void ReplaceAll(object[] items)
	{
		base.Clear();
		foreach (var item in items)
		{
			base.Add(item);
		}
	}
#endif

	// Helper function
	internal void PrepareView(TreeViewNode originNode)
	{
		// Remove any existing RootNode events/children
		var existingOriginNode = m_originNode;
		if (existingOriginNode != null)
		{
			for (int i = (existingOriginNode.Children.Count - 1); i >= 0; i--)
			{
				var removeNode = existingOriginNode.Children[i];
				RemoveNodeAndDescendantsFromView(removeNode);
			}

			existingOriginNode.ChildrenChanged -= TreeViewNodeVectorChanged;
		}

		// Add new RootNode & children
		m_originNode = originNode;
		originNode.ChildrenChanged += TreeViewNodeVectorChanged;
		originNode.IsExpanded = true;

		int allOpenedDescendantsCount = 0;
		for (var i = 0; i < originNode.Children.Count; i++)
		{
			var addNode = originNode.Children[i];
			AddNodeToView(addNode, i + allOpenedDescendantsCount);
			allOpenedDescendantsCount = AddNodeDescendantsToView(addNode, i, allOpenedDescendantsCount);
		}
	}

	internal void SetOwners(TreeViewList owningList, TreeView owningTreeView)
	{
		m_TreeViewList = owningList;
		m_TreeView = owningTreeView;
	}

	internal TreeViewList ListControl => m_TreeViewList;

	internal TreeView TreeView => m_TreeView;

	private bool IsInSingleSelectionMode()
	{
		return m_TreeViewList.SelectionMode == ListViewSelectionMode.Single;
	}

	// Private helpers
	private void AddNodeToView(TreeViewNode value, int index)
	{
		Insert(index, value);
	}

	private int AddNodeDescendantsToView(TreeViewNode value, int index, int offset)
	{
		if (value.IsExpanded)
		{
			int size = value.Children.Count;
			for (var i = 0; i < size; i++)
			{
				var childNode = value.Children[i];
				offset++;
				AddNodeToView(childNode, offset + index);
				offset = AddNodeDescendantsToView(childNode, index, offset);
			}

			return offset;
		}

		return offset;
	}

	private void RemoveNodeAndDescendantsFromView(TreeViewNode value)
	{
		if (value.IsExpanded)
		{
			int size = value.Children.Count;
			for (int i = 0; i < size; i++)
			{
				var childNode = value.Children[i];
				RemoveNodeAndDescendantsFromView(childNode);
			}
		}

		bool containsValue = IndexOfNode(value, out var valueIndex);
		if (containsValue)
		{
			RemoveAt(valueIndex);
		}
	}

	private void RemoveNodesAndDescendentsWithFlatIndexRange(int lowIndex, int highIndex)
	{
		if (lowIndex > highIndex)
		{
			throw new ArgumentOutOfRangeException(nameof(lowIndex));
		}

		for (int i = highIndex; i >= lowIndex; i--)
		{
			RemoveNodeAndDescendantsFromView(GetNodeAt(i));
		}
	}

	private int GetNextIndexInFlatTree(TreeViewNode node)
	{
		bool isNodeInFlatList = IndexOfNode(node, out var index);

		if (isNodeInFlatList)
		{
			index++;
		}
		else
		{
			// node is Root node, so next index in flat tree is 0
			index = 0;
		}
		return index;
	}

	// When ViewModel receives a event, it only includes the sender(parent TreeViewNode) and index.
	// We can't use sender[index] directly because it is already updated/removed
	// To find the removed TreeViewNode:
	//   calculate allOpenedDescendantsCount in sender[0..index-1] first
	//   then add offset and finally return TreeViewNode by looking up the flat tree.
	private TreeViewNode GetRemovedChildTreeViewNodeByIndex(TreeViewNode node, int childIndex)
	{
		var allOpenedDescendantsCount = 0;
		for (var i = 0; i < childIndex; i++)
		{
			TreeViewNode calcNode = node.Children[i];
			if (calcNode.IsExpanded)
			{
				allOpenedDescendantsCount += GetExpandedDescendantCount(calcNode);
			}
		}

		var childIndexInFlatTree = GetNextIndexInFlatTree(node) + childIndex + allOpenedDescendantsCount;
		return GetNodeAt(childIndexInFlatTree);
	}

	private int CountDescendants(TreeViewNode value)
	{
		int descendantCount = 0;
		var size = value.Children.Count;
		for (var i = 0; i < size; i++)
		{
			var childNode = value.Children[i];
			descendantCount++;
			if (childNode.IsExpanded)
			{
				descendantCount = descendantCount + CountDescendants(childNode);
			}
		}

		return descendantCount;
	}

	private int IndexOfNextSibling(TreeViewNode childNode)
	{
		var child = childNode;
		var parentNode = child.Parent;
		int stopIndex;
		bool isLastRelativeChild = true;
		while (parentNode != null && isLastRelativeChild)
		{
			int relativeIndex = parentNode.Children.IndexOf(child);
			if (parentNode.Children.Count - 1 != relativeIndex)
			{
				isLastRelativeChild = false;
			}
			else
			{
				child = parentNode;
				parentNode = parentNode.Parent;
			}
		}

		if (parentNode != null)
		{
			int siblingIndex = parentNode.Children.IndexOf(child);
			var siblingNode = parentNode.Children[siblingIndex + 1];
			IndexOfNode(siblingNode, out stopIndex);
		}
		else
		{
			stopIndex = Count;
		}

		return stopIndex;
	}

	private int GetExpandedDescendantCount(TreeViewNode parentNode)
	{
		var allOpenedDescendantsCount = 0;
		for (var i = 0; i < parentNode.Children.Count; i++)
		{
			var childNode = parentNode.Children[i];
			allOpenedDescendantsCount++;
			if (childNode.IsExpanded)
			{
				allOpenedDescendantsCount += CountDescendants(childNode);
			}
		}
		return allOpenedDescendantsCount;
	}

	internal bool IsNodeSelected(TreeViewNode targetNode)
	{
		return m_selectedNodes.IndexOf(targetNode) > -1;
	}

	private TreeNodeSelectionState NodeSelectionState(TreeViewNode targetNode)
	{
		return targetNode.SelectionState;
	}

	private void UpdateNodeSelection(TreeViewNode selectNode, TreeNodeSelectionState selectionState)
	{
		var node = selectNode;
		if (selectionState != node.SelectionState)
		{
			node.SelectionState = selectionState;
			var selectedNodes = m_selectedNodes;
			switch (selectionState)
			{
				case TreeNodeSelectionState.Selected:
					selectedNodes.InsertAtCore(selectedNodes.Count, selectNode);
					selectNode.ChildrenChanged += SelectedNodeChildrenChanged;
					break;

				case TreeNodeSelectionState.PartialSelected:
				case TreeNodeSelectionState.UnSelected:
					int index = selectedNodes.IndexOf(selectNode);
					if (index > -1)
					{
						selectedNodes.RemoveAtCore(index);
						selectNode.ChildrenChanged -= SelectedNodeChildrenChanged;
					}
					break;
			}
		}
	}

	internal void UpdateSelection(TreeViewNode selectNode, TreeNodeSelectionState selectionState)
	{
		if (NodeSelectionState(selectNode) != selectionState)
		{
			UpdateNodeSelection(selectNode, selectionState);

			if (!IsInSingleSelectionMode())
			{
				UpdateSelectionStateOfDescendants(selectNode, selectionState);
				UpdateSelectionStateOfAncestors(selectNode);
			}
		}
	}

	private void UpdateSelectionStateOfDescendants(TreeViewNode targetNode, TreeNodeSelectionState selectionState)
	{
		if (selectionState == TreeNodeSelectionState.PartialSelected) { return; }

		foreach (var childNode in targetNode.Children)
		{
			UpdateNodeSelection(childNode, selectionState);
			UpdateSelectionStateOfDescendants(childNode, selectionState);
			NotifyContainerOfSelectionChange(childNode, selectionState);
		}
	}

	private void UpdateSelectionStateOfAncestors(TreeViewNode targetNode)
	{
		var parentNode = targetNode.Parent;
		if (parentNode != null)
		{
			// no need to update m_originalNode since it's the logical root for TreeView and not accessible to users
			if (parentNode != m_originNode)
			{
				var previousState = NodeSelectionState(parentNode);
				var selectionState = SelectionStateBasedOnChildren(parentNode);

				if (previousState != selectionState)
				{
					UpdateNodeSelection(parentNode, selectionState);
					NotifyContainerOfSelectionChange(parentNode, selectionState);
					UpdateSelectionStateOfAncestors(parentNode);
				}
			}
		}
	}

	private TreeNodeSelectionState SelectionStateBasedOnChildren(TreeViewNode node)
	{
		bool hasSelectedChildren = false;
		bool hasUnSelectedChildren = false;

		foreach (var childNode in node.Children)
		{
			var state = NodeSelectionState(childNode);
			if (state == TreeNodeSelectionState.Selected)
			{
				hasSelectedChildren = true;
			}
			else if (state == TreeNodeSelectionState.UnSelected)
			{
				hasUnSelectedChildren = true;
			}

			if ((hasSelectedChildren && hasUnSelectedChildren) ||
				state == TreeNodeSelectionState.PartialSelected)
			{
				return TreeNodeSelectionState.PartialSelected;
			}
		}

		return hasSelectedChildren ? TreeNodeSelectionState.Selected : TreeNodeSelectionState.UnSelected;
	}

	internal void NotifyContainerOfSelectionChange(TreeViewNode targetNode, TreeNodeSelectionState selectionState)
	{
		if (m_TreeViewList != null)
		{
			var container = m_TreeViewList.ContainerFromNode(targetNode);
			if (container != null)
			{
				var targetItem = (TreeViewItem)container;
				targetItem.UpdateSelectionVisual(selectionState);
			}
			else
			{
				// Uno specific: Workaround for TreeViewSelectRegressionTest (seems to be broken in WinUI too).
				if (!m_TreeViewList.IsMultiselect)
				{
					m_TreeViewList.SelectedItem = IsContentMode ? targetNode?.Content : targetNode;
				}
			}
		}
	}

	internal IList<TreeViewNode> SelectedNodes => m_selectedNodes;

	internal IList<object> SelectedItems => m_selectedItems;

	internal void TrackItemSelected(object item)
	{
		if (m_selectionTrackingCounter > 0 && item != m_originNode)
		{
			m_addedSelectedItems.Add(new WeakReference<object>(item));
		}
	}

	internal void TrackItemUnselected(object item)
	{
		if (m_selectionTrackingCounter > 0 && item != m_originNode)
		{
			m_removedSelectedItems.Add(item);
		}
	}

	internal TreeViewNode GetAssociatedNode(object item)
	{
		if (m_itemToNodeMap.TryGetValue(item, out var result))
		{
			return result;
		}

		return null;
	}

	internal bool IndexOfNode(TreeViewNode targetNode, out int index)
	{
		index = base.IndexOf(targetNode);
		return index > -1;
	}

	private void TreeViewNodeVectorChanged(TreeViewNode sender, object args)
	{
		var vectorArgs = (IVectorChangedEventArgs)args;
		CollectionChange collectionChange = vectorArgs.CollectionChange;
		int index = (int)vectorArgs.Index;

		switch (collectionChange)
		{
			// Reset case, commonly seen when a TreeNode is cleared.
			// removes all nodes that need removing then 
			// toggles a collapse / expand to ensure order.
			case (CollectionChange.Reset):
				{
					var resetNode = sender;
					if (resetNode.IsExpanded)
					{
						//The lowIndex is the index of the first child, while the high index is the index of the last descendant in the list.
						int lowIndex = GetNextIndexInFlatTree(resetNode);
						int highIndex = IndexOfNextSibling(resetNode) - 1;
						RemoveNodesAndDescendentsWithFlatIndexRange(lowIndex, highIndex);

						// reset the status of resetNodes children
						CollapseNode(resetNode);
						ExpandNode(resetNode);
					}

					break;
				}

			// We will find the correct index of insertion by first checking if the
			// node we are inserting into is expanded. If it is we will start walking
			// down the tree and counting the open items. This is to ensure we place
			// the inserted item in the correct index. If along the way we bump into
			// the item being inserted, we insert there then return, because we don't
			// need to do anything further.
			case (CollectionChange.ItemInserted):
				{
					var targetNode = sender.Children[index];

					if (IsContentMode)
					{
						m_itemToNodeMap[targetNode.Content] = targetNode;
					}

					var parentNode = targetNode.Parent;
					int nextNodeIndex = GetNextIndexInFlatTree(parentNode);
					int allOpenedDescendantsCount = 0;

					if (parentNode.IsExpanded)
					{
						for (int i = 0; i < parentNode.Children.Count; i++)
						{
							var childNode = parentNode.Children[i];
							if (childNode == targetNode)
							{
								AddNodeToView(targetNode, nextNodeIndex + i + allOpenedDescendantsCount);
								if (targetNode.IsExpanded)
								{
									AddNodeDescendantsToView(targetNode, nextNodeIndex + i, allOpenedDescendantsCount);
								}
							}
							else if (childNode.IsExpanded)
							{
								allOpenedDescendantsCount += CountDescendants(childNode);
							}
						}
					}

					break;
				}

			// Removes a node from the ViewModel when a TreeNode
			// removes a child.
			case (CollectionChange.ItemRemoved):
				{
					var removingNodeParent = sender;
					if (removingNodeParent.IsExpanded)
					{
						var removedNode = GetRemovedChildTreeViewNodeByIndex(removingNodeParent, index);
						RemoveNodeAndDescendantsFromView(removedNode);
						if (IsContentMode)
						{
							m_itemToNodeMap.Remove(removedNode.Content);
						}
					}

					break;
				}

			// Triggered by a replace such as SetAt.
			// Updates the TreeNode that changed in the ViewModel.
			case (CollectionChange.ItemChanged):
				{
					var targetNode = sender.Children[index];
					var changingNodeParent = sender;
					if (changingNodeParent.IsExpanded)
					{
						var removedNode = GetRemovedChildTreeViewNodeByIndex(changingNodeParent, index);

						if (!IndexOfNode(removedNode, out var removedNodeIndex))
						{
							throw new InvalidOperationException("Node does not exist");
						}

						RemoveNodeAndDescendantsFromView(removedNode);
						Insert(removedNodeIndex, targetNode);

						if (IsContentMode)
						{
							m_itemToNodeMap.Remove(removedNode.Content);
							m_itemToNodeMap.Add(targetNode.Content, targetNode);
						}
					}

					break;
				}
		}
	}

	private void SelectedNodeChildrenChanged(TreeViewNode sender, object args)
	{
		var vectorChangedArgs = (IVectorChangedEventArgs)args;
		CollectionChange collectionChange = vectorChangedArgs.CollectionChange;
		var index = (int)vectorChangedArgs.Index; // Cast to int to be able to index
		var changingChildrenNode = sender;

		switch (collectionChange)
		{
			case (CollectionChange.ItemInserted):
				{
					var newNode = changingChildrenNode.Children[index];

					// If we are in multi select, we want the new child items to be also selected.
					if (!IsInSingleSelectionMode())
					{
						UpdateNodeSelection(newNode, NodeSelectionState(changingChildrenNode));
					}
					break;
				}

			case (CollectionChange.ItemChanged):
				{
					var newNode = changingChildrenNode.Children[index];
					UpdateNodeSelection(newNode, NodeSelectionState(changingChildrenNode));

					var selectedNodes = m_selectedNodes;
					for (var i = 0; i < selectedNodes.Count; i++)
					{
						var selectNode = selectedNodes[i];
						var ancestorNode = selectNode.Parent;
						while (ancestorNode != null && ancestorNode.Parent != null)
						{
							ancestorNode = ancestorNode.Parent;
						}

						if (ancestorNode != m_originNode)
						{
							selectedNodes.RemoveAtCore(i);
							selectNode.ChildrenChanged -= SelectedNodeChildrenChanged;
						}
					}
					break;
				}

			case (CollectionChange.ItemRemoved):
			case (CollectionChange.Reset):
				{
					//This checks if there are still children, then re-evaluates parents selection based on current state of remaining children
					//If a node has 2 children selected, and 1 unselected, and the unselected is removed, we then change the parent node to selected.
					//If the last child is removed, we preserve the current selection state of the parent, and this code need not execute.
					if (changingChildrenNode.Children.Count > 0)
					{
						var firstChildNode = changingChildrenNode.Children[0];
						UpdateSelectionStateOfAncestors(firstChildNode);
					}

					var selectedNodes = m_selectedNodes;
					for (int i = 0; i < selectedNodes.Count; i++)
					{
						var selectNode = selectedNodes[i];
						var ancestorNode = selectNode.Parent;
						while (ancestorNode != null && ancestorNode.Parent != null)
						{
							ancestorNode = ancestorNode.Parent;
						}

						if (ancestorNode != m_originNode)
						{
							selectedNodes.RemoveAtCore(i);
							selectNode.ChildrenChanged -= SelectedNodeChildrenChanged;

							// We've removed the node at position i, so we should keep i at the same position
							// because the new node at position i is a different one.
							i--;
						}
					}
					break;
				}
		}
	}

	private void TreeViewNodePropertyChanged(TreeViewNode sender, DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		if (property == TreeViewNode.IsExpandedProperty)
		{
			TreeViewNodeIsExpandedPropertyChanged(sender, args);
		}
		else if (property == TreeViewNode.HasChildrenProperty)
		{
			TreeViewNodeHasChildrenPropertyChanged(sender, args);
		}
	}

	private void TreeViewNodeIsExpandedPropertyChanged(TreeViewNode sender, DependencyPropertyChangedEventArgs args)
	{
		var targetNode = sender;
		if (targetNode.IsExpanded)
		{
			if (targetNode.Children.Count != 0)
			{
				int openedDescendantOffset = 0;
				IndexOfNode(targetNode, out var index);
				index = index + 1;
				for (var i = 0; i < targetNode.Children.Count; i++)
				{
					TreeViewNode childNode = targetNode.Children[i];
					AddNodeToView(childNode, index + i + openedDescendantOffset);
					openedDescendantOffset = AddNodeDescendantsToView(childNode, index + i, openedDescendantOffset);
				}
			}

			//Notify TreeView that a node is being expanded.
			NodeExpanding?.Invoke(targetNode, null);
		}
		else
		{
			for (var i = 0; i < targetNode.Children.Count; i++)
			{
				TreeViewNode childNode = targetNode.Children[i];
				RemoveNodeAndDescendantsFromView(childNode);
			}

			//Notife TreeView that a node is being collapsed
			NodeCollapsed?.Invoke(targetNode, null);
		}
	}

	private void TreeViewNodeHasChildrenPropertyChanged(TreeViewNode sender, DependencyPropertyChangedEventArgs args)
	{
		if (m_TreeViewList != null)
		{
			var targetNode = sender;
			var container = m_TreeViewList.ContainerFromNode(targetNode);
			if (container != null)
			{
				TreeViewItem targetItem = (TreeViewItem)container;
				targetItem.GlyphOpacity = targetNode.HasChildren ? 1.0 : 0.0;
			}
		}
	}

	public bool IsContentMode
	{
		get => m_isContentMode;
		set => m_isContentMode = value;
	}

	private void ClearEventTokenVectors()
	{
		// Remove ChildrenChanged and ExpandedChanged events			
		for (var i = 0; i < Count; i++)
		{
			if (i < base.Count)
			{
				var current = base[i];
				var tvnCurrent = (TreeViewNode)current;
				tvnCurrent.ChildrenChanged -= TreeViewNodeVectorChanged;
				tvnCurrent.ExpandedChanged -= TreeViewNodePropertyChanged;
			}
		}

		// Remove SelectedNodeChildrenChangedEvent
		var selectedNodes = m_selectedNodes;
		if (selectedNodes != null)
		{
			for (var i = 0; i < selectedNodes.Count; i++)
			{
				var current = selectedNodes[i];
				if (current != null)
				{
					var node = (TreeViewNode)current;
					node.ChildrenChanged -= SelectedNodeChildrenChanged;
				}
			}
		}
	}

	#region Uno specific

	/// <summary>
	/// The custom enumerator is necessary to ensure that when IsContentMode
	/// is set to true, the IndexFromItem method works properly (it must enumerate
	/// actual items, not TreeViewNodes).
	/// </summary>
	/// <returns></returns>
	public override IEnumerator<object> GetEnumerator()
	{
		return new TreeViewViewModelEnumerator(this);
	}

	private class TreeViewViewModelEnumerator : IEnumerator<object>
	{
		private readonly TreeViewViewModel _treeViewViewModel;
		private int _currentIndex = -1;

		public TreeViewViewModelEnumerator(TreeViewViewModel treeViewViewModel)
		{
			_treeViewViewModel = treeViewViewModel;
		}

		public object Current => _treeViewViewModel[_currentIndex];

		public bool MoveNext()
		{
			_currentIndex++;
			return _currentIndex < _treeViewViewModel.Count;
		}

		public void Reset()
		{
			_currentIndex = -1;
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}

	#endregion
}
