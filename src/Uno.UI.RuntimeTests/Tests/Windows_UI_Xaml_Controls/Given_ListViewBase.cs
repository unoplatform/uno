using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.ListViewPages;

#if !WINAPPSDK
using Uno.UI;
#endif

#if __APPLE_UIKIT__
using Foundation;
#endif

using Point = Windows.Foundation.Point;
using TabView = Microsoft.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;

using static Private.Infrastructure.TestServices;
using static Uno.UI.Extensions.ViewExtensions;
using MUXControlsTestApp.Utilities;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
#if __APPLE_UIKIT__
	[Ignore("Disable all listview tests until crash is resolved https://github.com/unoplatform/uno/issues/17101")]
#endif
	public partial class Given_ListViewBase // resources
	{
		private ResourceDictionary _testsResources;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		private Style BasicContainerStyle => _testsResources["BasicListViewContainerStyle"] as Style;

		private Style ContainerMarginStyle => _testsResources["ListViewContainerMarginStyle"] as Style;

		private Style NoSpaceContainerStyle => _testsResources["NoExtraSpaceListViewContainerStyle"] as Style;

		private Style FocusableContainerStyle => _testsResources["FocusableListViewItemStyle"] as Style;

		private DataTemplate TextBlockItemTemplate => _testsResources["TextBlockItemTemplate"] as DataTemplate;

		private DataTemplate WrappingTextBlockItemTemplate => _testsResources["WrappingTextBlockItemTemplate"] as DataTemplate;

		private DataTemplate SelfHostingItemTemplate => _testsResources["SelfHostingItemTemplate"] as DataTemplate;

		/// <summary>
		/// Size = 152w x 29h
		/// </summary>
		private DataTemplate FixedSizeItemTemplate => _testsResources["FixedSizeItemTemplate"] as DataTemplate;

		private DataTemplate NV286_Template => _testsResources["NV286_Template"] as DataTemplate;
		private DataTemplate DefaultItemTemplate => _testsResources["DefaultItemTemplate"] as DataTemplate;

		private ItemsPanelTemplate NoCacheItemsStackPanel => _testsResources["NoCacheItemsStackPanel"] as ItemsPanelTemplate;

		private ItemsPanelTemplate NonVirtualizingItemsPanel => _testsResources["NonVirtualizingItemsPanel"] as ItemsPanelTemplate;

		private DataTemplate SelectableItemTemplateA => _testsResources["SelectableItemTemplateA"] as DataTemplate;
		private DataTemplate SelectableItemTemplateB => _testsResources["SelectableItemTemplateB"] as DataTemplate;
		private DataTemplate SelectableItemTemplateC => _testsResources["SelectableItemTemplateC"] as DataTemplate;

		private DataTemplate RedSelectableTemplate => _testsResources["RedSelectableTemplate"] as DataTemplate;
		private DataTemplate GreenSelectableTemplate => _testsResources["GreenSelectableTemplate"] as DataTemplate;
		private DataTemplate BeigeSelectableTemplate => _testsResources["BeigeSelectableTemplate"] as DataTemplate;

		private DataTemplate SelectableBoundTemplateA => _testsResources["SelectableBoundTemplateA"] as DataTemplate;
		private DataTemplate SelectableBoundTemplateB => _testsResources["SelectableBoundTemplateB"] as DataTemplate;

		private DataTemplate BoundHeightItemTemplate => _testsResources["BoundHeightItemTemplate"] as DataTemplate;

		private DataTemplate ReuseCounterItemTemplate => _testsResources["ReuseCounterItemTemplate"] as DataTemplate;
	}

	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ListViewBase // test cases
	{
		[TestMethod]
		[RunsOnUIThread]
		public void ValidSelectionChange()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(3, list.SelectedItem);
			list.SelectedItem = 5;
			Assert.AreEqual(5, list.SelectedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeValidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(3, list.SelectedItem);
			list.SelectedItem = 17;
			Assert.AreEqual(3, list.SelectedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task ContainerIndicesAreUpdated()
		{
			var source = new ObservableCollection<string>();
			var SUT = new ListView { ItemsSource = source };
			WindowHelper.WindowContent = SUT;

			source.Add("test");

			await WindowHelper.WaitForIdle();

			source.Insert(0, "different");

			await WindowHelper.WaitForIdle();

#if HAS_UNO
			SUT.MaterializedContainers.Should().HaveCount(2);

			var containerIndices = SUT.MaterializedContainers
				.Select(container => container.GetValue(ItemsControl.IndexForItemContainerProperty))
				.OfType<int>()
				.OrderBy(index => index)
				.ToArray();

			containerIndices.Should().Equal(0, 1);
#endif

			var container0 = SUT.ContainerFromIndex(0);
			var containerItem = SUT.ContainerFromItem("different");
			Assert.AreEqual(container0, containerItem);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__
		[Ignore("Unlike other platforms, MaterializedContainers are removed immediately upon removal, and are not created on insertion until re-measure.")]
#endif
		public async Task ContainerIndicesAreUpdated_OnRemoveAndAdd()
		{
			var source = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item #{i}"));
			var item2 = source[2];
			var SUT = new ListView { ItemsSource = source };
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();
			SUT.MaterializedContainers.Should().HaveCount(5);

			var snapshotOriginal = GetMaterializedItems();
			source.RemoveAt(2);
			var snapshotRemoved = GetMaterializedItems();
			source.Insert(3, item2);
			var snapshotReinserted = GetMaterializedItems();
			await WindowHelper.WaitForIdle();
			var snapshotRemeasured = GetMaterializedItems();

			snapshotOriginal.Should().BeEquivalentTo(new (int index, object item)[]
			{
				(0, "Item #0"),
				(1, "Item #1"),
				(2, "Item #2"),
				(3, "Item #3"),
				(4, "Item #4"),
			});
			snapshotRemoved.Should().BeEquivalentTo(new (int index, object item)[]
			{
				(-1, null),
				(0, "Item #0"),
				(1, "Item #1"),
				(2, "Item #3"),
				(3, "Item #4"),
			});
			snapshotReinserted.Should().BeEquivalentTo(new (int index, object item)[]
			{
				(-1, null),
				(0, "Item #0"),
				(1, "Item #1"),
				(2, "Item #3"),
				/* 'Item #2' will be inserted here at index=3, but is not yet measured */
				(4, "Item #4"), // index got shifted with insertion
			});
			snapshotRemeasured.Should().BeEquivalentTo(new (int index, object item)[]
			{
				(0, "Item #0"),
				(1, "Item #1"),
				(2, "Item #3"),
				(3, "Item #2"),
				(4, "Item #4"),
			});

			(int Index, object Item)[] GetMaterializedItems() => SUT.MaterializedContainers
				.Select(container => (
					Index: (int)container.GetValue(ItemsControl.IndexForItemContainerProperty),
					Item: (container as ContentControl)?.Content
				))
				.OrderBy(x => x.Index)
				.ToArray();
		}
#endif


#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task ContainerParentIsKept_OnRemoveAndAdd()
		{
			var source = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Item #{i}"));

			var indexToAssert = 1;
			var item2 = source[indexToAssert];
			var SUT = new ListView { ItemsSource = source };
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			source.RemoveAt(indexToAssert);
			await WindowHelper.WaitForIdle();

			source.Insert(indexToAssert, item2);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull((SUT.ContainerFromIndex(indexToAssert) as ListViewItem)?.GetParent());

			// There are situations where the isse occurs the second time.
			indexToAssert = 3;
			var item4 = source[indexToAssert];

			source.RemoveAt(indexToAssert);
			await WindowHelper.WaitForIdle();

			source.Insert(indexToAssert, item4);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull((SUT.ContainerFromIndex(indexToAssert) as ListViewItem)?.GetParent());
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidChanges_ShouldNotBeReflectedOnSelectedItem()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };

			// select 3 (value, not index)
			list.SelectedItem = 3;
			Assert.AreEqual(3, list.SelectedItem);

			// modifying the original source, should not be reflected on the listview
			// since the source is not ObservableCollection or INotifyCollectionChanged
			source[3] = 13;
			Assert.AreEqual(3, list.SelectedItem);

			// setting an invalid value for SelectedItem, should be reverted to old value
			// and in this case, that should be the 3 from the original **unmodified** source
			list.SelectedItem = 17;
			Assert.AreEqual(3, list.SelectedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContainerSet_Then_ContentShouldBeSet()
		{
			var resources = new TestsResources();

			var SUT = new ListView
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = TextBlockItemTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			var source = new[] {
				"item 0",
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			var tb = si.FindFirstDescendant<TextBlock>();
			Assert.AreEqual("item 0", tb?.Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_IsItsOwnItemContainer_FromSource()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.AreEqual("item 1", si.Content);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_Parents_Include_ListView()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.IsNull(si.Parent);
			var parent = VisualTreeHelper.GetParent(si);
			while (parent is not null && parent is not ListView listView)
			{
				parent = VisualTreeHelper.GetParent(parent);
			}

			Assert.IsInstanceOfType(parent, typeof(ListView));
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Item_GetParentInternal_Include_ListView()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.IsNull(si.Parent);
			var parent = Uno.UI.Extensions.DependencyObjectExtensions.GetParentInternal(si, false);
			while (parent is not null && parent is not ListView listView)
			{
				parent = Uno.UI.Extensions.DependencyObjectExtensions.GetParentInternal(parent, false);
			}

			Assert.IsInstanceOfType(parent, typeof(ListView));
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NoItemTemplate()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = null,
				ItemTemplateSelector = null,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				"Item 1"
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.AreEqual("Item 1", si.Content);
#if WINAPPSDK // On iOS and Android (others not tested), ContentTemplateRoot is null, and TemplatedRoot is a ContentPresenter containing an ImplicitTextBlock
			Assert.IsInstanceOfType(si.ContentTemplateRoot, typeof(TextBlock));
			Assert.AreEqual("Item 1", (si.ContentTemplateRoot as TextBlock).Text);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_IsItsOwnItemContainer_FromSource_With_DataTemplate()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = TextBlockItemTemplate,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new object[] {
				new ListViewItem(){ Content = "item 1" },
				"item 2"
			};

			SUT.ItemsSource = source;

			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(SUT).Length == 2);
			var si = SUT.ContainerFromItem(source[0]) as SelectorItem;
			Assert.AreEqual("item 1", si.Content);
			Assert.AreSame(si, source[0]);
#if !WINAPPSDK
			Assert.IsFalse(si.IsGeneratedContainer);
#endif

			var si2 = SUT.ContainerFromItem(source[1]) as ListViewItem;

			Assert.IsNotNull(si2);
			Assert.AreNotSame(si2, source[1]);
			Assert.AreEqual("item 2", si2.Content);
#if !WINAPPSDK
			Assert.AreEqual("item 2", si2.DataContext);
			Assert.IsTrue(si2.IsGeneratedContainer);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TemplateRoot_IsOwnContainer()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = SelfHostingItemTemplate,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new object[]
			{
				"item 1",
				"item 2"
			};

			SUT.ItemsSource = source;

			ListViewItem lvi = null;
			await WindowHelper.WaitFor(() => (lvi = SUT.ContainerFromItem(source[0]) as ListViewItem) != null);

			Assert.IsNull(lvi.FindFirstDescendant<ListViewItem>());
			Assert.IsNull(lvi.FindFirstAncestor<ListViewItem>());
			Assert.AreEqual("SelfHostingListViewItem", lvi.Name);

			var content = lvi.Content as Border;
			Assert.IsNotNull(content);
			Assert.AreEqual("SelfHostingBorder", content.Name);
			Assert.AreEqual("item 1", (content.Child as TextBlock)?.Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task SingleItemSelected()
		{
			var child1 = new ListViewItem
			{
				IsSelected = true,
				Content = "child 1"
			};

			var child2 = new ListViewItem
			{
				Content = "child 2"
			};

			var child3 = new ListViewItem
			{
				Content = "child 3"
			};

			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single
			};
			list.Items.Add(child1);
			list.Items.Add(child2);
			list.Items.Add(child3);

			var sut = new Grid
			{
				Children = { list }
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();


			Assert.AreEqual(0, list.SelectedIndex);
			Assert.AreEqual(list.SelectedItem, child1);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task MultipleItemsSelected()
		{
			var child1 = new ListViewItem
			{
				IsSelected = true,
				Content = "child 1"
			};

			var child2 = new ListViewItem
			{
				Content = "child 2"
			};

			var child3 = new ListViewItem
			{
				IsSelected = true,
				Content = "child 3"
			};

			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple
			};
			list.Items.Add(child1);
			list.Items.Add(child2);
			list.Items.Add(child3);

			var sut = new Grid
			{
				Children = { list }
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();


			Assert.AreEqual(list.SelectedItems[0], child1);
			Assert.AreEqual(list.SelectedItems[1], child3);
		}

		[TestMethod]
		public async Task NoItemSelectedMultiple()
		{
			var child1 = new ListViewItem
			{
				Content = "child 1"
			};

			var child2 = new ListViewItem
			{
				Content = "child 2"
			};

			var child3 = new ListViewItem
			{
				Content = "child 3"
			};

			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple
			};
			list.Items.Add(child1);
			list.Items.Add(child2);
			list.Items.Add(child3);

			var sut = new Grid
			{
				Children = { list }
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();


			Assert.AreEqual(0, list.SelectedItems.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task NoItemSelectedSingle()
		{
			var child1 = new ListViewItem
			{
				Content = "child 1"
			};

			var child2 = new ListViewItem
			{
				Content = "child 2"
			};

			var child3 = new ListViewItem
			{
				Content = "child 3"
			};

			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single
			};
			list.Items.Add(child1);
			list.Items.Add(child2);
			list.Items.Add(child3);

			var sut = new Grid
			{
				Children = { list }
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();


			Assert.AreEqual(-1, list.SelectedIndex);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__
		[Ignore("The test can't find MultiSelectSquare")]
#endif
		public async Task When_Different_Selections_IsMultiSelectCheckBoxEnabled()
		{
			var singleList = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var multipleList = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var extendedList = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var singleList2 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single,
				IsMultiSelectCheckBoxEnabled = true,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var multipleList2 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
				IsMultiSelectCheckBoxEnabled = true,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var extendedList2 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
				IsMultiSelectCheckBoxEnabled = true,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var singleList3 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single,
				IsMultiSelectCheckBoxEnabled = false,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var multipleList3 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
				IsMultiSelectCheckBoxEnabled = false,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var extendedList3 = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
				IsMultiSelectCheckBoxEnabled = false,
				Items =
				{
					new ListViewItem
					{
						Content = "child 1"
					}
				}
			};

			var sp = new StackPanel
			{
				Children =
				{
					singleList,
					multipleList,
					extendedList,
					singleList2,
					multipleList2,
					extendedList2,
					singleList3,
					multipleList3,
					extendedList3
				}
			};

			WindowHelper.WindowContent = sp;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Collapsed, ((Border)singleList.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Visible, ((Border)multipleList.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)extendedList.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)singleList2.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Visible, ((Border)multipleList2.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)extendedList2.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)singleList3.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)multipleList3.FindName("MultiSelectSquare")).Visibility);
			Assert.AreEqual(Visibility.Collapsed, ((Border)extendedList3.FindName("MultiSelectSquare")).Visibility);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Multiple_Selection_Pointer()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var centers = items.Select(item => item.GetAbsoluteBounds().GetCenter()).ToList();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			mouse.Press(centers[1]);
			mouse.Release();

			selected.Add(items[1]);
			await AssertSelected();

			mouse.Press(centers[3], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			selected.AddRange(items.Where((_, i) => i is > 1 and <= 3).ToList());
			await AssertSelected();

			mouse.Press(centers[6]);
			mouse.Release();

			selected.Add(items[6]);
			await AssertSelected();

			mouse.Press(centers[8], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			selected.AddRange(items.Where((_, i) => i is > 6 and <= 8));
			await AssertSelected();

			mouse.Press(centers[8]);
			mouse.Release();

			selected.Remove(items[8]);
			await AssertSelected();

			mouse.Press(centers[0], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			items.Where((_, i) => i < 8).ForEach(item => selected.Remove(item));
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Multiple_Selection_Keyboard()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			list.SelectedIndex = 1;
			var result = await FocusManager.TryFocusAsync(list.ContainerFromIndex(1), FocusState.Pointer);
			Assert.IsTrue(result.Succeeded);

			selected.Add(items[1]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is > 1 and <= 3).ToList());
			await AssertSelected();

			await KeyboardHelper.Down(list);
			await KeyboardHelper.Down(list);

			await AssertSelected();

			await KeyboardHelper.Space(list);
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is >= 5 and <= 8).ToList());
			await AssertSelected();

			await KeyboardHelper.Down(list);
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Up, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Up, VirtualKeyModifiers.Shift));

			items.Where((_, i) => i is >= 7 and <= 8).ForEach(item => selected.Remove(item));
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Extended_Selection_Pointer()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var centers = items.Select(item => item.GetAbsoluteBounds().GetCenter()).ToList();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			mouse.Press(centers[1]);
			mouse.Release();

			selected.Add(items[1]);
			await AssertSelected();

			mouse.Press(centers[3]);
			mouse.Release();

			selected.Remove(items[1]);
			selected.Add(items[3]);
			await AssertSelected();

			mouse.Press(centers[5], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			selected.AddRange(items.Where((_, i) => i is > 3 and <= 5).ToList());
			await AssertSelected();

			mouse.Press(centers[7], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			selected.AddRange(items.Where((_, i) => i is > 5 and <= 7).ToList());
			await AssertSelected();

			mouse.Press(centers[1], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			items.Where((_, i) => i is > 3 and <= 7).ForEach(item => selected.Remove(item));
			selected.AddRange(items.Where((_, i) => i is >= 1 and < 3).ToList());
			await AssertSelected();

			mouse.Press(centers[8], VirtualKeyModifiers.Control);
			mouse.Release(VirtualKeyModifiers.Control);

			selected.Add(items[8]);
			await AssertSelected();

			mouse.Press(centers[4], VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			items.Where((_, i) => i is >= 1 and <= 3).ForEach(item => selected.Remove(item));
			selected.AddRange(items.Where((_, i) => i is >= 4 and < 8).ToList());
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Extended_Selection_Keyboard()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			list.SelectedIndex = 1;
			var result = await FocusManager.TryFocusAsync(list.ContainerFromIndex(1), FocusState.Pointer);
			Assert.IsTrue(result.Succeeded);

			selected.Add(items[1]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is > 1 and <= 3).ToList());
			await AssertSelected();

			await KeyboardHelper.Down(list);

			items.Where((_, i) => i is >= 1 and <= 3).ForEach(item => selected.Remove(item));
			selected.Add(items[4]);
			await AssertSelected();

			await KeyboardHelper.Down(list);

			selected.Remove(items[4]);
			selected.Add(items[5]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Space, VirtualKeyModifiers.Control));

			selected.Add(items[7]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Up, modifiers: VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Up, modifiers: VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Up, modifiers: VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Space, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is >= 4 and < 7 && i != 5).ToList());
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Control));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Space, VirtualKeyModifiers.Shift));

			items.Where((_, i) => i is >= 4 and < 7).ForEach(item => selected.Remove(item));
			selected.Add(items[8]);
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Extended_Selection_SelectedIndex_Changed_Keyboard()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			list.SelectedIndex = 1;
			var result = await FocusManager.TryFocusAsync(list.ContainerFromIndex(1), FocusState.Pointer);
			Assert.IsTrue(result.Succeeded);

			selected.Add(items[1]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is > 1 and <= 3).ToList());
			await AssertSelected();

			list.SelectedIndex = 6;
			items.Where((_, i) => i is >= 1 and <= 3).ForEach(item => selected.Remove(item));
			selected.Add(items[6]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.Remove(items[6]);
			selected.AddRange(items.Where((_, i) => i is >= 1 and <= 4).ToList());
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Extended_Selection_SelectedIndex_Changed_Mixed()
		{
			var items = Enumerable.Range(0, 10).Select(i => new ListViewItem { Content = i }).ToArray();
			var list = new ListView
			{
				SelectionMode = ListViewSelectionMode.Extended,
			};

			items.ForEach((ListViewItem item) => list.Items.Add(item));

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var selected = new List<ListViewItem>();
			await AssertSelected();

			list.SelectedIndex = 1;
			var result = await FocusManager.TryFocusAsync(list.ContainerFromIndex(1), FocusState.Pointer);
			Assert.IsTrue(result.Succeeded);

			selected.Add(items[1]);
			await AssertSelected();

			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));
			list.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(list, VirtualKey.Down, VirtualKeyModifiers.Shift));

			selected.AddRange(items.Where((_, i) => i is > 1 and <= 3).ToList());
			await AssertSelected();

			list.SelectedIndex = 6;
			items.Where((_, i) => i is >= 1 and <= 3).ForEach(item => selected.Remove(item));
			selected.Add(items[6]);
			await AssertSelected();

			mouse.Press(((ListViewItem)list.ContainerFromIndex(8)).GetAbsoluteBounds().GetCenter(), VirtualKeyModifiers.Shift);
			mouse.Release(VirtualKeyModifiers.Shift);

			selected.AddRange(items.Where((_, i) => i is >= 1 and <= 8 and not 6).ToList());
			await AssertSelected();

			async Task AssertSelected()
			{
				await WindowHelper.WaitForIdle();
				selected.ForEach(item => Assert.IsTrue(item.IsSelected));
				items.Except(selected).ForEach(item => Assert.IsFalse(item.IsSelected));
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if WINAPPSDK
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_Horizontal_Keyboard_Navigation()
		{
			var SUT = (ListView)XamlReader.Load("""
				<ListView
					xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
					ItemsSource="12345"
				        ScrollViewer.HorizontalScrollBarVisibility="Visible"
				        ScrollViewer.HorizontalScrollMode="Enabled"
				        ScrollViewer.VerticalScrollMode="Disabled">
					<ListView.ItemsPanel>
						<ItemsPanelTemplate>
							<ItemsStackPanel Orientation="Horizontal" />
						</ItemsPanelTemplate>
					</ListView.ItemsPanel>
				</ListView>
			""");

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			SUT.SelectedIndex = 1;
			var result = await FocusManager.TryFocusAsync(SUT.ContainerFromIndex(1), FocusState.Pointer);
			Assert.IsTrue(result.Succeeded);

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.SelectedIndex);

			await KeyboardHelper.Right();
			await KeyboardHelper.Right();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, SUT.SelectedIndex);

			await KeyboardHelper.Left();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, SUT.SelectedIndex);
		}

		[TestMethod]
		[RunsOnUIThread]
