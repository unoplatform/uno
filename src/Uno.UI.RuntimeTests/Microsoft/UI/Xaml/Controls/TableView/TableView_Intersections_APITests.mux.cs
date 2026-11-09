// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Intersections_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewShapingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewGroupingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSortingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewAutomationTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewIntersectionTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Feature intersections: selection, sort, filter, grouping, live data and automation combined.
[TestClass]
public class TableViewIntersectionTests : TableViewApiTestBase
{
	#region Selection across reshapes

	[TestMethod]
	[TestProperty("Description", "Verifies the selected item survives a sort, a filter that keeps it, and clearing both, with SelectedIndex tracking its projected row at each step.")]
	public async Task VerifySelectionSurvivesSortThenFilterThenClear()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ShapedPerson diego = null;
		var selectionChangedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			var items = MakeShapedPeople();
			diego = items.First(p => p.Name == "Diego");
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			VerifySelectionOn(tableView, diego, "baseline");
			tableView.SelectionChanged += (s, e) => selectionChangedCount++;

			source.Sort("Name", SortDirection.Descending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Rafa", "Owen", "Mei", "Ines", "Diego", "Asha" }, "sorted by name descending");
			VerifySelectionOn(tableView, diego, "after the sort");

			source.Filter(o => Person(o).Role == "Engineer");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Rafa", "Owen", "Diego" }, "sorted and filtered to engineers");
			VerifySelectionOn(tableView, diego, "after the filter");

			source.ClearFilter();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Rafa", "Owen", "Mei", "Ines", "Diego", "Asha" }, "filter cleared, sort kept");
			VerifySelectionOn(tableView, diego, "after clearing the filter");

			source.ClearSort();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Asha", "Diego", "Mei", "Rafa", "Ines", "Owen" }, "both cleared");
			VerifySelectionOn(tableView, diego, "after clearing the sort");

			Verify.AreEqual(0, selectionChangedCount,
				"The selected item never changed, so no reshape may raise SelectionChanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filtering the selected item out clears the selection once, and restoring the filter does not resurrect it.")]
	public async Task VerifySelectedItemWhenFilteredOutThenRestored()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ShapedPerson diego = null;
		var events = new List<(IList<object> Added, IList<object> Removed)>();

