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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_ListViewBase
	{
		private ResourceDictionary _testsResources;

		private Style BasicContainerStyle => _testsResources["BasicListViewContainerStyle"] as Style;

		private Style ContainerMarginStyle => _testsResources["ListViewContainerMarginStyle"] as Style;

		private DataTemplate TextBlockItemTemplate => _testsResources["TextBlockItemTemplate"] as DataTemplate;

		private DataTemplate SelfHostingItemTemplate => _testsResources["SelfHostingItemTemplate"] as DataTemplate;

		private DataTemplate FixedSizeItemTemplate => _testsResources["FixedSizeItemTemplate"] as DataTemplate;

		private ItemsPanelTemplate NoCacheItemsStackPanel => _testsResources["NoCacheItemsStackPanel"] as ItemsPanelTemplate;

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
			var containerIndices = SUT.MaterializedContainers
				.Select(container => container.GetValue(ItemsControl.IndexForItemContainerProperty))
				.OfType<int>()
				.OrderBy(index => index)
				.ToArray();

			CollectionAssert.AreEqual(new int[] { 0, 1 }, containerIndices);
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

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);
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

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.AreEqual("item 1", si.Content);
			Assert.AreEqual(2, GetPanelChildren(SUT).Length);

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

		private static ContentControl[] GetPanelChildren(ListViewBase list) {
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
	}
}
