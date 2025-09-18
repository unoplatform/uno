using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.ListViewPages;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif

using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewNode = Microsoft.UI.Xaml.Controls.TreeViewNode;
using TreeViewItem = Microsoft.UI.Xaml.Controls.TreeViewItem;
using TreeViewList = Microsoft.UI.Xaml.Controls.TreeViewList;
using Combinatorial.MSTest;

#if HAS_UNO
using Windows.Foundation.Metadata;
using TreeNodeSelectionState = Microsoft.UI.Xaml.Controls.TreeNodeSelectionState;
#endif

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_TreeView // resources
	{
		private ResourceDictionary _testsResources;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		/// <summary>IsSelected and IsExpanded are two-ways bound.</summary>
		private DataTemplate TreeViewItemTemplate => _testsResources["TreeViewItemTemplate"] as DataTemplate;

		/// <summary>IsExpanded is two-ways bound</summary>
		private DataTemplate TreeViewItemTemplate_WithoutIsSelectedBinding => _testsResources["TreeViewItemTemplate_WithoutIsSelectedBinding"] as DataTemplate;
	}
	public partial class Given_TreeView
	{
#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_TreeViewItem_Dragged_Near_the_Edge()
		{
			TreeViewNode node;
			var treeView = new TreeView
			{
				RootNodes =
				{
					(node = new TreeViewNode
					{
						Content = "drag me"
					})
				}
			};

			await UITestHelper.Load(treeView);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var tvi = (TreeViewItem)treeView.ContainerFromNode(node);
			var dragStartingCount = 0;
			tvi.AddHandler(UIElement.DragStartingEvent, new RoutedEventHandler((_, _) => dragStartingCount++), true);

			// The important part here is to start the press and move out without moving more than TapMaxXDelta or TapMaxYDelta,
			// so that dragging starts after the pointer is outside the item
			mouse.Press(tvi.GetAbsoluteBoundsRect().GetMidX(), tvi.GetAbsoluteBoundsRect().Bottom - 3);
			await WindowHelper.WaitForIdle();

			mouse.MoveBy(0, 1);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, dragStartingCount);

			mouse.MoveBy(0, 15); // move out of the tvi
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, dragStartingCount);
		}
#endif

#if HAS_UNO
		[TestMethod]
		[CombinatorialData]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("The behaviour of virtualizing panels is only accurate for managed virtualizing panels.")]
#endif
		public async Task When_Scrolled_IsExpanded_Should_Be_Preserved(bool bindIsExpanded)
		{
			var itemsSource = Enumerable.Range(0, 8).Select(i =>
				new TestTreeNodeModel($"Root{i}", false, false)
					{
						new TestTreeNodeModel($"Root{i} Child 1", false, false)
						{
							new TestTreeNodeModel($"Root{i} Grandchild 1", false, false),
							new TestTreeNodeModel($"Root{i} Grandchild 2", false, false)
						},
						new TestTreeNodeModel($"Root{i} Child 2", false, false)
					}
				)
				.ToList();

			var treeView = new TreeView
			{
				Height = 100,
				ItemsSource = itemsSource,
				ItemTemplate = new DataTemplate(() =>
				{
					var tvi = new TreeViewItem();
					tvi.SetBinding(TreeViewItem.ItemsSourceProperty, new Binding("Items"));
					tvi.SetBinding(ContentControl.ContentProperty, new Binding("Label"));
					if (bindIsExpanded)
					{
						tvi.SetBinding(TreeViewItem.IsExpandedProperty, new Binding("IsExpanded"));
					}

					return tvi;
				})
			};

			await UITestHelper.Load(treeView);

			if (bindIsExpanded)
			{
				itemsSource[0].IsExpanded = true;
			}
			else
			{
				((TreeViewItem)treeView.ContainerFromItem(itemsSource[0])).IsExpanded = true;
			}

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, itemsSource
				.Select(item => treeView.ContainerFromItem(item))
				.OfType<TreeViewItem>()
				.Count(c => c.IsExpanded));

			var sv = treeView.FindFirstDescendant<ScrollViewer>();
			sv.ScrollToVerticalOffset(9999);
			await WindowHelper.WaitForIdle();

