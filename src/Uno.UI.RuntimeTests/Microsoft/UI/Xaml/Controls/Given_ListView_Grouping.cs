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

// Expected behaviours come from WinUI 2.5.1 (dxaml/lib ModernCollectionBasePanel*.cpp, ModernCollectionBasePanel_StickyHeaders.cpp,
// ListViewBase_Partial*.cpp) and the public GroupStyle / ItemsStackPanel / ItemsWrapGrid docs.
[TestClass]
[RunsOnUIThread]
public class Given_ListView_Grouping
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

	private sealed class ParityTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Even { get; set; }
		public DataTemplate Odd { get; set; }

		protected override DataTemplate SelectTemplateCore(object item) =>
			int.Parse(((Group)item).Key.Substring(1)) % 2 == 0 ? Even : Odd;

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
	}

	// Keeps the view source alive for the lifetime of the test.
	private CollectionViewSource _cvs;

	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static DataTemplate Template(string xaml) => (DataTemplate)XamlReader.Load(
		$"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{xaml}</DataTemplate>");

	private static ObservableCollection<Group> CreateGroups(int groupCount, int itemsPerGroup) =>
		new(Enumerable.Range(0, groupCount).Select(i => new Group($"G{i}", itemsPerGroup)));

	private static List<string> Flatten(IEnumerable<Group> groups) => groups.SelectMany(g => g).ToList();

	private ListViewBase CreateList(
		IList<Group> groups,
		bool grid = false,
		string panelXaml = null,
		bool hidesIfEmpty = false,
		DataTemplate headerTemplate = null,
		DataTemplateSelector headerSelector = null)
	{
		ListViewBase list = grid ? new GridView() : new ListView();
		list.Width = 450;
		list.Height = 320;
		list.ItemTemplate = Template("<Border Width='80' Height='40'><TextBlock Text='{Binding}' /></Border>");
		panelXaml ??= grid ? "<ItemsWrapGrid Orientation='Horizontal' ItemWidth='100' ItemHeight='50' />" : "<ItemsStackPanel />";
		list.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
			$"<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{panelXaml}</ItemsPanelTemplate>");

		_cvs = new CollectionViewSource { Source = groups, IsSourceGrouped = true };
		list.ItemsSource = _cvs.View;

		GroupStyle style = new() { HidesIfEmpty = hidesIfEmpty };
		if (headerSelector is not null)
		{
			style.HeaderTemplateSelector = headerSelector;
		}
		else
		{
			style.HeaderTemplate = headerTemplate ?? Template("<TextBlock Text='{Binding Key}' Height='40' />");
		}

		list.GroupStyle.Add(style);
		return list;
	}

	private sealed class FixedGroupStyleSelector : GroupStyleSelector
	{
		public GroupStyle Style { get; init; }

		protected override GroupStyle SelectGroupStyleCore(object group, uint level) => Style;
	}

	private static FixedGroupStyleSelector SelectorWith(string headerText, bool hidesIfEmpty = false) => new()
	{
		Style = new GroupStyle
		{
			HidesIfEmpty = hidesIfEmpty,
			HeaderTemplate = Template($"<TextBlock Text='{headerText}' Height='40' />"),
		},
	};

	[TestMethod]
	public async Task When_GroupStyleSelector_Swapped_After_Load_Then_Headers_Use_New_Style()
	{
		var list = CreateList(CreateGroups(3, 2));
		list.GroupStyle.Clear();
		list.GroupStyleSelector = SelectorWith("First");
		await Load(list);
		Assert.IsNotNull(FindFirst<TextBlock>(Headers(list)[0], t => t.Text == "First"));

		list.GroupStyleSelector = SelectorWith("Second");
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, Headers(list).Count);
		foreach (var header in Headers(list))
		{
			Assert.IsNotNull(FindFirst<TextBlock>(header, t => t.Text == "Second"));
		}
	}

	[TestMethod]
	public async Task When_GroupStyleSelector_Swapped_After_Load_Then_HidesIfEmpty_Follows()
	{
		var groups = CreateGroups(3, 2);
		groups[1].Clear();
		var list = CreateList(groups);
		list.GroupStyle.Clear();
		list.GroupStyleSelector = SelectorWith("Header", hidesIfEmpty: false);
		await Load(list);
		Assert.AreEqual(3, Headers(list).Count);

		list.GroupStyleSelector = SelectorWith("Header", hidesIfEmpty: true);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, Headers(list).Count);
	}

	private static async Task<T> Load<T>(T list)
		where T : ListViewBase
	{
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		return list;
	}

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static Rect Bounds(FrameworkElement element) =>
		element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	// The panel keeps recycled headers as its children, so only the realized ones count.
	private static List<ListViewBaseHeaderItem> Headers(ListViewBase list) =>
		((Panel)list.ItemsPanelRoot).Children.OfType<ListViewBaseHeaderItem>().Where(h => h.GetVirtualizationInformationOrNull() is { IsRealized: true }).OrderBy(h => Bounds(h).Y).ThenBy(h => Bounds(h).X).ToList();

	private static List<string> HeaderKeys(ListViewBase list) =>
		Headers(list).Select(h => ((Group)h.Content).Key).OrderBy(k => int.Parse(k.Substring(1))).ToList();

	private static ListViewBaseHeaderItem HeaderOf(ListViewBase list, string key) =>
		Headers(list).Single(h => ((Group)h.Content).Key == key);

	private static FrameworkElement Item(ListViewBase list, int flatIndex) => (FrameworkElement)list.ContainerFromIndex(flatIndex);

	private static bool IsDescendantOf(DependencyObject element, DependencyObject ancestor)
	{
		for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (ReferenceEquals(current, ancestor))
			{
				return true;
			}
		}

		return false;
	}

	private static T FindFirst<T>(DependencyObject root, Func<T, bool> predicate)
		where T : DependencyObject
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T match && predicate(match))
			{
				return match;
			}

			var nested = FindFirst(child, predicate);
			if (nested is not null)
			{
				return nested;
			}
		}

		return null;
	}

	private static async Task ScrollTo(ListViewBase list, double offset)
	{
		var sv = GetScrollViewer(list);
		sv.ChangeView(null, offset, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(sv.VerticalOffset - offset) < Tolerance, timeoutMS: 3000, message: $"Scroll to {offset}");
		await WindowHelper.WaitForIdle();
	}

	// Every realized container must show the item the source holds at its flat index.
	private static void AssertRealizedMatchesGroups(ListViewBase list, IEnumerable<Group> groups)
	{
		var flat = Flatten(groups);
		Assert.AreEqual(flat.Count, list.Items.Count, "Items.Count is the flattened item count");

		int realized = 0;
		for (int i = 0; i < flat.Count; i++)
		{
			if (list.ContainerFromIndex(i) is ContentControl container)
			{
				realized++;
				Assert.AreEqual(flat[i], container.Content, $"Content at flat index {i}");
				Assert.AreEqual(i, list.IndexFromContainer(container), $"IndexFromContainer at {i}");
			}
		}

		Assert.IsGreaterThan(0, realized, "Some containers should be realized");
	}

	private static async Task WaitForHeaders(ListViewBase list, params string[] keys) =>
		await WindowHelper.WaitFor(() => HeaderKeys(list).SequenceEqual(keys), timeoutMS: 3000, message: $"Headers [{string.Join(", ", keys)}], found [{string.Join(", ", HeaderKeys(list))}]");

	#region Realization

	[TestMethod]
	public void When_Created_Then_Group_Panel_Defaults_Match_WinUI()
	{
		ItemsStackPanel panel = new();

		Assert.IsTrue(panel.AreStickyGroupHeadersEnabled);
		Assert.AreEqual(GroupHeaderPlacement.Top, panel.GroupHeaderPlacement);
	}

	[TestMethod]
	public async Task When_Grouped_Then_Headers_And_Items_Are_Realized_In_Order()
	{
		var groups = CreateGroups(3, 5);
		var list = await Load(CreateList(groups));

		await WaitForHeaders(list, "G0", "G1", "G2");
		for (int g = 0; g < 3; g++)
		{
			Assert.AreSame(groups[g], HeaderOf(list, $"G{g}").Content, "Header content is the group object");
		}

		AssertRealizedMatchesGroups(list, groups);

		// Top placement, vertical: header, then its items, then the next header.
		var h0 = Bounds(HeaderOf(list, "G0"));
		var first = Bounds(Item(list, 0));
		var last = Bounds(Item(list, 4));
		var h1 = Bounds(HeaderOf(list, "G1"));
		Assert.AreEqual(h0.Bottom + HeaderOf(list, "G0").Margin.Bottom, first.Top, Tolerance);
		Assert.AreEqual(last.Bottom, h1.Top, Tolerance);
	}

	[TestMethod]
	public async Task When_Many_Groups_Then_Headers_And_Items_Are_Virtualized()
	{
		var groups = CreateGroups(500, 10);
		var list = await Load(CreateList(groups));
		var panel = (Panel)list.ItemsPanelRoot;

		Assert.IsGreaterThan(0, Headers(list).Count);
		Assert.IsLessThan(100, Headers(list).Count, "Headers realized");
		Assert.IsLessThan(300, panel.Children.Count, "Children realized");
		Assert.IsNull(list.ContainerFromIndex(4999));

		list.ScrollIntoView(groups[400][5]);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(4005) is not null, timeoutMS: 3000, message: "Container of G400.5 realized");
		await WindowHelper.WaitForIdle();

		Assert.IsLessThan(100, Headers(list).Count, "Headers realized after scrolling");
		Assert.IsLessThan(300, panel.Children.Count, "Children realized after scrolling");
		Assert.IsNull(list.ContainerFromIndex(1));
		Assert.IsFalse(Headers(list).Any(h => ((Group)h.Content).Key == "G0"), "Header of G0 should be recycled");
	}

	[TestMethod]
	public async Task When_Grouped_Then_Item_Indices_Are_Flat_Across_Groups()
	{
		ObservableCollection<Group> groups = new() { new("G0", 3), new("G1", 5), new("G2", 2), new("G3", 4) };
		var list = CreateList(groups);
		list.Height = 1000;
		await Load(list);

		var flat = Flatten(groups);
		Assert.AreEqual(14, list.Items.Count);
		for (int i = 0; i < flat.Count; i++)
		{
			var container = list.ContainerFromIndex(i);
			Assert.IsNotNull(container, $"Container {i}");
			Assert.IsNotInstanceOfType<ListViewBaseHeaderItem>(container);
			Assert.AreEqual(flat[i], list.ItemFromContainer(container));
			Assert.AreEqual(i, list.IndexFromContainer(container));
			Assert.AreSame(container, list.ContainerFromItem(flat[i]));
		}

		Assert.IsNull(list.ContainerFromIndex(14));
	}

	[TestMethod]
	public async Task When_Group_Header_Requested_From_Item_Then_Its_Group_Header_Is_Returned()
	{
		var groups = CreateGroups(3, 4);
		var list = CreateList(groups);
		list.Height = 1000;
		await Load(list);

		for (int g = 0; g < 3; g++)
		{
			for (int i = 0; i < 4; i++)
			{
				var header = list.GroupHeaderContainerFromItemContainer(list.ContainerFromIndex(g * 4 + i));
				Assert.IsNotNull(header, $"Header for G{g}.{i}");
				Assert.AreSame(groups[g], ((ListViewBaseHeaderItem)header).Content);
			}
		}
	}

	#endregion

	#region Sticky headers

	private async Task<(ListViewBase List, ScrollViewer Sv)> LoadSticky(bool sticky, string extra = "")
	{
		var groups = CreateGroups(5, 30);
		var list = await Load(CreateList(groups, panelXaml: $"<ItemsStackPanel AreStickyGroupHeadersEnabled='{sticky}' {extra} />"));
		await WaitForHeaders(list, "G0", "G1");
		return (list, GetScrollViewer(list));
	}

	[TestMethod]
	[DataRow(true, 0.0)]
	[DataRow(false, -200.0)]
	public async Task When_Scrolled_Within_Group_Then_Header_Position_Depends_On_Sticky(bool sticky, double expectedTop)
	{
		var (list, sv) = await LoadSticky(sticky);

		await ScrollTo(list, 200);

		var header = HeaderOf(list, "G0");
		Assert.AreEqual(Bounds(sv).Top + expectedTop, Bounds(header).Top, Tolerance);
	}

	[TestMethod]
	public async Task When_Sticky_Header_Overlaps_Items_Then_Header_Is_Hit_Testable_And_On_Top()
	{
		var (list, sv) = await LoadSticky(true);
		await ScrollTo(list, 200);

		var header = HeaderOf(list, "G0");
		var headerBounds = Bounds(header);
		var overlapped = Enumerable.Range(0, 30)
			.Select(i => list.ContainerFromIndex(i) as FrameworkElement)
			.Where(c => c is not null)
			.Where(c => Bounds(c).Top < headerBounds.Bottom - Tolerance && Bounds(c).Bottom > headerBounds.Top + Tolerance)
			.ToList();
		Assert.IsNotEmpty(overlapped, "Some item should be scrolled underneath the sticky header");

		var text = FindFirst<TextBlock>(header, t => t.Text == "G0");
		Assert.IsNotNull(text);
		var textBounds = Bounds(text);
		Point point = new(textBounds.X + textBounds.Width / 2, textBounds.Y + textBounds.Height / 2);
		// FindElementsInHostCoordinates yields document order on Uno, so ask the pointer's hit test directly.
		var (hit, _) = VisualTreeHelper.HitTest(point, list.XamlRoot?.VisualTree.RootElement);

		Assert.IsNotNull(hit);
		Assert.IsTrue(IsDescendantOf(hit, header), $"Topmost element at the header was {hit}");
		Assert.IsGreaterThanOrEqualTo(Bounds(sv).Top - Tolerance, headerBounds.Top);
	}

	[TestMethod]
	public async Task When_Next_Group_Reaches_Top_Then_Its_Header_Takes_Over()
	{
		var (list, sv) = await LoadSticky(true);
		double group1Start = Bounds(HeaderOf(list, "G1")).Top - Bounds(HeaderOf(list, "G0")).Top;

		await ScrollTo(list, group1Start);

		Assert.AreEqual(Bounds(sv).Top, Bounds(HeaderOf(list, "G1")).Top, Tolerance);
		var g0 = Headers(list).FirstOrDefault(h => ((Group)h.Content).Key == "G0");
		Assert.IsTrue(g0 is null || Bounds(g0).Bottom <= Bounds(sv).Top + Tolerance, "G0 header must be gone from the viewport");
	}

	[TestMethod]
	public async Task When_Next_Header_Approaches_Then_Sticky_Header_Is_Pushed_Out()
	{
		var (list, sv) = await LoadSticky(true);
		double group1Start = Bounds(HeaderOf(list, "G1")).Top - Bounds(HeaderOf(list, "G0")).Top;
		double headerHeight = Bounds(HeaderOf(list, "G0")).Height;

		await ScrollTo(list, group1Start - headerHeight / 2);

		var g0 = Bounds(HeaderOf(list, "G0"));
		var g1 = Bounds(HeaderOf(list, "G1"));
		Assert.AreEqual(g1.Top, g0.Bottom + HeaderOf(list, "G0").Margin.Bottom, Tolerance, "The next header pushes the sticky one up");
		Assert.IsGreaterThan(Bounds(sv).Top, g1.Top);
	}

	[TestMethod]
	public async Task When_ScrollIntoView_Item_Under_Sticky_Header_Then_Scrolls_It_Clear_Of_The_Header()
	{
		// DoScrollItemIntoView: with sticky headers an item is only "visible" when it sits below its group's header.
		var (list, sv) = await LoadSticky(true);
		var groups = (IList<Group>)_cvs.Source;
		double headerHeight = Bounds(HeaderOf(list, "G0")).Height;
		double item5Top = Bounds(Item(list, 5)).Top - Bounds(sv).Top;
		await ScrollTo(list, item5Top - 10);

		list.ScrollIntoView(groups[0][5]);
		await WindowHelper.WaitFor(() => Bounds(Item(list, 5)).Top - Bounds(sv).Top >= headerHeight - Tolerance, timeoutMS: 3000, message: "Item 5 clear of the sticky header");
	}

	#endregion

	#region GroupStyle

	[TestMethod]
	[DataRow(false, 3)]
	[DataRow(true, 2)]
	public async Task When_Group_Is_Empty_Then_HidesIfEmpty_Decides_If_Header_Shows(bool hidesIfEmpty, int expectedHeaders)
	{
		ObservableCollection<Group> groups = new() { new("G0", 2), new("G1", 0), new("G2", 2) };
		var list = CreateList(groups, hidesIfEmpty: hidesIfEmpty);
		list.Height = 1000;
		await Load(list);

		await WindowHelper.WaitFor(() => Headers(list).Count == expectedHeaders, timeoutMS: 3000, message: $"{expectedHeaders} headers, found {Headers(list).Count}");
		Assert.AreEqual(4, list.Items.Count);
		Assert.AreEqual(hidesIfEmpty, !HeaderKeys(list).Contains("G1"));
	}

	[TestMethod]
	public async Task When_HeaderTemplate_Set_Then_Header_Shows_Templated_Content()
	{
		var groups = CreateGroups(3, 2);
		var template = Template("<StackPanel><TextBlock Tag='templated' Text='{Binding Key}' Height='40' /></StackPanel>");
		var list = CreateList(groups, headerTemplate: template);
		await Load(list);
		await WaitForHeaders(list, "G0", "G1", "G2");

		foreach (var header in Headers(list))
		{
			var text = FindFirst<TextBlock>(header, t => Equals(t.Tag, "templated"));
			Assert.IsNotNull(text, "Templated TextBlock");
			Assert.AreEqual(((Group)header.Content).Key, text.Text);
		}
	}

	[TestMethod]
	public async Task When_HeaderTemplateSelector_Set_Then_Each_Header_Gets_Its_Template()
	{
		var groups = CreateGroups(4, 2);
		ParityTemplateSelector selector = new()
		{
			Even = Template("<TextBlock Tag='even' Text='{Binding Key}' Height='40' />"),
			Odd = Template("<TextBlock Tag='odd' Text='{Binding Key}' Height='40' />"),
		};
		var list = CreateList(groups, headerSelector: selector);
		list.Height = 1000;
		await Load(list);
		await WaitForHeaders(list, "G0", "G1", "G2", "G3");

		foreach (var header in Headers(list))
		{
			var key = ((Group)header.Content).Key;
			string expected = int.Parse(key.Substring(1)) % 2 == 0 ? "even" : "odd";
			var text = FindFirst<TextBlock>(header, t => Equals(t.Tag, expected));
			Assert.IsNotNull(text, $"{key} should use the {expected} template");
			Assert.AreEqual(key, text.Text);
		}
	}

	#endregion

	#region ScrollIntoView

	[TestMethod]
	public async Task When_ScrollIntoView_Item_In_Far_Group_Then_Item_Is_Visible()
	{
		var groups = CreateGroups(100, 10);
		var list = await Load(CreateList(groups));
		var sv = GetScrollViewer(list);

		list.ScrollIntoView(groups[50][3]);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(503) is not null, timeoutMS: 3000, message: "G50.3 realized");
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		var item = Bounds(Item(list, 503));
		var viewport = Bounds(sv);
		Assert.IsGreaterThanOrEqualTo(viewport.Top - Tolerance, item.Top);
		Assert.IsLessThanOrEqualTo(viewport.Bottom + Tolerance, item.Bottom);
		Assert.AreEqual("G50.3", ((ContentControl)list.ContainerFromIndex(503)).Content);
	}

	[TestMethod]
	public async Task When_ScrollIntoView_Last_And_First_Group_Items_Then_List_Scrolls_Both_Ways()
	{
		var groups = CreateGroups(100, 10);
		var list = await Load(CreateList(groups));
		var sv = GetScrollViewer(list);

		list.ScrollIntoView(groups[99][9], ScrollIntoViewAlignment.Leading);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(999) is not null, timeoutMS: 3000, message: "Last item realized");
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(sv.ScrollableHeight, sv.VerticalOffset, Tolerance, "Leading on the last item clamps to the end");
		Assert.IsNull(list.ContainerFromIndex(1));

		list.ScrollIntoView(groups[0][0]);
		await WindowHelper.WaitFor(() => list.ContainerFromIndex(0) is not null, timeoutMS: 3000, message: "First item realized");
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		var item = Bounds(Item(list, 0));
		Assert.IsGreaterThanOrEqualTo(Bounds(sv).Top - Tolerance, item.Top);
		Assert.IsNull(list.ContainerFromIndex(999));
	}

	#endregion

	#region Source changes

	private async Task<(ListViewBase List, ObservableCollection<Group> Groups)> LoadSmall(bool hidesIfEmpty = false)
	{
		ObservableCollection<Group> groups = new() { new("G0", 3), new("G1", 3), new("G2", 3) };
		var list = CreateList(groups, hidesIfEmpty: hidesIfEmpty);
		list.Height = 1000;
		await Load(list);
		await WaitForHeaders(list, "G0", "G1", "G2");
		return (list, groups);
	}

	[TestMethod]
	public async Task When_Item_Added_To_Group_Then_Containers_Shift()
	{
		var (list, groups) = await LoadSmall();

		groups[1].Insert(1, "New");

		await WindowHelper.WaitFor(() => list.Items.Count == 10 && Equals((list.ContainerFromIndex(4) as ContentControl)?.Content, "New"), timeoutMS: 3000, message: "New item realized at flat index 4");
		await WindowHelper.WaitForIdle();
		AssertRealizedMatchesGroups(list, groups);
		Assert.AreEqual(3, Headers(list).Count);
	}

	[TestMethod]
	public async Task When_Item_Removed_From_Group_Then_Containers_Shift()
	{
		var (list, groups) = await LoadSmall();

		groups[0].RemoveAt(0);

		await WindowHelper.WaitFor(() => list.Items.Count == 8 && Equals((list.ContainerFromIndex(0) as ContentControl)?.Content, "G0.1"), timeoutMS: 3000, message: "Containers shifted");
		await WindowHelper.WaitForIdle();
		AssertRealizedMatchesGroups(list, groups);
		Assert.AreEqual(3, Headers(list).Count);
	}

	[TestMethod]
	public async Task When_Group_Added_Then_Header_And_Items_Appear()
	{
		var (list, groups) = await LoadSmall();

		groups.Insert(1, new Group("G9", 2));

		await WaitForHeaders(list, "G0", "G1", "G2", "G9");
		await WindowHelper.WaitFor(() => list.Items.Count == 11, timeoutMS: 3000, message: "Items.Count includes the new group");
		await WindowHelper.WaitForIdle();
		AssertRealizedMatchesGroups(list, groups);
		var inserted = HeaderOf(list, "G9");
		Assert.IsGreaterThan(Bounds(HeaderOf(list, "G0")).Top, Bounds(inserted).Top);
		Assert.IsLessThan(Bounds(HeaderOf(list, "G1")).Top, Bounds(inserted).Top, "G9 sits between G0 and G1");
	}

	[TestMethod]
	public async Task When_Group_Removed_Then_Header_And_Items_Disappear()
	{
		var (list, groups) = await LoadSmall();

		groups.RemoveAt(0);

		await WaitForHeaders(list, "G1", "G2");
		await WindowHelper.WaitFor(() => list.Items.Count == 6, timeoutMS: 3000, message: "Items.Count dropped by the group size");
		await WindowHelper.WaitForIdle();
		AssertRealizedMatchesGroups(list, groups);
	}

	[TestMethod]
	[DataRow(false, 3)]
	[DataRow(true, 2)]
	public async Task When_Group_Emptied_Then_HidesIfEmpty_Decides_If_Header_Stays(bool hidesIfEmpty, int expectedHeaders)
	{
		var (list, groups) = await LoadSmall(hidesIfEmpty);

		groups[1].Clear();

		await WindowHelper.WaitFor(() => list.Items.Count == 6 && Headers(list).Count == expectedHeaders, timeoutMS: 3000, message: $"{expectedHeaders} headers, found {Headers(list).Count}");
		Assert.AreEqual(hidesIfEmpty, !HeaderKeys(list).Contains("G1"));
		AssertRealizedMatchesGroups(list, groups);
	}

	#endregion

	#region GridView and header placement

	[TestMethod]
	public async Task When_GridView_Grouped_Then_Header_Takes_A_Row_And_Items_Wrap_Below_It()
	{
		ObservableCollection<Group> groups = new() { new("G0", 10), new("G1", 7) };
		var grid = CreateList(groups, grid: true);
		grid.Height = 1000;
		await Load(grid);
		await WaitForHeaders(grid, "G0", "G1");

		var h0 = Bounds(HeaderOf(grid, "G0"));
		var h1 = Bounds(HeaderOf(grid, "G1"));
		var i0 = Bounds(Item(grid, 0));
		var i3 = Bounds(Item(grid, 3));
		var i4 = Bounds(Item(grid, 4));
		var i9 = Bounds(Item(grid, 9));
		var j0 = Bounds(Item(grid, 10));

		Assert.AreEqual(h0.Bottom + HeaderOf(grid, "G0").Margin.Bottom, i0.Top, Tolerance, "First row starts under the header");
		Assert.AreEqual(i0.Top, i3.Top, Tolerance, "Four items of 100px fit in 450px");
		Assert.AreEqual(i0.Bottom + Item(grid, 0).Margin.Bottom, i4.Top, Tolerance, "Fifth item wraps");
		Assert.AreEqual(i9.Bottom + Item(grid, 9).Margin.Bottom, h1.Top, Tolerance, "Next header starts below the last row of the previous group");
		Assert.AreEqual(h1.Bottom + HeaderOf(grid, "G1").Margin.Bottom, j0.Top, Tolerance, "Next group items start under their header");
		AssertRealizedMatchesGroups(grid, groups);
	}

	[TestMethod]
	public async Task When_GroupHeaderPlacement_Left_Then_Header_Sits_Beside_Its_Group()
	{
		// Vertical virtualization + Left placement uses the "parallel" header strategy.
		ObservableCollection<Group> groups = new() { new("G0", 5), new("G1", 5) };
		var list = CreateList(groups, panelXaml: "<ItemsStackPanel GroupHeaderPlacement='Left' />");
		list.Height = 1000;
		await Load(list);
		await WaitForHeaders(list, "G0", "G1");

		var h0 = Bounds(HeaderOf(list, "G0"));
		var h1 = Bounds(HeaderOf(list, "G1"));
		var first = Bounds(Item(list, 0));
		var last = Bounds(Item(list, 4));
		var second = Bounds(Item(list, 5));

		Assert.AreEqual(first.Top, h0.Top, Tolerance, "Header aligns with the first item of its group");
		Assert.IsLessThanOrEqualTo(first.Left + Tolerance, h0.Right, "Items start after the header");
		Assert.AreEqual(second.Top, h1.Top, Tolerance);
		Assert.IsGreaterThanOrEqualTo(last.Bottom - Tolerance, second.Top, "Groups still stack vertically");
	}

	[TestMethod]
	public async Task When_GroupHeaderPlacement_Left_Then_Headers_Are_Not_Sticky()
	{
		// ReevaluateGroupHeaderStrategy: sticky headers need vertical virtualization and a non-Left placement.
		var groups = CreateGroups(5, 30);
		var list = await Load(CreateList(groups, panelXaml: "<ItemsStackPanel GroupHeaderPlacement='Left' AreStickyGroupHeadersEnabled='True' />"));
		await WaitForHeaders(list, "G0", "G1");
		var sv = GetScrollViewer(list);

		await ScrollTo(list, 200);

		Assert.AreEqual(Bounds(sv).Top - 200, Bounds(HeaderOf(list, "G0")).Top, Tolerance);
	}

	#endregion
}
