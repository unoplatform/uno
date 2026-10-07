// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_AutomationPeer_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

// TODO Uno: Uno's internal CalendarView RowOrColumnMajor in Microsoft.UI.Xaml.Controls is visible here through InternalsVisibleTo.
using RowOrColumnMajor = Microsoft.UI.Xaml.Automation.RowOrColumnMajor;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewAutomationTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 12 of the TableView API test plan: accessibility and automation.
//
// Expectations here come from TableView.idl:617-690, which declares the exact provider interface
// list for each of the five peers, and from TableView-dev-spec.md:329, which states that the UIA
// grid model is on a VISIBLE-column basis - a deliberate divergence from WPF's
// DataGridAutomationPeer. Where neither says anything, the test's comment says so.
//
// Two structural facts shape every test in this file:
//
//   * The column header peer is virtual. Its Owner() is the TableView, not a header element, and
//     it is manufactured per column by TableViewAutomationPeer::GetOrCreateColumnHeaderPeer. The
//     table-level route (IGridProvider.GetColumnHeaders) returns IRawElementProviderSimple and
//     there is no public provider->peer conversion, so tests that assert on header naming, help
//     text or set position construct the peer directly. TableView.idl:670 makes that constructor
//     public for exactly this reason.
//
//   * Providers are opaque to a C# test. Anything typed IRawElementProviderSimple can only be
//     asserted for null / non-null / count / reference identity. No test in this repo prises one
//     open with raw UIA pattern ids, and these do not start.
//
// NOT COVERED HERE, deliberately:
//   * The Value pattern on cells - Category 10 owns the advertisement gate in full (four tests).
//   * Tooltip -> HelpText mapping and its duplicate suppression - Category 14. This file keeps
//     only the sort-state half of header help text, which has no tooltip in play.
//   * "Invoke sorts the column" - Category 7.5 owns the positive path; this file owns the
//     withholding. Rule of thumb: what happens to the DATA is Category 7, what the PEER
//     ADVERTISES is Category 12.
//   * Automation events (structure-changed, ExpandCollapseState property-changed). The product
//     gates every raise on AutomationPeer.ListenerExists(...), which is false unless a real UIA
//     client is attached. An API test cannot attach one, so such a test would pass identically if
//     the events were deleted. See section 12.6 of the plan.
[TestClass]
public class TableViewAutomationPeerTests : TableViewApiTestBase
{
	// ---------- 12.1 TableView peer patterns and identity ----------

