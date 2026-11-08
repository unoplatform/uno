// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Recycling_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRecyclingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewGroupingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// An open edit surviving a recycle needs BeginEdit, which has no API, so the interaction tests cover it.
[TestClass]
public class TableViewGroupHeaderRecyclingTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a group header that was pooled before the template changed shows the current template when it is realized again.")]
	public async Task VerifyPooledGroupHeaderShowsTheCurrentTemplateAfterRecycling()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableGroupedTable();
			tableView.GroupHeaderTemplate = CreateRecyclingMarkerTemplate(OldMarker);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var markers = ProjectedGroupHeaderTexts(tableView);
			Verify.IsGreaterThan(markers.Count, 0, "Precondition: group headers must realize before scrolling.");
			Verify.IsTrue(markers.All(text => text.StartsWith(OldMarker)),
				$"Precondition: every header must start on the first template. Saw: {Describe(markers)}.");
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(ProjectedGroupHeaderTexts(tableView).Contains(OldMarker + FirstGroupKey),
				"Precondition: the first group's header must have left realization, so its container is pooled.");

			// Swapped while the first header is pooled; the contract is that it is re-prepared on its way back in.
			tableView.GroupHeaderTemplate = CreateRecyclingMarkerTemplate(NewMarker);
		});

		await SettleLayout(tableView);

		await ScrollToStart(tableView);

		RunOnUIThread.Execute(() =>
		{
			var markers = ProjectedGroupHeaderTexts(tableView);
			Verify.IsGreaterThan(markers.Count, 0, "Headers must realize again after scrolling back.");

			Verify.IsFalse(markers.Any(text => text.StartsWith(OldMarker)),
				$"No header may resurface with the replaced template. Saw: {Describe(markers)}.");

			Verify.IsTrue(markers.All(text => text.StartsWith(NewMarker)),
				$"Every realized header must carry the current template. Saw: {Describe(markers)}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a default group header reused for another group shows that group's key and count text.")]
	public async Task VerifyRecycledDefaultGroupHeaderShowsItsNewGroupText()
	{
		TableView tableView = null;
		var firstKeys = new Dictionary<TableViewGroupHeader, object>();

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			foreach (var header in ProjectedGroupHeaders(tableView))
			{
				firstKeys[header] = (header.Content as TableViewGroupInfo)?.Key;
			}
			Verify.IsGreaterThan(firstKeys.Count, 0, "Precondition: group headers must realize before scrolling.");
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var headers = ProjectedGroupHeaders(tableView);
			Verify.IsTrue(headers.Any(header => firstKeys.TryGetValue(header, out var key) && !Equals(key, (header.Content as TableViewGroupInfo)?.Key)),
				"Precondition: at least one header container must now host a different group.");

			foreach (var header in headers)
			{
				var info = header.Content as TableViewGroupInfo;
				VerifyDefaultGroupHeaderText(header, info?.KeyText, $"header for '{info?.Key}' after recycling");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsed group state is rebuilt from the group rather than from the container when headers are recycled.")]
	public async Task VerifyCollapsedGroupStateSurvivesHeaderRecycling()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(ProjectedRowCount(tableView), 0, "Precondition: data rows must be projected before collapsing.");

			tableView.CollapseAllGroups();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual(0, ProjectedRowCount(tableView), "Precondition: collapsing every group must leave only headers projected."));

		await ScrollToEnd(tableView);
		await ScrollToStart(tableView);

		RunOnUIThread.Execute(() =>
		{
			var headers = ProjectedGroupHeaders(tableView);
			Verify.IsGreaterThan(headers.Count, 0, "Headers must realize again after scrolling back.");

			foreach (var header in headers)
			{
				Verify.IsFalse(header.IsExpanded,
					"A recycled group header must report the collapsed state of the group it now represents.");
			}

			Verify.AreEqual(0, ProjectedRowCount(tableView),
				"No data row may reappear after recycling a collapsed group's header.");
		});
	}
}