#if NETFX_CORE
		[Ignore("KeyboardHelper doesn't work on Windows")]
#endif
		public async Task When_Space_Or_Enter()
		{
			var SUT = new ListView
			{
				ItemsSource = "012345"
			};

			var grid = new Grid
			{
				Children =
				{
					SUT
				}
			};

			var handled = false;
			grid.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, args) => handled = args.Handled), true);

			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForIdle();

			var lvi1 = (ListViewItem)SUT.ContainerFromIndex(0);
			lvi1.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			await KeyboardHelper.Space();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(lvi1.IsSelected);
			Assert.IsTrue(handled);

			handled = false;

			SUT.SelectedIndex = -1;

			await KeyboardHelper.Enter();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(handled);
		}

		[TestMethod]
		public async Task When_IsItsOwnItemContainer_Recycling()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var oldTwo = new ListViewItem() { Content = "item 2" };
			var source = new ObservableCollection<ListViewItem> {
				new ListViewItem(){ Content = "item 1" },
				oldTwo,
			};

			SUT.ItemsSource = source;

			await WindowHelper.WaitFor(() => GetAllPanelChildren(SUT).Length == 2);

			var si = SUT.ContainerFromItem(source[0]) as SelectorItem;
			Assert.AreEqual("item 1", si.Content);

			source.RemoveAt(1);

			await WindowHelper.WaitFor(() => GetAllPanelChildren(SUT).Length == 1);

			var newTwo = new ListViewItem { Content = "item 2" };
			Assert.AreNotEqual(oldTwo, newTwo);

			source.Add(newTwo);

			await WindowHelper.WaitFor(() => GetAllPanelChildren(SUT).Length == 2);
			Assert.AreEqual(newTwo, GetAllPanelChildren(SUT).Last());
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Outer_ElementName_Binding()
		{
			var page = new ListViewPages.ListViewTemplateOuterBindingPage();

			for (int _ = 0; _ < 5; _++)
			{
				WindowHelper.WindowContent = page;
				await WindowHelper.WaitForIdle();

				var list = page.FindFirstDescendant<ListView>();
				Assert.IsNotNull(list);

				for (int i = 0; i < 3; i++)
				{
					ListViewItem lvi = null;
					await WindowHelper.WaitFor(() => (lvi = list.ContainerFromItem(i) as ListViewItem) != null);
					var sp = lvi.FindFirstDescendant<StackPanel>();
					var tb = sp?.FindFirstDescendant<TextBlock>();
					Assert.IsNotNull(tb);
					Assert.AreEqual("OuterContextText", tb.Text);
				}

				WindowHelper.WindowContent = null; // Unload page+list
				await WindowHelper.WaitForIdle();
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_CollectionViewSource_In_Xaml()
		{
			var page = new ListViewCollectionViewSourcePage();

			Assert.AreEqual(0, page.SubjectListView.Items.Count);

			page.CVS.Source = new[] { "One", "Two", "Three" };

			WindowHelper.WindowContent = page;

			await WindowHelper.WaitForLoaded(page.SubjectListView);

			await WindowHelper.WaitForIdle();

#if WINAPPSDK // TODO: subscribe to changes to Source property
			Assert.AreEqual(3, page.SubjectListView.Items.Count);
#endif
			ListViewItem lvi = null;
			await WindowHelper.WaitFor(() => (lvi = page.SubjectListView.ContainerFromItem("One") as ListViewItem) != null);
		}

		[TestMethod]
		public void When_Selection_SelectedValuePath_Set()
		{
			var SUT = new ListView();
			var source = new Dictionary<int, string>
			{
				{0, "Zero" },
				{1, "One" },
				{2, "Two" }
			};
			SUT.ItemsSource = source;
			SUT.SelectedValuePath = "Key";

			Assert.IsNull(SUT.SelectedValue);
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedValue = 1;

			var item1 = source.First(kvp => kvp.Key == 1);
			Assert.AreEqual(1, SUT.SelectedValue);
			Assert.AreEqual(item1, SUT.SelectedItem);
			Assert.AreEqual(1, SUT.SelectedIndex);

			// Set invalid
			SUT.SelectedValue = 4;

			Assert.IsNull(SUT.SelectedValue);
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);
		}

		[TestMethod]
		public void When_Selection_SelectedValue_Path_Not_Set()
		{
			var SUT = new ListView();
			var source = new List<string>
			{
				"Zero",
				"One",
				"Two",
			};
			SUT.ItemsSource = source;

			Assert.IsNull(SUT.SelectedValue);
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedValue = "Two";

			Assert.AreEqual("Two", SUT.SelectedValue);
			Assert.AreEqual("Two", SUT.SelectedItem);
			Assert.AreEqual(2, SUT.SelectedIndex);

			SUT.SelectedValue = "Eleventy";

			Assert.IsNull(SUT.SelectedValue);
			Assert.IsNull(SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);
		}

		[TestMethod]
#if __CROSSRUNTIME__
		[Ignore("This test is flaky on netstd platforms")]
#endif
		[RunsOnUIThread]
		public async Task When_Scrolled_To_End_And_Last_Item_Removed()
		{
			var container = new Grid { Height = 210 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 20));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(list);

			ScrollTo(list, 10000); // Scroll to end
			await Task.Delay(200);

			ListViewItem lastItem = null;
			await WindowHelper.WaitFor(() => (lastItem = list.ContainerFromItem(19) as ListViewItem) != null);
			var secondLastItem = list.ContainerFromItem(18) as ListViewItem;

			await WindowHelper.WaitFor(() => GetTop(lastItem, container), 181, comparer: ApproxEquals);
			await WindowHelper.WaitFor(() => GetTop(secondLastItem, container), 152, comparer: ApproxEquals);

			source.Remove(19);

			await WindowHelper.WaitFor(() => list.Items.Count == 19);

			// Force rebuild the layout so that TranslateTransform picks up
			// the updated values
			ScrollTo(list, 0); // Scroll to top
			ScrollTo(list, 100000); // Scroll to end

			await WindowHelper.WaitForEqual(181, () => GetTop(list.ContainerFromItem(18) as ListViewItem, container), tolerance: 2);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested.")]
#endif
		public async Task When_SmallExtent_And_Large_List_Scroll_To_End_Full_Size()
		{
			var materialized = 0;
			var container = new Grid { Height = 100, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 100,
						Child = tb
					};

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 50));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			ScrollTo(list, 1000000); // Scroll to end
			await Task.Delay(200);

			await WindowHelper.WaitForIdle();

			materialized.Should().BeLessThan(5);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested")]
#endif
		public async Task When_SmallExtent_And_Large_List_Scroll_To_End_Half_Size()
		{
			var materialized = 0;
			var container = new Grid { Height = 100, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 50));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			ScrollTo(list, 1000000); // Scroll to end

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, materialized);
		}


		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested.")]
#elif __WASM__
		[Ignore("Flaky in CI.")]
#endif
		public async Task When_Large_List_Scroll_To_End_Then_Back_Up_And_First_Item()
		{
			var materialized = 0;
			var container = new Grid { Height = 500, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 50));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			for (int i = 0; i < 3; i++)
			{
				ScrollTo(list, 1000000); // Scroll to end

				await Task.Delay(200);
				await WindowHelper.WaitForIdle();

				ScrollTo(list, 5); // Scroll back up

				await Task.Delay(600);
				await WindowHelper.WaitForIdle();

				var firstContainer = (FrameworkElement)list.ContainerFromIndex(0);

				firstContainer.Should().NotBeNull();
				LayoutInformation.GetLayoutSlot(firstContainer).Y.Should().BeLessOrEqualTo(0);

				var secondContainer = (FrameworkElement)list.ContainerFromIndex(1);

				secondContainer.Should().NotBeNull();
				LayoutInformation.GetLayoutSlot(secondContainer).Y.Should().BeApproximately(50, 0.6);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested.")]
#elif __WASM__
		[Ignore("Flaky in CI.")]
#endif
		public async Task When_Large_List_Scroll_To_End_Then_Back_Up_And_First_Item2()
		{
			var container = new Grid { Height = 500, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};

					return border;
				})
			};
			container.Children.Add(list);

			var source = new List<string>();

			var random = new Random(42);
			string RandomString(int length)
			{
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
				return new string(Enumerable.Repeat(chars, length)
					.Select(s => s[random.Next(s.Length)]).ToArray());
			}

			for (var i = 0; i < 100; i++)
			{
				source.Add(RandomString(5));
			}
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			ScrollTo(list, 1000000); // Scroll to end

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			ScrollTo(list, 50); // scroll back up but not all the way
			ScrollTo(list, 0);

			await Task.Delay(600);
			await WindowHelper.WaitForIdle();

			var firstContainer = (FrameworkElement)list.ContainerFromIndex(0);

			firstContainer.Should().NotBeNull();
			LayoutInformation.GetLayoutSlot(firstContainer).Y.Should().BeLessOrEqualTo(0);

			var secondContainer = (FrameworkElement)list.ContainerFromIndex(1);

			secondContainer.Should().NotBeNull();
			LayoutInformation.GetLayoutSlot(secondContainer).Y.Should().BeApproximately(50, 0.6);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_RENDER_TARGET_BITMAP
		[Ignore("Cannot take screenshot on this platform.")]
