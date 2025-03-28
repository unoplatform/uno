// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TreeViewNode.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

[Bindable]
public partial class TreeViewNode : DependencyObject, ICustomPropertyProvider, IStringable
{
	private WeakReference<TreeViewNode> m_parentNode;
	private bool m_HasUnrealizedChildren;
	private object m_itemsSource = null;
	private ItemsSourceView m_itemsDataSource = null;

	public TreeViewNode()
	{
		var collection = new TreeViewNodeVector();
		collection.SetParent(this);
		Children = collection;
		collection.VectorChanged += ChildVectorChanged;
	}

	public TreeViewNode Parent
	{
		get
		{
			TreeViewNode parentNode = null;
			if (m_parentNode?.TryGetTarget(out parentNode) == true)
			{
				return parentNode;
			}
			return null;
		}

		internal set
		{
			if (value != null)
			{
				m_parentNode = new WeakReference<TreeViewNode>(value);
			}
			else
			{
				m_parentNode = null;
			}

			//A parentless node has a depth of -1, and the first level of visible nodes in
			// the tree view will have a depth of 0 (-1 + 1);
			UpdateDepth(value != null ? value.Depth + 1 : -1);
		}
	}

	public bool HasUnrealizedChildren
	{
		get => m_HasUnrealizedChildren;
		set
		{
			m_HasUnrealizedChildren = value;
			UpdateHasChildren();
		}
	}

	public IList<TreeViewNode> Children { get; }

	internal bool IsContentMode { get; set; }

	internal TreeNodeSelectionState SelectionState { get; set; }

	private void UpdateDepth(int depth)
	{
		// Update our depth
		SetValue(DepthProperty, depth);

		// Update children's depth
		foreach (var child in Children)
		{
			child.UpdateDepth(depth + 1);
		}
	}

	private void UpdateHasChildren()
	{
		bool hasChildren = Children.Count != 0 || m_HasUnrealizedChildren;
		SetValue(HasChildrenProperty, hasChildren);
	}

	private void ChildVectorChanged(IObservableVector<TreeViewNode> sender, IVectorChangedEventArgs args)
	{
		// We can end up in a state where the TreeViewNode has not yet been destroyed but it is no longer 'reachable' via
		// tracker references. In this state tracker_refs do not resolve. So we check for this before handling the event.
		if (Children is not null)
		{
			var collectionChange = args.CollectionChange;
			var index = args.Index;
			UpdateHasChildren();
			RaiseChildrenChanged(collectionChange, index);
		}
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		DependencyProperty property = args.Property;
		ExpandedChanged?.Invoke(this, args);
	}

	private void RaiseChildrenChanged(CollectionChange collectionChange, uint index)
	{
		var args = new VectorChangedEventArgs(collectionChange, index);
		ChildrenChanged?.Invoke(this, args);
	}

	internal object ItemsSource
	{
		get => m_itemsSource;
		set
		{
			if (m_itemsDataSource != null)
			{
				m_itemsDataSource.CollectionChanged -= OnItemsSourceChanged;
			}
			m_itemsSource = value;
			m_itemsDataSource = value != null ? new InspectingDataSource(value) : null;
			if (m_itemsDataSource != null)
			{
				m_itemsDataSource.CollectionChanged += OnItemsSourceChanged;
			}
			SyncChildrenNodesWithItemsSource();
		}
	}