// Automation peers must describe the item a recycled container now hosts.
[TestClass]
public class TableViewAutomationRecyclingTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a recycled row's automation peer names the item the row now represents.")]
	public async Task VerifyRecycledRowPeerNamesTheNewItem()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(RealizedProjectedRows(tableView).Any(row => ReferenceEquals(row.DataContext, items[0])),
				"Precondition: the first item must be realized before scrolling."));

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var rows = RealizedProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must still be realized after scrolling.");

			Verify.IsFalse(rows.Any(row => ReferenceEquals(row.DataContext, items[0])),
				"Precondition: the first item must have left realization, so its container was recycled.");

			foreach (var row in rows)
			{
				var item = row.DataContext as Person;
				Verify.IsNotNull(item, "Every realized row must carry a data item.");

				var name = GetRowPeer(row).GetName();
				Log.Comment($"Row hosting '{item.Name}' reports name '{name}'.");

				Verify.IsTrue(name != null && name.Contains(item.Name),
					$"A recycled row must be named for the item it now hosts; '{name}' does not mention '{item.Name}'.");

				Verify.IsFalse(name.Contains(items[0].Name),
					$"A recycled row must not still name the item it previously hosted; saw '{name}'.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled cell's automation peer reports the value of the item its row now represents.")]
	public async Task VerifyRecycledCellPeerReportsTheNewItemsValue()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var rows = RealizedProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must still be realized after scrolling.");

			Verify.IsFalse(rows.Any(row => ReferenceEquals(row.DataContext, items[0])),
				"Precondition: the first item must have left realization.");

			foreach (var row in rows)
			{
				var item = row.DataContext as Person;
				Verify.IsNotNull(item, "Every realized row must carry a data item.");

				var cellPeer = GetCellPeer(GetRowPeer(row), 0);
				Verify.IsNotNull(cellPeer, "Every realized row must expose a peer for its first visible cell.");

				var name = cellPeer.GetName();
				Verify.IsTrue(name != null && name.Contains(item.Name),
					$"A recycled cell must report the current item's value; '{name}' does not mention '{item.Name}'.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled row reports the set position of the index it now occupies rather than the one it was prepared for.")]
	public async Task VerifyRecycledRowPeerReportsTheNewPositionInSet()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var projected = GetProjectedElements(tableView, requireAllRealized: false);
			var rows = RealizedProjectedRows(tableView);

			Verify.IsGreaterThan(rows.Count, 0, "Rows must still be realized after scrolling.");

			var checkedAny = false;

			foreach (var row in rows)
			{
				var index = projected.IndexOf(row);

				if (index < 0)
				{
					continue;
				}

				checkedAny = true;

				var position = GetRowPeer(row).GetPositionInSet();
				Log.Comment($"Row at projected index {index} reports PositionInSet {position}.");

				Verify.AreEqual(index + 1, position,
					$"A recycled row at projected index {index} must report PositionInSet {index + 1}.");
			}

			Verify.IsTrue(checkedAny, "Precondition: at least one realized row must be locatable in the projection.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled row whose row peer exists but whose cells have no peers keeps its cell elements and shows the new item.")]
	public async Task VerifyRecycledRowWithoutCellPeersReusesItsCells()
	{
		TableView tableView = null;
		List<Person> items = null;
		var cellsBefore = new Dictionary<TableViewRow, List<UIElement>>();
		var contentBefore = new Dictionary<TableViewRow, List<object>>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in RealizedProjectedRows(tableView))
			{
				// Cells are only enumerated, never asked for peers (FromElement would create one).
				GetRowPeer(row);
				cellsBefore[row] = GetCellsHost(row).Children.ToList();
				contentBefore[row] = cellsBefore[row].Select(cell => (object)GetCellContent((Grid)cell)).ToList();
			}

			Verify.IsGreaterThan(cellsBefore.Count, 0, "Precondition: rows must be realized before scrolling.");
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var recycledCount = 0;

			foreach (var row in RealizedProjectedRows(tableView))
			{
				var item = row.DataContext as Person;
				Verify.IsNotNull(item, "Every realized row must carry a data item.");

				var cells = GetCellsHost(row).Children.ToList();
				Verify.AreEqual(GetRowCellColumns(row).Count, cells.Count, "Every column must have a cell.");
				Verify.AreEqual(item.Name, GetCellText((Grid)cells[0]), "A recycled row's first cell must show its current item.");
				Verify.AreEqual(item.Role, GetCellText((Grid)cells[1]), "A recycled row's second cell must show its current item.");

				if (!cellsBefore.TryGetValue(row, out var previous))
				{
					continue;
				}

				recycledCount++;
				for (var i = 0; i < cells.Count; i++)
				{
					Verify.IsTrue(ReferenceEquals(previous[i], cells[i]),
						$"Recycling '{item.Name}' into a row with no cell peers must reuse cell {i}, not regenerate it.");
					Verify.IsNotNull(contentBefore[row][i], $"Precondition: cell {i} had generated content before recycling.");
					Verify.IsTrue(ReferenceEquals(contentBefore[row][i], GetCellContent((Grid)cells[i])),
						$"Recycling '{item.Name}' must keep the generated content of cell {i}.");
				}
			}

			Log.Comment($"{recycledCount} recycled rows reused their cells.");
			Verify.IsGreaterThan(recycledCount, 0, "Precondition: at least one container must have been recycled to a new item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled row whose cells already have automation peers replaces them, so a held cell provider never silently describes another item.")]
	public async Task VerifyRecycledRowWithCellPeersReplacesItsCells()
	{
		TableView tableView = null;
		List<Person> items = null;
		var peersBefore = new Dictionary<TableViewRow, TableViewCellAutomationPeer>();
		var cellsBefore = new Dictionary<TableViewRow, UIElement>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in RealizedProjectedRows(tableView))
			{
				peersBefore[row] = GetCellPeer(row, 0);
				cellsBefore[row] = GetRowCell(row, 0);
			}

			Verify.IsGreaterThan(peersBefore.Count, 0, "Precondition: rows must be realized before scrolling.");
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var recycledCount = 0;

			foreach (var row in RealizedProjectedRows(tableView))
			{
				var item = row.DataContext as Person;
				Verify.IsNotNull(item, "Every realized row must carry a data item.");

				var cellPeer = GetCellPeer(row, 0);
				var name = cellPeer.GetName();
				Verify.IsTrue(name != null && name.Contains(item.Name),
					$"A recycled cell peer must report the current item's value; '{name}' does not mention '{item.Name}'.");
				Verify.AreEqual(item.Name, GetCellText(GetRowCell(row, 0)), "A recycled row must show its current item.");

				if (!peersBefore.TryGetValue(row, out var previousPeer))
				{
					continue;
				}

				recycledCount++;
				Verify.IsFalse(ReferenceEquals(previousPeer, cellPeer),
					$"A row recycled to '{item.Name}' must not hand out the cell provider of its previous item.");
				Verify.IsFalse(ReferenceEquals(cellsBefore[row], GetRowCell(row, 0)),
					$"A row recycled to '{item.Name}' while its cells had peers must replace those cells.");
			}

			Log.Comment($"{recycledCount} recycled rows replaced their cells.");
			Verify.IsGreaterThan(recycledCount, 0, "Precondition: at least one container must have been recycled to a new item.");
		});
	}
}

