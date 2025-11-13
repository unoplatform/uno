// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeView.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a hierarchical list with expanding and collapsing nodes that contain nested items.
/// </summary>
public partial class TreeView : Control
{
	private const string c_listControlName = "ListControl";

	private TreeViewList m_listControl;
	private TreeViewNode m_rootNode;
	private IList<TreeViewNode> m_pendingSelectedNodes;

#if HAS_UNO
	internal object PendingSelectedItem { get; set; }
#endif

	/// <summary>
	/// Initializes a new instance of the TreeView control.
	/// </summary>
	public TreeView()
	{
		this.SetDefaultStyleKey();

		m_rootNode = new TreeViewNode();
		m_pendingSelectedNodes = new List<TreeViewNode>();
	}

	/// <summary>
	/// Gets or sets the collection of root nodes of the tree.
	/// </summary>
	public IList<TreeViewNode> RootNodes => m_rootNode.Children;

	internal TreeViewList ListControl => m_listControl;

	internal TreeViewList MutableListControl => m_listControl;

	/// <summary>
	/// Returns the item that corresponds to the specified, generated container.
	/// </summary>
	/// <param name="container">The DependencyObject that corresponds to the item to be returned.</param>
	/// <returns>The contained item, or the container if it does not contain an item.</returns>
	public object ItemFromContainer(DependencyObject container) =>
		ListControl?.ItemFromContainer(container);

	/// <summary>
	/// Returns the container corresponding to the specified item.
	/// </summary>
	/// <param name="item">The item to retrieve the container for.</param>
	/// <returns>A container that corresponds to the specified item, if the item
	/// has a container and exists in the collection; otherwise, null.</returns>
	public DependencyObject ContainerFromItem(object item) =>
		ListControl?.ContainerFromItem(item);

	/// <summary>
	/// Returns the TreeViewNode corresponding to the specified container.
	/// </summary>
	/// <param name="container">he container to retrieve the TreeViewNode for.</param>
	/// <returns>The node that corresponds to the specified container.</returns>
	public TreeViewNode NodeFromContainer(DependencyObject container) =>
		ListControl?.NodeFromContainer(container);

	/// <summary>
	/// Returns the container corresponding to the specified node.
	/// </summary>
	/// <param name="node">The node to retrieve the container for.</param>
	/// <returns>A container that corresponds to the specified node, if the node
	/// has a container and exists in the collection; otherwise, null.</returns>
	public DependencyObject ContainerFromNode(TreeViewNode node) =>
		ListControl?.ContainerFromNode(node);

	/// <summary>
	/// Gets or sets the node that is selected in the tree.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the collection of nodes that are selected in the tree.
	/// </summary>
	public IList<TreeViewNode> SelectedNodes
	{
		get
		{
			return ListControl?.ListViewModel?.SelectedNodes ??
				m_pendingSelectedNodes;
			// we'll treat the pending selected nodes as SelectedNodes value
			// if we don't have a list control or a view model
		}
	}

	/// <summary>
	/// Gets the currently selected items.
	/// </summary>
	public IList<object> SelectedItems => ListControl?.ListViewModel?.SelectedItems;

	internal void UpdateSelection(TreeViewNode node, bool isSelected)
	{
		var viewModel = ListControl?.ListViewModel;
		if (viewModel != null)
		{
			if (isSelected != viewModel.IsNodeSelected(node))
			{
				viewModel.SelectNode(node, isSelected);
			}
		}
	}

	/// <summary>
	/// Expands a given node.
	/// </summary>
	/// <param name="value">Node.</param>
	public void Expand(TreeViewNode value)
	{
		var vm = ListControl.ListViewModel;
		vm.ExpandNode(value);
	}

	/// <summary>
	/// Collapses a given node.
	/// </summary>
	/// <param name="value">Node.</param>
	public void Collapse(TreeViewNode value)
	{
		var vm = ListControl.ListViewModel;
		vm.CollapseNode(value);
	}

	/// <summary>
	/// Selects all nodes in the tree.
	/// </summary>
	public void SelectAll()
	{
		var vm = ListControl.ListViewModel;
		vm.SelectAll();
	}

	private void OnItemClick(object sender, ItemClickEventArgs args)
	{
		var itemInvokedArgs = new TreeViewItemInvokedEventArgs(args.ClickedItem);
		ItemInvoked?.Invoke(this, itemInvokedArgs);
	}

	private void OnContainerContentChanging(object sender, ContainerContentChangingEventArgs args)
	{
		ContainerContentChanged?.Invoke((TreeView)sender, args);
	}

