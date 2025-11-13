// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewModel.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

// Need to update node selection states on UI before vector changes.
// Listen on vector change events don't solve the problem because the event already happened when the event handler gets called.
// i.e. the node is already gone when we get to ItemRemoved callback.
internal class SelectedTreeNodeVector : ObservableVector<TreeViewNode>
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

	internal void Append(TreeViewNode node) => InsertAt(Count, node);

	public override void Add(TreeViewNode node) => Append(node);

	internal void InsertAt(int index, TreeViewNode node)
	{
		// UpdateSelection will call InsertAtCore
		UpdateSelection(node, TreeNodeSelectionState.Selected);
	}

	public override void Insert(int index, TreeViewNode node) => InsertAt(index, node);

	internal void SetAt(int index, TreeViewNode node)
	{
		RemoveAt(index);
		InsertAt(index, node);
	}

	public override TreeViewNode this[int index]
	{
		get => base[index];
		set => SetAt(index, value);
	}

	public override void RemoveAt(int index)
	{
		var oldNode = base[index];
		// UpdateNodeSelection will call RemoveAtCore
		UpdateSelection(oldNode, TreeNodeSelectionState.UnSelected);
	}

	internal void RemoveAtEnd() => RemoveAt(Count - 1);

	internal void ReplaceAll(IEnumerable<TreeViewNode> nodes)
	{
		Clear();

		foreach (var node in nodes)
		{
			Append(node);
		}
	}

	public override void Clear()
	{
		while (Count > 0)
		{
			RemoveAtEnd();
		}
	}

	// Default write methods will trigger TreeView visual updates.
	// If you want to update vector content without notifying TreeViewNodes, use "core" version of the methods.
	internal void InsertAtCore(int index, TreeViewNode node)
	{
		if (!Contains(node))
		{
			base.Insert(index, node);

			// Keep SelectedItems and SelectedNodes in sync
			var viewModel = m_viewModel;
			if (viewModel != null)
			{
				var selectedItems = viewModel.SelectedItems;
				if (selectedItems.Count != Count)
				{
					var listControl = viewModel.ListControl;
					var item = listControl?.ItemFromNode(node);
					if (item != null)
					{
						selectedItems.Insert(index, item);
						viewModel.TrackItemSelected(item);
					}
				}
			}
		}
	}

	internal void RemoveAtCore(int index)
	{
		base.RemoveAt(index);

		// Keep SelectedItems and SelectedNodes in sync
		var viewModel = m_viewModel;
		if (viewModel != null)
		{
			var selectedItems = viewModel.SelectedItems;
			if (Count != selectedItems.Count)
			{
				var item = selectedItems[index];
				selectedItems.RemoveAt(index);
				viewModel.TrackItemUnselected(item);
			}
		}
	}
}