// Recycled rows must repaint index- and table-dependent chrome for the slot they now occupy.
[TestClass]
public class TableViewRowChromeRecyclingTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies recycled rows carry the banding for their new index and draw grid lines, with and without a row peer.")]
	public async Task VerifyRecycledRowsRepaintBandingAndGridLines()
	{
		TableView tableView = null;
		List<Person> items = null;
		Brush baseBrush = null;
		Brush alternateBrush = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			baseBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
			alternateBrush = new SolidColorBrush(Microsoft.UI.Colors.LightBlue);
			tableView = CreateScrollableTable(items);
			tableView.RowBackground = baseBrush;
			tableView.AlternatingRowBackground = alternateBrush;
			tableView.GridLinesVisibility = TableViewGridLinesVisibility.All;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Every other row gets a peer so both recycle routes (with and without item tracking) are covered.
			var rows = RealizedProjectedRows(tableView);
			for (var i = 0; i < rows.Count; i += 2)
			{
				GetRowPeer(rows[i]);
			}

			VerifyChrome(tableView, items, baseBrush, alternateBrush, "before scrolling");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 37.0 * 7);
		RunOnUIThread.Execute(() => VerifyChrome(tableView, items, baseBrush, alternateBrush, "after a partial scroll"));

		await ScrollToEnd(tableView);
		RunOnUIThread.Execute(() => VerifyChrome(tableView, items, baseBrush, alternateBrush, "after scrolling to the end"));

		await ScrollToStart(tableView);
		RunOnUIThread.Execute(() => VerifyChrome(tableView, items, baseBrush, alternateBrush, "after scrolling back"));
	}

	private static void VerifyChrome(
		TableView tableView,
		List<Person> items,
		Brush baseBrush,
		Brush alternateBrush,
		string context)
	{
		var rows = RealizedProjectedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"Rows must be realized ({context}).");

		foreach (var row in rows)
		{
			var item = row.DataContext as Person;
			var index = items.IndexOf(item);
			Verify.IsGreaterThanOrEqual(index, 0, $"Every realized row must map to a source item ({context}).");

			Verify.AreEqual(item.Name, GetCellText(GetRowCell(row, 0)), $"Row {index} must show its own item ({context}).");

			var expected = (index % 2) == 0 ? baseBrush : alternateBrush;
			Verify.AreEqual(expected, row.Background, $"Row {index} must carry the brush for its parity ({context}).");

			// The final row may suppress its bottom line where it meets the table border.
			if (index != items.Count - 1)
			{
				Verify.IsGreaterThan(row.BorderThickness.Bottom, 0.0, $"Row {index} must draw its horizontal grid line ({context}).");
			}

			var cell = GetRowCell(row, 0);
			Verify.IsGreaterThan(cell.BorderThickness.Right, 0.0, $"Row {index} must draw its vertical grid line ({context}).");
			Verify.IsNotNull(cell.BorderBrush, $"Row {index}'s vertical grid line must have a brush ({context}).");
		}
	}
}

// Grouping and sorting must not defeat virtualization.
[TestClass]
public class TableViewVirtualizationAcrossFeaturesTests : TableViewApiTestBase
{
	private const int ManyGroupsItemCount = 2000;
	private const int ItemsPerGroup = 5;
	private const int ManyGroupsCount = ManyGroupsItemCount / ItemsPerGroup;

	[TestMethod]
	[TestProperty("Description", "Verifies a grouped source with hundreds of groups realizes only a viewport-bounded set of headers and rows at every scroll offset.")]
	public async Task VerifyGroupedProjectionRealizesBoundedRowsAcrossManyGroups()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateManyGroupsTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		var bound = 0;
		var scrollable = 0.0;

		RunOnUIThread.Execute(() =>
		{
			bound = MixedRealizationBound(tableView);
			scrollable = GetBodyScroller(tableView)?.ScrollableHeight ?? 0.0;

			Verify.AreEqual(ManyGroupsItemCount + ManyGroupsCount, GetProjectedCount(tableView),
				"Precondition: the projection must hold one header per group plus every data row.");
			Verify.IsLessThan(bound, GetProjectedCount(tableView),
				"Precondition: the projection must be far larger than the bound, or the bound proves nothing.");

			VerifyGroupedRealizationBounded(tableView, bound, "at the start", expectRows: true);
		});

