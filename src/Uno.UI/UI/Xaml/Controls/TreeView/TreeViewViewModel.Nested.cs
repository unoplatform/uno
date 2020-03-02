using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.UI.Xaml.Controls.TreeView
{
	internal partial class TreeViewViewModel
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
	}
}