#endif
		public async Task When_Large_List_Scroll_To_End_Then_Back_Up_TryClick()
		{
			Assert.Inconclusive("Failing after inertia changes: https://github.com/unoplatform/uno-private/issues/1047");

			var container = new Grid { Height = 500, Width = 100 };

			var list = new ListView
			{
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};

					return border;
				})
			};
			container.Children.Add(list);

			var source = new List<string>();

			var random = new Random(42);
			string RandomString(int length)
			{
				const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
				return new string(Enumerable.Repeat(chars, length)
					.Select(s => s[random.Next(s.Length)]).ToArray());
			}

			for (var i = 0; i < 100; i++)
			{
				source.Add(RandomString(5));
			}
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			var mouse = injector.GetMouse();

			var bounds = list.GetAbsoluteBounds();
			mouse.MoveTo(new Point(bounds.GetMidX(), bounds.Top + 10));
			mouse.Press();
			mouse.Release();

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			// This problem is incredibly difficult to reproduce. These are the exact
			// mouse wheel deltas that my trackpad produced when I reproduced this manually.
			var deltas = new[]
			{
				-266,
				-393,
				-626,
				-730,
				-410,
				-644,
				-666,
				-328,
				-584,
				-671,
				-535,
				-250,
				-460,
				-514,
				-538,
				-382,
				-692,
				-342,
				-557,
				-331,
				-540,
				-320,
				-523,
				-385,
				-428,
				-397,
				-388,
				-143,
				-382,
				-370,
				-366,
				-358,
				-566,
				-128,
				-545,
				-121,
				-540,
				-97,
				-536,
				-55,
				-476,
				-246,
				-344,
				-203,
				-311,
				-190,
				-295,
				-225,
				-234,
				-86,
				-209,
				-26,
				-389,
				-189,
				-329,
				-57,
				-282,
				-130,
				-205,
				-108,
				-183,
				-106,
				-172,
				-102,
				-86,
				-75,
				-108,
				-112,
				-64,
				-24,
				-20,
				-20,
				-13,
				-11,
				-7,
				-4,
				-5,
				-2,
				231,
				122,
				164,
				266,
				887,
				167,
				565,
				434,
				382,
				372,
				344,
				170,
				269,
				547,
				158,
				272,
				434,
				168,
				253,
				386,
				154,
				269,
				244,
				238,
				399,
				91,
				374,
				153,
				282,
				143,
				225,
				194,
				60,
				183,
				154,
				262,
				60,
				249,
				55,
				232,
				46,
				168,
				37,
				128,
				55,
				82,
				37,
				68,
				27,
				35,
				27,
				17,
				13,
				12,
				2,
				6,
				3,
				2,
			};

			foreach (var delta in deltas.Where(i => i < 0))
			{
				mouse.Wheel(delta);
			}

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			mouse.MoveTo(new Point(bounds.GetMidX(), bounds.Top + 220));
			mouse.Press();
			mouse.Release();

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			foreach (var delta in deltas.Where(i => i > 0))
			{
				mouse.Wheel(delta);
				await WindowHelper.WaitForIdle();
			}

			mouse.MoveTo(new Point(bounds.GetMidX(), bounds.Top + 10));
			mouse.Press();
			mouse.Release();

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			mouse.MoveTo(new Point(bounds.GetMidX(), bounds.Top + 60));
			mouse.Press();
			mouse.Release();

			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			// The second container should be selected and nothing else.
			// Trying to check for this with references could be misleading,
			// since this is originally a virtualization issue and references
			// could be to different things than those shown on the screen.
			var si = await UITestHelper.ScreenShot(list, true);
			// on macOS/metal we get the color #1A6AA7 which is quite close but not identical,
			// similar inaccuracy is happening on Linux as well
			byte tolerance = 1;
			ImageAssert.HasColorAt(si, 70, 65, Colors.FromARGB("#1A69A6"), tolerance); // selected

			// check starting from below the second item that nothing looks selected or hovered
			ImageAssert.DoesNotHaveColorInRectangle(si, new Rectangle(100, 110, si.Width - 100, si.Height - 110), Colors.FromARGB("#1A69A6"), tolerance); // selected
			ImageAssert.DoesNotHaveColorInRectangle(si, new Rectangle(100, 110, si.Width - 100, si.Height - 110), Colors.FromARGB("#FFE6E6E6"), tolerance); // hovered
		}

		[TestMethod]
		[RunsOnUIThread]
#if !__CROSSRUNTIME__
		[Ignore("Native listviews differ in virtualization mechanics")]
#endif
		public async Task ListView_ObservableCollection_Creation_Count()
		{
			var SUT = new ListView_ObservableCollection_CreationCount();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			await AdvanceAutomation("Added");
			await AdvanceAutomation("Scrolled1");

			var expectedTemplateCreationCount = GetTemplateCreationCount();
			//var expectedTemplateBindCount = GetTemplateBindCount(); // For some reason WASM performs extra bindings on scrolling
			var expectedContainerCreationCount = GetContainerCreationCount();

			await AdvanceAutomation("Scrolled2");

			Assert.AreEqual(expectedTemplateCreationCount, GetTemplateCreationCount());
			Assert.AreEqual(expectedContainerCreationCount, GetContainerCreationCount());

			var expectedTemplateBindCount = GetTemplateBindCount();

			await AdvanceAutomation("Added above");

			Assert.AreEqual(expectedTemplateCreationCount, GetTemplateCreationCount());
			Assert.AreEqual(expectedContainerCreationCount, GetContainerCreationCount());
			Assert.AreEqual(expectedTemplateBindCount, GetTemplateBindCount()); // Note: this doesn't actually seem to be the case on Windows - the bind count increases for some reason

			await AdvanceAutomation("Removed above");

			Assert.AreEqual(expectedTemplateCreationCount, GetTemplateCreationCount());
			Assert.AreEqual(expectedContainerCreationCount, GetContainerCreationCount());
			Assert.AreEqual(expectedTemplateBindCount, GetTemplateBindCount());

			int GetTemplateCreationCount() => int.Parse(((TextBlock)SUT.FindName("CreationCountText")).Text);
			int GetTemplateBindCount() => int.Parse(((TextBlock)SUT.FindName("BindCountText")).Text);
			int GetContainerCreationCount() => int.Parse(((TextBlock)SUT.FindName("CreationCount2Text")).Text);

			async Task AdvanceAutomation(string automationStep)
			{
				var button = (Button)SUT.FindName("AutomateButton");
				button.RaiseClick();
				await WindowHelper.WaitFor(() => ((TextBlock)SUT.FindName("AutomationStepTextBlock")).Text == automationStep);
				await WindowHelper.WaitForIdle();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Arrow_keys_ListView_Only_Scrolled_As_Needed()
		{
			var SUT = new ListView
			{
				Height = 120, // fits 2 items and a bit
				ItemsSource = "12345"
			};

			await UITestHelper.Load(SUT);

			var sv = (ScrollViewer)SUT.GetTemplateChild("ScrollViewer");
			var lvi = (ListViewItem)SUT.ContainerFromIndex(0);

			lvi.Focus(FocusState.Programmatic);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sv.VerticalOffset);

			await KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, sv.VerticalOffset);

			await KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();

			sv.VerticalOffset.Should().BeApproximately(lvi.ActualHeight * 3 - 120, 2);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested")]
#endif
		public async Task When_SmallExtent_And_Large_List_Scroll_To_End_And_Back_Half_Size()
		{
			var materialized = 0;
			var dataContextChanged = 0;
			var container = new Grid { Height = 100, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};
					tb.RegisterPropertyChangedCallback(FrameworkElement.DataContextProperty, (s, e) => dataContextChanged++);

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 50));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			using var scope = new AssertionScope();

			var scroll = list.FindFirstDescendant<ScrollViewer>();
			Assert.IsNotNull(scroll);

			dataContextChanged.Should().BeLessThan(5, $"dataContextChanged {dataContextChanged}");

			ScrollTo(list, scroll.ExtentHeight / 2); // Scroll to middle

			await WindowHelper.WaitForIdle();

			materialized.Should().BeLessThan(8, $"materialized {materialized}");
			dataContextChanged.Should().BeLessThan(10, $"dataContextChanged {dataContextChanged}");

			ScrollTo(list, scroll.ExtentHeight / 4); // Scroll to Quarter

			await WindowHelper.WaitForIdle();

			materialized.Should().BeLessThan(8, $"materialized {materialized}");
			dataContextChanged.Should().BeLessThan(15, $"dataContextChanged {dataContextChanged}");
		}

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested")]
#endif
		public async Task When_SmallExtent_And_Very_Large_List_Scroll_To_End_And_Back_Half_Size()
		{
			var materialized = 0;
			var container = new Grid { Height = 100, Width = 100 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = 50,
						Child = tb
					};

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 500));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			using var scope = new AssertionScope();

			var scroll = list.FindFirstDescendant<ScrollViewer>();
			Assert.IsNotNull(scroll);

			ScrollTo(list, scroll.ExtentHeight / 2); // Scroll to middle
			await Task.Delay(200); // Allow the scroll to complete

			await WindowHelper.WaitForIdle();

			materialized.Should().BeLessThan(10, $"materialized {materialized}");

			ScrollTo(list, scroll.ExtentHeight / 4); // Scroll to Quarter
			await Task.Delay(200); // Allow the scroll to complete

			await WindowHelper.WaitForIdle();

			materialized.Should().BeLessThan(10, $"materialized {materialized}");
		}


		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__ || __ANDROID__
		[Ignore("Disabled because of animated scrolling, even when explicitly requested")]
