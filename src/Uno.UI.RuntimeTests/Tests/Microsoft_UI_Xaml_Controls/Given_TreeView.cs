using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.ListViewPages;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using System.ComponentModel;
using Windows.UI.Input.Preview.Injection;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewNode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewNode;
using TreeViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItem;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_TreeView
	{
		private ResourceDictionary _testsResources;

		private DataTemplate TreeViewItemTemplate => _testsResources["TreeViewItemTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
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
			Assert.AreEqual(dragStartingCount, 0);

			mouse.MoveBy(0, 15); // move out of the tvi
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(dragStartingCount, 1);
		}
#endif

		[TestMethod]
#if __ANDROID__ || __IOS__
		[Ignore("Fails")]
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
		// This test actually always passes due to catching the NRE in SafeRaiseEvent in RaiseDragEnterOrOver
		// so you need to check manually if there are any logged exceptions
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_TreeViewItem_Dragged_NRE()
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
		}
#endif

		[TestMethod]
		public async Task When_Setting_SelectedItem_DoesNotTakeEffect()
		{
			var treeView = new TreeView();
			treeView.ItemTemplate = TreeViewItemTemplate;
			var testViewModelItems = new[]
			{
				new TestViewModelItem()
				{
					Label = "First item",
				},
				new TestViewModelItem()
				{
					Label = "Second item",
				},
			};
			treeView.ItemsSource = testViewModelItems;

			await UITestHelper.Load(treeView);

			treeView.SelectedItem = treeView.RootNodes[1];

#if !HAS_UNO
			var listControl = (TreeViewList)typeof(Control).GetMethod("GetTemplateChild", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(treeView, new[] { "ListControl" });
#else
			var listControl = treeView.ListControl;
#endif
			// Yes, that's how it behaves on WinUI :/
			Assert.AreEqual(-1, listControl.SelectedIndex);
		}

		[TestMethod]
		public async Task When_Setting_SelectedItem_TakesEffect()
		{
			TreeView treeView = new TreeView
			{
				RootNodes =
				{
					new TreeViewNode
					{
						Content = "1",
					},
					new TreeViewNode
					{
						Content = "2"
					},
					new TreeViewNode
					{
						Content = "3"
					}
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

		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Sublist_Of_Last_Item_Cleared()
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

			TestViewModelItem Get_Depth_0_Item() => new TestViewModelItem
			{
				Label = $"Item {++initial_Depth_0}",
				Items =
				{
					Get_Depth_1_Item(),
					Get_Depth_1_Item(),
					Get_Depth_1_Item(),
				}
			};

			TestViewModelItem Get_Depth_1_Item() => new TestViewModelItem
			{
				Label = $"Subitem {(char)(++initial_Depth_1)}",
				Items =
				{
					Get_Depth_2_Item(),
					Get_Depth_2_Item(),
				}
			};
			TestViewModelItem Get_Depth_2_Item() => new TestViewModelItem { Label = $"Subitem {(char)(++initial_Depth_2)}" };
		}

		public class TestViewModelItem : INotifyPropertyChanged
		{
			public event PropertyChangedEventHandler PropertyChanged;

			public bool IsExpanded { get; set; } = true;

			public ObservableCollection<TestViewModelItem> Items { get; } = new ObservableCollection<TestViewModelItem>();

			public string Label
			{
				get { return _label; }
				set
				{
					if (_label != value)
					{
						_label = value;
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
					}
				}
			}
			string _label;
		}
	}
}
