// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Virtualization_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

using Common;
using System.Threading.Tasks;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewShapingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewVirtualizationTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;
// Category 15 of the TableView test plan: virtualization safety.
//
// SUBJECT. Only the ways TableView could DEFEAT the virtualization it sits on, plus the one axis
// it deliberately does not virtualize. Row virtualization itself is ItemsRepeater + StackLayout,
// and scrolling is ScrollViewer; re-asserting either here would test another control. Plan
// section 15.0 lists the four backlog items dropped on that basis.
[TestClass]
public class TableViewVirtualizationTests : TableViewApiTestBase
{
	#region 15.1 Row virtualization is not defeated

	[TestMethod]
	[TestProperty("Description", "Verifies a source of thousands of items realizes only a viewport-sized set of rows.")]
	public async Task VerifyLargeSourceRealizesBoundedRowCount()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateVirtualizationTable(MakeManyItems(LargeSourceCount));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var realized = RealizedElementCount(tableView);
			var bound = RealizationBound(tableView);

			if (bound <= 0)
			{
				return;
			}

			Log.Comment($"Source {LargeSourceCount}, realized {realized}, bound {bound}.");

			Verify.IsGreaterThan(realized, 0, "The viewport must be filled.");
			Verify.IsLessThanOrEqual(realized, bound,
				$"Realization must stay inside the viewport-plus-cache bound; realized {realized}, bound {bound}.");
			Verify.IsLessThan(realized, LargeSourceCount,
				"A large source must not realize one container per item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a long scroll grows neither the realized set nor the container pool.")]
	public async Task VerifyRealizedRowCountStaysBoundedAfterLongScroll()
	{
		TableView tableView = null;
		var bound = 0;
		var containersAtStart = 0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateVirtualizationTable(MakeManyItems(LargeSourceCount));
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			bound = RealizationBound(tableView);
			containersAtStart = GetRealizedRows(tableView).Count;
			Log.Comment($"Before scrolling: realized {RealizedElementCount(tableView)}, containers {containersAtStart}, bound {bound}.");
		});

		if (bound <= 0)
		{
			return;
		}

		var scrollable = 0.0;
		RunOnUIThread.Execute(() => scrollable = GetBodyScroller(tableView)?.ScrollableHeight ?? 0.0);

		// Ten hops across the whole source. A retained container shows up as a pool that grows
		// with the distance scrolled, which a single scroll would not expose.
		for (var hop = 1; hop <= 10; hop++)
		{
			await ScrollBodyTo(tableView, ScrollAxis.Vertical, scrollable * hop / 11.0);

			RunOnUIThread.Execute(() =>
			{
				var realized = RealizedElementCount(tableView);
				var containers = GetRealizedRows(tableView).Count;
				Log.Comment($"Hop {hop}: realized {realized}, containers {containers}.");

				Verify.IsGreaterThan(realized, 0, $"Rows must still be realized at hop {hop}.");
				Verify.IsLessThanOrEqual(realized, bound,
					$"Realization must stay bounded at hop {hop}; realized {realized}, bound {bound}.");
			});
		}

		RunOnUIThread.Execute(() =>
		{
			var containersAtEnd = GetRealizedRows(tableView).Count;

			// GetRealizedRows walks the visual tree deliberately here: a cleared container stays
			// parented to the repeater's panel, so this counts the POOL, which is exactly the
			// number that leaks if the prepare/clear path retains rows.
			Log.Comment($"Containers parented: {containersAtStart} at start, {containersAtEnd} after ten hops.");

			Verify.IsLessThanOrEqual(containersAtEnd, bound,
				$"The container pool must not grow with the distance scrolled; {containersAtStart} at start, {containersAtEnd} at end, bound {bound}.");
		});
	}

	#endregion

	#region 15.2 Columns are deliberately not virtualized

	[TestMethod]
	[TestProperty("Description", "Verifies every column keeps a header and a cell on every realized row at the supported column ceiling.")]
	public async Task VerifySupportedColumnCountRealizesEveryCell()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateVirtualizationTable(MakeManyItems(60), SupportedColumnCeiling);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => VerifyEveryColumnIsRealized(tableView, "before scrolling"));

		var scroller = default(ScrollViewer);
		RunOnUIThread.Execute(() =>
		{
			scroller = GetBodyScroller(tableView);
			if (scroller == null)
			{
				return;
			}

			Verify.IsGreaterThan(scroller.ScrollableWidth, 0.0,
				"Precondition: 50 columns must overflow the viewport horizontally.");

			scroller.ChangeView(scroller.ScrollableWidth, null, null, true);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => VerifyEveryColumnIsRealized(tableView, "scrolled to the far edge"));
	}

	#endregion
}

internal static class TableViewVirtualizationTestHelpers
{
	internal const int LargeSourceCount = 5000;

	// TableView-dev-spec.md:141 sets the supported range at ~5-50 columns and says the control is
	// explicitly not designed past it, so 50 is the ceiling to test, not a starting point.
	internal const int SupportedColumnCeiling = 50;

	internal static TableView CreateVirtualizationTable(IList<Person> items, int columnCount = 3)
	{
		var tableView = CreateTableViewShell(items, 600, 400);

		for (var i = 0; i < columnCount; i++)
		{
			tableView.Columns.Add(MakeTextColumn($"Column {i}", "Name", new GridLength(120)));
		}

		return tableView;
	}

	// Counts what the repeater has actually realized. NOT a visual-tree walk: cleared containers
	// stay parented to the repeater's panel and are arranged off-screen at (-10000, -10000), so a
	// visual-tree count measures the pool rather than realization.
	internal static int RealizedElementCount(TableView tableView)
		=> GetProjectedElements(tableView, requireAllRealized: false).Count(element => element != null);

	// The documented bound: rows intersecting the viewport, plus two viewports of cache on each
	// side (TableView-dev-spec.md:137). Derived from a realized row's height so a density change
	// cannot silently invalidate it; the slack covers partial rows at both edges.
	internal static int RealizationBound(TableView tableView)
	{
		var rows = GetRealizedRows(tableView);
		var rowHeight = rows.Select(row => row.ActualHeight).FirstOrDefault(height => height > 0.0);

		if (rowHeight <= 0.0)
		{
			Verify.Fail("The fixture must realize at least one row with a non-zero height.");
			return 0;
		}

		var scroller = GetBodyScroller(tableView);
		if (scroller == null || scroller.ViewportHeight <= 0.0)
		{
			Verify.Fail("PART_BodyScroller should report a viewport height once laid out.");
			return 0;
		}

		var viewportRows = (int)Math.Ceiling(scroller.ViewportHeight / rowHeight);
		Log.Comment($"Row height {rowHeight}, viewport {scroller.ViewportHeight}, viewport rows {viewportRows}.");

		return (viewportRows * 5) + 4;
	}

	internal static void VerifyEveryColumnIsRealized(TableView tableView, string context)
	{
		Verify.AreEqual(SupportedColumnCeiling, GetHeaderColumns(tableView).Count,
			$"Every column must keep a header cell ({context}).");

		var rows = GetRealizedRows(tableView);
		Verify.IsGreaterThan(rows.Count, 0, $"The fixture must realize rows ({context}).");

		foreach (var row in rows)
		{
			var columns = GetRowCellColumns(row);

			// Every non-null column gets a cell on every realized row - collapsed columns get a
			// hidden one - because columns are deliberately not virtualized.
			Verify.AreEqual(SupportedColumnCeiling, columns.Count,
				$"Every realized row must carry one cell per column ({context}).");
		}
	}
}