#endif
		public async Task When_LargeExtent_And_Very_Large_List_Scroll_To_End_And_Back_Half_Size()
		{
			const int ElementHeight = 50;
			var materialized = 0;
			var dataContextChanged = 0;
			var container = new Grid { Height = 300, Width = 300 };

			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = ElementHeight,
						Child = tb
					};
					tb.RegisterPropertyChangedCallback(FrameworkElement.DataContextProperty, (s, e) => dataContextChanged++);

					materialized++;

					return border;
				})
			};
			container.Children.Add(list);

			var source = new ObservableCollection<int>(Enumerable.Range(0, 500));
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			using var scope = new AssertionScope();

			var scroll = list.FindFirstDescendant<ScrollViewer>();
			Assert.IsNotNull(scroll);

			int expectedMaterialized = 0, expectedDCChanged = 0;
			int[] previouslyMaterializedItems = [];
			async Task ScrollAndValidate(string context, double? scrollTo)
			{
				if (scrollTo is { } voffset)
				{
					ScrollTo(list, voffset);
				}

				await WindowHelper.WaitForIdle();

#if HAS_UNO && !(__IOS__ || __ANDROID__)
				var evpScaling = (list.ItemsPanelRoot as IVirtualizingPanel).GetLayouter().CacheLength * VirtualizingPanelLayout.ExtendedViewportScaling;
#else
				var evpScaling = 0.5;
#endif

				var offset = scroll.VerticalOffset;
				var max = scroll.ExtentHeight;
				var vp = scroll.ViewportHeight;

				var evpStart = Math.Clamp(offset - vp * evpScaling, 0, max);
				var evpEnd = Math.Clamp(offset + vp + vp * evpScaling, 0, max);

				var firstIndex = (int)Math.Round(evpStart / ElementHeight, 0, MidpointRounding.ToNegativeInfinity);
				var lastIndex = (int)Math.Round(evpEnd / ElementHeight, 0, MidpointRounding.ToPositiveInfinity) - 1;
				var itemsInEVP = Enumerable.Range(firstIndex, lastIndex - firstIndex + 1).ToArray();
				var newItemsInEVP = itemsInEVP.Except(previouslyMaterializedItems).ToArray();

				// materialized starts with +1 extra, since we use it to determine whether the DataTemplate itself is a self-container
				// Math.Max to count the historical highest, since "materialization" doesnt "unhappen" (we dont count tear-down).
				expectedMaterialized = Math.Max(expectedMaterialized, 1 + itemsInEVP.Length);
				// dc-changed counts the total items prepared and "re-entrancy"(out of effective-viewport and back in).
				// we just need to add the new items since last time
				expectedDCChanged += newItemsInEVP.Length;
				previouslyMaterializedItems = itemsInEVP;

				materialized.Should().BeLessOrEqualTo(expectedMaterialized, $"[{context}] materialized {materialized}");
				dataContextChanged.Should().BeLessOrEqualTo(expectedDCChanged, $"[{context}] dataContextChanged {dataContextChanged}");
			}

			await ScrollAndValidate("initial state", null);
			await ScrollAndValidate("scrolled past element#0", ElementHeight);
			await ScrollAndValidate("scrolled past element#2", ElementHeight * 3);

			var isiOS = OperatingSystem.IsIOS();
			if (!isiOS)
			{
				// TODO: this is flaky on iOS, needs investigation: uno-private#1415
				await ScrollAndValidate("scrolled to 1/2", scroll.ExtentHeight / 2);
				await ScrollAndValidate("scrolled back to 1/4", scroll.ExtentHeight / 4);
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Their_Own_Container()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			using var _ = new AssertionScope();

			// Containers/indices/items can be retrieved
			Assert.AreEqual(items[1], list.ContainerFromItem(items[1]));
			Assert.AreEqual(items[2], list.ContainerFromIndex(2));
			Assert.AreEqual(3, list.IndexFromContainer(items[3]));
			Assert.AreEqual(items[1], list.ItemFromContainer(items[1]));
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Own_Container_ContainerFromItem_Owner()
		{
			var list = new ListView();
			var item = new ListViewItem();
			var items = new ObservableCollection<ListViewItem>()
			{
				item,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 1);

			var container = list.ContainerFromItem(item);
			Assert.AreEqual(list, ItemsControl.ItemsControlFromItemContainer(container));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Own_Container_ContainerFromIndex_Owner()
		{
			var list = new ListView();
			var item = new ListViewItem();
			var items = new ObservableCollection<ListViewItem>()
			{
				item,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 1);

			var container = list.ContainerFromIndex(0);
			Assert.AreEqual(list, ItemsControl.ItemsControlFromItemContainer(container));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Own_Container_Direct_Owner()
		{
			var list = new ListView();
			var item = new ListViewItem();
			var items = new ObservableCollection<ListViewItem>()
			{
				item,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 1);

			Assert.AreEqual(list, ItemsControl.ItemsControlFromItemContainer(item));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Not_Own_Container_ContainerFromItem_Owner()
		{
			var list = new ListView();
			var items = new ObservableCollection<int>()
			{
				1,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 1);

			var container = list.ContainerFromItem(items[0]);
			Assert.AreEqual(list, ItemsControl.ItemsControlFromItemContainer(container));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Not_Own_Container_ContainerFromIndex_Owner()
		{
			var list = new ListView();
			var items = new ObservableCollection<int>()
			{
				1,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 1);

			var container = list.ContainerFromIndex(0);
			Assert.AreEqual(list, ItemsControl.ItemsControlFromItemContainer(container));
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Their_Own_Container_In_OnItemsChanged_Removal()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item removal

			var removedItem = items[1];
			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before removed
				Assert.AreEqual(items[0], list.ContainerFromItem(items[0]));
				Assert.AreEqual(items[0], list.ContainerFromIndex(0));
				Assert.AreEqual(items[0], list.ItemFromContainer(items[0]));
				Assert.AreEqual(0, list.IndexFromContainer(items[0]));

				// Test removed container/index/item
				Assert.IsNull(list.ContainerFromItem(removedItem));
				// In UWP, the Item is returned even though it is already removed
				// This is a weird behavior and doesn't seem too useful anyway, so we currently
				// ignore it
				// Assert.AreEqual(removedItem, list.ItemFromContainer(removedItem));
				Assert.AreEqual(-1, list.IndexFromContainer(removedItem));

				// Test container/index/item right after removed
				Assert.AreEqual(items[1], list.ContainerFromItem(items[1]));
				Assert.AreEqual(items[1], list.ContainerFromIndex(1));
				Assert.AreEqual(items[1], list.ItemFromContainer(items[1]));
				Assert.AreEqual(1, list.IndexFromContainer(items[1]));

				// Test container/index/item after removed
				Assert.AreEqual(items[2], list.ContainerFromItem(items[2]));
				Assert.AreEqual(items[2], list.ContainerFromIndex(2));
				Assert.AreEqual(items[2], list.ItemFromContainer(items[2]));
				Assert.AreEqual(2, list.IndexFromContainer(items[2]));
			};

			items.Remove(removedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Their_Own_Container_In_OnItemsChanged_Addition()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item removal

			var addedItem = new ListViewItem();
			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before added
				Assert.AreEqual(items[0], list.ContainerFromItem(items[0]));
				Assert.AreEqual(items[0], list.ContainerFromIndex(0));
				Assert.AreEqual(items[0], list.ItemFromContainer(items[0]));
				Assert.AreEqual(0, list.IndexFromContainer(items[0]));

				// Test added container/index/item
#if HAS_UNO
				// UWP returns null/-1 here, which differs from "the same"
				// situation in case of collection change. For simplicity
				// we return the correct values here too. It should not have
				// any adverse impact.
				Assert.AreEqual(addedItem, list.ContainerFromItem(addedItem));
				Assert.AreEqual(addedItem, list.ContainerFromIndex(1));
				Assert.AreEqual(addedItem, list.ItemFromContainer(addedItem));
				Assert.AreEqual(1, list.IndexFromContainer(addedItem));
#endif

				// Test container/index/item right after added
				Assert.AreEqual(items[2], list.ContainerFromItem(items[2]));
				Assert.AreEqual(items[2], list.ContainerFromIndex(2));
				Assert.AreEqual(items[2], list.ItemFromContainer(items[2]));
				Assert.AreEqual(2, list.IndexFromContainer(items[2]));

				// Test container/index/item after removed
				Assert.AreEqual(items[3], list.ContainerFromItem(items[3]));
				Assert.AreEqual(items[3], list.ContainerFromIndex(3));
				Assert.AreEqual(items[3], list.ItemFromContainer(items[3]));
				Assert.AreEqual(3, list.IndexFromContainer(items[3]));
			};

			items.Insert(1, addedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Their_Own_Container_In_OnItemsChanged_Change()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item change
			var oldItem = items[1];
			var newItem = new ListViewItem() { Content = "New" };

			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before removed
				Assert.AreEqual(items[0], list.ContainerFromItem(items[0]));
				Assert.AreEqual(items[0], list.ContainerFromIndex(0));
				Assert.AreEqual(items[0], list.ItemFromContainer(items[0]));
				Assert.AreEqual(0, list.IndexFromContainer(items[0]));

				// Test old container/index/item
				Assert.IsNull(list.ContainerFromItem(oldItem));
				Assert.IsNull(list.ItemFromContainer(oldItem));
				Assert.AreEqual(-1, list.IndexFromContainer(oldItem));

				// Test new container/index/item
				Assert.AreEqual(newItem, list.ContainerFromItem(newItem));
				Assert.AreEqual(newItem, list.ContainerFromIndex(1));
				Assert.AreEqual(newItem, list.ItemFromContainer(newItem));
				Assert.AreEqual(1, list.IndexFromContainer(newItem));

				// Test container/index/item right after changed
				Assert.AreEqual(items[2], list.ContainerFromItem(items[2]));
				Assert.AreEqual(items[2], list.ContainerFromIndex(2));
				Assert.AreEqual(items[2], list.ItemFromContainer(items[2]));
				Assert.AreEqual(2, list.IndexFromContainer(items[2]));

				// Test container/index/item after changed
				Assert.AreEqual(items[3], list.ContainerFromItem(items[3]));
				Assert.AreEqual(items[3], list.ContainerFromIndex(3));
				Assert.AreEqual(items[3], list.ItemFromContainer(items[3]));
				Assert.AreEqual(3, list.IndexFromContainer(items[3]));
			};

			items[1] = newItem;
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Their_Own_Container_In_OnItemsChanged_Reset()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item change
			var newItems = new ObservableCollection<ListViewItem>()
			{
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
				new ListViewItem(),
			};

			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item from old source
				Assert.IsNull(list.ContainerFromItem(items[1]));
				Assert.IsNull(list.ItemFromContainer(items[1]));
				Assert.AreEqual(-1, list.IndexFromContainer(items[1]));

				// Test container/index/item from new source
#if HAS_UNO
				// UWP returns null/-1 here, which differs from "the same"
				// situation in case of collection change. For simplicity
				// we return the correct values here too. It should not have
				// any adverse impact.
				Assert.AreEqual(newItems[1], list.ContainerFromItem(newItems[1]));
				Assert.AreEqual(newItems[1], list.ContainerFromIndex(1));
				Assert.AreEqual(newItems[1], list.ItemFromContainer(newItems[1]));
				Assert.AreEqual(1, list.IndexFromContainer(newItems[1]));
#endif
			};

			list.ItemsSource = newItems;
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Not_Their_Own_Container()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<int>()
			{
				1,
				2,
				3,
				4,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			using var _ = new AssertionScope();

			// Containers/indices/items can be retrieved
			var container2 = (ListViewItem)list.ContainerFromItem(2);
			Assert.AreEqual(2, container2.Content);
			var container3 = (ListViewItem)list.ContainerFromIndex(2);
			Assert.AreEqual(3, container3.Content);
			Assert.AreEqual(2, list.IndexFromContainer(container3));
			Assert.AreEqual(2, list.ItemFromContainer(container2));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Not_Their_Own_Container_In_OnItemsChanged_Removal()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<int>()
			{
				1,
				2,
				3,
				4,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item removal

			var removedItem = items[1];
			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before removed
				var container0 = (ListViewItem)list.ContainerFromItem(items[0]);
				Assert.AreEqual(items[0], container0.Content);
				var containerIndex0 = list.ContainerFromIndex(0);
				Assert.AreEqual(container0, containerIndex0);
				Assert.AreEqual(items[0], list.ItemFromContainer(container0));
				Assert.AreEqual(0, list.IndexFromContainer(container0));

				// Test removed container/index/item
				Assert.IsNull(list.ContainerFromItem(removedItem));

				// Test container/index/item right after removed
				var container1 = (ListViewItem)list.ContainerFromItem(items[1]);
				Assert.AreEqual(items[1], container1.Content);
				var containerIndex1 = list.ContainerFromIndex(1);
				Assert.AreEqual(container1, containerIndex1);
				Assert.AreEqual(items[1], list.ItemFromContainer(container1));
				Assert.AreEqual(1, list.IndexFromContainer(container1));

				// Test container/index/item after removed
				var container2 = (ListViewItem)list.ContainerFromItem(items[2]);
				Assert.AreEqual(items[2], container2.Content);
				var containerIndex2 = list.ContainerFromIndex(2);
				Assert.AreEqual(container2, containerIndex2);
				Assert.AreEqual(items[2], list.ItemFromContainer(container2));
				Assert.AreEqual(2, list.IndexFromContainer(container2));
			};

			items.Remove(removedItem);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Not_Their_Own_Container_In_OnItemsChanged_Addition()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<int>()
			{
				1,
				2,
				3,
				4,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item removal

			var addedItem = new ListViewItem();
			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before added
				var container0 = (ListViewItem)list.ContainerFromItem(items[0]);
				Assert.AreEqual(items[0], container0.Content);
				var containerIndex0 = list.ContainerFromIndex(0);
				Assert.AreEqual(container0, containerIndex0);
				Assert.AreEqual(items[0], list.ItemFromContainer(container0));
				Assert.AreEqual(0, list.IndexFromContainer(container0));

				// Test added container/index/item
				// UWP returns null/-1 here, which differs from "the same"
				// situation in case of collection change. For simplicity
				// we return the correct values here too. It should not have
				// any adverse impact.
				var container1 = (ListViewItem)list.ContainerFromItem(42);
				Assert.IsNull(container1);
				var containerIndex1 = (ListViewItem)list.ContainerFromIndex(1);
				Assert.IsNull(containerIndex1);

				// Test container/index/item right after added
				var container2 = (ListViewItem)list.ContainerFromItem(items[2]);
				Assert.AreEqual(items[2], container2.Content);
				var containerIndex2 = list.ContainerFromIndex(2);
				Assert.AreEqual(container2, containerIndex2);
				Assert.AreEqual(items[2], list.ItemFromContainer(container2));
				Assert.AreEqual(2, list.IndexFromContainer(container2));

				// Test container/index/item after removed
				var container3 = (ListViewItem)list.ContainerFromItem(items[3]);
				Assert.AreEqual(items[3], container3.Content);
				var containerIndex3 = list.ContainerFromIndex(3);
				Assert.AreEqual(container3, containerIndex3);
				Assert.AreEqual(items[3], list.ItemFromContainer(container3));
				Assert.AreEqual(3, list.IndexFromContainer(container3));
			};

			items.Insert(1, 42);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Not_Their_Own_Container_In_OnItemsChanged_Change()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<int>()
			{
				1,
				2,
				3,
				4,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item change
			var oldItem = items[1];
			var oldContainer = list.ContainerFromItem(items[1]);

			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before removed
				var container0 = (ListViewItem)list.ContainerFromItem(items[0]);
				Assert.AreEqual(items[0], container0.Content);
				var containerIndex0 = list.ContainerFromIndex(0);
				Assert.AreEqual(container0, containerIndex0);
				Assert.AreEqual(items[0], list.ItemFromContainer(container0));
				Assert.AreEqual(0, list.IndexFromContainer(container0));

				// Test old container for old item
				Assert.IsNull(list.ContainerFromItem(oldItem));

				// Container for new item should abe available
				var container1 = (ListViewItem)list.ContainerFromItem(items[1]);
				Assert.IsNotNull(container1);
				var containerIndex1 = list.ContainerFromIndex(1);
				Assert.IsNotNull(containerIndex1);

				// Test container/index/item right after changed
				var container2 = (ListViewItem)list.ContainerFromItem(items[2]);
				Assert.AreEqual(items[2], container2.Content);
				var containerIndex2 = list.ContainerFromIndex(2);
				Assert.AreEqual(container2, containerIndex2);
				Assert.AreEqual(items[2], list.ItemFromContainer(container2));
				Assert.AreEqual(2, list.IndexFromContainer(container2));

				// Test container/index/item after removed
				var container3 = (ListViewItem)list.ContainerFromItem(items[3]);
				Assert.AreEqual(items[3], container3.Content);
				var containerIndex3 = list.ContainerFromIndex(3);
				Assert.AreEqual(container3, containerIndex3);
				Assert.AreEqual(items[3], list.ItemFromContainer(container3));
				Assert.AreEqual(3, list.IndexFromContainer(container3));
			};

			items[1] = 42;
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Not_Their_Own_Container_In_OnItemsChanged_Reset()
		{
			var list = new OnItemsChangedListView();
			var items = new ObservableCollection<int>()
			{
				1,
				2,
				3,
				4,
			};

			list.ItemsSource = items;
			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			// Item change
			var newItems = new ObservableCollection<int>()
			{
				5,
				6,
				7,
				8,
			};

			var oldItem = items[1];
			var oldContainer = list.ContainerFromIndex(1);

			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item from old source
				Assert.IsNull(list.ContainerFromItem(oldItem));
				Assert.IsNull(list.ItemFromContainer(oldContainer));
				Assert.AreEqual(-1, list.IndexFromContainer(oldContainer));

				// Test container/index/item from new source
				var container2 = (ListViewItem)list.ContainerFromItem(newItems[2]);
				Assert.IsNull(container2);
				var containerIndex2 = list.ContainerFromIndex(2);
				Assert.IsNull(containerIndex2);
			};

			list.ItemsSource = newItems;
		}

		[TestMethod]
		public async Task When_ItemTemplateSelector_Set()
		{
			var itemsSource = new[] { "item 1", "item 2", "item 3" };
			var templateSelector = new KeyedTemplateSelector
			{
				Templates = {
					{ itemsSource[0], SelectableItemTemplateA },
					{ itemsSource[1], SelectableItemTemplateB },
					{ itemsSource[2], SelectableItemTemplateC },
				}
			};

			var list = new ListView
			{
				ItemsSource = itemsSource,
				ItemTemplateSelector = templateSelector
			};

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);

			var container1 = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem);
			var text1 = container1.FindFirstDescendant<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text1);
			Assert.AreEqual("Selectable A", text1.Text);

			var container2 = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(1) as ListViewItem);
			var text2 = container2.FindFirstDescendant<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text2);
			Assert.AreEqual("Selectable B", text2.Text);

			var container3 = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(2) as ListViewItem);
			var text3 = container3.FindFirstDescendant<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text3);
			Assert.AreEqual("Selectable C", text3.Text);
		}

		[TestMethod]
		public async Task When_ItemTemplateSelector_Set_And_Uwp()
		{
			using var _ = StyleHelper.UseUwpStyles();
			await When_ItemTemplateSelector_Set();
		}

		[TestMethod]
		public async Task When_Removed_From_Tree_And_Selection_TwoWay_Bound()
		{
			var page = new ListViewBoundSelectionPage();

			var dc = new When_Removed_From_Tree_And_Selection_TwoWay_Bound_DataContext();
			page.DataContext = dc;

			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			Assert.IsNull(dc.MySelection);

			var SUT = page.MyListView;
			SUT.SelectedItem = "Rice";
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Rice", dc.MySelection);

			page.HostPanel.Children.Remove(page.IntermediateGrid);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(SUT.DataContext);

			Assert.AreEqual("Rice", dc.MySelection);
		}

		[TestMethod]
		public async Task When_ItemsSource_Move()
		{
			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate
			};

			var source = new ObservableCollection<string>
			{
				"Item0",
				"Item1",
				"Item2",
			};
			list.ItemsSource = source;
			list.SelectedIndex = 0;

			var outer = new Grid { Children = { list } };
			WindowHelper.WindowContent = outer;

			await WindowHelper.WaitForLoaded(list);
			var container1 = await WindowHelper.WaitForNonNull(() => list.ContainerFromItem("Item1") as ListViewItem);
			Assert.AreEqual(29, GetTop(container1, outer), Epsilon);
			Assert.AreEqual(0, list.SelectedIndex);
			Assert.AreEqual("Item0", list.SelectedItem);

			source.Move(1, 2);
			await WindowHelper.WaitForEqual(58, () =>
			{
				var container = list.ContainerFromItem("Item1") as ListViewItem;
				return GetTop(container, outer);
			});

			Assert.AreEqual(0, list.SelectedIndex);
			Assert.AreEqual("Item0", list.SelectedItem);

			source.Move(2, 0);
			await WindowHelper.WaitForEqual(0, () =>
			{
				var container = list.ContainerFromItem("Item1") as ListViewItem;
				return GetTop(container, outer);
			});

			Assert.AreEqual(-1, list.SelectedIndex);
			Assert.IsNull(list.SelectedItem);
		}

		[TestMethod]
		public async Task When_Selection_Events()
		{
			var list = new ListView
			{
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = FixedSizeItemTemplate
			};

			var items = Enumerable.Range(0, 4).Select(x => "Item_" + x).ToArray();
			list.ItemsSource = items;
			list.SelectedIndex = 0;

			var model = new When_Selection_Events_DataContext();
			list.DataContext = model;
			list.SetBinding(Selector.SelectedIndexProperty, new Binding { Path = new PropertyPath(nameof(model.SelectedIndex)), Mode = BindingMode.TwoWay });
			list.SetBinding(Selector.SelectedItemProperty, new Binding { Path = new PropertyPath(nameof(model.SelectedItem)), Mode = BindingMode.TwoWay });
			list.SetBinding(Selector.SelectedValueProperty, new Binding { Path = new PropertyPath(nameof(model.SelectedValue)), Mode = BindingMode.TwoWay });

			WindowHelper.WindowContent = list;
			await WindowHelper.WaitForLoaded(list);
			await WindowHelper.WaitFor(() => GetPanelVisibleChildren(list).Length == 4);

			list.SelectionChanged += (s, e) =>
			{
				Assert.AreEqual("Item_1", list.SelectedItem);
				Assert.AreEqual("Item_1", list.SelectedValue);
				Assert.AreEqual(1, model.SelectedIndex);
				Assert.AreEqual("Item_1", model.SelectedItem);
				Assert.AreEqual("Item_1", model.SelectedValue);
			};

			// update selection
			list.SelectedIndex = 1;
		}

		[TestMethod]
		public async Task When_DisplayMemberPath_Property_Changed()
		{
			var itemsSource = (new[] { "aaa", "bbb", "ccc", "ddd" }).Select(s => new When_DisplayMemberPath_Property_Changed_DataContext { Display = s }).ToArray();

			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				DisplayMemberPath = "Display",
				ItemsSource = itemsSource
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var secondContainer = await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(1) as ListViewItem);
			await WindowHelper.WaitForLoaded(secondContainer);

			var tb = secondContainer.FindFirstDescendant<TextBlock>();
			Assert.AreEqual("bbb", tb.Text);

			foreach (var item in itemsSource)
			{
				item.Display = item.Display.ToUpperInvariant();
			}

			await WindowHelper.WaitForResultEqual("BBB", () => tb.Text);
		}

		[TestMethod]
		public async Task When_Item_Removed_And_Relayout_NV286()
		{
			using (FeatureConfigurationHelper.UseListViewAnimations())
			{
				var source = new ObservableCollection<string>();
				var index = 0;
				for (int i = 0; i < 5; i++)
				{
					InsertAnItem();
				}

				var SUT = new ListView
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Width = 300,
					Height = 180,
					Background = new SolidColorBrush(Colors.Beige),
					ItemsSource = source,
					ItemTemplate = NV286_Template
				};

				var errorCatchGrid = new ErrorCatchGrid
				{
					Children =
					{
						SUT
					}
				};

				WindowHelper.WindowContent = errorCatchGrid;
				await WindowHelper.WaitForLoaded(SUT);
				await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(3));

				source.RemoveAt(source.Count - 1);
				InsertAnItem();
				InsertAnItem();

				await Task.Delay(5); // The key to reproing the bug is to trigger a relayout asynchronously, while the disappear animation is still in flight
				var viewToModify = SUT.ContainerFromIndex(3) as ListViewItem;
				Assert.IsNotNull(viewToModify);
				var border = viewToModify.FindFirstDescendant<Border>(b => b.Name == "ItemBorder");
				Assert.IsNotNull(border);
				border.Width += 20;

				await WindowHelper.WaitForIdle();

				if (errorCatchGrid.Exception is { } e)
				{
					throw e;
				}

				// If all goes well, the app will not crash when the removed item finishes animating

				void InsertAnItem()
				{
					index++;
					source.Insert(0, $"Item {index}");
				}
			}
		}

		[TestMethod]
		public async Task When_FE_Item_Removed()
		{
			var item = new Border();
			var SUT = new ListView() { Items = { item } };

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(0));

			Assert.IsTrue(SUT.Items.Remove(item), "Failed to remove item from ListView.");
			Assert.IsNull(item.Parent, "The item is still attached to a parent after being removed from ListView.");
		}

		[TestMethod]
		public async Task When_Item_Removed_Selection_Stays()
		{
			var SUT = new ListView
			{
				SelectionMode = ListViewSelectionMode.Single,
				Items =
				{
					new ListViewItem { Content = "Item 1" },
					new ListViewItem { Content = "Item 2" }
				}
			};

			await UITestHelper.Load(SUT);

			SUT.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();

			SUT.Items.RemoveAt(0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, SUT.Items.Count);
			Assert.AreEqual(0, SUT.SelectedIndex);
			Assert.AreEqual("Item 2", ((ListViewItem)SUT.Items[0]).Content);
		}

		[TestMethod]
