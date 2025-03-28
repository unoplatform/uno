// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewList.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using TreeViewListAutomationPeer = Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers.TreeViewListAutomationPeer;
using DragEventArgs = Windows.UI.Xaml.DragEventArgs;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Represents a flattened list of tree view items so that operations such
/// as keyboard navigation and drag-and-drop can be inherited from ListView.
/// </summary>
public partial class TreeViewList : ListView
{
	private bool m_isMultiselectEnabled = false;
	private bool m_itemsSourceAttached = false;
	private string m_dropTargetDropEffectString;
	private UIElement m_draggedOverItem;
	private int m_emptySlotIndex;
	private TreeViewNode m_draggedTreeViewNode;

	/// <summary>
	/// Initializes a new instance of the TreeViewList control.
	/// </summary>
	public TreeViewList()
	{
		ListViewModel = new TreeViewViewModel();
		IsCustomReorder = true;
		DragItemsStarting += OnDragItemsStarting;
		DragItemsCompleted += OnDragItemsCompleted;
		ContainerContentChanging += OnContainerContentChanging;
	}

	internal TreeViewViewModel ListViewModel { get; private set; }

	internal TreeViewNode DraggedTreeViewNode
	{
		get
		{
			return m_draggedTreeViewNode;
		}
	}

	private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
	{
		var dragItem = args.Items[0];
		if (dragItem != null)
		{
			var tvItem = (TreeViewItem)ContainerFromItem(dragItem);
			m_draggedTreeViewNode = NodeFromContainer(tvItem);
			bool isMultipleDragging = false;
			if (IsMutiSelectWithSelectedItems)
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
							args.Items.Add(node.Content);
						}
						else
						{
							args.Items.Add(node);
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
		// Only need to update unrecycled on screen items.
		if (!args.InRecycleQueue)
		{
			var targetItem = (TreeViewItem)args.ItemContainer;
			var targetNode = NodeFromContainer(targetItem);

			var treeViewItem = targetItem;
			var treeViewNode = targetNode;

			var itemsSource = targetItem.ItemsSource;
			if (itemsSource != null)
			{
				if (treeViewNode.ItemsSource == null)
				{
					treeViewItem.SetItemsSource(targetNode, itemsSource);
				}
			}
#if HAS_UNO // #15214: workaround for initial selection being lost
			if (treeViewItem.IsSelected)
			{
				ListViewModel.UpdateSelection(treeViewNode, TreeNodeSelectionState.Selected);
			}
			else if (ListViewModel.TreeView.PendingSelectedItem == args.Item)
			{
				ListViewModel.UpdateSelection(treeViewNode, TreeNodeSelectionState.Selected);
			}
#endif
			treeViewItem.UpdateIndentation(targetNode.Depth);
			treeViewItem.UpdateSelectionVisual(targetNode.SelectionState);
		}
	}

	// IControlOverrides
	protected override void OnDrop(Windows.UI.Xaml.DragEventArgs e)
	{
		var args = e;

		if (args.AcceptedOperation == DataPackageOperation.Move && !args.Handled)
		{
			if (m_draggedTreeViewNode != null && IsIndexValid(m_emptySlotIndex))
			{
				// Get the node at which we will insert
				TreeViewNode insertAtNode = NodeAtFlatIndex(m_emptySlotIndex);

				if (IsMutiSelectWithSelectedItems)
				{
					// Multiselect drag and drop. In the selected items, find all the selected subtrees 
					// and move each of those subtrees.
					var selectedRootNodes = GetRootsOfSelectedSubtrees();

					// Loop through in reverse order because we are inserting above the previous item to get the order correct.
					for (int i = selectedRootNodes.Count - 1; i >= 0; --i)
					{
						MoveNodeInto(selectedRootNodes[i], insertAtNode);
					}
				}
				else
				{
					MoveNodeInto(m_draggedTreeViewNode, insertAtNode);
				}

				UpdateDropTargetDropEffect(false, false, null);
				args.Handled = true;
			}
		}

		base.OnDrop(e);
	}

	// Required as OnDrop is protected and can't be accessed from outside
	internal void OnDropInternal(Windows.UI.Xaml.DragEventArgs e) => OnDrop(e);

	private void MoveNodeInto(TreeViewNode node, TreeViewNode insertAtNode)
	{
		int nodeFlatIndex = FlatIndex(node);
		if (insertAtNode != node && IsFlatIndexValid(nodeFlatIndex))
		{
			RemoveNodeFromParent(node);

			int insertOffset = nodeFlatIndex < m_emptySlotIndex ? 1 : 0;
			// if the insertAtNode is a parent that is expanded
			// insert as the first child
			if (insertAtNode.IsExpanded && insertOffset == 1)
			{
				((TreeViewNodeVector)insertAtNode.Children).InsertAt(0, node);
			}
			else
			{
				// Add the item to the new parent (parent of the insertAtNode)
				var children = (TreeViewNodeVector)insertAtNode.Parent.Children;
				int insertNodeIndexInParent = IndexInParent(insertAtNode);
				children.InsertAt(insertNodeIndexInParent + insertOffset, node);
			}
		}
	}

	protected override void OnDragOver(Windows.UI.Xaml.DragEventArgs args)
	{
		if (!args.Handled)
		{
			args.AcceptedOperation = DataPackageOperation.None;
			IInsertionPanel insertionPanel = ItemsPanelRoot as IInsertionPanel;

			// reorder is only supported with panels that implement IInsertionPanel
			if (insertionPanel != null && m_draggedTreeViewNode != null && CanReorderItems)
			{
				int aboveIndex = -1;
				int belowIndex = -1;
				var itemsSource = ListViewModel;
				int size = itemsSource.Count;
				var point = args.GetPosition((UIElement)insertionPanel);
				insertionPanel.GetInsertionIndexes(point, out aboveIndex, out belowIndex);

				// a value of -1 means we're inserting at the end of the list or before the last item
				if (belowIndex == -1)
				{
					// this allows the next part of this code to test if we're dropping before or after the last item
					belowIndex = size - 1;
				}

				// if we have an insertion point
				// a value of 0 means we're inserting at the beginning
				// a value greater than size - 1 means we're inserting at the end as items are collapsing
				if (belowIndex > size - 1)
				{
					// don't go out of bounds
					// since items might be collapsing as we're dragging
					m_emptySlotIndex = size - 1;
				}
				else if (belowIndex > 0 && m_draggedTreeViewNode != null)
				{
					m_emptySlotIndex = -1;
					TreeViewNode tvi = m_draggedTreeViewNode;
					if (ListViewModel.IndexOfNode(tvi, out var draggedIndex))
					{
						int indexToUse = (draggedIndex < belowIndex) ? belowIndex : (belowIndex - 1);
						var item = ContainerFromIndex(indexToUse);
						if (item != null)
						{
							var treeViewItem = (TreeViewItem)item;
							var pointInsideItem = args.GetPosition(treeViewItem);

							// if the point is in the top half of the item
							// we need to insert before that item
							if (pointInsideItem.Y < treeViewItem.ActualHeight / 2)
							{
								m_emptySlotIndex = belowIndex - 1;
							}
							else
							{
								m_emptySlotIndex = belowIndex;
							}
						}
					}
				}
				else
				{
					// top of the list
					m_emptySlotIndex = 0;
				}

				bool allowReorder = true;
				if (IsFlatIndexValid(m_emptySlotIndex))
				{
					var insertAtNode = NodeAtFlatIndex(m_emptySlotIndex);
					if (IsMultiselect)
					{
						// If insertAtNode is in the selected items, then we do not want to allow a dropping.
						var selectedItems = ListViewModel.SelectedNodes;
						for (var i = 0; i < selectedItems.Count; i++)
						{
							var selectedNode = selectedItems[i];
							if (selectedNode == insertAtNode)
							{
								allowReorder = false;
								break;
							}
						}
					}
					else
					{
						if (m_draggedTreeViewNode != null && insertAtNode != null && insertAtNode.Parent != null)
						{
							var insertContainer = ContainerFromNode(insertAtNode.Parent);
							if (insertContainer != null)
							{
								allowReorder = ((TreeViewItem)insertContainer).AllowDrop;
							}
						}
					}
				}
				else
				{
					// m_emptySlotIndex does not exist in the ViewModel - don't allow the reorder.
					allowReorder = false;
				}

				if (allowReorder)
				{
					args.AcceptedOperation = DataPackageOperation.Move;
				}
				else
				{
					m_emptySlotIndex = -1;
					args.AcceptedOperation = DataPackageOperation.None;
					args.Handled = true;
				}
			}

			UpdateDropTargetDropEffect(false, false, null);
		}
		base.OnDragOver(args);
	}

	protected override void OnDragEnter(Windows.UI.Xaml.DragEventArgs args)
	{
		if (!args.Handled)
		{
			UpdateDropTargetDropEffect(false, false, null);
		}
		base.OnDragEnter(args);
	}

	protected override void OnDragLeave(Windows.UI.Xaml.DragEventArgs args)
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
		var itemNode = NodeFromContainer(element);
		TreeViewNode itemNodeImplNoRef = itemNode;
		TreeViewItem itemContainer = (TreeViewItem)element;
		var selectionState = itemNodeImplNoRef.SelectionState;

		//Set the expanded property to match that of the Node, and enable Drop by default
		itemContainer.AllowDrop = true;

		if (IsContentMode)
		{
			bool hasChildren = itemContainer.HasUnrealizedChildren || itemNode.HasChildren;
			itemContainer.GlyphOpacity = hasChildren ? 1.0 : 0.0;
			if (itemContainer.IsExpanded != itemNode.IsExpanded)
			{
				DispatcherQueue.TryEnqueue(() =>
				{
#if !HAS_UNO // Uno Specific: this is actually a bug in WinUI, copy WinUI's fix when https://github.com/microsoft/microsoft-ui-xaml/issues/9549 is resolved
					itemNode.IsExpanded = itemContainer.IsExpanded;
#else
					// The "source of truth" for IsExpanded should come from the (likely recycled) container only if
					// it has a binding on IsExpanded. Otherwise, the container doesn't know anything and we should
					// use the IsExpanded value of the Node (which remembers the last value of IsExpanded before
					// the container was recycled)
					var bindingExists = itemContainer.GetBindingExpression(TreeViewItem.IsExpandedProperty) is { };
					if (bindingExists)
					{
						itemNode.IsExpanded = itemContainer.IsExpanded;
					}
					else
					{
						itemContainer.IsExpanded = itemNode.IsExpanded;
					}
#endif
				});
			}
		}
		else
		{
			itemContainer.IsExpanded = itemNode.IsExpanded;
			itemContainer.GlyphOpacity = itemNode.HasChildren ? 1.0 : 0.0;
		}

		//Set startup TemplateSettings properties
		var templateSettings = itemContainer.TreeViewItemTemplateSettings;
		templateSettings.ExpandedGlyphVisibility = itemNode.IsExpanded ? Visibility.Visible : Visibility.Collapsed;
		templateSettings.CollapsedGlyphVisibility = !itemNode.IsExpanded ? Visibility.Visible : Visibility.Collapsed;

		base.PrepareContainerForItemOverride(element, item);

		if (selectionState != itemNodeImplNoRef.SelectionState)
		{
			ListViewModel.UpdateSelection(itemNodeImplNoRef, selectionState);
		}
	}

