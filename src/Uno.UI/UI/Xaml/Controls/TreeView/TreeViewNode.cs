using System.Collections.Generic;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewNode : DependencyObject
	{
		private TreeViewNode _parent;
		private bool _hasUnrealizedChildren;

		public TreeViewNode()
		{
			var collection = new ObservableVector<TreeViewNode>();
			collection.SetParent(this);
			Children = collection;
			collection.VectorChanged += ChildVectorChanged;
		}

		internal object ItemsSource { get; set; }

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

		private void RaiseChildrenChanged(CollectionChange collectionChange, uint index)
		{
			var args = new VectorChangedEventArgs(collectionChange, index);
			ChildrenChanged?.Invoke(this, args);
		}

		internal enum TreeNodeSelectionState
		{
			UnSelected,
			PartialSelected,
			Selected,
		}
	}
}
