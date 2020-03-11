using Windows.Foundation.Collections;
using TreeNodeSelectionState = Windows.UI.Xaml.Controls.TreeViewNode.TreeNodeSelectionState;

namespace Windows.UI.Xaml.Controls
{
	internal partial class TreeViewViewModel
	{
		// Need to update node selection states on UI before vector changes.
		// Listen on vector change events don't solve the problem because the event already happened when the event handler gets called.
		// i.e. the node is alreay gone when we get to ItemRemoved callback.
		private class SelectedTreeNodeVector : ObservableVector<TreeViewNode>
		{
			TreeViewViewModel m_viewModel = null;

			private void UpdateSelection(TreeViewNode node, TreeNodeSelectionState state)
			{
				if (node.SelectionState != state)
				{
					var viewModel = m_viewModel;

					if (viewModel != null)
					{
						viewModel.UpdateSelection(node, state);
						viewModel.NotifyContainerOfSelectionChange(node, state);
					}
				}
			}

			public void SetViewModel(TreeViewViewModel viewModel)
			{
				m_viewModel = viewModel;
			}

			// Default write methods will trigger TreeView visual updates.
			// If you want to update vector content without notifying TreeViewNodes, use "core" version of the methods.
			internal void InsertAtCore(int index, TreeViewNode node)
			{
				GetVectorInnerImpl().Insert(index, node);

				// Keep SelectedItems and SelectedNodes in sync
				var viewModel = m_viewModel;
				if (viewModel != null)
				{
					var selectedItems = viewModel.SelectedItems;
					if (selectedItems.Count != Count)
					{
						var listControl = viewModel.ListControl;
						if (listControl != null)
						{
							var item = listControl.ItemFromNode(node);
							if (item != null)
							{
								selectedItems.Insert(index, item);
							}
						}
					}
				}
			}

			internal void RemoveAtCore(int index)
			{
				GetVectorInnerImpl().RemoveAt(index);

				// Keep SelectedItems and SelectedNodes in sync
				var viewModel = m_viewModel;
				if (viewModel != null)
				{
					var selectedItems = viewModel.SelectedItems;
					if (Count != selectedItems.Count)
					{
						selectedItems.RemoveAt(index);
					}
				}
			}

			//void InsertAt(int index, TreeViewNode node)
			//{
			//	if (!Contains(node))
			//	{
			//		// UpdateSelection will call InsertAtCore
			//		UpdateSelection(node, TreeNodeSelectionState.Selected);
			//	}
			//}


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
			//    
			//}
			//    }

			//public:
			//   





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

		}



		//#pragma endregion

		//// Similiar to SelectedNodesVector above, we need to make decisions before the item is inserted or removed.
		//// we can't use vector change events because the event already happened when event hander gets called.
		//#pragma region SelectedItemsVector

		//typedef typename VectorOptionsFromFlag<winrt::IInspectable, MakeVectorParam<VectorFlag::Observable, VectorFlag::DependencyObjectBase>()> SelectedItemsVectorOptions;

		private class SelectedItemsVector : ObservableVector<object>
		{
			private TreeViewViewModel m_viewModel;

			public void SetViewModel(TreeViewViewModel viewModel)
			{
				m_viewModel = viewModel;
			}
		}

		
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
