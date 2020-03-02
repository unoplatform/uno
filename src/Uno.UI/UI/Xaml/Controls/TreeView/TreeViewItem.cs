using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;
using TreeNodeSelectionState = Windows.UI.Xaml.Controls.TreeViewNode.TreeNodeSelectionState;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewItem
	{
		private bool m_expansionCycled;
		private CheckBox m_selectionBox;

		public TreeViewItem()
		{
			DefaultStyleKey = typeof(TreeViewItem);
			SetValue(TreeViewItemTemplateSettingsProperty, new TreeViewItemTemplateSettings());
		}

		protected override void OnKeyDown(KeyRoutedEventArgs e)
		{
			var targetNode = TreeNode;
			if (targetNode != null)
			{
				var treeView = AncestorTreeView;
				var key = e.Key;
				var originalKey = e.OriginalKey;

				if (originalKey == VirtualKey.GamepadA &&
					treeView.ListControl.IsMultiselect)
				{
					HandleGamepadAInMultiselectMode(targetNode);
					e.Handled = true;
				}
			}
			//		if (auto targetNode = TreeNode())
			//    {
			//			auto treeView = AncestorTreeView();
			//			auto key = e.Key();
			//			auto originalKey = e.OriginalKey();

			//			// If in multi-selection and gamepad a is pressed
			//			if (originalKey == winrt::VirtualKey::GamepadA &&
			//				treeView->ListControl()->IsMultiselect())
			//			{
			//				HandleGamepadAInMultiselectMode(targetNode);
			//				e.Handled(true);
			//			}
			//			else if (IsInReorderMode(key))
			//			{
			//				HandleReorder(key);
			//				e.Handled(true);
			//			}
			//			// Collapse/Expand
			//			else if (IsExpandCollapse(key))
			//			{
			//				auto handled = HandleExpandCollapse(key);
			//				if (handled)
			//				{
			//					e.Handled(true);
			//				}
			//			}
			//			else if (key == winrt::VirtualKey::Space &&
			//					 treeView->ListControl()->IsMultiselect())
			//			{
			//				auto selectionBox = m_selectionBox.get();
			//				bool isSelected = CheckBoxSelectionState(selectionBox) == TreeNodeSelectionState::Selected;
			//				selectionBox.IsChecked(!isSelected);
			//				e.Handled(true);
			//			}
			//		}

			// Call to base for all other key presses
			base.OnKeyDown(e);
		}

		protected override void OnDrop(DragEventArgs args)
		{
			//		if (!args.Handled() && args.AcceptedOperation() == winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move)
			//		{
			//			winrt::TreeViewItem droppedOnItem = *this;
			//			auto treeView = AncestorTreeView();
			//			if (treeView)
			//			{
			//				auto treeViewList = treeView->ListControl();
			//				if (auto droppedNode = treeViewList->DraggedTreeViewNode())
			//            {
			//					if (treeViewList->IsMutiSelectWithSelectedItems())
			//					{
			//						auto droppedOnNode = treeView->NodeFromContainer(droppedOnItem);
			//						auto selectedRoots = treeViewList->GetRootsOfSelectedSubtrees();
			//						for (auto & node : selectedRoots)
			//						{
			//							auto nodeIndex = treeViewList->FlatIndex(node);
			//							if (treeViewList->IsFlatIndexValid(nodeIndex))
			//							{
			//								treeViewList->RemoveNodeFromParent(node);
			//								winrt::get_self<TreeViewNodeVector>(droppedOnNode.Children())->Append(node);
			//							}
			//						}

			//						args.Handled(true);
			//						treeViewList->OnDrop(args);
			//					}
			//					else
			//					{
			//						auto droppedOnNode = treeView->NodeFromContainer(droppedOnItem);

			//						// Remove the item that was dragged
			//						unsigned int removeIndex;
			//						droppedNode.Parent().Children().IndexOf(droppedNode, removeIndex);

			//						if (droppedNode != droppedOnNode)
			//						{
			//							winrt::get_self<TreeViewNodeVector>(droppedNode.Parent().Children())->RemoveAt(removeIndex);

			//							// Append the dragged dropped item as a child of the node it was dropped onto
			//							winrt::get_self<TreeViewNodeVector>(droppedOnNode.Children())->Append(droppedNode);

			//							// If not set to true then the Reorder code of listview will override what is being done here.
			//							args.Handled(true);
			//							treeViewList->OnDrop(args);
			//						}
			//						else
			//						{
			//							args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::None);
			//						}

			//					}
			//				}
			//			}
			base.OnDrop(args);
		}

		protected override void OnDragOver(DragEventArgs args)
		{
			//		auto treeView = AncestorTreeView();
			//		if (treeView && !args.Handled())
			//		{
			//			auto treeViewList = treeView->ListControl();
			//			winrt::TreeViewItem draggedOverItem = *this;
			//			winrt::TreeViewNode draggedOverNode = treeView->NodeFromContainer(draggedOverItem);
			//			winrt::TreeViewNode draggedNode = treeViewList->DraggedTreeViewNode();

			//			if (draggedNode && treeView->CanReorderItems())
			//			{
			//				if (treeViewList->IsMutiSelectWithSelectedItems())
			//				{
			//					if (treeViewList->IsSelected(draggedOverNode))
			//					{
			//						args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::None);
			//						treeViewList->SetDraggedOverItem(nullptr);
			//					}
			//					else
			//					{
			//						args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move);
			//						treeViewList->SetDraggedOverItem(draggedOverItem);
			//					}
			//				}
			//				else
			//				{
			//					winrt::TreeViewNode walkNode = draggedOverNode.Parent();
			//					while (walkNode && walkNode != draggedNode)
			//					{
			//						walkNode = walkNode.Parent();
			//					}

			//					if (walkNode != draggedNode && draggedNode != draggedOverNode)
			//					{
			//						args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move);
			//						treeViewList->SetDraggedOverItem(draggedOverItem);
			//					}

			//					treeViewList->UpdateDropTargetDropEffect(false, false, nullptr);
			//				}
			//			}
			//		}

			base.OnDragOver(args);
		}

		protected override void OnDragEnter(DragEventArgs args)
		{


			//winrt::TreeViewItem draggedOverItem = *this;

			//args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::None);
			//args.DragUIOverride().IsGlyphVisible(true);

			//auto treeView = AncestorTreeView();
			//if (treeView && treeView->CanReorderItems() && !args.Handled())
			//{
			//	auto treeViewList = treeView->ListControl();
			//	winrt::TreeViewNode draggedNode = treeViewList->DraggedTreeViewNode();
			//	if (draggedNode)
			//	{
			//		winrt::TreeViewNode draggedOverNode = treeView->NodeFromContainer(draggedOverItem);
			//		winrt::TreeViewNode walkNode = draggedOverNode.Parent();

			//		while (walkNode && walkNode != draggedNode)
			//		{
			//			walkNode = walkNode.Parent();
			//		}

			//		if (walkNode != draggedNode && draggedNode != draggedOverNode)
			//		{
			//			args.AcceptedOperation(winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move);
			//		}

			//		winrt::TreeViewNode droppedOnNode = TreeNode();
			//		if (droppedOnNode != draggedNode) // Don't expand if drag hovering on itself.
			//		{
			//			// Set up timer.
			//			if (!draggedOverNode.IsExpanded() && draggedOverNode.HasChildren())
			//			{
			//				if (m_expandContentTimer)
			//				{
			//					auto expandContentTimer = m_expandContentTimer.get();
			//					expandContentTimer.Stop();
			//					expandContentTimer.Start();
			//				}
			//				else
			//				{
			//					// Initialize timer.
			//					winrt::TimeSpan interval = winrt::TimeSpan::duration(c_dragOverInterval);
			//					auto expandContentTimer = winrt::DispatcherTimer();
			//					m_expandContentTimer.set(expandContentTimer);
			//					expandContentTimer.Interval(interval);
			//					expandContentTimer.Tick({ this, &TreeViewItem::OnExpandContentTimerTick });
			//					expandContentTimer.Start();
			//				}
			//			}
			//		}
			//	}
			//}

			base.OnDragEnter(args);
		}

		protected override void OnDragLeave(DragEventArgs args)
		{
			//		if (!args.Handled())
			//		{
			//			if (auto treeView = AncestorTreeView())
			//        {
			//				auto treeViewList = treeView->ListControl();
			//				treeViewList->SetDraggedOverItem(nullptr);
			//			}

			//			if (m_expandContentTimer)
			//			{
			//				m_expandContentTimer.get().Stop();
			//			}
			//		}
			base.OnDragLeave(args);
		}

		protected override AutomationPeer OnCreateAutomationPeer() =>
			new TreeViewItemAutomationPeer(this);

		// IFrameworkElementOverrides

		protected override void OnApplyTemplate()
		{
			//		RecycleEvents();

			//		winrt::IControlProtected controlProtected = *this;
			//		m_selectionBox.set(GetTemplateChildT<winrt::CheckBox>(c_multiSelectCheckBoxName, controlProtected));
			//		RegisterPropertyChangedCallback(winrt::SelectorItem::IsSelectedProperty(), { this, &TreeViewItem::OnIsSelectedChanged });

			//		if (m_selectionBox)
			//		{
			//			m_checkedEventRevoker = m_selectionBox.get().Checked(winrt::auto_revoke, { this, &TreeViewItem::OnCheckToggle });
			//			m_uncheckedEventRevoker = m_selectionBox.get().Unchecked(winrt::auto_revoke, { this, &TreeViewItem::OnCheckToggle });
			//		}

			//		auto chevron = GetTemplateChildT<winrt::UIElement>(c_expandCollapseChevronName, controlProtected);
			//		if (chevron)
			//		{
			//			m_expandCollapseChevronPointerPressedToken = chevron.PointerPressed({ this, &TreeViewItem::OnExpandCollapseChevronPointerPressed });
			//			m_expandCollapseChevron.set(chevron);
			//		}
			//		auto node = TreeNode();
			//		if (node && IsInContentMode())
			//		{
			//			UpdateNodeIsExpandedAsync(node, IsExpanded());
			//			node.HasUnrealizedChildren(HasUnrealizedChildren());
			//		}
			base.OnApplyTemplate();
		}

		//	TreeViewItem::~TreeViewItem()
		//	{
		//		// We only need to use safe_get in the deconstruction loop
		//		RecycleEvents(true /* useSafeGet */);
		//	}

		//	template<typename T>
		//	T TreeViewItem::GetAncestorView()
		//{
		//    winrt::DependencyObject treeViewItemAncestor = *this;
		//	T ancestorView = nullptr;
		//    while (treeViewItemAncestor && !ancestorView)
		//    {
		//        treeViewItemAncestor = winrt::VisualTreeHelper::GetParent(treeViewItemAncestor);
		//        ancestorView = treeViewItemAncestor.try_as<T>();
		//    }
		//    return ancestorView;
		//}

		public TreeView AncestorTreeView
		{
			get
			{
				//	if (!m_ancestorTreeView.get())
				//	{
				//		m_ancestorTreeView = winrt::make_weak<winrt::TreeView>(GetAncestorView<winrt::TreeView>());
				//	}
				//	return winrt::get_self<TreeView>(m_ancestorTreeView.get())->get_strong();
				throw new NotImplementedException();
			}
		}

		//void TreeViewItem::OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args)
		//{
		//	winrt::IDependencyProperty property = args.Property();
		//	if (auto node = TreeNode()) 
		//    {
		//		if (property == s_IsExpandedProperty)
		//		{
		//			bool value = unbox_value<bool>(args.NewValue());
		//			if (node.IsExpanded() != value)
		//			{
		//				UpdateNodeIsExpandedAsync(node, value);
		//			}
		//			RaiseExpandCollapseAutomationEvent(value);
		//		}
		//		else if (property == s_ItemsSourceProperty)
		//		{
		//			winrt::IInspectable value = args.NewValue();

		//			auto treeViewNode = winrt::get_self<TreeViewNode>(node);
		//			treeViewNode->ItemsSource(value);
		//			if (IsInContentMode())
		//			{
		//				// The children have changed, validate and update GlyphOpacity
		//				bool hasChildren = HasUnrealizedChildren() || treeViewNode->HasChildren();
		//				GlyphOpacity(hasChildren ? 1.0 : 0.0);
		//			}
		//		}
		//		else if (property == s_HasUnrealizedChildrenProperty)
		//		{
		//			bool value = unbox_value<bool>(args.NewValue());
		//			node.HasUnrealizedChildren(value);
		//		}
		//	}
		//}

		private void OnExpandContentTimerTick(object sender, EventArgs e)
		{
			//	if (m_expandContentTimer)
			//	{
			//		m_expandContentTimer.get().Stop();
			//	}

			//	if (auto draggedOverNode = TreeNode())
			//    {
			//		if (draggedOverNode && !draggedOverNode.IsExpanded())
			//		{
			//			draggedOverNode.IsExpanded(true);
			//		}
			//	}
		}

		//void TreeViewItem::RaiseExpandCollapseAutomationEvent(bool isExpanded)
		//{
		//	if (winrt::AutomationPeer::ListenerExists(winrt::AutomationEvents::PropertyChanged))
		//	{
		//		auto expandState = isExpanded ? winrt::ExpandCollapseState::Expanded : winrt::ExpandCollapseState::Collapsed;
		//		if (auto peer = winrt::FrameworkElementAutomationPeer::FromElement(*this))
		//        {
		//			auto treeViewItemPeer = peer.as< winrt::TreeViewItemAutomationPeer > ();
		//			winrt::get_self<TreeViewItemAutomationPeer>(treeViewItemPeer)->RaiseExpandCollapseAutomationEvent(expandState);
		//		}
		//	}
		//}

		//bool TreeViewItem::IsInReorderMode(winrt::VirtualKey key)
		//{
		//	auto ctrlState = winrt::CoreWindow::GetForCurrentThread().GetKeyState(winrt::VirtualKey::Control);
		//	bool isControlPressed = (ctrlState & winrt::CoreVirtualKeyStates::Down) == winrt::CoreVirtualKeyStates::Down;

		//	auto altState = winrt::CoreWindow::GetForCurrentThread().GetKeyState(winrt::VirtualKey::Menu);
		//	bool isAltPressed = (altState & winrt::CoreVirtualKeyStates::Down) == winrt::CoreVirtualKeyStates::Down;

		//	auto shiftState = winrt::CoreWindow::GetForCurrentThread().GetKeyState(winrt::VirtualKey::Shift);
		//	bool isShiftPressed = (shiftState & winrt::CoreVirtualKeyStates::Down) == winrt::CoreVirtualKeyStates::Down;

		//	bool isDirectionPressed = IsDirectionalKey(key);

		//	bool canReorderItems = false;
		//	if (auto treeView = AncestorTreeView())
		//    {
		//		canReorderItems = treeView->CanReorderItems();
		//	}

		//	return canReorderItems && isDirectionPressed && isShiftPressed && isAltPressed && !isControlPressed;
		//}

		private void UpdateTreeViewItemVisualState(TreeNodeSelectionState state)
		{
			if (state == TreeNodeSelectionState.Selected)
			{
				VisualStateManager.GoToState(this, "TreeViewMultiSelectEnabledSelected", false);
			}
			else
			{
				VisualStateManager.GoToState(this, "TreeViewMultiSelectEnabledUnselected", false);
			}
		}

		//void TreeViewItem::OnCheckToggle(winrt::IInspectable const& sender, winrt::RoutedEventArgs const& /*args*/)
		//{
		//	if (auto treeView = AncestorTreeView())
		//    {
		//		auto listControl = treeView->ListControl();
		//		int index = listControl->IndexFromContainer(*this);
		//		auto selectionState = CheckBoxSelectionState(sender.as< winrt::CheckBox > ());
		//		listControl->ListViewModel()->ModifySelectByIndex(index, selectionState);
		//		UpdateTreeViewItemVisualState(selectionState);
		//		RaiseSelectionChangeEvents(selectionState == TreeNodeSelectionState::Selected);
		//	}
		//}

		//void TreeViewItem::RaiseSelectionChangeEvents(bool isSelected)
		//{
		//	if (winrt::AutomationPeer peer = winrt::FrameworkElementAutomationPeer::FromElement(*this))
		//    {
		//		auto treeViewItemPeer = winrt::get_self<TreeViewItemAutomationPeer>(peer.as< winrt::TreeViewItemAutomationPeer > ());

		//		auto selectionEvent = isSelected ? winrt::AutomationEvents::SelectionItemPatternOnElementAddedToSelection : winrt::AutomationEvents::SelectionItemPatternOnElementRemovedFromSelection;
		//		treeViewItemPeer->RaiseAutomationEvent(selectionEvent);

		//		auto isSelectedProperty = winrt::SelectionItemPatternIdentifiers::IsSelectedProperty();
		//		treeViewItemPeer->RaisePropertyChangedEvent(isSelectedProperty, box_value(!isSelected).as< winrt::IReference<bool> > (), box_value(isSelected).as< winrt::IReference<bool> > ());
		//	}
		//}

		private void UpdateSelection(bool isSelected)
		{
			var treeView = AncestorTreeView;
			if (treeView != null)
			{
				var node = TreeNode;
				if (node != null)
				{
					treeView?.UpdateSelection(node, isSelected);
				}
			}
		}

		internal void UpdateSelectionVisual(TreeNodeSelectionState state)
		{
			var treeView = AncestorTreeView;
			if (treeView != null)
			{
				var listControl = treeView.ListControl;
				if (listControl.IsMultiselect)
				{
					UpdateMultipleSelection(state);
				}
				else
				{
					var node = TreeNode;
					if (node != null)
					{
						var viewModel = listControl.ListViewModel;
						var isNodeSelected = viewModel.IsNodeSelected(node);
						if (isNodeSelected != IsSelected)
						{
							IsSelected = isNodeSelected;
						}
					}
				}
			}
		}

		private void OnIsSelectedChanged(DependencyObject sender, DependencyProperty args)
		{
			UpdateSelection((bool)GetValue(args));
		}

		private void UpdateMultipleSelection(TreeViewNode.TreeNodeSelectionState state)
		{
			switch (state)
			{
				case TreeNodeSelectionState.Selected:
					m_selectionBox.IsChecked = true;
					break;
				case TreeNodeSelectionState.PartialSelected:
					m_selectionBox.IsChecked = null;
					break;
				case TreeNodeSelectionState.UnSelected:
					m_selectionBox.IsChecked = false;
					break;
			}
			UpdateTreeViewItemVisualState(state);
		}

		private bool IsSelectedInternal()
		{
			// Check Selector::IsChecked for single selection since we use
			// ListView's single selection. In multiple selection we roll our own.
			bool isSelected = IsSelected;
			var treeView = AncestorTreeView;
			if (treeView != null)
			{
				var listControl = treeView.ListControl;
				if (listControl != null && listControl.IsMultiselect)
				{
					var state = CheckBoxSelectionState(m_selectionBox);
					isSelected = state == TreeNodeSelectionState.Selected;
				}
			}

			return isSelected;
		}

		internal void UpdateIndentation(int depth)
		{
			var thickness = new Thickness(depth * 16, 0, 0, 0);

			var templateSettings = TreeViewItemTemplateSettings;
			templateSettings.Indentation = thickness;
		}

		private bool IsExpandCollapse(VirtualKey key)
		{
			var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
			bool isControlPressed = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			var altState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu);
			bool isAltPressed = (altState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
			bool isShiftPressed = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			bool isDirectionPressed = IsDirectionalKey(key);

			return (isDirectionPressed && !isShiftPressed && !isAltPressed && !isControlPressed);
		}

		private void ReorderItems(ListView listControl, TreeViewNode targetNode, int position, int childIndex, bool isForwards)
		{
			int positionModifier = isForwards ? 1 : -1;
			var skippedItem = listControl.ContainerFromIndex(position + positionModifier) as TreeViewItem;
			skippedItem?.Focus(FocusState.Keyboard);

			var parentNode = targetNode.Parent;
			var children = parentNode.Children;
			children.RemoveAt(childIndex);
			children.Insert(childIndex + positionModifier, targetNode);
			listControl.UpdateLayout();

			var lvi = AncestorTreeView.ContainerFromNode(targetNode) as TreeViewItem;
			if (lvi != null)
			{
				var targetItem = lvi;
				targetItem.Focus(FocusState.Keyboard);
				listControl.UpdateDropTargetDropEffect(false, false, targetItem);
				AutomationPeer ancestorPeer = FrameworkElementAutomationPeer.FromElement(listControl);
				ancestorPeer.RaiseAutomationEvent(AutomationEvents.Dropped);
			}
		}

		private void HandleGamepadAInMultiselectMode(TreeViewNode node)
		{
			if (node.HasChildren)
			{
				// Non-leaf node: cycle between selection and expansion
				if (!m_expansionCycled)
				{
					node.IsExpanded = !node.IsExpanded;
					m_expansionCycled = true;
				}
				else
				{
					m_expansionCycled = ToggleSelection();
				}
			}
			else
			{
				// Leaf node: toggle selection 
				ToggleSelection();
			}
		}

		private bool ToggleSelection()
		{
			var currentState = CheckBoxSelectionState(m_selectionBox);
			var newState = currentState == TreeNodeSelectionState.Selected ?
				TreeNodeSelectionState.UnSelected : TreeNodeSelectionState.Selected;
			UpdateMultipleSelection(newState);
			return newState == TreeNodeSelectionState.Selected;
		}

		private void HandleReorder(VirtualKey key)
		{
			TreeViewNode targetNode = TreeNode;

			TreeViewNode parentNode = targetNode.Parent;
			var listControl = AncestorTreeView.ListControl;
			int position = listControl.IndexFromContainer(this);
			if (key == VirtualKey.Up || key == VirtualKey.Left && position != 0)
			{
				int childIndex = parentNode.Children.IndexOf(targetNode);

				if (childIndex != 0)
				{
					if (targetNode.IsExpanded)
					{
						targetNode.IsExpanded = false;
					}

					ReorderItems(listControl, targetNode, position, childIndex, false);
				}
			}
			else if ((key == winrt::VirtualKey::Down || key == winrt::VirtualKey::Right) && (position != listControl->Items().Size() - 1))
			{
				unsigned int childIndex;
				parentNode.Children().IndexOf(targetNode, childIndex);
				if (childIndex != parentNode.Children().Size() - 1)
				{
					if (targetNode.IsExpanded())
					{
						targetNode.IsExpanded(false);
					}

					ReorderItems(*listControl, targetNode, position, childIndex, true);
				}
			}
		}

		//bool TreeViewItem::HandleExpandCollapse(winrt::VirtualKey key)
		//{
		//	auto treeView = AncestorTreeView();
		//	winrt::TreeViewNode targetNode = treeView->NodeFromContainer(*this);
		//	const auto flowDirectionReversed = (FlowDirection() == winrt::FlowDirection::RightToLeft);
		//	bool isExpanded = targetNode.IsExpanded();
		//	bool handled = false;

		//	// Inputs for Collapse/Move to parent
		//	if ((key == winrt::VirtualKey::Left && !flowDirectionReversed) ||
		//		(key == winrt::VirtualKey::Right && flowDirectionReversed))
		//	{
		//		if (isExpanded)// Is expanded : need to collapse
		//		{
		//			targetNode.IsExpanded(false);
		//			auto targetValue = treeView->ContainerFromNode(targetNode);
		//			if (targetValue)
		//			{
		//				auto targetItem = targetValue.as< winrt::TreeViewItem > ();
		//				targetItem.Focus(winrt::FocusState::Keyboard);
		//			}

		//			handled = true;
		//		}
		//		else // Is collapsed: need to select parent
		//		{
		//			if (auto parentNode = targetNode.Parent())
		//            {
		//				if (auto parentValue = treeView->ContainerFromNode(parentNode))
		//                {
		//					auto parentItem = parentValue.as< winrt::TreeViewItem > ();
		//					parentItem.Focus(winrt::FocusState::Keyboard);
		//					handled = true;
		//				}
		//			}
		//		}
		//	}
		//	// Inputs for Expand/Move to first child
		//	else if ((key == winrt::VirtualKey::Right && !flowDirectionReversed) ||
		//			 (key == winrt::VirtualKey::Left && flowDirectionReversed))
		//	{
		//		if (!isExpanded && targetNode.HasChildren()) // Is collapsed : need to expand
		//		{
		//			targetNode.IsExpanded(true);
		//			handled = true;
		//		}
		//		else if (targetNode.Children().Size() > 0) // Is expanded and has children: need to select first child (if applicable)
		//		{
		//			auto childNode = targetNode.Children().GetAt(0).as< winrt::TreeViewNode > ();
		//			auto childValue = treeView->ContainerFromNode(childNode);
		//			if (childValue)
		//			{
		//				auto childItem = childValue.as< winrt::TreeViewItem > ();
		//				childItem.Focus(winrt::FocusState::Keyboard);
		//				handled = true;
		//			}
		//		}
		//	}

		//	return handled;
		//}

		//void TreeViewItem::OnExpandCollapseChevronPointerPressed(
		//    const winrt::IInspectable& /*sender*/,
		//    const winrt::PointerRoutedEventArgs& args)
		//{
		//	winrt::TreeViewNode targetNode = TreeNode();
		//	auto isExpanded = !targetNode.IsExpanded();
		//	targetNode.IsExpanded(isExpanded);

		//	args.Handled(true);
		//}

		//void TreeViewItem::RecycleEvents(bool useSafeGet)
		//{
		//	if (auto chevron = useSafeGet ? m_expandCollapseChevron.safe_get() : m_expandCollapseChevron.get())
		//    {
		//		if (m_expandCollapseChevronPointerPressedToken.value)
		//		{
		//			chevron.PointerCanceled(m_expandCollapseChevronPointerPressedToken);
		//			m_expandCollapseChevronPointerPressedToken.value = 0;
		//		}
		//	}
		//}

		///* static */
		private static bool IsDirectionalKey(VirtualKey key)
		{
			return
				key == VirtualKey.Up ||
				key == VirtualKey.Down ||
				key == VirtualKey.Left ||
				key == VirtualKey.Right;
		}

		private TreeViewNode TreeNode => AncestorTreeView?.NodeFromContainer(this);

		////Setting IsExpanded changes the itemssource collection on the listview, which cannot be done during layout.
		////We schedule it on the dispatcher so that it runs after layout pass.
		//void TreeViewItem::UpdateNodeIsExpandedAsync(winrt::TreeViewNode const& node, bool isExpanded)
		//{
		//	auto dispatcher = winrt::Window::Current().Dispatcher();
		//	auto ignore = dispatcher.RunAsync(
		//		winrt::CoreDispatcherPriority::Normal,
		//		winrt::DispatchedHandler([node, isExpanded]()

		//	{
		//		node.IsExpanded(isExpanded);
		//	}));
		//}

		private bool IsInContentMode => AncestorTreeView.ListControl.ListViewModel.IsContentMode;

		private TreeNodeSelectionState CheckBoxSelectionState(CheckBox checkBox)
		{
			//	winrt::IReference<bool> winrtBool = checkBox.IsChecked();
			//	if (winrtBool)
			//	{
			//		if (winrtBool.Value())
			//		{
			//			return TreeNodeSelectionState::Selected;
			//		}

			//		return TreeNodeSelectionState::UnSelected;
			//	}

			return TreeNodeSelectionState.PartialSelected;
		}
	}
}
