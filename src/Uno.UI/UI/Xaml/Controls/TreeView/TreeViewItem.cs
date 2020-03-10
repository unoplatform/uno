using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
				else if (IsInReorderMode(key))
				{
					HandleReorder(key);
					e.Handled = true;
				}
				// Collapse/Expand
				else if (IsExpandCollapse(key))
				{
					var handled = HandleExpandCollapse(key);
					if (handled)
					{
						e.Handled = true;
					}
				}
				else if (key == VirtualKey.Space &&
						 treeView.ListControl.IsMultiselect)
				{
					var selectionBox = m_selectionBox;
					bool isSelected = CheckBoxSelectionState(selectionBox) == TreeNodeSelectionState.Selected;
					selectionBox.IsChecked = !isSelected;
					e.Handled = true;
				}
			}

			// Call to base for all other key presses
			base.OnKeyDown(e);
		}

		protected override void OnDrop(DragEventArgs args)
		{
			//		if (!args.Handled() && args.AcceptedOperation() == winrt::Windows::ApplicationModel::DataTransfer::DataPackageOperation::Move)
			//		{
			//			winrt::TreeViewItem droppedOnItem = *this;
			//			var treeView = AncestorTreeView();
			//			if (treeView)
			//			{
			//				var treeViewList = treeView->ListControl();
			//				if (var droppedNode = treeViewList->DraggedTreeViewNode())
			//            {
			//					if (treeViewList->IsMutiSelectWithSelectedItems())
			//					{
			//						var droppedOnNode = treeView->NodeFromContainer(droppedOnItem);
			//						var selectedRoots = treeViewList->GetRootsOfSelectedSubtrees();
			//						for (var & node : selectedRoots)
			//						{
			//							var nodeIndex = treeViewList->FlatIndex(node);
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
			//						var droppedOnNode = treeView->NodeFromContainer(droppedOnItem);

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
			//		var treeView = AncestorTreeView();
			//		if (treeView && !args.Handled())
			//		{
			//			var treeViewList = treeView->ListControl();
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

			//var treeView = AncestorTreeView();
			//if (treeView && treeView->CanReorderItems() && !args.Handled())
			//{
			//	var treeViewList = treeView->ListControl();
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
			//					var expandContentTimer = m_expandContentTimer.get();
			//					expandContentTimer.Stop();
			//					expandContentTimer.Start();
			//				}
			//				else
			//				{
			//					// Initialize timer.
			//					winrt::TimeSpan interval = winrt::TimeSpan::duration(c_dragOverInterval);
			//					var expandContentTimer = winrt::DispatcherTimer();
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
			if (!args.Handled)
			{
				var treeView = AncestorTreeView;
				if (treeView != null)
				{
					var treeViewList = treeView.ListControl;
					treeViewList.SetDraggedOverItem(null);
				}

				if (m_expandContentTimer)
				{
					m_expandContentTimer.Stop();
				}
			}
			base.OnDragLeave(args);
		}

		protected override AutomationPeer OnCreateAutomationPeer() =>
			new TreeViewItemAutomationPeer(this);

		// IFrameworkElementOverrides

		protected override void OnApplyTemplate()
		{
			RecycleEvents();

			IControlProtected controlProtected = *this;
			m_selectionBox = (CheckBox)GetTemplateChild(c_multiSelectCheckBoxName);
			RegisterPropertyChangedCallback(winrt::SelectorItem::IsSelectedProperty(), { this, &TreeViewItem::OnIsSelectedChanged });

			if (m_selectionBox)
			{
				m_checkedEventRevoker = m_selectionBox.get().Checked(winrt::auto_revoke, { this, &TreeViewItem::OnCheckToggle });
				m_uncheckedEventRevoker = m_selectionBox.get().Unchecked(winrt::auto_revoke, { this, &TreeViewItem::OnCheckToggle });
			}

			var chevron = (UIElement)GetTemplateChild(c_expandCollapseChevronName);
			if (chevron != null)
			{
				m_expandCollapseChevronPointerPressedToken = chevron.PointerPressed({ this, &TreeViewItem::OnExpandCollapseChevronPointerPressed });
				m_expandCollapseChevron.set(chevron);
			}
			var node = TreeNode;
			if (node != null && IsInContentMode)
			{
				UpdateNodeIsExpandedAsync(node, IsExpanded);
				node.HasUnrealizedChildren = HasUnrealizedChildren;
			}
			base.OnApplyTemplate();
		}

		//	TreeViewItem::~TreeViewItem()
		//	{
		//		// We only need to use safe_get in the deconstruction loop
		//		RecycleEvents(true /* useSafeGet */);
		//	}

		//	template<typename T>
		private T GetAncestorView<T>()
			where T : class
		{
			DependencyObject treeViewItemAncestor = this;
			T ancestorview = null;
			while (treeViewItemAncestor != null && ancestorview == null)
			{
				treeViewItemAncestor = VisualTreeHelper.GetParent(treeViewItemAncestor);
				ancestorview = treeViewItemAncestor as T;
			}
			return ancestorview;
		}

		public TreeView AncestorTreeView
		{
			get
			{
				if (m_ancestorTreeView == null)
				{
					m_ancestorTreeView = GetAncestorView<TreeView>();
				}
				return m_ancestorTreeView;
			}
		}

		//void TreeViewItem::OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args)
		//{
		//	winrt::IDependencyProperty property = args.Property();
		//	if (var node = TreeNode()) 
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

		//			var treeViewNode = winrt::get_self<TreeViewNode>(node);
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
			if (m_expandContentTimer)
			{
				m_expandContentTimer.Stop();
			}

			var draggedOverNode = TreeNode;
			if (draggedOverNode != null && !draggedOverNode.IsExpanded)
			{
				draggedOverNode.IsExpanded = true;
			}
		}

		private void RaiseExpandCollapseAutomationEvent(bool isExpanded)
		{
			if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
			{
				var expandState = isExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
				var peer = FrameworkElementAutomationPeer.FromElement(this);
				if (peer != null)
				{
					var treeViewItemPeer = (TreeViewItemAutomationPeer)peer;
					treeViewItemPeer.RaiseExpandCollapseAutomationEvent(expandState);
				}
			}
		}

		private bool IsInReorderMode(VirtualKey key)
		{
			var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Control);
			bool isControlPressed = (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			var altState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Menu);
			bool isAltPressed = (altState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			var shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
			bool isShiftPressed = (shiftState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			bool isDirectionPressed = IsDirectionalKey(key);

			bool canReorderItems = false;
			var treeView = AncestorTreeView;
			if (AncestorTreeView != null)
			{
				canReorderItems = treeView.CanReorderItems;
			}

			return canReorderItems && isDirectionPressed && isShiftPressed && isAltPressed && !isControlPressed;
		}

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

		private void OnCheckToggle(object sender, RoutedEventArgs args)
		{
			var treeView = AncestorTreeView;
			if (treeView != null)
			{
				var listControl = treeView.ListControl;
				int index = listControl.IndexFromContainer(this);
				var selectionState = CheckBoxSelectionState((CheckBox)sender);
				listControl.ListViewModel.ModifySelectByIndex(index, selectionState);
				UpdateTreeViewItemVisualState(selectionState);
				RaiseSelectionChangeEvents(selectionState == TreeNodeSelectionState.Selected);
			}
		}

		private void RaiseSelectionChangeEvents(bool isSelected)
		{
			var peer = FrameworkElementAutomationPeer.FromElement(this);
			if (peer != null)
			{
				var treeViewItemPeer = (TreeViewItemAutomationPeer)peer;

				var selectionEvent = isSelected ?
					AutomationEvents.SelectionItemPatternOnElementAddedToSelection :
					AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection;
				treeViewItemPeer.RaiseAutomationEvent(selectionEvent);

				var isSelectedProperty = SelectionItemPatternIdentifiers.IsSelectedProperty;
				treeViewItemPeer.RaisePropertyChangedEvent(isSelectedProperty, !isSelected, isSelected);
			}
		}

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
			else if ((key == VirtualKey.Down || key == VirtualKey.Right) && (position != listControl.Items.Count - 1))
			{
				var childIndex = parentNode.Children.IndexOf(targetNode);
				if (childIndex != parentNode.Children.Count - 1)
				{
					if (targetNode.IsExpanded)
					{
						targetNode.IsExpanded = false;
					}

					ReorderItems(listControl, targetNode, position, childIndex, true);
				}
			}
		}

		private bool HandleExpandCollapse(VirtualKey key)
		{
			var treeView = AncestorTreeView;
			var targetNode = treeView.NodeFromContainer(this);
			var flowDirectionReversed = FlowDirection == FlowDirection.RightToLeft;
			bool isExpanded = targetNode.IsExpanded;
			bool handled = false;

			// Inputs for Collapse/Move to parent
			if ((key == VirtualKey.Left && !flowDirectionReversed) ||
				(key == VirtualKey.Right && flowDirectionReversed))
			{
				if (isExpanded)// Is expanded : need to collapse
				{
					targetNode.IsExpanded = false;
					var targetValue = treeView.ContainerFromNode(targetNode);
					if (targetValue != null)
					{
						var targetItem = (TreeViewItem)targetValue;
						targetItem.Focus(FocusState.Keyboard);
					}

					handled = true;
				}
				else // Is collapsed: need to select parent
				{
					var parentNode = targetNode.Parent;
					if (parentNode != null)
					{
						var parentValue = treeView.ContainerFromNode(parentNode);
						if (parentValue != null)
						{
							var parentItem = (TreeViewItem)parentValue;
							parentItem.Focus(FocusState.Keyboard);
							handled = true;
						}
					}
				}
			}
			// Inputs for Expand/Move to first child
			else if ((key == VirtualKey.Right && !flowDirectionReversed) ||
					 (key == VirtualKey.Left && flowDirectionReversed))
			{
				if (!isExpanded && targetNode.HasChildren) // Is collapsed : need to expand
				{
					targetNode.IsExpanded = true;
					handled = true;
				}
				else if (targetNode.Children.Count > 0) // Is expanded and has children: need to select first child (if applicable)
				{
					var childNode = targetNode.Children[0];
					var childValue = treeView.ContainerFromNode(childNode);
					if (childValue != null)
					{
						var childItem = (TreeViewItem)childValue;
						childItem.Focus(FocusState.Keyboard);
						handled = true;
					}
				}
			}

			return handled;
		}

		private void OnExpandCollapseChevronPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			TreeViewNode targetNode = TreeNode;
			var isExpanded = !targetNode.IsExpanded;
			targetNode.IsExpanded = isExpanded;
			args.Handled = true;
		}

		private void RecycleEvents(bool useSafeGet)
		{
			if (var chevron = useSafeGet ? m_expandCollapseChevron : m_expandCollapseChevron.get())
		    {
				if (m_expandCollapseChevronPointerPressedToken.value)
				{
					chevron.PointerCanceled(m_expandCollapseChevronPointerPressedToken);
					m_expandCollapseChevronPointerPressedToken.value = 0;
				}
			}
		}

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

		//Setting IsExpanded changes the itemssource collection on the listview, which cannot be done during layout.
		//We schedule it on the dispatcher so that it runs after layout pass.
		private void UpdateNodeIsExpandedAsync(TreeViewNode node, bool isExpanded)
		{
			var dispatcher = Window.Current.Dispatcher;
			var ignore = dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
				{
					node.IsExpanded = isExpanded;
				});
		}

		private bool IsInContentMode => AncestorTreeView.ListControl.ListViewModel.IsContentMode;

		private TreeNodeSelectionState CheckBoxSelectionState(CheckBox checkBox)
		{
			var winrtBool = checkBox.IsChecked;
			if (winrtBool != null)
			{
				if (winrtBool.Values)
				{
					return TreeNodeSelectionState.Selected;
				}

				return TreeNodeSelectionState.UnSelected;
			}

			return TreeNodeSelectionState.PartialSelected;
		}
	}
}
