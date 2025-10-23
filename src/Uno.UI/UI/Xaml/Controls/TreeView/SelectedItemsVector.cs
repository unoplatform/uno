// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ViewModel.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

//// Similar to SelectedNodesVector, we need to make decisions before the item is inserted or removed.
//// we can't use vector change events because the event already happened when event hander gets called.
internal class SelectedItemsVector : ObservableVector<object>
{
	private TreeViewViewModel m_viewModel;

	public void SetViewModel(TreeViewViewModel viewModel)
	{
		m_viewModel = viewModel;
	}

	public override void Add(object item) => Append(item);

	internal void Append(object item) => InsertAt(Count, item);

	internal void InsertAt(int index, object item)
	{
		if (!Contains(item))
		{
			base.Insert(index, item);

			// Keep SelectedNodes and SelectedItems in sync
			var viewModel = m_viewModel;
			if (viewModel != null)
			{
				var selectedNodes = viewModel.SelectedNodes;
				if (selectedNodes.Count != Count)
				{
					var listControl = viewModel.ListControl;
					if (listControl != null)
					{
						var node = listControl.NodeFromItem(item);
						if (node != null)
						{
							selectedNodes.Insert(index, node);
						}
					}
				}
			}
		}
	}

	internal void SetAt(int index, object item)
	{
		RemoveAt(index);
		InsertAt(index, item);
	}

	public override void RemoveAt(int index)
	{
		base.RemoveAt(index);

		// Keep SelectedNodes and SelectedItems in sync
		var viewModel = m_viewModel;
		if (viewModel != null)
		{
			var selectedNodes = viewModel.SelectedNodes;
			if (Count != selectedNodes.Count)
			{
				selectedNodes.RemoveAt(index);
			}
		}
	}

	internal void RemoveAtEnd() => RemoveAt(Count - 1);

	internal void ReplaceAll(IEnumerable<object> items)
	{
		Clear();

		foreach (var node in items)
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
}