	private void OnItemsSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
	{
		// We can end up in a state where the TreeViewNode has not yet been destroyed but it is no longer 'reachable' via
		// tracker references. In this state tracker_refs do not resolve. So we check for this before handling the event.
		if (Children is not null)
		{
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						// TreeViewNode and ItemsSource will update each other when data changes.
						// For ItemsSource -> TreeViewNode changes, m_itemsDataSource.Count() > Children().Size()
						// We'll add the new node to children collection.
						// For TreeViewNode -> ItemsSource changes, m_itemsDataSource.Count() == Children().Size()
						// the node is already in children collection, we don't want to update TreeViewNode again here.
						if (m_itemsDataSource.Count != Children.Count)
						{
							AddToChildrenNodes(args.NewStartingIndex, args.NewItems.Count);
						}
						break;
					}

				case NotifyCollectionChangedAction.Remove:
					{
						// TreeViewNode and ItemsSource will update each other when data changes.
						// For ItemsSource -> TreeViewNode changes, m_itemsDataSource.Count() < Children().Size()
						// We'll remove the node from children collection.
						// For TreeViewNode -> ItemsSource changes, m_itemsDataSource.Count() == Children().Size()
						// the node is already removed, we don't want to update TreeViewNode again here.
						if (m_itemsDataSource.Count != Children.Count)
						{
							RemoveFromChildrenNodes(args.OldStartingIndex, args.OldItems.Count);
						}
						break;
					}

				case NotifyCollectionChangedAction.Reset:
					{
						SyncChildrenNodesWithItemsSource();
						break;
					}

				case NotifyCollectionChangedAction.Replace:
					{
						RemoveFromChildrenNodes(args.OldStartingIndex, args.OldItems.Count);
						AddToChildrenNodes(args.NewStartingIndex, args.NewItems.Count);
						break;
					}
			}
		}
	}

	private void AddToChildrenNodes(int index, int count)
	{
		for (int i = index + count - 1; i >= index; i--)
		{
			var item = m_itemsDataSource.GetAt(i);
			var node = new TreeViewNode();
			node.Content = item;
			((TreeViewNodeVector)Children).InsertAt(index, node, false /* updateItemsSource */);
		}
	}

	private void RemoveFromChildrenNodes(int index, int count)
	{
		for (int i = 0; i < count; i++)
		{
			((TreeViewNodeVector)Children).RemoveAt(index, false /* updateItemsSource */);
		}
	}

	private void SyncChildrenNodesWithItemsSource()
	{
		if (!AreChildrenNodesEqualToItemsSource())
		{
			var children = (TreeViewNodeVector)Children;
			children.Clear(false /* updateItemsSource */, false /* updateIsExpanded */);

			var size = m_itemsDataSource != null ? m_itemsDataSource.Count : 0;
			for (var i = 0; i < size; i++)
			{
				var item = m_itemsDataSource.GetAt(i);
				var node = new TreeViewNode();
				node.Content = item;
				node.IsContentMode = true;
				children.Append(node, false /* updateItemsSource */);
			}
		}
	}

	private bool AreChildrenNodesEqualToItemsSource()
	{
		var children = Children;
		int childrenCount = children != null ? children.Count : 0;
		int itemsSourceCount = m_itemsDataSource != null ? m_itemsDataSource.Count : 0;

		if (childrenCount != itemsSourceCount)
		{
			return false;
		}

		// Compare the actual content in collections when counts are equal
		for (int i = 0; i < itemsSourceCount; i++)
		{
			if (children[i].Content != m_itemsDataSource.GetAt(i))
			{
				return false;
			}
		}

		return true;
	}

	private string GetContentAsString()
	{
		var content = Content;
		if (content != null)
		{
			var result = content as ICustomPropertyProvider;
			if (result != null)
			{
				return result.GetStringRepresentation();
			}

			var resultStringable = content as IStringable;
			if (resultStringable != null)
			{
				return resultStringable.ToString();
			}

			return content?.ToString() ?? GetType().Name;
		}

		return GetType().Name;
	}

	#region ICustomPropertyProvider

	Type ICustomPropertyProvider.Type => typeof(TreeViewNode);

	ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name) => null;

	ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null;

	string ICustomPropertyProvider.GetStringRepresentation() => GetContentAsString();

	#endregion

	#region IStringable

	public override string ToString()
	{
		return GetContentAsString();
	}

	#endregion
}
