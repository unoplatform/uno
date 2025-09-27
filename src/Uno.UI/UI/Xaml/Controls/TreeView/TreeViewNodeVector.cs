// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewNode.cpp, tag winui3/release/1.4.2

using System;
using System.Collections;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

internal class TreeViewNodeVector : ObservableVector<TreeViewNode>
{
	private TreeViewNode m_parent;

	public TreeViewNodeVector()
	{

	}

	public void SetParent(TreeViewNode value)
	{
		m_parent = value;
	}

	private IList GetWritableParentItemsSource()
	{
		IList parentItemsSource = null;

		if (m_parent?.ItemsSource != null)
		{
			parentItemsSource = m_parent?.ItemsSource as IList;
		}

		return parentItemsSource;
	}

	public void Append(TreeViewNode item, bool updateItemsSource = true)
	{
		InsertAt(Count, item, updateItemsSource);
	}

	public override void Add(TreeViewNode item) => Append(item);

	public void InsertAt(int index, TreeViewNode item, bool updateItemsSource = true)
	{
		if (m_parent == null)
		{
			throw new InvalidOperationException("Parent node must be set");
		}
		if (index > base.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), "Index out of range for Insert");
		}

		item.Parent = m_parent;

		base.Insert(index, item);

		if (updateItemsSource)
		{
			var itemsSource = GetWritableParentItemsSource();
			if (itemsSource != null)
			{
				itemsSource.Insert(index, item.Content);
			}
		}
	}

	public override void Insert(int index, TreeViewNode item) => InsertAt(index, item);

	public void SetAt(int index, TreeViewNode item, bool updateItemsSource = true)
	{
		RemoveAt(index, updateItemsSource, false /* updateIsExpanded */);
		InsertAt(index, item, updateItemsSource);
	}

	public override TreeViewNode this[int index]
	{
		get => base[index];
		set => SetAt(index, value);
	}

	public void RemoveAt(int index, bool updateItemsSource = true, bool updateIsExpanded = true)
	{
		var targetNode = this[index];
		targetNode.Parent = null;

		base.RemoveAt(index);

		if (updateItemsSource)
		{
			var source = GetWritableParentItemsSource();
			if (source != null)
			{
				source.RemoveAt(index);
			}
		}

		if (updateIsExpanded && base.Count == 0)
		{
			var ownerNode = m_parent;
			if (ownerNode != null)
			{
				// Only set IsExpanded to false if we are not the root node
				var ownerParent = ownerNode.Parent;
				if (ownerParent != null)
				{
					ownerNode.IsExpanded = false;
				}
			}
		}
	}

	public override void RemoveAt(int index) => RemoveAt(index, true);

	public void RemoveAtEnd(bool updateItemsSource = true)
	{
		var index = Count - 1;
		RemoveAt(index, updateItemsSource);
	}

	public void ReplaceAll(TreeViewNode[] values, bool updateItemsSource = true)
	{
		var count = Count;
		if (count > 0)
		{
			Clear(updateItemsSource);

			var itemsSource = GetWritableParentItemsSource();
			// Set parent on new elements
			if (m_parent == null)
			{
				throw new InvalidOperationException("Parent must be set");
			}
			foreach (var value in values)
			{
				value.Parent = m_parent;
				if (itemsSource != null)
				{
					itemsSource.Add(value.Content);
				}
			}

			base.Clear();
			foreach (var value in values)
			{
				base.Add(value);
			}
		}
	}

	public void Clear(bool updateItemsSource = true, bool updateIsExpanded = true)
	{
		var count = Count;

		if (count > 0)
		{
			for (var i = 0; i < count; i++)
			{
				var node = this[i];
				node.Parent = null;
			}

			base.Clear();

			if (updateItemsSource)
			{
				var itemsSource = GetWritableParentItemsSource();
				if (itemsSource != null)
				{
					itemsSource.Clear();
				}
			}
		}

		if (updateIsExpanded)
		{
			var ownerNode = m_parent;
			if (ownerNode != null)
			{
				// Only set IsExpanded to false if we are not the root node
				var ownerParent = ownerNode.Parent;
				if (ownerParent != null)
				{
					ownerNode.IsExpanded = false;
				}
			}
		}
	}

	public override void Clear() => Clear(true, true);
}