#if !__SKIA__ // other platforms need some additional delay for some reason
			await Task.Delay(1000);
#endif

			Assert.AreEqual(0, itemsSource
				.Select(item => treeView.ContainerFromItem(item))
				.OfType<TreeViewItem>()
				.Count(c => c.IsExpanded));

			sv.ScrollToVerticalOffset(0);
			await WindowHelper.WaitForIdle();

#if !__SKIA__ // other platforms need some additional delay for some reason
			await Task.Delay(1000);
#endif

			Assert.AreEqual(1, itemsSource
				.Select(item => treeView.ContainerFromItem(item))
				.OfType<TreeViewItem>()
				.Count(c => c.IsExpanded));
			Assert.IsTrue(((TreeViewItem)treeView.ContainerFromItem(itemsSource[0])).IsExpanded);
		}
#endif

		[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("The behaviour of virtualizing panels is only accurate for managed virtualizing panels.")]
#endif
		public async Task When_TreeViewItem_Collapsed_Children_Removed_From_Tree()
		{
			var treeView = new TreeView
			{
				RootNodes =
				{
					new TreeViewNode
					{
						Content = "Parent",
						IsExpanded = true,
						Children =
						{
							new TreeViewNode
							{
								Content = "Child",
								IsExpanded = true
							}
						}
					}
				}
			};

			await UITestHelper.Load(treeView);

			Assert.AreEqual(2, treeView.FindVisualChildByType<ItemsStackPanel>().Children.Count);

			treeView.RootNodes[0].IsExpanded = false;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, treeView.FindVisualChildByType<ItemsStackPanel>().Children.Count);

			treeView.RootNodes[0].IsExpanded = true;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, treeView.FindVisualChildByType<ItemsStackPanel>().Children.Count);
		}

#if HAS_UNO
		// https://github.com/unoplatform/uno/issues/16041
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif HAS_UNO && !HAS_UNO_WINUI
		[Ignore("Fails on UWP branch as mixing WUX and MUX types causes errors.")]
