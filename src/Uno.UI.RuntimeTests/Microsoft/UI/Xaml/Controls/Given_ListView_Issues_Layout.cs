#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ListView_Issues_Layout
{
	private const double Tolerance = 1.0;

	public sealed class Group : ObservableCollection<string>
	{
		public Group(string key, int count)
			: base(Enumerable.Range(0, count).Select(i => $"{key}.{i}"))
		{
			Key = key;
		}

		public string Key { get; }

		public override string ToString() => Key;
	}

	public sealed class Row
	{
		public Row(int index, double height)
		{
			Index = index;
			Height = height;
		}

		public int Index { get; }

		public double Height { get; }
	}

	private sealed class CountingListView : ListView
	{
		public int MeasureCount { get; private set; }

		public int ArrangeCount { get; private set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureCount++;
			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeCount++;
			return base.ArrangeOverride(finalSize);
		}
	}

	private CollectionViewSource? _cvs;

	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static DataTemplate Template(string xaml) => (DataTemplate)XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{xaml}</DataTemplate>");

	private static ItemsPanelTemplate PanelTemplate(string panelXaml) => (ItemsPanelTemplate)XamlReader.Load(
		$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{panelXaml}</ItemsPanelTemplate>");

	private static List<string> Strings(int count) => Enumerable.Range(0, count).Select(i => $"Item {i}").ToList();

	private static ListView CreateNoCacheList(object source) => new()
	{
		Width = 300,
		Height = 400,
		ItemsSource = source,
		ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
		ItemsPanel = PanelTemplate("<ItemsStackPanel CacheLength='0' />"),
	};

	private static async Task<T> Load<T>(T element)
		where T : FrameworkElement
	{
		WindowHelper.WindowContent = element;
		await WindowHelper.WaitForLoaded(element);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		return element;
	}

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static IEnumerable<T> Descendants<T>(DependencyObject root)
		where T : DependencyObject
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T t)
			{
				yield return t;
			}

			foreach (var d in Descendants<T>(child))
			{
				yield return d;
			}
		}
	}

	private static Rect BoundsIn(FrameworkElement element, FrameworkElement relativeTo) =>
		element.TransformToVisual(relativeTo).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static async Task ChangeViewAndWait(ScrollViewer sv, double offset)
	{
		sv.ChangeView(null, offset, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(sv.VerticalOffset - offset) < Tolerance, timeoutMS: 3000, message: $"Scroll to {offset}");
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/166")]
	public async Task When_ItemsStackPanel_VerticalAlignment_Bottom_And_Few_Items_Then_Items_Hug_Bottom()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = Strings(3),
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel VerticalAlignment='Bottom' ItemsUpdatingScrollMode='KeepLastItemInView' />"),
		};
		await Load(list);

		var last = (FrameworkElement)list.ContainerFromIndex(2);
		var first = (FrameworkElement)list.ContainerFromIndex(0);
		var panel = (FrameworkElement)list.ItemsPanelRoot;
		var panelBounds = BoundsIn(panel, list);

		Assert.AreEqual(panelBounds.Bottom, BoundsIn(last, list).Bottom, Tolerance, "Last item sits at the bottom of the panel");
		Assert.IsGreaterThan(BoundsIn(first, list).Y, BoundsIn(last, list).Y);
		Assert.IsGreaterThan(list.Height / 2, BoundsIn(first, list).Y, "Items are pushed down, not top aligned");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4257")]
	public async Task When_KeepLastItemInView_And_Item_Appended_At_End_Then_Last_Item_Is_In_View()
	{
		ObservableCollection<string> items = new(Strings(30));
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = items,
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel VerticalAlignment='Bottom' ItemsUpdatingScrollMode='KeepLastItemInView' />"),
		};
		await Load(list);
		var sv = GetScrollViewer(list);
		await ChangeViewAndWait(sv, sv.ScrollableHeight);

		for (int i = 0; i < 3; i++)
		{
			items.Add($"New {i}");
			await WindowHelper.WaitForIdle();
		}

		await WindowHelper.WaitFor(() => list.ContainerFromIndex(items.Count - 1) is not null && Math.Abs(sv.VerticalOffset - sv.ScrollableHeight) < Tolerance, timeoutMS: 3000, message: "Scrolled to the newest item");

		var last = (FrameworkElement)list.ContainerFromIndex(items.Count - 1);
		var lastBounds = BoundsIn(last, list);
		Assert.AreEqual("New 2", ((ListViewItem)last).Content);
		Assert.IsLessThanOrEqualTo(list.ActualHeight + Tolerance, lastBounds.Bottom);
		Assert.IsGreaterThanOrEqualTo(-Tolerance, lastBounds.Y);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/473")]
	public async Task When_ItemsStackPanel_GroupPadding_Then_Gap_Between_Groups_And_Items_Narrowed()
	{
		Group[] groups = { new("G0", 2), new("G1", 2), new("G2", 2) };
		_cvs = new CollectionViewSource { Source = groups, IsSourceGrouped = true };
		ListView list = new()
		{
			Width = 300,
			Height = 600,
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel GroupPadding='10,0,20,40' />"),
			ItemsSource = _cvs.View,
		};
		list.GroupStyle.Add(new GroupStyle { HeaderTemplate = Template("<TextBlock Text='{Binding Key}' Height='30' />") });
		await Load(list);

		var lastOfG0 = (FrameworkElement)list.ContainerFromIndex(1);
		var firstOfG0 = (FrameworkElement)list.ContainerFromIndex(0);
		var header1 = Descendants<ListViewHeaderItem>(list).First(h => Descendants<TextBlock>(h).Any(t => t.Text == "G1"));

		double gap = BoundsIn(header1, list).Y - BoundsIn(lastOfG0, list).Bottom;
		Assert.AreEqual(40, gap, Tolerance, "GroupPadding.Bottom separates the groups");

		var panelBounds = BoundsIn((FrameworkElement)list.ItemsPanelRoot, list);
		var itemBounds = BoundsIn(firstOfG0, list);
		Assert.AreEqual(panelBounds.Width - 30, itemBounds.Width, Tolerance, "Left + right padding shrink the items");
		Assert.AreEqual(panelBounds.X + 10, itemBounds.X, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/2136")]
	public async Task When_ItemsStackPanel_Then_Containers_Stretch_To_Panel_Width()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = Strings(5),
			ItemTemplate = Template("<Border Height='40' HorizontalAlignment='Stretch'><TextBlock Text='{Binding}' VerticalAlignment='Center' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsStackPanel />"),
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
		};
		await Load(list);

		var panel = (FrameworkElement)list.ItemsPanelRoot;
		for (int i = 0; i < 5; i++)
		{
			var container = (FrameworkElement)list.ContainerFromIndex(i);
			Assert.AreEqual(panel.ActualWidth, container.ActualWidth, Tolerance, $"Container {i}");
			var padding = ((ListViewItem)container).Padding;
			Assert.AreEqual(panel.ActualWidth - padding.Left - padding.Right, BoundsIn(Descendants<Border>(container).First(b => b.Height == 40), list).Width, Tolerance, $"Border {i}");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8939")]
	public async Task When_Many_Items_Then_Vertical_ScrollBar_Is_Shown_Initially()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 300,
			ItemsSource = Strings(100),
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
		};
		await Load(list);

		var sv = GetScrollViewer(list);
		Assert.IsGreaterThan(0, sv.ScrollableHeight);
		Assert.AreEqual(Visibility.Visible, sv.ComputedVerticalScrollBarVisibility);
		var bar = Descendants<ScrollBar>(sv).First(b => b.Orientation == Orientation.Vertical);
		Assert.AreEqual(Visibility.Visible, bar.Visibility);
		Assert.IsGreaterThan(0, bar.Maximum);
		Assert.IsGreaterThan(0, bar.ViewportSize);
		Assert.IsGreaterThan(0, bar.ActualHeight);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14080")]
	public async Task When_Scrolled_Far_With_No_Cache_Then_First_Container_Stays_Realized()
	{
		var list = CreateNoCacheList(Strings(1000));
		await Load(list);
		var sv = GetScrollViewer(list);
		await ChangeViewAndWait(sv, 500 * 40);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(500) is not null, timeoutMS: 3000, message: "Middle realized");
		await WindowHelper.WaitForIdle();

		Assert.IsNotNull(list.ContainerFromIndex(0), "WinUI never recycles the first container");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14080")]
	public async Task When_Focused_Item_Scrolled_Out_Of_View_Then_Container_Stays_Realized()
	{
		var list = CreateNoCacheList(Strings(1000));
		await Load(list);
		var sv = GetScrollViewer(list);
		var focused = (ListViewItem)list.ContainerFromIndex(1);
		Assert.IsTrue(focused.Focus(FocusState.Programmatic));
		await WindowHelper.WaitForIdle();

		await ChangeViewAndWait(sv, 500 * 40);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(500) is not null, timeoutMS: 3000, message: "Middle realized");
		await WindowHelper.WaitForIdle();

		Assert.AreSame(focused, list.ContainerFromIndex(1), "A focused container is not recycled");

		await ChangeViewAndWait(sv, 0);
		Assert.AreSame(focused, list.ContainerFromIndex(1));
		Assert.AreNotEqual(FocusState.Unfocused, focused.FocusState);
	}
}
