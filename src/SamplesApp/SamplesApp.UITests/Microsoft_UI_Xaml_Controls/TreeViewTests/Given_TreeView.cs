using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.TreeViewTests
{
	[ActivePlatforms(Platform.iOS)]
	public partial class Given_TreeView : SampleControlUITestBase
	{
		// Many UI tests were skipped as required UI test features are not supported in Uno
		// (Keyboard navigation, Drag and Drop, Gamepad, Automation interaction)

		[SetUp]
		public void TestSetup()
		{
			Run("UITests.Shared.Microsoft_UI_Xaml_Controls.TreeViewTests.TreeViewPage");
		}

		private void ExpandCollapseTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				var itemRoot = LabelFirstItem();

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				itemRoot.FastTap();

				// Should be expanded now
				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());

				ClickButton("AddSecondLevelOfNodes");
				ClickButton("LabelItems");

				var root0 = QueryAll("Root.0");
				root0.Tap();
				ClickButton("GetItemCount");
				Assert.AreEqual("5", ReadResult());

				ClickButton("LabelItems");

				itemRoot.FastTap();
				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				ClickButton("LabelItems");

				itemRoot.FastTap();
				ClickButton("GetItemCount");
				Assert.AreEqual("5", ReadResult());


				// TODO: Uno specific! needed to ensure automation ids
				// are assigned to correct item containers
				ClickButton("LabelItems");


				var root1 = QueryAll("Root.1");
				root1.FastTap();
				ClickButton("GetItemCount");
				Assert.AreEqual("8", ReadResult());

				// Collapse and expand
				itemRoot.FastTap();
				itemRoot.FastTap();
				ClickButton("GetItemCount");
				Assert.AreEqual("8", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void ExpandCollapseTest_NodeMode()
		{
			ExpandCollapseTest();
		}

		[Test]
		[AutoRetry]
		public void ExpandCollapseTest_ContentMode()
		{
			ExpandCollapseTest(isContentMode: true);
		}

		private void TreeViewItemClickTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				var ItemRoot = LabelFirstItem();

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				ItemRoot.FastTap();

				// Should be expanded now
				Assert.AreEqual("ItemClicked:Root", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewItemClickTest_NodeMode()
		{
			TreeViewItemClickTest();
		}

		[Test]
		[AutoRetry]
		public void TreeViewItemClickTest_ContentMode()
		{
			TreeViewItemClickTest(isContentMode: true);
		}

		[Test]
		[AutoRetry]
		public void FlyoutTreeViewItemClickTest()
		{
			{
				// click button to popup flyout treeview
				ClickButton("TreeViewInFlyout");

				// expand tree
				TapOnFlyoutTreeViewRootItemChevron();

				ClickButton("GetFlyoutItemCount");
				Assert.AreEqual("4", ReadResult());

				// click button to hide flyout treeview
				TapOutsideFlyout(); //TODO: Uno Specific - flyout may hide the button, use Clear instead

				// click button to popup flyout treeview again
				ClickButton("TreeViewInFlyout");

				//Expand once more
				TapOnFlyoutTreeViewRootItemChevron();
				ClickButton("GetFlyoutItemCount");
				Assert.AreEqual("4", ReadResult());
			}
		}

		private void TreeViewSwappingNodesTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				var ItemRoot = LabelFirstItem();

				ItemRoot.FastTap();

				// Should be expanded now
				Assert.AreEqual("ItemClicked:Root", ReadResult());

				ClickButton("MoveNodesToNewTreeView");

				//Wait.ForIdle();
				ClickButton("GetTree2ItemCount");
				Assert.AreEqual("6", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewSwappingNodesTest_NodeMode()
		{
			TreeViewSwappingNodesTest();
		}

		[Test]
		[Ignore("Currently failing on WASM, issue #4082")]
		[AutoRetry]
		public void TreeViewSwappingNodesTest_ContentMode()
		{
			TreeViewSwappingNodesTest(isContentMode: true);
		}

		[Test]
		[AutoRetry]
		public void TreeViewDensityChange()
		{
			{
				var root = LabelFirstItem().FirstResult();
				int height = (int)root.Rect.Height;
				Assert.AreEqual(height, 32);
			}
		}

		[Test]
		[AutoRetry]
		public void FlyoutTreeViewSelectionChangedCrash()
		{
			{
				// click button to popup flyout treeview
				ClickButton("TreeViewInFlyout");

				// expand tree
				TapOnFlyoutTreeViewRootItemChevron();

				ClickButton("GetFlyoutItemCount");
				Assert.AreEqual("4", ReadResult());

				// tap button to hide flyout treeview
				var flyoutButton = QueryAll("TreeViewInFlyout");
				Assert.IsNotNull(flyoutButton, "Verifying that we found the button");
				flyoutButton.FastTap();
				//Wait.ForIdle();

				//Click Button to test for crash
				ClickButton("ChangeSelectionAfterFlyout");

				Console.WriteLine("Congratulations, no crash!");
			}
		}

		private void TreeViewMultiSelectItemTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				ClickButton("ToggleSelectionMode");
				ClickButton("LabelItems");

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				var ItemRoot = LabelFirstItem().FirstResult();

				_app.TapCoordinates((float)(ItemRoot.Rect.X + ItemRoot.Rect.Width * .75), (float)(ItemRoot.Rect.Y + ItemRoot.Rect.Height * .5));

				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());
				ClickButton("LabelItems");

				_app.TapCoordinates((float)(ItemRoot.Rect.X + ItemRoot.Rect.Width * .05), (float)(ItemRoot.Rect.Y + ItemRoot.Rect.Height * .5));

				ClickButton("GetSelected");
				Assert.AreEqual("Selected: Root, Root.0, Root.1, Root.2", ReadResult());

				Console.WriteLine("Retrieve first item as generic UIElement");
				var Item0 = QueryAll("Root.0").FirstResult();
				Assert.IsNotNull(ItemRoot, "Verifying that we found a UIElement called Root.0");

				_app.TapCoordinates((float)(Item0.Rect.X + Item0.Rect.Width * .1), (float)(Item0.Rect.Y + Item0.Rect.Height * .5));

				ClickButton("GetSelected");
				Assert.AreEqual("Selected: Root.1, Root.2", ReadResult());

				// tap on partial selected node will set it to unselected state
				_app.TapCoordinates((float)(ItemRoot.Rect.X + ItemRoot.Rect.Width * .05), (float)(ItemRoot.Rect.Y + ItemRoot.Rect.Height * .5));

				ClickButton("GetSelected");
				Assert.AreEqual("Nothing selected", ReadResult());

				_app.TapCoordinates((float)(ItemRoot.Rect.X + ItemRoot.Rect.Width * .05), (float)(ItemRoot.Rect.Y + ItemRoot.Rect.Height * .5));

				ClickButton("GetSelected");
				Assert.AreEqual("Selected: Root, Root.0, Root.1, Root.2", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewMultiSelectItemTest_NodeMode()
		{
			TreeViewMultiSelectItemTest();
		}

		[Test]
		[AutoRetry]
		public void TreeViewMultiSelectItemTest_ContentMode()
		{
			TreeViewMultiSelectItemTest(isContentMode: true);
		}

		private void ValidateExpandingRaisedOnItemHavingUnrealizedChildren(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				var itemRoot = LabelFirstItem();
				itemRoot.FastTap();
				//Wait.ForIdle();

				ClickButton("DisableClickToExpand");
				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());

				ClickButton("SetRoot1HasUnrealizedChildren");

				ClickButton("LabelItems");
				var root1 = QueryAll("Root.1");
				Assert.IsNotNull(root1, "Verifying root.1 is found");
				TapAt(root1, 36, 12); // offset by approximate chevron position
									  //Wait.ForIdle();

				Assert.AreEqual("Expanding Raised", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void ValidateExpandingRaisedOnItemHavingUnrealizedChildren_NodeMode()
		{
			ValidateExpandingRaisedOnItemHavingUnrealizedChildren();
		}

		[Test]
		[AutoRetry]
		public void ValidateExpandingRaisedOnItemHavingUnrealizedChildren_ContentMode()
		{
			ValidateExpandingRaisedOnItemHavingUnrealizedChildren(isContentMode: true);
		}

		[Test]
		[Ignore("Currently fails on all platforms, issue #4084")]
		[AutoRetry]
		public void TreeViewItemTemplateSelectorTest()
		{
			// TODO: Go to ItemTemplateSelectorTestPage
			{
				ClickButton("ItemTemplateSelectorTestPage");

				Console.WriteLine("ItemTemplateSelector test page is ready");
				var node1 = QueryAll("Template1").FirstResult();
				Assert.IsNotNull(node1, "Verifying template 1 is set");
				var node2 = QueryAll("Template2").FirstResult();
				Assert.IsNotNull(node2, "Verifying template 2 is set");

				// Verify item container styles are set correctly by checking heights
				Assert.AreEqual(node1.Rect.Height, 50);
				Assert.AreEqual(node2.Rect.Height, 60);
			}
		}

		private void TreeViewPartialSelectionTest(bool isContentMode = false)
		{
			{
				SetContentMode(isContentMode);

				var ItemRoot = LabelFirstItem();
				ItemRoot.FastTap();

				ClickButton("AddSecondLevelOfNodes");
				// expand all nodes
				ClickButton("LabelItems");
				var root0 = QueryAll("Root.0");
				Assert.IsNotNull(root0, "Verifying Root.0 is found");
				var root1 = QueryAll("Root.1");
				Assert.IsNotNull(root1, "Verifying Root.1 is found");
				root0.FastTap();

				// Uno specifc: Ensure the items are labeled properly after expanding
				ClickButton("LabelItems");

				root1.FastTap();
				ClickButton("GetItemCount");
				Assert.AreEqual("8", ReadResult());
				ClickButton("LabelItems");

				ClickButton("DisableClickToExpand");
				ClickButton("ToggleSelectionMode");

				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("u|u|u|u|u|u|u|u|", ReadResult());

				var root00 = QueryAll("Root.0.0");
				Assert.IsNotNull(root00, "Verifying Root.0.0 is found");
				ToggleTreeViewItemCheckBox(root00, 3);
				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("p|s|s|u|u|u|u|u|", ReadResult());

				var root10 = QueryAll("Root.1.0");
				Assert.IsNotNull(root10, "Verifying Root.1.0 is found");
				ToggleTreeViewItemCheckBox(root10, 3);
				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("p|s|s|p|s|u|u|u|", ReadResult());

				ToggleTreeViewItemCheckBox(root1, 2);
				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("p|s|s|u|u|u|u|u|", ReadResult());

				ToggleTreeViewItemCheckBox(root1, 2);
				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("p|s|s|s|s|s|s|u|", ReadResult());

				var root2 = QueryAll("Root.2");
				Assert.IsNotNull(root2, "Verifying Root.2 is found");
				ToggleTreeViewItemCheckBox(root2, 2);
				ClickButton("GetMultiSelectCheckBoxStates");
				Assert.AreEqual("s|s|s|s|s|s|s|s|", ReadResult());
			}
		}

		[Test]
		[Ignore("Currently failing on WASM, issue #4083")]
		[AutoRetry]
		public void TreeViewPartialSelectionTest_NodeMode()
		{
			TreeViewPartialSelectionTest();
		}

		[Test]
		[Ignore("Currently failing on WASM, issue #4083")]
		[AutoRetry]
		public void TreeViewPartialSelectionTest_ContentMode()
		{
			TreeViewPartialSelectionTest(isContentMode: true);
		}

		[Test]
		[AutoRetry]
		public void TreeViewSelectedNodeTest()
		{
			{
				var root = LabelFirstItem(); // TODO: TreeItem
				root.FastTap();
				//Wait.ForIdle();

				ClickButton("LabelItems");
				var root0 = QueryAll("Root.0");
				Assert.IsNotNull(root0, "Verifying Root.0 is found");

				root0.FastTap();

				ClickButton("GetSelected");
				Assert.AreEqual("Selected: Root.0", ReadResult());

				ClickButton("ToggleRoot0Selection");

				ClickButton("GetSelected");
				Assert.AreEqual("Nothing selected", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewSelectedItemTest()
		{
			// databinding is only available on RS5+
			{
				SetContentMode(true);

				var root = LabelFirstItem(); // TODO: TreeItem
				root.FastTap();
				//Wait.ForIdle();

				ClickButton("LabelItems");
				var root0 = QueryAll("Root.0");
				Assert.IsNotNull(root0, "Verifying Root.0 is found");

				root0.FastTap();

				ClickButton("GetSelected");
				Assert.AreEqual("Selected: Root.0", ReadResult());

				ClickButton("ToggleRoot0Selection");

				ClickButton("GetSelected");
				Assert.AreEqual("Nothing selected", ReadResult());
			}
		}

		// test for #1756 https://github.com/microsoft/microsoft-ui-xaml/issues/1756
		[Test]
		[Ignore("Fails on Android, issue #4099")]
		[AutoRetry]
		public void TreeViewSelectRegressionTest()
		{
			// Running 5 times since the bug doesn't repro consistently.
			for (int i = 0; i < 5; i++)
			{
				{
					ClickButton("AddExtraNodes");
					ClickButton("LabelItems");

					ClickButton("SelectLastRootNode");
					ClickButton("GetSelected");
					Assert.AreEqual("Selected: Node 50", ReadResult());

					var node1 = QueryAll("Node 1");
					Assert.IsNotNull(node1, "Verifying Node 1 is found");
					node1.FastTap();

					ClickButton("GetSelected");
					Assert.AreEqual("Selected: Node 1", ReadResult());
				}
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewNodeInheritenceTest()
		{
			{
				var itemRoot = LabelFirstItem(); // TODO: TreeItem
				itemRoot.FastTap();
				//Wait.ForIdle();

				ClickButton("AddInheritedTreeViewNode");
				ClickButton("LabelItems");
				var node = QueryAll("Inherited from TreeViewNode");
				Assert.IsNotNull(node, "Verify TreeViewNode content is correct");
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewDataLateInitTest()
		{
			{
				ClickButton("TreeViewLateDataInitTestPage");

				ClickButton("InitializeItemsSource");
				//Wait.ForIdle();
				var node1 = QueryAll("Root");
				Assert.IsNotNull(node1, "Verify data binding");
			}
		}

		[Test]
		[AutoRetry]
		public void TreeViewNodeInMarkupTest()
		{
			{
				var root = QueryAll("Root");
				Assert.IsNotNull(root, "Verify root node content");
			}
		}

		[Test]
		// Regression test for https://github.com/microsoft/microsoft-ui-xaml/issues/1790
		[AutoRetry]
		public void ItemsSourceResyncTest()
		{
			// TreeView databinding only works on RS5+
			{
				SetContentMode(true);

				ClickButton("GetItemCount");
				Assert.AreEqual("1", ReadResult());

				ClickButton("ResetItemsSource");
				//Wait.ForIdle();
				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());

				ClickButton("ResetItemsSourceAsync");
				//Wait.ForIdle();
				ClickButton("GetItemCount");
				Assert.AreEqual("6", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void ItemsSourceSwitchForthAndBackTest()
		{
			// TreeView databinding only works on RS5+
			{
				SetContentMode(true);

				ClickButton("SwapItemsSource");
				//Wait.ForIdle();
				ClickButton("GetItemCount");
				Assert.AreEqual("2", ReadResult());

				ClickButton("SwapItemsSource");
				//Wait.ForIdle();
				ClickButton("ExpandRootNode");
				//Wait.ForIdle();
				ClickButton("GetItemCount");
				Assert.AreEqual("4", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void SelectedItemBindingsWork()
		{
			{
				var setContentButton = QueryAll("TwoWayBoundButton");
				var setSelectedItemButton = QueryAll("SelectRoot2Item");
				var readResultButton = QueryAll("ReadBindingResult");

				setContentButton.FastTap();
				//Wait.ForIdle();

				readResultButton.FastTap();
				//Wait.ForIdle();
				Assert.AreEqual("Root.1;Root.1", ReadResult());

				setSelectedItemButton.FastTap();
				//Wait.ForIdle();
				readResultButton.FastTap();
				//Wait.ForIdle();
				Assert.AreEqual("Root.2;Root.2", ReadResult());
			}
		}

		[Test]
		[AutoRetry]
		public void SingleSelectWithUnrealizedChildrenDoesNotMoveSelection()
		{
			{
				ClickButton("TreeViewUnrealizedChildrenTestPage");

				TapOnTreeViewAt(50, 12, "GetSelectedItemName");

				Console.WriteLine("Selecting item");
				ClickButton("GetSelectedItemName");
				//Wait.ForIdle();

				Console.WriteLine("Verifying current selection");
				var textBlock = QueryAll("SelectedItemName");
				Assert.AreEqual("Item: 0; layer: 3", textBlock.GetText());

				Console.WriteLine("Expanding selected item");
				TapOnTreeViewAt(12, 12, "GetSelectedItemName");
				//Wait.ForIdle();

				Console.WriteLine("Verifying selection again");
				ClickButton("GetSelectedItemName");
				//Wait.ForIdle();
				textBlock = QueryAll("SelectedItemName");
				Assert.AreEqual("Item: 0; layer: 3", textBlock.GetText());
			}
		}

		private void SetContentMode(bool isContentMode)
		{
			if (isContentMode)
			{
				ClickButton("SetContentMode");
			}
		}

		private void ClickButton(string buttonName)
		{
			var button = QueryAll(buttonName);
			button.FastTap();
			// Wait.ForIdle();
		}

		private string ReadResult()
		{
			var textBlock = QueryAll("Results");
			return textBlock.GetText();
		}

		private void TapOnTreeViewAt(double x, double y, string buttonName)
		{
			// Note: Unable to get the treeview UIObject. Using the button above and accounting
			// for its height as a workaround.
			var buttonAboveTreeView = QueryAll(buttonName).FirstResult();
			Assert.IsNotNull(buttonAboveTreeView, "Verifying that we found a UIElement called " + buttonName);

			_app.TapCoordinates((float)(buttonAboveTreeView.Rect.X + x), (float)(buttonAboveTreeView.Rect.Bottom + y));
			//Wait.ForIdle();
		}

		private void TapOutsideFlyout()
		{
			var textBlock = QueryAll("Results");
			textBlock.FastTap();
		}

		private void TapOnFlyoutTreeViewRootItemChevron()
		{
			// Chevron has 12px left padding, and it's 12px wide. 
			// 18 is the center point of chevron.
			TapOnTreeViewAt(18, 20, "GetFlyoutItemCount");
		}

		private QueryEx LabelFirstItem()
		{
			ClickButton("LabelItems");
			Console.WriteLine("Retrieve first item as generic UIElement");
			var ItemRoot = QueryAll("Root");
			Assert.IsNotNull(ItemRoot, "Verifying that we found a UIElement called Root");
			return ItemRoot;
		}

		private void ToggleTreeViewItemCheckBox(QueryEx item, int level)
		{
			var firstItem = item.FirstResult();
			_app.TapCoordinates((float)(firstItem.Rect.X + firstItem.Rect.Width * 0.05 * level), (float)(firstItem.Rect.Y + firstItem.Rect.Height * .5));
		}

		// Uno specific

		private void TapAt(QueryEx query, double x, double y)
		{
			var firstResult = query.FirstResult();

			_app.TapCoordinates((float)(firstResult.Rect.X + x), (float)(firstResult.Rect.Y + y));
		}

		private QueryEx QueryAll(string name)
		{
			IAppQuery AllQuery(IAppQuery query)
				// TODO: .All() is not yet supported for wasm.
				=> AppInitializer.GetLocalPlatform() == Platform.Browser ? query : query.All();

			Query allQuery = q => AllQuery(q).Marked(name);
			return new QueryEx(allQuery);
		}
	}
}