		RunOnUIThread.Execute(() =>
		{
			var items = MakeShapedPeople();
			diego = items.First(p => p.Name == "Diego");
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			VerifySelectionOn(tableView, diego, "baseline");
			tableView.SelectionChanged += (s, e) => events.Add((e.AddedItems.ToList(), e.RemovedItems.ToList()));

			source.Filter(o => Person(o).Role == "Designer");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Asha", "Ines" }, "filtered to designers");
			VerifySelectionOn(tableView, null, "the selected row left the projection");
			VerifyNoIndexSelected(tableView, "after the filter");

			Verify.AreEqual(1, events.Count, "Losing the selected row must raise exactly one SelectionChanged.");
			Verify.AreEqual(0, events[0].Added.Count, "The clearing event must add nothing.");
			Verify.AreEqual(1, events[0].Removed.Count, "The clearing event must remove exactly one item.");
			Verify.AreSame(diego, events[0].Removed[0], "The clearing event must report the filtered-out item as removed.");

			source.ClearFilter();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Asha", "Diego", "Mei", "Rafa", "Ines", "Owen" }, "filter cleared");
			VerifySelectionOn(tableView, null, "a selection lost to a filter is a real deselect and must not come back");
			VerifyNoIndexSelected(tableView, "after the filter is cleared");
			Verify.AreEqual(1, events.Count, "Restoring the rows must not raise SelectionChanged; nothing was selected or deselected.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies applying GroupBy while a row is selected keeps that item selected and moves SelectedIndex to its projected index, counting header rows.")]
	public async Task VerifyGroupingAppliedWhileRowSelectedKeepsSelection()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ShapedPerson owen = null;

		RunOnUIThread.Execute(() =>
		{
			var items = MakeShapedPeople();
			owen = items.First(p => p.Name == "Owen");
			source = TableViewSource.From(items);
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(5);
			VerifySelectionOn(tableView, owen, "baseline");
			Verify.AreEqual(5, tableView.SelectedIndex, "Baseline: Owen is the last of six flat rows.");

			source.GroupBy(o => Person(o).Role);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView,
				new[] { "#Designer(2)", "Asha", "Ines", "#Engineer(3)", "Diego", "Rafa", "Owen", "#Architect(1)", "Mei" },
				"grouped by role");

			VerifySelectionOn(tableView, owen, "after grouping");
			Verify.AreEqual(6, tableView.SelectedIndex,
				"SelectedIndex addresses the displayed projection, so the two header rows above Owen count.");

			source.ClearGroupBy();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Asha", "Diego", "Mei", "Rafa", "Ines", "Owen" }, "grouping cleared");
			VerifySelectionOn(tableView, owen, "after clearing the grouping");
			Verify.AreEqual(5, tableView.SelectedIndex, "Without headers Owen is back at flat index 5.");
		});
	}

	#endregion

	#region Sort-key edits under an active filter

	[TestMethod]
	[TestProperty("Description", "Verifies a visible row whose sort key changed is moved to its sorted slot by the next collection change while a filter is active.")]
	public async Task VerifySortKeyChangeRepositionsRowWhileFilterActive()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			source = TableViewSource.From(items)
				.Sort("Name", SortDirection.Ascending)
				.Filter(o => Person(o).Role == "Engineer");
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(source.IsLiveShaping, "Precondition: this test covers the live-shaping-off repair path.");
			VerifyProjection(tableView, new[] { "Diego", "Owen", "Rafa" }, "sorted and filtered");

			items.First(p => p.Name == "Diego").Name = "Zane";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Live shaping is off, so the edit alone must not reorder; the Add below is what repairs it.
			VerifyProjection(tableView, new[] { "Zane", "Owen", "Rafa" }, "precondition: the sort-key edit left the row stale");

			items.Add(new ShapedPerson("Tomas", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Owen", "Rafa", "Zane" },
				"the next collection change must move the renamed row to its sorted slot");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a live sort-key edit on a filtered-out item does not bring it back into the projection.")]
	public async Task VerifySortKeyChangeOnFilteredOutItemDoesNotResurfaceIt()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;
		ShapedPerson asha = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			asha = items.First(p => p.Name == "Asha");

			var source = TableViewSource.From(items)
				.Sort("Name", SortDirection.Ascending)
				.Filter(o => Person(o).Role == "Engineer");
			source.IsLiveShaping = true;

			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Diego", "Owen", "Rafa" }, "sorted and filtered");

			// The visible edit proves a live reshape ran; the hidden one would sort first if it leaked in.
			items.First(p => p.Name == "Rafa").Name = "Ben";
			asha.Name = "Aaron";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Ben", "Diego", "Owen" },
				"a live reshape must re-sort the visible edit and keep the filtered-out item out");

			asha.Role = "Engineer";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Aaron", "Ben", "Diego", "Owen" },
				"once it passes the filter the item must join at the slot its edited key sorts to");
		});
	}

	#endregion

	#region Grouping with collapse and source changes

	[TestMethod]
	[TestProperty("Description", "Verifies items added after CollapseAllGroups land in collapsed groups, update header counts, and appear once expanded.")]
	public async Task VerifyCollapseAllThenAddPlacesRowInCollapsedGroup()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);
			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "#Designer(2)", "#Engineer(3)", "#Architect(1)" }, "precondition: every group collapsed");

			items.Add(new ShapedPerson("Nadia", "Engineer", "Platform"));
			items.Add(new ShapedPerson("Zoe", "Analyst", "Research"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// CollapseAllGroups moves the baseline, so a group created afterwards also arrives collapsed.
			VerifyProjection(tableView,
				new[] { "#Designer(2)", "#Engineer(4)", "#Architect(1)", "#Analyst(1)" },
				"added rows must stay hidden in collapsed groups while the header counts update");

			for (var i = 0; i < 4; i++)
			{
				Verify.IsFalse(GetGroupInfo(tableView, i)?.IsExpanded ?? true, $"Group {i} must still report itself collapsed.");
			}
		});

		RunOnUIThread.Execute(() => tableView.ExpandAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView,
				new[] { "#Designer(2)", "Asha", "Ines", "#Engineer(4)", "Diego", "Rafa", "Owen", "Nadia", "#Architect(1)", "Mei", "#Analyst(1)", "Zoe" },
				"expanding must reveal the added rows inside their groups");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ExpandAllGroups after a source Reset expands every group, including one with a per-group override, and projects every row.")]
	public async Task VerifyExpandAllAfterSourceResetReconcilesEveryGroup()
	{
		TableView tableView = null;
		ResettableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ResettableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);
			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => GetExpandCollapseProvider(tableView, 1).Expand());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView,
				new[] { "#Designer(2)", "#Engineer(3)", "Diego", "Rafa", "Owen", "#Architect(1)" },
				"precondition: all collapsed except Engineer");

			items.ReplaceAll(new[]
			{
				new ShapedPerson("Nadia", "Engineer", "Platform"),
				new ShapedPerson("Tomas", "Analyst", "Research"),
				new ShapedPerson("Kai", "Designer", "Studio"),
				new ShapedPerson("Lena", "Analyst", "Research"),
			});
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView,
				new[] { "#Engineer(1)", "Nadia", "#Analyst(2)", "#Designer(1)" },
				"precondition: after the Reset Engineer keeps its override and the other groups follow the collapsed baseline");
		});

		RunOnUIThread.Execute(() => tableView.ExpandAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView,
				new[] { "#Engineer(1)", "Nadia", "#Analyst(2)", "Tomas", "Lena", "#Designer(1)", "Kai" },
				"ExpandAllGroups must expand every group and project every row");

			Verify.AreEqual(items.Count, GetProjectedCount(tableView) - GroupHeaderCount(tableView),
				"Every source item must be projected exactly once.");

			for (var i = 0; i < 3; i++)
			{
				Verify.IsTrue(GetGroupInfo(tableView, i)?.IsExpanded ?? false, $"Group {i} must report itself expanded.");
			}
		});
	}

	#endregion

	#region Collection changes under an active sort

	[TestMethod]
	[TestProperty("Description", "Verifies appending several matching and non-matching items under an active sort and filter yields the sorted, filtered order.")]
	public async Task VerifyBulkAppendUnderActiveSortAndFilterProducesCorrectOrder()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items)
				.Sort("Name", SortDirection.Ascending)
				.Filter(o => Person(o).Role == "Engineer");
			tableView = CreateShapedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Diego", "Owen", "Rafa" }, "precondition: sorted and filtered");

			foreach (var person in new[]
			{
				new ShapedPerson("Bea", "Engineer", "Platform"),
				new ShapedPerson("Zed", "Designer", "Studio"),
				new ShapedPerson("Cruz", "Engineer", "Platform"),
				new ShapedPerson("Abe", "Architect", "Research"),
				new ShapedPerson("Yara", "Engineer", "Platform"),
				new ShapedPerson("Lior", "Designer", "Studio"),
				new ShapedPerson("Nadia", "Engineer", "Platform"),
			})
			{
				items.Add(person);
			}
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var expected = ExpectedEngineersByName(items);
			Verify.AreEqual(7, expected.Count, "Precondition: three original engineers plus four of the seven appended items pass the filter.");

			VerifyProjection(tableView, expected, "after appending under sort and filter");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Remove and Move on the source under an active sort keep the projection sorted, with ties in source order.")]
	public async Task VerifyRemoveAndMoveUnderActiveSortKeepProjectionCorrect()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateShapedTable(TableViewSource.From(items).Sort("Role", SortDirection.Ascending));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Mei", "Asha", "Ines", "Diego", "Rafa", "Owen" },
				"precondition: sorted by role, ties in source order");

			items.Remove(items.First(p => p.Name == "Rafa"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Mei", "Asha", "Ines", "Diego", "Owen" }, "after Remove");

			// Moving Owen ahead of Diego reorders a tie, which the sort must reflect.
			items.Move(items.IndexOf(items.First(p => p.Name == "Owen")), items.IndexOf(items.First(p => p.Name == "Diego")));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Mei", "Asha", "Ines", "Owen", "Diego" }, "after a Move within a tie");

			items.Move(items.IndexOf(items.First(p => p.Name == "Mei")), items.Count - 1);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyProjection(tableView, new[] { "Mei", "Asha", "Ines", "Owen", "Diego" },
				"moving a row with a unique key must not change the sorted order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies inserting and moving source items from inside Sorted leaves a sorted projection with no duplicate or missing rows.")]
	public async Task VerifyReentrantSourceMutationFromSortedEventLeavesProjectionConsistent()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;
		var handlerRan = 0;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateSortTable(items);
			tableView.Sorted += (s, e) =>
			{
				handlerRan++;
				if (handlerRan == 1)
				{
					items.Insert(0, new ShapedPerson("Bea", "Engineer", "Platform"));
					items.Move(0, items.Count - 1);
				}
			};
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, handlerRan, "The Sorted handler should have run exactly once.");
			Verify.AreEqual(7, items.Count, "Precondition: the handler's insert took effect on the source.");
			Verify.AreEqual("Bea", items[items.Count - 1].Name, "Precondition: the handler's move took effect on the source.");
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[0].SortDirection,
				"Mutating the source from Sorted must not undo the sort that raised it.");

			var actual = RowValues(tableView, person => person.Name);
			Verify.AreEqual(actual.Count, actual.Distinct().Count(), "No row may be projected twice.");

			VerifySequence(
				items.Select(p => p.Name).OrderBy(name => name, StringComparer.Ordinal).ToList(),
				actual,
				"after inserting and moving from inside Sorted");
		});
	}

	#endregion

	#region Automation across column and group changes

	[TestMethod]
	[TestProperty("Description", "Verifies the table peer's column headers follow visible columns, in order, after a column is moved and another collapsed.")]
	public async Task VerifyColumnHeaderProvidersFollowVisibleColumnsAfterReorderAndHide()
	{
		TableView tableView = null;
		TableViewColumn nameColumn = null;
		TableViewColumn roleColumn = null;
		TableViewColumn aliasColumn = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			// Alias over Role, so a cell's name shows which column it belongs to.
			tableView.Columns.RemoveAt(2);
			tableView.Columns.Add(MakeTextColumn("Alias", "Role"));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, RequireGridProvider(tableView).ColumnCount, "Precondition: three visible columns.");

			nameColumn = tableView.Columns[0];
			roleColumn = tableView.Columns[1];
			aliasColumn = tableView.Columns[2];

			tableView.Columns.RemoveAt(2);
			tableView.Columns.Insert(0, aliasColumn);
			roleColumn.Visibility = Visibility.Collapsed;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreSame(aliasColumn, tableView.Columns[0], "Precondition: Alias was moved to the front.");

			Verify.AreEqual(2, RequireGridProvider(tableView).ColumnCount, "ColumnCount must count only the visible columns.");

			var table = RequireTablePeer(tableView).GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			var headers = table.GetColumnHeaders();
			Verify.AreEqual(2, headers == null ? 0 : headers.Length, "GetColumnHeaders must return one provider per visible column.");

			var aliasPeer = new TableViewColumnHeaderAutomationPeer(tableView, aliasColumn);
			var namePeer = new TableViewColumnHeaderAutomationPeer(tableView, nameColumn);
			var rolePeer = new TableViewColumnHeaderAutomationPeer(tableView, roleColumn);

			Verify.AreEqual("Alias", aliasPeer.GetName(), "The first header peer must name the moved column.");
			Verify.AreEqual(1, aliasPeer.GetPositionInSet(), "The moved column must be the first visible header.");
			Verify.AreEqual(2, namePeer.GetPositionInSet(), "Name must follow it as the second visible header.");
			Verify.AreEqual(2, aliasPeer.GetSizeOfSet(), "SizeOfSet must count visible columns only.");
			Verify.AreEqual(2, namePeer.GetSizeOfSet(), "SizeOfSet must be the same for every visible header.");
			Verify.AreEqual(0, rolePeer.GetPositionInSet(), "A collapsed column has no position in the header set.");

			var rowPeer = GetRowPeer(GetProjectedRow(tableView, 0));
			Verify.AreEqual(2, rowPeer.GetChildren()?.Count ?? 0, "The row peer must expose one cell per visible column.");
			for (int i = 0; i < 2; i++)
			{
				var gridItem = GetCellPeer(rowPeer, i).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
				Verify.AreEqual(i, gridItem?.Column ?? -1, $"Cell peer {i} must report visible column {i}.");
			}
			Verify.AreEqual("Designer", GetCellPeer(rowPeer, 0).GetName(), "The first cell must belong to the moved column.");
			Verify.AreEqual("Asha", GetCellPeer(rowPeer, 1).GetName(), "The second cell must belong to the Name column.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies every grouped row reports PositionInSet and SizeOfSet relative to its own group, in every group, before and after a filter changes the group sizes.")]
	public async Task VerifyGroupedRowPositionInSetIsGroupRelativeInEveryGroup()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople())
				.GroupBy(o => Person(o).Role)
				.Sort("Name", SortDirection.Descending);
			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView), "Precondition: three groups.");
			VerifyGroupRelativeRowPositions(tableView, "1,2,3", "grouped and sorted");

			source.Filter(o => Person(o).Name != "Rafa" && Person(o).Name != "Ines");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GroupHeaderCount(tableView), "Precondition: the filter keeps every group.");
			VerifyGroupRelativeRowPositions(tableView, "1,1,2", "after the filter shrank two groups");
		});
	}

	#endregion
}

