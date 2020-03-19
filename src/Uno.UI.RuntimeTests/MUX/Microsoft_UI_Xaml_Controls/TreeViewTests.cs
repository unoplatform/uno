// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Threading;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Animation;
using System.Collections.ObjectModel;
using MUXControlsTestApp;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

#if !WINDOWS_UWP
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewItem = Microsoft.UI.Xaml.Controls.TreeViewItem;
using TreeViewList = Microsoft.UI.Xaml.Controls.TreeViewList;
using TreeViewNode = Microsoft.UI.Xaml.Controls.TreeViewNode;
using TreeViewSelectionMode = Microsoft.UI.Xaml.Controls.TreeViewSelectionMode;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
	partial class SpecialTreeView : TreeView { }
	partial class SpecialTreeViewItem : TreeViewItem { }
	partial class SpecialTreeViewList : TreeViewList
	{
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new SpecialTreeViewItem();
		}
	}
	class SpecialTreeViewNode : TreeViewNode { }

	[TestClass]
	public class TreeViewTests : MUXApiTestBase
	{
		[TestMethod]
		public void TreeViewNodeTest()
		{
			RunOnUIThread.Execute(() =>
			{
				TreeViewNode treeViewNode1 = new TreeViewNode();
				TreeViewNode treeViewNode2 = new TreeViewNode();
				TreeViewNode treeViewNode3 = new TreeViewNode();

				Verify.AreEqual(false, treeViewNode1.HasChildren);
				Verify.AreEqual(false, (bool)treeViewNode1.GetValue(TreeViewNode.HasChildrenProperty));

				//Test adding a single TreeViewNode
				treeViewNode1.Children.Add(treeViewNode2);

				Verify.AreEqual(treeViewNode2.Parent, treeViewNode1);
				Verify.AreEqual(treeViewNode1.Children.Count, 1);
				Verify.AreEqual(treeViewNode1.Children[0], treeViewNode2);
				Verify.AreEqual(treeViewNode1.IsExpanded, false);
				Verify.AreEqual(treeViewNode2.Depth, 0);

				//Test removing a single TreeViweNode
				treeViewNode1.Children.RemoveAt(0);

				Verify.IsNull(treeViewNode2.Parent);
				Verify.AreEqual(treeViewNode1.Children.Count, 0);
				Verify.AreEqual(treeViewNode2.Depth, -1);

				//Test insert multiple TreeViewNodes
				treeViewNode1.Children.Insert(0, treeViewNode2);
				treeViewNode1.Children.Insert(0, treeViewNode3);

				Verify.AreEqual(treeViewNode1.Children.Count, 2);

				Verify.AreEqual(treeViewNode1.Children[0], treeViewNode3);
				Verify.AreEqual(treeViewNode3.Depth, 0);
				Verify.AreEqual(treeViewNode3.Parent, treeViewNode1);

				Verify.AreEqual(treeViewNode1.Children[1], treeViewNode2);
				Verify.AreEqual(treeViewNode2.Depth, 0);
				Verify.AreEqual(treeViewNode2.Parent, treeViewNode1);

				//Test remove multiple TreeViewNodes
				treeViewNode1.Children.RemoveAt(0);

				Verify.AreEqual(treeViewNode1.Children[0], treeViewNode2);
				Verify.AreEqual(treeViewNode1.Children.Count, 1);
				Verify.IsNull(treeViewNode3.Parent);
				Verify.AreEqual(treeViewNode3.Depth, -1);

				treeViewNode1.Children.RemoveAt(0);

				Verify.AreEqual(treeViewNode1.Children.Count, 0);
				Verify.IsNull(treeViewNode2.Parent);
				Verify.AreEqual(treeViewNode2.Depth, -1);
			});
		}

		[TestMethod]
		public void TreeViewClearAndSetAtTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();

				Content = treeView;
				Content.UpdateLayout();
				var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
				// Verify TreeViewNode::SetAt
				TreeViewNode setAtChildCheckNode = new TreeViewNode() { Content = "Set At Child" };
				TreeViewNode setAtRootCheckNode = new TreeViewNode() { Content = "Set At Root" };

				TreeViewNode child1 = new TreeViewNode() { Content = "Child 1" };
				child1.Children.Add(new TreeViewNode() { Content = "Child 1:1" });

				TreeViewNode child2 = new TreeViewNode() { Content = "Child 2" };
				child2.Children.Add(new TreeViewNode() { Content = "Child 2:1" });
				child2.Children.Add(new TreeViewNode() { Content = "Child 2:2" });

				treeView.RootNodes.Add(child1);
				child1.IsExpanded = true;
				treeView.RootNodes.Add(child2);
				Verify.AreEqual(listControl.Items.Count, 3);

				// SetAt node under child node which is not expanded
				child2.Children[1] = setAtChildCheckNode;
				Verify.AreEqual(listControl.Items.Count, 3);

				// SetAt node under root node and child2 is expanded
				treeView.RootNodes[0] = setAtRootCheckNode;
				child2.IsExpanded = true;
				Verify.AreEqual(listControl.Items.Count, 4);

				// Verify RootNode.Clear
				treeView.RootNodes.Clear();
				Verify.AreEqual(listControl.Items.Count, 0);

				// test clear without any child node
				treeView.RootNodes.Clear();
				Verify.AreEqual(listControl.Items.Count, 0);
			});
		}

		[TestMethod]
		public void TreeViewItemSourceResetRecreateItems()
		{
			RunOnUIThread.Execute(() =>
			{

				ExtendedObservableCollection<TreeViewItemSource> items
					= new ExtendedObservableCollection<TreeViewItemSource>();
				TreeViewItemSource item1 = new TreeViewItemSource() { Content = "item1" };
				TreeViewItemSource item2 = new TreeViewItemSource() { Content = "item2" };
				TreeViewItemSource item3 = new TreeViewItemSource() { Content = "item3" };
				items.Add(item1);
				items.Add(item2);
				items.Add(item3);

				var treeView = new TreeView();
				treeView.ItemsSource = items;

				Verify.AreEqual(treeView.RootNodes.Count, 3);
				Verify.AreEqual(treeView.RootNodes[0].Content as TreeViewItemSource, items[0]);

				List<TreeViewItemSource> newItems = new List<TreeViewItemSource>();
				TreeViewItemSource item4 = new TreeViewItemSource() { Content = "item4" };
				TreeViewItemSource item5 = new TreeViewItemSource() { Content = "item5" };

				newItems.Add(item4);
				newItems.Add(item5);

				items.ReplaceAll(newItems);

				Verify.AreEqual(treeView.RootNodes.Count, 2);
				Verify.AreEqual(treeView.RootNodes[0].Content as TreeViewItemSource, items[0]);


			});
		}

		[TestMethod]
		public void TreeViewUpdateTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeViewNode1 = new TreeViewNode();
				var treeViewNode2 = new TreeViewNode();
				var treeViewNode3 = new TreeViewNode();
				var treeViewNode4 = new TreeViewNode();
				var treeViewNode5 = new TreeViewNode();

				treeViewNode1.Children.Add(treeViewNode2);
				treeViewNode1.Children.Add(treeViewNode3);
				treeViewNode1.Children.Add(treeViewNode4);
				treeViewNode1.Children.Add(treeViewNode5);

				var treeView = new TreeView();
				Content = treeView;
				Content.UpdateLayout();
				var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
				treeView.RootNodes.Add(treeViewNode1);
				Verify.AreEqual(listControl.Items.Count, 1);

				treeView.Expand(treeViewNode1);
				Verify.AreEqual(listControl.Items.Count, 5);

				treeViewNode1.Children.RemoveAt(2);
				Verify.AreEqual(listControl.Items.Count, 4);
				Verify.AreEqual(listControl.Items[0], treeViewNode1);
				Verify.AreEqual(listControl.Items[1], treeViewNode2);
				Verify.AreEqual(listControl.Items[2], treeViewNode3);
				Verify.AreEqual(listControl.Items[3], treeViewNode5);

				treeViewNode1.Children.Insert(1, treeViewNode4);
				Verify.AreEqual(listControl.Items.Count, 5);
				Verify.AreEqual(listControl.Items[0], treeViewNode1);
				Verify.AreEqual(listControl.Items[1], treeViewNode2);
				Verify.AreEqual(listControl.Items[2], treeViewNode4);
				Verify.AreEqual(listControl.Items[3], treeViewNode3);
				Verify.AreEqual(listControl.Items[4], treeViewNode5);

				treeViewNode1.Children.Clear();
				Verify.AreEqual(listControl.Items.Count, 1);
				Verify.AreEqual(listControl.Items[0], treeViewNode1);
			});
		}

		//[TestMethod] Disabled with issue number #1775
		public void TreeViewInheritanceTest()
		{
			RunOnUIThread.Execute(() =>
			{
				StackPanel stackPanel = new StackPanel();
				SpecialTreeView inheritedTreeView = new SpecialTreeView();
				SpecialTreeViewList inheritedTreeViewList = new SpecialTreeViewList();
				SpecialTreeViewItem inheritedTreeViewItem = new SpecialTreeViewItem();
				SpecialTreeViewNode inheritedTreeViewNode = new SpecialTreeViewNode();
				IList<string> data = Enumerable.Range(0, 5).Select(x => "Item " + x).ToList();

				Verify.IsNotNull(stackPanel);
				stackPanel.Children.Add(inheritedTreeView);
				stackPanel.Children.Add(inheritedTreeViewList);
				inheritedTreeViewList.ItemsSource = data;
				Content = stackPanel;
				Content.UpdateLayout();
			});
		}

		[TestMethod]
		public void VerifyTreeViewIsNotTabStop()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();
				Content = treeView;
				Content.UpdateLayout();
				Verify.IsFalse(treeView.IsTabStop);
			});
		}

		[TestMethod]
		public void VerifyClearingNodeWithNoChildren()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeViewNode1 = new TreeViewNode();
				var treeView = new TreeView();

				Content = treeView;
				Content.UpdateLayout();
				var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
				treeView.RootNodes.Add(treeViewNode1);
				var children = (treeViewNode1.Children as IObservableVector<TreeViewNode>);
				children.VectorChanged += (vector, args) =>
				{
					if (((IVectorChangedEventArgs)args).CollectionChange == CollectionChange.Reset)
					{
						// should not reset if there are not children items
						throw new InvalidOperationException();
					}
				};
				Verify.AreEqual(listControl.Items.Count, 1);

				// this should no-op and not crash
				treeViewNode1.Children.Clear();
			});
		}

		[TestMethod]
		public void TreeViewNodeDPTest()
		{
			RunOnUIThread.Execute(() =>
			{
				TreeViewNode rootNode = new TreeViewNode() { Content = "Root" };
				TreeViewNode childNode = new TreeViewNode() { Content = "Child" };
				rootNode.Children.Add(childNode);
				rootNode.IsExpanded = true;

				Verify.AreEqual((string)rootNode.GetValue(TreeViewNode.ContentProperty), "Root");
				Verify.AreEqual((string)childNode.GetValue(TreeViewNode.ContentProperty), "Child");

				Verify.AreEqual((int)rootNode.GetValue(TreeViewNode.DepthProperty), -1);
				Verify.AreEqual((int)childNode.GetValue(TreeViewNode.DepthProperty), 0);

				Verify.AreEqual((bool)rootNode.GetValue(TreeViewNode.IsExpandedProperty), true);
				Verify.AreEqual((bool)childNode.GetValue(TreeViewNode.IsExpandedProperty), false);

				Verify.AreEqual((bool)rootNode.GetValue(TreeViewNode.HasChildrenProperty), true);
				Verify.AreEqual((bool)childNode.GetValue(TreeViewNode.HasChildrenProperty), false);
			});
		}

		[TestMethod]
		public void TreeViewItemTemplateTest()
		{
			RunOnUIThread.Execute(() =>
			{
				TreeView treeView = new TreeView();
				treeView.Loaded += (object sender, RoutedEventArgs e) =>
				{
					var dataTemplate = (DataTemplate)XamlReader.Load(
				   @"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> 
                          <TextBlock Text='TreeViewItemTemplate'/>
                      </DataTemplate>");
					treeView.ItemTemplate = dataTemplate;
					var node = new TreeViewNode();
					treeView.RootNodes.Add(node);
					var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
					var treeViewItem = listControl.ContainerFromItem(node) as TreeViewItem;
					Verify.AreEqual(treeViewItem.ContentTemplate, dataTemplate);
				};
			});
		}

		[TestMethod]
		public void ValidateTreeViewItemSourceChangeUpdatesChevronOpacity()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();
				var collection = new ObservableCollection<int>();
				collection.Add(5);
				treeView.ItemsSource = collection;
				Content = treeView;
				Content.UpdateLayout();
				var tvi = (TreeViewItem)treeView.ContainerFromItem(5);
				Verify.AreEqual(tvi.GlyphOpacity, 0.0);
				tvi.ItemsSource = collection;
				Content.UpdateLayout();
				Verify.AreEqual(tvi.GlyphOpacity, 1.0);
			});
		}

		[TestMethod]
		public void TreeViewItemContainerStyleTest()
		{
			RunOnUIThread.Execute(() =>
			{
				TreeView treeView = new TreeView();
				treeView.Loaded += (object sender, RoutedEventArgs e) =>
				{
					var style = (Style)XamlReader.Load(
				   @"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> 
                          <Setter Property='Background' Value='Green'/>
                      </Style>");
					treeView.ItemContainerStyle = style;
					var node = new TreeViewNode();
					treeView.RootNodes.Add(node);
					var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
					var treeViewItem = listControl.ContainerFromItem(node) as TreeViewItem;
					Verify.AreEqual(treeViewItem.Style, style);
				};
			});
		}

		[TestMethod]
		public void TreeViewItemContainerTransitionTest()
		{
			RunOnUIThread.Execute(() =>
			{
				TreeView treeView = new TreeView();
				treeView.Loaded += (object sender, RoutedEventArgs e) =>
				{
					var transition = (TransitionCollection)XamlReader.Load(
				   @"<TransitionCollection xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> 
                            <ContentThemeTransition />
                      </TransitionCollection>");
					treeView.ItemContainerTransitions = transition;
					var node = new TreeViewNode();
					treeView.RootNodes.Add(node);
					var listControl = FindVisualChildByName(treeView, "ListControl") as TreeViewList;
					var treeViewItem = listControl.ContainerFromItem(node) as TreeViewItem;
					Verify.AreEqual(treeViewItem.ContentTransitions, transition);
				};
			});
		}

		[TestMethod]
		public void TreeViewItemsSourceTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();
				var items = CreateTreeViewItemsSource();
				treeView.ItemsSource = items;

				Verify.AreEqual(treeView.RootNodes.Count, 2);
				Verify.AreEqual(treeView.RootNodes[0].Content as TreeViewItemSource, items[0]);
			});
		}

		[TestMethod]
		public void TreeViewItemsSourceUpdateTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();
				var items = CreateTreeViewItemsSource();
				treeView.ItemsSource = items;

				// Insert
				var newItem = new TreeViewItemSource() { Content = "newItem" };
				items.Add(newItem);
				Verify.AreEqual(treeView.RootNodes.Count, 3);
				var itemFromNode = treeView.RootNodes[2].Content as TreeViewItemSource;
				Verify.AreEqual(newItem.Content, itemFromNode.Content);

				// Remove
				items.Remove(newItem);
				Verify.AreEqual(treeView.RootNodes.Count, 2);

				// Replace
				var item3 = new TreeViewItemSource() { Content = "3" };
				items[1] = item3;
				itemFromNode = treeView.RootNodes[1].Content as TreeViewItemSource;
				Verify.AreEqual(item3.Content, itemFromNode.Content);

				// Clear
				items.Clear();
				Verify.AreEqual(treeView.RootNodes.Count, 0);

			});
		}

		[TestMethod]
		public void TreeViewNodeStringableTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var node = new TreeViewNode() { Content = "Node" };
				Verify.AreEqual(node.Content, node.ToString());

				// Test inherited type
				var node2 = new TreeViewNode2() { Content = "Inherited from TreeViewNode" };
				Verify.AreEqual(node2.Content, node2.ToString());
			});
		}

		[TestMethod]
		public void TreeViewPendingSelectedNodesTest()
		{
			RunOnUIThread.Execute(() =>
			{
				var treeView = new TreeView();
				treeView.SelectionMode = TreeViewSelectionMode.Multiple;

				var node1 = new TreeViewNode() { Content = "Node1" };
				var node2 = new TreeViewNode() { Content = "Node2" };
				treeView.RootNodes.Add(node1);
				treeView.RootNodes.Add(node2);
				treeView.SelectedNodes.Add(node1);

				Content = treeView;
				Content.UpdateLayout();
				Verify.AreEqual(true, IsMultiSelectCheckBoxChecked(treeView, node1));
				Verify.AreEqual(false, IsMultiSelectCheckBoxChecked(treeView, node2));
			});
		}

		//[TestMethod]
		//public void VerifyVisualTree()
		//{
		//	TreeView treeView = null;
		//	RunOnUIThread.Execute(() =>
		//	{
		//		treeView = new TreeView() { Width = 400, Height = 400 };
		//		var node1 = new TreeViewNode() { Content = "Node1" };
		//		treeView.RootNodes.Add(node1);
		//	});
		//	TestUtilities.SetAsVisualTreeRoot(treeView);

		//	VisualTreeTestHelper.VerifyVisualTree(root: treeView, masterFilePrefix: "TreeView");
		//}

		private bool IsMultiSelectCheckBoxChecked(TreeView tree, TreeViewNode node)
		{
			var treeViewItem = tree.ContainerFromNode(node) as TreeViewItem;
			var checkBox = FindVisualChildByName(treeViewItem, "MultiSelectCheckBox") as CheckBox;
			return checkBox.IsChecked == true;
		}

		public static DependencyObject FindVisualChildByName(FrameworkElement parent, string name)
		{
			if (parent.Name == name)
			{
				return parent;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

			for (int i = 0; i < childrenCount; i++)
			{
				FrameworkElement childAsFE = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;

				if (childAsFE != null)
				{
					DependencyObject result = FindVisualChildByName(childAsFE, name);

					if (result != null)
					{
						return result;
					}
				}
			}

			return null;
		}

		private ObservableCollection<TreeViewItemSource> CreateTreeViewItemsSource()
		{
			var items = new ObservableCollection<TreeViewItemSource>();
			var item1 = new TreeViewItemSource() { Content = "1" };
			var item1_1 = new TreeViewItemSource() { Content = "1.1" };
			var item2 = new TreeViewItemSource() { Content = "2" };

			item1.Children.Add(item1_1);
			items.Add(item1);
			items.Add(item2);

			return items;
		}
	}
}