#endif
		public async Task When_TreeViewItem_Dragged_NRE()
		{
			using var _ = new DisposableAction(() => TestableTreeViewItem.DraggingThrewException = false);
			var treeView = new TreeView
			{
				RootNodes =
				{
					new TreeViewNode
					{
						Content = "Parent",
						IsExpanded = true,
						Children =
						{
							new TreeViewNode
							{
								Content = "Child",
								IsExpanded = true
							}
						}
					}
				},
				Style = new Style
				{
					BasedOn = Style.GetDefaultStyleForType(typeof(TreeView)),
					Setters =
					{
						new Setter(Control.TemplateProperty, new ControlTemplate(() =>
						{
							var tvl = new CustomTreeViewList
							{
								Name = "ListControl"
							};
							tvl.SetBinding(FrameworkElement.BackgroundProperty, new TemplateBinding(new PropertyPath("Background")));
							tvl.SetBinding(ItemsControl.ItemTemplateProperty, new TemplateBinding(new PropertyPath("ItemTemplate")));
							tvl.SetBinding(ItemsControl.ItemTemplateSelectorProperty, new TemplateBinding(new PropertyPath("ItemTemplateSelector")));
							tvl.SetBinding(ItemsControl.ItemTemplateSelectorProperty, new TemplateBinding(new PropertyPath("ItemTemplateSelector")));
							tvl.SetBinding(ItemsControl.ItemContainerStyleProperty, new TemplateBinding(new PropertyPath("ItemContainerStyle")));
							tvl.SetBinding(ItemsControl.ItemContainerStyleSelectorProperty, new TemplateBinding(new PropertyPath("ItemContainerStyleSelector")));
							tvl.SetBinding(ItemsControl.ItemContainerTransitionsProperty, new TemplateBinding(new PropertyPath("ItemContainerTransitions")));
							tvl.SetBinding(ListViewBase.CanDragItemsProperty, new TemplateBinding(new PropertyPath("CanDragItems")));
							tvl.SetBinding(UIElement.AllowDropProperty, new TemplateBinding(new PropertyPath("AllowDrop")));
							tvl.SetBinding(ListViewBase.CanReorderItemsProperty, new TemplateBinding(new PropertyPath("CanReorderItems")));
							return tvl;
						}))
					}
				}
			};

			await UITestHelper.Load(treeView);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// The bug is extremely hard to reproduce. Even slight changes to the numbers will fail to repro.
			// The main idea is to have the first PointerEnteredEvent in the "Parent" TreeViewItem
			// and the very next "frame" (i.e. pointer event) should jump to the the "Child" TreeViewItem.
			// At least, that's the leading hypothesis.
			// Note that removing the WaitForIdle's will throw an NRE and this is somewhat expected. The TreeView
			// removes the children "nodes" immediately when a parent node starts dragging, but the children
			// are asynchronously removed, so if an "about to be removed" child node receives a DragOver/DragEnter,
			// the event callback will run some logic that assumes that the node is still in the TreeView, when it
			// technically isn't (it's in the tree, but the TreeView has internally discarded it), which throws the NRE.
			mouse.Press(treeView.TransformToVisual(null).TransformPoint(new Point(54, 17)));
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(treeView.TransformToVisual(null).TransformPoint(new Point(54, 29)), 1);
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(treeView.TransformToVisual(null).TransformPoint(new Point(86, 42)), 1);
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(treeView.TransformToVisual(null).TransformPoint(new Point(118, 55)), 1);
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(treeView.TransformToVisual(null).TransformPoint(new Point(121, 55)), 1);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(TestableTreeViewItem.DraggingThrewException);
		}

		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif HAS_UNO && !HAS_UNO_WINUI
		[Ignore("Fails on UWP branch as mixing WUX and MUX types causes errors.")]
#endif
		public async Task When_TreeViewItem_Dragged_NRE2()
		{
			using var _ = new DisposableAction(() => TestableTreeViewItem.DraggingThrewException = false);
			var treeView = new TreeView
			{
				ItemTemplate = new DataTemplate(() =>
				{
					var tvi = new TestableTreeViewItem();
					tvi.SetBinding(TreeViewItem.ItemsSourceProperty, new Binding("Items"));
					tvi.SetBinding(ContentControl.ContentProperty, new Binding("Label"));
					return tvi;
				}),
				ItemsSource = new ObservableCollection<TestTreeNodeModel>
				{
					new TestTreeNodeModel("Root 1")
					{
						Items =
						{
							new TestTreeNodeModel("Child 1.1"),
							new TestTreeNodeModel("Child 1.2"),
						}
					},
					new TestTreeNodeModel("Root 2")
					{
						Items =
						{
							new TestTreeNodeModel("Child 2.1"),
						}
					},
				}
			};

			await UITestHelper.Load(treeView);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			treeView.RootNodes[0].IsExpanded = true;
			await WindowHelper.WaitForIdle();

			var ttv = treeView.TransformToVisual(null);

			var test = new[]
			{
				("move", new Point(103, 29)),
				("press", new Point()),
				("move", new Point(103, 36)),
				("move", new Point(103, 40)),
				("move", new Point(103, 47)),
				("release", new Point()),
			};


			foreach (var (action, position) in test)
			{
				if (action == "press")
				{
					mouse.Press();
				}
				else if (action == "release")
				{
					mouse.Release();
				}
				else
				{
					Assert.AreEqual("move", action);
					mouse.MoveTo(ttv.TransformPoint(position), 1);
				}
				await WindowHelper.WaitForIdle();
			}

			Assert.IsFalse(TestableTreeViewItem.DraggingThrewException);
		}
