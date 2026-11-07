using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Expected behaviours come from WinUI 2.5.1 (dxaml/lib ModernCollectionBasePanel*.cpp, ListViewBase_Partial*.cpp)
// and the public ItemsStackPanel / ItemsWrapGrid / ListViewBase docs.
[TestClass]
[RunsOnUIThread]
public class Given_ListView_Virtualization
{
	private const double Tolerance = 1.0;

	public enum Place
	{
		Above,
		In,
		Below,
	}

	private sealed class ResettableCollection : ObservableCollection<string>
	{
		public ResettableCollection(IEnumerable<string> items)
			: base(items)
		{
		}

		public void ReplaceAll(IEnumerable<string> items)
		{
			Items.Clear();
			foreach (var item in items)
			{
				Items.Add(item);
			}

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}
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
		public string Label => $"Row {Index}";
	}

	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static DataTemplate Template(string borderAttributes) => (DataTemplate)XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border {borderAttributes}><TextBlock Text='{{Binding}}' /></Border></DataTemplate>");

	private static ItemsPanelTemplate PanelTemplate(string panelXaml) => (ItemsPanelTemplate)XamlReader.Load(
		$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{panelXaml}</ItemsPanelTemplate>");

	private static List<string> Strings(int count, string prefix = "Item ") =>
		Enumerable.Range(0, count).Select(i => $"{prefix}{i}").ToList();

	// ListView: ItemsStackPanel. GridView: ItemsWrapGrid (wraps into rows, or into columns when horizontal).
	private static ListViewBase CreateList(bool grid, bool horizontal, object source, string panelAttributes = "")
	{
		ListViewBase list = grid ? new GridView() : new ListView();
		list.Width = 400;
		list.Height = 320;
		list.ItemsSource = source;
		list.ItemTemplate = Template("Width='80' Height='40'");

		string panel = grid
			? $"<ItemsWrapGrid Orientation='{(horizontal ? "Vertical" : "Horizontal")}' {panelAttributes} />"
			: $"<ItemsStackPanel Orientation='{(horizontal ? "Horizontal" : "Vertical")}' {panelAttributes} />";
		list.ItemsPanel = PanelTemplate(panel);

		if (horizontal)
		{
			ScrollViewer.SetHorizontalScrollMode(list, ScrollMode.Enabled);
			ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
			ScrollViewer.SetVerticalScrollMode(list, ScrollMode.Disabled);
			ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
		}

		return list;
	}

	private static async Task<T> Load<T>(T list)
		where T : ListViewBase
	{
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();
		return list;
	}

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static Rect Bounds(FrameworkElement element) =>
		element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static double Offset(ScrollViewer sv, bool horizontal) => horizontal ? sv.HorizontalOffset : sv.VerticalOffset;

	private static double ViewportSize(ScrollViewer sv, bool horizontal) => horizontal ? sv.ViewportWidth : sv.ViewportHeight;

	private static double Extent(ScrollViewer sv, bool horizontal) => horizontal ? sv.ExtentWidth : sv.ExtentHeight;

	private static (int FirstVisible, int LastVisible, int FirstCache, int LastCache) Indices(ListViewBase list) => list.ItemsPanelRoot switch
	{
		ItemsStackPanel p => (p.FirstVisibleIndex, p.LastVisibleIndex, p.FirstCacheIndex, p.LastCacheIndex),
		ItemsWrapGrid g => (g.FirstVisibleIndex, g.LastVisibleIndex, g.FirstCacheIndex, g.LastCacheIndex),
		_ => throw new InvalidOperationException("Unexpected items panel"),
	};

	private static object ContentAt(ListViewBase list, int index) => (list.ContainerFromIndex(index) as ContentControl)?.Content;

	// Size of one realized container along the scroll axis.
	private static double ItemSize(ListViewBase list, bool horizontal)
	{
		var container = (FrameworkElement)list.ContainerFromIndex(Indices(list).FirstVisible);
		return horizontal ? container.ActualWidth : container.ActualHeight;
	}

	// Position of a container inside the scrollable extent, along the scroll axis.
	private static double ExtentPosition(ListViewBase list, FrameworkElement container, bool horizontal)
	{
		var c = Bounds(container);
		var p = Bounds((FrameworkElement)list.ItemsPanelRoot);
		return horizontal ? c.X - p.X : c.Y - p.Y;
	}

	private static async Task ScrollTo(ListViewBase list, double offset, bool horizontal = false)
	{
		var sv = GetScrollViewer(list);
		sv.ChangeView(horizontal ? offset : null, horizontal ? null : offset, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(Offset(sv, horizontal) - offset) < Tolerance, timeoutMS: 3000, message: $"Scroll to {offset}");
		await WindowHelper.WaitForIdle();
	}

	// The cache grows over several idle ticks, so wait until the indices stop moving.
	private static async Task SettleCache(ListViewBase list)
	{
		var last = Indices(list);
		int stable = 0;
		for (int i = 0; i < 100 && stable < 4; i++)
		{
			await WindowHelper.WaitForIdle();
			var now = Indices(list);
			stable = now == last ? stable + 1 : 0;
			last = now;
		}
	}

	private static async Task ScrollIntoViewAndSettle(ListViewBase list, object item, int index, ScrollIntoViewAlignment? alignment = null)
	{
		if (alignment is { } a)
		{
			list.ScrollIntoView(item, a);
		}
		else
		{
			list.ScrollIntoView(item);
		}

		await WindowHelper.WaitFor(() => list.ContainerFromIndex(index) is not null, timeoutMS: 3000, message: $"Container {index} realized");
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
	}

	// Every container in the visible range must show the item the source holds at that index.
	private static void AssertRealizedMatchesSource(ListViewBase list, IList<string> items)
	{
		var (first, last, _, _) = Indices(list);
		Assert.IsGreaterThanOrEqualTo(0, first, "FirstVisibleIndex");

		for (int i = first; i <= last; i++)
		{
			var container = list.ContainerFromIndex(i) as ContentControl;
			Assert.IsNotNull(container, $"Container {i} should be realized");
			Assert.AreEqual(items[i], container.Content, $"Content at {i}");
			Assert.AreEqual(i, list.IndexFromContainer(container), $"IndexFromContainer at {i}");
		}
	}

	private static async Task<(ListViewBase List, ObservableCollection<string> Items, ScrollViewer Sv)> LoadScrolledToMiddle(string panelAttributes = "")
	{
		ObservableCollection<string> items = new(Strings(1000));
		var list = await Load(CreateList(false, false, items, panelAttributes));
		await ScrollIntoViewAndSettle(list, items[500], 500, ScrollIntoViewAlignment.Leading);
		await WindowHelper.WaitFor(() => Indices(list).FirstVisible == 500, timeoutMS: 3000, message: "First visible is 500");
		await SettleCache(list);
		return (list, items, GetScrollViewer(list));
	}

	private static int IndexFor(Place place, ListViewBase list)
	{
		var ix = Indices(list);
		return place switch
		{
			Place.Above => ix.FirstCache - 50,
			Place.In => ix.FirstVisible + 2,
			_ => ix.LastCache + 50,
		};
	}

	#region Realized count

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_Huge_Source_Then_Realized_Count_Is_Bounded(bool grid, bool horizontal)
	{
		var list = await Load(CreateList(grid, horizontal, Strings(100000)));

		var panel = (Panel)list.ItemsPanelRoot;
		Assert.IsGreaterThan(0, panel.Children.Count);
		Assert.IsLessThan(500, panel.Children.Count, $"Realized {panel.Children.Count} children");
		Assert.IsNotNull(list.ContainerFromIndex(0));
		Assert.IsNull(list.ContainerFromIndex(99999));
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public async Task When_Scrolled_Far_Then_Realized_Count_Stays_Bounded(bool grid, bool horizontal)
	{
		var items = Strings(100000);
		var list = await Load(CreateList(grid, horizontal, items));

		await ScrollIntoViewAndSettle(list, items[50000], 50000);
		await SettleCache(list);

		var panel = (Panel)list.ItemsPanelRoot;
		Assert.IsLessThan(500, panel.Children.Count, $"Realized {panel.Children.Count} children");
		Assert.IsNotNull(list.ContainerFromIndex(50000));
		Assert.IsNull(list.ContainerFromIndex(0));
	}

	#endregion

	#region Recycling

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Scrolling_Then_Containers_Are_Reused(bool grid)
	{
		HashSet<SelectorItem> containers = new();
		HashSet<int> indices = new();
		var list = CreateList(grid, false, Strings(5000));
		list.ContainerContentChanging += (s, e) =>
		{
			if (!e.InRecycleQueue)
			{
				containers.Add(e.ItemContainer);
				indices.Add(e.ItemIndex);
			}
		};
		await Load(list);

		var sv = GetScrollViewer(list);
		double page = sv.ViewportHeight;
		for (int i = 1; i <= 30; i++)
		{
			await ScrollTo(list, i * page);
		}

		Assert.IsGreaterThan(100, indices.Count, "Should have prepared many distinct items");
		Assert.IsLessThan(indices.Count / 2, containers.Count, $"{containers.Count} containers for {indices.Count} items");
	}

	[TestMethod]
	public async Task When_Container_Leaves_Window_Then_InRecycleQueue_Is_Raised()
	{
		HashSet<SelectorItem> prepared = new();
		List<SelectorItem> recycled = new();
		var list = CreateList(false, false, Strings(5000));
		list.ContainerContentChanging += (s, e) =>
		{
			if (e.InRecycleQueue)
			{
				recycled.Add(e.ItemContainer);
			}
			else
			{
				prepared.Add(e.ItemContainer);
			}
		};
		await Load(list);

		var sv = GetScrollViewer(list);
		for (int i = 1; i <= 15; i++)
		{
			await ScrollTo(list, i * sv.ViewportHeight);
		}

		Assert.IsNotEmpty(recycled);
		Assert.IsTrue(recycled.All(prepared.Contains), "Only containers that were prepared before can be recycled");
	}

	[TestMethod]
	public async Task When_ChoosingItemContainer_Then_Raised_With_Item_And_Index()
	{
		var items = Strings(50);
		List<(int Index, object Item)> choosing = new();
		var list = CreateList(false, false, items);
		list.ChoosingItemContainer += (s, e) => choosing.Add((e.ItemIndex, e.Item));
		await Load(list);

		Assert.IsNotEmpty(choosing);
		foreach (var (index, item) in choosing)
		{
			Assert.AreEqual(items[index], item);
		}

		Assert.IsTrue(choosing.Any(c => c.Index == 0));
	}

	[TestMethod]
	public async Task When_ChoosingItemContainer_Supplies_Container_Then_It_Is_Used()
	{
		var items = Strings(50);
		ListViewItem custom = new() { Tag = "custom" };
		var list = CreateList(false, false, items);
		list.ChoosingItemContainer += (s, e) =>
		{
			if (e.ItemIndex == 0 && e.ItemContainer is null)
			{
				e.ItemContainer = custom;
			}
		};
		await Load(list);

		Assert.AreSame(custom, list.ContainerFromIndex(0));
		Assert.AreEqual("Item 0", custom.Content);
	}

	[TestMethod]
	public async Task When_ContainerContentChanging_Then_Phases_Run_In_Order()
	{
		Dictionary<int, List<uint>> phases = new();
		var list = CreateList(false, false, Strings(50));
		void OnChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.InRecycleQueue)
			{
				return;
			}

			if (!phases.TryGetValue(args.ItemIndex, out var seen))
			{
				phases[args.ItemIndex] = seen = new();
			}

			seen.Add(args.Phase);
			if (args.Phase < 2)
			{
				args.RegisterUpdateCallback(OnChanging);
			}
		}

		list.ContainerContentChanging += OnChanging;
		await Load(list);
		await WindowHelper.WaitFor(() => phases.TryGetValue(0, out var seen) && seen.Contains(2), timeoutMS: 3000, message: "Phase 2 reached for item 0");

		CollectionAssert.AreEqual(new uint[] { 0, 1, 2 }, phases[0]);
	}