#if __WASM__ || __SKIA__
		[Ignore("Fails on WASM/Skia - https://github.com/unoplatform/uno/issues/7323")]
#endif
		public async Task When_ItemTemplate_Selector_Correct_Reuse()
		{
			var selector = new KeyedTemplateSelector<ItemColor>(o => (o as ItemColorViewModel)?.ItemType ?? ItemColor.None)
			{
				Templates =
				{
					{ItemColor.Red, RedSelectableTemplate },
					{ItemColor.Green, GreenSelectableTemplate},
					{ItemColor.Beige, BeigeSelectableTemplate}
				}
			};

			var source = new List<ItemColorViewModel>();
			int itemNo = 0;
			void AddItem(ItemColor itemType)
			{
				itemNo++;
				source.Add(new ItemColorViewModel { ItemType = itemType, ItemIndex = itemNo });
			}

			AddItem(ItemColor.Red);
			AddItem(ItemColor.Green);

			for (int i = 0; i < 30; i++)
			{
				AddItem(ItemColor.Beige);
			}

			AddItem(ItemColor.Green);

			var SUT = new ListView
			{
				Width = 180,
				Height = 320,
				ItemsSource = source,
				ItemTemplateSelector = selector,
				ItemsPanel = NoCacheItemsStackPanel,
				ItemContainerStyle = NoSpaceContainerStyle
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var redLeader = SUT.ContainerFromIndex(0) as ListViewItem;
			var greenLeader = SUT.ContainerFromIndex(1) as ListViewItem;
			var redGrid = redLeader.FindFirstDescendant<CounterGrid>();
			var greenGrid = greenLeader.FindFirstDescendant<CounterGrid>();

			var sv = SUT.FindFirstDescendant<ScrollViewer>();

			sv.ChangeView(null, 100, null, disableAnimation: true);
			await Task.Delay(20);

			var redCount1 = redGrid.LocalBindCount;
			var greenCount1 = greenGrid.LocalBindCount;

			for (int i = 100; i < 5000; i += 100)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
				await Task.Delay(50);
			}

			await Task.Delay(500);

			var redCount2 = redGrid.LocalBindCount;
			var greenCount2 = greenGrid.LocalBindCount;

			Assert.AreEqual(redCount1, redCount2); // Red template should not have been rebound
			Assert.AreEqual(greenCount1 + 1, greenCount2); // Green template should be reused once for final item
		}

		[TestMethod]
		public async Task When_ItemTemplate_Selector_And_Clear_Then_Released()
		{
			var selector = new KeyedTemplateSelector<ItemColor>(o => (o as ItemColorViewModel)?.ItemType ?? ItemColor.None)
			{
				Templates =
				{
					{ItemColor.Red, RedSelectableTemplate },
					{ItemColor.Green, GreenSelectableTemplate},
					{ItemColor.Beige, BeigeSelectableTemplate}
				}
			};

			var source = new ObservableCollection<ItemColorViewModel>();
			var refSource = new ObservableCollection<WeakReference>();
			int itemNo = 0;
			void AddItem(ItemColor itemType)
			{
				itemNo++;
				var item = new ItemColorViewModel { ItemType = itemType, ItemIndex = itemNo };
				source.Add(item);
				refSource.Add(new(item));
			}

			AddItem(ItemColor.Red);
			AddItem(ItemColor.Green);

			for (int i = 0; i < 10; i++)
			{
				AddItem(ItemColor.Beige);
			}

			AddItem(ItemColor.Green);

			var SUT = new ListView
			{
				Width = 180,
				Height = 320,
				ItemsSource = source,
				ItemTemplateSelector = selector,
				ItemsPanel = NoCacheItemsStackPanel,
				ItemContainerStyle = NoSpaceContainerStyle
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			source.RemoveAt(0);

			await WindowHelper.WaitForIdle();

			await AssertCollectedReference(refSource[0]);

			source.Clear();

			await WindowHelper.WaitForIdle();

			foreach (var itemRef in refSource)
			{
				await AssertCollectedReference(itemRef);
			}
		}

		[TestMethod]
#if __WASM__
		[Ignore] // https://github.com/unoplatform/uno/issues/7323
#endif
		public async Task When_Unequal_Size_Item_Removed()
		{
			var source = new ObservableCollection<ItemHeightViewModel>(
				Enumerable.Range(0, 20).Select(i => new ItemHeightViewModel { DisplayString = $"Item {i}", ItemHeight = 54 })
			);

			source[0].ItemHeight = 143;

			var SUT = new ListView
			{
				Height = 200,
				ItemsSource = source,
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemsPanel = NoCacheItemsStackPanel,
				ItemTemplate = BoundHeightItemTemplate
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var sv = SUT.FindFirstDescendant<ScrollViewer>();
			Assert.IsNotNull(sv);
			var panel = SUT.FindFirstDescendant<ItemsStackPanel>();
			for (int i = 100; i <= 1000; i += 100)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
#if __SKIA__ || __WASM__
				// Without invalidating, the ListView.managed items panel size remains at its original estimated size, which was overestimated based on abnormally large first item, which would result in scroll overshooting
				panel.InvalidateMeasure();
#endif
				await Task.Delay(10);
			}

			await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(source.Count - 1));

			Assert.AreEqual(969, sv.VerticalOffset, delta: 2);

			source.RemoveAt(0);

			for (int i = 1000; i >= -100; i -= 100)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
#if __SKIA__ || __WASM__
				panel.InvalidateMeasure();
#endif
				await Task.Delay(10);
			}

			var firstContainer = await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(0) as ListViewItem);
			var textBlock = firstContainer.FindFirstDescendant<TextBlock>(t => t.Name == "DisplayStringTextBlock");
			Assert.AreEqual("Item 1", textBlock.Text);

			Assert.AreEqual(0, sv.VerticalOffset);

			var listBounds = SUT.GetOnScreenBounds();
			var itemBounds = firstContainer.GetOnScreenBounds();
			Assert.AreEqual(listBounds.Y, itemBounds.Y, 2); // Top of first item should align with top of list
		}

		[TestMethod]
#if __WASM__
		[Ignore("Fails on WASM")]
#endif
		public async Task When_Unmaterialized_Item_Size_Changed()
		{
			var source = new ObservableCollection<ItemHeightViewModel>(
				Enumerable.Range(0, 20).Select(i => new ItemHeightViewModel { DisplayString = $"Item {i}", ItemHeight = 54 })
			);

			var SUT = new ListView
			{
				Height = 200,
				ItemsSource = source,
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemsPanel = NoCacheItemsStackPanel,
				ItemTemplate = BoundHeightItemTemplate
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			var sv = SUT.FindFirstDescendant<ScrollViewer>();
			Assert.IsNotNull(sv);
			var panel = SUT.FindFirstDescendant<ItemsStackPanel>();
			for (int i = 100; i <= 1000; i += 100)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
				await Task.Yield();
			}

			await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(source.Count - 1));

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(880, sv.VerticalOffset, delta: 2);

			source[0].ItemHeight = 143;

			for (int i = 1000; i >= -100; i -= 100)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
#if __SKIA__ || __WASM__
				panel.InvalidateMeasure();
#endif
				await Task.Delay(50);
				await Task.Delay(50);
				await Task.Delay(50);
			}

			var firstContainer = await WindowHelper.WaitForNonNull(() => SUT.ContainerFromIndex(0) as ListViewItem);
			var textBlock = firstContainer.FindFirstDescendant<TextBlock>(t => t.Name == "DisplayStringTextBlock");
			Assert.AreEqual("Item 0", textBlock.Text);

			Assert.AreEqual(0, sv.VerticalOffset);

			var listBounds = SUT.GetOnScreenBounds();
			var itemBounds = firstContainer.GetOnScreenBounds();
			Assert.AreEqual(listBounds.Y, itemBounds.Y); // Top of first item should align with top of list
		}

		[TestMethod]
		public async Task When_TemplateSelector_And_List_Reloaded()
		{
			var itemsSource = new ObservableCollection<SourceAwareItem>();
			var selector = new SourceAwareSelector(itemsSource, SelectableBoundTemplateA, SelectableBoundTemplateB);
			var counter = 0;

			AddItem();
			AddItem();
			AddItem();

			var list = new ListView()
			{
				Width = 200,
				Height = 300,
				ItemsSource = itemsSource,
				ItemTemplateSelector = selector
			};

			WindowHelper.WindowContent = list;

			await WindowHelper.WaitForLoaded(list);

			AddItem();
			AddItem();
			AddItem();

			RemoveItem();
			RemoveItem();
			RemoveItem();

			await WindowHelper.WaitFor(() =>
			{
				var firstContainer = (list.ContainerFromIndex(0) as ListViewItem);
				return firstContainer?.Content == itemsSource[0];
			});

			WindowHelper.WindowContent = null; // Unload list

			await Task.Delay(100);

			WindowHelper.WindowContent = list;

			await WindowHelper.WaitForLoaded(list);

			if (selector.Exception is { } ex)
			{
				throw ex;
			}

			void AddItem()
			{
				itemsSource.Add(new SourceAwareItem { No = counter });
				counter++;
			}

			void RemoveItem()
			{
				itemsSource.RemoveAt(0);
			}
		}

		[TestMethod]
		public async Task When_TemplateSelector_And_List_Reloaded_Uwp()
		{
			using var uwpStyles = StyleHelper.UseUwpStyles();
			await When_TemplateSelector_And_List_Reloaded();
		}

		[TestMethod]
		public async Task When_List_Given_More_Space()
		{

			var list = new ListView
			{
				ItemsPanel = NoCacheItemsStackPanel,
				ItemTemplate = FixedSizeItemTemplate,
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item {i}").ToArray()
			};
			var host = new Grid
			{
				Height = 100,
				Children =
				{
					list
				}
			};

			WindowHelper.WindowContent = host;
			await WindowHelper.WaitForLoaded(list);

			Assert.IsNotNull(list.ContainerFromIndex(2));
			Assert.IsNull(list.ContainerFromIndex(8));

			host.Height = 300;

			await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(8));
		}

		[TestMethod]
		public async Task When_Pool_Aware_View_In_Item_Template()
		{
			using (FeatureConfigurationHelper.UseTemplatePooling())
			{
				var initialReuseCount = ReuseCountGrid.GlobalReuseCount;
				var SUT = new ListView
				{
					ItemsSource = Enumerable.Range(1, 4).ToArray(),
					ItemsPanel = NonVirtualizingItemsPanel,
					ItemTemplate = ReuseCounterItemTemplate
				};

				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(initialReuseCount, ReuseCountGrid.GlobalReuseCount);
			}
		}

		[TestMethod]
		[Ignore("Test fails in CI when Fluent styles are used #18105")]
		public async Task When_Item_Removed_Then_DataContext_Released()
		{
			using (FeatureConfigurationHelper.UseTemplatePooling())
			{
				var collection = new ObservableCollection<TestReleaseObject>();

				var SUT = new ListView
				{
					Width = 200,
					Height = 300,
					ItemsSource = collection
				};

				var itemRef = AddItem(collection);

				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(1, SUT.Items.Count);

				var container = SUT.ContainerFromIndex(0) as ContentControl;

				Assert.AreEqual(itemRef.Target, container.Content);

				collection.Clear();

				// Ensure the container has properly been cleaned
				// up after being removed.
				Assert.IsNull(container.Content);

				Assert.AreEqual(0, SUT.Items.Count);

				await WindowHelper.WaitForIdle();

				await AssertCollectedReference(itemRef);

				static WeakReference AddItem(ObservableCollection<TestReleaseObject> collection)
				{
					var item = new TestReleaseObject();
					collection.Add(item);

					return new(item);
				}
			}
		}

		[TestMethod]
		public async Task When_Binding_and_Item_Removed()
		{
			const int ITEMS_TO_ADD = 6;
			const int INDEX_TO_DELETE = 3;
			using (FeatureConfigurationHelper.UseListViewAnimations())
			{
				var source = Enumerable.Range(0, ITEMS_TO_ADD).Select(a => new DefaultItem { Name = $"Item {a}", Value = a * a }).ToArray();

				var SUT = new ListView
				{
					Width = 200,
					Height = 300,
					ItemTemplate = DefaultItemTemplate
				};

				var model = new When_Deleting_Item_DataContext(source);
				SUT.DataContext = model;
				SUT.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath(nameof(model.Items)), Mode = BindingMode.OneWay });

				WindowHelper.WindowContent = SUT;

				await WindowHelper.WaitForLoaded(SUT);

				Assert.AreEqual(ITEMS_TO_ADD, SUT.Items.Count);

				var container = SUT.ContainerFromIndex(INDEX_TO_DELETE) as ContentControl;

				model.Items.RemoveAt(INDEX_TO_DELETE);

				// Ensure the container has properly been cleaned
				// up after being removed.
				Assert.IsNull(container.Content);

				Assert.IsNull(container.GetBindingExpression(ContentControl.ContentProperty));

				Assert.AreEqual(5, SUT.Items.Count);
			}
		}

#if __SKIA__ || __WASM__
		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		public async Task When_Focus_Tab()
		{
			var stack = new StackPanel();
			stack.Children.Add(new Button() { Content = "Before" });
			var SUT = new ListView()
			{
				TabNavigation = KeyboardNavigationMode.Local,
				Height = 400,
				ItemContainerStyle = FocusableContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};
			stack.Children.Add(SUT);
			stack.Children.Add(new Button() { Content = "After" });

			var item1 = new ListViewItem() { Content = "item 1" };
			var item2 = new ListViewItem() { Content = "item 2" };

			SUT.Items.Add(item1);
			SUT.Items.Add(item2);

			WindowHelper.WindowContent = stack;

			await WindowHelper.WaitForIdle();

			item1.Focus(FocusState.Keyboard);
			var success = FocusManager.TryMoveFocus(
				FocusNavigationDirection.Next,
				new FindNextElementOptions() { SearchRoot = TestServices.WindowHelper.XamlRoot.Content });
			Assert.IsTrue(success);

			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);
			Assert.AreEqual(item2, focused);
		}

		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		public async Task When_Focus_Shift_Tab()
		{
			var stack = new StackPanel();
			stack.Children.Add(new Button() { Content = "Before" });
			var SUT = new ListView()
			{
				TabNavigation = KeyboardNavigationMode.Local,
				Height = 400,
				ItemContainerStyle = FocusableContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};
			stack.Children.Add(SUT);
			stack.Children.Add(new Button() { Content = "After" });

			var item1 = new ListViewItem() { Content = "item 1" };
			var item2 = new ListViewItem() { Content = "item 2" };

			SUT.Items.Add(item1);
			SUT.Items.Add(item2);

			WindowHelper.WindowContent = stack;

			await WindowHelper.WaitForIdle();

			item2.Focus(FocusState.Keyboard);
			var success = FocusManager.TryMoveFocus(
				FocusNavigationDirection.Previous,
				new FindNextElementOptions() { SearchRoot = TestServices.WindowHelper.XamlRoot.Content });
			Assert.IsTrue(success);

			var focused = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot);
			Assert.AreEqual(item1, focused);
		}

		[TestMethod]
		[RequiresFullWindow]
		[RunsOnUIThread]
		public async Task When_Item_Focus_VisualState()
		{
			var SUT = new ListView()
			{
				TabNavigation = KeyboardNavigationMode.Local,
				Height = 400,
				ItemContainerStyle = FocusableContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};
			var item1 = new ListViewItem() { Content = "item 1" };
			SUT.Items.Add(item1);

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			item1.Focus(FocusState.Keyboard);
			VisualState state = null;
			await WindowHelper.WaitForNonNull(() => state = VisualStateManager.GetCurrentState(item1, "FocusStates"));

			Assert.AreEqual("Focused", state?.Name);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Incremental_Load_Default()
		{
			const int BatchSize = 25;

			// setup
			var container = new Grid { Height = 210, VerticalAlignment = VerticalAlignment.Bottom };

			var list = new ListView
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = FixedSizeItemTemplate // height=29
			};
			container.Children.Add(list);

			var source = new InfiniteSource<int>(async start =>
			{
				await Task.Delay(25);
				return Enumerable.Range(start, BatchSize).ToArray();
			});
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(list);
			await Task.Delay(1000);
			var initial = GetCurrenState();

			// scroll to bottom
			ScrollTo(list, 10000);
			await Task.Delay(500);
			await WindowHelper.WaitForIdle();
			var first = GetCurrenState();

			// scroll to bottom
			ScrollTo(list, 10000);
			await Task.Delay(500);
			await WindowHelper.WaitForIdle();
			var second = GetCurrenState();

			Assert.IsTrue(initial.Count / BatchSize > 0, $"Should start with a few batch(es) loaded: count0={initial.Count}");
			Assert.IsTrue(initial.Count + BatchSize <= first.Count, $"Should have more batch(es) loaded after first scroll: count0={initial.Count}, count1={first.Count}");
			Assert.IsTrue(initial.LastMaterialized < first.LastMaterialized, $"No extra item materialized after first scroll: index0={initial.LastMaterialized}, index1={first.LastMaterialized}");
			Assert.IsTrue(first.Count + BatchSize <= second.Count, $"Should have even more batch(es) after second scroll: count1={first.Count}, count2={second.Count}");
			Assert.IsTrue(first.LastMaterialized < second.LastMaterialized, $"No extra item materialized after second scroll: index1={first.LastMaterialized}, index2={second.LastMaterialized}");

			(int Count, int LastMaterialized) GetCurrenState() =>
			(
				source.Count,
				Enumerable.Range(0, source.Count).Reverse().FirstOrDefault(x => list.ContainerFromIndex(x) != null)
			);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Incremental_Load_ShouldStop()
		{
			const int BatchSize = 25;

			// setup
			var container = new Grid { Height = 210, VerticalAlignment = VerticalAlignment.Bottom };

			var list = new ListView
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = FixedSizeItemTemplate // height=29
			};
			container.Children.Add(list);

			var source = new InfiniteSource<int>(async start =>
			{
				await Task.Delay(25);
				return Enumerable.Range(start, BatchSize).ToArray();
			});
			list.ItemsSource = source;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(list);
			await Task.Delay(1000);
			var initial = GetCurrenState();

			// scroll to bottom
			ScrollTo(list, 10000);
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
			var firstScroll = GetCurrenState();

			// Has'No'MoreItems
			source.HasMoreItems = false;

			// scroll to bottom
			ScrollTo(list, 10000);
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
			var secondScroll = GetCurrenState();

			Assert.IsTrue(initial.Count / BatchSize > 0, $"Should start with a few batch(es) loaded: count0={initial.Count}");
			Assert.IsTrue(initial.Count + BatchSize <= firstScroll.Count, $"Should have more batch(es) loaded after first scroll: count0={initial.Count}, count1={firstScroll.Count}");
			Assert.IsTrue(initial.LastMaterialized < firstScroll.LastMaterialized, $"No extra item materialized after first scroll: index0={initial.LastMaterialized}, index={firstScroll.LastMaterialized}");
			Assert.AreEqual(firstScroll.Count, secondScroll.Count, $"Should still have same number of batches after second scroll: count1={firstScroll.Count}, count2={secondScroll.Count}");
			Assert.AreEqual(firstScroll.Count - 1, secondScroll.LastMaterialized, $"Should reach end of list from first scroll: count1={firstScroll.LastMaterialized}, index2={secondScroll.LastMaterialized}");

			(int Count, int LastMaterialized) GetCurrenState() =>
			(
				source.Count,
				Enumerable.Range(0, source.Count).Reverse().FirstOrDefault(x => list.ContainerFromIndex(x) != null)
			);
		}