#endif

		[TestMethod]
		public async Task When_Setting_SelectedItem_DoesNotTakeEffect()
		{
			var treeView = new TreeView();
			treeView.ItemTemplate = TreeViewItemTemplate;
			var testViewModelItems = new[]
			{
				new TestTreeNodeModel("First item"),
				new TestTreeNodeModel("Second item"),
			};
			treeView.ItemsSource = testViewModelItems;

			await UITestHelper.Load(treeView);

			treeView.SelectedItem = treeView.RootNodes[1];

			var listControl = treeView.FindFirstDescendant<Microsoft.UI.Xaml.Controls.TreeViewList>("ListControl");
			// Yes, that's how it behaves on WinUI :/
			Assert.AreEqual(-1, listControl.SelectedIndex);
		}

#if __APPLE_UIKIT__
		[Ignore("Fails on iOS 17 https://github.com/unoplatform/uno/issues/17102")]
#endif
		[TestMethod]
		public async Task When_Setting_SelectedItem_TakesEffect()
		{
			TreeView treeView = new TreeView
			{
				RootNodes =
			{
				new TreeViewNode{ Content = "1" },
				new TreeViewNode{ Content = "2" },
				new TreeViewNode{ Content = "3" },
			}
			};
			await UITestHelper.Load(treeView);

			treeView.SelectedItem = treeView.RootNodes[1];

#if !HAS_UNO
			var listControl = (TreeViewList)typeof(Control).GetMethod("GetTemplateChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(treeView, new[] { "ListControl" });
#else
			var listControl = treeView.ListControl;
#endif
			Assert.AreEqual(1, listControl.SelectedIndex);
		}

#if HAS_UNO
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_ItemTemplateSelector_DataTemplate_Root_IsNot_TreeViewItem()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
			}

			Border border = null;

			var sp = new StackPanel
			{
				Background = new SolidColorBrush(Colors.Blue),
				Children =
				{
					new TreeView
					{
						ItemsSource = "12",
						ItemTemplateSelector = new TreeItemTemplateSelector
						{
							Template = new DataTemplate(() => border = new Border
							{
								Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
								Width = 100,
								Height = 100,
								Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							})
						}
					}
				}
			};

			await UITestHelper.Load(sp);

			var tv = sp.FindFirstDescendant<TreeView>();

			Assert.IsNotNull(border);
			var containerX = border.GetAbsoluteBoundsRect().X;
			var screenshot = await UITestHelper.ScreenShot(tv);

			var tvi = sp.FindFirstDescendant<TreeViewItem>();
			var x = containerX + 50d;
			ImageAssert.HasColorAt(screenshot, new Point(x, screenshot.Height / 4), Microsoft.UI.Colors.Red);
			ImageAssert.HasColorAt(screenshot, new Point(x, screenshot.Height * 3 / 4), Microsoft.UI.Colors.Red);

			((TreeViewItem)sp.FindFirstDescendant<TreeViewList>().ContainerFromIndex(0)).ActualHeight.Should().BeApproximately(112, 1);
			((TreeViewItem)sp.FindFirstDescendant<TreeViewList>().ContainerFromIndex(1)).ActualHeight.Should().BeApproximately(112, 1);
			Assert.AreEqual("1", ((ContentPresenter)((TreeViewItem)sp.FindFirstDescendant<TreeViewList>().ContainerFromIndex(0)).FindName("ContentPresenter")).FindFirstDescendant<TextBlock>().Text);
			Assert.AreEqual("2", ((ContentPresenter)((TreeViewItem)sp.FindFirstDescendant<TreeViewList>().ContainerFromIndex(1)).FindName("ContentPresenter")).FindFirstDescendant<TextBlock>().Text);
		}
