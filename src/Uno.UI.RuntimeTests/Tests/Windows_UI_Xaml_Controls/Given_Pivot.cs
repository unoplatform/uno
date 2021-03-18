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

			var tbs = pi.GetAllChildren(null, false).OfType<TextBlock>().Cast<TextBlock>();

			tbs.Should().NotBeNull();
			tbs.Should().HaveCount(1);
			items[0].Content.Should().Be(tbs.ElementAt(0).Text);

			await WindowHelper.WaitFor(() => (pi = SUT.ContainerFromItem(items[1]) as PivotItem) != null);

			var tbs2 = pi.GetAllChildren(null, false).OfType<TextBlock>().Cast<TextBlock>();

			tbs2.Should().NotBeNull();
			tbs2.Should().HaveCount(1);
			items[1].Content.Should().Be(tbs2.ElementAt(0).Text);
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