	private void OnNodeExpanding(TreeViewNode sender, object args)
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
			Expanding?.Invoke(this, treeViewExpandingEventArgs);
		}
	}

	private void OnNodeCollapsed(TreeViewNode sender, object args)
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
			Collapsed?.Invoke(this, treeViewCollapsedEventArgs);
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
		else if (property == SelectedItemProperty)
		{
			var items = SelectedItems;
			var selected = items?.Count > 0 ? items[0] : null;
#if !HAS_UNO
			if (args.NewValue != selected)
			{
				ListControl?.ListViewModel?.SelectSingleItem(args.NewValue);
			}
#else // #15214: workaround for initial selection being lost
			PendingSelectedItem = null;
			if (args.NewValue != selected)
			{
				ListControl?.ListViewModel?.SelectSingleItem(args.NewValue);
				// ^ the vm can fail to select the item for two reasons:
				// 1. OnApplyTemplate was not yet called
				// 2. the item is a descendant of a collapsed node
				// in these cases, we should mark the item pending for selection.
				// so it can be later picked up by TreeViewList::OnContainerContentChanging when it becomes materialized
				if (items == null || (items is [var selected2] && args.NewValue != selected2))
				{
					PendingSelectedItem = args.NewValue;
				}
			}
#endif
		}
	}

	private void OnListControlDragItemsStarting(object sender, DragItemsStartingEventArgs args)
	{
		var treeViewArgs = new TreeViewDragItemsStartingEventArgs(args);
		DragItemsStarting?.Invoke(this, treeViewArgs);
	}

	private void OnListControlDragItemsCompleted(object sender, DragItemsCompletedEventArgs args)
	{
		object CreateNewParent(IReadOnlyList<object> items, TreeViewList listControl, TreeViewNode rootNode)
		{
			if (listControl != null && items != null && items.Count > 0)
			{
				var draggedNode = listControl.NodeFromItem(items[0]);
				if (draggedNode != null)
				{
					var parentNode = draggedNode.Parent;
					if (parentNode != null && parentNode != rootNode)
					{
						return ListControl.ItemFromNode(parentNode);
					}
				}
			}
			return null;
		}
		var newParent = CreateNewParent(args.Items, ListControl, m_rootNode);

		var treeViewArgs = new TreeViewDragItemsCompletedEventArgs(args, newParent);
		DragItemsCompleted?.Invoke(this, treeViewArgs);
	}

	private void OnListControlSelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		if (SelectionMode == TreeViewSelectionMode.Single)
		{
			RaiseSelectionChanged(args.AddedItems, args.RemovedItems);

			object GetNewSelectedItem(SelectionChangedEventArgs args)
			{
				var newItems = args.AddedItems;
				if (newItems != null)
				{
					if (newItems.Count > 0)
					{
						return newItems[0];
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}
			var newSelectedItem = GetNewSelectedItem(args);

			if (SelectedItem != newSelectedItem)
			{
				SelectedItem = newSelectedItem;
			}
		}
	}

	private void UpdateItemsSelectionMode(bool isMultiSelect)
	{
		var listControl = ListControl;
		if (listControl.IsMultiselect != isMultiSelect)
		{
			listControl.EnableMultiselect(isMultiSelect);
		}

		var viewModel = listControl.ListViewModel;
		int size = viewModel.Count;

		for (int i = 0; i < size; i++)
		{
			var updateContainer = listControl.ContainerFromIndex(i) as TreeViewItem;
			if (updateContainer != null)
			{
				if (isMultiSelect)
				{
					var targetNode = viewModel.GetNodeAt(i);
					if (targetNode != null)
					{
						if (viewModel.IsNodeSelected(targetNode))
						{
							VisualStateManager.GoToState(updateContainer, "TreeViewMultiSelectEnabledSelected", false);
						}
						else
						{
							VisualStateManager.GoToState(updateContainer, "TreeViewMultiSelectEnabledUnselected", false);
						}
					}
				}
				else
				{
					VisualStateManager.GoToState(updateContainer, "TreeViewMultiSelectDisabled", false);
				}
			}
		}
	}

	internal void RaiseSelectionChanged(IList<object> addedItems, IList<object> removedItems)
	{
		var treeViewArgs = new TreeViewSelectionChangedEventArgs(addedItems, removedItems);
		SelectionChanged?.Invoke(this, treeViewArgs);
	}

	protected override void OnApplyTemplate()
	{
		m_listControl = GetTemplateChild(c_listControlName) as TreeViewList;
		var listControl = m_listControl;
		if (listControl != null)
		{
			var listPtr = listControl;
			var viewModel = listPtr.ListViewModel;
			if (m_rootNode == null)
			{
				m_rootNode = new TreeViewNode();
			}

			if (ItemsSource != null)
			{
				viewModel.IsContentMode = true;
			}
			viewModel.PrepareView(m_rootNode);
			viewModel.SetOwners(listControl, this);
			viewModel.NodeExpanding += OnNodeExpanding;
			viewModel.NodeCollapsed += OnNodeCollapsed;

			var selectionMode = SelectionMode;
			if (selectionMode == TreeViewSelectionMode.Single)
			{
				listControl.SelectionMode = ListViewSelectionMode.Single;
			}
			else
			{
				listControl.SelectionMode = ListViewSelectionMode.None;
				if (selectionMode == TreeViewSelectionMode.Multiple)
				{
					UpdateItemsSelectionMode(true);
				}
			}

			listControl.ItemClick += OnItemClick;
			listControl.ContainerContentChanging += OnContainerContentChanging;
			listControl.DragItemsStarting += OnListControlDragItemsStarting;
			listControl.DragItemsCompleted += OnListControlDragItemsCompleted;
			listControl.SelectionChanged += OnListControlSelectionChanged;

			if (m_pendingSelectedNodes != null && m_pendingSelectedNodes.Count > 0)
			{
				var selectedNodes = viewModel.SelectedNodes;
				foreach (var node in m_pendingSelectedNodes)
				{
					selectedNodes.Add(node);
				}
				m_pendingSelectedNodes.Clear();
			}
		}
	}
}
