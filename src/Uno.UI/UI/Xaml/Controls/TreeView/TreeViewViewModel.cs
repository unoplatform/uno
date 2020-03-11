using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using TreeNodeSelectionState = Windows.UI.Xaml.Controls.TreeViewNode.TreeNodeSelectionState;

namespace Windows.UI.Xaml.Controls
{
	internal partial class TreeViewViewModel : ObservableVector<object>
	{
		private bool m_isContentMode;
		private SelectedTreeNodeVector m_selectedNodes;
		private SelectedItemsVector m_selectedItems;
		private Dictionary<object, TreeViewNode> m_itemToNodeMap;

		private TreeViewNode m_originNode;
		private TreeViewList m_TreeViewList;

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

		//ViewModel.~ViewModel()
		//{
		//	if (m_rootNodeChildrenChangedEventToken.value != 0)
		//	{
		//		if (var origin = m_originNode.safe_get())
		//        {
		//			get_self<TreeViewNode>(origin).ChildrenChanged(m_rootNodeChildrenChangedEventToken);
		//		}

		//		ClearEventTokenVectors();
		//	}
		//}

		internal void ExpandNode(TreeViewNode value)
		{
			value.IsExpanded = true;
		}

		internal void CollapseNode(TreeViewNode value)
		{
			value.IsExpanded = false;
		}

		internal event TypedEventHandler<TreeViewNode, object> NodeExpanding;

		internal event TypedEventHandler<TreeViewNode, object> NodeCollapsed;

		internal void SelectAll()
		{
			UpdateSelection(m_originNode, TreeNodeSelectionState.Selected);
		}

		internal void ModifySelectByIndex(int index, TreeNodeSelectionState state)
		{
			var targetNode = GetNodeAt(index);
			UpdateSelection(targetNode, state);
		}

		internal int Size
		{
			get
			{
				var inner = GetVectorInnerImpl();
				return inner.Count;
			}
		}

		private object GetAt(int index)
		{
			TreeViewNode node = GetNodeAt(index);
			return IsContentMode ? node.Content : node;
		}

		private new int IndexOf(object value)
		{
			var inner = GetVectorInnerImpl();
			return inner.IndexOf(value);
		}

		private object[] GetMany(int startIndex)
		{
			var inner = GetVectorInnerImpl();
			if (IsContentMode)
			{
				var vector = new List<object>();
				int size = Size;
				for (int i = 0; i < size; i++)
				{
					vector.Append(GetNodeAt(i).Content);
				}
				return vector.Skip(startIndex).ToArray();
			}
			return inner.Skip(startIndex).ToArray();
		}

		internal TreeViewNode GetNodeAt(int index)
		{
			var inner = GetVectorInnerImpl();
			return (TreeViewNode)inner[index];
		}

		//void ViewModel.SetAt(uint32_t index, IInspectable const& value)
		//{
		//	var inner = GetVectorInnerImpl();
		//	var current = inner.GetAt(index).as< TreeViewNode > ();
		//	inner.SetAt(index, value);

		//	TreeViewNode newNode = value.as< TreeViewNode > ();

		//	var tvnCurrent = get_self<TreeViewNode>(current);
		//	tvnCurrent.ChildrenChanged(m_collectionChangedEventTokenVector[index]);
		//	tvnCurrent.RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector[index]);

		//	// Hook up events and replace tokens
		//	var tvnNewNode = get_self<TreeViewNode>(newNode);
		//	m_collectionChangedEventTokenVector[index] = tvnNewNode.ChildrenChanged({ this, &ViewModel.TreeViewNodeVectorChanged });
		//	m_IsExpandedChangedEventTokenVector[index] = tvnNewNode.AddExpandedChanged({ this, &ViewModel.TreeViewNodePropertyChanged });
		//}

		//void ViewModel.InsertAt(uint32_t index, IInspectable const& value)
		//{
		//	GetVectorInnerImpl().InsertAt(index, value);
		//	TreeViewNode newNode = value.as< TreeViewNode > ();

