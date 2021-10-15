using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ListViewPages;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ListViewBase
	{
		private ResourceDictionary _testsResources;

		private Style BasicContainerStyle => _testsResources["BasicListViewContainerStyle"] as Style;

		private Style ContainerMarginStyle => _testsResources["ListViewContainerMarginStyle"] as Style;

		private Style NoSpaceContainerStyle => _testsResources["NoExtraSpaceListViewContainerStyle"] as Style;

		private DataTemplate TextBlockItemTemplate => _testsResources["TextBlockItemTemplate"] as DataTemplate;

		private DataTemplate SelfHostingItemTemplate => _testsResources["SelfHostingItemTemplate"] as DataTemplate;

		private DataTemplate FixedSizeItemTemplate => _testsResources["FixedSizeItemTemplate"] as DataTemplate;

		private DataTemplate NV286_Template => _testsResources["NV286_Template"] as DataTemplate;

		private ItemsPanelTemplate NoCacheItemsStackPanel => _testsResources["NoCacheItemsStackPanel"] as ItemsPanelTemplate;

		private DataTemplate SelectableItemTemplateA => _testsResources["SelectableItemTemplateA"] as DataTemplate;
		private DataTemplate SelectableItemTemplateB => _testsResources["SelectableItemTemplateB"] as DataTemplate;
		private DataTemplate SelectableItemTemplateC => _testsResources["SelectableItemTemplateC"] as DataTemplate;

		private DataTemplate RedSelectableTemplate => _testsResources["RedSelectableTemplate"] as DataTemplate;
		private DataTemplate GreenSelectableTemplate => _testsResources["GreenSelectableTemplate"] as DataTemplate;
		private DataTemplate BeigeSelectableTemplate => _testsResources["BeigeSelectableTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void ValidSelectionChange()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 5;
			Assert.AreEqual(list.SelectedItem, 5);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeValidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 17;
			Assert.AreEqual(list.SelectedItem, 3);
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

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeInvalidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			source[3] = 13;
			list.SelectedItem = 17;
#if NETFX_CORE
			Assert.AreEqual(list.SelectedItem, 3);
#else
			Assert.IsNull(list.SelectedItem);
#endif
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

			var tb = si.FindFirstChild<TextBlock>();
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
#if !NETFX_CORE // On iOS and Android (others not tested), ContentTemplateRoot is null, and TemplatedRoot is a ContentPresenter containing an ImplicitTextBlock
			return;
#endif

			Assert.IsInstanceOfType(si.ContentTemplateRoot, typeof(TextBlock));
			Assert.AreEqual("Item 1", (si.ContentTemplateRoot as TextBlock).Text);
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

			await WindowHelper.WaitFor(() => GetPanelChildren(SUT).Length == 2);
			var si = SUT.ContainerFromItem(source[0]) as SelectorItem;
			Assert.AreEqual("item 1", si.Content);
			Assert.AreSame(si, source[0]);
#if !NETFX_CORE
			Assert.IsFalse(si.IsGeneratedContainer);
#endif

			var si2 = SUT.ContainerFromItem(source[1]) as ListViewItem;

			Assert.IsNotNull(si2);
			Assert.AreNotSame(si2, source[1]);
			Assert.AreEqual("item 2", si2.Content);
#if !NETFX_CORE
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

			Assert.IsNull(lvi.FindFirstChild<ListViewItem>(includeCurrent: false));
			Assert.IsNull(lvi.FindFirstParent<ListViewItem>(includeCurrent: false));
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


			Assert.AreEqual(list.SelectedIndex, 0);
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


			Assert.AreEqual(list.SelectedItems.Count, 0);
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


			Assert.AreEqual(list.SelectedIndex, -1);
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

			await WindowHelper.WaitFor(() => GetPanelChildren(SUT).Length == 2);

			var si = SUT.ContainerFromItem(source[0]) as SelectorItem;
			Assert.AreEqual("item 1", si.Content);

			source.RemoveAt(1);

			await WindowHelper.WaitFor(() => GetPanelChildren(SUT).Length == 1);

			var newTwo = new ListViewItem { Content = "item 2" };
			Assert.AreNotEqual(oldTwo, newTwo);

			source.Add(newTwo);

			await WindowHelper.WaitFor(() => GetPanelChildren(SUT).Length == 2);
			Assert.AreEqual(newTwo, GetPanelChildren(SUT).Last());
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

				var list = page.FindFirstChild<ListView>();
				Assert.IsNotNull(list);

				for (int i = 0; i < 3; i++)
				{
					ListViewItem lvi = null;
					await WindowHelper.WaitFor(() => (lvi = list.ContainerFromItem(i) as ListViewItem) != null);
					var sp = lvi.FindFirstChild<StackPanel>();
					var tb = sp?.FindFirstChild<TextBlock>();
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
		public async Task When_CollectionViewSource_In_Xaml()
		{
			var page = new ListViewCollectionViewSourcePage();

			Assert.AreEqual(0, page.SubjectListView.Items.Count);

			page.CVS.Source = new[] { "One", "Two", "Three" };

			WindowHelper.WindowContent = page;

			await WindowHelper.WaitForLoaded(page.SubjectListView);

			await WindowHelper.WaitForIdle();

#if NETFX_CORE // TODO: subscribe to changes to Source property
			Assert.AreEqual(3, page.SubjectListView.Items.Count);
#endif
			ListViewItem lvi = null;
			await WindowHelper.WaitFor(() => (lvi = page.SubjectListView.ContainerFromItem("One") as ListViewItem) != null);
		}

		private static ContentControl[] GetPanelChildren(ListViewBase list)
		{
#if __ANDROID__ || __IOS__
			return list.GetItemsPanelChildren().OfType<ContentControl>().ToArray();
#else
			return list.ItemsPanelRoot
				.Children
				.OfType<ContentControl>()
				.Where(c => c.Visibility == Visibility.Visible) // Managed ItemsStackPanel currently uses the dirty trick of leaving reyclable items attached to panel and collapsed
				.ToArray();
#endif
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

			Assert.AreEqual(null, SUT.SelectedValue);
			Assert.AreEqual(null, SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedValue = 1;

			var item1 = source.First(kvp => kvp.Key == 1);
			Assert.AreEqual(1, SUT.SelectedValue);
			Assert.AreEqual(item1, SUT.SelectedItem);
			Assert.AreEqual(1, SUT.SelectedIndex);

			// Set invalid
			SUT.SelectedValue = 4;

			Assert.AreEqual(null, SUT.SelectedValue);
			Assert.AreEqual(null, SUT.SelectedItem);
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

			Assert.AreEqual(null, SUT.SelectedValue);
			Assert.AreEqual(null, SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);

			SUT.SelectedValue = "Two";

			Assert.AreEqual("Two", SUT.SelectedValue);
			Assert.AreEqual("Two", SUT.SelectedItem);
			Assert.AreEqual(2, SUT.SelectedIndex);

			SUT.SelectedValue = "Eleventy";

			Assert.AreEqual(null, SUT.SelectedValue);
			Assert.AreEqual(null, SUT.SelectedItem);
			Assert.AreEqual(-1, SUT.SelectedIndex);
		}

		[TestMethod]
#if __IOS__
		[Ignore("Test is flaky on iOS")]
#endif
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

			ScrollBy(list, 10000); // Scroll to end

			ListViewItem lastItem = null;
			await WindowHelper.WaitFor(() => (lastItem = list.ContainerFromItem(19) as ListViewItem) != null);
			var secondLastItem = list.ContainerFromItem(18) as ListViewItem;

			await WindowHelper.WaitFor(() => GetTop(lastItem, container), 181, comparer: ApproxEquals);
			await WindowHelper.WaitFor(() => GetTop(secondLastItem, container), 152, comparer: ApproxEquals);

			source.Remove(19);

			await WindowHelper.WaitFor(() => list.Items.Count == 19);

			await WindowHelper.WaitForEqual(181, () => GetTop(list.ContainerFromItem(18) as ListViewItem, container), tolerance: 2);
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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

			using var _ = new AssertionScope();

			// Containers/indices/items can be retrieved
			Assert.AreEqual(items[1], list.ContainerFromItem(items[1]));
			Assert.AreEqual(items[2], list.ContainerFromIndex(2));
			Assert.AreEqual(3, list.IndexFromContainer(items[3]));
			Assert.AreEqual(items[1], list.ItemFromContainer(items[1]));
		}

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
				Assert.AreEqual(null, list.ContainerFromItem(removedItem));
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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

			// Item change
			var oldItem = items[1];
			var newItem = new ListViewItem();

			list.ItemsChangedAction = () =>
			{
				using var _ = new AssertionScope();

				// Test container/index/item before removed
				Assert.AreEqual(items[0], list.ContainerFromItem(items[0]));
				Assert.AreEqual(items[0], list.ContainerFromIndex(0));
				Assert.AreEqual(items[0], list.ItemFromContainer(items[0]));
				Assert.AreEqual(0, list.IndexFromContainer(items[0]));

				// Test old container/index/item
				Assert.AreEqual(null, list.ContainerFromItem(oldItem));
				Assert.AreEqual(null, list.ItemFromContainer(oldItem));
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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
				Assert.AreEqual(null, list.ContainerFromItem(items[1]));
				Assert.AreEqual(null, list.ItemFromContainer(items[1]));
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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
				Assert.AreEqual(null, list.ContainerFromItem(removedItem));

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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

				// Test old container/index/item
				Assert.AreEqual(null, list.ContainerFromItem(oldItem));
				Assert.AreEqual(null, list.ItemFromContainer(oldContainer));
				Assert.AreEqual(-1, list.IndexFromContainer(oldContainer));

#if HAS_UNO
				// Test new container/index/item
				// In UWP the container for the new item is returned, but its
				// content is not yet set.
				// We match the situation with Reset and return nulls.
				var container1 = (ListViewItem)list.ContainerFromItem(items[1]);
				Assert.IsNull(container1);
				var containerIndex1 = list.ContainerFromIndex(1);
				Assert.IsNull(containerIndex1);
#endif

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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

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
				Assert.AreEqual(null, list.ContainerFromItem(oldItem));
				Assert.AreEqual(null, list.ItemFromContainer(oldContainer));
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
			var text1 = container1.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text1);
			Assert.AreEqual(text1.Text, "Selectable A");

			var container2 = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(1) as ListViewItem);
			var text2 = container2.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text2);
			Assert.AreEqual(text2.Text, "Selectable B");

			var container3 = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(2) as ListViewItem);
			var text3 = container3.FindFirstChild<TextBlock>(tb => tb.Name == "TextBlockInTemplate");
			Assert.IsNotNull(text3);
			Assert.AreEqual(text3.Text, "Selectable C");
		}

		[TestMethod]
		public async Task When_ItemTemplateSelector_Set_And_Fluent()
		{
			using (StyleHelper.UseFluentStyles())
			{
				await When_ItemTemplateSelector_Set();
			}
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
			Assert.AreEqual(29, GetTop(container1, outer));
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
			Assert.AreEqual(null, list.SelectedItem);
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
			await WindowHelper.WaitFor(() => GetPanelChildren(list).Length == 4);

			list.SelectionChanged += (s, e) =>
			{
				Assert.AreEqual(list.SelectedItem, "Item_1");
				Assert.AreEqual(list.SelectedValue, "Item_1");
				Assert.AreEqual(model.SelectedIndex, 1);
				Assert.AreEqual(model.SelectedItem, "Item_1");
				Assert.AreEqual(model.SelectedValue, "Item_1");
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

			var tb = secondContainer.FindFirstChild<TextBlock>();
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
				var border = viewToModify.FindFirstChild<Border>(b => b.Name == "ItemBorder");
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
#if __WASM__
		[Ignore("Fails on WASM - https://github.com/unoplatform/uno/issues/7323")] 
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

			var redLeader = SUT.ContainerFromIndex(0) as ListViewItem;
			var greenLeader = SUT.ContainerFromIndex(1) as ListViewItem;
			var redGrid = redLeader.FindFirstChild<CounterGrid>();
			var greenGrid = greenLeader.FindFirstChild<CounterGrid>();

			var sv = SUT.FindFirstChild<ScrollViewer>();

			sv.ChangeView(null, 100, null, disableAnimation: true);
			await Task.Delay(20);

			var redCount1 = redGrid.LocalBindCount;
			var greenCount1 = greenGrid.LocalBindCount;

			for (int i = 300; i < 1000; i += 300)
			{
				sv.ChangeView(null, i, null, disableAnimation: true);
				await Task.Delay(20);
			}


			var redCount2 = redGrid.LocalBindCount;
			var greenCount2 = greenGrid.LocalBindCount;

			Assert.AreEqual(redCount1, redCount2); // Red template should not have been rebound
			Assert.AreEqual(greenCount1 + 1, greenCount2); // Green template should be reused once for final item
		}



		private bool ApproxEquals(double value1, double value2) => Math.Abs(value1 - value2) <= 2;

		private class When_Removed_From_Tree_And_Selection_TwoWay_Bound_DataContext : System.ComponentModel.INotifyPropertyChanged
		{
			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public string[] MyItems { get; } = new[] { "Red beans", "Rice" };

			private string _mySelection;
			public string MySelection
			{
				get => _mySelection;
				set
				{
					var changing = _mySelection != value;
					_mySelection = value;
					if (changing)
					{
						PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(nameof(MySelection)));
					}
				}
			}
		}

		private class When_Selection_Events_DataContext : global::System.ComponentModel.INotifyPropertyChanged
		{
			public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			#region SelectedItem
			private object _selectedItem;
			public object SelectedItem
			{
				get => _selectedItem;
				set => RaiseAndSetIfChanged(ref _selectedItem, value);
			}
			#endregion
			#region SelectedValue
			private object _selectedValue;
			public object SelectedValue
			{
				get => _selectedValue;
				set => RaiseAndSetIfChanged(ref _selectedValue, value);
			}
			#endregion
			#region SelectedIndex
			private int _selectedIndex;

			public int SelectedIndex
			{
				get => _selectedIndex;
				set => RaiseAndSetIfChanged(ref _selectedIndex, value);
			}
			#endregion

			protected void RaiseAndSetIfChanged<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
			{
				if (!EqualityComparer<T>.Default.Equals(backingField, value))
				{
					backingField = value;
					PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));
				}
			}
		}

		private class When_DisplayMemberPath_Property_Changed_DataContext : System.ComponentModel.INotifyPropertyChanged
		{
			private string _display;

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public string Display
			{
				get => _display;
				set
				{
					if (value != _display)
					{
						_display = value;
						PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Display)));
					}
				}
			}
		}
	}

	public partial class OnItemsChangedListView : ListView
	{
		public Action ItemsChangedAction = null;

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);
			ItemsChangedAction?.Invoke();
		}
	}

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
}
