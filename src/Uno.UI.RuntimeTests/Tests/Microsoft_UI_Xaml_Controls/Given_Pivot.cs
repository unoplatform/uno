using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using FluentAssertions;
using FluentAssertions.Execution;
using static Private.Infrastructure.TestServices;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Pivot
	{
		private TestsResources _testsResources;

		private DataTemplate PivotHeaderTemplate => _testsResources["PivotHeaderTemplate"] as DataTemplate;

		private DataTemplate PivotItemTemplate => _testsResources["PivotItemTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_Binding()
		{
			var SUT = new Pivot()
			{
				HeaderTemplate = PivotHeaderTemplate,
				ItemTemplate = PivotItemTemplate
			};

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(SUT);
			WindowHelper.WindowContent = root;

			await WindowHelper.WaitForIdle();

			var items = (root.DataContext as MyContext)?.Items;

			SUT.SetBinding(Pivot.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });

			PivotItem pi = null;
			await WindowHelper.WaitFor(() => (pi = SUT.ContainerFromItem(items[0]) as PivotItem) != null);

			// This requires two calls to WaitForIdle, even in Windows.
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			var tbs = pi.GetAllChildren(null, false).OfType<TextBlock>().Cast<TextBlock>();

			tbs.Should().NotBeNull();
			tbs.Should().HaveCount(1);
			items[0].Content.Should().Be(tbs.ElementAt(0).Text);

			await WindowHelper.WaitFor(() => (pi = SUT.ContainerFromItem(items[1]) as PivotItem) != null);

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			var tbs2 = pi.GetAllChildren(null, false).OfType<TextBlock>().Cast<TextBlock>();

			tbs2.Should().NotBeNull();

#if !__IOS__ && !__ANDROID__
			// Pivot items are materialized on demand, there should not be any text block in the second item.
			tbs2.Should().HaveCount(0);
#else
			// iOS/Android still materializes the content of the second item, even if it's not visible.
			tbs2.Should().HaveCount(1);
			items[1].Content.Should().Be(tbs2.ElementAt(0).Text);
#endif
		}

#if !WINAPPSDK // GetTemplateChild is protected in UWP while public in Uno.
		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_Changing_Header_Affects_UI()
		{
			var pivotItem = new PivotItem { Header = "Initial text" };
			var SUT = new Pivot
			{
				Items = { pivotItem },
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var pivotHeaderPanel = (PivotHeaderPanel)SUT.GetTemplateChild("StaticHeader");
			var headerItem = (PivotHeaderItem)pivotHeaderPanel.Children.Single();
			headerItem.Content.Should().Be("Initial text");
			pivotItem.Header = "New text";
			headerItem.Content.Should().Be("New text");
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_Changing_SelectedItem_Affects_SelectedIndex()
		{
			var pivotItem1 = new PivotItem { Header = "First" };
			var pivotItem2 = new PivotItem { Header = "Second" };
			var SUT = new Pivot
			{
				Items = { pivotItem1, pivotItem2 },
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			SUT.SelectedIndex.Should().Be(0);
			SUT.SelectedItem.Should().Be(pivotItem1);

			SUT.SelectedItem = pivotItem2;

			SUT.SelectedIndex.Should().Be(1);
			SUT.SelectedItem.Should().Be(pivotItem2);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Pivot_Single_ItemContent_Visible()
		{
			var items = Enumerable.Range(0, 3).ToArray();
			var setup = new Pivot
			{
				ItemsSource = items,
				SelectedItem = items.Last(),
			};
			await UITestHelper.Load(setup);

			var containers = items.Select((x, i) => setup.ContainerFromIndex(i)).OfType<PivotItem>().ToArray();

			Assert.AreEqual(3, containers.Length, "Should have 3 containers");
			Assert.AreEqual(1, containers.Count(x => x.Visibility == Visibility.Visible), "Only one PivotItem should be visible");
		}

		private class MyContext
		{
			public MyContext()
			{
				Items = new List<Item>
				{
					new Item { Title ="Pivot1", Content="ContentPivot1" },
					new Item { Title ="Pivot2", Content="ContentPivot2" },
					new Item { Title ="Pivot3", Content="ContentPivot3" },
				};
			}

			public IList<Item> Items { get; private set; }
		}
	}

	public class Item
	{
		public string Title { get; set; }
		public string Content { get; set; }
	}
}
