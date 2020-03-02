using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
    public partial class TreeViewList
    {
//		TreeViewList::TreeViewList()
//{
//    ListViewModel(winrt::make_self<ViewModel>());

//		DragItemsStarting({ this, &TreeViewList::OnDragItemsStarting });
//    DragItemsCompleted({ this, &TreeViewList::OnDragItemsCompleted });
//    ContainerContentChanging({ this, &TreeViewList::OnContainerContentChanging });
//}

//	com_ptr<ViewModel> TreeViewList::ListViewModel() const
//{
//    return m_viewModel.get();
//}

//void TreeViewList::ListViewModel(com_ptr<ViewModel> viewModel)
//{
//	m_viewModel.set(viewModel);
//}

//winrt::TreeViewNode TreeViewList::DraggedTreeViewNode()
//{
//	return m_draggedTreeViewNode.get();
//}

//void TreeViewList::DraggedTreeViewNode(winrt::TreeViewNode const& node)
//{
//	m_draggedTreeViewNode.set(node);
//}

//void TreeViewList::OnDragItemsStarting(const winrt::IInspectable& /*sender*/, const winrt::DragItemsStartingEventArgs& args)
//{
//	auto dragItem = args.Items().GetAt(0);
//	if (dragItem)
//	{
//		auto tvItem = ContainerFromItem(dragItem);
//		m_draggedTreeViewNode.set(NodeFromContainer(tvItem));
//		bool isMultipleDragging = false;
//		if (IsMutiSelectWithSelectedItems())
//		{
//			int selectedCount = ListViewModel()->GetSelectedNodes().Size();
//			if (selectedCount > 1)
//			{
//				auto settings = tvItem.as< winrt::TreeViewItem > ().TreeViewItemTemplateSettings();
//				winrt::get_self<TreeViewItemTemplateSettings>(settings)->DragItemsCount(selectedCount);
//				isMultipleDragging = true;

//				// TreeView items' selection states are maintained by ViewModel, not by TreeViewList.
//				// When TreeView is set to multi-select mode, the underlying TreeViewList's selection mode is set to None (See OnPropertyChanged function in TreeView.cpp).
//				// TreeViewList has no knowledge about item selections happened in TreeView, no matter how many items are actually selected, args.Items() always contains only one item that is currently being dragged by cursor.
//				// Here, we manually add selected nodes to args.Items in order to expose all selected (and dragged) items to outside handlers.
//				args.Items().Clear();

//				for (auto const&node : ListViewModel()->GetSelectedNodes())
//                {
//					if (IsContentMode())
//					{
//						args.Items().Append(node.Content());
//					}
//					else
//					{
//						args.Items().Append(node);
//					}
//				}
//			}
//		}
//		else
//		{
//			// If not in multi-select mode, collapse the dragged node.
//			m_draggedTreeViewNode.get().IsExpanded(false);
//		}

//		winrt::VisualStateManager::GoToState(tvItem.as< winrt::TreeViewItem > (), isMultipleDragging ? L"MultipleDraggingPrimary" : L"Dragging", false);
//		UpdateDropTargetDropEffect(false, false, nullptr);
//	}
//}

//void TreeViewList::OnDragItemsCompleted(const winrt::IInspectable& /*sender*/, const winrt::DragItemsCompletedEventArgs& /*args*/)
//{
//	m_draggedTreeViewNode.set(nullptr);
//	m_emptySlotIndex = -1;
//}

//void TreeViewList::OnContainerContentChanging(const winrt::IInspectable& /*sender*/, const winrt::ContainerContentChangingEventArgs& args)
//{
//	// Only need to update unrecycled on screen items.
//	if (!args.InRecycleQueue())
//	{
//		auto targetItem = args.ItemContainer().as< winrt::TreeViewItem > ();
//		auto targetNode = NodeFromContainer(targetItem);
//		auto treeViewItem = winrt::get_self<TreeViewItem>(targetItem);
//		treeViewItem->UpdateIndentation(targetNode.Depth());
//		treeViewItem->UpdateSelectionVisual(winrt::get_self<TreeViewNode>(targetNode)->SelectionState());
//	}
//}

//// IControlOverrides
//void TreeViewList::OnDrop(winrt::DragEventArgs const& e)
//{
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

//	__super::OnDrop(e);
//}

//void TreeViewList::MoveNodeInto(winrt::TreeViewNode const& node, winrt::TreeViewNode const& insertAtNode)
//{
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
//}

//void TreeViewList::OnDragOver(winrt::DragEventArgs const& args)
//{
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

//	__super::OnDragOver(args);
//}

//void TreeViewList::OnDragEnter(winrt::DragEventArgs const& args)
//{
//	if (!args.Handled())
//	{
//		UpdateDropTargetDropEffect(false, false, nullptr);
//	}
//	__super::OnDragEnter(args);
//}

//void TreeViewList::OnDragLeave(winrt::DragEventArgs const& args)
//{
//	m_emptySlotIndex = -1;
//	__super::OnDragLeave(args);
//	if (!args.Handled())
//	{
//		UpdateDropTargetDropEffect(false, true, nullptr);
//	}
//}

//// IItemsControlOverrides
//void TreeViewList::PrepareContainerForItemOverride(winrt::DependencyObject const& element, winrt::IInspectable const& item)
//{
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
//}

//winrt::DependencyObject TreeViewList::GetContainerForItemOverride()
//{
//	winrt::TreeViewItem targetItem = winrt::TreeViewItem();
//	return targetItem;
//}

//// IFrameworkElementOverrides
//void TreeViewList::OnApplyTemplate()
//{
//	if (!m_itemsSourceAttached)
//	{
//		ItemsSource(*ListViewModel());
//		IsItemClickEnabled(true);
//		m_itemsSourceAttached = true;
//	}

//	__super::OnApplyTemplate();
//}

//// IUIElementOverrides
//winrt::AutomationPeer TreeViewList::OnCreateAutomationPeer()
//{
//	return winrt::make<TreeViewListAutomationPeer>(*this);
//}

//winrt::hstring TreeViewList::GetDropTargetDropEffect()
//{
//	if (m_dropTargetDropEffectString.empty())
//	{
//		UpdateDropTargetDropEffect(true, false, nullptr);
//	}
//	return m_dropTargetDropEffectString;
//}

//void TreeViewList::SetDraggedOverItem(winrt::TreeViewItem newDraggedOverItem)
//{
//	m_draggedOverItem.set(newDraggedOverItem);
//}

//void TreeViewList::UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving, winrt::TreeViewItem keyboardReorderedContainer)
//{
//	//Preserve old value of string in case it's needed.
//	winrt::hstring oldValue = m_dropTargetDropEffectString;

//	winrt::TreeViewItem dragItem{ nullptr };
//	winrt::AutomationPeer dragItemPeer{ nullptr };

//	if (keyboardReorderedContainer)
//	{
//		dragItem = keyboardReorderedContainer;
//	}
//	else
//	{
//		auto listItem = ContainerFromItem(m_draggedOverItem.get());
//		if (listItem)
//		{
//			dragItem = listItem.as< winrt::TreeViewItem > ();
//		}
//	}

//	if (dragItem)
//	{
//		winrt::hstring dragItemString = L"";
//		winrt::hstring itemBeforeInsertPositionString = L"";
//		winrt::hstring itemAfterInsertPositionString = L"";
//		winrt::hstring draggedOverString = L"";

//		dragItemPeer = winrt::FrameworkElementAutomationPeer::FromElement(dragItem);
//		if (dragItemPeer)
//		{
//			dragItemString = dragItemPeer.GetNameCore();
//		}

//		if (dragItemString.empty())
//		{
//			dragItemString = ResourceAccessor::GetLocalizedStringResource(SR_DefaultItemString);
//		}

//		if (isLeaving)
//		{
//			m_dropTargetDropEffectString = StringUtil::FormatString(
//				ResourceAccessor::GetLocalizedStringResource(SR_CancelDraggingString),
//				dragItemString);
//		}
//		else
//		{
//			winrt::TreeViewItem itemAfterInsertPosition{ nullptr };
//			winrt::TreeViewItem itemBeforeInsertPosition{ nullptr };

//			if (m_draggedOverItem)
//			{
//				winrt::AutomationPeer draggedOverItemPeer = winrt::FrameworkElementAutomationPeer::FromElement(m_draggedOverItem.get());
//				if (draggedOverItemPeer)
//				{
//					draggedOverString = draggedOverItemPeer.GetNameCore();
//				}
//			}
//			else
//			{
//				int insertIndex = -1;
//				int afterInsertIndex = -1;
//				int beforeInsertIndex = -1;
//				auto dragItemIndex = IndexFromContainer(dragItem);

//				if (keyboardReorderedContainer)
//				{
//					insertIndex = dragItemIndex;
//				}
//				else
//				{
//					insertIndex = m_emptySlotIndex;
//				}

//				if (insertIndex != -1)
//				{
//					afterInsertIndex = insertIndex + 1;
//					beforeInsertIndex = insertIndex - 1;
//				}

//				if (insertIndex > dragItemIndex)
//				{
//					beforeInsertIndex++;
//				}
//				else if (insertIndex < dragItemIndex)
//				{
//					afterInsertIndex--;
//				}

//				itemBeforeInsertPositionString = GetAutomationName(beforeInsertIndex);
//				itemAfterInsertPositionString = GetAutomationName(afterInsertIndex);
//			}

//			m_dropTargetDropEffectString = BuildEffectString(itemBeforeInsertPositionString, itemAfterInsertPositionString, dragItemString, draggedOverString);
//		}

//		if (!forceUpdate && oldValue != m_dropTargetDropEffectString)
//		{
//			winrt::AutomationPeer treePeer = winrt::FrameworkElementAutomationPeer::FromElement(*this);
//			treePeer.RaisePropertyChangedEvent(winrt::DropTargetPatternIdentifiers::DropTargetEffectProperty(), box_value(oldValue.data()), box_value(m_dropTargetDropEffectString.data()));
//		}
//	}
//}

//void TreeViewList::EnableMultiselect(bool isEnabled)
//{
//	m_isMultiselectEnabled = isEnabled;
//}

//bool TreeViewList::IsMultiselect() const
//{
//    return m_isMultiselectEnabled;
//}


//bool TreeViewList::IsMutiSelectWithSelectedItems() const
//{
//    auto selectedItems = ListViewModel()->GetSelectedNodes();
//bool isMutiSelect = m_isMultiselectEnabled && selectedItems.Size() > 0;
//    return isMutiSelect;
//}

//bool TreeViewList::IsSelected(const winrt::TreeViewNode& node) const
//{
//    bool isSelected = false;
//auto selectedItems = ListViewModel()->GetSelectedNodes();
//    for (unsigned int i = 0; i<selectedItems.Size(); i++)
//    {
//        if (selectedItems.GetAt(i) == node)
//        {
//            isSelected = true;
//            break;
//        }
//    }

//    return isSelected;
//}

//std::vector<winrt::TreeViewNode> TreeViewList::GetRootsOfSelectedSubtrees() const
//{
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
//}

//int TreeViewList::FlatIndex(const winrt::TreeViewNode& node) const
//{
//    unsigned int flatIndex = 0;
//bool found = ListViewModel()->IndexOfNode(node, flatIndex);
//flatIndex = found? flatIndex : -1;
//    return flatIndex;
//}

//bool TreeViewList::IsFlatIndexValid(int index) const
//{
//    return index >= 0 && index<static_cast<int>(ListViewModel()->Size());
//}

//unsigned int TreeViewList::RemoveNodeFromParent(const winrt::TreeViewNode& node)
//{
//	unsigned int indexInParent = 0;
//	auto children = winrt::get_self<TreeViewNodeVector>(node.Parent().Children());
//	if (children->IndexOf(node, indexInParent))
//	{
//		children->RemoveAt(indexInParent);
//	}

//	return indexInParent;
//}

//bool TreeViewList::IsIndexValid(int index)
//{
//	return index >= 0 && index < (int)Items().Size();
//}

//hstring TreeViewList::GetAutomationName(int index)
//{
//	winrt::TreeViewItem item{ nullptr };
//	hstring automationName = L"";

//	if (IsIndexValid(index))
//	{
//		auto interim = ContainerFromIndex(index);
//		if (interim)
//		{
//			item = interim.as< winrt::TreeViewItem > ();
//		}
//	}

//	if (item)
//	{
//		winrt::AutomationPeer itemPeer = winrt::FrameworkElementAutomationPeer::FromElement(item);
//		if (itemPeer)
//		{
//			automationName = itemPeer.GetNameCore();
//		}
//	}

//	return automationName;
//}

//hstring TreeViewList::BuildEffectString(hstring priorString, hstring afterString, hstring dragString, hstring dragOverString)
//{
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
//}

//unsigned int TreeViewList::IndexInParent(const winrt::TreeViewNode& node)
//{
//	unsigned int indexInParent;
//	node.Parent().Children().IndexOf(node, indexInParent);
//	return indexInParent;
//}

//winrt::TreeViewNode TreeViewList::NodeAtFlatIndex(int index) const
//{
//    return ListViewModel()->GetNodeAt(index);
//}

//winrt::TreeViewNode TreeViewList::GetRootOfSelection(const winrt::TreeViewNode& node) const
//{
//    winrt::TreeViewNode current = node;
//    while (current.Parent() && IsSelected(current.Parent()))
//    {
//        current = current.Parent();
//    }

//    return current;
//}

//winrt::TreeViewNode TreeViewList::NodeFromContainer(winrt::DependencyObject const& container)
//{
//	int index = container ? IndexFromContainer(container) : -1;
//	if (index >= 0 && index < static_cast<int32_t>(ListViewModel()->Size()))
//	{
//		return NodeAtFlatIndex(index);
//	}
//	return nullptr;
//}

//winrt::DependencyObject TreeViewList::ContainerFromNode(winrt::TreeViewNode const& node)
//{
//	if (!node)
//	{
//		return nullptr;
//	}

//	return IsContentMode() ? ContainerFromItem(node.Content()) : ContainerFromItem(node);
//}

//winrt::TreeViewNode TreeViewList::NodeFromItem(winrt::IInspectable const& item)
//{
//	return IsContentMode() ?
//		ListViewModel().get()->GetAssociatedNode(item) :
//		item.try_as<winrt::TreeViewNode>();
//}

//winrt::IInspectable TreeViewList::ItemFromNode(winrt::TreeViewNode const& node)
//{
//	return (IsContentMode() && node) ? node.Content() : static_cast<winrt::IInspectable>(node);
//}

//bool TreeViewList::IsContentMode()
//{
//	if (auto viewModel = ListViewModel())
//    {
//		return viewModel->IsContentMode();
//	}

//	return false;
//}        
    }
}
