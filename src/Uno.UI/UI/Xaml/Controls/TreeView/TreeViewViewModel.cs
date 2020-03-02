using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
    public class TreeViewViewModel
    {
//		// Need to update node selection states on UI before vector changes.
//		// Listen on vector change events don't solve the problem because the event already happened when the event handler gets called.
//		// i.e. the node is alreay gone when we get to ItemRemoved callback.
//#pragma region SelectedTreeNodeVector

//		typedef typename VectorOptionsFromFlag<winrt::TreeViewNode, MakeVectorParam<VectorFlag::Observable, VectorFlag::DependencyObjectBase>()> SelectedTreeNodeVectorOptions;

//class SelectedTreeNodeVector :

//	public ReferenceTracker<
//	SelectedTreeNodeVector,
//	reference_tracker_implements_t<typename SelectedTreeNodeVectorOptions::VectorType>::type,
//	typename TreeViewNodeVectorOptions::IterableType,
//	typename TreeViewNodeVectorOptions::ObservableVectorType>,
//    public TreeViewNodeVectorOptions::IVectorOwner
//{
//    Implement_Vector_Read(SelectedTreeNodeVectorOptions)

//private:
//    winrt::weak_ref<ViewModel> m_viewModel { nullptr };

//		void UpdateSelection(winrt::TreeViewNode const& node, TreeNodeSelectionState state)
//    {
//        if (winrt::get_self<TreeViewNode>(node)->SelectionState() != state)
//        {
//            if (auto viewModel = m_viewModel.get())
//            {
//                viewModel->UpdateSelection(node, state);
//		viewModel->NotifyContainerOfSelectionChange(node, state);
//	}
//}
//    }

//public:
//    void SetViewModel(ViewModel& viewModel)
//{
//	m_viewModel = viewModel.get_weak();
//}

//void Append(winrt::TreeViewNode const& node)
//{
//	InsertAt(Size(), node);
//}

//void InsertAt(unsigned int index, winrt::TreeViewNode const& node)
//{
//	if (!Contains(node))
//	{
//		// UpdateSelection will call InsertAtCore
//		UpdateSelection(node, TreeNodeSelectionState::Selected);
//	}
//}

//void SetAt(unsigned int index, winrt::TreeViewNode const& node)
//{
//	RemoveAt(index);
//	InsertAt(index, node);
//}

//void RemoveAt(unsigned int index)
//{
//	auto inner = GetVectorInnerImpl();
//	auto oldNode = winrt::get_self<TreeViewNode>(inner->GetAt(index));
//	// UpdateNodeSelection will call RemoveAtCore
//	UpdateSelection(*oldNode, TreeNodeSelectionState::UnSelected);
//}

//void RemoveAtEnd()
//{
//	RemoveAt(Size() - 1);
//}

//void ReplaceAll(winrt::array_view<winrt::TreeViewNode const> nodes)
//{
//	Clear();

//	for (auto const&node : nodes)
//        {
//		Append(node);
//	}
//}

//void Clear()
//{
//	while (Size() > 0)
//	{
//		RemoveAtEnd();
//	}
//}

//bool Contains(winrt::TreeViewNode const& node)
//{
//	uint32_t index;
//	return GetVectorInnerImpl()->IndexOf(node, index);
//}

//// Default write methods will trigger TreeView visual updates.
//// If you want to update vector content without notifying TreeViewNodes, use "core" version of the methods.
//void InsertAtCore(unsigned int index, winrt::TreeViewNode const& node)
//{
//	GetVectorInnerImpl()->InsertAt(index, node);

//	// Keep SelectedItems and SelectedNodes in sync
//	if (auto viewModel = m_viewModel.get())
//        {
//		auto selectedItems = viewModel->GetSelectedItems();
//		if (selectedItems.Size() != Size())
//		{
//			if (auto listControl = viewModel->ListControl())
//                {
//				if (auto item = winrt::get_self<TreeViewList>(listControl)->ItemFromNode(node))
//                    {
//					selectedItems.InsertAt(index, item);
//				}
//			}
//		}
//	}
//}

//void RemoveAtCore(unsigned int index)
//{
//	GetVectorInnerImpl()->RemoveAt(index);

//	// Keep SelectedItems and SelectedNodes in sync
//	if (auto viewModel = m_viewModel.get())
//        {
//		auto selectedItems = viewModel->GetSelectedItems();
//		if (Size() != selectedItems.Size())
//		{
//			selectedItems.RemoveAt(index);
//		}
//	}
//}
//};

//#pragma endregion

//// Similiar to SelectedNodesVector above, we need to make decisions before the item is inserted or removed.
//// we can't use vector change events because the event already happened when event hander gets called.
//#pragma region SelectedItemsVector

//typedef typename VectorOptionsFromFlag<winrt::IInspectable, MakeVectorParam<VectorFlag::Observable, VectorFlag::DependencyObjectBase>()> SelectedItemsVectorOptions;

//class SelectedItemsVector :

//	public ReferenceTracker<
//	SelectedItemsVector,
//	reference_tracker_implements_t<typename SelectedItemsVectorOptions::VectorType>::type,
//	typename SelectedItemsVectorOptions::IterableType,
//	typename SelectedItemsVectorOptions::ObservableVectorType>,
//    public SelectedItemsVectorOptions::IVectorOwner
//{
//    Implement_Vector_Read(SelectedItemsVectorOptions)

//private:
//    winrt::weak_ref<ViewModel> m_viewModel { nullptr };

//public:
//    void SetViewModel(ViewModel& viewModel)
//{
//	m_viewModel = viewModel.get_weak();
//}

//void Append(winrt::IInspectable const& item)
//{
//	InsertAt(Size(), item);
//}

//void InsertAt(unsigned int index, winrt::IInspectable const& item)
//{
//	if (!Contains(item))
//	{
//		GetVectorInnerImpl()->InsertAt(index, item);

//		// Keep SelectedNodes and SelectedItems in sync
//		if (auto viewModel = m_viewModel.get())
//            {
//			auto selectedNodes = viewModel->GetSelectedNodes();
//			if (selectedNodes.Size() != Size())
//			{
//				if (auto listControl = viewModel->ListControl())
//                    {
//					if (auto node = winrt::get_self<TreeViewList>(listControl)->NodeFromItem(item))
//                        {
//						selectedNodes.InsertAt(index, node);
//					}
//				}
//			}
//		}
//	}
//}

//void SetAt(unsigned int index, winrt::IInspectable const& item)
//{
//	RemoveAt(index);
//	InsertAt(index, item);
//}

//void RemoveAt(unsigned int index)
//{
//	GetVectorInnerImpl()->RemoveAt(index);

//	// Keep SelectedNodes and SelectedItems in sync
//	if (auto viewModel = m_viewModel.get())
//        {
//		auto selectedNodes = viewModel->GetSelectedNodes();
//		if (Size() != selectedNodes.Size())
//		{
//			selectedNodes.RemoveAt(index);
//		}
//	}
//}

//void RemoveAtEnd()
//{
//	RemoveAt(Size() - 1);
//}

//void ReplaceAll(winrt::array_view<winrt::IInspectable const> items)
//{
//	Clear();

//	for (auto const&node : items)
//        {
//		Append(node);
//	}
//}

//void Clear()
//{
//	while (Size() > 0)
//	{
//		RemoveAtEnd();
//	}
//}

//bool Contains(winrt::IInspectable const& item)
//{
//	uint32_t index;
//	return GetVectorInnerImpl()->IndexOf(item, index);
//}
//};

//#pragma endregion

//ViewModel::ViewModel()
//{
//    auto selectedNodes = winrt::make_self<SelectedTreeNodeVector>();
//selectedNodes->SetViewModel(*this);
//m_selectedNodes.set(* selectedNodes);

//    auto selectedItems = winrt::make_self<SelectedItemsVector>();
//selectedItems->SetViewModel(*this);
//m_selectedItems.set(* selectedItems);

//    m_itemToNodeMap.set(winrt::make<HashMap<winrt::IInspectable, winrt::TreeViewNode>>());
//}

//ViewModel::~ViewModel()
//{
//	if (m_rootNodeChildrenChangedEventToken.value != 0)
//	{
//		if (auto origin = m_originNode.safe_get())
//        {
//			winrt::get_self<TreeViewNode>(origin)->ChildrenChanged(m_rootNodeChildrenChangedEventToken);
//		}

//		ClearEventTokenVectors();
//	}
//}

//void ViewModel::ExpandNode(const winrt::TreeViewNode& value)
//{
//	value.IsExpanded(true);
//}

//void ViewModel::CollapseNode(const winrt::TreeViewNode& value)
//{
//	value.IsExpanded(false);
//}

//winrt::event_token ViewModel::NodeExpanding(const winrt::TypedEventHandler<winrt::TreeViewNode, winrt::IInspectable>& handler)
//{
//	return m_nodeExpandingEventSource.add(handler);
//}

//void ViewModel::NodeExpanding(const winrt::event_token token)
//{
//	m_nodeExpandingEventSource.remove(token);
//}

//winrt::event_token ViewModel::NodeCollapsed(const winrt::TypedEventHandler<winrt::TreeViewNode, winrt::IInspectable>& handler)
//{
//	return m_nodeCollapsedEventSource.add(handler);
//}

//void ViewModel::NodeCollapsed(const winrt::event_token token)
//{
//	m_nodeCollapsedEventSource.remove(token);
//}

//void ViewModel::SelectAll()
//{
//	UpdateSelection(m_originNode.get(), TreeNodeSelectionState::Selected);
//}

//void ViewModel::ModifySelectByIndex(int index, TreeNodeSelectionState const& state)
//{
//	auto targetNode = GetNodeAt(index);
//	UpdateSelection(targetNode, state);
//}

//uint32_t ViewModel::Size()
//{
//	auto inner = GetVectorInnerImpl();
//	return inner->Size();
//}

//winrt::IInspectable ViewModel::GetAt(uint32_t index)
//{
//	winrt::TreeViewNode node = GetNodeAt(index);
//	return IsContentMode() ? node.Content() : node;
//}

//bool ViewModel::IndexOf(winrt::IInspectable const& value, uint32_t& index)
//{
//	if (auto indexOfFunction = GetCustomIndexOfFunction())
//    {
//		return indexOfFunction(value, index);
//	}

//	else
//	{
//		auto inner = GetVectorInnerImpl();
//		return inner->IndexOf(value, index);
//	}
//}

//uint32_t ViewModel::GetMany(uint32_t const startIndex, winrt::array_view<winrt::IInspectable> values)
//{
//	auto inner = GetVectorInnerImpl();
//	if (IsContentMode())
//	{
//		auto vector = winrt::make<Vector<winrt::IInspectable>>();
//		int size = Size();
//		for (int i = 0; i < size; i++)
//		{
//			vector.Append(GetNodeAt(i).Content());
//		}
//		return vector.GetMany(startIndex, values);
//	}
//	return inner->GetMany(startIndex, values);
//}

//winrt::IVectorView<winrt::IInspectable> ViewModel::GetView()
//{
//	throw winrt::hresult_not_implemented();
//}

//winrt::TreeViewNode ViewModel::GetNodeAt(uint32_t index)
//{
//	auto inner = GetVectorInnerImpl();
//	return inner->GetAt(index).as< winrt::TreeViewNode > ();
//}

//void ViewModel::SetAt(uint32_t index, winrt::IInspectable const& value)
//{
//	auto inner = GetVectorInnerImpl();
//	auto current = inner->GetAt(index).as< winrt::TreeViewNode > ();
//	inner->SetAt(index, value);

//	winrt::TreeViewNode newNode = value.as< winrt::TreeViewNode > ();

//	auto tvnCurrent = winrt::get_self<TreeViewNode>(current);
//	tvnCurrent->ChildrenChanged(m_collectionChangedEventTokenVector[index]);
//	tvnCurrent->RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector[index]);

//	// Hook up events and replace tokens
//	auto tvnNewNode = winrt::get_self<TreeViewNode>(newNode);
//	m_collectionChangedEventTokenVector[index] = tvnNewNode->ChildrenChanged({ this, &ViewModel::TreeViewNodeVectorChanged });
//	m_IsExpandedChangedEventTokenVector[index] = tvnNewNode->AddExpandedChanged({ this, &ViewModel::TreeViewNodePropertyChanged });
//}

//void ViewModel::InsertAt(uint32_t index, winrt::IInspectable const& value)
//{
//	GetVectorInnerImpl()->InsertAt(index, value);
//	winrt::TreeViewNode newNode = value.as< winrt::TreeViewNode > ();

//	//Hook up events and save tokens
//	auto tvnNewNode = winrt::get_self<TreeViewNode>(newNode);
//	m_collectionChangedEventTokenVector.insert(m_collectionChangedEventTokenVector.begin() + index, tvnNewNode->ChildrenChanged({ this, &ViewModel::TreeViewNodeVectorChanged }));
//	m_IsExpandedChangedEventTokenVector.insert(m_IsExpandedChangedEventTokenVector.begin() + index, tvnNewNode->AddExpandedChanged({ this, &ViewModel::TreeViewNodePropertyChanged }));
//}

//void ViewModel::RemoveAt(uint32_t index)
//{
//	auto inner = GetVectorInnerImpl();
//	auto current = inner->GetAt(index).as< winrt::TreeViewNode > ();
//	inner->RemoveAt(index);

//	// Unhook event handlers
//	auto tvnCurrent = winrt::get_self<TreeViewNode>(current);
//	tvnCurrent->ChildrenChanged(m_collectionChangedEventTokenVector[index]);
//	tvnCurrent->RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector[index]);

//	// Remove tokens from vectors
//	m_collectionChangedEventTokenVector.erase(m_collectionChangedEventTokenVector.begin() + index);
//	m_IsExpandedChangedEventTokenVector.erase(m_IsExpandedChangedEventTokenVector.begin() + index);
//}

//void ViewModel::Append(winrt::IInspectable const& value)
//{
//	GetVectorInnerImpl()->Append(value);
//	winrt::TreeViewNode newNode = value.as< winrt::TreeViewNode > ();

//	// Hook up events and save tokens
//	auto tvnNewNode = winrt::get_self<TreeViewNode>(newNode);
//	m_collectionChangedEventTokenVector.push_back(tvnNewNode->ChildrenChanged({ this, &ViewModel::TreeViewNodeVectorChanged }));
//	m_IsExpandedChangedEventTokenVector.push_back(tvnNewNode->AddExpandedChanged({ this, &ViewModel::TreeViewNodePropertyChanged }));
//}

//void ViewModel::RemoveAtEnd()
//{
//	auto inner = GetVectorInnerImpl();
//	auto current = inner->GetAt(Size() - 1).as< winrt::TreeViewNode > ();
//	inner->RemoveAtEnd();

//	// unhook events
//	auto tvnCurrent = winrt::get_self<TreeViewNode>(current);
//	tvnCurrent->ChildrenChanged(m_collectionChangedEventTokenVector.back());
//	tvnCurrent->RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector.back());

//	// remove tokens
//	m_collectionChangedEventTokenVector.pop_back();
//	m_IsExpandedChangedEventTokenVector.pop_back();
//}

//void ViewModel::Clear()
//{
//	// Don't call GetVectorInnerImpl()->Clear() directly because we need to remove hooked events
//	unsigned int count = Size();
//	while (count != 0)
//	{
//		RemoveAtEnd();
//		count--;
//	}
//}

//void ViewModel::ReplaceAll(winrt::array_view<winrt::IInspectable const> items)
//{
//	auto inner = GetVectorInnerImpl();
//	return inner->ReplaceAll(items);
//}

//// Helper function
//void ViewModel::PrepareView(const winrt::TreeViewNode& originNode)
//{
//	// Remove any existing RootNode events/children
//	if (auto existingOriginNode = m_originNode.get())
//    {
//		for (int i = (existingOriginNode.Children().Size() - 1); i >= 0; i--)
//		{
//			auto removeNode = existingOriginNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//			RemoveNodeAndDescendantsFromView(removeNode);
//		}

//		if (m_rootNodeChildrenChangedEventToken.value != 0)
//		{
//			existingOriginNode.Children().as< winrt::IObservableVector < winrt::TreeViewNode >> ().VectorChanged(m_rootNodeChildrenChangedEventToken);
//		}
//	}

//	// Add new RootNode & children
//	m_originNode.set(originNode);
//	m_rootNodeChildrenChangedEventToken = winrt::get_self<TreeViewNode>(originNode)->ChildrenChanged({ this, &ViewModel::TreeViewNodeVectorChanged });
//	originNode.IsExpanded(true);

//	int allOpenedDescendantsCount = 0;
//	for (unsigned int i = 0; i < originNode.Children().Size(); i++)
//	{
//		auto addNode = originNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//		AddNodeToView(addNode, i + allOpenedDescendantsCount);
//		allOpenedDescendantsCount = AddNodeDescendantsToView(addNode, i, allOpenedDescendantsCount);
//	}
//}

//void ViewModel::SetOwningList(winrt::TreeViewList const& owningList)
//{
//	m_TreeViewList = winrt::make_weak(owningList);
//}

//winrt::TreeViewList ViewModel::ListControl()
//{
//	return m_TreeViewList.get();
//}

//bool ViewModel::IsInSingleSelectionMode()
//{
//	return m_TreeViewList.get().SelectionMode() == winrt::ListViewSelectionMode::Single;
//}

//// Private helpers
//void ViewModel::AddNodeToView(const winrt::TreeViewNode& value, unsigned int index)
//{
//	InsertAt(index, value);
//}

//int ViewModel::AddNodeDescendantsToView(const winrt::TreeViewNode& value, unsigned int index, int offset)
//{
//	if (value.IsExpanded())
//	{
//		unsigned int size = value.Children().Size();
//		for (unsigned int i = 0; i < size; i++)
//		{
//			auto childNode = value.Children().GetAt(i).as< winrt::TreeViewNode > ();
//			offset++;
//			AddNodeToView(childNode, offset + index);
//			offset = AddNodeDescendantsToView(childNode, index, offset);
//		}

//		return offset;
//	}

//	return offset;
//}

//void ViewModel::RemoveNodeAndDescendantsFromView(const winrt::TreeViewNode& value)
//{
//	UINT32 valueIndex;
//	if (value.IsExpanded())
//	{
//		unsigned int size = value.Children().Size();
//		for (unsigned int i = 0; i < size; i++)
//		{
//			auto childNode = value.Children().GetAt(i).as< winrt::TreeViewNode > ();
//			RemoveNodeAndDescendantsFromView(childNode);
//		}
//	}

//	bool containsValue = IndexOfNode(value, valueIndex);
//	if (containsValue)
//	{
//		RemoveAt(valueIndex);
//	}
//}

//void ViewModel::RemoveNodesAndDescendentsWithFlatIndexRange(unsigned int lowIndex, unsigned int highIndex)
//{
//	MUX_ASSERT(lowIndex <= highIndex);

//	for (int i = static_cast<int>(highIndex); i >= static_cast<int>(lowIndex); i--)
//	{
//		RemoveNodeAndDescendantsFromView(GetNodeAt(i));
//	}
//}

//int ViewModel::GetNextIndexInFlatTree(const winrt::TreeViewNode& node)
//{
//	unsigned int index = 0;
//	bool isNodeInFlatList = IndexOfNode(node, index);

//	if (isNodeInFlatList)
//	{
//		index++;
//	}
//	else
//	{
//		// node is Root node, so next index in flat tree is 0
//		index = 0;
//	}
//	return index;
//}

//// When ViewModel receives a event, it only includes the sender(parent TreeViewNode) and index.
//// We can't use sender[index] directly because it is already updated/removed
//// To find the removed TreeViewNode:
////   calcuate allOpenedDescendantsCount in sender[0..index-1] first
////   then add offset and finally return TreeViewNode by looking up the flat tree.
//winrt::TreeViewNode ViewModel::GetRemovedChildTreeViewNodeByIndex(winrt::TreeViewNode const& node, unsigned int childIndex)
//{
//	unsigned int allOpenedDescendantsCount = 0;
//	for (unsigned int i = 0; i < childIndex; i++)
//	{
//		winrt::TreeViewNode calcNode = node.Children().GetAt(i).as< winrt::TreeViewNode > ();
//		if (calcNode.IsExpanded())
//		{
//			allOpenedDescendantsCount += GetExpandedDescendantCount(calcNode);
//		}
//	}

//	unsigned int childIndexInFlatTree = GetNextIndexInFlatTree(node) + childIndex + allOpenedDescendantsCount;
//	return GetNodeAt(childIndexInFlatTree);
//}

//int ViewModel::CountDescendants(const winrt::TreeViewNode& value)
//{
//	int descendantCount = 0;
//	unsigned int size = value.Children().Size();
//	for (unsigned int i = 0; i < size; i++)
//	{
//		auto childNode = value.Children().GetAt(i).as< winrt::TreeViewNode > ();
//		descendantCount++;
//		if (childNode.IsExpanded())
//		{
//			descendantCount = descendantCount + CountDescendants(childNode);
//		}
//	}

//	return descendantCount;
//}

//unsigned int ViewModel::IndexOfNextSibling(winrt::TreeViewNode& childNode)
//{
//	auto parentNode = childNode.Parent();
//	unsigned int stopIndex;
//	bool isLastRelativeChild = true;
//	while (parentNode && isLastRelativeChild)
//	{
//		unsigned int relativeIndex;
//		parentNode.Children().IndexOf(childNode, relativeIndex);
//		if (parentNode.Children().Size() - 1 != relativeIndex)
//		{
//			isLastRelativeChild = false;
//		}
//		else
//		{
//			childNode = parentNode;
//			parentNode = parentNode.Parent();
//		}
//	}

//	if (parentNode)
//	{
//		unsigned int siblingIndex;
//		parentNode.Children().IndexOf(childNode, siblingIndex);
//		auto siblingNode = parentNode.Children().GetAt(siblingIndex + 1);
//		IndexOfNode(siblingNode, stopIndex);
//	}
//	else
//	{
//		stopIndex = Size();
//	}

//	return stopIndex;
//}

//unsigned int ViewModel::GetExpandedDescendantCount(winrt::TreeViewNode& parentNode)
//{
//	unsigned int allOpenedDescendantsCount = 0;
//	for (unsigned int i = 0; i < parentNode.Children().Size(); i++)
//	{
//		auto childNode = parentNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//		allOpenedDescendantsCount++;
//		if (childNode.IsExpanded())
//		{
//			allOpenedDescendantsCount += CountDescendants(childNode);
//		}
//	}
//	return allOpenedDescendantsCount;
//}

//bool ViewModel::IsNodeSelected(winrt::TreeViewNode const& targetNode)
//{
//	unsigned int index;
//	return m_selectedNodes.get().IndexOf(targetNode, index);
//}

//TreeNodeSelectionState ViewModel::NodeSelectionState(winrt::TreeViewNode const& targetNode)
//{
//	return winrt::get_self<TreeViewNode>(targetNode)->SelectionState();
//}

//void ViewModel::UpdateNodeSelection(winrt::TreeViewNode const& selectNode, TreeNodeSelectionState const& selectionState)
//{
//	auto node = winrt::get_self<TreeViewNode>(selectNode);
//	if (selectionState != node->SelectionState())
//	{
//		node->SelectionState(selectionState);
//		auto selectedNodes = winrt::get_self<SelectedTreeNodeVector>(m_selectedNodes.get());
//		switch (selectionState)
//		{
//			case TreeNodeSelectionState::Selected:
//				selectedNodes->InsertAtCore(selectedNodes->Size(), selectNode);
//				m_selectedNodeChildrenChangedEventTokenVector.push_back(winrt::get_self<TreeViewNode>(selectNode)->ChildrenChanged({ this, &ViewModel::SelectedNodeChildrenChanged }));
//				break;

//			case TreeNodeSelectionState::PartialSelected:
//			case TreeNodeSelectionState::UnSelected:
//				unsigned int index;
//				if (selectedNodes->IndexOf(selectNode, index))
//				{
//					selectedNodes->RemoveAtCore(index);
//					winrt::get_self<TreeViewNode>(selectNode)->ChildrenChanged(m_selectedNodeChildrenChangedEventTokenVector[index]);
//					m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + index);
//				}
//				break;
//		}
//	}
//}

//void ViewModel::UpdateSelection(winrt::TreeViewNode const& selectNode, TreeNodeSelectionState const& selectionState)
//{
//	if (NodeSelectionState(selectNode) != selectionState)
//	{
//		UpdateNodeSelection(selectNode, selectionState);

//		if (!IsInSingleSelectionMode())
//		{
//			UpdateSelectionStateOfDescendants(selectNode, selectionState);
//			UpdateSelectionStateOfAncestors(selectNode);
//		}
//	}
//}

//void ViewModel::UpdateSelectionStateOfDescendants(winrt::TreeViewNode const& targetNode, TreeNodeSelectionState const& selectionState)
//{
//	if (selectionState == TreeNodeSelectionState::PartialSelected) return;

//	for (auto const&childNode : targetNode.Children())
//    {
//		UpdateNodeSelection(childNode, selectionState);
//		UpdateSelectionStateOfDescendants(childNode, selectionState);
//		NotifyContainerOfSelectionChange(childNode, selectionState);
//	}
//}

//void ViewModel::UpdateSelectionStateOfAncestors(winrt::TreeViewNode const& targetNode)
//{
//	if (auto parentNode = targetNode.Parent())
//    {
//		// no need to update m_originalNode since it's the logical root for TreeView and not accessible to users
//		if (parentNode != m_originNode.safe_get())
//		{
//			auto previousState = NodeSelectionState(parentNode);
//			auto selectionState = SelectionStateBasedOnChildren(parentNode);

//			if (previousState != selectionState)
//			{
//				UpdateNodeSelection(parentNode, selectionState);
//				NotifyContainerOfSelectionChange(parentNode, selectionState);
//				UpdateSelectionStateOfAncestors(parentNode);
//			}
//		}
//	}
//}

//TreeNodeSelectionState ViewModel::SelectionStateBasedOnChildren(winrt::TreeViewNode const& node)
//{
//	bool hasSelectedChildren{ false };
//	bool hasUnSelectedChildren{ false };

//	for (auto const&childNode : node.Children())
//    {
//		auto state = NodeSelectionState(childNode);
//		if (state == TreeNodeSelectionState::Selected)
//		{
//			hasSelectedChildren = true;
//		}
//		else if (state == TreeNodeSelectionState::UnSelected)
//		{
//			hasUnSelectedChildren = true;
//		}

//		if ((hasSelectedChildren && hasUnSelectedChildren) ||
//			state == TreeNodeSelectionState::PartialSelected)
//		{
//			return TreeNodeSelectionState::PartialSelected;
//		}
//	}

//	return hasSelectedChildren ? TreeNodeSelectionState::Selected : TreeNodeSelectionState::UnSelected;
//}

//void ViewModel::NotifyContainerOfSelectionChange(winrt::TreeViewNode const& targetNode, TreeNodeSelectionState const& selectionState)
//{
//	if (m_TreeViewList)
//	{
//		auto container = winrt::get_self<TreeViewList>(m_TreeViewList.get())->ContainerFromNode(targetNode);
//		if (container)
//		{
//			winrt::TreeViewItem targetItem = container.as< winrt::TreeViewItem > ();
//			winrt::get_self<TreeViewItem>(targetItem)->UpdateSelectionVisual(selectionState);
//		}
//	}
//}

//winrt::IVector<winrt::TreeViewNode> ViewModel::GetSelectedNodes()
//{
//	return m_selectedNodes.get();
//}

//winrt::IVector<winrt::IInspectable> ViewModel::GetSelectedItems()
//{
//	return m_selectedItems.get();
//}

//winrt::TreeViewNode ViewModel::GetAssociatedNode(winrt::IInspectable item)
//{
//	return m_itemToNodeMap.get().Lookup(item);
//}

//bool ViewModel::IndexOfNode(winrt::TreeViewNode const& targetNode, uint32_t& index)
//{
//	return GetVectorInnerImpl()->IndexOf(targetNode, index);
//}

//void ViewModel::TreeViewNodeVectorChanged(winrt::TreeViewNode const& sender, winrt::IInspectable const& args)
//{
//	winrt::CollectionChange collectionChange = args.as< winrt::IVectorChangedEventArgs > ().CollectionChange();
//	unsigned int index = args.as< winrt::IVectorChangedEventArgs > ().Index();

//	switch (collectionChange)
//	{
//		// Reset case, commonly seen when a TreeNode is cleared.
//		// removes all nodes that need removing then 
//		// toggles a collapse / expand to ensure order.
//		case (winrt::CollectionChange::Reset):
//			{
//				auto resetNode = sender.as< winrt::TreeViewNode > ();
//				if (resetNode.IsExpanded())
//				{
//					//The lowIndex is the index of the first child, while the high index is the index of the last descendant in the list.
//					unsigned int lowIndex = GetNextIndexInFlatTree(resetNode);
//					unsigned int highIndex = IndexOfNextSibling(resetNode) - 1;
//					RemoveNodesAndDescendentsWithFlatIndexRange(lowIndex, highIndex);

//					// reset the status of resetNodes children
//					CollapseNode(resetNode);
//					ExpandNode(resetNode);
//				}

//				break;
//			}

//		// We will find the correct index of insertion by first checking if the
//		// node we are inserting into is expanded. If it is we will start walking
//		// down the tree and counting the open items. This is to ensure we place
//		// the inserted item in the correct index. If along the way we bump into
//		// the item being inserted, we insert there then return, because we don't
//		// need to do anything further.
//		case (winrt::CollectionChange::ItemInserted):
//			{
//				auto targetNode = sender.as< winrt::TreeViewNode > ().Children().GetAt(index).as< winrt::TreeViewNode > ();

//				if (IsContentMode())
//				{
//					m_itemToNodeMap.get().Insert(targetNode.Content(), targetNode);
//				}

//				auto parentNode = targetNode.Parent();
//				unsigned int nextNodeIndex = GetNextIndexInFlatTree(parentNode);
//				int allOpenedDescendantsCount = 0;

//				if (parentNode.IsExpanded())
//				{
//					for (unsigned int i = 0; i < parentNode.Children().Size(); i++)
//					{
//						auto childNode = parentNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//						if (childNode == targetNode)
//						{
//							AddNodeToView(targetNode, nextNodeIndex + i + allOpenedDescendantsCount);
//							if (targetNode.IsExpanded())
//							{
//								AddNodeDescendantsToView(targetNode, nextNodeIndex + i, allOpenedDescendantsCount);
//							}
//						}
//						else if (childNode.IsExpanded())
//						{
//							allOpenedDescendantsCount += CountDescendants(childNode);
//						}
//					}
//				}

//				break;
//			}

//		// Removes a node from the ViewModel when a TreeNode
//		// removes a child.
//		case (winrt::CollectionChange::ItemRemoved):
//			{
//				auto removingNodeParent = sender.as< winrt::TreeViewNode > ();
//				if (removingNodeParent.IsExpanded())
//				{
//					auto removedNode = GetRemovedChildTreeViewNodeByIndex(removingNodeParent, index);
//					RemoveNodeAndDescendantsFromView(removedNode);
//					if (IsContentMode())
//					{
//						m_itemToNodeMap.get().Remove(removedNode.Content());
//					}
//				}

//				break;
//			}

//		// Triggered by a replace such as SetAt.
//		// Updates the TreeNode that changed in the ViewModel.
//		case (winrt::CollectionChange::ItemChanged):
//			{
//				auto targetNode = sender.as< winrt::TreeViewNode > ().Children().GetAt(index).as< winrt::TreeViewNode > ();
//				auto changingNodeParent = sender.as< winrt::TreeViewNode > ();
//				if (changingNodeParent.IsExpanded())
//				{
//					auto removedNode = GetRemovedChildTreeViewNodeByIndex(changingNodeParent, index);
//					unsigned int removedNodeIndex = 0;
//					MUX_ASSERT(IndexOfNode(removedNode, removedNodeIndex));

//					RemoveNodeAndDescendantsFromView(removedNode);
//					InsertAt(removedNodeIndex, targetNode.as< winrt::IInspectable > ());

//					if (IsContentMode())
//					{
//						m_itemToNodeMap.get().Remove(removedNode.Content());
//						m_itemToNodeMap.get().Insert(targetNode.Content(), targetNode);
//					}
//				}

//				break;
//			}
//	}
//}

//void ViewModel::SelectedNodeChildrenChanged(winrt::TreeViewNode const& sender, winrt::IInspectable const& args)
//{
//	winrt::CollectionChange collectionChange = args.as< winrt::IVectorChangedEventArgs > ().CollectionChange();
//	unsigned int index = args.as< winrt::IVectorChangedEventArgs > ().Index();
//	auto changingChildrenNode = sender.as< winrt::TreeViewNode > ();

//	switch (collectionChange)
//	{
//		case (winrt::CollectionChange::ItemInserted):
//			{
//				auto newNode = changingChildrenNode.Children().GetAt(index);
//				UpdateNodeSelection(newNode, NodeSelectionState(changingChildrenNode));
//				break;
//			}

//		case (winrt::CollectionChange::ItemChanged):
//			{
//				auto newNode = changingChildrenNode.Children().GetAt(index);
//				UpdateNodeSelection(newNode, NodeSelectionState(changingChildrenNode));

//				auto selectedNodes = winrt::get_self<SelectedTreeNodeVector>(m_selectedNodes.get());
//				for (unsigned int i = 0; i < selectedNodes->Size(); i++)
//				{
//					auto selectNode = selectedNodes->GetAt(i);
//					auto ancestorNode = selectNode.Parent();
//					while (ancestorNode && ancestorNode.Parent())
//					{
//						ancestorNode = ancestorNode.Parent();
//					}

//					if (ancestorNode != m_originNode.get())
//					{
//						selectedNodes->RemoveAtCore(i);
//						m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + i);
//					}
//				}
//				break;
//			}

//		case (winrt::CollectionChange::ItemRemoved):
//		case (winrt::CollectionChange::Reset):
//			{
//				//This checks if there are still children, then re-evaluates parents selection based on current state of remaining children
//				//If a node has 2 children selected, and 1 unselected, and the unselected is removed, we then change the parent node to selected.
//				//If the last child is removed, we preserve the current selection state of the parent, and this code need not execute.
//				if (changingChildrenNode.Children().Size() > 0)
//				{
//					auto firstChildNode = changingChildrenNode.Children().GetAt(0);
//					UpdateSelectionStateOfAncestors(firstChildNode);
//				}

//				auto selectedNodes = winrt::get_self<SelectedTreeNodeVector>(m_selectedNodes.get());
//				for (unsigned int i = 0; i < selectedNodes->Size(); i++)
//				{
//					auto selectNode = selectedNodes->GetAt(i);
//					auto ancestorNode = selectNode.Parent();
//					while (ancestorNode && ancestorNode.Parent())
//					{
//						ancestorNode = ancestorNode.Parent();
//					}

//					if (ancestorNode != m_originNode.get())
//					{
//						selectedNodes->RemoveAtCore(i);
//						m_selectedNodeChildrenChangedEventTokenVector.erase(m_selectedNodeChildrenChangedEventTokenVector.begin() + i);
//					}
//				}
//				break;
//			}

//	}
//}

//void ViewModel::TreeViewNodePropertyChanged(winrt::TreeViewNode const& sender, winrt::IDependencyPropertyChangedEventArgs const& args)
//{
//	winrt::IDependencyProperty property = args.Property();
//	if (property == TreeViewNode::s_IsExpandedProperty)
//	{
//		TreeViewNodeIsExpandedPropertyChanged(sender, args);
//	}
//	else if (property == TreeViewNode::s_HasChildrenProperty)
//	{
//		TreeViewNodeHasChildrenPropertyChanged(sender, args);
//	}
//}

//void ViewModel::TreeViewNodeIsExpandedPropertyChanged(winrt::TreeViewNode const& sender, winrt::IDependencyPropertyChangedEventArgs const& args)
//{
//	auto targetNode = sender.as< winrt::TreeViewNode > ();
//	if (targetNode.IsExpanded())
//	{
//		if (targetNode.Children().Size() != 0)
//		{
//			int openedDescendantOffset = 0;
//			unsigned int index;
//			IndexOfNode(targetNode, index);
//			index = index + 1;
//			for (unsigned int i = 0; i < targetNode.Children().Size(); i++)
//			{
//				winrt::TreeViewNode childNode{ nullptr };
//				childNode = targetNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//				AddNodeToView(childNode, index + i + openedDescendantOffset);
//				openedDescendantOffset = AddNodeDescendantsToView(childNode, index + i, openedDescendantOffset);
//			}
//		}

//		//Notify TreeView that a node is being expanded.
//		m_nodeExpandingEventSource(targetNode, nullptr);
//	}
//	else
//	{
//		for (unsigned int i = 0; i < targetNode.Children().Size(); i++)
//		{
//			winrt::TreeViewNode childNode{ nullptr };
//			childNode = targetNode.Children().GetAt(i).as< winrt::TreeViewNode > ();
//			RemoveNodeAndDescendantsFromView(childNode);
//		}

//		//Notife TreeView that a node is being collapsed
//		m_nodeCollapsedEventSource(targetNode, nullptr);
//	}
//}

//void ViewModel::TreeViewNodeHasChildrenPropertyChanged(winrt::TreeViewNode const& sender, winrt::IDependencyPropertyChangedEventArgs const& args)
//{
//	if (m_TreeViewList)
//	{
//		auto targetNode = sender.as< winrt::TreeViewNode > ();
//		auto container = winrt::get_self<TreeViewList>(m_TreeViewList.get())->ContainerFromNode(targetNode);
//		if (container)
//		{
//			winrt::TreeViewItem targetItem = container.as< winrt::TreeViewItem > ();
//			targetItem.GlyphOpacity(targetNode.HasChildren() ? 1.0 : 0.0);
//		}
//	}
//}

//void ViewModel::IsContentMode(const bool value)
//{
//	m_isContentMode = value;
//}

//bool ViewModel::IsContentMode()
//{
//	return m_isContentMode;
//}

//void ViewModel::ClearEventTokenVectors()
//{
//	// Remove ChildrenChanged and ExpandedChanged events
//	auto inner = GetVectorInnerImpl();
//	for (uint32_t i = 0; i < Size(); i++)
//	{
//		if (auto current = inner->SafeGetAt(i))
//        {
//		auto tvnCurrent = winrt::get_self<TreeViewNode>(current.as< winrt::TreeViewNode > ());
//		tvnCurrent->ChildrenChanged(m_collectionChangedEventTokenVector.at(i));
//		tvnCurrent->RemoveExpandedChanged(m_IsExpandedChangedEventTokenVector.at(i));
//	}
//}

//    // Remove SelectedNodeChildrenChangtedEvent
//    if (auto selectedNodes = m_selectedNodes.safe_get())
//    {
//        for (uint32_t i = 0; i<selectedNodes.Size(); i++)
//        {
//            if (auto current = selectedNodes.GetAt(i))
//            {
//                auto node = winrt::get_self<TreeViewNode>(current.as< winrt::TreeViewNode > ());
//node->ChildrenChanged(m_selectedNodeChildrenChangedEventTokenVector[i]);
//            }
//        }
//    }

//    // Clear token vectors
//    m_collectionChangedEventTokenVector.clear();
//    m_IsExpandedChangedEventTokenVector.clear();
//    m_selectedNodeChildrenChangedEventTokenVector.clear();
//}
        
    }
}
