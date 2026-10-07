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
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.DevTools.Input;
using Uno.UI.Helpers.WinUI;
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

		// CanSortColumn rejects the column, so the code wins over the IDL's "programmatic SortByColumn still works".
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
	public async Task When_CanUserSortColumns_Turned_Off_Clears_Sort()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		var age = table.Columns[1];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		var sorting = new List<TableViewSortingEventArgs>();
		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorting += (_, e) => sorting.Add(e);
		table.Sorted += (_, e) => sorted.Add(e);

		// The gate turning off drops the sort it was responsible for, through a regular ClearSort.
		table.CanUserSortColumns = false;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.None, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.None);
		Assert.AreEqual(1, sorting.Count);
		Assert.IsNull(sorting[0].Column);
		Assert.AreEqual(1, sorted.Count);
		Assert.IsNull(sorted[0].Column);
		Assert.AreEqual(SortDirection.None, sorted[0].Direction);

		// CanSortColumn does not consult CanUserSortColumns: programmatic sorting still works.
		Assert.IsTrue(table.SortByColumn(age, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.Ascending, age.SortDirection);
		var ages = GetRealizedRows(table).Select(r => ((Person)r.DataContext).Age).ToList();
		CollectionAssert.AreEqual(ages.OrderBy(a => a).ToList(), ages);
	}

	[TestMethod]
	public async Task When_CanSort_Turned_Off_On_Sorted_Column()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		var sortedCount = 0;
		table.Sorted += (_, _) => sortedCount++;

		// OnColumnCanSortChanged asks for SortByColumn(column, None), but CanSortColumn already
		// rejects the opted-out column, so the request is a no-op and the sort stays applied.
		// The code wins over its own comment ("must not keep an active sort applied to it").
		name.CanSort = false;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.Ascending, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.Ascending);
		Assert.AreEqual(0, sortedCount);
		Assert.IsNull(FindSortIndicator(GetHeaderCell(table, name)), "the rebuilt header drops the chevron");

		// ClearSort does not go through CanSortColumn, so it still clears the column.
		Assert.IsTrue(table.ClearSort());
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.None);
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
		var expectedIndex = items.OrderBy(p => p.Name, StringComparer.CurrentCulture).ToList().IndexOf(selected);
		Assert.AreNotEqual(0, expectedIndex, "the sort must move the selected row for this test to mean anything");

		var selectionChanged = 0;
		table.SelectionChanged += (_, _) => selectionChanged++;

		// RecomputeSortDPsAndRaiseInternal re-applies the selection before Sorted is raised.
		int? indexInSorted = null;
		bool? rowSelectedInSorted = null;
		table.Sorted += (_, _) =>
		{
			indexInSorted = table.SelectedIndex;
			rowSelectedInSorted = GetRow(table, table.SelectedIndex)?.IsSelected;
		};

		table.SortByColumn(table.Columns[0], SortDirection.Ascending);
		await WindowHelper.WaitForIdle();

		Assert.AreSame(selected, table.SelectedItem, "selection follows the row, not the slot");
		Assert.AreEqual(expectedIndex, table.SelectedIndex);
		Assert.AreEqual(0, selectionChanged, "the item is unchanged, so no clear-then-reselect is reported");
		Assert.AreEqual(expectedIndex, indexInSorted, "SelectedIndex already follows the row when Sorted is raised");
		// The Reset may have unrealized the row by then; a realized one must already be stamped.
		Assert.AreNotEqual(false, rowSelectedInSorted, "the row at the new index is not stamped selected when Sorted is raised");
		Assert.IsTrue(GetRow(table, expectedIndex)!.IsSelected);
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
	public async Task When_ItemsSource_Replaced_Sort_Reset_Silently()
	{
		var table = CreateTable(People(6));
		var name = table.Columns[0];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		AssertIndicator(table, name, SortDirection.Ascending);

		var sortingCount = 0;
		var sortedCount = 0;
		table.Sorting += (_, _) => sortingCount++;
		table.Sorted += (_, _) => sortedCount++;

		// ResetSortStateForNewItemsSource: the data set the sort described is gone, so the state is
		// dropped without a cancellable Sorting or a Sorted.
		var replacement = People(6);
		table.ItemsSource = replacement;
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(table.Columns.All(c => c.SortDirection == SortDirection.None));
		foreach (var column in table.Columns)
		{
			AssertIndicator(table, column, SortDirection.None);
		}
		AssertRowOrder(table, replacement, SortDirection.None);
		Assert.AreEqual(0, sortingCount);
		Assert.AreEqual(0, sortedCount);
	}

	[TestMethod]
	public async Task When_TableViewSource_Reshape_Does_Not_Reraise_Sorted()
	{
		var items = new ObservableCollection<Person>(People(6));
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		var name = table.Columns[0];
		await LoadAsync(table, height: 600);

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		// Nothing sorted anywhere: a group change reaches the reconciliation but raises nothing.
		source.GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(0, sorted.Count, "a group change with nothing sorted is not a sort change");

		source.ClearGroupBy();
		await WindowHelper.WaitForIdle();

		source.Sort(nameof(Person.Name), SortDirection.Ascending);
		await WindowHelper.WaitFor(() => name.SortDirection == SortDirection.Ascending, message: "the column did not follow the source's path sort");
		await WindowHelper.WaitForIdle();
		var countAfterSort = sorted.Count;
		Assert.AreEqual(1, countAfterSort);

		// Already reconciled to exactly this state: an unrelated filter must not report a sort change.
		source.Filter(new TableViewPredicate(item => ((Person)item!).Age >= 20));
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(countAfterSort, sorted.Count);
		Assert.AreEqual(SortDirection.Ascending, name.SortDirection);
	}

	[TestMethod]
	public async Task When_TableViewSource_Sort_Replaces_Control_Axis()
	{
		var items = People(6);
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		var name = table.Columns[0];
		var age = table.Columns[1];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(age, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		// Last writer wins: the app's axis removes the control's own instead of stacking behind it.
		source.Sort(nameof(Person.Name), SortDirection.Descending);
		await WindowHelper.WaitFor(() => name.SortDirection == SortDirection.Descending, message: "the column did not follow the source's path sort");
		await WindowHelper.WaitForIdle();

		var axes = source.ActiveSortAxisInfos();
		Assert.AreEqual(1, axes.Count, "only the app's axis remains");
		Assert.AreEqual(nameof(Person.Name), axes[0].SortMemberPath);
		Assert.AreEqual(SortDirection.Descending, axes[0].Direction);

		Assert.AreEqual(SortDirection.None, age.SortDirection);
		AssertIndicator(table, age, SortDirection.None);
		AssertIndicator(table, name, SortDirection.Descending);
		AssertRowOrder(table, items, SortDirection.Descending);
		Assert.AreSame(name, sorted[^1].Column);
		Assert.AreEqual(SortDirection.Descending, sorted[^1].Direction);
	}

	[TestMethod]
	public async Task When_Sort_While_Editing_Commits_First()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		table.IsReadOnly = false;
		await LoadAsync(table);

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		Assert.IsTrue(table.IsEditing);

		var editor = FindEditor(table)!;
		editor.Text = "Aaron";

		// TryTerminateEditForControlInitiatedReshape closes the editor before the rows move.
		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual("Aaron", items[1].Name);
		Assert.AreEqual(SortDirection.Ascending, name.SortDirection);
		AssertRowOrder(table, items, SortDirection.Ascending);
		Assert.AreEqual("Aaron", GetRowNames(table)[0]);
	}

	[TestMethod]
	public async Task When_Sort_While_Editing_Vetoed()
	{
		var items = People(6);
		var original = items[1].Name;
		var table = CreateTable(items);
		var name = table.Columns[0];
		table.IsReadOnly = false;
		await LoadAsync(table);

		table.CellEditEnding += (_, e) => e.Cancel = true;
		var sortingCount = 0;
		var sortedCount = 0;
		table.Sorting += (_, _) => sortingCount++;
		table.Sorted += (_, _) => sortedCount++;

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Aaron";

		// A vetoed close blocks the reshape: the sort is refused and the editor stays open.
		Assert.IsFalse(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(table.IsEditing);
		Assert.IsNotNull(FindEditor(table));
		Assert.AreEqual(original, items[1].Name);
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		Assert.AreEqual(0, sortingCount, "Sorting is raised only after the edit has closed");
		Assert.AreEqual(0, sortedCount);
	}

	[TestMethod]
	public async Task When_Sorting_Handler_Removes_Column()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		await LoadAsync(table);

		var sortedCount = 0;
		table.Sorting += (_, e) => table.Columns.Remove(e.Column!);
		table.Sorted += (_, _) => sortedCount++;

		// IsSortRequestStillValid re-checks ownership after Sorting returns.
		Assert.IsFalse(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.Columns.Contains(name));
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		Assert.AreEqual(0, sortedCount);
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

		var sortingCount = 0;
		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorting += (_, _) => sortingCount++;
		table.Sorted += (_, e) => sorted.Add(e);

		table.Columns.Remove(name);

		// The leaving column's own state is cleared at once; the clear itself is queued.
		Assert.AreEqual(SortDirection.None, name.SortDirection);
		await WindowHelper.WaitFor(() => sorted.Count == 1);

		Assert.IsNull(sorted[0].Column);
		Assert.AreEqual(SortDirection.None, sorted[0].Direction);
		Assert.AreEqual(0, sortingCount, "the queued clear is not cancellable");
	}

	[TestMethod]
	public async Task When_Sorted_Column_Removed_Then_Resorted_Same_Tick()
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		var age = table.Columns[1];
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		// The removal queues a clear; the newer sort lands before it runs, and the stale-clear guard
		// in RecomputeSortDPsAndRaiseInternal drops it.
		table.Columns.Remove(name);
		Assert.IsTrue(table.SortByColumn(age, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(SortDirection.Ascending, age.SortDirection);
		var ages = GetRealizedRows(table).Select(r => ((Person)r.DataContext).Age).ToList();
		CollectionAssert.AreEqual(ages.OrderBy(a => a).ToList(), ages);
		Assert.AreEqual(1, sorted.Count, "no queued clear overrides the newer sort");
		Assert.AreSame(age, sorted[0].Column);
		Assert.AreEqual(SortDirection.Ascending, sorted[0].Direction);
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
	public async Task When_CustomSortComparer_Swapped_While_Sorted()
	{
		var items = new List<Person>
		{
			new("Bo", 1, "Oslo"),
			new("Alexandra", 2, "Oslo"),
			new("Cyd", 3, "Oslo"),
			new("Al", 4, "Oslo"),
		};

		var table = CreateTable(items);
		var byLength = new TableViewTemplateColumn
		{
			Header = "Length",
			CustomSortComparer = new NameLengthComparer(),
		};
		table.Columns.Add(byLength);
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(byLength, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		CollectionAssert.AreEqual(new[] { "Bo", "Al", "Cyd", "Alexandra" }, GetRowNames(table));

		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorted += (_, e) => sorted.Add(e);

		// The setter re-applies the active direction (SortByColumn(None), then SortByColumn(direction))
		// rather than leaving the rows in the old comparer's order.
		byLength.CustomSortComparer = new ReversedComparer(new NameLengthComparer());
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(2, sorted.Count);
		Assert.AreSame(byLength, sorted[0].Column);
		Assert.AreEqual(SortDirection.None, sorted[0].Direction);
		Assert.AreSame(byLength, sorted[1].Column);
		Assert.AreEqual(SortDirection.Ascending, sorted[1].Direction);
		Assert.AreEqual(SortDirection.Ascending, byLength.SortDirection);
		CollectionAssert.AreEqual(new[] { "Alexandra", "Cyd", "Bo", "Al" }, GetRowNames(table));
	}

	[TestMethod]
	public async Task When_TextColumn_Explicit_SortMemberPath_Wins()
	{
		// TableViewTextColumn::GetSortMemberPathCore: an explicit SortMemberPath beats the binding path.
		var items = People(6);
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		var name = table.Columns[0];
		name.SortMemberPath = nameof(Person.Age);
		await LoadAsync(table);

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(items.OrderBy(p => p.Age).Select(p => p.Name).ToList(), GetRowNames(table), "sorted by Age, not by the displayed Name");

		Assert.IsTrue(table.ClearSort());
		await WindowHelper.WaitForIdle();

		// ReconcileSortStateWithSource matches the app's path axis on GetSortMemberPathCore.
		source.Sort(nameof(Person.Age), SortDirection.Descending);
		await WindowHelper.WaitFor(() => name.SortDirection == SortDirection.Descending, message: "the chevron did not follow the app's Age sort");
		AssertIndicator(table, name, SortDirection.Descending);
		Assert.AreEqual(SortDirection.None, table.Columns[1].SortDirection, "the Age column is not the first match");
	}

	[TestMethod]
	public async Task When_TextColumn_Binding_With_Explicit_Source_Is_Not_Sortable()
	{
		// GetEditingPropertyPath: a path relative to an explicit Source (or RelativeSource / ElementName)
		// names nothing on the row, so the column has no sort path.
		var table = CreateTable(People(6));
		var other = new Person("Other", 1, "Lima");
		var sourced = new TableViewTextColumn
		{
			Header = "Sourced",
			Binding = new Binding { Source = other, Path = new PropertyPath(nameof(Person.Name)) },
		};
		table.Columns.Add(sourced);
		await LoadAsync(table, width: 700);

		Assert.IsFalse(table.SortByColumn(sourced, SortDirection.Ascending));
		Assert.AreEqual(SortDirection.None, sourced.SortDirection);

		// The header builds its chevron from CanUserSortColumns && CanSort alone (TableView::RebuildHeaders),
		// so the unresolvable path leaves it in place at None.
		AssertIndicator(table, sourced, SortDirection.None);
	}

	[TestMethod]
	public async Task When_GetSortMemberPathCore_Overridden()
	{
		var items = People(6);
		var source = TableViewSource.From(items);
		var table = CreateTable(source);
		var byAge = new AgeSortedTemplateColumn { Header = "Custom" };
		table.Columns.Add(byAge);
		await LoadAsync(table, width: 700);

		// The override makes a template column (no binding, no comparer) sortable.
		Assert.IsTrue(table.SortByColumn(byAge, SortDirection.Ascending));
		await WindowHelper.WaitForIdle();
		CollectionAssert.AreEqual(items.OrderBy(p => p.Age).Select(p => p.Name).ToList(), GetRowNames(table));

		Assert.IsTrue(table.ClearSort());
		await WindowHelper.WaitForIdle();

		// The chevron follows an app-declared path axis matched through the override. The Age text
		// column comes first in Columns and also matches, so remove it to isolate the override.
		table.Columns.RemoveAt(1);
		source.Sort(nameof(Person.Age), SortDirection.Descending);
		await WindowHelper.WaitFor(() => byAge.SortDirection == SortDirection.Descending, message: "the chevron did not follow the app's sort through the override");
		AssertIndicator(table, byAge, SortDirection.Descending);
	}

	private sealed class AgeSortedTemplateColumn : TableViewTemplateColumn
	{
		protected internal override string GetSortMemberPathCore() => nameof(Person.Age);
	}

	private sealed class ReversedComparer : ITableViewSortComparer
	{
		private readonly ITableViewSortComparer _inner;

		public ReversedComparer(ITableViewSortComparer inner) => _inner = inner;

		public int Compare(object? left, object? right) => -_inner.Compare(left, right);
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
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_GroupHeader_Toggle_Keeps_Focus(bool rightToLeft)
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 32, "Oslo"),
		};
		var source = TableViewSource.From(items).GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		var table = CreateTable(source);
		if (rightToLeft)
		{
			table.FlowDirection = FlowDirection.RightToLeft;
		}

		await LoadAsync(table, height: 600);

		var header = GetRealizedGroupHeaders(table)[0];
		Assert.IsTrue(header.Focus(FocusState.Keyboard));
		await WindowHelper.WaitForIdle();

		// Only Enter/Space go through RequestToggle; the arrows go through RequestExpansion, which
		// asks the owner directly and raises no ToggleRequested.
		var toggles = new List<object?>();
		foreach (var realized in GetRealizedGroupHeaders(table))
		{
			realized.ToggleRequested += (_, e) => toggles.Add(e.GroupKey);
		}

		await PressAsync("enter");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, toggles.Count);
		Assert.AreEqual("Oslo", toggles[0]);

		var focused = GetFocusedOsloHeader();
		Assert.IsFalse(focused.IsExpanded);
		Assert.AreEqual(1, GetRealizedRows(table).Count, "only the Kyoto row remains");

		// Right expands in LTR and collapses in RTL (TreeViewItem / Expander convention).
		var expandKey = rightToLeft ? "left" : "right";
		var collapseKey = rightToLeft ? "right" : "left";

		await PressAsync(expandKey);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(GetFocusedOsloHeader().IsExpanded);
		Assert.AreEqual(3, GetRealizedRows(table).Count);

		// SetGroupExpansion is idempotent: expanding an expanded group changes nothing.
		await PressAsync(expandKey);
		await WindowHelper.WaitForIdle();
		Assert.IsTrue(GetFocusedOsloHeader().IsExpanded);
		Assert.AreEqual(3, GetRealizedRows(table).Count);

		await PressAsync(collapseKey);
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(GetFocusedOsloHeader().IsExpanded);
		Assert.AreEqual(1, GetRealizedRows(table).Count);

		await PressAsync(collapseKey);
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(GetFocusedOsloHeader().IsExpanded);
		Assert.AreEqual(1, GetRealizedRows(table).Count);

		Assert.AreEqual(1, toggles.Count, "the arrow keys raise no ToggleRequested");

		// Space toggles like Enter.
		var spaceHeader = GetFocusedOsloHeader();
		var spaceToggles = 0;
		spaceHeader.ToggleRequested += (_, _) => spaceToggles++;

		await PressAsync("space");
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, spaceToggles);
		Assert.IsTrue(GetFocusedOsloHeader().IsExpanded);
		Assert.AreEqual(3, GetRealizedRows(table).Count);

		static TableViewGroupHeader GetFocusedOsloHeader()
		{
			var focused = FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as TableViewGroupHeader;
			Assert.IsNotNull(focused, "focus stays on the group header across the reshape");
			Assert.AreEqual("Oslo", ((TableViewGroupInfo)focused!.Content).Key);
			return focused;
		}
	}

	[TestMethod]
	public async Task When_GroupHeaderTemplate()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
		};
		var source = TableViewSource.From(items).GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		var table = CreateTable(source);
		var appTemplate = (DataTemplate)XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<TextBlock Text="{Binding KeyText}" />
			</DataTemplate>
			""");
		table.GroupHeaderTemplate = appTemplate;
		await LoadAsync(table, height: 600);

		var headers = GetRealizedGroupHeaders(table);
		Assert.AreEqual(2, headers.Count);
		Assert.IsTrue(headers.All(h => ReferenceEquals(h.ContentTemplate, appTemplate)), "the app template is set on each prepared header");

		// GroupHeaderTemplate carries no change callback at this tag (TableView.idl), so the revert is
		// observed on headers prepared afterwards: ClearValue lets the Style's template re-apply.
		table.GroupHeaderTemplate = null;
		source.ClearGroupBy();
		await WindowHelper.WaitForIdle();
		source.GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		await WindowHelper.WaitForIdle();

		headers = GetRealizedGroupHeaders(table);
		Assert.AreEqual(2, headers.Count);
		foreach (var header in headers)
		{
			Assert.AreEqual(DependencyProperty.UnsetValue, header.ReadLocalValue(ContentControl.ContentTemplateProperty));
			Assert.IsNotNull(header.ContentTemplate, "the default Style's ContentTemplate setter applies");
			Assert.AreNotSame(appTemplate, header.ContentTemplate);
		}
	}

	[TestMethod]
	public async Task When_GroupInfo_KeyText_For_Non_String_Keys()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo", "Note"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 30, "Oslo"),
		};
		// A null key has no built-in group identity (RowIdentity::TryGetGroupIdentity fails with "null group
		// key"), so the bucket needs an app-supplied identity.
		var source = TableViewSource.From(items).GroupBy(
			new TableViewKeySelector(item => ((Person)item!).Notes),
			new TableViewIdentitySelector(key => key as string ?? "<null>"));
		var table = CreateTable(source);
		await LoadAsync(table, height: 600);

		var infos = GetRealizedGroupHeaders(table).Select(h => (TableViewGroupInfo)h.Content).ToList();
		Assert.AreEqual(2, infos.Count);
		Assert.AreEqual("Note", infos[0].KeyText);
		Assert.IsNull(infos[1].Key);
		Assert.AreEqual(LocalizedOrFallback(ResourceAccessor.SR_TableViewGroupHeaderNull, "(null)"), infos[1].KeyText, "a null key reads as the localized null label");

		// A non-string key is stringified (IStringable on a C# key), not reported as the "(group)" fallback.
		source.GroupBy(new TableViewKeySelector(item => ((Person)item!).Age));
		await WindowHelper.WaitForIdle();

		infos = GetRealizedGroupHeaders(table).Select(h => (TableViewGroupInfo)h.Content).ToList();
		CollectionAssert.AreEqual(new[] { "30", "31" }, infos.Select(i => i.KeyText).ToList());
		Assert.AreEqual(30, infos[0].Key);
	}

	[TestMethod]
	public async Task When_HeaderTemplateSelector_Takes_Precedence()
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		name.HeaderTemplate = HeaderTemplate("FromTemplate");
		var selector = new FixedTemplateSelector(HeaderTemplate("FromSelector"));
		name.HeaderTemplateSelector = selector;
		await LoadAsync(table);

		var presenter = GetHeaderCell(table, name).Children.OfType<ContentPresenter>().First();
		Assert.AreSame(selector, presenter.ContentTemplateSelector);
		Assert.AreEqual(DependencyProperty.UnsetValue, presenter.ReadLocalValue(ContentPresenter.ContentTemplateProperty), "the template is not applied alongside the selector");

		var texts = Descendants(GetHeaderCell(table, name)).OfType<TextBlock>().Select(t => t.Text).ToList();
		CollectionAssert.Contains(texts, "FromSelector");
		CollectionAssert.DoesNotContain(texts, "FromTemplate");

		// Without a selector, the template applies.
		name.HeaderTemplateSelector = null;
		await WindowHelper.WaitForIdle();

		presenter = GetHeaderCell(table, name).Children.OfType<ContentPresenter>().First();
		Assert.AreSame(name.HeaderTemplate, presenter.ContentTemplate);
		texts = Descendants(GetHeaderCell(table, name)).OfType<TextBlock>().Select(t => t.Text).ToList();
		CollectionAssert.Contains(texts, "FromTemplate");

		static DataTemplate HeaderTemplate(string text)
			=> (DataTemplate)XamlReader.Load(
				$"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<TextBlock Text="{text}" />
				</DataTemplate>
				""");
	}

	[TestMethod]
	public async Task When_HeaderToolTip()
	{
		var table = CreateTable(People(3));
		var name = table.Columns[0];
		name.HeaderToolTip = "";
		await LoadAsync(table);

		// Null or empty means no tooltip.
		Assert.IsNull(ToolTipService.GetToolTip(GetHeaderCell(table, name)));

		name.HeaderToolTip = "The name";
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("The name", GetCellToolTipText(GetHeaderCell(table, name)));

		name.HeaderToolTip = "";
		await WindowHelper.WaitForIdle();
		Assert.IsNull(GetCellToolTipText(GetHeaderCell(table, name)), "an empty value retracts the tooltip");
	}

	private sealed class FixedTemplateSelector : DataTemplateSelector
	{
		private readonly DataTemplate _template;

		public FixedTemplateSelector(DataTemplate template) => _template = template;

		protected override DataTemplate SelectTemplateCore(object item) => _template;

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => _template;
	}

	private static string LocalizedOrFallback(string resourceName, string fallback)
		=> ResourceAccessor.GetLocalizedStringResource(resourceName) is { Length: > 0 } resolved ? resolved : fallback;

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