internal static class TableViewIntersectionTestHelpers
{
	// Call on the UI thread. A null expected item asserts that nothing is selected.
	internal static void VerifySelectionOn(TableView tableView, object expected, string context)
	{
		if (expected == null)
		{
			Verify.AreEqual(-1, tableView.SelectedIndex, $"SelectedIndex must be -1 when nothing is selected ({context}).");
			Verify.IsNull(tableView.SelectedItem, $"SelectedItem must be null when nothing is selected ({context}).");
			return;
		}

		Verify.AreSame(expected, tableView.SelectedItem, $"SelectedItem must still be the same item object ({context}).");

		var index = GetProjectedItems(tableView).FindIndex(item => ReferenceEquals(item, expected));
		Verify.IsGreaterThanOrEqual(index, 0, $"The selected item must be projected ({context}).");
		Verify.AreEqual(index, tableView.SelectedIndex, $"SelectedIndex must be the item's projected index ({context}).");
		Verify.IsTrue(tableView.IsSelected(index), $"IsSelected must agree with SelectedIndex ({context}).");
	}

	internal static void VerifyNoIndexSelected(TableView tableView, string context)
	{
		var count = GetProjectedCount(tableView);
		for (var i = 0; i < count; i++)
		{
			Verify.IsFalse(tableView.IsSelected(i), $"Projected index {i} must not report IsSelected ({context}).");
		}
	}

