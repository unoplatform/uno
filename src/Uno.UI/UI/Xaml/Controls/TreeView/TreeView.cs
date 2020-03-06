using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input.ForceFeedback;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeView : Control
	{
		private TreeViewNode m_rootNode;
		private IList<TreeViewNode> m_pendingSelectedNodes;
		private TreeViewList m_listControl;

		public TreeView()
		{
			DefaultStyleKey = typeof(TreeView);

			m_rootNode = new TreeViewNode();
			m_pendingSelectedNodes = new List<TreeViewNode>();
		}

		internal TreeViewList ListControl { get => m_listControl; private set => m_listControl = value; }

		public IList<TreeViewNode> RootNodes => m_rootNode.Children;

		public object ItemFromContainer(DependencyObject container) =>
			ListControl?.ItemFromContainer(container);

		public DependencyObject ContainerFromItem(object item) =>
			ListControl?.ContainerFromItem(item);

		public TreeViewNode NodeFromContainer(DependencyObject container) =>
			ListControl?.NodeFromContainer(container);

		public DependencyObject ContainerFromNode(TreeViewNode node) =>
			ListControl?.ContainerFromNode(node);

		public TreeViewNode SelectedNode
		{
			get
			{
				return SelectedNodes.Count > 0 ? SelectedNodes[0] : null;
			}
			set
			{
				if (SelectedNodes.Count > 0)
				{
					SelectedNodes.Clear();
				}
				if (value != null)
				{
					SelectedNodes.Add(value);
				}
			}
		}

		public IList<TreeViewNode> SelectedNodes
		{
			get
			{
				return ListControl?.ListViewModel?.SelectedNodes ??
					m_pendingSelectedNodes; // we'll treat the pending selected nodes as SelectedNodes value if we don't have a list control or a view model
			}
		}

		public object SelectedItem
		{
			get
			{
				return SelectedItems.Count > 0 ? SelectedItems[0] : null;
			}
			set
			{
				if (SelectedItems.Count > 0)
				{
					SelectedItems.Clear();
				}
				if (value != null)
				{
					SelectedItems.Add(value);
				}
			}
		}

		public IList<object> SelectedItems => ListControl?.ListViewModel?.SelectedItems;

		internal void UpdateSelection(TreeViewNode node, bool isSelected)
		{
			var viewModel = ListControl?.ListViewModel;
			if (viewModel != null)
			{
				if (isSelected != viewModel.IsNodeSelected(node))
				{
					var selectedNodes = viewModel.SelectedNodes;
					if (isSelected)
					{
						if (SelectionMode == TreeViewSelectionMode.Single && selectedNodes.Count > 0)
						{
							selectedNodes.Clear();
						}
						selectedNodes.Append(node);
					}
					else
					{
						var index = selectedNodes.IndexOf(node);
						if (index > -1)
						{
							selectedNodes.RemoveAt(index);
						}
					}
				}
			}
		}

		public void Expand(TreeViewNode node)
		{
			var vm = ListControl.ListViewModel;
			vm.ExpandNode(node);
		}

		public void Collapse(TreeViewNode node)
		{
			var vm = ListControl.ListViewModel;
			vm.CollapseNode(node);
		}

		public void SelectAll()
		{
			var vm = ListControl.ListViewModel;
			vm.SelectAll();
		}

		private void OnItemClick(object sender, ItemClickEventArgs args)
		{
			var itemInvokedArgs = new TreeViewItemInvokedEventArgs(args.ClickedItem);
			//TODO: m_itemInvokedEventSource(*this, *itemInvokedArgs);
		}

		private void OnContainerContentChanging(object sender, ContainerContentChangingEventArgs args)
		{
			//	m_containerContentChangedSource(sender.as< winrt::ListView > (), args);
		}

		private void OnNodeExpanding(TreeViewNode sender, EventArgs args)
		{
			var treeViewExpandingEventArgs = new TreeViewExpandingEventArgs(sender);

			if (ListControl != null)
			{
				if (ContainerFromNode(sender) is TreeViewItem expandingTvi)
				{
					if (!expandingTvi.IsExpanded)
					{
						expandingTvi.IsExpanded = true;
					}

					//Update TemplateSettings properties
					var templateSettings = expandingTvi.TreeViewItemTemplateSettings;
					templateSettings.ExpandedGlyphVisibility = Visibility.Visible;
					templateSettings.CollapsedGlyphVisibility = Visibility.Collapsed;
				}
				//TODO: m_expandingEventSource(*this, *treeViewExpandingEventArgs);
			}
		}

		private void OnNodeCollapsed(TreeViewNode sender, EventArgs args)
		{
			var treeViewCollapsedEventArgs = new TreeViewCollapsedEventArgs(sender);

			if (ListControl != null)
			{
				if (ContainerFromNode(sender) is TreeViewItem collapsedTvi)
				{
					//Update TVI properties
					if (collapsedTvi.IsExpanded)
					{
						collapsedTvi.IsExpanded = false;
					}

					//Update TemplateSettings properties
					var templateSettings = collapsedTvi.TreeViewItemTemplateSettings;
					templateSettings.ExpandedGlyphVisibility = Visibility.Collapsed;
					templateSettings.CollapsedGlyphVisibility = Visibility.Visible;
				}
				//TODO: m_collapsedEventSource(*this, *treeViewCollapsedEventArgs);
			}
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == SelectionModeProperty && ListControl != null)
			{
				TreeViewSelectionMode value = SelectionMode;
				switch (value)
				{
					case TreeViewSelectionMode.None:
						{
							ListControl.SelectionMode = ListViewSelectionMode.None;
							UpdateItemsSelectionMode(false);
						}
						break;

					case TreeViewSelectionMode.Single:
						{
							ListControl.SelectionMode = ListViewSelectionMode.Single;
							UpdateItemsSelectionMode(false);
						}
						break;

					case TreeViewSelectionMode.Multiple:
						{
							ListControl.SelectionMode = ListViewSelectionMode.None;
							UpdateItemsSelectionMode(true);
						}
						break;

				}
			}
			else if (property == ItemsSourceProperty)
			{
				m_rootNode.IsContentMode = true;

				if (ListControl != null)
				{
					var viewModel = ListControl.ListViewModel;
					viewModel.IsContentMode = true;
				}

				m_rootNode.ItemsSource = ItemsSource;
			}
		}

		private void OnListControlDragItemsStarting(object sender, DragItemsStartingEventArgs args)
		{
			var treeViewArgs = new TreeViewDragItemsStartingEventArgs(args);
			//TODO: m_dragItemsStartingEventSource(*this, *treeViewArgs);
		}

		private void OnListControlDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
		{
			//		const auto newParent = [items = args.Items(), listControl = ListControl(), rootNode = m_rootNode.get()]()


			//	{
			//			if (listControl && items && items.Size() > 0)
			//			{
			//				if (const auto draggedNode = listControl->NodeFromItem(items.GetAt(0)))
			//            {
			//					const auto parentNode = draggedNode.Parent();
			//					if (parentNode && parentNode != rootNode)
			//					{
			//						return listControl->ItemFromNode(parentNode);
			//					}
			//				}
			//			}
			//			return static_cast<winrt::IInspectable>(nullptr);
			//		} ();

			//		const auto treeViewArgs = winrt::make_self<TreeViewDragItemsCompletedEventArgs>(args, newParent);
			//		m_dragItemsCompletedEventSource(*this, *treeViewArgs);
		}


		private void UpdateItemsSelectionMode(bool isMultiSelect)
		{
			var listControl = ListControl;
			listControl.EnableMultiselect(isMultiSelect);

			var viewModel = listControl.ListViewModel;
			//int size = viewModel->Size();

			//			for (int i = 0; i < size; i++)
			//			{
			//				auto updateContainer = listControl->ContainerFromIndex(i).as< winrt::TreeViewItem > ();
			//				if (updateContainer)
			//				{
			//					if (isMultiSelect)
			//					{
			//						if (auto targetNode = viewModel->GetNodeAt(i))
			//                {
			//				if (viewModel->IsNodeSelected(targetNode))
			//				{
			//					winrt::VisualStateManager::GoToState(updateContainer, L"TreeViewMultiSelectEnabledSelected", false);
			//				}
			//				else
			//				{
			//					winrt::VisualStateManager::GoToState(updateContainer, L"TreeViewMultiSelectEnabledUnselected", false);
			//				}
			//			}
			//		}
			//            else
			//            {
			//                winrt::VisualStateManager::GoToState(updateContainer, L"TreeViewMultiSelectDisabled", false);
			//            }
			//}
			//    }
		}

		protected override void OnApplyTemplate()
		{
			ListControl = GetTemplateChild(c_listControlName);
			if (ListControl != null)
			{
				var listPtr = m_listControl;
				var viewModel = listPtr.ListViewModel();
				if (m_rootNode == null)
				{
					m_rootNode = new TreeViewNode();
				}

				if (ItemsSource != null)
				{
					viewModel.IsContentMode = true;
				}
				viewModel.PrepareView(m_rootNode);
				viewModel.SetOwningList(ListControl);
				viewModel.NodeExpanding({ this, &TreeView::OnNodeExpanding });
				viewModel.NodeCollapsed({ this, &TreeView::OnNodeCollapsed });

				var selectionMode = SelectionMode;
				if (selectionMode == TreeViewSelectionMode.Single)
				{
					ListControl.SelectionMode = ListViewSelectionMode.Single;
				}
				else
				{
					ListControl.SelectionMode = ListViewSelectionMode.None;
					if (selectionMode == TreeViewSelectionMode.Multiple)
					{
						UpdateItemsSelectionMode(true);
					}
				}

				m_itemClickRevoker = listControl.ItemClick(winrt::auto_revoke, { this, &TreeView::OnItemClick });
				m_containerContentChangingRevoker = listControl.ContainerContentChanging(winrt::auto_revoke, { this, &TreeView::OnContainerContentChanging });
				m_dragItemsStartingRevoker = listControl.DragItemsStarting(winrt::auto_revoke, { this, &TreeView::OnListControlDragItemsStarting });
				m_dragItemsCompletedRevoker = listControl.DragItemsCompleted(winrt::auto_revoke, { this, &TreeView::OnListControlDragItemsCompleted });

				if (m_pendingSelectedNodes && m_pendingSelectedNodes.get().Size() > 0)
				{
					auto selectedNodes = viewModel->GetSelectedNodes();
					for (auto const&node : m_pendingSelectedNodes.get())
				            {
						selectedNodes.Append(node);
					}
					m_pendingSelectedNodes.get().Clear();
				}
			}
		}
	}
}
