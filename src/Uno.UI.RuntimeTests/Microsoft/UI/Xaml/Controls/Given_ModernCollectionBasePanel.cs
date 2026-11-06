using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ModernCollectionBasePanel
{
	private static DataTemplate Template(string attributes) => (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock {attributes} Text='{{Binding}}' /></DataTemplate>");

	private static (ListView List, ObservableCollection<string> Items) CreateList(int count, double height = 300)
	{
		ObservableCollection<string> items = new(Enumerable.Range(0, count).Select(i => $"Item {i}"));
		ListView list = new()
		{
			Height = height,
			Width = 200,
			ItemsSource = items,
			ItemTemplate = Template("Height='40'"),
		};

		return (list, items);
	}

	private static ItemsStackPanel GetPanel(ListView list) => (ItemsStackPanel)list.ItemsPanelRoot;

	[TestMethod]
	public async Task When_Many_Items_Then_Virtualized()
	{
		var (list, _) = CreateList(1000);
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		var panel = GetPanel(list);

		Assert.IsLessThan(200, panel.Children.Count, $"Realized {panel.Children.Count} children");
		Assert.AreEqual(0, panel.FirstVisibleIndex);
		Assert.IsGreaterThan(0, panel.LastVisibleIndex);
		Assert.IsNotNull(list.ContainerFromIndex(0));
		Assert.IsNull(list.ContainerFromIndex(999));
	}

	[TestMethod]
	public async Task When_Scrolled_To_End_Then_Last_Realized()
	{
		var (list, _) = CreateList(1000);
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		list.ScrollIntoView("Item 999");
		await WindowHelper.WaitForIdle();

		var panel = GetPanel(list);
		Assert.AreEqual(999, panel.LastVisibleIndex);
		Assert.IsNotNull(list.ContainerFromIndex(999));
		Assert.IsNull(list.ContainerFromIndex(100));
		Assert.IsLessThan(200, panel.Children.Count);
	}

	[TestMethod]
	public async Task When_Item_Inserted_Then_Container_Follows()
	{
		var (list, items) = CreateList(10);
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		items.Insert(0, "New");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("New", list.ItemFromContainer(list.ContainerFromIndex(0)));
		Assert.AreEqual("Item 0", list.ItemFromContainer(list.ContainerFromIndex(1)));
		Assert.AreEqual(1, list.IndexFromContainer(list.ContainerFromIndex(1)));
	}

	[TestMethod]
	public async Task When_Item_Removed_Then_Container_Follows()
	{
		var (list, items) = CreateList(10);
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		items.RemoveAt(0);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("Item 1", list.ItemFromContainer(list.ContainerFromIndex(0)));
	}

	[TestMethod]
	public async Task When_Source_Cleared_Then_No_Containers()
	{
		var (list, items) = CreateList(10);
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();

		items.Clear();
		await WindowHelper.WaitForIdle();

		Assert.IsNull(list.ContainerFromIndex(0));
		Assert.AreEqual(-1, GetPanel(list).FirstVisibleIndex);
	}

	[TestMethod]
	public async Task When_GridView_Then_Wraps()
	{
		GridView grid = new()
		{
			Width = 400,
			Height = 300,
			ItemsSource = Enumerable.Range(0, 500).Select(i => $"Item {i}").ToArray(),
			ItemTemplate = Template("Width='90' Height='40'"),
		};
		WindowHelper.WindowContent = grid;
		await WindowHelper.WaitForLoaded(grid);
		await WindowHelper.WaitForIdle();

		var panel = (ItemsWrapGrid)grid.ItemsPanelRoot;
		Assert.IsLessThan(300, panel.Children.Count);

		var first = (UIElement)grid.ContainerFromIndex(0);
		var second = (UIElement)grid.ContainerFromIndex(1);
		Assert.AreEqual(first.ActualOffset.Y, second.ActualOffset.Y);
		Assert.IsGreaterThan(first.ActualOffset.X, second.ActualOffset.X);
	}
}