		foreach (var fraction in new[] { 1.0 / 3.0, 2.0 / 3.0 })
		{
			await ScrollBodyTo(tableView, ScrollAxis.Vertical, scrollable * fraction);

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNull(GetProjectedElements(tableView, requireAllRealized: false)[0],
					$"Precondition: the first group header must have left realization at {fraction:P0}.");

				VerifyGroupedRealizationBounded(tableView, bound, $"at {fraction:P0}", expectRows: true);
			});
		}

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() => VerifyGroupedRealizationBounded(tableView, bound, "at the end", expectRows: true));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing every group of a large grouped source realizes headers only, at every scroll offset.")]
	public async Task VerifyCollapsedGroupsRealizeNoDataRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateManyGroupsTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(ProjectedRowCount(tableView), 0, "Precondition: data rows must be realized before collapsing.");

			tableView.CollapseAllGroups();
		});

		await SettleLayout(tableView);

		var bound = 0;
		var scrollable = 0.0;

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ManyGroupsCount, GetProjectedCount(tableView),
				"A full collapse must project exactly one header per group.");

			bound = MixedRealizationBound(tableView);
			scrollable = GetBodyScroller(tableView)?.ScrollableHeight ?? 0.0;

			Verify.IsLessThan(bound, ManyGroupsCount,
				"Precondition: the collapsed projection must still be far larger than the bound, so headers themselves virtualize.");

			VerifyGroupedRealizationBounded(tableView, bound, "collapsed, at the start", expectRows: false);
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, scrollable / 2.0);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(GetProjectedElements(tableView, requireAllRealized: false)[0],
				"Precondition: the first group header must have left realization.");

			VerifyGroupedRealizationBounded(tableView, bound, "collapsed, halfway", expectRows: false);
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyGroupedRealizationBounded(tableView, bound, "collapsed, at the end", expectRows: false);

			foreach (var header in ProjectedGroupHeaders(tableView))
			{
				Verify.IsFalse(header.IsExpanded, "Every header realized after scrolling must report its group collapsed.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a long hop-scroll over a sorted large source keeps realization and the container pool bounded and every realized row in sorted position.")]
	public async Task VerifyScrollingUnderActiveSortKeepsRealizationBounded()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(TableViewVirtualizationTestHelpers.LargeSourceCount);

			var indexOf = new Dictionary<Person, int>();
			for (var i = 0; i < items.Count; i++)
			{
				indexOf[items[i]] = i;
			}

			var source = TableViewSource.From(items).Sort(o => indexOf[(Person)o], SortDirection.Descending);
			tableView = CreateTableView(source, headers: new[] { "Name", "Role" }, width: 500, height: 240);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		var bound = 0;
		var scrollable = 0.0;
		var leftStart = false;

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(ReferenceEquals(items[items.Count - 1], GetProjectedItem(tableView, 0)),
				"Precondition: the descending sort must be active, so the last source item projects first.");

			bound = TableViewVirtualizationTestHelpers.RealizationBound(tableView);
			scrollable = GetBodyScroller(tableView)?.ScrollableHeight ?? 0.0;
			Verify.IsLessThan(bound, items.Count, "Precondition: the source must be far larger than the bound.");

			VerifySortedRealization(tableView, items, bound, "at the start");
		});

		for (var hop = 1; hop <= 10; hop++)
		{
			await ScrollBodyTo(tableView, ScrollAxis.Vertical, scrollable * hop / 11.0);

			RunOnUIThread.Execute(() =>
			{
				leftStart |= GetProjectedElements(tableView, requireAllRealized: false)[0] == null;
				VerifySortedRealization(tableView, items, bound, $"at hop {hop}");
			});
		}

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(leftStart, "Precondition: the first sorted row must have left realization, so containers were recycled.");

			// A visual-tree count on purpose: it measures the container pool, which grows if recycled rows leak.
			var containers = GetRealizedRows(tableView).Count;
			Log.Comment($"Containers parented after ten hops: {containers}, bound {bound}.");

			Verify.IsLessThanOrEqual(containers, bound,
				$"The container pool must not grow with the distance scrolled under a sort; {containers} containers, bound {bound}.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer is reference-stable while its row stays realized, and that re-acquiring it after a scroll away and back describes the same item and coordinates.")]
	public async Task VerifyCellPeerIdentityIsStableAcrossScrollAwayAndBack()
	{
		TableView tableView = null;
		List<Person> items = null;
		Person target = null;
		TableViewCellAutomationPeer before = null;
		string nameBefore = null;
		const int targetIndex = 1;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			target = items[targetIndex];
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var row = RequireRealizedRowFor(tableView, target);
			if (row == null)
			{
				return;
			}

			before = GetCellPeer(row, 0);

			// TableViewRowAutomationPeer::GetOrCreateCellPeer is the single source of cell-peer identity while realized.
			Verify.IsTrue(ReferenceEquals(before, GetCellPeer(row, 0)),
				"Re-acquiring a realized cell's peer must return the same provider; UIA compares providers by identity.");

			nameBefore = before.GetName();
			Verify.IsTrue(nameBefore != null && nameBefore.Contains(target.Name),
				$"Precondition: the cell peer must name the target item; saw '{nameBefore}'.");
			VerifyCellCoordinates(before, targetIndex, 0, "before scrolling");
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsFalse(RealizedProjectedRows(tableView).Any(row => ReferenceEquals(row.DataContext, target)),
				"Precondition: the target item must have left realization, so its container was recycled."));

		await ScrollToStart(tableView);

		RunOnUIThread.Execute(() =>
		{
			var row = RequireRealizedRowFor(tableView, target);
			if (row == null)
			{
				return;
			}

			var after = GetCellPeer(row, 0);

			// Peers are per container: rehosting another item drops the cell-peer cache, so only value equality holds.
			Log.Comment($"Re-acquired peer is {(ReferenceEquals(before, after) ? "the same" : "a different")} instance.");

			Verify.AreEqual(nameBefore, after.GetName(),
				"The re-acquired cell peer must report the same name for the same item after a scroll away and back.");
			VerifyCellCoordinates(after, targetIndex, 0, "after scrolling away and back");

			Verify.IsTrue(ReferenceEquals(after, GetCellPeer(row, 0)),
				"Once realized again, the cell peer must again be reference-stable across acquisitions.");
		});
	}

	private static TableView CreateManyGroupsTable()
	{
		var items = Enumerable
			.Range(0, ManyGroupsItemCount)
			.Select(i => new Person { Name = $"Person {i}", Role = $"Group {i / ItemsPerGroup}" })
			.ToList();

		var source = TableViewSource.From(items);
		source.GroupBy(o => ((Person)o).Role);

		return CreateTableView(source, headers: new[] { "Name", "Role" }, width: 500, height: 240);
	}

	// RealizationBound's viewport-plus-cache formula, sized by the shortest realized element so shorter group headers cannot exceed it.
	private static int MixedRealizationBound(TableView tableView)
	{
		var heights = GetProjectedElements(tableView, requireAllRealized: false)
			.OfType<FrameworkElement>()
			.Select(element => element.ActualHeight)
			.Where(height => height > 0.0)
			.ToList();

		var scroller = GetBodyScroller(tableView);

		if (heights.Count == 0 || scroller == null || scroller.ViewportHeight <= 0.0)
		{
			Verify.Fail("The fixture must realize sized elements inside a laid-out viewport.");
			return 0;
		}

		var viewportElements = (int)global::System.Math.Ceiling(scroller.ViewportHeight / heights.Min());
		Log.Comment($"Shortest element {heights.Min()}, viewport {scroller.ViewportHeight}, viewport elements {viewportElements}.");

		return (viewportElements * 5) + 4;
	}

	private static void VerifyGroupedRealizationBounded(TableView tableView, int bound, string context, bool expectRows)
	{
		var realized = TableViewVirtualizationTestHelpers.RealizedElementCount(tableView);
		var headers = ProjectedGroupHeaders(tableView).Count;
		var rows = ProjectedRowCount(tableView);
		Log.Comment($"{context}: realized {realized} ({headers} headers, {rows} rows), bound {bound}.");

		Verify.IsGreaterThan(headers, 0, $"Group headers must be realized ({context}).");
		Verify.IsLessThanOrEqual(realized, bound,
			$"Realization, headers included, must stay inside the viewport-plus-cache bound ({context}); realized {realized}, bound {bound}.");

		if (expectRows)
		{
			Verify.IsGreaterThan(rows, 0, $"Data rows must be realized alongside headers ({context}).");
		}
		else
		{
			Verify.AreEqual(0, rows, $"No data row may be realized while every group is collapsed ({context}).");
		}
	}

	private static void VerifySortedRealization(TableView tableView, List<Person> items, int bound, string context)
	{
		var elements = GetProjectedElements(tableView, requireAllRealized: false);
		var realized = elements.Count(element => element != null);
		Log.Comment($"{context}: realized {realized}, bound {bound}.");

		Verify.IsGreaterThan(realized, 0, $"Rows must be realized ({context}).");
		Verify.IsLessThanOrEqual(realized, bound,
			$"Realization must stay bounded under a sort ({context}); realized {realized}, bound {bound}.");

		for (var i = 0; i < elements.Count; i++)
		{
			if (elements[i] is TableViewRow row)
			{
				Verify.IsTrue(ReferenceEquals(items[items.Count - 1 - i], row.DataContext),
					$"The row realized at sorted index {i} must host the item sorted there ({context}).");
			}
		}
	}

	private static TableViewRow RequireRealizedRowFor(TableView tableView, Person item)
	{
		var row = RealizedProjectedRows(tableView).FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, item));
		Verify.IsNotNull(row, $"'{item.Name}' must be realized.");
		return row;
	}

	private static void VerifyCellCoordinates(TableViewCellAutomationPeer cellPeer, int row, int column, string context)
	{
		var gridItem = cellPeer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.GridItem)
			as Microsoft.UI.Xaml.Automation.Provider.IGridItemProvider;

		if (gridItem == null)
		{
			Verify.Fail($"The cell peer must advertise GridItem ({context}).");
			return;
		}

		Verify.AreEqual(row, gridItem.Row, $"The cell peer must report the row of the item it describes ({context}).");
		Verify.AreEqual(column, gridItem.Column, $"The cell peer must report its visible-column index ({context}).");
	}
}