	protected override DependencyObject GetContainerForItemOverride()
	{
		var targetItem = new TreeViewItem() { IsGeneratedContainer = true }; // Uno specific IsGeneratedContainer
		return targetItem;
	}

	// IFrameworkElementOverrides
	protected override void OnApplyTemplate()
	{
		if (!m_itemsSourceAttached)
		{
			ItemsSource = ListViewModel;
			IsItemClickEnabled = true;
			m_itemsSourceAttached = true;
		}

		base.OnApplyTemplate();
	}

	// IUIElementOverrides
	protected override AutomationPeer OnCreateAutomationPeer() =>
		new TreeViewListAutomationPeer(this);

	internal string GetDropTargetDropEffect()
	{
		if (string.IsNullOrEmpty(m_dropTargetDropEffectString))
		{
			UpdateDropTargetDropEffect(true, false, null);
		}
		return m_dropTargetDropEffectString;
	}

	internal void SetDraggedOverItem(TreeViewItem newDraggedOverItem)
	{
		m_draggedOverItem = newDraggedOverItem;
	}

	internal void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving, TreeViewItem keyboardReorderedContainer)
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
			var listItem = ContainerFromItem(m_draggedOverItem);
			if (listItem != null)
			{
				dragItem = (TreeViewItem)listItem;
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
				dragItemString = dragItemPeer.GetName();
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
				//unused variables in MUX source
				//TreeViewItem itemAfterInsertPosition = null;
				//TreeViewItem itemBeforeInsertPosition = null;

				if (m_draggedOverItem != null)
				{
					AutomationPeer draggedOverItemPeer = FrameworkElementAutomationPeer.FromElement(m_draggedOverItem);
					if (draggedOverItemPeer != null)
					{
						draggedOverString = draggedOverItemPeer.GetName();
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

	internal bool IsMutiSelectWithSelectedItems
	{
		get
		{
			var selectedItems = ListViewModel.SelectedNodes;
			bool isMutiSelect = m_isMultiselectEnabled && selectedItems.Count > 0;
			return isMutiSelect;
		}
	}

	internal bool IsSelected(TreeViewNode node)
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

	internal IList<TreeViewNode> GetRootsOfSelectedSubtrees()
	{
		var roots = new List<TreeViewNode>();
		var selectedItems = ListViewModel.SelectedNodes;
		for (var i = 0; i < selectedItems.Count; i++)
		{
			var item = selectedItems[i];
			var selectionRoot = GetRootOfSelection(item);

			if (!roots.Contains(selectionRoot))
			{
				roots.Add(selectionRoot);
			}
		}

		return roots;
	}

	internal int FlatIndex(TreeViewNode node)
	{
		bool found = ListViewModel.IndexOfNode(node, out var flatIndex);
		flatIndex = found ? flatIndex : -1;
		return flatIndex;
	}

	internal bool IsFlatIndexValid(int index)
	{
		return index >= 0 && index < ListViewModel.Count;
	}

	internal int RemoveNodeFromParent(TreeViewNode node)
	{
		var children = (TreeViewNodeVector)node.Parent.Children;
		int indexInParent = children.IndexOf(node);

		if (indexInParent > -1)
		{
			children.RemoveAt(indexInParent);
		}

		return indexInParent;
	}

	private bool IsIndexValid(int index)
	{
		return index >= 0 && index < Items.Count;
	}

	private string GetAutomationName(int index)
	{
		TreeViewItem item = null;
		string automationName = "";

		if (IsIndexValid(index))
		{
			var interim = ContainerFromIndex(index) as TreeViewItem;
			if (interim != null)
			{
				item = interim;
			}
		}

		if (item != null)
		{
			AutomationPeer itemPeer = FrameworkElementAutomationPeer.FromElement(item);
			if (itemPeer != null)
			{
				automationName = itemPeer.GetName();
			}
		}

		return automationName;
	}

	private string BuildEffectString(string priorString, string afterString, string dragString, string dragOverString)
	{
		string resultString;
		if (!string.IsNullOrEmpty(priorString) && !string.IsNullOrEmpty(afterString))
		{
			resultString = StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PlaceBetweenString),
				dragString, priorString, afterString);
		}
		else if (!string.IsNullOrEmpty(priorString))
		{
			resultString = StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PlaceAfterString),
				dragString, priorString);
		}
		else if (!string.IsNullOrEmpty(afterString))
		{
			resultString = StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_PlaceBeforeString),
				dragString, afterString);
		}
		else if (!string.IsNullOrEmpty(dragOverString))
		{
			resultString = StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_DropIntoNodeString),
				dragString, dragOverString);
		}
		else
		{
			resultString = StringUtil.FormatString(
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_FallBackPlaceString),
				dragString);
		}

		return resultString;
	}

	private int IndexInParent(TreeViewNode node)
	{
		int indexInParent = node.Parent.Children.IndexOf(node);
		return indexInParent;
	}

	private TreeViewNode NodeAtFlatIndex(int index)
	{
		return ListViewModel.GetNodeAt(index);
	}

	private TreeViewNode GetRootOfSelection(TreeViewNode node)
	{
		TreeViewNode current = node;
		while (current.Parent != null && IsSelected(current.Parent))
		{
			current = current.Parent;
		}

		return current;
	}

	internal TreeViewNode NodeFromContainer(DependencyObject container)
	{
		int index = container != null ? IndexFromContainer(container) : -1;
		if (index >= 0 && index < ListViewModel.Count)
		{
			return NodeAtFlatIndex(index);
		}
		return null;
	}

	internal DependencyObject ContainerFromNode(TreeViewNode node)
	{
		if (node == null)
		{
			return null;
		}

		return IsContentMode ? ContainerFromItem(node.Content) : ContainerFromItem(node);
	}

	internal TreeViewNode NodeFromItem(object item)
	{
		return IsContentMode ?
			ListViewModel.GetAssociatedNode(item) :
			item as TreeViewNode;
	}

	internal object ItemFromNode(TreeViewNode node)
	{
		return (IsContentMode && node != null) ? node.Content : node;
	}

	internal bool IsContentMode => ListViewModel?.IsContentMode ?? false;


	// Uno Specific

	private TreeView m_ancestorTreeView;

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

	internal TreeView AncestorTreeView
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
}