#if __APPLE_UIKIT__ || __ANDROID__
		[TestMethod]
		public async Task When_Smooth_Scrolling()
		{
			// setup
			var container = new Grid { Height = 210, VerticalAlignment = VerticalAlignment.Bottom };

			var source = Enumerable.Range(0, 50).ToArray();
			var lv = new ListView
			{
				ItemsSource = source,
				ItemTemplate = FixedSizeItemTemplate, // height=29
				ItemContainerStyle = BasicContainerStyle,
			};
			container.Children.Add(lv);

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(lv);
			await Task.Delay(1000);

			// check the listview doesnt already have all items materialized
			var count = lv.NativePanel?.EnumerateChildren().Count();
			Assert.IsTrue(count < source.Length, $"Native ListView is not {(count.HasValue ? $"virtualized (count={count})" : "loaded")}.");

			// scroll to bottom
			Uno.UI.Helpers.ListViewHelper.SmoothScrollToIndex(lv, lv.Items.Count - 1);
			await Task.Delay(2000);
			await WindowHelper.WaitForIdle();

			// check if the last item is now materialized
			var materialized = lv.NativePanel.EnumerateChildren()
				.Reverse()
#if __ANDROID__
				.Select(x => (x as ListViewItem)?.Content as int?)
#elif __APPLE_UIKIT__
				.Select(x => ((x as ListViewBaseInternalContainer)?.Content as ListViewItem)?.Content as int?)
#endif
				.ToArray();
			Assert.IsTrue(materialized.Contains(source.Last()), $"Failed to scroll. materialized: {string.Join(",", materialized)}");
		}
#endif

		[TestMethod]
		public async Task When_SelectionMode_Is_Multiple()
		{
			// #11971: It was too early to apply MultiSelectStates in PrepareContainerForItemOverride,
			// as LVI doesnt have its Control::Template, which defines the visual-states, applied yet.
			var SUT = new ListView()
			{
				Height = 200,
				SelectionMode = ListViewSelectionMode.Multiple,
				ItemsSource = Enumerable.Range(3, 12).ToArray(),
			};
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// Validate the newly materialized item has MultiSelectStates set
			var lvi0 = (ListViewItem)SUT.ContainerFromIndex(0);
			var root = lvi0 is not null && VisualTreeHelper.GetChildrenCount(lvi0) > 0 ? VisualTreeHelper.GetChild(lvi0, 0) : null;
			var vsgs = root is FrameworkElement rootAsFE ? VisualStateManager.GetVisualStateGroups(rootAsFE) : null;
			var vsg = vsgs?.FirstOrDefault(x => x.Name == "MultiSelectStates");

			Assert.IsNotNull(vsg, "VisualStateGroup[Name=MultiSelectStates] was not found.");
			Assert.AreEqual("MultiSelectEnabled", vsg.CurrentState?.Name);
		}

#if HAS_UNO
		[TestMethod]
		public async Task Valid_MultipleSelectionMode_ValidSelectionStates()
		{
			const string msc = "MultiSelectCheck";
			var lvi0 = new ListViewItem
			{
				Content = "child 1"
			};
			var SUT = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple
			};
			SUT.Items.Add(lvi0);

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			// Validate the newly materialized item has MultiSelectStates set
			var root = lvi0 is not null && VisualTreeHelper.GetChildrenCount(lvi0) > 0 ? VisualTreeHelper.GetChild(lvi0, 0) : null;
			var vsgs = root is FrameworkElement rootAsFE ? VisualStateManager.GetVisualStateGroups(rootAsFE) : null;
			var vsgMultiSelectStates = vsgs?.FirstOrDefault(x => x.Name == "MultiSelectStates");
			var vsgCommonStates = vsgs?.FirstOrDefault(x => x.Name == "CommonStates");
			var mscfe = lvi0.GetTemplateChild(msc) as FrameworkElement;

			Assert.IsNotNull(mscfe, "MultiSelectCheck was not found.");
			Assert.IsNotNull(vsgMultiSelectStates, "VisualStateGroup[Name=MultiSelectStates] was not found.");
			Assert.IsNotNull(vsgCommonStates, "VisualStateGroup[Name=CommonStates] was not found.");
			Assert.AreEqual("MultiSelectEnabled", vsgMultiSelectStates.CurrentState?.Name);
			Assert.AreNotEqual("Selected", vsgCommonStates.CurrentState?.Name);
			Assert.AreEqual(0, mscfe.Opacity);
			lvi0.IsSelected = true;
			Assert.AreEqual("Selected", vsgCommonStates.CurrentState?.Name);
			Assert.AreEqual(1, mscfe.Opacity);
		}
#endif

		[TestMethod]
		[DataRow(nameof(ListView), "add")]
		[DataRow(nameof(ListView), "remove")]
		// note: ItemsControl won't reproduce the bug; we just throw that in here for sanity purpose.
		[DataRow(nameof(ItemsControl), "add")]
		[DataRow(nameof(ItemsControl), "remove")]
		public async Task When_ItemsSource_NotNotify_Changed(string sutType, string scenario)
		{
			var source = "qwe,asd,zxc".Split(',').ToList();
			var sut = sutType switch
			{
				nameof(ListView) => new ListView() { ItemsSource = source },
				nameof(ItemsControl) => new ItemsControl() { ItemsSource = source },

				_ => throw new ArgumentOutOfRangeException(nameof(sutType)),
			};

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			await WindowHelper.WaitForIdle();

			((Action)(scenario switch
			{
				"add" => () => source.Add("qwe2"),
				"remove" => () => source.RemoveAt(0),

				_ => throw new ArgumentOutOfRangeException(nameof(scenario)),
			})).Invoke();
			sut.InvalidateMeasure();
			sut.ItemsPanelRoot.InvalidateMeasure();
			await WindowHelper.WaitForIdle();

			var children =
#if __ANDROID__ || __APPLE_UIKIT__
				sut is ListView lv
					? lv.NativePanel.EnumerateChildren()
					: sut.ItemsPanelRoot.Children;
#else
				sut.ItemsPanelRoot.Children;
#endif
			var materialized = children.Count(IsVisible);
			Assert.AreEqual(3, materialized, $"ListView should still contains 3 materialized items, no more no less.");

			bool IsVisible(object x) => x is UIElement uie
				? uie.Visibility == Visibility.Visible
#if __APPLE_UIKIT__
				: !(x as UIKit.UIView)?.Hidden ?? false;
#else
				: false;
#endif
		}

		[TestMethod]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("The behaviour of virtualizing panels is only accurate for managed virtualizing panels.")]