	internal static List<string> ExpectedEngineersByName(IEnumerable<ShapedPerson> items)
		=> items.Where(p => p.Role == "Engineer").Select(p => p.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();


	// expectedSizes is the ascending, comma-joined list of group sizes, so it does not depend on group order.
	internal static void VerifyGroupRelativeRowPositions(TableView tableView, string expectedSizes, string context)
	{
		var groups = new List<List<TableViewRow>>();
		foreach (var element in GetProjectedElements(tableView))
		{
			if (element is TableViewGroupHeader)
			{
				groups.Add(new List<TableViewRow>());
			}
			else if (element is TableViewRow row)
			{
				if (groups.Count == 0)
				{
					Verify.Fail($"A data row was projected before any group header ({context}).");
					return;
				}

				groups[groups.Count - 1].Add(row);
			}
		}

		Verify.AreEqual(expectedSizes, string.Join(",", groups.Select(g => g.Count).OrderBy(c => c)),
			$"Precondition: group sizes ({context}).");

		foreach (var group in groups)
		{
			for (var i = 0; i < group.Count; i++)
			{
				var peer = GetRowPeer(group[i]);
				Verify.AreEqual(i + 1, peer.GetPositionInSet(),
					$"Row {i} of a {group.Count}-row group must report PositionInSet {i + 1} ({context}).");
				Verify.AreEqual(group.Count, peer.GetSizeOfSet(),
					$"A row in a {group.Count}-row group must report SizeOfSet {group.Count} ({context}).");
			}
		}
	}
}
