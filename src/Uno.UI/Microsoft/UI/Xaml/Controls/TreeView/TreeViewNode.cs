using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewNode : DependencyObject
	{
		private TreeViewNode _parent;
		private bool _hasUnrealizedChildren;
		private object m_itemsSource = null;

		public TreeViewNode()
		{
			var collection = new TreeViewNodeVector();
			collection.SetParent(this);
			Children = collection;
			collection.VectorChanged += ChildVectorChanged;
		}

		public TreeViewNode Parent
		{
			get => _parent;
			private set
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
			//TODO:
			//m_propertyChangedEventSource(this, args);
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
				//TODO:
				//m_itemItemsSourceViewChangedRevoker.revoke();
				m_itemsSource = value;
				//m_itemsDataSource = value != null ? new ItemsSourceView(value) : null;
				//if (m_itemsDataSource)
				//{
				//	m_itemItemsSourceViewChangedRevoker = m_itemsDataSource.CollectionChanged(auto_revoke, { this, &TreeViewNode.OnItemsSourceChanged });
				//}
				//SyncChildrenNodesWithItemsSource();
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
						if (m_itemsDataSource.Count != Children.Count))
						{
							AddToChildrenNodes(args.NewStartingIndex(), args.NewItems().Size());
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
						if (m_itemsDataSource.Count() != static_cast<int>(Children().Size()))
						{
							RemoveFromChildrenNodes(args.OldStartingIndex(), args.OldItems().Size());
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
						RemoveFromChildrenNodes(args.OldStartingIndex(), args.OldItems().Size());
						AddToChildrenNodes(args.NewStartingIndex(), args.NewItems().Size());
						break;
					}
			}
		}

		//	void TreeViewNode.AddToChildrenNodes(int index, int count)
		//	{
		//		for (int i = index + count - 1; i >= index; i--)
		//		{
		//			auto item = m_itemsDataSource.GetAt(i);
		//			auto node = make_self<TreeViewNode>();
		//			node->Content(item);
		//			get_self<TreeViewNodeVector>(Children())->InsertAt(index, *node, false /* updateItemsSource */);
		//		}
		//	}

		//	void TreeViewNode.RemoveFromChildrenNodes(int index, int count)
		//	{
		//		for (int i = 0; i < count; i++)
		//		{
		//			get_self<TreeViewNodeVector>(Children())->RemoveAt(index, false /* updateItemsSource */);
		//		}
		//	}

		//	void TreeViewNode.SyncChildrenNodesWithItemsSource()
		//	{
		//		if (!AreChildrenNodesEqualToItemsSource())
		//		{
		//			auto children = get_self<TreeViewNodeVector>(Children());
		//			children->Clear(false /* updateItemsSource */);

		//			auto size = m_itemsDataSource ? m_itemsDataSource.Count() : 0;
		//			for (auto i = 0; i < size; i++)
		//			{
		//				auto item = m_itemsDataSource.GetAt(i);
		//				auto node = make_self<TreeViewNode>();
		//				node->Content(item);
		//				node->IsContentMode(true);
		//				children->Append(*node, false /* updateItemsSource */);
		//			}
		//		}
		//	}

		//	bool TreeViewNode.AreChildrenNodesEqualToItemsSource()
		//	{
		//		auto children = Children();
		//		UINT32 childrenCount = children ? children.Size() : 0;
		//		UINT32 itemsSourceCount = m_itemsDataSource ? m_itemsDataSource.Count() : 0;

		//		if (childrenCount != itemsSourceCount)
		//		{
		//			return false;
		//		}

		//		// Compare the actual content in collections when counts are equal
		//		for (UINT32 i = 0; i < itemsSourceCount; i++)
		//		{
		//			if (children.GetAt(i).Content() != m_itemsDataSource.GetAt(i))
		//			{
		//				return false;
		//			}
		//		}

		//		return true;
		//	}

		//	hstring TreeViewNode.GetContentAsString()
		//	{
		//		if (auto content = Content())
		//    {
		//			if (auto result = content.try_as<ICustomPropertyProvider>())
		//        {
		//				return result.GetStringRepresentation();
		//			}

		//			if (auto result = content.try_as<IStringable>())
		//        {
		//				return result.ToString();
		//			}

		//			return unbox_value_or<hstring>(content, Type().Name);
		//		}

		//		return Type().Name;
		//	}

		//#pragma region ICustomPropertyProvider

		//	TypeName TreeViewNode.Type()
		//	{
		//		auto outer = get_strong().as< IInspectable > ();
		//		TypeName typeName;
		//		typeName.Kind = TypeKind.Metadata;
		//		typeName.Name = get_class_name(outer);
		//		return typeName;
		//	}

		//	ICustomProperty TreeViewNode.GetCustomProperty(hstring const& name)
		//	{
		//		return nullptr;
		//	}

		//	ICustomProperty TreeViewNode.GetIndexedProperty(hstring const& name, TypeName const& type)
		//	{
		//		return nullptr;
		//	}

		//	hstring TreeViewNode.GetStringRepresentation()
		//	{
		//		return GetContentAsString();
		//	}

		//#pragma endregion

		//#pragma region IStringable
		//	hstring TreeViewNode.ToString()
		//	{
		//		return GetContentAsString();
		//	}
		//#pragma endregion

		internal enum TreeNodeSelectionState
		{
			UnSelected,
			PartialSelected,
			Selected,
		}
	}
}
