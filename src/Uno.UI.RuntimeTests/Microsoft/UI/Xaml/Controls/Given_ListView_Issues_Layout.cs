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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16246")]
	public async Task When_Variable_Heights_ScrollIntoView_Then_Item_Fully_In_Viewport()
	{
		Random random = new(42);
		var heights = Enumerable.Range(0, 500).Select(_ => 20 + random.Next(0, 181)).ToList();
		var rows = heights.Select((h, i) => new Row(i, h)).ToList();
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemsSource = rows,
			ItemTemplate = Template("<Border Height='{Binding Height}'><TextBlock Text='{Binding Index}' /></Border>"),
		};
		await Load(list);
		var sv = GetScrollViewer(list);

		foreach (int index in new[] { 10, 400, 3 })
		{
			list.ScrollIntoView(rows[index]);
			await WindowHelper.WaitFor(() => list.ContainerFromIndex(index) is not null, timeoutMS: 3000, message: $"Container {index}");
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			var container = (FrameworkElement)list.ContainerFromIndex(index);
			var bounds = BoundsIn(container, list);
			var viewport = BoundsIn(sv, list);
			Assert.IsGreaterThanOrEqualTo(viewport.Y - Tolerance, bounds.Y, $"Item {index} top");
			Assert.IsLessThanOrEqualTo(viewport.Bottom + Tolerance, bounds.Bottom, $"Item {index} bottom");

			double? previousY = null;
			foreach (var c in Descendants<ListViewItem>(list.ItemsPanelRoot).OrderBy(i => list.IndexFromContainer(i)))
			{
				double y = BoundsIn(c, list).Y;
				if (previousY is { } p)
				{
					Assert.IsGreaterThanOrEqualTo(p - Tolerance, y, $"Container order after scrolling to {index}");
				}

				previousY = y;
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19062")]
	public async Task When_Grouped_Source_With_ItemsWrapGrid_Then_Items_Are_Shown()
	{
		Group[] groups = { new("A", 3), new("B", 2) };
		_cvs = new CollectionViewSource { Source = groups, IsSourceGrouped = true };
		ListView list = new()
		{
			Width = 400,
			Height = 400,
			ItemsSource = _cvs.View,
			ItemTemplate = Template("<TextBlock Text='{Binding}' HorizontalAlignment='Center' />"),
			ItemsPanel = PanelTemplate("<ItemsWrapGrid />"),
		};
		await Load(list);

		var containers = Descendants<ListViewItem>(list).ToList();
		Assert.HasCount(5, containers);
		foreach (var c in containers)
		{
			Assert.IsGreaterThan(0, c.ActualWidth);
			Assert.IsGreaterThan(0, c.ActualHeight);
			Assert.IsNotNull(Descendants<TextBlock>(c).FirstOrDefault(t => !string.IsNullOrEmpty(t.Text)));
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21463")]
	public async Task When_ItemsStackPanel_With_Narrow_Button_Items_Then_Container_Fills_Viewport()
	{
		ListView list = new()
		{
			Width = 300,
			Height = 300,
		};
		ScrollViewer.SetHorizontalScrollMode(list, ScrollMode.Auto);
		ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
		for (int i = 0; i < 3; i++)
		{
			list.Items.Add(new Button { Width = 40, Content = "Click Me" });
		}

		await Load(list);
		var sv = GetScrollViewer(list);

		for (int i = 0; i < 3; i++)
		{
			var container = (FrameworkElement)list.ContainerFromIndex(i);
			Assert.AreEqual(sv.ViewportWidth, container.ActualWidth, Tolerance, $"Container {i} fills the viewport");
			Assert.AreEqual(40, ((FrameworkElement)list.Items[i]).ActualWidth, Tolerance, $"Button {i}");
		}

		Assert.AreEqual(0, sv.ScrollableWidth, Tolerance);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21964")]
	public async Task When_GridView_With_Huge_Source_Then_Realized_Count_Stays_Small()
	{
		GridView grid = new()
		{
			Width = 800,
			Height = 600,
			ItemsSource = Enumerable.Range(0, 50000).ToList(),
			ItemTemplate = Template("<Border><TextBlock Text='{Binding}' /></Border>"),
			ItemsPanel = PanelTemplate("<ItemsWrapGrid Orientation='Horizontal' ItemWidth='100' ItemHeight='100' />"),
		};
		await Load(grid);

		int Realized() => Descendants<GridViewItem>(grid.ItemsPanelRoot).Count();
		Assert.IsGreaterThan(0, Realized());
		Assert.IsLessThan(500, Realized(), "Realized containers at the start");

		var sv = GetScrollViewer(grid);
		await ChangeViewAndWait(sv, sv.ScrollableHeight);
		await WindowHelper.WaitFor(() => grid.ContainerFromIndex(49999) is not null, timeoutMS: 5000, message: "Last item realized");
		await WindowHelper.WaitForIdle();
		Assert.IsLessThan(500, Realized(), "Realized containers at the end");
		Assert.IsNull(grid.ContainerFromIndex(0));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24110")]
	public async Task When_Items_Moved_Beyond_Realized_Range_Then_Containers_Match_Source()
	{
		ObservableCollection<string> items = new(Enumerable.Range(0, 200).Select(i => $"Item {i}"));
		ListView list = new()
		{
			Width = 300,
			Height = 300,
			ItemsSource = items,
			ItemTemplate = Template("<Border Height='100'><TextBlock Text='{Binding}' /></Border>"),
		};
		await Load(list);

		foreach (var (from, to) in new[] { (5, 150), (150, 3), (100, 190) })
		{
			items.Move(from, to);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			double previousY = double.MinValue;
			var realized = Descendants<ListViewItem>(list.ItemsPanelRoot).OrderBy(c => list.IndexFromContainer(c)).ToList();
			Assert.IsNotEmpty(realized);
			foreach (var c in realized)
			{
				int index = list.IndexFromContainer(c);
				Assert.AreEqual(items[index], c.Content, $"Content at {index} after Move({from},{to})");
				double y = BoundsIn(c, list).Y;
				Assert.IsGreaterThan(previousY, y, $"Container {index} order after Move({from},{to})");
				previousY = y;
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24332")]
	public async Task When_ScrollIntoView_From_SelectionChanged_Before_First_Layout_Then_List_Stays_Consistent()
	{
		var items = Strings(100);
		ListView list = new()
		{
			Width = 300,
			Height = 400,
			ItemTemplate = Template("<Border Height='40'><TextBlock Text='{Binding}' /></Border>"),
		};
		list.SelectionChanged += (_, _) =>
		{
			if (list.SelectedItem is { } selected)
			{
				list.ScrollIntoView(selected);
			}
		};
		list.ItemsSource = items;
		list.SelectedItem = items[60];
		await Load(list);
		var sv = GetScrollViewer(list);

		void AssertConsistent(string when)
		{
			var panelTop = BoundsIn((FrameworkElement)list.ItemsPanelRoot, list).Y;
			var realized = Descendants<ListViewItem>(list.ItemsPanelRoot).OrderBy(c => list.IndexFromContainer(c)).ToList();
			Assert.IsNotEmpty(realized, when);
			double? previousY = null;
			int? previousIndex = null;
			foreach (var c in realized)
			{
				int index = list.IndexFromContainer(c);
				double y = BoundsIn(c, list).Y;
				if (y - panelTop < -10000)
				{
					// WinUI parks the pinned first container far off-screen.
					continue;
				}

				Assert.AreEqual(items[index], c.Content, $"{when}: content at {index}");
				Assert.AreEqual(index * 40, y - panelTop, Tolerance, $"{when}: container {index} sits at its slot");
				if (previousY is { } py && previousIndex is { } pi)
				{
					Assert.AreEqual((index - pi) * 40, y - py, Tolerance, $"{when}: container {index} follows {pi}");
				}

				previousY = y;
				previousIndex = index;
			}
		}

		AssertConsistent("after load");
		if (list.ContainerFromIndex(60) is FrameworkElement selectedContainer)
		{
			var bounds = BoundsIn(selectedContainer, list);
			Assert.IsGreaterThanOrEqualTo(-Tolerance, bounds.Y);
			Assert.IsLessThanOrEqualTo(sv.ViewportHeight + Tolerance, bounds.Bottom);
		}

		await ChangeViewAndWait(sv, 0);
		await WindowHelper.WaitForIdle();
		AssertConsistent("scrolled to top");
		for (int i = 0; i < 10; i++)
		{
			Assert.IsNotNull(list.ContainerFromIndex(i), $"Container {i} is realized");
		}

		list.SelectedItem = items[59];
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		Assert.IsNotNull(list.ContainerFromIndex(59));
		AssertConsistent("after selecting the previous item");
	}
}
