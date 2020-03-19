using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeView : Control
	{
		private const string c_listControlName = "ListControl";

		private TreeViewNode m_rootNode;
		private IList<TreeViewNode> m_pendingSelectedNodes;

		public TreeView()
		{
			DefaultStyleKey = typeof(TreeView);

			m_rootNode = new TreeViewNode();
			m_pendingSelectedNodes = new List<TreeViewNode>();

			//this.RegisterDisposablePropertyChangedCallback((s, p, e) => OnPropertyChanged(e));
		}

		internal TreeViewList ListControl { get; private set; }

		public IList<TreeViewNode> RootNodes => m_rootNode.Children;

		public object ItemFromContainer(DependencyObject container) =>
			ListControl?.ItemFromContainer(container);

		public DependencyObject ContainerFromItem(object item) =>
			ListControl?.ContainerFromItem(item);

		public TreeViewNode NodeFromContainer(DependencyObject container) =>
			ListControl?.NodeFromContainer(container);

		public DependencyObject ContainerFromNode(TreeViewNode node) =>
			ListControl?.ContainerFromNode(node);

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

		public object SelectedItem
		{
			get
			{
				return SelectedItems.Count > 0 ? SelectedItems[0] : null;
			}
			set
			{
				if (SelectedItems.Count > 0)
				{
					SelectedItems.Clear();
				}
				if (value != null)
				{
					SelectedItems.Add(value);
				}
			}
		}

		public IList<object> SelectedItems => ListControl?.ListViewModel?.SelectedItems;

		internal void UpdateSelection(TreeViewNode node, bool isSelected)
		{
			var viewModel = ListControl?.ListViewModel;
			if (viewModel != null)
			{
				if (isSelected != viewModel.IsNodeSelected(node))
				{
					var selectedNodes = viewModel.SelectedNodes;
					if (isSelected)
					{
						if (SelectionMode == TreeViewSelectionMode.Single && selectedNodes.Count > 0)
						{
							selectedNodes.Clear();
						}
						selectedNodes.Append(node);
					}
					else
					{
						var index = selectedNodes.IndexOf(node);
						if (index > -1)
						{
							selectedNodes.RemoveAt(index);
						}
					}
				}
			}
		}

		public void Expand(TreeViewNode node)
		{
			var vm = ListControl.ListViewModel;
			vm.ExpandNode(node);
		}

		public void Collapse(TreeViewNode node)
		{
			var vm = ListControl.ListViewModel;
			vm.CollapseNode(node);
		}

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


		private void UpdateItemsSelectionMode(bool isMultiSelect)
		{
			var listControl = ListControl;
			listControl.EnableMultiselect(isMultiSelect);

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

		protected override void OnApplyTemplate()
		{
			ListControl = (TreeViewList)GetTemplateChild(c_listControlName);
			if (ListControl != null)
			{
				var listPtr = ListControl;
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
				viewModel.SetOwningList(ListControl);
				viewModel.NodeExpanding += OnNodeExpanding;
				viewModel.NodeCollapsed += OnNodeCollapsed;

				var selectionMode = SelectionMode;
				if (selectionMode == TreeViewSelectionMode.Single)
				{
					ListControl.SelectionMode = ListViewSelectionMode.Single;
				}
				else
				{
					ListControl.SelectionMode = ListViewSelectionMode.None;
					if (selectionMode == TreeViewSelectionMode.Multiple)
					{
						UpdateItemsSelectionMode(true);
					}
				}

				ListControl.ItemClick += OnItemClick;
				ListControl.ContainerContentChanging += OnContainerContentChanging;
				ListControl.DragItemsStarting += OnListControlDragItemsStarting;
				ListControl.DragItemsCompleted += OnListControlDragItemsCompleted;

				if (m_pendingSelectedNodes != null && m_pendingSelectedNodes.Count > 0)
				{
					var selectedNodes = viewModel.SelectedNodes;
					foreach (var node in m_pendingSelectedNodes)
					{
						selectedNodes.Append(node);
					}
					m_pendingSelectedNodes.Clear();
				}
			}
		}
	}
}