	[TestMethod]
	public async Task When_RegisterUpdateCallback_With_Phase_Then_Callback_Gets_That_Phase()
	{
		List<uint> callbackPhases = new();
		var list = CreateList(false, false, Strings(50));
		void OnCallback(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.ItemIndex == 0)
			{
				callbackPhases.Add(args.Phase);
			}
		}

		list.ContainerContentChanging += (s, e) =>
		{
			if (!e.InRecycleQueue && e.Phase == 0 && e.ItemIndex == 0)
			{
				e.RegisterUpdateCallback(5, OnCallback);
			}
		};
		await Load(list);
		await WindowHelper.WaitFor(() => callbackPhases.Count > 0, timeoutMS: 3000, message: "Callback invoked");

		CollectionAssert.AreEqual(new uint[] { 5 }, callbackPhases);
	}

	#endregion

	#region Cache and visible indices

	[TestMethod]
	public async Task When_Scrolled_Then_Visible_Indices_Follow_Offset()
	{
		var list = await Load(CreateList(false, false, Strings(1000)));
		var sv = GetScrollViewer(list);
		double size = ItemSize(list, false);

		await ScrollTo(list, 10 * size);

		var (first, last, _, _) = Indices(list);
		Assert.AreEqual(10, first);
		Assert.AreEqual((int)Math.Ceiling((10 * size + sv.ViewportHeight) / size) - 1, last);
	}

	[TestMethod]
	public async Task When_Cached_Then_Cache_Range_Contains_Visible_Range_And_Is_Realized()
	{
		var list = await Load(CreateList(false, false, Strings(1000)));
		await ScrollTo(list, 50 * ItemSize(list, false));
		await SettleCache(list);

		var (firstVisible, lastVisible, firstCache, lastCache) = Indices(list);
		Assert.IsLessThanOrEqualTo(firstVisible, firstCache);
		Assert.IsGreaterThanOrEqualTo(lastVisible, lastCache);
		Assert.IsLessThan(firstVisible, firstCache, "Default cache should extend above the viewport");
		Assert.IsGreaterThan(lastVisible, lastCache, "Default cache should extend below the viewport");

		for (int i = firstCache; i <= lastCache; i++)
		{
			Assert.IsNotNull(list.ContainerFromIndex(i), $"Cached item {i} should have a container");
		}
	}

	[TestMethod]
	public async Task When_CacheLength_Zero_Then_Cache_Matches_Visible()
	{
		// CacheLength is the total buffer, split over both sides: maxCacheBufferPerSide = CacheLength * viewport / 2.
		var list = await Load(CreateList(false, false, Strings(1000), "CacheLength='0'"));
		await ScrollTo(list, 50 * ItemSize(list, false));
		await SettleCache(list);

		var (firstVisible, lastVisible, firstCache, lastCache) = Indices(list);
		Assert.IsLessThanOrEqualTo(1, firstVisible - firstCache);
		Assert.IsLessThanOrEqualTo(1, lastCache - lastVisible);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_CacheLength_Larger_Then_More_Is_Realized(bool grid)
	{
		var small = await Load(CreateList(grid, false, Strings(5000), "CacheLength='0'"));
		await SettleCache(small);
		int smallCount = ((Panel)small.ItemsPanelRoot).Children.Count;

		var large = await Load(CreateList(grid, false, Strings(5000), "CacheLength='4'"));
		await SettleCache(large);
		int largeCount = ((Panel)large.ItemsPanelRoot).Children.Count;

		Assert.IsGreaterThan(smallCount, largeCount, $"CacheLength 4 realized {largeCount}, CacheLength 0 realized {smallCount}");
	}

	[TestMethod]
	public async Task When_CacheLength_Two_Then_Cache_Extends_One_Viewport_Per_Side()
	{
		var list = await Load(CreateList(false, false, Strings(2000), "CacheLength='2'"));
		var sv = GetScrollViewer(list);
		double size = ItemSize(list, false);
		await ScrollTo(list, 100 * size);
		await SettleCache(list);

		double viewportItems = sv.ViewportHeight / size;
		var (firstVisible, lastVisible, firstCache, lastCache) = Indices(list);
		Assert.AreEqual(viewportItems, firstVisible - firstCache, 2.5, "Items cached above");
		Assert.AreEqual(viewportItems, lastCache - lastVisible, 2.5, "Items cached below");
	}

	#endregion

	#region ScrollIntoView

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ScrollIntoView_Default_Item_Below_Then_Aligned_To_Trailing_Edge(bool horizontal)
	{
		var items = Strings(1000);
		var list = await Load(CreateList(false, horizontal, items));
		var sv = GetScrollViewer(list);

		await ScrollIntoViewAndSettle(list, items[500], 500);

		var container = (FrameworkElement)list.ContainerFromIndex(500);
		double size = horizontal ? container.ActualWidth : container.ActualHeight;
		double relativeEnd = ExtentPosition(list, container, horizontal) + size - Offset(sv, horizontal);
		Assert.AreEqual(ViewportSize(sv, horizontal), relativeEnd, Tolerance);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ScrollIntoView_Default_Item_Above_Then_Aligned_To_Leading_Edge(bool horizontal)
	{
		var items = Strings(1000);
		var list = await Load(CreateList(false, horizontal, items));
		var sv = GetScrollViewer(list);
		await ScrollIntoViewAndSettle(list, items[800], 800);

		await ScrollIntoViewAndSettle(list, items[300], 300);

		var container = (FrameworkElement)list.ContainerFromIndex(300);
		double relativeStart = ExtentPosition(list, container, horizontal) - Offset(sv, horizontal);
		Assert.AreEqual(0, relativeStart, Tolerance);
	}

	[TestMethod]
	public async Task When_ScrollIntoView_Default_Item_Visible_Then_Does_Not_Scroll()
	{
		var items = Strings(1000);
		var list = await Load(CreateList(false, false, items));
		var sv = GetScrollViewer(list);

		list.ScrollIntoView(items[2]);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ScrollIntoView_Leading_Then_Item_At_Leading_Edge(bool horizontal)
	{
		var items = Strings(1000);
		var list = await Load(CreateList(false, horizontal, items));
		var sv = GetScrollViewer(list);

		await ScrollIntoViewAndSettle(list, items[500], 500, ScrollIntoViewAlignment.Leading);

		var container = (FrameworkElement)list.ContainerFromIndex(500);
		double relativeStart = ExtentPosition(list, container, horizontal) - Offset(sv, horizontal);
		Assert.AreEqual(0, relativeStart, Tolerance);
		Assert.AreEqual(500, Indices(list).FirstVisible);
	}

	[TestMethod]
	public async Task When_ScrollIntoView_Leading_Last_Item_Then_Clamped_To_End()
	{
		var items = Strings(1000);
		var list = await Load(CreateList(false, false, items));
		var sv = GetScrollViewer(list);

		await ScrollIntoViewAndSettle(list, items[999], 999, ScrollIntoViewAlignment.Leading);

		Assert.AreEqual(sv.ScrollableHeight, sv.VerticalOffset, Tolerance);
		Assert.AreEqual(999, Indices(list).LastVisible);
	}

	[TestMethod]
	public async Task When_ScrollIntoView_Grid_Item_Then_Row_Is_Visible()
	{
		var items = Strings(5000);
		var list = await Load(CreateList(true, false, items));
		var sv = GetScrollViewer(list);

		await ScrollIntoViewAndSettle(list, items[3000], 3000);

		var container = (FrameworkElement)list.ContainerFromIndex(3000);
		double top = ExtentPosition(list, container, false) - sv.VerticalOffset;
		Assert.IsGreaterThanOrEqualTo(-Tolerance, top);
		Assert.IsLessThanOrEqualTo(sv.ViewportHeight + Tolerance, top + container.ActualHeight);
	}

	#endregion

	#region Variable item sizes

	[TestMethod]
	public async Task When_Variable_Heights_Scrolled_Down_And_Back_Then_Item_Positions_Are_Stable()
	{
		List<Row> rows = Enumerable.Range(0, 2000).Select(i => new Row(i, 30 + (i % 5) * 20)).ToList();
		var list = CreateList(false, false, rows);
		list.ItemTemplate = (DataTemplate)XamlReader.Load(
			"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Height='{Binding Height}'><TextBlock Text='{Binding Label}' /></Border></DataTemplate>");
		await Load(list);
		var sv = GetScrollViewer(list);

		// A few pages down, so estimation is already in play.
		for (int i = 1; i <= 10; i++)
		{
			await ScrollTo(list, i * 100);
		}

		var anchorIndex = Indices(list).FirstVisible + 2;
		var anchor = (FrameworkElement)list.ContainerFromIndex(anchorIndex);
		double before = ExtentPosition(list, anchor, false);
		double offsetBefore = sv.VerticalOffset;

		for (int i = 1; i <= 30; i++)
		{
			await ScrollTo(list, offsetBefore + i * 100);
		}

		for (int i = 29; i >= 0; i--)
		{
			await ScrollTo(list, offsetBefore + i * 100);
		}

		await WindowHelper.WaitFor(() => list.ContainerFromIndex(anchorIndex) is not null, timeoutMS: 3000, message: "Anchor realized again");
		anchor = (FrameworkElement)list.ContainerFromIndex(anchorIndex);
		Assert.AreEqual(before, ExtentPosition(list, anchor, false), Tolerance, "Anchor moved while scrolling back");
		Assert.AreEqual(offsetBefore, sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	public async Task When_Variable_Heights_Scrolled_Back_To_Top_Then_First_Item_At_Origin()
	{
		List<Row> rows = Enumerable.Range(0, 2000).Select(i => new Row(i, 30 + (i % 5) * 20)).ToList();
		var list = CreateList(false, false, rows);
		list.ItemTemplate = (DataTemplate)XamlReader.Load(
			"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Height='{Binding Height}'><TextBlock Text='{Binding Label}' /></Border></DataTemplate>");
		await Load(list);

		for (int i = 1; i <= 40; i++)
		{
			await ScrollTo(list, i * 150);
		}

		for (int i = 39; i >= 0; i--)
		{
			await ScrollTo(list, i * 150);
		}

		// ModernCollectionBasePanel.CorrectForEstimationErrors keeps item 0 at the origin.
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(0) is not null, timeoutMS: 3000, message: "Container 0 realized");
		var first = (FrameworkElement)list.ContainerFromIndex(0);
		Assert.AreEqual(0, ExtentPosition(list, first, false), Tolerance);
		Assert.AreEqual(0, GetScrollViewer(list).VerticalOffset, Tolerance);
	}

	#endregion

	#region ItemsUpdatingScrollMode

	[TestMethod]
	public async Task When_KeepItemsInView_And_Item_Inserted_Above_Then_Visible_Items_Do_Not_Move()
	{
		ObservableCollection<string> items = new(Strings(200));
		var list = await Load(CreateList(false, false, items));
		var sv = GetScrollViewer(list);
		double size = ItemSize(list, false);
		await ScrollTo(list, 20 * size);

		for (int i = 0; i < 5; i++)
		{
			items.Insert(0, $"New {i}");
		}

		await WindowHelper.WaitFor(() => Equals(ContentAt(list, 25), "Item 20"), timeoutMS: 3000, message: "Item 20 moved to index 25");
		await WindowHelper.WaitForIdle();

		var container = (FrameworkElement)list.ContainerFromIndex(25);
		Assert.AreEqual(0, ExtentPosition(list, container, false) - sv.VerticalOffset, Tolerance);
		Assert.AreEqual(25 * size, sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	public async Task When_KeepItemsInView_At_Top_And_Item_Inserted_Then_Stays_At_Top()
	{
		ObservableCollection<string> items = new(Strings(200));
		var list = await Load(CreateList(false, false, items));
		var sv = GetScrollViewer(list);

		items.Insert(0, "New");

		await WindowHelper.WaitFor(() => Equals(ContentAt(list, 0), "New"), timeoutMS: 3000, message: "New item realized");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	public async Task When_KeepScrollOffset_And_Item_Inserted_Above_Then_Offset_Is_Kept()
	{
		ObservableCollection<string> items = new(Strings(200));
		var list = await Load(CreateList(false, false, items, "ItemsUpdatingScrollMode='KeepScrollOffset'"));
		var sv = GetScrollViewer(list);
		double size = ItemSize(list, false);
		await ScrollTo(list, 20 * size);

		for (int i = 0; i < 5; i++)
		{
			items.Insert(0, $"New {i}");
		}

		await WindowHelper.WaitFor(() => Equals(ContentAt(list, 20), "Item 15"), timeoutMS: 3000, message: "Item 15 now at index 20");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(20 * size, sv.VerticalOffset, Tolerance);
		Assert.AreEqual(20, Indices(list).FirstVisible);
	}

	[TestMethod]
	public async Task When_KeepLastItemInView_At_End_And_Item_Added_Then_Scrolls_To_New_End()
	{
		ObservableCollection<string> items = new(Strings(100));
		var list = await Load(CreateList(false, false, items, "ItemsUpdatingScrollMode='KeepLastItemInView'"));
		var sv = GetScrollViewer(list);
		await ScrollTo(list, sv.ScrollableHeight);
		double extentBefore = sv.ExtentHeight;

		items.Add("Tail");

		await WindowHelper.WaitFor(() => sv.ExtentHeight > extentBefore && Math.Abs(sv.VerticalOffset - sv.ScrollableHeight) < Tolerance, timeoutMS: 3000, message: "Scrolled to new end");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(items.Count - 1, Indices(list).LastVisible);
	}

	[TestMethod]
	public async Task When_KeepLastItemInView_Not_At_End_And_Item_Added_Then_Offset_Is_Kept()
	{
		ObservableCollection<string> items = new(Strings(100));
		var list = await Load(CreateList(false, false, items, "ItemsUpdatingScrollMode='KeepLastItemInView'"));
		var sv = GetScrollViewer(list);
		await ScrollTo(list, 0);
		double extentBefore = sv.ExtentHeight;

		items.Add("Tail");

		await WindowHelper.WaitFor(() => sv.ExtentHeight > extentBefore, timeoutMS: 3000, message: "Extent grew");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	public async Task When_KeepLastItemInView_Initially_Loaded_Then_Starts_At_End()
	{
		// ModernCollectionBasePanel::TrackLastElement scrolls to the last element when layout is first refreshed.
		ObservableCollection<string> items = new(Strings(100));
		var list = await Load(CreateList(false, false, items, "ItemsUpdatingScrollMode='KeepLastItemInView'"));
		var sv = GetScrollViewer(list);

		await WindowHelper.WaitFor(() => Indices(list).LastVisible == items.Count - 1, timeoutMS: 3000, message: "Last item visible");
		Assert.AreEqual(sv.ScrollableHeight, sv.VerticalOffset, Tolerance);
	}

	#endregion

	#region Collection changes

	[TestMethod]
	[DataRow(Place.Above)]
	[DataRow(Place.In)]
	[DataRow(Place.Below)]
	public async Task When_Item_Inserted_Then_Containers_Match_Source(Place place)
	{
		var (list, items, sv) = await LoadScrolledToMiddle();
		double size = ItemSize(list, false);
		string top = (string)ContentAt(list, Indices(list).FirstVisible);
		double extent = sv.ExtentHeight;

		items.Insert(IndexFor(place, list), "Inserted");

		await WindowHelper.WaitFor(() => Math.Abs(sv.ExtentHeight - (extent + size)) < Tolerance, timeoutMS: 3000, message: "Extent grew by one item");
		await WindowHelper.WaitForIdle();

		AssertRealizedMatchesSource(list, items);
		var first = Indices(list).FirstVisible;
		Assert.AreEqual(top, ContentAt(list, first), "Top item changed");
		var topContainer = (FrameworkElement)list.ContainerFromIndex(first);
		Assert.AreEqual(0, ExtentPosition(list, topContainer, false) - sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	[DataRow(Place.Above)]
	[DataRow(Place.In)]
	[DataRow(Place.Below)]
	public async Task When_Item_Removed_Then_Containers_Match_Source(Place place)
	{
		var (list, items, sv) = await LoadScrolledToMiddle();
		double size = ItemSize(list, false);
		string top = (string)ContentAt(list, Indices(list).FirstVisible);
		double extent = sv.ExtentHeight;

		items.RemoveAt(IndexFor(place, list));

		await WindowHelper.WaitFor(() => Math.Abs(sv.ExtentHeight - (extent - size)) < Tolerance, timeoutMS: 3000, message: "Extent shrank by one item");
		await WindowHelper.WaitForIdle();

		AssertRealizedMatchesSource(list, items);
		var first = Indices(list).FirstVisible;
		Assert.AreEqual(top, ContentAt(list, first), "Top item changed");
		var topContainer = (FrameworkElement)list.ContainerFromIndex(first);
		Assert.AreEqual(0, ExtentPosition(list, topContainer, false) - sv.VerticalOffset, Tolerance);
	}

	[TestMethod]
	[DataRow(Place.Above)]
	[DataRow(Place.In)]
	[DataRow(Place.Below)]
	public async Task When_Item_Moved_Then_Containers_Match_Source(Place place)
	{
		var (list, items, sv) = await LoadScrolledToMiddle();
		double extent = sv.ExtentHeight;
		int from = place == Place.In ? Indices(list).FirstVisible + 1 : IndexFor(place, list);
		int to = place == Place.In ? Indices(list).FirstVisible + 3 : from + 5;
		string moved = items[from];

		items.Move(from, to);

		if (place == Place.In)
		{
			await WindowHelper.WaitFor(() => Equals(ContentAt(list, to), moved), timeoutMS: 3000, message: "Moved item shows at its new index");
		}

		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		AssertRealizedMatchesSource(list, items);
		Assert.AreEqual(extent, sv.ExtentHeight, Tolerance, "A move must not change the extent");
	}

	[TestMethod]
	public async Task When_Source_Reset_Then_Containers_Match_New_Source()
	{
		ResettableCollection items = new(Strings(1000));
		var list = await Load(CreateList(false, false, items));
		await ScrollIntoViewAndSettle(list, items[500], 500, ScrollIntoViewAlignment.Leading);

		items.ReplaceAll(Strings(1000, "Fresh "));

		await WindowHelper.WaitFor(() => list.ContainerFromIndex(Indices(list).FirstVisible) is ContentControl { Content: string s } && s.StartsWith("Fresh "), timeoutMS: 3000, message: "Fresh content realized");
		await WindowHelper.WaitForIdle();

		AssertRealizedMatchesSource(list, items);
		Assert.IsLessThan(500, ((Panel)list.ItemsPanelRoot).Children.Count);
	}

	[TestMethod]
	public async Task When_Source_Reset_To_Few_Items_Then_Scrolled_To_Top()
	{
		ResettableCollection items = new(Strings(1000));
		var list = await Load(CreateList(false, false, items));
		var sv = GetScrollViewer(list);
		await ScrollIntoViewAndSettle(list, items[500], 500, ScrollIntoViewAlignment.Leading);

		items.ReplaceAll(Strings(5, "Few "));

		await WindowHelper.WaitFor(() => Equals(ContentAt(list, 4), "Few 4"), timeoutMS: 3000, message: "Last of the few items realized");
		await WindowHelper.WaitForIdle();

		Assert.IsNull(list.ContainerFromIndex(5));
		Assert.AreEqual(0, sv.VerticalOffset, Tolerance);
		AssertRealizedMatchesSource(list, items);
	}

	[TestMethod]
	public async Task When_ItemsSource_Replaced_Then_Old_Containers_Are_Dropped()
	{
		var list = await Load(CreateList(false, false, Strings(100000)));
		var sv = GetScrollViewer(list);
		await ScrollTo(list, 20000);
		var replacement = Strings(10, "New ");

		list.ItemsSource = replacement;

		await WindowHelper.WaitFor(() => Equals(ContentAt(list, 0), "New 0"), timeoutMS: 3000, message: "New source realized");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, sv.VerticalOffset, Tolerance);
		Assert.IsNotNull(list.ContainerFromIndex(9));
		Assert.IsNull(list.ContainerFromIndex(10));
		Assert.AreEqual(10, ((Panel)list.ItemsPanelRoot).Children.Count);
		AssertRealizedMatchesSource(list, replacement);
	}

	[TestMethod]
	public async Task When_ItemsSource_Cleared_Then_No_Containers()
	{
		var list = await Load(CreateList(false, false, Strings(1000)));

		list.ItemsSource = null;
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(0) is null, timeoutMS: 3000, message: "Containers removed");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, ((Panel)list.ItemsPanelRoot).Children.Count);
		Assert.AreEqual(-1, Indices(list).FirstVisible);
	}

	#endregion

	#region ItemsWrapGrid

	private static async Task<GridView> LoadWrapGrid(string panelAttributes, bool horizontal = false, double width = 450)
	{
		var grid = (GridView)CreateList(true, horizontal, Strings(500), panelAttributes);
		grid.Width = width;
		grid.ItemTemplate = Template("Width='80' Height='40'");
		return await Load(grid);
	}

	private static Point Position(UIElement container) => new(Bounds((FrameworkElement)container).X, Bounds((FrameworkElement)container).Y);

	[TestMethod]
	public async Task When_ItemWidth_Set_Then_Items_Fill_Rows_With_That_Width()
	{
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50'");

		var c0 = (FrameworkElement)grid.ContainerFromIndex(0);
		var c1 = (FrameworkElement)grid.ContainerFromIndex(1);
		var c3 = (FrameworkElement)grid.ContainerFromIndex(3);
		Assert.AreEqual(100, c0.ActualWidth, Tolerance);
		Assert.AreEqual(50, c0.ActualHeight, Tolerance);
		Assert.AreEqual(Position(c0).X + 100, Position(c1).X, Tolerance);
		Assert.AreEqual(Position(c0).Y, Position(c3).Y, Tolerance);
	}

	[TestMethod]
	public async Task When_ItemWidth_Fits_Four_Then_Fifth_Item_Wraps_To_Next_Row()
	{
		// Width 450 leaves room for four 100px items, whether or not a classic scrollbar takes space.
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50'");

		var c0 = (FrameworkElement)grid.ContainerFromIndex(0);
		var c4 = (FrameworkElement)grid.ContainerFromIndex(4);
		Assert.AreEqual(Position(c0).X, Position(c4).X, Tolerance);
		Assert.AreEqual(Position(c0).Y + 50, Position(c4).Y, Tolerance);
	}

	[TestMethod]
	public async Task When_MaximumRowsOrColumns_Set_Then_Row_Is_Limited()
	{
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50' MaximumRowsOrColumns='2'");

		var c0 = (FrameworkElement)grid.ContainerFromIndex(0);
		var c1 = (FrameworkElement)grid.ContainerFromIndex(1);
		var c2 = (FrameworkElement)grid.ContainerFromIndex(2);
		Assert.AreEqual(Position(c0).Y, Position(c1).Y, Tolerance);
		Assert.AreEqual(Position(c0).X, Position(c2).X, Tolerance);
		Assert.AreEqual(Position(c0).Y + 50, Position(c2).Y, Tolerance);
	}

	[TestMethod]
	public async Task When_MaximumRowsOrColumns_Changed_Then_Layout_Updates()
	{
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50'");
		var panel = (ItemsWrapGrid)grid.ItemsPanelRoot;

		panel.MaximumRowsOrColumns = 3;

		await WindowHelper.WaitFor(() =>
			grid.ContainerFromIndex(3) is FrameworkElement c3 && grid.ContainerFromIndex(0) is FrameworkElement c0
			&& Math.Abs(Position(c3).X - Position(c0).X) < Tolerance, timeoutMS: 3000, message: "Fourth item wraps to the second row");
		var c2 = (FrameworkElement)grid.ContainerFromIndex(2);
		var first = (FrameworkElement)grid.ContainerFromIndex(0);
		Assert.AreEqual(Position(first).Y, Position(c2).Y, Tolerance);
	}

	[TestMethod]
	public async Task When_Vertical_MaximumRowsOrColumns_Set_Then_Column_Is_Limited()
	{
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50' MaximumRowsOrColumns='3'", horizontal: true);

		var c0 = (FrameworkElement)grid.ContainerFromIndex(0);
		var c2 = (FrameworkElement)grid.ContainerFromIndex(2);
		var c3 = (FrameworkElement)grid.ContainerFromIndex(3);
		Assert.AreEqual(Position(c0).X, Position(c2).X, Tolerance);
		Assert.AreEqual(Position(c0).Y + 100, Position(c2).Y, Tolerance);
		Assert.AreEqual(Position(c0).Y, Position(c3).Y, Tolerance);
		Assert.AreEqual(Position(c0).X + 100, Position(c3).X, Tolerance);
	}

	[TestMethod]
	public async Task When_ItemHeight_Changed_Then_Containers_Are_Resized()
	{
		var grid = await LoadWrapGrid("ItemWidth='100' ItemHeight='50'");
		var panel = (ItemsWrapGrid)grid.ItemsPanelRoot;

		panel.ItemHeight = 70;

		await WindowHelper.WaitFor(() => grid.ContainerFromIndex(0) is FrameworkElement c && Math.Abs(c.ActualHeight - 70) < Tolerance, timeoutMS: 3000, message: "Containers are 70px tall");
		var c0 = (FrameworkElement)grid.ContainerFromIndex(0);
		var c4 = (FrameworkElement)grid.ContainerFromIndex(4);
		Assert.AreEqual(Position(c0).Y + 70, Position(c4).Y, Tolerance);
	}

	#endregion
}
