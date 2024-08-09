using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using FluentAssertions;
using FluentAssertions.Execution;
using static Private.Infrastructure.TestServices;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;

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

			// For some reason, the count is 0 in Windows. So this doesn't currently match Windows.
			tbs2.Should().HaveCount(1);
			items[1].Content.Should().Be(tbs2.ElementAt(0).Text);
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
