using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Interop;
using PropertyChangedEventHandler = System.ComponentModel.PropertyChangedEventHandler;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using Type = System.Type;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SelectionModel : global::System.ComponentModel.INotifyPropertyChanged, ICustomPropertyProvider
	{
		public SelectionModel()
		{
			// Parent is null for root node.
			m_rootNode = new SelectionNode(this, null /* parent */);
			// Parent is null for leaf node since it is shared. This is ok since we just
			// use the leaf as a placeholder and never ask stuff of it.
			m_leafNode = new SelectionNode(this, null /* parent */);
		}

		~SelectionModel()
		{
			ClearSelection(false /*resetAnchor*/, false /*raiseSelectionChanged*/);
			m_rootNode = null;
			m_leafNode = null;
			m_selectedIndicesCached = null;
			m_selectedItemsCached = null;
		}

		#region ISelectionModel

		public object Source
		{
			get => m_rootNode.Source;
			set
			{
				ClearSelection(true /* resetAnchor */, false /* raiseSelectionChanged */);
				m_rootNode.Source = value;
				OnSelectionChanged();
				RaisePropertyChanged(nameof(Source));
			}
		}

		public bool SingleSelect
		{
			get => m_singleSelect;
			set
			{
				if (m_singleSelect != !!value)
				{
					m_singleSelect = value;
					var selectedIndices = SelectedIndices;

					// Only update selection and raise SelectionChanged event when:
					// - we switch from SelectionMode.Multiple to SelectionMode.Single and
					// - more than one item was selected at the time of the switch
					if (value && selectedIndices != null && selectedIndices.Count > 1)
					{
						// We want to be single select, so make sure there is only 
						// one selected item.
						var firstSelectionIndexPath = selectedIndices[0];
						ClearSelection(true /* resetAnchor */, false /*raiseSelectionChanged */);
						SelectWithPathImpl(firstSelectionIndexPath, true /* select */, true /* raiseSelectionChanged */);
					}

					RaisePropertyChanged(nameof(SingleSelect));
				}
			}
		}

		public IndexPath AnchorIndex
		{
			get
			{
				IndexPath anchor = null;
				if (m_rootNode.AnchorIndex >= 0)
				{
					List<int> path = new List<int>();
					var current = m_rootNode;
					while (current != null && current.AnchorIndex >= 0)
					{
						path.Add(current.AnchorIndex);
						current = current.GetAt(current.AnchorIndex, false);
					}

					anchor = new IndexPath(path);
				}

				return anchor;
			}
			set
			{
				if (value != null)
				{
					SelectionTreeHelper.TraverseIndexPath(
						m_rootNode,
						value,
						true, /* realizeChildren */
						(SelectionNode currentNode, IndexPath path, int depth, int childIndex) =>
						{
							currentNode.AnchorIndex = path.GetAt(depth);
						}
					);
				}
				else
				{
					m_rootNode.AnchorIndex = -1;
				}

				RaisePropertyChanged("AnchorIndex");
			}
		}

		public IndexPath SelectedIndex
		{
			get
			{
				IndexPath selectedIndex = null;
				var selectedIndices = SelectedIndices;
				if (selectedIndices != null && selectedIndices.Count > 0)
				{
					selectedIndex = selectedIndices[0];
				}

				return selectedIndex;
			}
			set
			{
				var isSelected = IsSelectedAt(value);
				if (isSelected == null || !isSelected.Value)
				{
					ClearSelection(true /* resetAnchor */, false /*raiseSelectionChanged */);
					SelectWithPathImpl(value, true /* select */, false /* raiseSelectionChanged */);
					OnSelectionChanged();
				}
			}
		}

		public object SelectedItem
		{
			get
			{
				object item = null;
				var selectedItems = SelectedItems;
				if (selectedItems != null && selectedItems.Count > 0)
				{
					item = selectedItems[0];
				}

				return item;
			}
		}

		public IReadOnlyList<object> SelectedItems
		{
			get
			{
				if (m_selectedItemsCached == null)
				{
					List<SelectedItemInfo> selectedInfos = new List<SelectedItemInfo>();
					if (m_rootNode.Source != null)
					{
						SelectionTreeHelper.Traverse(
							m_rootNode,
							false,/* realizeChildren */
							(SelectionTreeHelper.TreeWalkNodeInfo currentInfo) =>
							{
								if (currentInfo.Node.SelectedCount > 0)
								{
									selectedInfos.Add(new SelectedItemInfo()
									{
										Node = new WeakReference<SelectionNode>(currentInfo.Node),
										Path = currentInfo.Path
									});
								}
							});
					}

					// Instead of creating a dumb vector that takes up the space for all the selected items,
					// we create a custom VectorView implimentation that calls back using a delegate to find 
					// the selected item at a particular index. This avoid having to create the storage and copying
					// needed in a dumb vector. This also allows us to expose a tree of selected nodes into an 
					// easier to consume flat vector view of objects.
					var selectedItems = new SelectedItems<object>(
						selectedInfos,
						(IList<SelectedItemInfo> infos, int index) => // callback for GetAt(index)
						{
							int currentIndex = 0;
							object item = null;
							foreach (var info in infos)
							{
								if (info.Node.TryGetTarget(out var node))
								{
									int currentCount = node.SelectedCount;
									if (index >= currentIndex && index < currentIndex + currentCount)
									{
										int targetIndex = node.SelectedIndices[index - currentIndex];
										item = node.ItemsSourceView.GetAt(targetIndex);
										break;
									}

									currentIndex += currentCount;
								}
								else
								{
									throw new InvalidOperationException("Selection has changed since SelectedItems property was read.");
								}
							}

							return item;
						});

					m_selectedItemsCached = selectedItems;
				}

				return m_selectedItemsCached;
			}
		}

		public IReadOnlyList<IndexPath> SelectedIndices
		{
			get
			{
				if (m_selectedIndicesCached == null)
				{
					List<SelectedItemInfo> selectedInfos = new List<SelectedItemInfo>();
					SelectionTreeHelper.Traverse(
						m_rootNode,
						false, /* realizeChildren */
						(SelectionTreeHelper.TreeWalkNodeInfo currentInfo) =>
						{
							if (currentInfo.Node.SelectedCount > 0)
							{
								selectedInfos.Add(new SelectedItemInfo() { Node = new WeakReference<SelectionNode>(currentInfo.Node), Path = currentInfo.Path });
							}
						});

					// Instead of creating a dumb vector that takes up the space for all the selected indices,
					// we create a custom VectorView implimentation that calls back using a delegate to find 
					// the IndexPath at a particular index. This avoid having to create the storage and copying
					// needed in a dumb vector. This also allows us to expose a tree of selected nodes into an 
					// easier to consume flat vector view of IndexPaths.
					var indices = new SelectedItems<IndexPath>(
						selectedInfos,
						(IList<SelectedItemInfo> infos, int index) => // callback for GetAt(index)
						{
							int currentIndex = 0;
							IndexPath path = null;
							foreach (var info in infos)
							{
								if (info.Node.TryGetTarget(out var node))
								{
									int currentCount = node.SelectedCount;
									if (index >= currentIndex && index < currentIndex + currentCount)
									{
										int targetIndex = node.SelectedIndices[index - currentIndex];
										path = info.Path.CloneWithChildIndex(targetIndex);
										break;
									}

									currentIndex += currentCount;
								}

								else
								{
									throw new InvalidOperationException("Selection has changed since SelectedIndices property was read.");
								}
							}

							return path;
						});

					m_selectedIndicesCached = indices;
				}

				return m_selectedIndicesCached;
			}
		}

		public void SetAnchorIndex(int index)
		{
			AnchorIndex = new IndexPath(index);
		}

		public void SetAnchorIndex(int groupIndex, int itemIndex)
		{
			AnchorIndex = new IndexPath(groupIndex, itemIndex);
		}

		public void Select(int index)
		{
			SelectImpl(index, true /* select */);
		}

		public void Select(int groupIndex, int itemIndex)
		{
			SelectWithGroupImpl(groupIndex, itemIndex, true /* select */);
		}

		public void SelectAt(IndexPath index)
		{
			SelectWithPathImpl(index, true /* select */, true /* raiseSelectionChanged */);
		}

		public void Deselect(int index)
		{
			SelectImpl(index, false /* select */);
		}

		public void Deselect(int groupIndex, int itemIndex)
		{
			SelectWithGroupImpl(groupIndex, itemIndex, false /* select */);
		}

		public void DeselectAt(IndexPath index)
		{
			SelectWithPathImpl(index, false /* select */, true /* raiseSelectionChanged */);
		}

		public bool? IsSelected(int index)
		{
			MUX_ASSERT(index >= 0);
			var isSelected = m_rootNode.IsSelectedWithPartial(index);
			return isSelected;
		}

		public bool? IsSelected(int groupIndex, int itemIndex)
		{
			MUX_ASSERT(groupIndex >= 0 && itemIndex >= 0);
			bool? isSelected = false;
			var childNode = m_rootNode.GetAt(groupIndex, false /*realizeChild*/);
			if (childNode != null)
			{
				isSelected = childNode.IsSelectedWithPartial(itemIndex);
			}

			return isSelected;
		}

		public bool? IsSelectedAt(IndexPath index)
		{
			var path = index;
			MUX_ASSERT(path.IsValid());
			bool isRealized = true;
			var node = m_rootNode;
			for (int i = 0; i < path.GetSize() - 1; i++)
			{
				var childIndex = path.GetAt(i);
				node = node.GetAt(childIndex, false /* realizeChild */);
				if (node == null)
				{
					isRealized = false;
					break;
				}
			}

			bool? isSelected = false;
			if (isRealized)
			{
				var size = path.GetSize();
				if (size == 0)
				{
					isSelected = SelectionNode.ConvertToNullableBool(node.EvaluateIsSelectedBasedOnChildrenNodes());
				}
				else
				{
					isSelected = node.IsSelectedWithPartial(path.GetAt(size - 1));
				}
			}

			return isSelected;
		}

		public void SelectRangeFromAnchor(int index)
		{
			SelectRangeFromAnchorImpl(index, true /* select */ );
		}

		public void SelectRangeFromAnchor(int endGroupIndex, int endItemIndex)
		{
			SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, true /* select */);
		}

		public void SelectRangeFromAnchorTo(IndexPath index)
		{
			SelectRangeImpl(AnchorIndex, index, true /* select */);
		}

		public void DeselectRangeFromAnchor(int index)
		{
			SelectRangeFromAnchorImpl(index, false /* select */);
		}

		public void DeselectRangeFromAnchor(int endGroupIndex, int endItemIndex)
		{
			SelectRangeFromAnchorWithGroupImpl(endGroupIndex, endItemIndex, false /* select */);
		}

		public void DeselectRangeFromAnchorTo(IndexPath index)
		{
			SelectRangeImpl(AnchorIndex, index, false /* select */);
		}


		public void SelectRange(IndexPath start, IndexPath end)
		{
			SelectRangeImpl(start, end, true /* select */);
		}

		public void DeselectRange(IndexPath start, IndexPath end)
		{
			SelectRangeImpl(start, end, false /* select */);
		}

		public void SelectAll()
		{
			SelectionTreeHelper.Traverse(
				m_rootNode,
				true, /* realizeChildren */
				(SelectionTreeHelper.TreeWalkNodeInfo info) =>

			{
				if (info.Node.DataCount > 0)
				{
					info.Node.SelectAll();
				}
			});

			OnSelectionChanged();
		}

		// This function assumes a flat list data source.
		// This allows us to avoid devirtualizing all the items.
		internal void SelectAllFlat()
		{
			m_rootNode.SelectAll();
			OnSelectionChanged();
		}


		public void ClearSelection()
		{
			ClearSelection(true /*resetAnchor*/, true /* raiseSelectionChanged */);
		}

		#endregion

		#region ICustomPropertyProvider

		Type ICustomPropertyProvider.Type => GetType();

		ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name)
		{
			// Exposing SelectedItem through ICustomPropertyProvider so that Binding can work 
			// for SelectedItem. This is requried since SelectedItem is not a dependency proeprty and
			// is evaluated when requested.
			if (name == "SelectedItem")
			{
				var selectedItemCustomProperty = new CustomProperty(
					nameof(SelectedItem) /* name */,
					typeof(object) /* typeName */,
					(target) => ((SelectionModel)target).SelectedItem /* getter */,
					null /* setter */);
				return selectedItemCustomProperty;
			}

			return null;
		}

		// No indexed properties exposed via ICustomPropertyProvider
		ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null;

		string ICustomPropertyProvider.GetStringRepresentation() => "SelectionModel";

		#endregion

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region ISelectionModelProtected

		protected void OnPropertyChanged(string propertyName)
		{
			RaisePropertyChanged(propertyName);
		}

		#endregion

		private void RaisePropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		internal void OnSelectionInvalidatedDueToCollectionChange()
		{
			m_selectionInvalidatedDueToCollectionChange = true;
			try
			{
				OnSelectionChanged();
			}
			finally
			{
				m_selectionInvalidatedDueToCollectionChange = false;
			}
		}

		internal object ResolvePath(object data, IndexPath dataIndexPath)
		{
			object resolved = null;
			// Raise ChildrenRequested event if there is a handler
			if (ChildrenRequested != null)
			{
				if (m_childrenRequestedEventArgs == null)
				{
					m_childrenRequestedEventArgs = new SelectionModelChildrenRequestedEventArgs(data, dataIndexPath, false /*throwOnAccess*/);
				}
				else
				{
					m_childrenRequestedEventArgs.Initialize(data, dataIndexPath, false /*throwOnAccess*/);
				}

				ChildrenRequested?.Invoke(this, m_childrenRequestedEventArgs);
				resolved = m_childrenRequestedEventArgs.Children;

				// Clear out the values in the args so that it cannot be used after the event handler call.
				m_childrenRequestedEventArgs.Initialize(null, null, true /*throwOnAccess*/);
			}
			else
			{
				// No handlers for ChildrenRequested event. If data is of type ItemsSourceView
				// or a type that can be used to create a ItemsSourceView using ItemsSourceView.CreateFrom, then we can
				// auto-resolve that as the child. If not, then we consider the value as a leaf. This is to 
				// avoid having to provide the event handler for the most common scenarios. If the app dev does
				// not want this default behavior, they can provide the handler to override.
				//TODO: MZ: Exhaustive enough?
				if (data is ItemsSourceView ||
					data is IBindableObservableVector ||
					data is IList<object> ||
					data is IEnumerable<object>)
				{
					resolved = data;
				}
			}

			return resolved;
		}

		private void ClearSelection(bool resetAnchor, bool raiseSelectionChanged)
		{
			SelectionTreeHelper.Traverse(
				m_rootNode,
				false, /* realizeChildren */
				(SelectionTreeHelper.TreeWalkNodeInfo info) =>
				{
					info.Node.Clear();
				});

			if (resetAnchor)
			{
				AnchorIndex = null;
			}

			if (raiseSelectionChanged)
			{
				OnSelectionChanged();
			}
		}

		private void OnSelectionChanged()
		{
			m_selectedIndicesCached = null;
			m_selectedItemsCached = null;

			// Raise SelectionChanged event
			if (SelectionChanged != null)
			{
				if (m_selectionChangedEventArgs == null)
				{
					m_selectionChangedEventArgs = new SelectionModelSelectionChangedEventArgs();
				}

				SelectionChanged?.Invoke(this, m_selectionChangedEventArgs);
			}

			RaisePropertyChanged("SelectedIndex");
			RaisePropertyChanged("SelectedIndices");
			if (m_rootNode.Source != null)
			{
				RaisePropertyChanged("SelectedItem");
				RaisePropertyChanged("SelectedItems");
			}
		}

		private void SelectImpl(int index, bool select)
		{
			if (m_rootNode.IsSelected(index) != select)
			{
				if (m_singleSelect)
				{
					ClearSelection(true /*resetAnchor*/, false /* raiseSelectionChanged */);
				}
				var selected = m_rootNode.Select(index, select);
				if (selected)
				{
					AnchorIndex = new IndexPath(index);
				}
				OnSelectionChanged();
			}
		}

		private void SelectWithGroupImpl(int groupIndex, int itemIndex, bool select)
		{
			if (m_singleSelect)
			{
				ClearSelection(true /*resetAnchor*/, false /* raiseSelectionChanged */);
			}

			var childNode = m_rootNode.GetAt(groupIndex, true /* realize */);
			var selected = childNode.Select(itemIndex, select);
			if (selected)
			{
				AnchorIndex = new IndexPath(groupIndex, itemIndex);
			}

			OnSelectionChanged();
		}

		private void SelectWithPathImpl(IndexPath index, bool select, bool raiseSelectionChanged)
		{
			bool newSelection = true;

			// Handle single select differently as comparing indexpaths is faster
			if (m_singleSelect)
			{
				var selectedIndex = SelectedIndex;
				if (selectedIndex != null)
				{
					// If paths are equal and we want to select, skip everything and do nothing
					if (select && selectedIndex.CompareTo(index) == 0)
					{
						newSelection = false;
					}
				}
				else
				{
					// If we are in single select and selectedIndex is null, deselecting is not a new change.
					// Selecting something is a new change, so set flag to appropriate value here.
					newSelection = select;
				}
			}

			// Selection is actually different from previous one, so update.
			if (newSelection)
			{
				bool selected = false;
				// If we unselect something, raise event any way, otherwise changedSelection is false
				bool changedSelection = false;

				// We only need to clear selection by walking the data structure from the beginning when:
				// - we are in single selection mode and 
				// - want to select something.
				// 
				// If we want to unselect something we unselect it directly in TraverseIndexPath below and raise the SelectionChanged event
				// if required.
				if (m_singleSelect && select)
				{
					ClearSelection(true /*resetAnchor*/, false /* raiseSelectionChanged */);
				}

				SelectionTreeHelper.TraverseIndexPath(
					m_rootNode,
					index,
					true, /* realizeChildren */
					(SelectionNode currentNode, IndexPath path, int depth, int childIndex) =>
					{
						if (depth == path.GetSize() - 1)
						{
							if (currentNode.IsSelected(childIndex) != select)
							{
								// Node has different value then we want to set, so lets update!
								changedSelection = true;
							}
							selected = currentNode.Select(childIndex, select);
						}
					}
				);

				if (selected)
				{
					AnchorIndex = index;
				}

				// The walk tree operation can change the indices, and the next time it get's read,
				// we would throw an exception. That's what we are preventing with next two lines
				m_selectedIndicesCached = null;
				m_selectedItemsCached = null;

				if (raiseSelectionChanged && changedSelection)
				{
					OnSelectionChanged();
				}
			}
		}

		private void SelectRangeFromAnchorImpl(int index, bool select)
		{
			int anchorIndex = 0;
			var anchor = AnchorIndex;
			if (anchor != null)
			{
				MUX_ASSERT(anchor.GetSize() == 1);
				anchorIndex = anchor.GetAt(0);
			}

			bool selected = m_rootNode.SelectRange(new IndexRange(anchorIndex, index), select);
			if (selected)
			{
				OnSelectionChanged();
			}
		}

		private void SelectRangeFromAnchorWithGroupImpl(int endGroupIndex, int endItemIndex, bool select)
		{
			int startGroupIndex = 0;
			int startItemIndex = 0;
			var anchorIndex = AnchorIndex;
			if (anchorIndex != null)
			{
				MUX_ASSERT(anchorIndex.GetSize() == 2);
				startGroupIndex = anchorIndex.GetAt(0);
				startItemIndex = anchorIndex.GetAt(1);
			}

			// Make sure start > end
			if (startGroupIndex > endGroupIndex ||
				(startGroupIndex == endGroupIndex && startItemIndex > endItemIndex))
			{
				int temp = startGroupIndex;
				startGroupIndex = endGroupIndex;
				endGroupIndex = temp;
				temp = startItemIndex;
				startItemIndex = endItemIndex;
				endItemIndex = temp;
			}

			bool selected = false;
			for (int groupIdx = startGroupIndex; groupIdx <= endGroupIndex; groupIdx++)
			{
				var groupNode = m_rootNode.GetAt(groupIdx, true /* realizeChild */);
				int startIndex = groupIdx == startGroupIndex ? startItemIndex : 0;
				int endIndex = groupIdx == endGroupIndex ? endItemIndex : groupNode.DataCount - 1;
				selected |= groupNode.SelectRange(new IndexRange(startIndex, endIndex), select);
			}

			if (selected)
			{
				OnSelectionChanged();
			}
		}

		private void SelectRangeImpl(IndexPath start, IndexPath end, bool select)
		{
			var winrtStart = start;
			var winrtEnd = end;

			// Make sure start <= end 
			if (winrtEnd.CompareTo(winrtStart) == -1)
			{
				var temp = winrtStart;
				winrtStart = winrtEnd;
				winrtEnd = temp;
			}

			// Note: Since we do not know the depth of the tree, we have to walk to each leaf
			SelectionTreeHelper.TraverseRangeRealizeChildren(
				m_rootNode,
				winrtStart,
				winrtEnd,
				(SelectionTreeHelper.TreeWalkNodeInfo info) =>
				{
					if (info.Node.DataCount == 0)
					{
						// Select only leaf nodes
						info.ParentNode.Select(info.Path.GetAt(info.Path.GetSize() - 1), select);
					}
				});

			OnSelectionChanged();
		}
	}
}