		//	//Hook up events and save tokens
		//	var tvnNewNode = get_self<TreeViewNode>(newNode);
		//	m_collectionChangedEventTokenVector.insert(m_collectionChangedEventTokenVector.begin() + index, tvnNewNode.ChildrenChanged({ this, &ViewModel.TreeViewNodeVectorChanged }));
		//	m_IsExpandedChangedEventTokenVector.insert(m_IsExpandedChangedEventTokenVector.begin() + index, tvnNewNode.AddExpandedChanged({ this, &ViewModel.TreeViewNodePropertyChanged }));
		//}

		//void ViewModel.RemoveAt(uint32_t index)
		//{
		//	var inner = GetVectorInnerImpl();
		//	var current = inner.GetAt(index).as< TreeViewNode > ();
		//	inner.RemoveAt(index);

		//	// Unhook event handlers
		//	var tvnCurrent = get_self<TreeViewNode>(current);
		//	tvnCurrent.ChildrenChanged(m_collectionChangedEventTokenVector[index]);
		//	tvnCurrent.RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector[index]);

		//	// Remove tokens from vectors
		//	m_collectionChangedEventTokenVector.erase(m_collectionChangedEventTokenVector.begin() + index);
		//	m_IsExpandedChangedEventTokenVector.erase(m_IsExpandedChangedEventTokenVector.begin() + index);
		//}

		private void Append(object value)
		{
			GetVectorInnerImpl().Add(value);
			TreeViewNode newNode = (TreeViewNode)value;

			// Hook up events and save tokens
			var tvnNewNode = newNode;
			m_collectionChangedEventTokenVector.push_back(tvnNewNode.ChildrenChanged({ this, &ViewModel.TreeViewNodeVectorChanged }));
			m_IsExpandedChangedEventTokenVector.push_back(tvnNewNode.AddExpandedChanged({ this, &ViewModel.TreeViewNodePropertyChanged }));
		}

		private void RemoveAtEnd()
		{
			var inner = GetVectorInnerImpl();
			var current = (TreeViewNode)inner[Count - 1];
			inner.RemoveAt(inner.Count - 1);

			// unhook events
			var tvnCurrent = current;
			tvnCurrent.ChildrenChanged(m_collectionChangedEventTokenVector.back());
			tvnCurrent.RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector.back());

			// remove tokens
			m_collectionChangedEventTokenVector.pop_back();
			m_IsExpandedChangedEventTokenVector.pop_back();
		}

		private new void Clear()
		{
			// Don't call GetVectorInnerImpl().Clear() directly because we need to remove hooked events
			int count = Size;
			while (count != 0)
			{
				RemoveAtEnd();
				count--;
			}
		}

		private void ReplaceAll(object[] items)
		{
			var inner = GetVectorInnerImpl();
			inner.Clear();
			inner.AddRange(items);
		}

		//// Helper function
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

