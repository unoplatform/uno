// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media;
using System.Collections.ObjectModel;

using TreeViewSelectionMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewSelectionMode;
using TreeViewNode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode;
using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItemInvokedEventArgs;
using TreeViewExpandingEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewExpandingEventArgs;
using TreeViewDragItemsStartingEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs;
using TreeViewDragItemsCompletedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs;
using TreeViewList = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewList;
using TreeViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItem;
//using MaterialHelperTestApi = Microsoft.UI.Private.Media.MaterialHelperTestApi;
using System.Threading.Tasks;

// Uno specific
using Uno.UI.Samples.Controls;
using MUXControlsTestApp.Utilities;
using Uno.Extensions.Specialized;

// Replace listControl.GetItems().Count() with listControl.GetItems().Count()

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	[Sample("MUX", "TreeView")]
	public sealed partial class TreeViewPage : MUXTestPage
	{
		bool _disableClickToExpand;
		public TreeViewItem flyoutTVI;
		TreeViewNode _visualRoot;
		TreeViewNode _virtualizedNode;
		public ObservableCollection<TreeViewItemSource> TestTreeViewItemsSource { get; set; }
		public ObservableCollection<TreeViewItemSource> TestTreeView2ItemsSource { get; set; }

		public TreeViewPage()
		{
			this.DataContext = this;
			this.InitializeComponent();

			//TODO:
			//MaterialHelperTestApi.IgnoreAreEffectsFast = true;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;

			_disableClickToExpand = false;

			SetupCustomDragUIOverride.Click += SetupCustomDragUIOverride_Click;

			_visualRoot = CreateTree();
			TestTreeView.RootNodes.Add(_visualRoot);

			TestTreeView2.RootNodes.Add(new TreeViewNode() { Content = "item1" });
			TestTreeView2.RootNodes.Add(new TreeViewNode() { Content = "item2" });

			TestTreeViewItemsSource = PrepareItemsSource();

			var item1 = new TreeViewItemSource() { Content = "item1" };
			var item2 = new TreeViewItemSource() { Content = "item2" };
			TestTreeView2ItemsSource = new ObservableCollection<TreeViewItemSource>() { item1, item2 };
		}

		private ObservableCollection<TreeViewItemSource> PrepareItemsSource(bool expandRootNode = false)
		{
			var root0 = new TreeViewItemSource() { Content = "Root.0" };
			var root1 = new TreeViewItemSource() { Content = "Root.1" };
			var root2 = new TreeViewItemSource() { Content = "Root.2" };
			var root = new TreeViewItemSource() { Content = "Root", Children = { root0, root1, root2 }, IsExpanded = expandRootNode };

			return new ObservableCollection<TreeViewItemSource> { root };
		}

		private async Task<ObservableCollection<TreeViewItemSource>> PrepareItemsSourceAsync()
		{
			return await Task.Run(() =>
			{
				var items = PrepareItemsSource(expandRootNode: true);

				var root0 = items[0].Children[0];
				var x0 = new TreeViewItemSource { Content = "Root.0.0" };
				root0.Children.Add(x0);

				var root1 = items[0].Children[1];
				var y0 = new TreeViewItemSource { Content = "Root.1.0" };
				var y1 = new TreeViewItemSource { Content = "Root.1.1" };
				root1.Children.Add(y0);
				root1.Children.Add(y1);
				root1.IsExpanded = true;

				return items;
			});
		}

		protected
#if !WINAPPSDK
			internal
#endif
			override void OnNavigatedFrom(NavigationEventArgs e)
		{
			// Unset all override flags to avoid impacting subsequent tests
			//TODO:
			//MaterialHelperTestApi.IgnoreAreEffectsFast = false;
			//MaterialHelperTestApi.SimulateDisabledByPolicy = false;
			base.OnNavigatedFrom(e);
		}

		private string GetSelection(TreeView tree)
		{
			int count = tree.SelectedNodes.Count;

			if (count != tree.SelectedItems.Count)
			{
				return "SelectedNodes.Count != SelectedItems.Count";
			}

			List<string> result = new List<string>();
			for (int i = 0; i < count; i++)
			{
				// Make sure selectedNodes and SelectedItems are in sync
				var node = tree.SelectedNodes[i];
				var item = IsInContentMode() ? node.Content : node;
				if (item != tree.SelectedItems[i])
				{
					return "$SelectedNodes[{i}] != SelectedItems[{i}]";
				}

				result.Add(GetNodeContent(node));
			}

			result.Sort();

			// Verify SelectedItem and SelectedNode for single selection
			if (tree.SelectionMode == TreeViewSelectionMode.Single && result.Count > 0)
			{
				if (tree.SelectedItem != tree.SelectedItems[0] || tree.SelectedNode != tree.SelectedNodes[0])
				{
					return "SelectedItem!=SelectedItems[0] || SelectedNode!=SelectedNodes[0]";
				}
			}

			return result.Count > 0 ? "Selected: " + string.Join(", ", result) : "Nothing selected";
		}

		private void GetSelected_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				Results.Text = GetSelection(ContentModeTestTreeView);
			}
			else
			{
				Results.Text = GetSelection(TestTreeView);
			}
		}

		private void TestTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			var data = ((TreeViewNode)args.InvokedItem).Content.ToString();
			Results.Text = "ItemClicked:" + data;

			if (!_disableClickToExpand)
			{
				var node = args.InvokedItem as TreeViewNode;
				node.IsExpanded = !node.IsExpanded;
			}
		}

		private void TestTreeViewContentMode_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
		{
			var item = args.InvokedItem as TreeViewItemSource;
			Results.Text = "ItemClicked:" + item.Content;

			if (!_disableClickToExpand)
			{
				item.IsExpanded = !item.IsExpanded;
			}
		}

		private void MoveNodesToNewTreeView_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				ContentModeTestTreeView.ItemsSource = null;
				TestTreeView2ItemsSource.Add(TestTreeViewItemsSource[0]);
				ContentModeTestStackPanel.Children.Remove(ContentModeTestTreeView);
			}
			else if (_visualRoot != null)
			{
				TestTreeView.RootNodes.Remove(_visualRoot);
				TestTreeView2.RootNodes.Add(_visualRoot);
				TestStackPanel.Children.Remove(TestTreeView);
			}
		}

		private void DisableClickToExpand_Click(object sender, RoutedEventArgs e)
		{
			_disableClickToExpand = true;
		}

		private void SetupExpandingNodeEvent_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				TreeViewItemSource item = new TreeViewItemSource() { Content = "Virtualized", HasUnrealizedChildren = true };
				TestTreeViewItemsSource[0].Children.Add(item);
				ContentModeTestTreeView.Expanding += ContentModeTestTreeView_Expanding;
			}
			else if (_visualRoot != null)
			{
				_virtualizedNode = new TreeViewNode() { Content = "Virtualized" };
				_virtualizedNode.HasUnrealizedChildren = true;
				_visualRoot.Children.Add(_virtualizedNode);

				TestTreeView.Expanding += TestTreeView_Expanding;
			}
		}

		private void LabelItems_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				SetAutomationIdForNodes(ContentModeTestTreeView);
			}
			else
			{
				SetAutomationIdForNodes(TestTreeView);
			}
		}

		private void SetAutomationIdForNodes(TreeView tree)
		{
			var listControl = FindVisualChildByName(this.TestTreeView, "ListControl") as TreeViewList;
			Stack<TreeViewNode> pendingNodes = new Stack<TreeViewNode>();
			foreach (var node in tree.RootNodes)
			{
				pendingNodes.Push(node);
			}
			while (pendingNodes.Count > 0)
			{
				var currentNode = pendingNodes.Pop();
				foreach (var child in currentNode.Children)
				{
					pendingNodes.Push(child);
				}

				var container = tree.ContainerFromNode(currentNode);
				if (container != null)
				{
					AutomationProperties.SetAutomationId(container as DependencyObject, GetNodeContent(currentNode));
					(container as FrameworkElement).SetValue(FrameworkElement.NameProperty, GetNodeContent(currentNode));
				}
			}
		}

		private String GetNodeContent(TreeViewNode node)
		{
			if (IsInContentMode())
			{
				TreeViewItemSource item = node.Content as TreeViewItemSource;
				return item.Content;
			}
			return node.Content.ToString();
		}

		private string GetRootNodeChildrenOrder(TreeView tree)
		{
			List<string> result = new List<string>();
			Stack<TreeViewNode> pendingNodes = new Stack<TreeViewNode>();
			pendingNodes.Push(tree.RootNodes[0]);
			while (pendingNodes.Count > 0)
			{
				var currentNode = pendingNodes.Pop();
				var size = currentNode.Children.Count;
				for (int i = 0; i < size; i++)
				{
					pendingNodes.Push(currentNode.Children[size - 1 - i]);
				}

				result.Add(GetNodeContent(currentNode));
			}

			return string.Join(" | ", result);
		}

		private string GetItemsSourceOrder()
		{
			List<string> result = new List<string>();
			Stack<TreeViewItemSource> pendingItems = new Stack<TreeViewItemSource>();
			pendingItems.Push(TestTreeViewItemsSource[0]);
			while (pendingItems.Count > 0)
			{
				var currentItem = pendingItems.Pop();
				var size = currentItem.Children.Count;
				for (int i = 0; i < size; i++)
				{
					pendingItems.Push(currentItem.Children[size - 1 - i]);
				}
				result.Add(currentItem.Content);
			}

			return string.Join(" | ", result);
		}

		private void GetChildrenOrder_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				var itemsSourceOrder = GetItemsSourceOrder();
				var treeViewNodeOrder = GetRootNodeChildrenOrder(ContentModeTestTreeView);
				// Make sure ItemsSource and TreeViewNode orders are in sync
				if (itemsSourceOrder == treeViewNodeOrder)
				{
					Results.Text = itemsSourceOrder;
				}
				else
				{
					Results.Text = $"ItemsSourceOrder: {itemsSourceOrder}; TreeViewNodeOrder: {treeViewNodeOrder}";
				}
			}
			else
			{
				Results.Text = GetRootNodeChildrenOrder(TestTreeView);
			}
		}

		private void ToggleSelectionMode_Click(object sender, RoutedEventArgs e)
		{
			var selectionMode = TreeViewSelectionMode.Single;
			if (TestTreeView.SelectionMode == TreeViewSelectionMode.Single)
			{
				selectionMode = TreeViewSelectionMode.Multiple;
			}
			else if (TestTreeView.SelectionMode == TreeViewSelectionMode.Multiple)
			{
				selectionMode = TreeViewSelectionMode.None;
			}
			else
			{
				selectionMode = TreeViewSelectionMode.Single;
			}

			TestTreeView.SelectionMode = selectionMode;
			ContentModeTestTreeView.SelectionMode = selectionMode;
		}

		private void GetItemCommonStates_Click(object sender, RoutedEventArgs e)
		{
			string commonStates = string.Empty;
			var listControl = FindVisualChildByName(this.TestTreeView, "ListControl") as TreeViewList;
			if (IsInContentMode())
			{
				listControl = FindVisualChildByName(ContentModeTestTreeView, "ListControl") as TreeViewList;
			}

			for (int i = 0; i < GetItemsCount(listControl); i++)
			{
				var container = (TreeViewItem)listControl.ContainerFromIndex(0);
				var groups = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(container, 0));
				foreach (var group in groups)
				{
					if (group.Name == "CommonStates")
					{
						commonStates += group.CurrentState.Name + " ";
					}
				}
			}

			Results.Text = commonStates;
		}

		private void TestTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
		{
			var loadingNode = args.Node;
			if (loadingNode == _virtualizedNode)
			{
				loadingNode.Children.Add(new TreeViewNode() { Content = "Loaded: 1" });
				loadingNode.Children.Add(new TreeViewNode() { Content = "Loaded: 2" });
				loadingNode.Children.Add(new TreeViewNode() { Content = "Loaded: 3" });
				loadingNode.HasUnrealizedChildren = false;

				Results.Text = "Loaded";
			}
		}

		private void ContentModeTestTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
		{
			var loadingItem = args.Item as TreeViewItemSource;
			var count = TestTreeViewItemsSource[0].Children.Count;
			if (loadingItem == TestTreeViewItemsSource[0].Children[count - 1])
			{
				loadingItem.Children.Add(new TreeViewItemSource() { Content = "Loaded: 1" });
				loadingItem.Children.Add(new TreeViewItemSource() { Content = "Loaded: 2" });
				loadingItem.Children.Add(new TreeViewItemSource() { Content = "Loaded: 3" });
				loadingItem.HasUnrealizedChildren = false;

				Results.Text = "Loaded";
			}
		}

		private void ChangeFlowDirection_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.FlowDirection = FlowDirection.RightToLeft;
			ContentModeTestTreeView.FlowDirection = FlowDirection.RightToLeft;
		}

		private void GetItemCount_Click(object sender, RoutedEventArgs e)
		{
			var listControl = FindVisualChildByName(TestTreeView, "ListControl") as TreeViewList;
			if (IsInContentMode())
			{
				listControl = FindVisualChildByName(ContentModeTestTreeView, "ListControl") as TreeViewList;
			}
			Results.Text = GetItemsCount(listControl).ToString();
		}

		private void GetTree2ItemCount_Click(object sender, RoutedEventArgs e)
		{
			var listControl = FindVisualChildByName(TestTreeView2, "ListControl") as TreeViewList;
			if (IsInContentMode())
			{
				listControl = FindVisualChildByName(ContentModeTestTreeView2, "ListControl") as TreeViewList;
			}
			Results.Text = GetItemsCount(listControl).ToString();
		}

		private void GetFlyoutTreeViewItemCount_Click(object sender, RoutedEventArgs e)
		{
			var listControl = FindVisualChildByName(this.FlyoutTreeView, "ListControl") as TreeViewList;
			flyoutTVI = (TreeViewItem)listControl.ContainerFromIndex(0);
			Results.Text = GetItemsCount(listControl).ToString();
		}

		private void SetupNoReorderNodes_Click(object sender, RoutedEventArgs e)
		{
			var treeView = IsInContentMode() ? ContentModeTestTreeView : TestTreeView;
			var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
			var lockedItem = (TreeViewItem)listControl.ContainerFromIndex(1);
			lockedItem.AllowDrop = false;

			if (IsInContentMode())
			{
				var lockedNode = treeView.ItemFromContainer(lockedItem) as TreeViewItemSource;
				lockedNode.Children.Add(new TreeViewItemSource() { Content = "Locked1" });
				lockedNode.Children.Add(new TreeViewItemSource() { Content = "Locked2" });
				lockedNode.Children.Add(new TreeViewItemSource() { Content = "Locked3" });
			}
			else
			{
				var lockedNode = treeView.NodeFromContainer(lockedItem);
				lockedNode.Children.Add(new TreeViewNode() { Content = "Locked1" });
				lockedNode.Children.Add(new TreeViewNode() { Content = "Locked2" });
				lockedNode.Children.Add(new TreeViewNode() { Content = "Locked3" });
			}
		}

		private TreeViewNode CreateTree()
		{
			var root = new TreeViewNode() { Content = "Root" };
			var root0 = new TreeViewNode() { Content = "Root.0" };
			var root1 = new TreeViewNode() { Content = "Root.1" };
			var root2 = new TreeViewNode() { Content = "Root.2" };

			root.Children.Add(root0);
			root.Children.Add(root1);
			root.Children.Add(root2);

			return root;
		}

		private void TreeViewInFlyout_Click(object sender, RoutedEventArgs e)
		{
			FrameworkElement element = sender as FrameworkElement;
			if (element != null)
			{
				if (FlyoutTreeView.RootNodes.Count == 0)
				{
					FlyoutTreeView.RootNodes.Add(CreateTree());
				}
				else
				{
					FlyoutTreeView.RootNodes[0] = CreateTree();
				}

				FlyoutBase.ShowAttachedFlyout(element);
			}
		}

		private void ChangeSelectionAfterFlyout_Click(object sender, RoutedEventArgs e)
		{
			flyoutTVI.IsSelected = false;
		}

		private void SetupDragToDropTarget_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.Height = 200;
			TestTreeView2.Visibility = Visibility.Collapsed;

			ContentModeTestTreeView.Height = 200;
			ContentModeTestTreeView2.Visibility = Visibility.Collapsed;

			TestTreeView.DragItemsStarting += TestTreeView_DragItemsStarting;
			TestTreeView.DragOver += TreeView_DragOver;
			TestTreeView.Drop += TreeView_Drop;

			ContentModeTestTreeView.DragItemsStarting += TestTreeView_DragItemsStarting;
			ContentModeTestTreeView.DragOver += TreeView_DragOver;
			ContentModeTestTreeView.Drop += TreeView_Drop;

			DraggableElement.DragStarting += Draggable_DragStarting;
		}

		private void SetupCustomDragUIOverride_Click(object sender, RoutedEventArgs e)
		{
			List<TreeViewItem> items = FindVisualChildrenByType<TreeViewItem>(this.TestTreeView);
			TreeViewItem lastItem = items.Last();

			lastItem.DragEnter += Item_DragEnter;
			AutomationProperties.SetName(lastItem, "CustomDragUIOverrideDropTarget");
		}

		private void Item_DragEnter(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			e.DragUIOverride.Caption = "test caption";
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.IsGlyphVisible = false;
			e.DragUIOverride.IsContentVisible = false;
			e.Handled = true;
		}

		private async void TreeView_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			try
			{
				var text = await e.DataView.GetTextAsync();
				Results.Text = "Dropped: " + text;
			}
			catch (Exception ex)
			{
				ExceptionMessage.Text = ex.ToString();
			}
		}

		private void Draggable_DragStarting(UIElement sender, Windows.UI.Xaml.DragStartingEventArgs args)
		{
			args.Data.SetText("Test Item");
			args.Data.RequestedOperation = DataPackageOperation.Copy;
			Results.Text = "Drag started";
		}

		private void TreeView_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Copy;
		}

		private void SizeTreeViewsForDrags_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.Height = 200;
			TestTreeView2.Height = 200;
		}

		private void DropTarget_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			e.AcceptedOperation = DataPackageOperation.Copy;
		}

		private async void DropTarget_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
		{
			try
			{
				var text = await e.DataView.GetTextAsync();
				DropTargetTextBlock.Text = text;
			}
			catch (Exception ex)
			{
				ExceptionMessage.Text = ex.ToString();
			}
		}

		private void TestTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs e)
		{
			var items = new StringBuilder();
			if (IsInContentMode())
			{
				foreach (TreeViewItemSource item in e.Items)
				{
					if (items.Length > 0)
					{
						items.AppendLine();
					}

					items.Append(item.Content);
				}
			}
			else
			{
				foreach (TreeViewNode node in e.Items)
				{
					if (items.Length > 0)
					{
						items.AppendLine();
					}

					items.Append(node.Content.ToString());
				}
			}

			e.Data.SetText(items.ToString());
			e.Data.RequestedOperation = DataPackageOperation.Copy;
		}

		private void AddSecondLevelOfNodes_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				var root0 = TestTreeViewItemsSource[0].Children[0];
				var x0 = new TreeViewItemSource { Content = "Root.0.0" };
				root0.Children.Add(x0);

				var root1 = TestTreeViewItemsSource[0].Children[1];
				var y0 = new TreeViewItemSource { Content = "Root.1.0" };
				var y1 = new TreeViewItemSource { Content = "Root.1.1" };
				var y2 = new TreeViewItemSource { Content = "Root.1.2" };
				root1.Children.Add(y0);
				root1.Children.Add(y1);
				root1.Children.Add(y2);
			}
			else if (_visualRoot != null)
			{
				var root0 = _visualRoot.Children[0];
				var x0 = new TreeViewNode { Content = "Root.0.0" };
				root0.Children.Add(x0);

				var root1 = _visualRoot.Children[1];
				var y0 = new TreeViewNode { Content = "Root.1.0" };
				var y1 = new TreeViewNode { Content = "Root.1.1" };
				var y2 = new TreeViewNode { Content = "Root.1.2" };
				root1.Children.Add(y0);
				root1.Children.Add(y1);
				root1.Children.Add(y2);
			}
		}

		private void RemoveSecondLevelOfNode_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				var root0 = TestTreeViewItemsSource[0].Children[0];
				root0.Children.RemoveAt(0);
			}
			else if (_visualRoot != null)
			{
				var root0 = _visualRoot.Children[0];
				root0.Children.RemoveAt(0);
			}
		}

		private void ModifySecondLevelOfNode_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				var y0 = new TreeViewItemSource { Content = "THIS IS NEW" };
				var root0 = TestTreeViewItemsSource[0].Children[0];
				root0.Children[0] = y0;
			}
			else if (_visualRoot != null)
			{
				var y0 = new TreeViewNode { Content = "THIS IS NEW" };
				var root0 = _visualRoot.Children[0];
				root0.Children[0] = y0;
			}
		}

		private void SetRoot1HasUnrealizedChildren_Click(object sender, RoutedEventArgs e)
		{
			if (_visualRoot != null)
			{
				var root1 = _visualRoot.Children[1];
				root1.HasUnrealizedChildren = true;
				TestTreeView.Expanding += (tv, args) =>
				{
					Results.Text = "Expanding Raised";
				};

				TestTreeViewItemsSource[0].Children[1].HasUnrealizedChildren = true;
				ContentModeTestTreeView.Expanding += (tv, args) =>
				{
					Results.Text = "Expanding Raised";
				};
			}
		}

		private void SetupDragDropHandlersForApiTest_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.Height = 200;
			TestTreeView2.Height = 200;
			TestTreeView.DragItemsStarting += DragItemsStartingForApiTest;
			TestTreeView.DragItemsCompleted += DragItemsCompletedForApiTest;
			TestTreeView2.DragEnter += DragEnterForApiTest;
			TestTreeView2.DragOver += DragOverForApiTest;
			TestTreeView2.DragLeave += DragLeaveForApiTest;
			TestTreeView2.Drop += DropForApiTest;

			ContentModeTestTreeView.Height = 200;
			ContentModeTestTreeView2.Height = 200;
			ContentModeTestTreeView.DragItemsStarting += DragItemsStartingForApiTest;
			ContentModeTestTreeView.DragItemsCompleted += DragItemsCompletedForApiTest;
			ContentModeTestTreeView2.DragEnter += DragEnterForApiTest;
			ContentModeTestTreeView2.DragOver += DragOverForApiTest;
			ContentModeTestTreeView2.DragLeave += DragLeaveForApiTest;
			ContentModeTestTreeView2.Drop += DropForApiTest;
		}

		private void DragEnterForApiTest(object sender, Windows.UI.Xaml.DragEventArgs args)
		{
			args.AcceptedOperation = DataPackageOperation.Copy;
			Results.Text = "DragEnter";
		}

		private void DragOverForApiTest(object sender, Windows.UI.Xaml.DragEventArgs args)
		{
			args.AcceptedOperation = DataPackageOperation.Copy;
			if (!Results.Text.Contains("DragOver"))
			{
				Results.Text += "->DragOver";
			}
		}

		private void DragLeaveForApiTest(object sender, Windows.UI.Xaml.DragEventArgs args)
		{
			Results.Text += "->DragLeave";
		}

		private void DropForApiTest(object sender, Windows.UI.Xaml.DragEventArgs args)
		{
			Results.Text += "->Drop";
		}

		private void DisableItemDrag_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.CanDragItems = false;
			ContentModeTestTreeView.CanDragItems = false;
		}

		private void DisableItemReorder_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.CanReorderItems = false;
			ContentModeTestTreeView.CanReorderItems = false;
		}

		private void DragItemsStartingForApiTest(TreeView sender, TreeViewDragItemsStartingEventArgs args)
		{
			Results.Text = "DragItemsStarting:" + GetDraggedItemsNames(args.Items);
		}

		private void DragItemsCompletedForApiTest(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
		{
			Results.Text += "\nDragItemsCompleted:" + GetDraggedItemsNames(args.Items);

			var parent = args.NewParentItem;
			if (parent != null)
			{
				var parentName = IsInContentMode() ? (parent as TreeViewItemSource).Content : (parent as TreeViewNode).Content.ToString();
				Results.Text += "\nNewParent: " + parentName;
			}
		}

		private String GetDraggedItemsNames(IEnumerable<object> items)
		{
			var names = new StringBuilder();
			if (IsInContentMode())
			{
				foreach (TreeViewItemSource item in items)
				{
					if (names.Length > 0)
					{
						names.Append('|');
					}

					names.Append(item.Content);
				}
			}
			else
			{
				foreach (TreeViewNode node in items)
				{
					if (names.Length > 0)
					{
						names.Append('|');
					}

					names.Append(node.Content.ToString());
				}
			}

			return names.ToString();
		}

		private void ItemTemplateSelectorTestPage_Click(object sender, RoutedEventArgs e)
		{
			GetFrame().Navigate(typeof(TreeViewItemTemplateSelectorTestPage));
		}

		private void AddNodeWithEmpyUnrealizedChildren_Click(object sender, RoutedEventArgs e)
		{
			if (_visualRoot != null)
			{
				var node = new TreeViewNode()
				{
					Content = "Root.3",
					HasUnrealizedChildren = true,
					IsExpanded = true
				};
				_visualRoot.Children.Add(node);
			}
		}

		private void SetContentMode_Click(object sender, RoutedEventArgs e)
		{
			Mode.Text = "content mode";
		}

		private void ResetContentMode_Click(object sender, RoutedEventArgs e)
		{
			Mode.Text = string.Empty;
		}

		private bool IsInContentMode()
		{
			return Mode.Text.Equals("content mode");
		}

		private void GetCheckBoxStates(TreeView tree)
		{
			StringBuilder sb = new StringBuilder();
			Stack<TreeViewNode> pendingNodes = new Stack<TreeViewNode>();
			pendingNodes.Push(tree.RootNodes[0]);
			while (pendingNodes.Count > 0)
			{
				var currentNode = pendingNodes.Pop();
				var children = currentNode.Children;
				var size = children.Count;
				for (int i = 0; i < size; i++)
				{
					pendingNodes.Push(currentNode.Children[size - 1 - i]);
				}
				var treeViewItem = tree.ContainerFromNode(currentNode) as TreeViewItem;
				var checkBox = FindVisualChildByName(treeViewItem, "MultiSelectCheckBox") as CheckBox;
				if (checkBox.IsChecked == true)
				{
					// selected
					sb.Append("s|");
				}
				else if (checkBox.IsChecked == false)
				{
					// unselected
					sb.Append("u|");
				}
				else
				{
					// partial selected
					sb.Append("p|");
				}
			}

			Results.Text = sb.ToString();
		}

		private void GetMultiSelectCheckBoxStates_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				GetCheckBoxStates(ContentModeTestTreeView);
			}
			else
			{
				GetCheckBoxStates(TestTreeView);
			}
		}

		private void ToggleSelectedNodes_Click(object sender, RoutedEventArgs e)
		{
			if (IsInContentMode())
			{
				ContentModeTestTreeView.SelectionMode = TreeViewSelectionMode.Multiple;
				var item0 = TestTreeViewItemsSource[0].Children[0];
				var item2 = TestTreeViewItemsSource[0].Children[2];
				var selectedItems = ContentModeTestTreeView.SelectedItems;
				if (selectedItems.Contains(item0))
				{
					selectedItems.Remove(item0);
					selectedItems.Remove(item2);
				}
				else
				{
					selectedItems.Add(item0);
					selectedItems.Add(item2);
				}
			}
			else
			{
				TestTreeView.SelectionMode = TreeViewSelectionMode.Multiple;
				var node0 = TestTreeView.RootNodes[0].Children[0];
				var node2 = TestTreeView.RootNodes[0].Children[2];
				var selectedNodes = TestTreeView.SelectedNodes;
				if (selectedNodes.Contains(node0))
				{
					selectedNodes.Remove(node0);
					selectedNodes.Remove(node2);
				}
				else
				{
					selectedNodes.Add(node0);
					selectedNodes.Add(node2);
				}
			}
		}

		private void AddInheritedTreeViewNode_Click(object sender, RoutedEventArgs e)
		{
			if (_visualRoot != null)
			{
				var node = new TreeViewNode2() { Content = "Inherited from TreeViewNode" };
				_visualRoot.Children.Add(node);
			}
		}

		private void ClearNodes_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (IsInContentMode())
				{
					TestTreeViewItemsSource.Clear();
				}
				else if (_visualRoot != null)
				{
					_visualRoot.Children.Clear();
					_visualRoot = null;
					TestTreeView.RootNodes.Clear();
				}
			}
			catch (Exception ex)
			{
				ExceptionMessage.Text = ex.ToString();
			}
		}

		private void AddRootNode_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (IsInContentMode())
				{
					var newNode = new TreeViewItemSource() { Content = "Root" + TestTreeViewItemsSource.Count };
					TestTreeViewItemsSource.Add(newNode);
				}
				else
				{
					var newNode = new TreeViewNode() { Content = "Root" + TestTreeView.RootNodes.Count };
					TestTreeView.RootNodes.Add(newNode);
					if (_visualRoot == null)
					{
						_visualRoot = newNode;
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionMessage.Text = ex.ToString();
			}
		}

		private void AddExtraNodes_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 1; i <= 50; i++)
			{
				TestTreeView.RootNodes.Add(new TreeViewNode() { Content = "Node " + i });
			}
		}

		private void SelectLastRootNode_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.SelectedNode = TestTreeView.RootNodes[TestTreeView.RootNodes.Count - 1];
		}

		private void TreeViewLateDataInitTestPage_Click(object sender, RoutedEventArgs e)
		{
			GetFrame().Navigate(typeof(TreeViewLateDataInitTest));
		}

		private void ToggleRoot0Selection_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.SelectedNode = TestTreeView.SelectedNode == null ? TestTreeView.RootNodes[0].Children[0] : null;
			ContentModeTestTreeView.SelectedItem = ContentModeTestTreeView.SelectedItem == null ? TestTreeViewItemsSource[0].Children[0] : null;
		}

		private void TreeViewNodeInMarkupTestPage_Click(object sender, RoutedEventArgs e)
		{
			GetFrame().Navigate(typeof(TreeViewNodeInMarkupTestPage));
		}

		private void TreeViewUnrealizedChildrenTestPage_Click(object sender, RoutedEventArgs e)
		{
			GetFrame().Navigate(typeof(TreeViewUnrealizedChildrenTestPage));
		}

		private void ClearException_Click(object sender, RoutedEventArgs e)
		{
			ExceptionMessage.Text = string.Empty;
		}

		private async void ResetItemsSourceAsync_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Uno Specific - Bindings.Update does not work correctly, setting the value directly instead.
			ContentModeTestTreeView.ItemsSource = await PrepareItemsSourceAsync();
			//this.Bindings.Update();
		}

		private void ResetItemsSource_Click(object sender, RoutedEventArgs e)
		{
			ContentModeTestTreeView.ItemsSource = PrepareItemsSource(expandRootNode: true);
		}

		private void ExpandRootNode_Click(object sender, RoutedEventArgs e)
		{
			TestTreeViewItemsSource[0].IsExpanded = !TestTreeViewItemsSource[0].IsExpanded;
		}

		private void SwapItemsSource_Click(object sender, RoutedEventArgs e)
		{
			if (ContentModeTestTreeView.ItemsSource == TestTreeView2ItemsSource)
			{
				this.ContentModeTestTreeView.ItemsSource = TestTreeViewItemsSource;
			}
			else
			{
				this.ContentModeTestTreeView.ItemsSource = TestTreeView2ItemsSource;
			}
		}

		private void TwoWayBoundButton_Click(object sender, RoutedEventArgs e)
		{
			TwoWayBoundButton.Content = TestTreeView.RootNodes[0].Children[1];
		}

		private void SelectRoot2Item_Click(object sender, RoutedEventArgs e)
		{
			TestTreeView.SelectedItem = TestTreeView.RootNodes[0].Children[2];
		}
		private void ReadBindingResult_Click(object sender, RoutedEventArgs e)
		{
			Results.Text = ((TwoWayBoundButton.Content as TreeViewNode).Content as string)
				+ ";" + ((TestTreeView.SelectedItem as TreeViewNode).Content as string);
		}

		#region Uno specific
		public int GetItemsCount(TreeViewList listControl)
		{
#if !WINAPPSDK
			return listControl.GetItems().Count();
#else
			return listControl.Items.Count;
#endif
		}

		private Frame GetFrame()
		{
			NavFrameGrid.Visibility = Visibility.Visible;
			return NavFrame;
		}
		#endregion
	}
}