#endif
		public async Task When_Item_Removed_From_ItemsSource_Item_Removed_From_Tree()
		{
			var source = new ObservableCollection<string>()
			{
				"1",
				"2",
				"3"
			};

			var SUT = new ListView
			{
				ItemsSource = source
			};

			await UITestHelper.Load(SUT);

			Assert.AreEqual(3, SUT.FindVisualChildByType<ItemsStackPanel>().Children.Count);
			source.RemoveAt(2);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, SUT.FindVisualChildByType<ItemsStackPanel>().Children.Count);
			source.RemoveAt(1);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(1, SUT.FindVisualChildByType<ItemsStackPanel>().Children.Count);
			source.RemoveAt(0);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, SUT.FindVisualChildByType<ItemsStackPanel>().Children.Count);
		}

		[TestMethod]
		public async Task When_ItemsSource_INCC_Reset()
		{
			// note: In order to repro #12059 (extra SelectTemplate call) & SelectTemplateCore called with incorrect item (the `source` is passed as item),
			// we need a binding that inherits source from DataContext, and not: new Binding { Source = source }, or direct assignment.
			Action<object> onSelectTemplateHook = null;

			var source = new ObservableCollection<object>(new[] { new object() });
			var sut = new ListView
			{
				ItemTemplateSelector = new LambdaDataTemplateSelector(x =>
				{
					onSelectTemplateHook?.Invoke(x);
					return TextBlockItemTemplate;
				}),
			};
			sut.DataContext = source;
			sut.SetBinding(ItemsControl.ItemsSourceProperty, new Binding());

			WindowHelper.WindowContent = sut;
			await WindowHelper.WaitForLoaded(sut);
			await WindowHelper.WaitForIdle();

			// Clearing the source SHOULD NOT cause the ItemTemplateSelector to be re-evaluated,
			// especially so when we are leaving the source empty.
			var wasSelectTemplateCalled = false;
			onSelectTemplateHook = x => wasSelectTemplateCalled = true;
			source.Clear();

			if (wasSelectTemplateCalled)
			{
				Assert.Fail("DataTemplateSelector.SelectTemplateCore is invoked during an INCC reset.");
			}
		}

		[TestMethod]
		public async Task When_Items_Have_Duplicates_ListView() => await When_Items_Have_Duplicates_Common(new ListView());

		[TestMethod]
		public async Task When_Items_Have_Duplicates_GridView() => await When_Items_Have_Duplicates_Common(new GridView());

		[TestMethod]
		public async Task When_Items_Have_Duplicates_ComboBox() => await When_Items_Have_Duplicates_Common(new ComboBox());

		[TestMethod]
		public async Task When_Items_Have_Duplicates_FlipView() => await When_Items_Have_Duplicates_Common(new FlipView());

		private async Task When_Items_Have_Duplicates_Common(Selector sut)
		{
			var items = new ObservableCollection<string>(new[]
			{
				"String 1",
				"String 1",
				"String 1",
				"String 2",
				"String 2",
				"String 2",
				"String 3",
				"String 3",
				"String 3",
				"String 1",
				"String 1",
				"String 1",
				"String 2",
				"String 2",
				"String 2",
				"String 3",
				"String 3",
				"String 3",
			});
			sut.ItemsSource = items;
			var list = new List<SelectionChangedEventArgs>();
			sut.SelectionChanged += (_, e) => list.Add(e);
			sut.SelectedIndex = 2;
			Assert.AreEqual(2, sut.SelectedIndex);
			sut.SelectedIndex = 0;
			Assert.AreEqual(0, sut.SelectedIndex);
			sut.SelectedIndex = 1;
			Assert.AreEqual(1, sut.SelectedIndex);

			Assert.AreEqual(3, list.Count);
			var removed1 = list[0].RemovedItems;
			var removed2 = list[1].RemovedItems;
			var removed3 = list[2].RemovedItems;

			var added1 = list[0].AddedItems;
			var added2 = list[1].AddedItems;
			var added3 = list[2].AddedItems;

			if (sut is FlipView)
			{
				Assert.AreEqual("String 1", (string)removed1.Single());
			}
			else
			{
				Assert.AreEqual(0, removed1.Count);
			}

			Assert.AreEqual("String 1", (string)added1.Single());

			Assert.AreEqual("String 1", (string)removed2.Single());
			Assert.AreEqual("String 1", (string)added2.Single());

			Assert.AreEqual("String 1", (string)removed3.Single());
			Assert.AreEqual("String 1", (string)added3.Single());
		}

		[TestMethod]
		public async Task When_Items_Are_Equal_But_Different_References_ListView() => await When_Items_Are_Equal_But_Different_References_Common(new ListView());

		[TestMethod]
		public async Task When_Items_Are_Equal_But_Different_References_GridView() => await When_Items_Are_Equal_But_Different_References_Common(new GridView());

		[TestMethod]
		public async Task When_Items_Are_Equal_But_Different_References_ComboBox() => await When_Items_Are_Equal_But_Different_References_Common(new ComboBox());

		[TestMethod]
		public async Task When_Items_Are_Equal_But_Different_References_FlipView() => await When_Items_Are_Equal_But_Different_References_Common(new FlipView());

		private async Task When_Items_Are_Equal_But_Different_References_Common(Selector sut)
		{
			var obj1 = new AlwaysEqualClass();
			var obj2 = new AlwaysEqualClass();
			var items = new ObservableCollection<AlwaysEqualClass>(new[]
			{
				obj1, obj2
			});
			sut.ItemsSource = items;
			var list = new List<SelectionChangedEventArgs>();
			sut.SelectionChanged += (_, e) => list.Add(e);
			sut.SelectedIndex = 1;
			Assert.AreEqual(1, sut.SelectedIndex);
			Assert.AreSame(obj2, sut.SelectedItem);
			sut.SelectedIndex = 0;
			Assert.AreEqual(0, sut.SelectedIndex);
			Assert.AreSame(obj1, sut.SelectedItem);

			Assert.AreEqual(2, list.Count);
			var removed1 = list[0].RemovedItems;
			var removed2 = list[1].RemovedItems;

			var added1 = list[0].AddedItems;
			var added2 = list[1].AddedItems;

			if (sut is FlipView)
			{
				Assert.AreSame(obj1, removed1.Single());
			}
			else
			{
				Assert.AreEqual(0, removed1.Count);
			}
			Assert.AreSame(obj2, added1.Single());

			Assert.AreSame(obj2, removed2.Single());
			Assert.AreSame(obj1, added2.Single());
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Header_DataContext()
		{
			TextBlock header = new TextBlock { Text = "empty" };
			TextBlock header2 = new TextBlock { Text = "empty" };

			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				Header = new StackPanel
				{
					Background = new SolidColorBrush(Colors.Red),
					Children = {
						header,
						header2,
					}
				}
			};

			header.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyText") });
			header2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(".") });

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
			};

			SUT.ItemsSource = source;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(header.DataContext);

			SUT.DataContext = new When_Header_DataContext_Model("test value");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.DataContext, header.DataContext);
			Assert.AreEqual("test value", header.Text);
			Assert.AreEqual(SUT.DataContext, header2.DataContext);
			Assert.AreEqual(header2.DataContext.ToString(), header2.Text);
		}

		[RunsOnUIThread]
		[TestMethod]
		public async Task When_Footer_DataContext()
		{
			TextBlock header = new TextBlock { Text = "empty" };
			TextBlock header2 = new TextBlock { Text = "empty" };

			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				Footer = new StackPanel
				{
					Background = new SolidColorBrush(Colors.Red),
					Children = {
						header,
						header2,
					}
				}
			};

			header.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyText") });
			header2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(".") });

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
			};

			SUT.ItemsSource = source;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(header.DataContext);

			SUT.DataContext = new When_Header_DataContext_Model("test value");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.DataContext, header.DataContext);
			Assert.AreEqual("test value", header.Text);
			Assert.AreEqual(SUT.DataContext, header2.DataContext);
			Assert.AreEqual(header2.DataContext.ToString(), header2.Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow("GridView")]
		[DataRow("ListView")]
		public async Task When_Header_With_HeaderTemplate_Only_One_Header_Created(string listViewBaseType)
		{
			var SUT = XamlHelper.LoadXaml<ListViewBase>($$"""
				<{{listViewBaseType}}>
					<{{listViewBaseType}}.HeaderTemplate>
						<DataTemplate>
							<TextBlock x:Name="HeaderTemplateRoot" Text="{Binding}" />
						</DataTemplate>
					</{{listViewBaseType}}.HeaderTemplate>
				</{{listViewBaseType}}>
			""");
			SUT.Header = "Header";

			await UITestHelper.Load(SUT);

			var roots = SUT.EnumerateDescendants()
				.OfType<TextBlock>()
				.Where(x => x.Name == "HeaderTemplateRoot")
				.ToArray();

			Assert.AreEqual(1, roots.Length);
			Assert.AreEqual((string)SUT.Header, roots[0].Text);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/15093")]
#endif
		// For this test to work, make sure you are running the SampleApp with LightTheme enabled.
		public async Task When_ThemeChange()
		{
			const double TotalHeight = 500; // The ListView height.
			const double ItemHeight = 50; // The ListViewItem height
			const int NumberOfItemsShownAtATime = (int)(TotalHeight / ItemHeight); // The number of ListViewItems shown at a time.
			const int NumberOfItems = 50; // The total number of items.
			var grid = new Grid() { Height = TotalHeight };
			var SUT = new ListView()
			{
				ItemsSource = Enumerable.Range(0, NumberOfItems).Select(x => $"Item {x}").ToList(),
				ItemContainerStyle = NoSpaceContainerStyle,
				ItemTemplate = new DataTemplate(() =>
				{

					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());
					var border = new Border()
					{
						Height = ItemHeight,
						Child = tb
					};

					return border;
				})
			};

			grid.AddChild(SUT);
			WindowHelper.WindowContent = grid;
			await WindowHelper.WaitForIdle();
			var exploredTextBlocks = new HashSet<TextBlock>();
			foreach (var listViewItem in GetPanelVisibleChildren(SUT))
			{
				var tb = listViewItem.FindFirstDescendant<TextBlock>();
				exploredTextBlocks.Add(tb);
				Assert.AreEqual(Colors.Black, ((SolidColorBrush)tb.Foreground).Color);
			}

			using (ThemeHelper.UseDarkTheme())
			{
				var scrollPosition = NumberOfItemsShownAtATime * ItemHeight;

				ScrollTo(SUT, scrollPosition);
				await Task.Delay(500);
				await WindowHelper.WaitForIdle();
				var seenNewTextBlock = false;
				foreach (var listViewItem in GetPanelVisibleChildren(SUT))
				{
					var tb = listViewItem.FindFirstDescendant<TextBlock>();
					seenNewTextBlock |= exploredTextBlocks.Add(tb);
					Assert.AreEqual(Colors.White, ((SolidColorBrush)tb.Foreground).Color);
				}

				Assert.IsTrue(seenNewTextBlock);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GridView_Header_Orientation()
		{
			var header = new TextBlock
			{
				Text = "0",
				VerticalAlignment = VerticalAlignment.Bottom
			};

			var SUT = new GridView
			{
				ItemsSource = "12345",
				Header = header
			};

			await UITestHelper.Load(SUT);

			var item1 = SUT.ContainerFromIndex(0).FindVisualChildByType<TextBlock>();
			Assert.AreEqual("1", item1.Text);

			header.GetAbsoluteBounds().Y.Should().BeLessThan(item1.GetAbsoluteBounds().Y);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __WASM__ || __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/234")]
#endif
		public async Task When_HeaderTemplate_DataContext()
		{
			TextBlock header = null;

			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				HeaderTemplate = new DataTemplate(() =>
				{
					var s = new StackPanel
					{
						Background = new SolidColorBrush(Colors.Red),
						Children = {
							(header = new TextBlock { Text = "empty" }),
						}
					};

					header.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyText") });

					return s;
				})
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
			};

			SUT.ItemsSource = source;
			await WindowHelper.WaitForIdle();

			Assert.IsNull(header.DataContext);

			SUT.DataContext = new When_Header_DataContext_Model("test value");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SUT.DataContext, header.DataContext);
			Assert.AreEqual("test value", header.Text);
		}
#endif

#if __APPLE_UIKIT__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HeaderDataContext_Cleared_FromNavigation()
		{
			var frame = new Frame();

			WindowHelper.WindowContent = frame;
			await WindowHelper.WaitFor(() => frame.IsLoaded);
			await WindowHelper.WaitForIdle();

			frame.Navigate(typeof(When_HeaderDataContext_Cleared_FromNavigation_Page));
			await WindowHelper.WaitForIdle();

			var page = (When_HeaderDataContext_Cleared_FromNavigation_Page)frame.Content;
			var sut = frame.FindFirstDescendant<ListView>();
			var panel = (NativeListViewBase)sut.InternalItemsPanelRoot;

			page.LvHeaderDcChanged += (s, e) => { /* for debugging */ };
			Assert.IsNotNull(page.DataContext);

			for (var i = 0; i < 3; i++) // may not always trigger, but 3 times is usually more than enough
			{
				// scroll header out of viewport and back in
				ScrollTo(sut, 100000);
				await WindowHelper.WaitForIdle();
				await Task.Delay(1000);
				ScrollTo(sut, 0);
				await WindowHelper.WaitForIdle();
				await Task.Delay(1000);

				// frame navigate away and back
				frame.Navigate(typeof(BackNavigationPage));
				await WindowHelper.WaitForIdle();
				await Task.Delay(1000);
				frame.GoBack();
				await WindowHelper.WaitForIdle();

				// check if data-context is still set
				Assert.AreEqual(GetListViewHeader()?.DataContext, page.DataContext);
			}

			UIElement GetListViewHeader() => (panel.GetSupplementaryView(NativeListViewBase.ListViewHeaderElementKindNS, global::Foundation.NSIndexPath.FromRowSection(0, 0)) as ListViewBaseInternalContainer)?.Content;
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public Task When_SelectionChanged_Item_Is_BroughtIntoView_ListView() => When_SelectionChanged_Item_Is_BroughtIntoView<ListView>();

		[TestMethod]
		[RunsOnUIThread]
		public Task When_SelectionChanged_Item_Is_BroughtIntoView_TabView() => When_SelectionChanged_Item_Is_BroughtIntoView<TabView>();

		public async Task When_SelectionChanged_Item_Is_BroughtIntoView<T>() where T : FrameworkElement, new()
		{
			var source = Enumerable.Range(0, 100)
				.Select(x => typeof(T) == typeof(TabView)
					? (object)new TabViewItem { Header = x, Content = $"Content of {x}" }
					: x)
				.ToArray();
			var setup = CreateSetup();

			await UITestHelper.Load(setup);
			await Task.Delay(1000);

			SetSelectedItem(source.ElementAt(50));
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);

			var lv = setup as ListView ?? setup.FindFirstDescendant<ListView>(); // get the LV itself, or the TabListView within TabView used for header
			var sv = lv.FindFirstDescendant<ScrollViewer>();
			var container = lv.ContainerFromIndex(50) as ContentControl;

			var offset = container.TransformToVisual(lv).TransformPoint(default);
			var (offsetStart, vpExtent) = setup is TabView
				? (offset.X, sv.ViewportWidth) // horizontal
				: (offset.Y, sv.ViewportHeight); // vertical

			Assert.IsTrue(0 <= offsetStart && offsetStart <= vpExtent, $"Container#50 should be within viewport: 0 <= {offsetStart} <= {vpExtent}");

			FrameworkElement CreateSetup() => typeof(T).Name switch
			{
				nameof(ListView) => new ListView { Width = 400, Height = 200, ItemsSource = source },
				nameof(TabView) => new TabView { Width = 400, Height = 200, TabItemsSource = source },

				_ => throw new ArgumentOutOfRangeException($"Generic arg not accepted: {typeof(T).Name}")
			};
			void SetSelectedItem(object item)
			{
				if (setup is ListView lv) { lv.SelectedItem = item; }
				else if (setup is TabView tv) { tv.SelectedItem = item; }
				else { throw new NotImplementedException(); }
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("This test is for managed ListViewBase.")]
#endif
		public async Task When_ScrollIntoView_No_Virtualization()
		{
			var source = Enumerable.Range(0, 100).ToArray();
			var lv = new ListView { Width = 400, Height = 200, ItemsSource = source };

			await UITestHelper.Load(lv);

			lv.ScrollIntoView(source[50]);
			await WindowHelper.WaitForIdle();

			var sv = lv.FindFirstDescendant<ScrollViewer>();
			var container = (ContentControl)lv.ContainerFromIndex(50);

			var offset = container.TransformToVisual(lv).TransformPoint(default);
			var (offsetStart, vpExtent) = (offset.Y, sv.ViewportHeight);

			Assert.IsTrue(0 <= offsetStart && offsetStart + container.ActualHeight <= vpExtent, $"Container#50 should be within viewport: 0 <= {offsetStart} <= {vpExtent}");
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[Ignore("https://github.com/unoplatform/uno/issues/16246")]
		public async Task When_ScrollIntoView_Containers_With_Varying_Heights()
		{
			var random = new Random(42);
			var lv = new ListView
			{
				Height = 400,
				ItemsSource = Enumerable.Range(0, 100).Select(i => $"item {i}" + new string('\n', random.Next(0, 5))).ToArray(),
				ItemTemplate = new DataTemplate(() =>
				{
					var tb = new TextBlock();
					tb.SetBinding(TextBlock.TextProperty, new Binding());

					return new StackPanel
					{
						Padding = new Thickness(16),
						Children =
						{
							tb
						}
					};
				})
			};

			await UITestHelper.Load(lv);

			var sv = lv.FindFirstDescendant<ScrollViewer>();
			var i = 7049.5;
			while (i > 0)
			{
				sv.ScrollToVerticalOffset(i);
				await WindowHelper.WaitForIdle();
				i -= 400;
				// try i -= 800, it will also fail the test even though visually, item 0 is visible, but the list view is interally corrupted
			}

			sv.ScrollToVerticalOffset(i);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(lv.ContainerFromItem(0));
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Failing on WASM: https://github.com/unoplatform/uno/issues/17742")]
#endif
		public async Task When_UpdateLayout_In_DragDropping()
		{
			var SUT = new ListView
			{
				AllowDrop = true,
				CanDragItems = true,
				CanReorderItems = true
			};

			for (var i = 0; i < 3; i++)
			{
				SUT.Items.Add(new UpdateLayoutOnUnloadedControl
				{
					Content = new TextBlock
					{
						AllowDrop = true,
						Height = 100,
						Text = i.ToString()
					}
				});
			}

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// drag(pick-up) item#0
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter() with { Y = SUT.GetAbsoluteBoundsRect().Y + 50 });
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			// drop onto item#1
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter() with { Y = SUT.GetAbsoluteBoundsRect().Y + 100 }, 1);
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter() with { Y = SUT.GetAbsoluteBoundsRect().Y + 200 }, 1);
			await Task.Delay(1000);
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var textBlocks = SUT.FindFirstDescendant<ItemsStackPanel>().Children
				.Select(c => c.FindFirstDescendant<TextBlock>())
				.OrderBy(c => c.GetAbsoluteBoundsRect().Y)
				.ToList();
			Assert.AreEqual("1", textBlocks[0].Text);
			Assert.AreEqual("0", textBlocks[1].Text);
			Assert.AreEqual("2", textBlocks[2].Text);
		}

		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR || !HAS_RENDER_TARGET_BITMAP
		[Ignore("InputInjector or RenderTargetBitmap is not supported on this platform.")]
#endif
		public async Task When_Drop_Outside_Bounds()
		{
			var SUT = new ListView
			{
				AllowDrop = true,
				CanDragItems = true,
				CanReorderItems = true,
				Width = 50,
			};

			for (var i = 0; i < 3; i++)
			{
				SUT.Items.Add(new UpdateLayoutOnUnloadedControl
				{
					Content = new TextBlock
					{
						AllowDrop = true,
						Height = 100,
						Text = i.ToString()
					}
				});
			}

			await UITestHelper.Load(SUT, x => x.IsLoaded && SUT.ContainerFromIndex(2) is { });
			await WindowHelper.WaitForIdle();

			// Make screenshot of the initial state
			var screenshotBefore = await UITestHelper.ScreenShot(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// drag(pick-up) item#0
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter() with { Y = SUT.GetAbsoluteBoundsRect().Y + 50 });
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			// drop outside of ListView bounds
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().GetCenter() with { X = SUT.GetAbsoluteBoundsRect().Right + 100 }, 1);
			await WindowHelper.WaitForIdle();
			await Task.Delay(500);
			mouse.Release();
			await WindowHelper.WaitForIdle();
			await Task.Delay(500);


			var screenshotAfter = await UITestHelper.ScreenShot(SUT);

			// When the item is dropped outside the bounds of the list, all items should return to their original state
			await ImageAssert.AreEqualAsync(screenshotBefore, screenshotAfter);
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Failing on WASM: https://github.com/unoplatform/uno/issues/17742")]
#endif
		public async Task When_UpdateLayout_In_DragDropping_2()
		{
			var SUT = new ListView
			{
				AllowDrop = true,
				CanDragItems = true,
				CanReorderItems = true,
				ItemsSource = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(x => $"{(char)('A' + x)}"))
			};
			var border = new Border
			{
				Height = 100,
				Background = new SolidColorBrush(Colors.Pink),
			};
			var setup = new StackPanel { border, SUT };

			await UITestHelper.Load(setup, x => x.IsLoaded && SUT.ContainerFromIndex(2) is { });
			await WindowHelper.WaitForIdle();

			var count = SUT.ItemsPanelRoot.Children.Count;

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var borderRect = border.GetAbsoluteBoundsRect();
			var container0Rect = (SUT.ContainerFromIndex(0) as ListViewItem ?? throw new InvalidOperationException("failed to get container 0")).GetAbsoluteBoundsRect();
			var container2Rect = (SUT.ContainerFromIndex(2) as ListViewItem ?? throw new InvalidOperationException("failed to get container 2")).GetAbsoluteBoundsRect();

			// drag(pick-up) item#2 'C'
			mouse.MoveTo(container2Rect.GetCenter());
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			// drag(move) 'C' over to position 0
			mouse.MoveTo(container0Rect.GetCenter(), 1);
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);
			var countDragMove = SUT.ItemsPanelRoot.Children.Count;

			// drag(leave) 'C' out of the ListView, onto the Border
			mouse.MoveTo(container0Rect.GetCenter(), 1);
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);
			var countDragLeave = SUT.ItemsPanelRoot.Children.Count;

			// drag(enter,move) 'C' over to position 0
			mouse.MoveTo(container0Rect.GetCenter(), 1);
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);
			var countDragEnter = SUT.ItemsPanelRoot.Children.Count;

			Assert.AreEqual(count, countDragMove, "[DragMove]: invalid number of containers");
			Assert.AreEqual(count, countDragLeave, "[DragLeave]: invalid number of containers");
			Assert.AreEqual(count, countDragEnter, "[DragEnter]: invalid number of containers");
		}