[TestClass]
public class TableViewRecyclingCostTests : TableViewApiTestBase
{
	private const int ScrollSteps = 6;

	[TestMethod]
	[TestProperty("Description", "Verifies a recycled row refreshes its cell chrome once per recycle, not once per ElementClearing / DataContext change / ElementPrepared.")]
	public async Task VerifyRowRecycleRefreshesCellsOnce()
	{
		TableView tableView = null;
		CountingVisualStateManager vsm = null;
		var trackedRows = new HashSet<TableViewRow>();
		int recycles = 0;

		RunOnUIThread.Execute(() =>
		{
			vsm = new CountingVisualStateManager();
			tableView = CreateScrollableTable(MakeManyItems(RecyclingSourceCount));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// Each rebuild raises a RowHierarchyStates GoToState; a custom VSM on the template root counts them.
			foreach (var row in GetRealizedRows(tableView))
			{
				var root = VisualTreeHelper.GetChildrenCount(row) > 0 ? VisualTreeHelper.GetChild(row, 0) as FrameworkElement : null;
				Verify.IsNotNull(root, "Precondition: a realized row must have its template applied.");
				VisualStateManager.SetCustomVisualStateManager(root, vsm);
				trackedRows.Add(row);
			}
			Verify.IsGreaterThan(trackedRows.Count, 0, "Precondition: rows must realize.");

			GetRowsRepeater(tableView).ElementPrepared += (s, e) =>
			{
				if (e.Element is TableViewRow row && trackedRows.Contains(row))
				{
					++recycles;
				}
			};
		});

		await ScrollInSteps(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(recycles, 0, "Precondition: scrolling must recycle tracked rows.");

			// One RebuildCells pass plus RefreshRowHierarchyState's own; the slack absorbs incidental refreshes.
			const int expectedPerRecycle = 2;
			const int slack = 8;
			var ratio = (double)vsm.HierarchyStateCalls / recycles;
			Log.Comment($"Recycles={recycles} RowHierarchyStates GoToState calls={vsm.HierarchyStateCalls} perRecycle={ratio:F2}");
			Verify.IsLessThanOrEqual(vsm.HierarchyStateCalls, (expectedPerRecycle * recycles) + slack,
				$"A recycled row must refresh its cells once per recycle; saw {ratio:F2} hierarchy refreshes per recycle (expected <= {expectedPerRecycle}).");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies rows that were pooled while the columns were reordered show the new column order when they are recycled back in.")]
	public async Task VerifyColumnReorderWhileRowsArePooledReachesRecycledRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(MakeManyItems(RecyclingSourceCount));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);
		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			var first = tableView.Columns[0];
			tableView.Columns.RemoveAt(0);
			tableView.Columns.Add(first);
		});

		await SettleLayout(tableView);
		await ScrollToStart(tableView);
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var rows = RealizedProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must realize after scrolling back.");

			foreach (var row in rows)
			{
				var item = (Person)row.DataContext;
				Verify.IsTrue(GetRowCellColumns(row).SequenceEqual(tableView.Columns),
					$"'{item.Name}' must lay its cells out in the reordered column order.");
				Verify.AreEqual(item.Role, GetCellText(GetRowCell(row, 0)), $"'{item.Name}' must show Role first after the reorder.");
				Verify.AreEqual(item.Name, GetCellText(GetRowCell(row, 1)), $"'{item.Name}' must show Name second after the reorder.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies rows that were pooled while a column's Visibility toggled show the current visibility when they are recycled back in.")]
	public async Task VerifyColumnVisibilityToggleWhileRowsArePooledReachesRecycledRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(MakeManyItems(RecyclingSourceCount));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		foreach (var visibility in new[] { Visibility.Collapsed, Visibility.Visible })
		{
			await ScrollToEnd(tableView);
			RunOnUIThread.Execute(() => tableView.Columns[1].Visibility = visibility);
			await SettleLayout(tableView);
			await ScrollToStart(tableView);
			await SettleLayout(tableView);

			RunOnUIThread.Execute(() =>
			{
				var rows = RealizedProjectedRows(tableView);
				Verify.IsGreaterThan(rows.Count, 0, "Rows must realize after scrolling back.");

				foreach (var row in rows)
				{
					var item = (Person)row.DataContext;
					Verify.AreEqual(item.Name, GetCellText(GetRowCell(row, 0)), $"'{item.Name}' must show its own item.");
					Verify.AreEqual(visibility, GetRowCell(row, 1).Visibility,
						$"'{item.Name}' must carry the Role column's current visibility ({visibility}).");
				}
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer acquired before its row was recycled no longer describes the outgoing item.")]
	public async Task VerifyRecycledOutRowCellPeerDoesNotDescribeTheOldItem()
	{
		TableView tableView = null;
		List<Person> items = null;
		TableViewRow row = null;
		TableViewCellAutomationPeer before = null;
		Person target = null;
		bool preparedForAnotherItem = false;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			target = items[1];
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			row = RealizedProjectedRows(tableView).FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, target));
			Verify.IsNotNull(row, "Precondition: the target must be realized.");
			before = GetCellPeer(row, 0);
			Verify.IsTrue(NamesItem(before, target), "Precondition: the cell peer must name the target item.");

			GetRowsRepeater(tableView).ElementPrepared += (s, e) =>
			{
				if (ReferenceEquals(e.Element, row) && !ReferenceEquals(row.DataContext, target))
				{
					preparedForAnotherItem = true;
				}
			};
		});

		await ScrollToEnd(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(preparedForAnotherItem && !ReferenceEquals(row.DataContext, target),
				"Precondition: the target's row must have been re-prepared for a different item.");
			Verify.IsFalse(NamesItem(before, target),
				$"A cell peer of a recycled-out row must not still describe '{target.Name}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a CellTemplate change raised while a recycled row rebuilds for its new item still leaves that row with built cells after it is prepared.")]
	public async Task VerifyCellTemplateChangeDuringRecycleRebuildLeavesCellsBuilt()
	{
		TableView tableView = null;
		TableViewTemplateColumn templateColumn = null;
		CountingVisualStateManager vsm = null;
		TableViewRow changedDuring = null;
		int cellsAtPrepare = -1;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateMixedColumnTable(MakeManyItems(RecyclingSourceCount));
			templateColumn = (TableViewTemplateColumn)tableView.Columns[1];
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var repeater = GetRowsRepeater(tableView);
			vsm = new CountingVisualStateManager
			{
				// The repeater sets a recycled row's DataContext before giving it an index, so this
				// fires inside the DataContext-driven rebuild, ahead of ElementPrepared.
				OnHierarchyState = control =>
				{
					if (changedDuring == null && control is TableViewRow row &&
						row.DataContext != null && repeater.GetElementIndex(row) < 0)
					{
						changedDuring = row;
						templateColumn.CellTemplate = CreateBoundTextTemplate("Role");
					}
				}
			};

			foreach (var row in GetRealizedRows(tableView))
			{
				var root = VisualTreeHelper.GetChildrenCount(row) > 0 ? VisualTreeHelper.GetChild(row, 0) as FrameworkElement : null;
				Verify.IsNotNull(root, "Precondition: a realized row must have its template applied.");
				VisualStateManager.SetCustomVisualStateManager(root, vsm);
			}

			// Subscribed after TableView's own handler, so this observes the row once prepare has finished;
			// a later recycle would rebuild the row and hide the defect.
			repeater.ElementPrepared += (s, e) =>
			{
				if (cellsAtPrepare < 0 && changedDuring != null && ReferenceEquals(e.Element, changedDuring))
				{
					cellsAtPrepare = GetCellsHost(changedDuring).Children.Count;
				}
			};
		});

		await ScrollInSteps(tableView);
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(changedDuring, "Precondition: the CellTemplate must change while a recycled row rebuilds for its new item.");
			Verify.AreEqual(tableView.Columns.Count, cellsAtPrepare,
				"The row rebuilding when the CellTemplate changed must have a cell per column once it is prepared.");

			var rows = RealizedProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must realize after scrolling.");

			foreach (var row in rows)
			{
				var item = (Person)row.DataContext;
				Verify.AreEqual(tableView.Columns.Count, GetCellsHost(row).Children.Count, $"'{item.Name}' must have a cell per column.");
				Verify.AreEqual(item.Name, GetCellText(GetRowCell(row, 0)), $"'{item.Name}' must show its name.");
				Verify.AreEqual(item.Role, GetCellText(GetRowCell(row, 1)), $"'{item.Name}' must show the new CellTemplate.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer held across its row's recycle does not advertise VirtualizedItem while the row is pooled.")]
	public async Task VerifyHeldCellPeerDoesNotAdvertiseVirtualizedItemAfterRecycle()
	{
		TableView tableView = null;
		List<Person> items = null;
		TableViewRow row = null;
		TableViewCellAutomationPeer peer = null;
		Person target = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(RecyclingSourceCount);
			tableView = CreateScrollableTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// The last realized row: shrinking the viewport pools it without handing it a new item.
			row = RealizedProjectedRows(tableView).OrderByDescending(candidate => items.IndexOf((Person)candidate.DataContext)).First();
			target = (Person)row.DataContext;
			peer = GetCellPeer(row, 0);
			Verify.IsNull(peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.VirtualizedItem),
				"Precondition: a realized cell must not advertise VirtualizedItem.");
			tableView.Height = 60;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(RealizedProjectedRows(tableView).Any(candidate => ReferenceEquals(candidate.DataContext, target)),
				$"Precondition: '{target.Name}' must leave the realized range.");
			Verify.IsNull(row.DataContext, "Precondition: the target's row must be pooled, not re-bound to another item.");
			Verify.IsNull(peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.VirtualizedItem),
				"A cell peer whose row was recycled cannot be realized again, so it must not advertise VirtualizedItem.");
		});
	}

	private static bool NamesItem(TableViewCellAutomationPeer peer, Person item)
	{
		string name;
		try
		{
			name = peer.GetName();
		}
		catch (global::System.Exception)
		{
			// ElementNotAvailable: the peer no longer describes anything.
			return false;
		}
		return name != null &&
			global::System.Text.RegularExpressions.Regex.IsMatch(name, $@"\b{(global::System.Text.RegularExpressions.Regex.Escape(item.Name))}\b");
	}

	private static async Task ScrollInSteps(TableView tableView)
	{
		var extent = 0.0;
		RunOnUIThread.Execute(() => extent = GetBodyScroller(tableView).ScrollableHeight);
		Verify.IsGreaterThan(extent, 1.0, "The fixture must overflow its viewport, or nothing is recycled.");

		for (int step = 1; step <= ScrollSteps; ++step)
		{
			await ScrollBodyTo(tableView, ScrollAxis.Vertical, (extent - 1.0) * step / ScrollSteps);
		}
	}

	private sealed class CountingVisualStateManager : VisualStateManager
	{
		public int HierarchyStateCalls { get; private set; }

		public global::System.Action<Control> OnHierarchyState { get; set; }

		protected override bool GoToStateCore(Control control, FrameworkElement templateRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
		{
			if (stateName == "RowHierarchical" || stateName == "RowNotHierarchical")
			{
				++HierarchyStateCalls;
				OnHierarchyState?.Invoke(control);
			}
			return base.GoToStateCore(control, templateRoot, stateName, group, state, useTransitions);
		}
	}
}