	[TestMethod]
	[TestProperty("Description", "Verifies the TableView peer advertises Grid, Table and ItemContainer, and answers each as itself.")]
	public async Task VerifyTableViewPeerAdvertisesGridTableAndItemContainer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);

			// TableView.idl:617-624 declares IGridProvider, ITableProvider, ISelectionProvider and
			// IItemContainerProvider on the runtimeclass. Grid, Table and ItemContainer describe
			// the control's shape rather than its state, so they are unconditional.
			foreach (var pattern in new[] { PatternInterface.Grid, PatternInterface.Table, PatternInterface.ItemContainer })
			{
				var provider = peer.GetPattern(pattern);
				if (provider == null)
				{
					Verify.Fail($"The TableView peer must advertise the {pattern} pattern; TableView.idl:617-624 declares it.");
					continue;
				}

				// Structural patterns are implemented by the peer itself, not forwarded, so an
				// AT client that casts the provider back gets the peer it queried.
				Verify.IsTrue(ReferenceEquals(provider, peer),
					$"The {pattern} pattern should be answered by the TableView peer itself.");
			}

			Verify.IsNotNull(peer.GetPattern(PatternInterface.Grid) as IGridProvider, "Grid must cast to IGridProvider.");
			Verify.IsNotNull(peer.GetPattern(PatternInterface.Table) as ITableProvider, "Table must cast to ITableProvider.");
			Verify.IsNotNull(peer.GetPattern(PatternInterface.ItemContainer) as IItemContainerProvider, "ItemContainer must cast to IItemContainerProvider.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Selection pattern is advertised under SelectionMode.Single and withheld under None.")]
	public async Task VerifySelectionPatternTracksSelectionMode()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);

			// TableView.idl:619 declares ISelectionProvider, so the positive half is the published
			// contract.
			Verify.IsNotNull(peer.GetPattern(PatternInterface.Selection) as ISelectionProvider,
				"Selection must be advertised while SelectionMode is Single.");

			tableView.SelectionMode = TableViewSelectionMode.None;
			tableView.UpdateLayout();

			// The conditional withholding is NOT stated in the IDL or the design spec - it is a
			// product-code rationale that matches UIA guidance (do not advertise a pattern you
			// cannot honour). Flagged in the plan as (needs spec decision): a reader of the IDL
			// alone would conclude the pattern is unconditional.
			Verify.IsNull(peer.GetPattern(PatternInterface.Selection),
				"Selection must be withheld while SelectionMode is None; advertising it tells an AT client the grid is selectable when every Select() would be refused.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the selection provider reports single-select semantics and the current selection.")]
	public async Task VerifySelectionProviderReportsSingleSelectSemantics()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateAutomationTable(items);
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var selection = peer.GetPattern(PatternInterface.Selection) as ISelectionProvider;
			if (selection == null)
			{
				Verify.Fail("Selection must be advertised while SelectionMode is Single.");
				return;
			}

			// TableView.idl:531-554 limits SelectionMode to None/Single this release, which makes
			// CanSelectMultiple == false a contract rather than an implementation detail.
			Verify.IsFalse(selection.CanSelectMultiple,
				"CanSelectMultiple must be false; true makes AT offer multi-select affordances the control refuses.");

			// DeselectAll() is public, so selection can always be empty.
			Verify.IsFalse(selection.IsSelectionRequired,
				"IsSelectionRequired must be false; DeselectAll() is public, so an empty selection is always reachable.");

			var empty = selection.GetSelection();
			Verify.AreEqual(0, empty == null ? 0 : empty.Length, "With nothing selected the selection array must be empty.");

			// Row 1 is realized at this size, so this asserts the selection report rather than the
			// virtualization behaviour - a selected row scrolled out of realization reports as
			// unselected here by design.
			tableView.Select(1);
			tableView.UpdateLayout();

			var selected = selection.GetSelection();
			Verify.AreEqual(1, selected == null ? 0 : selected.Length,
				"Exactly one provider must be reported for a realized selected row.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Scroll pattern is answered by the body scroller's peer and never by the TableView peer itself.")]
	public async Task VerifyScrollPatternIsForwardedToBodyScrollerNotThePeer()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var provider = peer.GetPattern(PatternInterface.Scroll);

			// Narrator scrolls a grid through this pattern; without it every row below the fold is
			// unreachable by AT.
			if (provider == null)
			{
				Verify.Fail("The TableView peer must answer the Scroll pattern so AT can scroll to unrealized rows.");
				return;
			}

			// The negative half is IDL-backed: TableView.idl:617-624 lists four provider interfaces
			// and IScrollProvider is NOT among them, so the peer must not answer as itself. A peer
			// claiming a pattern it does not implement fails at the cast in every client.
			Verify.IsFalse(ReferenceEquals(provider, peer),
				"Scroll must be forwarded to the body scroller's peer; TableView.idl does not declare IScrollProvider on the TableView peer.");
			Verify.IsNotNull(provider as IScrollProvider,
				"The forwarded Scroll provider must cast to IScrollProvider.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the control types of the table, row and cell peers.")]
	public async Task VerifyTableViewPeerControlTypeIsDataGrid()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateAutomationTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var tablePeer = RequireTablePeer(tableView);
			Verify.AreEqual(AutomationControlType.DataGrid, tablePeer.GetAutomationControlType(),
				"The TableView peer must report DataGrid; anything else loses table reading mode in Narrator.");

			var rowPeer = RequireRowPeer(tableView, 0);
			// TableView-dev-spec.md:170 states the row peer is a DataItem outright.
			Verify.AreEqual(AutomationControlType.DataItem, rowPeer.GetAutomationControlType(),
				"The row peer must report DataItem.");

			var cellPeer = GetCellPeer(rowPeer, 0);
			Verify.AreEqual(AutomationControlType.DataItem, cellPeer.GetAutomationControlType(),
				"The cell peer must report DataItem so Narrator reads the composed cell name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies all five TableView peers report non-empty, pairwise distinct class names aligned to their owner type.")]
	public async Task VerifyPeerClassNamesAreDistinctAndOwnerAligned()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var tablePeer = RequireTablePeer(tableView);
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);
			var headerPeer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var groupHeaderPeer = GetGroupHeaderPeer(tableView, 0);

			var names = new Dictionary<string, string>
			{
				{ "TableView", tablePeer.GetClassName() },
				{ "TableViewRow", rowPeer.GetClassName() },
				{ "TableViewCell", cellPeer.GetClassName() },
				{ "TableViewColumnHeader", headerPeer.GetClassName() },
				{ "TableViewGroupHeader", groupHeaderPeer.GetClassName() },
			};

			foreach (var entry in names)
			{
				Log.Comment($"{entry.Key} peer class name: '{entry.Value}'");
				Verify.IsFalse(string.IsNullOrEmpty(entry.Value), $"The {entry.Key} peer must report a class name.");

				// Deliberately asserts the SUFFIX, not the exact qualification. The five peers are
				// inconsistent today: TableView and TableViewRow use hstring_name_of<T>() (the
				// repo-wide convention), while the cell, column-header and group-header peers
				// return hardcoded short literals. Pinning one exact form would freeze half of
				// them into the wrong convention, so the plan carries this as
				// (needs spec decision): pick one, then tighten this to equality.
				Verify.IsTrue(entry.Value != null && entry.Value.EndsWith(entry.Key, StringComparison.Ordinal),
					$"The {entry.Key} peer's class name should end in its owner's type name; saw '{entry.Value}'.");
			}

			// Two peers sharing a class name makes them indistinguishable to AT scripts and to
			// UIA-driven tooling, which commonly select by class name.
			Verify.AreEqual(names.Count, names.Values.Distinct().Count(),
				"All five peer class names must be pairwise distinct.");
		});
	}

	// ---------- 12.2 Grid coordinates ----------

	[TestMethod]
	[TestProperty("Description", "Verifies RowCount spans the whole item source and ColumnCount counts visible columns.")]
	public async Task VerifyGridRowAndColumnCountsMatchVisibleGrid()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(10));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// Not stated in the spec: reasoned from the UIA grid contract, which is defined over
			// the logical grid rather than the realized window. If RowCount reported only realized
			// rows, Narrator would announce "row 3 of 7" on a 200-item grid and stop navigating at
			// the realization boundary.
			Verify.AreEqual(10, grid.RowCount, "RowCount must span the whole item source, not just realized rows.");
			Verify.AreEqual(3, grid.ColumnCount, "ColumnCount must report the visible column count.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem returns a provider for a realized cell.")]
	public async Task VerifyGridGetItemReturnsProviderForRealizedCell()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// The most basic grid navigation there is: "move to column 2" on a row that is on screen.
			Verify.IsNotNull(grid.GetItem(0, 1), "GetItem must return a provider for a realized cell.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem realizes a row far outside the realization window.")]
	public async Task VerifyGridGetItemRealizesOffscreenRow()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable(TableViewRowTestHelpers.MakeManyItems(200));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// Spec silent. Reasoned from the UIA grid contract, which has no notion of realization:
			// the alternative (returning null) would need VirtualizedItemPattern to be honest, and
			// the control does not implement it. The realization is deliberately bounded to the one
			// requested row, so this asserts reachability, never "the whole source realized".
			Verify.IsNotNull(grid.GetItem(150, 0),
				"GetItem must realize the requested row on demand; otherwise AT can only reach rows the user already scrolled to.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IGridProvider.GetItem returns null for out-of-range coordinates without crashing.")]
	public async Task VerifyGridGetItemOutOfRangeReturnsNull()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);
			int rowCount = grid.RowCount;
			int columnCount = grid.ColumnCount;

			// UIA permits either null or a failure HRESULT here. This pins null, which is the safer
			// of the two and the one a C# caller can handle; the plan carries it as
			// (needs spec decision). AT clients do probe grid bounds, so the real assertion is that
			// probing neither crashes the app nor corrupts the realization window.
			Verify.IsNull(grid.GetItem(-1, 0), "A negative row index must return null.");
			Verify.IsNull(grid.GetItem(0, -1), "A negative column index must return null.");
			Verify.IsNull(grid.GetItem(rowCount, 0), "A row index at RowCount must return null.");
			Verify.IsNull(grid.GetItem(0, columnCount), "A column index at ColumnCount must return null.");

			Verify.IsNotNull(grid.GetItem(0, 0), "The grid must still be usable after out-of-range probes.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a collapsed column is absent from ColumnCount and from the row peer's children.")]
	public async Task VerifyHiddenColumnsAreSkippedInGridCoordinates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[1].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var grid = RequireGridProvider(tableView);

			// TableView-dev-spec.md:329 states this outright, including that it intentionally
			// diverges from WPF's DataGridAutomationPeer, which indexes the full collection.
			Verify.AreEqual(2, grid.ColumnCount, "A collapsed column must not be counted in the UIA grid.");

			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();
			Verify.AreEqual(2, children == null ? 0 : children.Count,
				"The row peer must expose one cell per VISIBLE column; a zero-width invisible cell would make Narrator stop on nothing.");

			// The deliberate asymmetry worth knowing: the visual layer still generates a hidden cell
			// per collapsed column, so the cell-host child count and the UIA child count legitimately
			// differ. Do not "fix" a failure here by counting panel children.
			var cellsHost = GetCellsHost(GetRealizedRows(tableView)[0]);
			Verify.AreEqual(3, cellsHost.Children.Count,
				"The visual layer keeps a cell per column; only the UIA projection filters to visible columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing a column shifts the GridItem.Column of the columns after it.")]
	public async Task VerifyHiddenColumnShiftsSubsequentColumnIndices()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[0].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();
			if (children == null || children.Count != 2)
			{
				Verify.Fail($"Expected 2 visible cells after collapsing the first column; saw {children?.Count ?? 0}.");
				return;
			}

			// Same spec sentence as the count test (TableView-dev-spec.md:329), different
			// assertion: that test proves the SIZE of the model, this proves the MAPPING. A
			// regression that renumbers without recounting breaks exactly one of them, which is why
			// both are kept.
			var first = children[0].GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			var second = children[1].GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (first == null || second == null)
			{
				Verify.Fail("Both remaining cells must advertise GridItem.");
				return;
			}

			Verify.AreEqual(0, first.Column, "The first visible column must report index 0 once the column before it is collapsed.");
			Verify.AreEqual(1, second.Column, "Indices to the right must shift down by one, so they stay inside 0..ColumnCount-1.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the table provider reports RowMajor and no row headers.")]
	public async Task VerifyTableProviderReportsRowMajorAndNoRowHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var table = peer.GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			// Spec silent on RowOrColumnMajor; reasoned from the control being row-oriented
			// throughout (TableView-dev-spec.md:165 describes keyboard handling as row-oriented).
			// ColumnMajor would make Narrator read down columns instead of across rows.
			Verify.AreEqual(RowOrColumnMajor.RowMajor, table.RowOrColumnMajor, "A row-oriented grid must report RowMajor.");

			// TableView has no row-header element at all, so a non-empty array would hold providers
			// for elements that do not exist.
			var rowHeaders = table.GetRowHeaders();
			Verify.AreEqual(0, rowHeaders == null ? 0 : rowHeaders.Length, "TableView has no row headers.");
		});
	}

	// ---------- 12.3 Cell and row peers ----------

	[TestMethod]
	[TestProperty("Description", "Verifies a row peer exposes one cell peer per visible column, in visible-column order.")]
	public async Task VerifyRowPeerExposesOneCellPerVisibleColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[1].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var children = rowPeer.GetChildren();

			// TableView-dev-spec.md:170 states the row peer exposes its cell peers as children.
			// Without them the row is an opaque leaf: AT reaches the row but not the values in it.
			if (children == null || children.Count != 2)
			{
				Verify.Fail($"The row peer must expose one cell peer per visible column; saw {children?.Count ?? 0}.");
				return;
			}

			// Assert the cast rather than using `as` and skipping - a null child must fail loudly.
			for (int i = 0; i < children.Count; i++)
			{
				Verify.IsTrue(children[i] is TableViewCellAutomationPeer,
					$"Row peer child {i} must be a TableViewCellAutomationPeer.");
			}

			// Visible-column order: the collapsed column is the middle one, so the two survivors are
			// the first and third columns' cells. Asserted through their composed names, which carry
			// the owning column's header.
			Verify.AreEqual("Name, Asha", ((TableViewCellAutomationPeer)children[0]).GetName(),
				"The first cell peer must belong to the first visible column.");
			Verify.AreEqual("Alias, Asha", ((TableViewCellAutomationPeer)children[1]).GetName(),
				"The second cell peer must belong to the third column, the next visible one.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer reports its grid coordinates and unit spans.")]
	public async Task VerifyCellPeerGridItemCoordinates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 1);
			var cellPeer = GetCellPeer(rowPeer, 2);

			// TableView.idl:676-683 declares IGridItemProvider on the cell peer.
			var gridItem = cellPeer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (gridItem == null)
			{
				Verify.Fail("The cell peer must advertise GridItem.");
				return;
			}

			// A cell reporting a position inconsistent with the one GetItem used to find it makes
			// Narrator's "row 2, column 3" contradict where the user actually is.
			Verify.AreEqual(1, gridItem.Row, "The cell must report the row index it was reached through.");
			Verify.AreEqual(2, gridItem.Column, "The cell must report its visible-column index.");

			// Spans of 1 are not spec'd; TableView has no merged cells, so anything else would be
			// meaningless.
			Verify.AreEqual(1, gridItem.RowSpan, "Cells never span rows.");
			Verify.AreEqual(1, gridItem.ColumnSpan, "Cells never span columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell peer reports its containing grid and exactly one column header, and no row headers.")]
	public async Task VerifyCellPeerTableItemReportsContainingGridAndColumnHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 1);

			// TableView.idl:676-683 declares ITableItemProvider. Only non-null and count are
			// assertable - IRawElementProviderSimple is opaque to a C# test.
			var tableItem = cellPeer.GetPattern(PatternInterface.TableItem) as ITableItemProvider;
			if (tableItem == null)
			{
				Verify.Fail("The cell peer must advertise TableItem.");
				return;
			}

			var gridItem = cellPeer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			Verify.IsNotNull(gridItem?.ContainingGrid, "A cell must report the grid that contains it.");

			// This is how a user orients themselves in an unfamiliar table: Narrator announces which
			// column the cell belongs to.
			var columnHeaders = tableItem.GetColumnHeaderItems();
			Verify.AreEqual(1, columnHeaders == null ? 0 : columnHeaders.Length,
				"A cell must report exactly one column header.");

			var rowHeaders = tableItem.GetRowHeaderItems();
			Verify.AreEqual(0, rowHeaders == null ? 0 : rowHeaders.Length, "TableView has no row headers.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell's automation name is '{column header}, {cell value}'.")]
	public async Task VerifyCellNameCombinesColumnHeaderAndValue()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);

			// TableView.idl:676 specifies the format outright: the cell peer supplies
			// "{column header}, {cell value}" names. Without the prefix, moving across a row reads
			// "Asha, Designer, 30" with no indication of what those are.
			//
			// The separator is a literal comma + space and is not localized. That is worth raising,
			// but the test pins what the IDL says.
			Verify.AreEqual("Name, Asha", cellPeer.GetName(),
				"The cell name must combine the column header and the cell value.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell name degrades to the surviving part when the header or the value is missing.")]
	public async Task VerifyCellNameFallsBackWhenEitherPartIsMissing()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var items = new List<Person> { new Person { Name = string.Empty, Role = "Designer" } };

			tableView = CreateTableViewShell(items, 500, 300);

			// A non-string header has no textual prefix. It must be an object that is neither
			// IStringable nor a String-typed IPropertyValue - a boxed primitive is NOT one, because
			// PropertyValue implements IStringable and stringifies happily.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new Border { Width = 20, Height = 12 },
				Binding = new Binding { Path = new PropertyPath("Role"), Mode = BindingMode.OneWay },
			});

			// A string header over an empty value.
			tableView.Columns.Add(MakeTextColumn("Name"));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);

			// Spec states the composed form but not the degenerate cases; reasoned from the composed
			// form being meaningless when a part is empty. A dangling ", " is announced literally by
			// Narrator ("comma Designer"), and a name that is only punctuation is worse than none.
			Verify.AreEqual("Designer", GetCellPeer(rowPeer, 0).GetName(),
				"With no string header the value must stand alone, with no leading separator.");
			Verify.AreEqual("Name", GetCellPeer(rowPeer, 1).GetName(),
				"With an empty value the header must stand alone, with no trailing separator.");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: template-column cell names read the generated ContentPresenter, not the template root. Re-enable once cell naming uses the template content.
	[Ignore("WinUI ignores this test: Product bug: template-column cell names read the generated ContentPresenter, not the template root. Re-enable once cell naming uses the template content.")]
	[TestProperty("Description", "Verifies a template column's cell name uses the template content's automation name.")]
	public async Task VerifyTemplateColumnCellNameUsesContentAutomationName()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);
			tableView.Columns.Add(new TableViewTemplateColumn
			{
				Header = "Status",
				CellTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                              <Border AutomationProperties.Name=""Active"" Width=""40"" Height=""20"" />
                          </DataTemplate>"),
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);
			var cellPeer = GetCellPeer(rowPeer, 0);

			// Spec silent; reasoned from the text-column case being named and template columns having
			// no reason to be a second-class citizen. A cell holding a button, a rating or a status
			// glyph is a common shape, and it must not read as nothing.
			Verify.AreEqual("Status, Active", cellPeer.GetName(),
				"A template column's cell name must incorporate the template content's automation name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the row peer advertises SelectionItem under SelectionMode.Single and withholds it under None.")]
	public async Task VerifyRowPeerSelectionItemPatternTracksSelectionMode()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rowPeer = RequireRowPeer(tableView, 0);

			// TableView.idl:629-633 declares ISelectionItemProvider on the row peer unconditionally,
			// so - as with the table-level selection test - the withholding half is reasoned from
			// UIA guidance rather than stated, and carries the same (needs spec decision).
			Verify.IsNotNull(rowPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider,
				"SelectionItem must be advertised while SelectionMode is Single.");

			tableView.SelectionMode = TableViewSelectionMode.None;
			tableView.UpdateLayout();

			Verify.IsNull(RequireRowPeer(tableView, 0).GetPattern(PatternInterface.SelectionItem),
				"SelectionItem must be withheld under SelectionMode.None; otherwise AT offers a per-row select the control refuses.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies row peers report IsSelected in step with the control's selection.")]
	public async Task VerifyRowPeerIsSelectedMatchesControlSelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			tableView.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			for (int i = 0; i < 3; i++)
			{
				var provider = RequireSelectionItemProvider(tableView, i);
				if (provider == null)
				{
					return;
				}

				Verify.AreEqual(i == 1, provider.IsSelected,
					$"Row {i} must report IsSelected == {i == 1}; otherwise Narrator announces the wrong row as selected.");
			}

			tableView.DeselectAll();
			tableView.UpdateLayout();

			for (int i = 0; i < 3; i++)
			{
				var provider = RequireSelectionItemProvider(tableView, i);
				if (provider == null)
				{
					return;
				}

				Verify.IsFalse(provider.IsSelected, $"Row {i} must report unselected after DeselectAll().");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies row-peer Select() makes the row the selection and AddToSelection() does not toggle it off.")]
	public async Task VerifyRowPeerSelectDrivesTheControlAndAddToSelectionDoesNotToggle()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = RequireSelectionItemProvider(tableView, 2);
			if (provider == null)
			{
				return;
			}

			provider.Select();
			tableView.UpdateLayout();
			Verify.AreEqual(2, tableView.SelectedIndex, "Select() on a row peer must make that row the control's selection.");

			// UIA forbids Select() from meaning "toggle": a client calling it twice would clear the
			// selection. The single-select AddToSelection == Select mapping follows in-box
			// precedent (ListViewItemAutomationPeer behaves the same rather than failing).
			provider.AddToSelection();
			tableView.UpdateLayout();
			Verify.AreEqual(2, tableView.SelectedIndex, "AddToSelection() on the already selected row must leave it selected, not toggle it off.");
			Verify.IsTrue(provider.IsSelected, "The row must still report itself selected.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies RemoveFromSelection on a row that is not the current selection is a no-op.")]
	public async Task VerifyRowPeerRemoveFromSelectionOnlyClearsWhenItIsTheSelection()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(1);
			tableView.UpdateLayout();
			Verify.AreEqual(1, tableView.SelectedIndex, "Precondition: row 1 is the selection.");

			var otherRow = RequireSelectionItemProvider(tableView, 2);
			if (otherRow == null)
			{
				return;
			}

			// Spec silent; reasoned from the operation being scoped to "this row". This is a real
			// sequence rather than a synthetic one: AT clients routinely cache providers across a
			// selection change, and a stale one must not be able to wipe the current selection.
			otherRow.RemoveFromSelection();
			tableView.UpdateLayout();

			Verify.AreEqual(1, tableView.SelectedIndex,
				"RemoveFromSelection() on a row that is not the selection must not clear the selection.");

			var selectedRow = RequireSelectionItemProvider(tableView, 1);
			if (selectedRow == null)
			{
				return;
			}

			selectedRow.RemoveFromSelection();
			tableView.UpdateLayout();
			Verify.AreEqual(-1, tableView.SelectedIndex,
				"RemoveFromSelection() on the selected row must clear the selection.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a row peer reports a selection container, linking the row back to the grid.")]
	public async Task VerifyRowPeerSelectionContainerIsTheTableView()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.SelectionMode = TableViewSelectionMode.Single;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = RequireSelectionItemProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Only non-null is assertable; the provider is opaque. Kept because it costs two lines
			// inside a class that already constructs everything it needs, and a null breaks the
			// selection model's parent link.
			Verify.IsNotNull(provider.SelectionContainer, "A row peer must report the grid that owns its selection.");
		});
	}

	// ---------- 12.4 Header and group peers ----------

	[TestMethod]
	[TestProperty("Description", "Verifies a column header peer names itself from a string Header.")]
	public async Task VerifyColumnHeaderPeerNameUsesStringHeader()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);

			// The base implementation would produce the TableView's own name, because the peer's
			// Owner() is the TableView - so every column would read identically.
			Verify.AreEqual("Name", peer.GetName(), "A string Header must be the column header peer's name.");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: non-string headers never receive an automation name. Re-enable once header naming falls back to the realized header content.
	[Ignore("WinUI ignores this test: Product bug: non-string headers never receive an automation name. Re-enable once header naming falls back to the realized header content.")]
	[TestProperty("Description", "Verifies a template-header column peer falls back to the realized header cell's automation name.")]
	public async Task VerifyColumnHeaderPeerNameFallsBackToRealizedHeaderForTemplateHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);

			// A non-string header rendered through HeaderTemplate, with an automation name on the
			// template root. The header object must be neither IStringable nor a String-typed
			// IPropertyValue, or TryGetColumnHeaderString short-circuits the fallback under test -
			// a boxed primitive stringifies and would never reach it.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new Border { Width = 20, Height = 12 },
				HeaderTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                              <Border AutomationProperties.Name=""Priority"" Width=""30"" Height=""16"" />
                          </DataTemplate>"),
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var name = peer.GetName();

			// Spec silent on the fallback ORDER; reasoned from string headers being the documented
			// primary source and template headers needing some name.
			Verify.AreEqual("Priority", name,
				"A template header's peer must take its name from the realized header, not from the TableView owner and not empty.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column header peers have pairwise-distinct runtime ids and keep their identity across enumerations.")]
	public async Task VerifyColumnHeaderPeersAreDistinctAndStable()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = RequireTablePeer(tableView);
			var table = peer.GetPattern(PatternInterface.Table) as ITableProvider;
			if (table == null)
			{
				Verify.Fail("The TableView peer must advertise the Table pattern.");
				return;
			}

			var first = table.GetColumnHeaders();
			var second = table.GetColumnHeaders();
			Verify.AreEqual(3, first == null ? 0 : first.Length, "Every visible column must contribute a header provider.");
			Verify.AreEqual(3, second == null ? 0 : second.Length, "A second enumeration must report the same three headers.");

			// Peer identity across enumerations is NOT assertable from a managed test: the array
			// holds IRawElementProviderSimple, ProviderFromPeer builds a fresh provider on each
			// enumeration, and the projection gives no stable reference identity for either. The
			// cache in TableViewAutomationPeer::GetColumnHeaders is what preserves peer identity,
			// and it is reachable only by code inspection. What IS observable is that the header
			// set is stable in shape and that its members stay individually identifiable.

			// The product states this as an invariant, with the reasoning written into
			// TableViewColumnHeaderAutomationPeer::GetRuntimeIdCore: the owner-derived id the base
			// class supplies is IDENTICAL for every column, which is a UIA protocol violation. A
			// self-contained per-column id is built instead.
			//
			// GetRuntimeId() is not callable from a managed test - only GetRuntimeIdCore is, and it
			// is protected. The generated automation id is the observable proxy: it is derived from
			// the same per-column IUnknown pointer, so if the runtime id collapsed to one value per
			// owner the generated id would collapse with it.
			var generatedIds = tableView.Columns
				.Select(column => new TableViewColumnHeaderAutomationPeer(tableView, column).GetAutomationId())
				.ToList();

			foreach (var id in generatedIds)
			{
				Log.Comment($"Column header automation id: {id}");
				Verify.IsFalse(string.IsNullOrEmpty(id),
					"Each column header peer must generate a per-column automation id when none is authored.");
			}

			Verify.AreEqual(generatedIds.Count, generatedIds.Distinct().Count(),
				"Column header ids must be pairwise distinct; identical ids make the headers indistinguishable to AT and conflate client caches.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an authored AutomationId on a realized header cell wins over the generated per-column fallback.")]
	public async Task VerifyColumnHeaderAutomationIdPrefersAuthoredId()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var headerCells = GetHeaderCells(tableView);
			if (headerCells.Count < 2)
			{
				Verify.Fail($"Expected at least two realized header cells; saw {headerCells.Count}.");
				return;
			}

			AutomationProperties.SetAutomationId(headerCells[0], "AuthoredNameColumn");
			tableView.UpdateLayout();

			var authored = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);
			var generated = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]);

			// An app that labels its headers for UI automation testing must not find its ids
			// silently ignored.
			Verify.AreEqual("AuthoredNameColumn", authored.GetAutomationId(), "An authored AutomationId must win.");

			// Without the generated fallback, headers are unaddressable before their templates
			// realize - which is the whole reason the peer is enumerable from Columns().
			var generatedId = generated.GetAutomationId();
			Log.Comment($"Generated automation id: '{generatedId}'");
			Verify.IsFalse(string.IsNullOrEmpty(generatedId), "A column with no authored id must still get a non-empty id.");
			Verify.AreNotEqual(authored.GetAutomationId(), generatedId, "Generated ids must differ between columns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies column header PositionInSet and SizeOfSet are 1-based over visible columns.")]
	public async Task VerifyColumnHeaderPositionInSetAndSizeOfSetUseVisibleColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.Columns[0].Visibility = Visibility.Collapsed;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// TableView-dev-spec.md:329 names PositionInSet / SizeOfSet explicitly among the values
			// that are on a visible-column basis, so this is directly spec-backed rather than
			// inferred from the count tests. Announcing "column 2 of 3" while only two columns
			// exist tells the user to look for a column that is not there.
			var second = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]);
			var third = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[2]);

			Verify.AreEqual(1, second.GetPositionInSet(), "The first visible column must report position 1.");
			Verify.AreEqual(2, second.GetSizeOfSet(), "SizeOfSet must count visible columns only.");
			Verify.AreEqual(2, third.GetPositionInSet(), "The second visible column must report position 2.");
			Verify.AreEqual(2, third.GetSizeOfSet(), "SizeOfSet must be the same for every header in the band.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the Invoke pattern is withheld when either sort gate is closed.")]
	public async Task VerifyInvokePatternIsWithheldWhenTheColumnCannotSort()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// TableView.idl:666-670 declares IInvokeProvider unconditionally; the two gates come from
			// the sorting contract. Category 7.5 owns the positive path (invoke actually sorts).
			var column = tableView.Columns[0];
			Verify.IsNotNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be offered when both sort gates are open.");

			// Offering "invoke to sort" on a column the control will not sort is worse than no
			// affordance: the action appears to succeed and nothing happens.
			tableView.CanUserSortColumns = false;
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be withheld while CanUserSortColumns is false.");

			tableView.CanUserSortColumns = true;
			column.CanSort = false;
			Verify.IsNull(new TableViewColumnHeaderAutomationPeer(tableView, column).GetPattern(PatternInterface.Invoke),
				"Invoke must be withheld while the column's CanSort is false.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column header peer is a childless HeaderItem.")]
	public async Task VerifyColumnHeaderPeerIsALeafHeaderItem()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0]);

			Verify.AreEqual(AutomationControlType.HeaderItem, peer.GetAutomationControlType(),
				"HeaderItem is the UIA control type for table column headers.");

			// The peer's Owner() is the TableView, so a base-class GetChildren() would return the
			// ENTIRE TableView subtree under every column header - the whole grid duplicated once
			// per column in the AT tree. That specific catastrophe is what the override prevents,
			// which makes this assertion worth as much as the control-type one.
			var children = peer.GetChildren();
			Verify.AreEqual(0, children == null ? 0 : children.Count,
				"A column header peer must be a leaf; otherwise the whole TableView subtree appears under every header.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies header help text carries a sort state only for columns the control will actually sort.")]
	public async Task VerifyColumnHeaderHelpTextReportsSortStateOnlyForSortableColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutomationTable();
			tableView.CanUserSortColumns = true;
			tableView.Columns[0].CanSort = true;
			tableView.Columns[1].CanSort = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var sortable = tableView.Columns[0];
			var helpTexts = new List<string>();

			foreach (var direction in new[] { SortDirection.None, SortDirection.Ascending, SortDirection.Descending })
			{
				tableView.SortByColumn(sortable, direction);
				tableView.UpdateLayout();

				var text = new TableViewColumnHeaderAutomationPeer(tableView, sortable).GetHelpText();
				Log.Comment($"Help text for SortDirection.{direction}: '{text}'");

				// Asserted as non-empty and pairwise distinct rather than matched against literals:
				// these are localized resources, and pinning English text turns the test into a
				// localization tripwire.
				Verify.IsFalse(string.IsNullOrEmpty(text), $"A sortable column must report a sort state for SortDirection.{direction}.");
				helpTexts.Add(text);
			}

			Verify.AreEqual(3, helpTexts.Distinct().Count(), "Each sort direction must produce a distinct help text.");

			// A column that cannot be sorted must not announce a sort state - that implies an
			// affordance which does not exist. Category 14 owns the tooltip/sort-state combination
			// and its duplicate suppression; there is no tooltip in play here.
			var notSortable = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[1]).GetHelpText();
			Verify.IsTrue(string.IsNullOrEmpty(notSortable),
				$"A non-sortable column must report no sort state; saw '{notSortable}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the group header peer advertises ExpandCollapse whether or not the group is expandable.")]
	public async Task VerifyGroupHeaderPeerExposesExpandCollapseUnconditionally()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			var peer = GetGroupHeaderPeer(tableView, 0);

			// Spelled out in the IDL itself (TableView.idl:649-655): "ExpandCollapse is
			// unconditional (a non-expandable group reports LeafNode rather than dropping the
			// pattern)", with the in-box Expander / NavigationViewItem precedent named. One of the
			// strongest expectations in this category.
			Verify.IsNotNull(peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider,
				"An expandable group header must advertise ExpandCollapse.");

			header.IsExpandable = false;
			tableView.UpdateLayout();

			var notExpandable = GetGroupHeaderPeer(tableView, 0);
			var provider = notExpandable.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
			if (provider == null)
			{
				Verify.Fail("A non-expandable group header must still advertise ExpandCollapse; a pattern that appears and disappears as data changes forces AT clients to re-query.");
				return;
			}

			Verify.AreEqual(ExpandCollapseState.LeafNode, provider.ExpandCollapseState,
				"A non-expandable group must report LeafNode rather than dropping the pattern.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ExpandCollapseState tracks the group header's IsExpanded.")]
	public async Task VerifyGroupHeaderExpandCollapseStateMatchesIsExpanded()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			header.IsExpandable = true;
			header.IsExpanded = true;
			tableView.UpdateLayout();

			var provider = GetExpandCollapseProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Narrator announcing "collapsed" for a group whose rows are visible - or the inverse -
			// is the failure this guards.
			Verify.AreEqual(ExpandCollapseState.Expanded, provider.ExpandCollapseState, "An expanded group must report Expanded.");

			header.IsExpanded = false;
			tableView.UpdateLayout();
			Verify.AreEqual(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"A collapsed group must report Collapsed.");

			header.IsExpandable = false;
			tableView.UpdateLayout();
			Verify.AreEqual(ExpandCollapseState.LeafNode, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"A group that cannot expand must report LeafNode regardless of IsExpanded.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies repeated Expand() and Collapse() calls in one turn are idempotent, not toggles.")]
	public async Task VerifyGroupHeaderExpandAndCollapseAreIdempotent()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = GetExpandCollapseProvider(tableView, 0);
			if (provider == null)
			{
				return;
			}

			// Both calls are made WITHIN ONE UI-thread turn on purpose. The product applies the
			// mutation on a later turn, so a guard reading IsExpanded() cannot make a toggle
			// directional - two Expand() calls in one client turn would queue two toggles and leave
			// the group COLLAPSED, the exact opposite of what was asked. Settling between the calls
			// destroys the condition under test and makes this vacuous.
			provider.Collapse();
			provider.Collapse();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"Two Collapse() calls in one turn must leave the group collapsed. Repeated calls are normal AT client behaviour.");

			var provider = GetExpandCollapseProvider(tableView, 0);
			provider.Expand();
			provider.Expand();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(ExpandCollapseState.Expanded, GetExpandCollapseProvider(tableView, 0).ExpandCollapseState,
				"Two Expand() calls in one turn must leave the group expanded; ExpandCollapsePattern requires Expand/Collapse to be idempotent.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header spans every visible column as a merged cell.")]
	public async Task VerifyGroupHeaderGridItemSpansAllVisibleColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = GetGroupHeaderPeer(tableView, 0);
			var gridItem = peer.GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (gridItem == null)
			{
				Verify.Fail("A group header peer must advertise GridItem; TableView.idl:655-662 declares IGridItemProvider on it.");
				return;
			}

			// A ColumnSpan of 1 would place the band in the first column only, so AT reports a
			// one-cell row where the user sees a full-width band, and the cells to its right appear
			// to belong to a row that has none.
			Verify.AreEqual(0, gridItem.Column, "The band starts at the first column.");
			Verify.AreEqual(1, gridItem.RowSpan, "The band occupies exactly one grid row.");
			Verify.AreEqual(3, gridItem.ColumnSpan, "The band must span every visible column.");

			// Row is the header's index in the flattened row list, so the first group header is the
			// first flattened row.
			Verify.AreEqual(0, gridItem.Row, "The first group header is the first row of the flattened list.");

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			tableView.UpdateLayout();

			// Visible-column basis, consistent with 12.2.
			var narrowed = GetGroupHeaderPeer(tableView, 0).GetPattern(PatternInterface.GridItem) as IGridItemProvider;
			if (narrowed == null)
			{
				Verify.Fail("The group header peer must still advertise GridItem after a column is collapsed.");
				return;
			}

			Verify.AreEqual(2, narrowed.ColumnSpan,
				"ColumnSpan must fall to the visible column count when a column is collapsed.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header peer's name composes the group's key text and count text.")]
	public async Task VerifyGroupHeaderPeerNameCombinesKeyAndCount()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			var info = header.Content as TableViewGroupInfo;
			if (info == null)
			{
				Verify.Fail("A group header's Content must be the TableViewGroupInfo projection.");
				return;
			}

			// Category 9 owns the VALUES of KeyText / ItemCountText; this owns only their
			// composition, so the expected string is built from the live projection. The name is
			// read from that projection rather than the visual tree, deliberately, so it reflects
			// current state rather than the last render.
			var expected = $"{info.KeyText} {info.ItemCountText}";
			Verify.AreEqual(expected, GetGroupHeaderPeer(tableView, 0).GetName(),
				"The group header name must be the key text and the count text joined by a single space.");

			Verify.IsFalse(string.IsNullOrEmpty(info.KeyText), "Precondition: the group must have key text to announce.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header peer reports the Group control type.")]
	public async Task VerifyGroupHeaderControlTypeIsGroup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedAutomationTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// DataItem would make Narrator read the band as a data row, so the user hears a row that
			// has no cells.
			Verify.AreEqual(AutomationControlType.Group, GetGroupHeaderPeer(tableView, 0).GetAutomationControlType(),
				"The group-header band is a Group, not a DataItem.");
		});
	}

	// ---------- 12.5 Primitive peers ----------
	//
	// Both primitives are MUX_INTERNAL but are projected into the test app -
	// TableView_Sizing_APITests.cs already drives ResizeGripper directly. These assert through the
	// base AutomationPeer members, which need no cast to the peer type.

	[TestMethod]
	[TestProperty("Description", "Verifies the SortIndicator peer is decorative and excluded from both the control and content views.")]
	public async Task VerifySortIndicatorPeerIsDecorativeAndExcludedFromBothViews()
	{
		SortIndicator indicator = null;

		RunOnUIThread.Execute(() =>
		{
			indicator = new SortIndicator { Direction = SortIndicatorDirection.Ascending };
			LoadContent(indicator);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(indicator);
			if (peer == null)
			{
				Verify.Fail("A SortIndicator must produce an automation peer.");
				return;
			}

			// The owning header announces the sort state in its help text. If the glyph were also a
			// control or content element, Narrator would stop on it and the user would hear the sort
			// state twice - once from the header and once from a stray image.
			Verify.IsFalse(peer.IsControlElement(), "The sort glyph must be excluded from the control view.");
			Verify.IsFalse(peer.IsContentElement(), "The sort glyph must be excluded from the content view.");
			Verify.AreEqual(AutomationControlType.Image, peer.GetAutomationControlType(), "The sort glyph is an Image.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer composes its owner's header text into its name.")]
	public async Task VerifyResizeGripperPeerNameIncludesOwningHeaderText()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			var bare = peer.GetName();
			Log.Comment($"Gripper name with no OwnerName: '{bare}'");
			Verify.IsFalse(string.IsNullOrEmpty(bare), "A gripper must always identify itself, even with no owner text.");

			gripper.OwnerName = "Price";
			gripper.UpdateLayout();

			var composed = FrameworkElementAutomationPeer.CreatePeerForElement(gripper).GetName();
			Log.Comment($"Gripper name with OwnerName 'Price': '{composed}'");

			// Containment rather than an exact string: the format comes from a localized resource
			// with a hardcoded fallback, so pinning either form is a localization tripwire. Without
			// the owner text every gripper in the table announces identically and a user moving
			// across the header row cannot tell which column they are about to resize.
			Verify.IsTrue(composed.Contains("Price"), $"The gripper name must carry its owner's header text; saw '{composed}'.");
			Verify.IsTrue(composed.Contains(bare), $"The gripper name must still carry the localized gripper name; saw '{composed}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer never sources its name from Tag or from an ancestor.")]
	public async Task VerifyResizeGripperPeerNameIgnoresTagAndAncestors()
	{
		ResizeGripper gripper = null;
		string bare = null;

		RunOnUIThread.Execute(() =>
		{
			var host = new Grid();
			AutomationProperties.SetName(host, "AncestorDecoy");

			gripper = new ResizeGripper { Width = 8, Height = 24, Tag = "TagDecoy" };
			host.Children.Add(gripper);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			bare = peer.GetName();
			Log.Comment($"Gripper name inside a named ancestor, with a Tag decoy: '{bare}'");

			// The product states the sourcing rule as an invariant: read from the declared OwnerName
			// property, never from Tag, never from an ancestor. Tag is a general-purpose slot a host
			// may already be using, which is what makes the decoy realistic - and the product calls
			// the failure out as "a silent wrong name is an accessibility bug". Silent is the
			// operative word: nothing else would catch it.
			Verify.IsFalse(bare.Contains("TagDecoy"), $"The gripper must not publish its Tag as a name; saw '{bare}'.");
			Verify.IsFalse(bare.Contains("AncestorDecoy"), $"The gripper must not publish an ancestor's name; saw '{bare}'.");
			Verify.IsFalse(string.IsNullOrEmpty(bare), "The gripper must still report its own localized name.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the ResizeGripper peer reports Thumb and an empty automation id.")]
	public async Task VerifyResizeGripperPeerControlTypeAndEmptyAutomationId()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			// Both choices are stated as deliberate in the product with their rationale, which makes
			// them invariants rather than incidental. Slider would make AT offer value and range
			// operations the gripper does not have: it reports drag distance and owns no value.
			Verify.AreEqual(AutomationControlType.Thumb, peer.GetAutomationControlType(), "The gripper is a Thumb, not a Slider.");

			// A constant id would be duplicated across every column's gripper, violating the
			// requirement that automation ids be unique among siblings.
			Verify.AreEqual(string.Empty, peer.GetAutomationId(), "With nothing authored the gripper's automation id must be empty.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an authored AutomationProperties.Name on a gripper is used verbatim, with no owner text composed in.")]
	public async Task VerifyAuthoredAutomationNameWinsOnGripper()
	{
		ResizeGripper gripper = null;

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24, OwnerName = "Price" };
			AutomationProperties.SetName(gripper, "Column splitter");

			LoadContent(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gripper);
			if (peer == null)
			{
				Verify.Fail("A ResizeGripper must produce an automation peer.");
				return;
			}

			// An app that names its grippers must not get a corrupted name - its text with the
			// framework's appended to it - and must have a way to opt out of the composition.
			Verify.AreEqual("Column splitter", peer.GetName(), "An authored automation name must win outright.");
		});
	}
}

