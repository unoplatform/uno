using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewList : ListView
	{
		private bool m_isMultiselectEnabled;
		private string m_dropTargetDropEffectString;
		private UIElement m_draggedOverItem;
		private int m_emptySlotIndex;
		private TreeViewNode m_draggedTreeViewNode;

		public TreeViewList()
		{
			ListViewModel = new TreeViewViewModel();

			//		DragItemsStarting({ this, &TreeViewList::OnDragItemsStarting });
			//    DragItemsCompleted({ this, &TreeViewList::OnDragItemsCompleted });
			//    ContainerContentChanging({ this, &TreeViewList::OnContainerContentChanging });
		}

		internal TreeViewViewModel ListViewModel { get; private set; }

		private TreeViewNode DraggedTreeViewNode { get; set; }

		private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
		{
			var dragItem = args.Items[0];
			if (dragItem)
			{
				var tvItem = (TreeViewItem)ContainerFromItem(dragItem);
				m_draggedTreeViewNode = NodeFromContainer(tvItem);
				bool isMultipleDragging = false;
				if (IsMutiSelectWithSelectedItems())
				{
					int selectedCount = ListViewModel.SelectedNodes.Count;
					if (selectedCount > 1)
					{
						var settings = tvItem.TreeViewItemTemplateSettings;
						settings.DragItemsCount = selectedCount;
						isMultipleDragging = true;

						// TreeView items' selection states are maintained by ViewModel, not by TreeViewList.
						// When TreeView is set to multi-select mode, the underlying TreeViewList's selection mode is set to None (See OnPropertyChanged function in TreeView.cpp).
						// TreeViewList has no knowledge about item selections happened in TreeView, no matter how many items are actually selected, args.Items() always contains only one item that is currently being dragged by cursor.
						// Here, we manually add selected nodes to args.Items in order to expose all selected (and dragged) items to outside handlers.
						args.Items.Clear();

						foreach (var node in ListViewModel.SelectedNodes)
						{
							if (IsContentMode)
							{
								args.Items.Append(node.Content);
							}
							else
							{
								args.Items.Append(node);
							}
						}
					}
				}
				else
				{
					// If not in multi-select mode, collapse the dragged node.
					m_draggedTreeViewNode.IsExpanded = false;
				}

				VisualStateManager.GoToState(
					tvItem,
					isMultipleDragging ? "MultipleDraggingPrimary" : "Dragging",
					false);
				UpdateDropTargetDropEffect(false, false, null);
			}
		}

		private void OnDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
		{
			m_draggedTreeViewNode = null;
			m_emptySlotIndex = -1;
		}

		private void OnContainerContentChanging(object sender, ContainerContentChangingEventArgs args)
		{
			//	// Only need to update unrecycled on screen items.
			if (!args.InRecycleQueue)
			{
				var targetItem = (TreeViewItem)args.ItemContainer;
				var targetNode = NodeFromContainer(targetItem);
				var treeViewItem = targetItem;
				treeViewItem.UpdateIndentation(targetNode.Depth);
				treeViewItem.UpdateSelectionVisual(targetNode.SelectionState);
			}
		}

		//// IControlOverrides
		protected override void OnDrop(DragEventArgs e)
		{
			//	winrt::DragEventArgs args = e;

			//	if (args.AcceptedOperation() == winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move && !args.Handled())
			//	{
			//		if (m_draggedTreeViewNode && IsIndexValid(m_emptySlotIndex))
			//		{
			//			// Get the node at which we will insert
			//			winrt::TreeViewNode insertAtNode = NodeAtFlatIndex(m_emptySlotIndex);

			//			if (IsMutiSelectWithSelectedItems())
			//			{
			//				// Multiselect drag and drop. In the selected items, find all the selected subtrees 
			//				// and move each of those subtrees.
			//				auto selectedRootNodes = GetRootsOfSelectedSubtrees();
			//				auto selectionSize = selectedRootNodes.size();

			//				// Loop through in reverse order because we are inserting above the previous item to get the order correct.
			//				for (int i = static_cast<int>(selectedRootNodes.size()) - 1; i >= 0; --i)
			//				{
			//					MoveNodeInto(selectedRootNodes[i], insertAtNode);
			//				}
			//			}
			//			else
			//			{
			//				MoveNodeInto(m_draggedTreeViewNode.get(), insertAtNode);
			//			}

			//			UpdateDropTargetDropEffect(false, false, nullptr);
			//			args.Handled(true);
			//		}
			//	}

			base.OnDrop(e);
		}

		private void MoveNodeInto(TreeViewNode node, TreeViewNode insertAtNode)
		{
			//	int nodeFlatIndex = FlatIndex(node);
			//	if (insertAtNode != node && IsFlatIndexValid(nodeFlatIndex))
			//	{
			//		RemoveNodeFromParent(node);

			//		int insertOffset = nodeFlatIndex < m_emptySlotIndex ? 1 : 0;
			//		// if the insertAtNode is a parent that is expanded
			//		// insert as the first child
			//		if (insertAtNode.IsExpanded() && insertOffset == 1)
			//		{
			//			winrt::get_self<TreeViewNodeVector>(insertAtNode.Children())->InsertAt(0, node);
			//		}
			//		else
			//		{
			//			// Add the item to the new parent (parent of the insertAtNode)
			//			auto children = winrt::get_self<TreeViewNodeVector>(insertAtNode.Parent().Children());
			//			int insertNodeIndexInParent = IndexInParent(insertAtNode);
			//			children->InsertAt(insertNodeIndexInParent + insertOffset, node);
			//		}
			//	}
		}

		protected override void OnDragOver(DragEventArgs e)
		{
			//	if (!args.Handled())
			//	{
			//		args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::None);
			//		winrt::IInsertionPanel insertionPanel = ItemsPanelRoot().as< winrt::IInsertionPanel > ();

			//		// reorder is only supported with panels that implement IInsertionPanel
			//		if (insertionPanel && m_draggedTreeViewNode && CanReorderItems())
			//		{
			//			int aboveIndex = -1;
			//			int belowIndex = -1;
			//			auto itemsSource = ListViewModel();
			//			int size = itemsSource->Size();
			//			auto point = args.GetPosition(insertionPanel.as< winrt::UIElement > ());
			//			insertionPanel.GetInsertionIndexes(point, aboveIndex, belowIndex);

			//			// a value of -1 means we're inserting at the end of the list or before the last item
			//			if (belowIndex == -1)
			//			{
			//				// this allows the next part of this code to test if we're dropping before or after the last item
			//				belowIndex = static_cast<int>(size - 1);
			//			}

			//			// if we have an insertion point
			//			// a value of 0 means we're inserting at the beginning
			//			// a value greater than size - 1 means we're inserting at the end as items are collapsing
			//			if (belowIndex > static_cast<int>(size - 1))
			//			{
			//				// don't go out of bounds
			//				// since items might be collapsing as we're dragging
			//				m_emptySlotIndex = static_cast<int>(size - 1);
			//			}
			//			else if (belowIndex > 0 && m_draggedTreeViewNode)
			//			{
			//				m_emptySlotIndex = -1;
			//				unsigned int draggedIndex;
			//				winrt::TreeViewNode tvi = m_draggedTreeViewNode.get();
			//				if (ListViewModel()->IndexOfNode(tvi, draggedIndex))
			//				{
			//					const int indexToUse = (static_cast<int>(draggedIndex) < belowIndex) ? belowIndex : (belowIndex - 1);
			//					if (auto item = ContainerFromIndex(indexToUse))
			//                    {
			//						auto treeViewItem = item.as< winrt::TreeViewItem > ();
			//						auto pointInsideItem = args.GetPosition(treeViewItem.as< winrt::UIElement > ());

			//						// if the point is in the top half of the item
			//						// we need to insert before that item
			//						if (pointInsideItem.Y < treeViewItem.ActualHeight() / 2)
			//						{
			//							m_emptySlotIndex = belowIndex - 1;
			//						}
			//						else
			//						{
			//							m_emptySlotIndex = belowIndex;
			//						}
			//					}
			//				}
			//			}
			//			else
			//			{
			//				// top of the list
			//				m_emptySlotIndex = 0;
			//			}

			//			bool allowReorder = true;
			//			if (IsFlatIndexValid(m_emptySlotIndex))
			//			{
			//				auto insertAtNode = NodeAtFlatIndex(m_emptySlotIndex);
			//				if (IsMultiselect())
			//				{
			//					// If insertAtNode is in the selected items, then we do not want to allow a dropping.
			//					auto selectedItems = ListViewModel()->GetSelectedNodes();
			//					for (unsigned int i = 0; i < selectedItems.Size(); i++)
			//					{
			//						auto selectedNode = selectedItems.GetAt(i);
			//						if (selectedNode == insertAtNode)
			//						{
			//							allowReorder = false;
			//							break;
			//						}
			//					}
			//				}
			//				else
			//				{
			//					if (m_draggedTreeViewNode && insertAtNode && insertAtNode.Parent())
			//					{
			//						auto insertContainer = ContainerFromNode(insertAtNode.Parent());
			//						if (insertContainer)
			//						{
			//							allowReorder = insertContainer.as< winrt::TreeViewItem > ().AllowDrop();
			//						}
			//					}
			//				}
			//			}
			//			else
			//			{
			//				// m_emptySlotIndex does not exist in the ViewModel - don't allow the reorder.
			//				allowReorder = false;
			//			}

			//			if (allowReorder)
			//			{
			//				args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move);
			//			}
			//			else
			//			{
			//				m_emptySlotIndex = -1;
			//				args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::None);
			//				args.Handled(true);
			//			}
			//		}

			//		UpdateDropTargetDropEffect(false, false, nullptr);
			//	}
			base.OnDragOver(e);
		}

		protected override void OnDragEnter(DragEventArgs args)
		{
			if (!args.Handled)
			{
				UpdateDropTargetDropEffect(false, false, null);
			}
			base.OnDragEnter(args);
		}

		protected override void OnDragLeave(DragEventArgs args)
		{
			m_emptySlotIndex = -1;
			base.OnDragLeave(args);
			if (!args.Handled)
			{
				UpdateDropTargetDropEffect(false, true, null);
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			//	auto itemNode = winrt::get_self<TreeViewNode>(NodeFromContainer(element));
			//	winrt::TreeViewItem itemContainer = element.as< winrt::TreeViewItem > ();
			//	auto selectionState = itemNode->SelectionState();

			//	//Set the expanded property to match that of the Node, and enable Drop by default
			//	itemContainer.AllowDrop(true);

			//	if (IsContentMode())
			//	{
			//		bool hasChildren = itemContainer.HasUnrealizedChildren() || itemNode->HasChildren();
			//		itemContainer.GlyphOpacity(hasChildren ? 1.0 : 0.0);
			//		if (itemContainer.IsExpanded() != itemNode->IsExpanded())
			//		{
			//			const DispatcherHelper dispatcher{ *this };
			//			dispatcher.RunAsync(

			//				[itemNode, itemContainer]()

			//				{
			//				itemNode->IsExpanded(itemContainer.IsExpanded());
			//			});
			//		}
			//	}
			//	else
			//	{
			//		itemContainer.IsExpanded(itemNode->IsExpanded());
			//		itemContainer.GlyphOpacity(itemNode->HasChildren() ? 1.0 : 0.0);
			//	}

			//	//Set startup TemplateSettings properties
			//	auto templateSettings = winrt::get_self<TreeViewItemTemplateSettings>(itemContainer.TreeViewItemTemplateSettings());
			//	templateSettings->ExpandedGlyphVisibility(itemNode->IsExpanded() ? winrt::Visibility::Visible : winrt::Visibility::Collapsed);
			//	templateSettings->CollapsedGlyphVisibility(!itemNode->IsExpanded() ? winrt::Visibility::Visible : winrt::Visibility::Collapsed);

			//	__super::PrepareContainerForItemOverride(element, item);

			//	if (selectionState != itemNode->SelectionState())
			//	{
			//		ListViewModel()->UpdateSelection(*itemNode, selectionState);
			//	}
		}

		protected override DependencyObject GetContainerForItemOverride()
		{
			//	winrt::TreeViewItem targetItem = winrt::TreeViewItem();
			//	return targetItem;
			throw new NotImplementedException();
		}

		//// IFrameworkElementOverrides
		protected override void OnApplyTemplate()
		{
			//	if (!m_itemsSourceAttached)
			//	{
			//		ItemsSource(*ListViewModel());
			//		IsItemClickEnabled(true);
			//		m_itemsSourceAttached = true;
			//	}

			base.OnApplyTemplate();
		}

		//// IUIElementOverrides
		protected override AutomationPeer OnCreateAutomationPeer() =>
			new TreeViewListAutomationPeer(this);

		private string GetDropTargetDropEffect()
		{
			//if (m_dropTargetDropEffectString.empty())
			//{
			//	UpdateDropTargetDropEffect(true, false, nullptr);
			//}
			//return m_dropTargetDropEffectString;
			throw new NotImplementedException();
		}

		private void SetDraggedOverItem(TreeViewItem newDraggedOverItem)
		{
			//m_draggedOverItem.set(newDraggedOverItem);
		}

		private void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving, TreeViewItem keyboardReorderedContainer)
		{
			//Preserve old value of string in case it's needed.
			string oldValue = m_dropTargetDropEffectString;

			TreeViewItem dragItem = null;
			AutomationPeer dragItemPeer = null;

			if (keyboardReorderedContainer != null)
			{
				dragItem = keyboardReorderedContainer;
			}
			else
			{
				var listItem = ContainerFromItem(m_draggedOverItem) as TreeViewItem;
				if (listItem != null)
				{
					dragItem = listItem;
				}
			}

			if (dragItem != null)
			{
				var dragItemString = "";
				var itemBeforeInsertPositionString = "";
				var itemAfterInsertPositionString = "";
				var draggedOverString = "";

				dragItemPeer = FrameworkElementAutomationPeer.FromElement(dragItem);
				if (dragItemPeer != null)
				{
					dragItemString = dragItemPeer.GetName();//TODO: dragItemPeer.GetNameCore();
				}

				if (string.IsNullOrEmpty(dragItemString))
				{
					dragItemString = ResourceAccessor.GetLocalizedStringResource(
						ResourceAccessor.SR_DefaultItemString);
				}

				if (isLeaving)
				{
					m_dropTargetDropEffectString = StringUtil.FormatString(
						ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_CancelDraggingString),
						dragItemString);
				}
				else
				{
					//TreeViewItem itemAfterInsertPosition = null;
					//TreeViewItem itemBeforeInsertPosition = null;

					if (m_draggedOverItem != null)
					{
						AutomationPeer draggedOverItemPeer = FrameworkElementAutomationPeer.FromElement(m_draggedOverItem);
						if (draggedOverItemPeer != null)
						{
							draggedOverString = draggedOverItemPeer.GetName(); //TODO: draggedOverItemPeer.GetNameCore();
						}
					}
					else
					{
						int insertIndex = -1;
						int afterInsertIndex = -1;
						int beforeInsertIndex = -1;
						var dragItemIndex = IndexFromContainer(dragItem);

						if (keyboardReorderedContainer != null)
						{
							insertIndex = dragItemIndex;
						}
						else
						{
							insertIndex = m_emptySlotIndex;
						}

						if (insertIndex != -1)
						{
							afterInsertIndex = insertIndex + 1;
							beforeInsertIndex = insertIndex - 1;
						}

						if (insertIndex > dragItemIndex)
						{
							beforeInsertIndex++;
						}
						else if (insertIndex < dragItemIndex)
						{
							afterInsertIndex--;
						}

						itemBeforeInsertPositionString = GetAutomationName(beforeInsertIndex);
						itemAfterInsertPositionString = GetAutomationName(afterInsertIndex);
					}

					m_dropTargetDropEffectString = BuildEffectString(itemBeforeInsertPositionString, itemAfterInsertPositionString, dragItemString, draggedOverString);
				}

				if (!forceUpdate && oldValue != m_dropTargetDropEffectString)
				{
					AutomationPeer treePeer = FrameworkElementAutomationPeer.FromElement(this);
					treePeer.RaisePropertyChangedEvent(DropTargetPatternIdentifiers.DropTargetEffectProperty, oldValue, m_dropTargetDropEffectString);
				}
			}
		}

		internal void EnableMultiselect(bool isEnabled)
		{
			m_isMultiselectEnabled = isEnabled;
		}

		internal bool IsMultiselect => m_isMultiselectEnabled;

		private bool IsMutiSelectWithSelectedItems()
		{
			//    auto selectedItems = ListViewModel()->GetSelectedNodes();
			//bool isMutiSelect = m_isMultiselectEnabled && selectedItems.Size() > 0;
			//    return isMutiSelect;
			throw new NotImplementedException();
		}

		private bool IsSelected(TreeViewNode node)
		{
			bool isSelected = false;
			var selectedItems = ListViewModel.SelectedNodes;
			for (var i = 0; i < selectedItems.Count; i++)
			{
				if (selectedItems[i] == node)
				{
					isSelected = true;
					break;
				}
			}

			return isSelected;
		}

		private IList<TreeViewNode> GetRootsOfSelectedSubtrees()
		{
			//    std::vector<winrt::TreeViewNode> roots;
			//auto selectedItems = ListViewModel()->GetSelectedNodes();
			//    for (unsigned int i = 0; i<selectedItems.Size(); i++)
			//    {
			//        auto item = selectedItems.GetAt(i);
			//auto selectionRoot = GetRootOfSelection(item);
			//auto it = std::find(roots.cbegin(), roots.cend(), selectionRoot);
			//        if (it == roots.cend())
			//        {
			//            roots.emplace_back(selectionRoot);
			//        }
			//    }

			//    return roots;
			throw new NotImplementedException();
		}

		private int FlatIndex(TreeViewNode node)
		{
			var flatIndex = 0;
			bool found = ListViewModel.IndexOfNode(node, flatIndex);
			flatIndex = found ? flatIndex : -1;
			return flatIndex;
		}

		private bool IsFlatIndexValid(int index)
		{
			//    return index >= 0 && index<static_cast<int>(ListViewModel()->Size());
			throw new NotImplementedException();
		}

		private int RemoveNodeFromParent(TreeViewNode node)
		{
			//	unsigned int indexInParent = 0;
			//	auto children = winrt::get_self<TreeViewNodeVector>(node.Parent().Children());
			//	if (children->IndexOf(node, indexInParent))
			//	{
			//		children->RemoveAt(indexInParent);
			//	}

			//	return indexInParent;
			throw new NotImplementedException();
		}

		private bool IsIndexValid(int index)
		{
			//	return index >= 0 && index < (int)Items().Size();
			throw new NotImplementedException();
		}

		private string GetAutomationName(int index)
		{
			TreeViewItem item = null;
			string automationName = "";

			if (IsIndexValid(index))
			{
				var interim = ContainerFromIndex(index) as TreeViewItem;
				if (item != null)
				{
					item = interim;
				}
			}

			if (item != null)
			{
				AutomationPeer itemPeer = FrameworkElementAutomationPeer.FromElement(item);
				if (itemPeer != null)
				{
					automationName = itemPeer.GetName(); //TODO: itemPeer.GetNameCore();
				}
			}

			return automationName;
		}

		private string BuildEffectString(string priorString, string afterString, string dragString, string dragOverString)
		{
			//	hstring resultString;
			//	if (!priorString.empty() && !afterString.empty())
			//	{
			//		resultString = StringUtil::FormatString(
			//			ResourceAccessor::GetLocalizedStringResource(SR_PlaceBetweenString),
			//			dragString.data(), priorString.data(), afterString.data());
			//	}
			//	else if (!priorString.empty())
			//	{
			//		resultString = StringUtil::FormatString(
			//			ResourceAccessor::GetLocalizedStringResource(SR_PlaceAfterString),
			//			dragString.data(), priorString.data());
			//	}
			//	else if (!afterString.empty())
			//	{
			//		resultString = StringUtil::FormatString(
			//			ResourceAccessor::GetLocalizedStringResource(SR_PlaceBeforeString),
			//			dragString.data(), afterString.data());
			//	}
			//	else if (!dragOverString.empty())
			//	{
			//		resultString = StringUtil::FormatString(
			//			ResourceAccessor::GetLocalizedStringResource(SR_DropIntoNodeString),
			//			dragString.data(), dragOverString.data());
			//	}
			//	else
			//	{
			//		resultString = StringUtil::FormatString(
			//			ResourceAccessor::GetLocalizedStringResource(SR_FallBackPlaceString),
			//			dragString.data());
			//	}

			//	return resultString;
			throw new NotImplementedException();
		}

		private uint IndexInParent(TreeViewNode node)
		{
			//	unsigned int indexInParent;
			//	node.Parent().Children().IndexOf(node, indexInParent);
			//	return indexInParent;
			throw new NotImplementedException();
		}

		private TreeViewNode NodeAtFlatIndex(int index)
		{
			//    return ListViewModel()->GetNodeAt(index);
			throw new NotImplementedException();
		}

		private TreeViewNode GetRootOfSelection(TreeViewNode node)
		{
			//    winrt::TreeViewNode current = node;
			//    while (current.Parent() && IsSelected(current.Parent()))
			//    {
			//        current = current.Parent();
			//    }

			//    return current;
			throw new NotImplementedException();
		}

		internal TreeViewNode NodeFromContainer(DependencyObject container)
		{
			//int index = container ? IndexFromContainer(container) : -1;
			//if (index >= 0 && index < static_cast<int32_t>(ListViewModel()->Size()))
			//{
			//	return NodeAtFlatIndex(index);
			//}
			//return nullptr;
			throw new NotImplementedException();
		}

		internal DependencyObject ContainerFromNode(TreeViewNode node)
		{
			//	if (!node)
			//	{
			//		return nullptr;
			//	}

			//	return IsContentMode() ? ContainerFromItem(node.Content()) : ContainerFromItem(node);
			throw new NotImplementedException();
		}

		private TreeViewNode NodeFromItem(object item)
		{
			//	return IsContentMode() ?
			//		ListViewModel().get()->GetAssociatedNode(item) :
			//		item.try_as<winrt::TreeViewNode>();
			throw new NotImplementedException();
		}

		private object ItemFromNode(TreeViewNode node)
		{
			//return (IsContentMode() && node) ? node.Content() : static_cast<winrt::IInspectable>(node);
			throw new NotImplementedException();
		}

		internal bool IsContentMode => ListViewModel?.IsContentMode ?? false;
	}
}