internal static class TableViewRecyclingTestHelpers
{
	internal const int RecyclingSourceCount = 120;

	internal const string OldMarker = "A:";
	internal const string NewMarker = "B:";

	internal static string FirstGroupKey => "Role 0";

	internal static TableView CreateScrollableTable(IList<Person> items)
		=> CreateTableView(items, headers: new[] { "Name", "Role" }, width: 500, height: 240);

	internal static TableView CreateScrollableGroupedTable()
	{
		var source = TableViewSource.From(MakeManyItems(RecyclingSourceCount));
		source.GroupBy(o => ((Person)o).Role);

		return CreateTableView(source, headers: new[] { "Name", "Role" }, width: 500, height: 240);
	}

	internal static DataTemplate CreateRecyclingMarkerTemplate(string marker) => (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
		$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
			   <StackPanel Orientation=""Horizontal"">
				   <TextBlock Text=""{marker}"" />
				   <TextBlock Text=""{{Binding KeyText}}"" />
			   </StackPanel>
		   </DataTemplate>");

	internal static async Task ScrollToEnd(TableView tableView)
	{
		var extent = 0.0;
		RunOnUIThread.Execute(() => extent = GetBodyScroller(tableView)?.ScrollableHeight ?? 0.0);

		Verify.IsGreaterThan(extent, 1.0, "The fixture must overflow its viewport, or nothing is recycled.");

		// One pixel short: ScrollBodyTo requires the extent to be strictly greater than the offset.
		await ScrollBodyTo(tableView, ScrollAxis.Vertical, extent - 1.0);
	}

	internal static Task ScrollToStart(TableView tableView) => ScrollBodyTo(tableView, ScrollAxis.Vertical, 0.0);

	// Not GetRealizedRows: it walks the visual tree and also returns pooled containers with a null DataContext.
	internal static List<TableViewRow> RealizedProjectedRows(TableView tableView)
		=> GetProjectedElements(tableView, requireAllRealized: false)
			.OfType<TableViewRow>()
			.ToList();

	// requireAllRealized:false throughout (and no GetGroupHeader/GetGroupHeaderPeer): a virtualizing fixture never has every element realized.
	internal static List<TableViewGroupHeader> ProjectedGroupHeaders(TableView tableView)
		=> GetProjectedElements(tableView, requireAllRealized: false)
			.OfType<TableViewGroupHeader>()
			.ToList();

	internal static int ProjectedRowCount(TableView tableView)
		=> GetProjectedElements(tableView, requireAllRealized: false)
			.OfType<TableViewRow>()
			.Count();

	// Scoped to the ContentPresenter so the expander glyph stays out of the text.
	internal static List<string> ProjectedGroupHeaderTexts(TableView tableView)
	{
		var texts = new List<string>();

		foreach (var header in ProjectedGroupHeaders(tableView))
		{
			var presenter = FindVisualChildrenByType<ContentPresenter>(header).FirstOrDefault();

			if (presenter == null)
			{
				continue;
			}

			var text = string.Concat(FindVisualChildrenByType<TextBlock>(presenter).Select(block => block.Text));

			if (!string.IsNullOrEmpty(text))
			{
				texts.Add(text);
			}
		}

		return texts;
	}

	internal static string Describe(IEnumerable<string> texts) => string.Join(" | ", texts);
}
