#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Input;
using Uno.UI.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public partial class Given_TableView
{
	private const string AscendingGlyph = "";
	private const string DescendingGlyph = "";

	[TestMethod]
	[DataRow(TableViewSortCycle.AscendingDescending, SortDirection.Ascending, SortDirection.Descending, SortDirection.Ascending)]
	[DataRow(TableViewSortCycle.AscendingDescendingNone, SortDirection.Ascending, SortDirection.Descending, SortDirection.None)]
	[DataRow(TableViewSortCycle.DescendingAscending, SortDirection.Descending, SortDirection.Ascending, SortDirection.Descending)]
	[DataRow(TableViewSortCycle.DescendingAscendingNone, SortDirection.Descending, SortDirection.Ascending, SortDirection.None)]
	public async Task When_Header_Sort_Cycle(TableViewSortCycle cycle, SortDirection first, SortDirection second, SortDirection third)
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		name.SortCycle = cycle;
		await LoadAsync(table);

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		AssertIndicator(table, name, SortDirection.None);

		foreach (var expected in new[] { first, second, third })
		{
			Assert.IsTrue(table.ToggleSortDirection(name));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(expected, name.SortDirection);
			Assert.AreSame(name, sorted[^1].Column);
			Assert.AreEqual(expected, sorted[^1].Direction);
			AssertIndicator(table, name, expected);
			AssertRowOrder(table, items, expected);
		}

		Assert.AreEqual(3, sorted.Count);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Header_Tapped_Sorts()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		await LoadAsync(table);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		// Tap the middle of the header, away from the resize gripper on the trailing edge.
		var headerCell = GetHeaderCell(table, name);
		var point = Center(headerCell);
		mouse.Press(point);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.Ascending, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.Ascending);

		mouse.Press(point);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.Descending, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.Descending);
	}

	[TestMethod]
	public async Task When_Sorting_Cancelled()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		await LoadAsync(table);

		var sorting = new List<TableViewSortingEventArgs>();
		var sortedCount = 0;
		table.Sorting += (_, e) =>
		{
			sorting.Add(e);
			e.Cancel = true;
		};
		table.Sorted += (_, _) => sortedCount++;

		Assert.IsFalse(table.ToggleSortDirection(name));
		Assert.IsFalse(table.SortByColumn(name, SortDirection.Descending));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, sorting.Count);
		Assert.AreSame(name, sorting[0].Column);
		Assert.AreEqual(SortDirection.Ascending, sorting[0].Direction, "the direction the toggle was about to apply");
		Assert.AreEqual(SortDirection.Descending, sorting[1].Direction);
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		Assert.AreEqual(0, sortedCount);
		AssertRowOrder(table, items, SortDirection.None);
	}

	[TestMethod]
	public async Task When_CanSort_False()
	{
		var table = CreateTable(People(6));
		var name = table.Columns[0];
		name.CanSort = false;
		await LoadAsync(table);

		// An opted-out column carries no chevron and no click handler.
		Assert.IsNull(FindSortIndicator(GetHeaderCell(table, name)));
		Assert.IsNotNull(FindSortIndicator(GetHeaderCell(table, table.Columns[1])));

		Assert.IsFalse(table.ToggleSortDirection(name));
		Assert.IsFalse(table.SortByColumn(name, SortDirection.Ascending));
		Assert.AreEqual(SortDirection.None, name.SortDirection);

		// The header peer withholds Invoke.
		var headers = GetColumnHeaderPeers(table);
		Assert.IsNull(headers[0].GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke));
		Assert.IsNotNull(headers[1].GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke));

		// The control-wide switch removes every affordance.
		table.CanUserSortColumns = false;
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(GetHeaderCells(table).All(c => FindSortIndicator(c) is null));
	}

	[TestMethod]
	public async Task When_SortByColumn_And_ClearSort()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		var age = table.Columns[1];
		await LoadAsync(table);

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		Assert.IsFalse(table.ClearSort(), "nothing to clear");
		Assert.IsFalse(table.SortByColumn(name, SortDirection.None), "nothing to clear");

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Descending));
		Assert.IsFalse(table.SortByColumn(name, SortDirection.Descending), "already in that state");

		// Single-column sort: sorting another column clears the first.
		Assert.IsTrue(table.SortByColumn(age, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		Assert.AreEqual(SortDirection.Ascending, age.SortDirection);
		AssertIndicator(table, name, SortDirection.None);
		AssertIndicator(table, age, SortDirection.Ascending);

		Assert.IsTrue(table.ClearSort());
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.None, age.SortDirection);
		Assert.IsNull(sorted[^1].Column, "Column = null is the clear-all sentinel");
		Assert.AreEqual(SortDirection.None, sorted[^1].Direction);
		AssertRowOrder(table, items, SortDirection.None);
	}

	[TestMethod]
	public async Task When_Sort_Preserves_Selected_Item()
	{
		var items = People(6);
		var table = CreateTable(items);
		await LoadAsync(table);

		table.Select(0);
		var selected = items[0];

		table.SortByColumn(table.Columns[0], SortDirection.Ascending);
		await WindowHelper.WaitForIdle();

		Assert.AreSame(selected, table.SelectedItem, "selection follows the row, not the slot");
		var expectedIndex = items.OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList().IndexOf(selected);
		Assert.AreEqual(expectedIndex, table.SelectedIndex);
	}

	[TestMethod]
	public async Task When_TableViewSource_Sort_Path_Matches_Column()
	{
		var items = People(6);
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		var name = table.Columns[0];
		var age = table.Columns[1];
		await LoadAsync(table);

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		// A path-declared axis names the property, so the matching column lights up.
		source.Sort(nameof(Person.Name), SortDirection.Descending);
		await WindowHelper.WaitFor(() => name.SortDirection == SortDirection.Descending, message: "the column did not follow the source's path sort");

		Assert.AreSame(name, sorted[^1].Column);
		Assert.AreEqual(SortDirection.Descending, sorted[^1].Direction);
		AssertIndicator(table, name, SortDirection.Descending);
		AssertRowOrder(table, items, SortDirection.Descending);

		// A key-selector axis names nothing: no chevron, and Sorted reports a null column.
		var countBefore = sorted.Count;
		source.ClearSort();
		source.Sort(new TableViewKeySelector(item => ((Person)item!).Age), SortDirection.Ascending);
		await WindowHelper.WaitFor(() => sorted.Count > countBefore && sorted[^1].Column is null);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.None, name.SortDirection);
		Assert.AreEqual(SortDirection.None, age.SortDirection);
		var ages = GetRealizedRows(table).Select(r => ((Person)r.DataContext).Age).ToList();
		CollectionAssert.AreEqual(ages.OrderBy(a => a).ToList(), ages);

		// The control's sort replaces the app's, and ClearSort clears every axis.
		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		AssertRowOrder(table, items, SortDirection.Ascending);

		Assert.IsTrue(table.ClearSort());
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(table.Columns.All(c => c.SortDirection == SortDirection.None));
		AssertRowOrder(table, items, SortDirection.None);
	}

	[TestMethod]
	public async Task When_Sorted_Column_Removed()
	{
		var table = CreateTable(People(6));
		var name = table.Columns[0];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		table.Columns.Remove(name);

		// The leaving column's own state is cleared at once; the clear itself is queued.
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		await WindowHelper.WaitFor(() => sorted.Count == 1);

		Assert.IsNull(sorted[0].Column);
		Assert.AreEqual(SortDirection.None, sorted[0].Direction);
	}

	[TestMethod]
	public async Task When_CustomSortComparer()
	{
		var items = new List<Person>
		{
			new("Bo", 1, "Oslo"),
			new("Alexandra", 2, "Oslo"),
			new("Cyd", 3, "Oslo"),
			new("Al", 4, "Oslo"),
		};

		var table = CreateTable(items);

		// A template column has no sort path; the comparer alone makes it sortable.
		var byLength = new TableViewTemplateColumn
		{
			Header = "Length",
			CustomSortComparer = new NameLengthComparer(),
		};
		table.Columns.Add(byLength);
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(byLength, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		// Stable: Bo and Al tie on length and keep their source order.
		CollectionAssert.AreEqual(new[] { "Bo", "Al", "Cyd", "Alexandra" }, GetRowNames(table));

		Assert.IsTrue(table.SortByColumn(byLength, SortDirection.Descending));
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "Alexandra", "Cyd", "Bo", "Al" }, GetRowNames(table));

		// Without a comparer or a path the column cannot sort.
		byLength.CustomSortComparer = null;
		table.ClearSort();
		Assert.IsFalse(table.SortByColumn(byLength, SortDirection.Ascending));
	}

	[TestMethod]
	public async Task When_GroupBy()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 32, "Oslo"),
			new("Dee", 33, "Lima"),
		};
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		await LoadAsync(table, height: 600);

		source.GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		await WindowHelper.WaitForIdle();

		// First-seen group order; the built-in header shows the key and a "(n)" count.
		var headers = GetRealizedGroupHeaders(table);
		var infos = headers.Select(h => (TableViewGroupInfo)h.Content).ToList();
		CollectionAssert.AreEqual(new[] { "Oslo", "Kyoto", "Lima" }, infos.Select(i => i.KeyText).ToList());
		CollectionAssert.AreEqual(new[] { "(2)", "(1)", "(1)" }, infos.Select(i => i.ItemCountText).ToList());
		Assert.AreEqual("Oslo", infos[0].Key);
		Assert.AreEqual(2, infos[0].ItemCount);
		Assert.AreEqual(0, infos[0].Level);
		Assert.IsTrue(infos.All(i => i.IsExpandable && i.IsExpanded));
		Assert.AreEqual(4, GetRealizedRows(table).Count);

		// Group headers share the index space with data rows but are not selectable.
		table.Select(0);
		Assert.AreEqual(-1, table.SelectedIndex);

		var changed = new List<string?>();
		foreach (var info in infos)
		{
			info.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
		}

		table.CollapseAllGroups();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, GetRealizedRows(table).Count, "collapsing hides every data row");
		Assert.AreEqual(3, GetRealizedGroupHeaders(table).Count);
		Assert.IsTrue(GetRealizedGroupHeaders(table).All(h => !h.IsExpanded));
		Assert.IsTrue(changed.Contains(nameof(TableViewGroupInfo.IsExpanded)), "the projection is updated in place and notifies");

		table.ExpandAllGroups();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(4, GetRealizedRows(table).Count);
		Assert.IsTrue(GetRealizedGroupHeaders(table).All(h => h.IsExpanded));

		source.ClearGroupBy();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0, GetRealizedGroupHeaders(table).Count);
		Assert.AreEqual(4, GetRealizedRows(table).Count);
	}

	[TestMethod]
	public async Task When_GroupHeader_Toggle_Keeps_Focus()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 32, "Oslo"),
		};
		var source = TableViewSource.From(items).GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		var table = CreateTable(source);
		await LoadAsync(table, height: 600);

		var header = GetRealizedGroupHeaders(table)[0];
		Assert.IsTrue(header.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		var toggles = new List<object?>();
		header.ToggleRequested += (_, e) => toggles.Add(e.GroupKey);

		await PressAsync("enter");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, toggles.Count);
		Assert.AreEqual("Oslo", toggles[0]);

		var focused = FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as TableViewGroupHeader;
		Assert.IsNotNull(focused, "focus stays on the group header across the reshape");
		Assert.AreEqual("Oslo", ((TableViewGroupInfo)focused!.Content).Key);
		Assert.IsFalse(focused.IsExpanded);
		Assert.AreEqual(1, GetRealizedRows(table).Count, "only the Kyoto row remains");

		// Left collapses, Right expands (TreeViewItem parity).
		await PressAsync("right");
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(3, GetRealizedRows(table).Count);
	}

	private sealed class NameLengthComparer : ITableViewSortComparer
	{
		public int Compare(object? left, object? right)
			=> ((Person)left!).Name.Length.CompareTo(((Person)right!).Name.Length);
	}

	private static void AssertRowOrder(TableView table, IList<Person> items, SortDirection direction)
	{
		var expected = direction switch
		{
			SortDirection.Ascending => items.OrderBy(p => p.Name, StringComparer.CurrentCulture),
			SortDirection.Descending => items.OrderByDescending(p => p.Name, StringComparer.CurrentCulture),
			_ => items.AsEnumerable(),
		};

		CollectionAssert.AreEqual(expected.Select(p => p.Name).ToList(), GetRowNames(table), $"row order for {direction}");
	}

	private static Control? FindSortIndicator(Grid headerCell)
		=> Descendants(headerCell).OfType<Control>().FirstOrDefault(c => c.GetType().Name == "SortIndicator");

	private static void AssertIndicator(TableView table, TableViewColumn column, SortDirection direction)
	{
		var indicator = FindSortIndicator(GetHeaderCell(table, column))!;
		Assert.IsNotNull(indicator);

		// Opacity and Glyph are driven imperatively after GoToState (microsoft-ui-xaml#6203).
		var layoutRoot = FindByName<FrameworkElement>(indicator, "LayoutRoot")!;
		var glyph = FindByName<FontIcon>(indicator, "GlyphIcon")!;
		Assert.AreEqual(direction == SortDirection.None ? 0.0 : 1.0, layoutRoot.Opacity, $"indicator opacity for {direction}");
		Assert.AreEqual(direction == SortDirection.Descending ? DescendingGlyph : AscendingGlyph, glyph.Glyph, $"indicator glyph for {direction}");
	}
}

#endif