#endif

		[TestMethod]
		public async Task When_SubList_Of_Last_Item_Cleared()
		{
			var initial_Depth_0 = 0;
			var initial_Depth_1 = 64;
			var initial_Depth_2 = 96;

			var treeView = new TreeView();
			treeView.ItemTemplate = TreeViewItemTemplate;
			var testViewModelItems = Enumerable.Range(0, 2).Select(_ => Get_Depth_0_Item()).ToArray();
			treeView.ItemsSource = testViewModelItems;
			WindowHelper.WindowContent = treeView;

			await WindowHelper.WaitForLoaded(treeView);

			await WindowHelper.WaitForNonNull(() => treeView.ContainerFromItem(testViewModelItems.Last().Items.First().Items.Last()));

			var currentItem = testViewModelItems.First().Items.Last().Items.First();

			Assert.IsNotNull(treeView.ContainerFromItem(currentItem));

			testViewModelItems.First().Items.Last().Items.Clear();

			await WindowHelper.WaitFor(() => treeView.ContainerFromItem(currentItem) == null);

			TestTreeNodeModel Get_Depth_0_Item() => new TestTreeNodeModel($"Item {++initial_Depth_0}")
			{
				Get_Depth_1_Item(),
				Get_Depth_1_Item(),
				Get_Depth_1_Item(),
			};
			TestTreeNodeModel Get_Depth_1_Item() => new TestTreeNodeModel($"Subitem {(char)(++initial_Depth_1)}")
			{
				Get_Depth_2_Item(),
				Get_Depth_2_Item(),
			};
			TestTreeNodeModel Get_Depth_2_Item() => new TestTreeNodeModel($"Subitem {(char)(++initial_Depth_2)}");
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		[CombinatorialData]
		public async Task When_SelectedItem_Expanded(
			[CombinatorialValues("1", "111")] string labelToSelect, bool useBinding)
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetFlattenNodes().FirstOrDefault(x => x.Label == labelToSelect) ?? throw new InvalidOperationException($"failed to find node: {labelToSelect}");
			tvm.SelectedItem = targetItem;

			var SUT = new TreeView()
			{
				ItemTemplate = TreeViewItemTemplate,
				DataContext = tvm,
			};
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			if (useBinding)
			{
				SUT.SetBinding(TreeView.ItemsSourceProperty, new Binding() { Path = new(nameof(tvm.ItemsSource)), Mode = BindingMode.TwoWay });
				SUT.SetBinding(TreeView.SelectedItemProperty, new Binding() { Path = new(nameof(tvm.SelectedItem)), Mode = BindingMode.TwoWay });
			}
			else
			{
				SUT.ItemsSource = tvm.ItemsSource;
				SUT.SelectedItem = tvm.SelectedItem;
			}

			await UITestHelper.Load(setup);

			Assert.AreEqual(targetItem, tvm.SelectedItem, "invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "invalid SUT.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "invalid SUT.SelectedNode.Content");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "selected node is not selected");
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		[DataRow("1")]
		[DataRow("111")]
		public async Task When_IsSelectedItem_Expanded(string labelToSelect)
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetFlattenNodes().FirstOrDefault(x => x.Label == labelToSelect) ?? throw new InvalidOperationException($"failed to find node: {labelToSelect}");
			targetItem.IsSelected = true;

			var SUT = new TreeView()
			{
				ItemTemplate = TreeViewItemTemplate,
				DataContext = tvm,
				ItemsSource = tvm.ItemsSource,
			};
			SUT.SetBinding(TreeView.SelectedItemProperty, new Binding() { Path = new(nameof(tvm.SelectedItem)), Mode = BindingMode.TwoWay });
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			await UITestHelper.Load(setup);

			Assert.AreEqual(targetItem, tvm.SelectedItem, "invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "invalid SUT.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "invalid SUT.SelectedNode.Content");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "selected node is not selected");
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		public async Task When_SelectedItem_NotExpandedToExpanded()
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetNode("111");
			tvm.SelectedItem = targetItem;
			tvm.ApplyToAllNodes(x => x.IsExpanded = false);

			var SUT = new TreeView()
			{
				ItemTemplate = TreeViewItemTemplate,
				DataContext = tvm,
				ItemsSource = tvm.ItemsSource,
				SelectedItem = tvm.SelectedItem,
			};
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			await UITestHelper.Load(setup);

			// assert: before expanding
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[collapsed]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[collapsed]invalid SUT.SelectedItem");
			Assert.IsNull(SUT.SelectedNode, "[collapsed]invalid SUT.SelectedNode should be null");
			Assert.IsNull(SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem)), "[collapsed]selected node should not exist yet");

			// expand the ancestry from top-down
			for (int i = 1; i < targetItem.Label.Length; i++)
			{
				tvm.GetNode(targetItem.Label.Substring(0, i)).IsExpanded = true;
				await UITestHelper.WaitForIdle();
			}

			// assert: after expanding
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[expanded]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[expanded]invalid SUT.SelectedItem");
			// TreeView.SelectedNode: on windows, this value remains null before AND after expanded/materialization.
			// It seems to be only set from UI? Setting SelectedItem to materialized item doesn't seem to update SelectedNode..?
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "[expanded]invalid SUT.SelectedNode.Content");
			//Assert.AreEqual(null, SUT.SelectedNode, "[expanded]invalid SUT.SelectedNode should still be null");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[expanded]selected node is not selected");
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		public async Task When_IsSelectedItem_NotExpandedToExpanded()
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetNode("111");
			targetItem.IsSelected = true;
			tvm.ApplyToAllNodes(x => x.IsExpanded = false);

			var SUT = new TreeView()
			{
				ItemTemplate = TreeViewItemTemplate,
				DataContext = tvm,
				ItemsSource = tvm.ItemsSource,
			};
			SUT.SetBinding(TreeView.SelectedItemProperty, new Binding() { Path = new(nameof(tvm.SelectedItem)), Mode = BindingMode.TwoWay });
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			await UITestHelper.Load(setup);

			// assert: before expanding
			Assert.IsNull(tvm.SelectedItem, "[collapsed]invalid tvm.SelectedItem");
			Assert.IsNull(SUT.SelectedItem, "[collapsed]invalid SUT.SelectedItem");
			Assert.IsNull(SUT.SelectedNode, "[collapsed]invalid SUT.SelectedNode should be null");
			Assert.IsNull(SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem)), "[collapsed]selected node should not exist yet");

			// expand the ancestry from top-down
			for (int i = 1; i < targetItem.Label.Length; i++)
			{
				tvm.GetNode(targetItem.Label.Substring(0, i)).IsExpanded = true;
				await UITestHelper.WaitForIdle();
			}

			// assert: after expanding
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[expanded]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[expanded]invalid SUT.SelectedItem");
			// TreeView.SelectedNode: on windows, this value remains null before AND after expanded/materialization.
			// It seems to be only set from UI? Setting SelectedItem to materialized item doesn't seem to update SelectedNode..?
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "[expanded]invalid SUT.SelectedNode.Content");
			//Assert.AreEqual(null, SUT.SelectedNode, "[expanded]invalid SUT.SelectedNode should still be null");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[expanded]selected node is not selected");
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		public async Task When_SelectedItem_ParentCollapsed()
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetNode("111");
			tvm.SelectedItem = targetItem;

			var SUT = new TreeView()
			{
				// using the template without the IsSelected binding
				// to prevent re-selection (in step-2) from saved IsSelected state (from step-0)
				ItemTemplate = TreeViewItemTemplate_WithoutIsSelectedBinding,
				DataContext = tvm,
				ItemsSource = tvm.ItemsSource,
			};
			SUT.SetBinding(TreeView.SelectedItemProperty, new Binding() { Path = new(nameof(tvm.SelectedItem)), Mode = BindingMode.TwoWay });
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			await UITestHelper.Load(setup);

			// step 0: starting with parent expanded
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[step0]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[step0]invalid SUT.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "[step0]invalid SUT.SelectedNode.Content");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[step0]target node is not selected");
#endif

			// step 1: collapsing parent should cause deselection
			tvm.GetNode("11").IsExpanded = false;
			await UITestHelper.WaitForIdle();
			SUT.ToString();
			Assert.IsNull(tvm.SelectedItem, "[step1]tvm.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedItem, "[step1]SUT.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedNode, "[step1]SUT.SelectedNode should be null");
			Assert.IsNull(SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem)), "target node should no longer be present.");

			// step 2: re-expanding parent should still keep selection empty
			// as it has been cleared, and "normally" nothing here should re-select it
			tvm.GetNode("11").IsExpanded = true;
			await UITestHelper.WaitForIdle();
			SUT.ToString();
			Assert.IsNull(tvm.SelectedItem, "[step2]tvm.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedItem, "[step2]SUT.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedNode, "[step2]SUT.SelectedNode should be null");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.UnSelected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[step2]target node should not be selected");