internal static class TableViewAutomationTestHelpers
{
	// Three text columns over distinct properties, so a composed cell name is unambiguous about
	// which column it came from.
	internal static TableView CreateAutomationTable() => CreateAutomationTable(MakeItems());

	internal static TableView CreateAutomationTable(List<Person> items)
		=> AddAutomationColumns(CreateTableViewShell(items, 600, 300));

	// The same three columns over a source grouped by Role, so group headers realize.
	internal static TableView CreateGroupedAutomationTable()
	{
		var source = TableViewSource.From(MakeItems());
		source.GroupBy(item => (object)((Person)item).Role);

		return AddAutomationColumns(CreateTableViewShell(source, 600, 400));
	}

	private static TableView AddAutomationColumns(TableView tableView)
	{
		tableView.Columns.Add(MakeTextColumn("Name"));
		tableView.Columns.Add(MakeTextColumn("Role"));
		tableView.Columns.Add(MakeTextColumn("Alias", "Name"));

		return tableView;
	}

	internal static TableViewAutomationPeer RequireTablePeer(TableView tableView)
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(tableView) as TableViewAutomationPeer;
		Verify.IsTrue(peer != null, "A TableView must produce a TableViewAutomationPeer.");
		return peer;
	}

	internal static IGridProvider RequireGridProvider(TableView tableView)
	{
		var provider = RequireTablePeer(tableView).GetPattern(PatternInterface.Grid) as IGridProvider;
		Verify.IsTrue(provider != null, "The TableView peer must advertise IGridProvider.");
		return provider;
	}

	internal static TableViewRowAutomationPeer RequireRowPeer(TableView tableView, int rowIndex)
	{
		var rows = GetRealizedRows(tableView);
		Verify.IsTrue(rows.Count > rowIndex, $"The test needs a realized row at index {rowIndex}; saw {rows.Count} rows.");
		return GetRowPeer(rows[rowIndex]);
	}

	internal static ISelectionItemProvider RequireSelectionItemProvider(TableView tableView, int rowIndex)
	{
		var peer = RequireRowPeer(tableView, rowIndex);
		var provider = peer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
		Verify.IsTrue(provider != null, $"Row {rowIndex}'s peer must advertise SelectionItem for this test to drive selection.");
		return provider;
	}

	internal static List<FrameworkElement> GetHeaderCells(TableView tableView)
		=> GetHeaderHost(tableView).Children
			.OfType<FrameworkElement>()
			.Where(child => child.Tag is TableViewColumn)
			.ToList();
}