#endif

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Failing on WASM https://github.com/unoplatform/uno/issues/17742")]
#endif
		public async Task When_DragDrop_ItemsSource_Is_Subclass_Of_ObservableCollection()
		{
			var SUT = new ListView
			{
				AllowDrop = true,
				CanDragItems = true,
				CanReorderItems = true,
				ItemsSource = new SubclassOfObservableCollection
				{
					"0",
					"1",
					"2"
				}
			};

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			// drag(pick-up) item#0
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().Location + new Point(20, SUT.ActualHeight / 6));
			await WindowHelper.WaitForIdle();
			mouse.Press();
			await WindowHelper.WaitForIdle();

			// drop onto item#1
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().Location + new Point(20, SUT.ActualHeight * 2 / 6));
			await WindowHelper.WaitForIdle();
			mouse.MoveTo(SUT.GetAbsoluteBoundsRect().Location + new Point(20, SUT.ActualHeight * 3 / 6));
			await WindowHelper.WaitForIdle();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			var textBlocks = SUT.FindFirstDescendant<ItemsStackPanel>().Children
				.Select(c => c.FindFirstDescendant<TextBlock>())
				.OrderBy(c => c.GetAbsoluteBoundsRect().Y)
				.ToList();
			Assert.AreEqual("1", textBlocks[0].Text);
			Assert.AreEqual("0", textBlocks[1].Text);
			Assert.AreEqual("2", textBlocks[2].Text);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ScrollIntoView_FreshlyAddedDefaultItem() // checks against #17695
		{
			var source = new ObservableCollection<string>();
			var sut = new ListView
			{
				ItemsSource = source,
			};

			await UITestHelper.Load(sut, x => x.IsLoaded); // custom criteria to prevent empty listview failure

			void AddItem(string item, bool select = false)
			{
				source.Add(item);
				if (select)
				{
					sut.SelectedItem = item;
				}
			}

			// We just assume here that there is enough space to display 3 items.
			// Here we are testing if adding a new items and immediately selecting it
			// doesn't result in a listview with missing items in the viewport.
			AddItem($"Item 1", select: true);
			await Task.Delay(10);
			AddItem($"Item 2", select: false);
			await Task.Delay(10);
			AddItem($"Item 3", select: true);

			await UITestHelper.WaitForIdle();

			var tree = sut.TreeGraph();
#if !__ANDROID__
			var panel = sut.FindFirstDescendant<ItemsStackPanel>() ?? throw new Exception("Failed to find the ListView's Panel (ItemsStackPanel)");
			Assert.AreEqual(3, panel.Children.Count);
#else
			var count = sut.MaterializedContainers.Count();
			Assert.AreEqual(3, count);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/257")]
#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
		[Ignore("This test is only for managed scrollers.")]
#endif
		public async Task When_ListView_Unloaded_Loaded_Scroll_Position()
		{
			var SUT = new ListView
			{
				ItemsSource = Enumerable.Range(0, 20).Select(i => i.ToString()).ToList(),
				Height = 200
			};

			await UITestHelper.Load(SUT, x => x.IsLoaded); // custom criteria to prevent empty listview failure

			var sv = SUT.FindFirstDescendant<ScrollViewer>() ?? throw new Exception("Failed to find the ListView's ScrollViewer");
			sv.ScrollToVerticalOffset(300);
			await UITestHelper.WaitForIdle();

			Assert.AreEqual(300, sv.VerticalOffset);

			await UITestHelper.Load(new Button());
			await UITestHelper.Load(SUT, x => x.IsLoaded); // custom criteria to prevent empty listview failure

			Assert.AreEqual(300, sv.VerticalOffset);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__
		[Ignore("droid: Scrollable/Extent-Height doesnt get updated until manually scroll occurs, but otherwise the visuals are good.")]
#endif
		public async Task When_ScrollIntoView_FreshlyAddedOffscreenItem()
		{
			const int FixedItemHeight = 29;

			var source = new ObservableCollection<string>();
			var sut = new ListView
			{
				ItemsSource = source,
				ItemTemplate = FixedSizeItemTemplate, // height=29
			};

			await UITestHelper.Load(sut, x => x.IsLoaded); // custom criteria to prevent empty listview failure

			void AddItem(string item, bool select = false)
			{
				source.Add(item);
				if (select)
				{
					sut.SelectedItem = item;
				}
			}

			// Fill the source with enough enough items to fill the available height.
			// Rounding up to the next tens, so we always start counting from XX1 XX2 XX3 for next step
			var fulfillSize = Math.Round(sut.XamlRoot.Size.Height / FixedItemHeight / 10, MidpointRounding.ToPositiveInfinity) * 10 + 10;
			for (int i = 0; i < fulfillSize; i++)
			{
				AddItem($"Item {i + 1}", select: false);
			}

			AddItem($"Item {fulfillSize + 1}", select: true);
			await Task.Delay(10);
			AddItem($"Item {fulfillSize + 2}", select: false);
			await Task.Delay(10);
			AddItem($"Item {fulfillSize + 3}", select: true);

			await UITestHelper.WaitForIdle();

			//var tree = sut.TreeGraph();
			var sv = sut.FindFirstDescendant<ScrollViewer>() ?? throw new Exception("Failed to find the ListView's ScrollViewer");

			// Here we aren't verifying the viewport is entirely filled,
			// But the last 3 are materialized, and that we are scrolled to the end.
			Assert.IsNotNull(sut.ContainerFromIndex(source.Count - 3), "Container#n-3 is null");
			Assert.IsNotNull(sut.ContainerFromIndex(source.Count - 2), "Container#n-2 is null");
			Assert.IsNotNull(sut.ContainerFromIndex(source.Count - 1), "Container#n-1 is null");

			Assert.AreEqual(sv.ScrollableHeight, sv.VerticalOffset, "ListView is not scrolled to the end.");
		}

		[TestMethod]
		public async Task When_SelectionChanged_DuringRefresh()
		{
			var source = new ObservableCollection<string>(Enumerable.Range(0, 4).Select(x => $"Item {x}"));
			var sut = new ListView
			{
				Height = source.Count * 29 * 1.5, // give ample room
				ItemsSource = source,
				ItemTemplate = FixedSizeItemTemplate, // height=29
			};

			await UITestHelper.Load(sut, x => x.IsLoaded);

			Assert.IsTrue(Enumerable.Range(0, 4).All(x => sut.ContainerFromIndex(x) is { }), "All containers should be materialized.");

			source.Move(1, 2); // swap: 0[1]23 -> 02[1]3
			sut.SelectedItem = source[2]; // select "Item 1" (at index 2)

			await UITestHelper.WaitForIdle();

			var tree = sut.TreeGraph();
			Assert.IsTrue(Enumerable.Range(0, 4).All(x => sut.ContainerFromIndex(x) is { }), "All containers should be materialized.");

#if !(__ANDROID__ || __IOS__ || __MACOS__)
			Assert.AreEqual(4, sut.ItemsPanelRoot.Children.OfType<ListViewItem>().Count(), "There should be only 4 materialized container.");
#endif
		}
	}

	public partial class Given_ListViewBase // data class, data-context, view-model, template-selector
	{
		public class KeyedTemplateSelector : DataTemplateSelector
		{
			public IDictionary<object, DataTemplate> Templates { get; } = new Dictionary<object, DataTemplate>();

			protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item); // On UWP only this overload is called when eg Button.ContentTemplateSelector is set

			protected override DataTemplate SelectTemplateCore(object item)
			{
				if (item == null)
				{
					return null;
				}

				var template = Templates.UnoGetValueOrDefault(item);
				return template;
			}
		}

		public class KeyedTemplateSelector<T> : DataTemplateSelector
		{
			private readonly Func<object, T> _keySelector;

			public KeyedTemplateSelector(Func<object, T> keySelector = null)
			{
				this._keySelector = keySelector;
			}

			public IDictionary<T, DataTemplate> Templates { get; } = new Dictionary<T, DataTemplate>();

			protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item); // On UWP only this overload is called when eg Button.ContentTemplateSelector is set

			protected override DataTemplate SelectTemplateCore(object item)
			{
				if (item == null)
				{
					return null;
				}

				T itemT;
				if (_keySelector != null)
				{
					itemT = _keySelector(item);
				}
				else if (item is T)
				{
					itemT = (T)item;
				}
				else
				{
					return null;
				}

				var template = Templates.UnoGetValueOrDefault(itemT);
				return template;
			}
		}

		public enum ItemColor
		{
			None,
			Red,
			Green,
			Beige
		}

		public class ItemColorViewModel
		{
			public ItemColor ItemType { get; set; }
			public int ItemIndex { get; set; }
			public string DisplayString => $"Item {ItemIndex}";
		}

		public class ItemHeightViewModel : ViewModelBase
		{
			private string _displayString;
			private double _itemHeight;

			public string DisplayString
			{
				get => _displayString;
				set => SetAndRaiseIfChanged(ref _displayString, value);
			}

			public double ItemHeight
			{
				get => _itemHeight;
				set => SetAndRaiseIfChanged(ref _itemHeight, value);
			}
		}

		public class SourceAwareItem
		{
			public int No { get; set; }

			public override string ToString() => $"Item {No}";
		}

		public class SourceAwareSelector : DataTemplateSelector
		{
			private readonly IList<SourceAwareItem> _itemsSource;
			public DataTemplate _dataTemplateA;
			private readonly DataTemplate _dataTemplateB;

			public Exception Exception { get; private set; }

			public SourceAwareSelector(IList<SourceAwareItem> itemsSource, DataTemplate dataTemplateA, DataTemplate dataTemplateB)
			{
				_itemsSource = itemsSource;
				_dataTemplateA = dataTemplateA;
				_dataTemplateB = dataTemplateB;
			}

			protected override DataTemplate SelectTemplateCore(object item)
			{
				if (
#if __APPLE_UIKIT__
				// On iOS, the template selector may be invoked with a null item. This is arguably also a bug, but not presently under test here.
				item != null &&
#endif
					!_itemsSource.Contains(item)
					)
				{
					var ex = new InvalidOperationException($"Selector called for item not in source ({item})");
					Exception = Exception ?? ex;
					throw ex;
				}

				if (item is SourceAwareItem dataItem && dataItem.No > 2)
				{
					return _dataTemplateB;
				}

				else
				{
					return _dataTemplateA;
				}
			}
		}

		public class InfiniteSource<T> : ObservableCollection<T>, ISupportIncrementalLoading
		{
			public delegate Task<T[]> AsyncFetch(int start);
			public delegate T[] Fetch(int start);

			private readonly AsyncFetch _fetchAsync;
			private int _start;

			public InfiniteSource(AsyncFetch fetch)
			{
				_fetchAsync = fetch;
				_start = 0;
			}

			public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
			{
				return AsyncInfo.Run(async ct =>
				{
					var items = await _fetchAsync(_start);
					foreach (var item in items)
					{
						Add(item);
					}
					_start += items.Length;

					return new LoadMoreItemsResult { Count = count };
				});
			}

			public bool HasMoreItems { get; set; } = true;
		}

		private record TestReleaseObject()
		{
			public byte[] test { get; set; } = new byte[1024 * 1024 * 2];
		}

		private class When_Removed_From_Tree_And_Selection_TwoWay_Bound_DataContext : ViewModelBase
		{
			public string[] MyItems { get; } = new[] { "Red beans", "Rice" };

			private string _mySelection;
			public string MySelection
			{
				get => _mySelection;
				set => SetAndRaiseIfChanged(ref _mySelection, value);
			}
		}

		private class When_Selection_Events_DataContext : ViewModelBase
		{
			#region SelectedItem
			private object _selectedItem;
			public object SelectedItem
			{
				get => _selectedItem;
				set => SetAndRaiseIfChanged(ref _selectedItem, value);
			}
			#endregion
			#region SelectedValue
			private object _selectedValue;
			public object SelectedValue
			{
				get => _selectedValue;
				set => SetAndRaiseIfChanged(ref _selectedValue, value);
			}
			#endregion
			#region SelectedIndex
			private int _selectedIndex;

			public int SelectedIndex
			{
				get => _selectedIndex;
				set => SetAndRaiseIfChanged(ref _selectedIndex, value);
			}
			#endregion
		}

		private class When_DisplayMemberPath_Property_Changed_DataContext : ViewModelBase
		{
			private string _display;
			public string Display
			{
				get => _display;
				set => SetAndRaiseIfChanged(ref _display, value);
			}
		}

		private class When_Deleting_Item_DataContext : ViewModelBase
		{
			public When_Deleting_Item_DataContext(IEnumerable<DefaultItem> source)
			{
				Items = new ObservableCollection<DefaultItem>(source);
			}

			private ObservableCollection<DefaultItem> _items;
			public ObservableCollection<DefaultItem> Items
			{
				get => _items;
				set => SetAndRaiseIfChanged(ref _items, value);
			}
		}

		class DefaultItem
		{
			public string Name { get; set; }

			public int Value { get; set; }
		}

		public class LambdaDataTemplateSelector : DataTemplateSelector
		{
			private readonly Func<object, DataTemplate> _impl;

			public LambdaDataTemplateSelector(Func<object, DataTemplate> impl)
			{
				this._impl = impl;
			}

			protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
			protected override DataTemplate SelectTemplateCore(object item) => _impl(item);
		}

		public record When_Header_DataContext_Model(string MyText);

		private sealed class AlwaysEqualClass : IEquatable<AlwaysEqualClass>
		{
			public bool Equals(AlwaysEqualClass obj) => true;
			public override bool Equals(object obj) => true;
			public override int GetHashCode() => 0;
		}

#if HAS_UNO
		public partial class When_HeaderDataContext_Cleared_FromNavigation_Page : Page
		{
			public event TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> LvHeaderDcChanged;

			public When_HeaderDataContext_Cleared_FromNavigation_Page()
			{
				DataContext = "MainVM";
				Content = new Grid
				{
					RowDefinitions =
					{
						new() { Height = new GridLength(1, GridUnitType.Auto) },
						new() { Height = new GridLength(1, GridUnitType.Star) },
					},
					Children =
					{
						new Button { Content = "Next" }.Apply(x =>
						{
							Grid.SetRow(x, 0);
							x.Click += (s, e) => Frame.Navigate(typeof(BackNavigationPage));
						}),
						new ListView
						{
							ItemsSource = Enumerable.Range(0, 200).Select(x => $"asd {x}"),
							HeaderTemplate = new DataTemplate(() => new StackPanel
							{
								new TextBlock() { Text = "header" },
								new TextBlock().Apply(x => x.SetBinding(TextBlock.TextProperty, new Binding())),
							}.Apply(x => x.DataContextChanged += (s, e) => LvHeaderDcChanged?.Invoke(s, e))),
						}.Apply(x => Grid.SetRow(x, 1)),
					},
				};
			}
		}
#endif

		public partial class BackNavigationPage : Page
		{
			public BackNavigationPage()
			{
				Content = new Button().Apply(x => x.Click += (s, e) => Frame.GoBack());
			}
		}

		public partial class UpdateLayoutOnUnloadedControl : UserControl
		{
			public UpdateLayoutOnUnloadedControl()
			{
				Unloaded += (_, _) => UpdateLayout();
			}
		}
	}

	public partial class Given_ListViewBase // helpers
	{
		private static ContentControl[] GetPanelVisibleChildren(ListViewBase list)
		{
#if __ANDROID__ || __APPLE_UIKIT__
			return list
				.GetItemsPanelChildren()
				.OfType<ContentControl>()
				.ToArray();
#else
			return list.ItemsPanelRoot
				.Children
				.OfType<ContentControl>()
				.Where(c => c.Visibility == Visibility.Visible) // Managed ItemsStackPanel currently uses the dirty trick of leaving reyclable items attached to panel and collapsed
				.ToArray();
#endif
		}

		private static ContentControl[] GetAllPanelChildren(ListViewBase list)
		{
#if __ANDROID__
			return list
				.GetItemsPanelChildren()
				.OfType<ContentControl>()
				.ToArray();
#elif __APPLE_UIKIT__
			return list
				.GetItemsPanelChildren()
				.OfType<ContentControl>()
				// iOS does not seem to provide to exclude the recycled items, so we mark
				// then using IsDisplayed.
				.Where(c => c.Superview?.Superview is ListViewBaseInternalContainer container && container.IsDisplayed)
				.ToArray();
#else
			return list.ItemsPanelRoot
				.Children
				.OfType<ContentControl>()
				.Where(c => c.Visibility == Visibility.Visible) // Managed ItemsStackPanel currently uses the dirty trick of leaving reyclable items attached to panel and collapsed
				.ToArray();
#endif
		}

		private static double GetTop(FrameworkElement element, FrameworkElement container)
		{
			if (element == null)
			{
				return double.NaN;
			}
			var transform = element.TransformToVisual(container);
			return transform.TransformPoint(new Point()).Y;
		}

		private bool ApproxEquals(double value1, double value2) => Math.Abs(value1 - value2) <= 2;

		private async Task AssertCollectedReference(WeakReference reference)
		{
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(3))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();

				if (!reference.IsAlive)
				{
					return;
				}

				await Task.Delay(100);
			}

			Assert.IsFalse(reference.IsAlive);
		}

		public partial class OnItemsChangedListView : ListView
		{
			public Action ItemsChangedAction;

			protected override void OnItemsChanged(object e)
			{
				base.OnItemsChanged(e);
				ItemsChangedAction?.Invoke();
			}
		}

		public class ViewItem : List<string>
		{
			public string Name { get; set; }

			public override string ToString()
			{
				return Name;
			}
		}

		public class SubclassOfObservableCollection : ObservableCollection<string>
		{
		}
	}
}