				if (m_rootNodeChildrenChangedEventToken.value != 0)
				{
					var childrenVector = (ObservableVector<TreeViewNode>)existingOriginNode.Children;
					childrenVector.VectorChanged(m_rootNodeChildrenChangedEventToken);
				}
			}

			//	// Add new RootNode & children
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

		internal void SetOwningList(TreeViewList owningList)
		{
			m_TreeViewList = owningList;
		}

		private TreeViewList ListControl => m_TreeViewList;

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
					var childNode = (TreeViewNode)value.Children[i];
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
		//   calcuate allOpenedDescendantsCount in sender[0..index-1] first
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
			var parentNode = childNode.Parent;
			int stopIndex;
			bool isLastRelativeChild = true;
			while (parentNode != null && isLastRelativeChild)
			{
				int relativeIndex = parentNode.Children.IndexOf(childNode);
				if (parentNode.Children.Count - 1 != relativeIndex)
				{
					isLastRelativeChild = false;
				}
				else
				{
					childNode = parentNode;
					parentNode = parentNode.Parent;
				}
			}

			if (parentNode != null)
			{
				int siblingIndex = parentNode.Children.IndexOf(childNode);
				var siblingNode = parentNode.Children[siblingIndex + 1];
				IndexOfNode(siblingNode, out stopIndex);
			}
			else
			{
				stopIndex = Size;
			}

			return stopIndex;
		}

		private int GetExpandedDescendantCount(TreeViewNode parentNode)
		{
			var allOpenedDescendantsCount = 0;
			for (var i = 0; i < parentNode.Children.Count; i++)
			{
				var childNode = (TreeViewNode)parentNode.Children[i];
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
			return m_selectedNodes.IndexOf(targetNode) != -1;
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
						m_selectedNodeChildrenChangedEventTokenVector.push_back(get_self<TreeViewNode>(selectNode).ChildrenChanged({ this, &ViewModel.SelectedNodeChildrenChanged }));
						break;

					case TreeNodeSelectionState.PartialSelected:
					case TreeNodeSelectionState.UnSelected:
						int index = selectedNodes.IndexOf(selectNode);
						if (index > -1)
						{
							selectedNodes.RemoveAtCore(index);
							selectNode.ChildrenChanged(m_selectedNodeChildrenChangedEventTokenVector[index]);
							m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + index);
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
			if (selectionState == TreeNodeSelectionState.PartialSelected) return;

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
			throw new NotImplementedException();
		}

		private void NotifyContainerOfSelectionChange(TreeViewNode targetNode, TreeNodeSelectionState selectionState)
		{
			if (m_TreeViewList != null)
			{
				var container = m_TreeViewList.ContainerFromNode(targetNode);
				if (container != null)
				{
					var targetItem = (TreeViewItem)container;
					targetItem.UpdateSelectionVisual(selectionState);
				}
			}
		}

		internal IList<TreeViewNode> SelectedNodes => m_selectedNodes;

		internal IList<object> SelectedItems => m_selectedItems;

		internal TreeViewNode GetAssociatedNode(object item)
		{
			return m_itemToNodeMap[item]; //TODO: Throw or null?
		}

		internal bool IndexOfNode(TreeViewNode targetNode, out int index)
		{
			index = GetVectorInnerImpl().IndexOf(targetNode);
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
						var resetNode = (TreeViewNode)sender;
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
							m_itemToNodeMap.Add(targetNode.Content, targetNode);
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
			var changingChildrenNode = (TreeViewNode)sender;

			switch (collectionChange)
			{
				case (CollectionChange.ItemInserted):
					{
						var newNode = changingChildrenNode.Children[index];
						UpdateNodeSelection(newNode, NodeSelectionState(changingChildrenNode));
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
								m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + i);
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
								m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + i);
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
			var targetNode = (TreeViewNode)sender;
			if (targetNode.IsExpanded)
			{
				if (targetNode.Children.Count != 0)
				{
					int openedDescendantOffset = 0;
					IndexOfNode(targetNode, out var index);
					index = index + 1;
					for (var i = 0; i < targetNode.Children.Count; i++)
					{
						TreeViewNode childNode = null;
						childNode = (TreeViewNode)targetNode.Children[i];
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
					TreeViewNode childNode = null;
					childNode = (TreeViewNode)targetNode.Children[i];
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

		public bool IsContentMode { get => m_isContentMode; set => m_isContentMode = value; }

		private void ClearEventTokenVectors()
		{
			// Remove ChildrenChanged and ExpandedChanged events
			var inner = GetVectorInnerImpl();
			for (var i = 0; i < Size; i++)
			{
				if (i < inner.Count)
				{
					var current = inner[i];
					var tvnCurrent = (TreeViewNode)current;
					tvnCurrent.ChildrenChanged(m_collectionChangedEventTokenVector.at(i));
					tvnCurrent.RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector.at(i));
				}
			}

			// Remove SelectedNodeChildrenChangtedEvent
			var selectedNodes = m_selectedNodes;
			if (selectedNodes != null)
			{
				for (var i = 0; i < selectedNodes.Count; i++)
				{
					var current = selectedNodes[i];
					if (current != null)
					{
						var node = (TreeViewNode)current;
						node.ChildrenChanged(m_selectedNodeChildrenChangedEventTokenVector[i]);
					}
				}
			}

			// Clear token vectors
			m_collectionChangedEventTokenVector.clear();
			m_IsExpandedChangedEventTokenVector.clear();
			m_selectedNodeChildrenChangedEventTokenVector.clear();
		}

	}
}