#endif
		}

		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("failing due to #16216; selection offset correction happens too late on ios")]
#endif
		public async Task When_IsSelectedItem_ParentCollapsed()
		{
			var tvm = new TestTreeViewModel
			{
				new("1")
				{
					new("11")
					{
						new("111"),
						new("112"),
					},
					new("12"),
				},
				new("2"),
			};
			var targetItem = tvm.GetNode("111");
			targetItem.IsSelected = true;

			var SUT = new TreeView()
			{
				ItemTemplate = TreeViewItemTemplate, // with IsSelected two-way binding
				DataContext = tvm,
				ItemsSource = tvm.ItemsSource,
			};
			SUT.SetBinding(TreeView.SelectedItemProperty, new Binding() { Path = new(nameof(tvm.SelectedItem)), Mode = BindingMode.TwoWay });
			var setup = new StackPanel()
			{
				Children =
				{
					new TextBlock()
						.Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding { Source = SUT, Path = new(nameof(SUT.SelectedItem)) })),
					SUT
				},
			};

			await UITestHelper.Load(setup);

			// step 0: starting with parent expanded
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[step0]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[step0]invalid SUT.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "[step0]invalid SUT.SelectedNode.Content");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[step0]target node is not selected");
#endif

			// step 1: collapsing parent should cause deselection
			tvm.GetNode("11").IsExpanded = false;
			await UITestHelper.WaitForIdle();
			SUT.ToString();
			Assert.IsNull(tvm.SelectedItem, "[step1]tvm.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedItem, "[step1]SUT.SelectedItem should be null");
			Assert.IsNull(SUT.SelectedNode, "[step1]SUT.SelectedNode should be null");
			Assert.IsNull(SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem)), "selected node should no longer be present.");

			// step 2: normally, re-expanding parent shouldn't change selection from its previous null state
			// however, the IsSelected=true on the model, should cause the tree-view to reselect it.
			tvm.GetNode("11").IsExpanded = true;
			await UITestHelper.WaitForIdle();
			SUT.ToString();
			Assert.AreEqual(targetItem, tvm.SelectedItem, "[step2]invalid tvm.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedItem, "[step2]invalid SUT.SelectedItem");
			Assert.AreEqual(targetItem, SUT.SelectedNode?.Content, "[step2]invalid SUT.SelectedNode.Content");
#if HAS_UNO
			Assert.AreEqual(TreeNodeSelectionState.Selected, SUT.NodeFromContainer(SUT.ContainerFromItem(targetItem))?.SelectionState, "[step2]target node is not selected");
#endif
		}

		[TestMethod]
		public async Task When_Simple_ItemsSource()
		{
			var SUT = new TreeView()
			{
				ItemsSource = new int[] { 1, 2 }
			};
			await UITestHelper.Load(SUT);
		}
	}
	public partial class Given_TreeView // helper methods, view-models
	{
		// below models are prefixed with Test- to not confuse with actually TreeView inner model..:
		public class TestTreeViewModel : ViewModelBase, IEnumerable<TestTreeNodeModel>
		{
			private TestTreeNodeModel selectedItem;

			public TestTreeNodeModel SelectedItem { get => selectedItem; set => SetAndRaiseIfChanged(ref selectedItem, value); }
			public ObservableCollection<TestTreeNodeModel> ItemsSource { get; init; } = new();

			#region Collection Initializer support
			public void Add(TestTreeNodeModel item) => ItemsSource.Add(item);
			public IEnumerator<TestTreeNodeModel> GetEnumerator() => ItemsSource.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => ItemsSource.GetEnumerator();
			#endregion

			public IEnumerable<TestTreeNodeModel> GetFlattenNodes()
			{
				// workaround: Flatten has ambiguous overloads for windows build...
				foreach (var item in ItemsSource)
				{
					yield return item;
					foreach (var nested in item.GetFlattenNodes())
					{
						yield return nested;
					}
				}
			}
			public TestTreeNodeModel/*?*/ FindNode(string label) => GetFlattenNodes().FirstOrDefault(x => x.Label == label);
			public TestTreeNodeModel GetNode(string label) => FindNode(label) ?? throw new ArgumentException($"failed to find node: {label}");
			public void ApplyToAllNodes(Action<TestTreeNodeModel> apply)
			{
				// workaround: ForEach has ambiguous overloads for windows build...
				foreach (var item in GetFlattenNodes())
				{
					apply(item);
				}
			}

			public string TreeGraph()
			{
				var buffer = new StringBuilder();
				foreach (var item in ItemsSource)
				{
					Walk(item);
				}
				return buffer.ToString();

				void Walk(TestTreeNodeModel item, int depth = 0)
				{
					buffer
						.Append(new string(' ', depth * 4))
						.Append(item.ToString())
						.AppendLine();
					foreach (var child in item.Items)
					{
						Walk(child, depth + 1);
					}
				}
			}
		}
		public class TestTreeNodeModel : ViewModelBase, IEnumerable<TestTreeNodeModel>
		{
			private string label;
			private bool isSelected;
			private bool isExpanded;

			public string Label { get => label; set => SetAndRaiseIfChanged(ref label, value); }
			public bool IsSelected { get => isSelected; set => SetAndRaiseIfChanged(ref isSelected, value); }
			public bool IsExpanded { get => isExpanded; set => SetAndRaiseIfChanged(ref isExpanded, value); }
			public ObservableCollection<TestTreeNodeModel> Items { get; } = new();

			public TestTreeNodeModel(string label, bool isSelected = false, bool isExpanded = true)
			{
				this.label = label;
				this.isSelected = isSelected;
				this.isExpanded = isExpanded;
			}

			public void RegisterPropertyChanged(string propertyName, Action<TestTreeNodeModel> callback)
			{
				PropertyChanged += (s, e) =>
				{
					if (e.PropertyName == propertyName)
					{
						callback(this);
					}
				};
			}

			public override string ToString() => string.Concat(
				IsExpanded ? "+" : "-",
				IsSelected ? "[S]" : "",
				" ",
				Label
			);

			#region Collection Initializer support
			public void Add(TestTreeNodeModel item) => Items.Add(item);
			public IEnumerator<TestTreeNodeModel> GetEnumerator() => Items.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();
			#endregion

			public IEnumerable<TestTreeNodeModel> GetFlattenNodes()
			{
				foreach (var item in Items)
				{
					yield return item;
					foreach (var nested in item.GetFlattenNodes())
					{
						yield return nested;
					}
				}
			}
		}

		public class TreeItemTemplateSelector : DataTemplateSelector
		{
			public DataTemplate Template { get; set; }
			protected override DataTemplate SelectTemplateCore(object item) => Template;
		}
	}

#if HAS_UNO
	partial class CustomTreeViewList : TreeViewList
	{
		protected override DependencyObject GetContainerForItemOverride()
		{
			var targetItem = new TestableTreeViewItem() { IsGeneratedContainer = true }; // Uno specific IsGeneratedContainer
			return targetItem;
		}
	}

	partial class TestableTreeViewItem : TreeViewItem
	{
		public static bool DraggingThrewException { get; set; }

		protected override void OnDragEnter(Microsoft.UI.Xaml.DragEventArgs args)
		{
			try
			{
				base.OnDragEnter(args);
			}
			catch (Exception)
			{
				DraggingThrewException = true;
			}
		}

		protected override void OnDragOver(Microsoft.UI.Xaml.DragEventArgs args)
		{
			try
			{
				base.OnDragOver(args);
			}
			catch (Exception)
			{
				DraggingThrewException = true;
			}
		}
	}
#endif
}
