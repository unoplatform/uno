using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewNode : DependencyObject, ICustomPropertyProvider, IStringable
	{
		private TreeViewNode _parent;
		private bool _hasUnrealizedChildren;
		private object m_itemsSource = null;
		private ItemsSourceView m_itemsDataSource = null;

		public TreeViewNode()
		{
			var collection = new TreeViewNodeVector();
			collection.SetParent(this);
			Children = collection;
			collection.VectorChanged += ChildVectorChanged;

			//this.RegisterDisposablePropertyChangedCallback((s, p, e) => OnPropertyChanged(e));
		}

		public TreeViewNode Parent
		{
			get => _parent;
			internal set
			{
				_parent = value;
				UpdateDepth(value != null ? value.Depth + 1 : -1);
			}
		}

		public bool HasUnrealizedChildren
		{
			get => _hasUnrealizedChildren;
			set
			{
				_hasUnrealizedChildren = value;
				UpdateHasChildren();
			}
		}

		internal bool IsContentMode { get; set; }

		internal TreeNodeSelectionState SelectionState { get; set; }

		public IList<TreeViewNode> Children { get; }

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
			bool hasChildren = Children.Count != 0 || HasUnrealizedChildren;
			SetValue(HasChildrenProperty, hasChildren);
		}

		private void ChildVectorChanged(IObservableVector<TreeViewNode> sender, IVectorChangedEventArgs args)
		{
			var collectionChange = args.CollectionChange;
			var index = args.Index;
			UpdateHasChildren();
			RaiseChildrenChanged(collectionChange, index);
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
				m_itemsDataSource = value != null ? new ItemsSourceView(value) : null;
				if (m_itemsDataSource != null)
				{
					m_itemsDataSource.CollectionChanged += OnItemsSourceChanged;
				}
				SyncChildrenNodesWithItemsSource();
			}
		}

		private void OnItemsSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
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

		void SyncChildrenNodesWithItemsSource()
		{
			if (!AreChildrenNodesEqualToItemsSource())
			{
				var children = (TreeViewNodeVector)Children;
				children.Clear(false /* updateItemsSource */);

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

				return content as string ?? GetType().Name;
			}

			return GetType().Name;
		}

		Type ICustomPropertyProvider.Type => typeof(TreeViewNode);

		ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name) => null;

		ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null;

		string ICustomPropertyProvider.GetStringRepresentation() => GetContentAsString();

		public override string ToString()
		{
			return GetContentAsString();
		}
	}
}
