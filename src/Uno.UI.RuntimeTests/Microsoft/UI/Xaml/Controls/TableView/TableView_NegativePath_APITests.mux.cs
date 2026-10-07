// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_NegativePath_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using MUXControlsTestApp.Utilities;
using System;
using Windows.Foundation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewNegativePathTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSelectionTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewShapingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSortingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 16 of the TableView test plan: error handling, edge cases, and reentrancy.
//
// SUBJECT. Bad input and reentrant calls that arrive through public API only - a null entry in
// Columns, non-finite width constraints, a degenerate host size, a source mutated from inside
// an event handler, and an app handler that throws. Gesture-driven teardown (a pending resize
// drag) lives in the interaction plan.
//
// SCOPE. Plan section 16.0 records why eleven backlog items are not here. The short version:
// an item whose expected result reads "leaves coherent state" or "per contract" has no
// expectation at all, and several of the remaining ones are already driven by sections 2 and 10.
// Every test below asserts a value that some line of the IDL or the dev spec commits to.
[TestClass]
public class TableViewNegativePathTests : TableViewApiTestBase
{
	#region 16.1 Malformed input through public API

	[TestMethod]
	[TestProperty("Description", "Verifies a null entry in Columns is skipped by header and cell generation instead of crashing or consuming a cell slot.")]
	public async Task VerifyNullColumnEntryIsSkipped()
	{
		TableView tableView = null;
		TableViewColumn first = null;
		TableViewColumn second = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeManyItems(4), 500, 260);

			first = MakeTextColumn("Name");
			second = MakeTextColumn("Role");

			tableView.Columns.Add(first);

			// The dev spec's virtualization note says a realized row renders a cell for every
			// non-null column, so a null entry is an anticipated state rather than abuse.
			tableView.Columns.Add(null);
			tableView.Columns.Add(second);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, tableView.Columns.Count, "The null entry should still occupy a slot in the collection.");
			Verify.IsNull(tableView.Columns[1], "Precondition: index 1 is the null entry.");

			var headerCells = GetHeaderHost(tableView).Children.Count;
			Verify.AreEqual(2, headerCells, "The header host should build a cell for each non-null column only.");

			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(2, headerColumns.Count, "Two columns should be represented in the header host.");
			Verify.AreSame(first, headerColumns[0], "The first header should belong to the first real column.");
			Verify.AreSame(second, headerColumns[1], "The null entry must not reorder the real columns.");

			var rows = ProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows should realize even with a null column present.");

			foreach (var row in rows)
			{
				var cellColumns = GetRowCellColumns(row);
				Verify.AreEqual(2, cellColumns.Count, "A row must carry exactly one cell per non-null column.");
				Verify.AreSame(first, cellColumns[0], "Cell order must match header order.");
				Verify.AreSame(second, cellColumns[1], "Cell order must match header order.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies NaN and infinite MinWidth/MaxWidth values never resolve into a non-finite ActualWidth.")]
	[TestProperty("data:Constraint", "{MinNaN, MaxNaN, MaxPositiveInfinity, MinNegativeInfinity}")]
	[DataRow("MinNaN")]
	[DataRow("MaxNaN")]
	[DataRow("MaxPositiveInfinity")]
	[DataRow("MinNegativeInfinity")]
	public async Task VerifyNonFiniteWidthConstraintsDoNotCorruptActualWidth(string constraint)
	{
		// TODO Uno: TAEF data row mapped to an MSTest [DataRow] parameter.
		await VerifyNonFiniteWidthConstraint(constraint.Trim());
	}

	// Split out of the data-driven test above so it can be ignored on its own: an infinite MinWidth
	// reaches layout as the column floor with no finite guard and crashes the test host, which also
	// skips assembly cleanup for every test after it.
	[TestMethod]
	[TestProperty("Description", "Verifies an infinite MinWidth never resolves into a non-finite ActualWidth.")]
	[TestProperty("Ignore", "True")] // Product bug: MinWidth = +Infinity crashes layout. Re-enable once the column floor is clamped to a finite value.
	[Ignore("WinUI ignores this test: Product bug: MinWidth = +Infinity crashes layout. Re-enable once the column floor is clamped to a finite value.")]
	public async Task VerifyInfiniteMinWidthDoesNotCorruptActualWidth()
	{
		await VerifyNonFiniteWidthConstraint("MinPositiveInfinity");
	}

	// TODO Uno: async because IdleSynchronizer.Wait maps to await TestServices.WindowHelper.WaitForIdle().
	private async Task VerifyNonFiniteWidthConstraint(string constraint)
	{
		Log.Comment($"Non-finite constraint under test: {constraint}.");

		const double AuthoredWidth = 200.0;

		TableView tableView = null;
		TableViewColumn column = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			column = tableView.Columns[0];
			column.Width = new GridLength(AuthoredWidth, GridUnitType.Pixel);

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(AuthoredWidth, column.ActualWidth,
				"Precondition: a pixel column should resolve to its authored width before the constraint is applied.");

			switch (constraint)
			{
				case "MinNaN":
					column.MinWidth = double.NaN;
					break;
				case "MaxNaN":
					column.MaxWidth = double.NaN;
					break;
				case "MaxPositiveInfinity":
					column.MaxWidth = double.PositiveInfinity;
					break;
				case "MinNegativeInfinity":
					column.MinWidth = double.NegativeInfinity;
					break;
				case "MinPositiveInfinity":
					column.MinWidth = double.PositiveInfinity;
					break;
				default:
					Verify.Fail($"Unhandled constraint case '{constraint}'.");
					break;
			}

			tableView.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var actual = column.ActualWidth;
			Log.Comment($"ActualWidth after applying {constraint}: {actual}.");

			Verify.IsFalse(double.IsNaN(actual), "ActualWidth must stay a real number.");
			Verify.IsFalse(double.IsInfinity(actual), "ActualWidth is a resolved pixel value and can never be infinite.");
			Verify.IsGreaterThanOrEqual(actual, 0.0, "A resolved width can never be negative.");

			if (constraint == "MaxPositiveInfinity")
			{
				// MaxWidth defaults to infinity in the IDL, so setting it explicitly is a no-op.
				Verify.AreEqual(AuthoredWidth, actual,
					"An infinite MaxWidth is the documented default and must leave the authored width alone.");
			}

			// A poisoned width propagates into Measure, so the rows are the real casualty.
			var rows = ProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must still realize after the constraint change.");

			foreach (var row in rows)
			{
				Verify.IsFalse(double.IsNaN(row.ActualWidth), "A row must not inherit a NaN width.");
				Verify.IsFalse(double.IsInfinity(row.ActualWidth), "A row must not inherit an infinite width.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a zero-sized host neither crashes star distribution nor permanently poisons resolved widths.")]
	public async Task VerifyZeroSizedHostDoesNotCrash()
	{
		Grid host = null;
		TableView tableView = null;
		TableViewColumn pixelColumn = null;
		TableViewColumn autoColumn = null;
		TableViewColumn starColumn = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			tableView = new TableView
			{
				ItemsSource = MakeManyItems(6),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
			};

			pixelColumn = MakeTextColumn("Pixel", "Name", new GridLength(120.0, GridUnitType.Pixel));
			autoColumn = MakeTextColumn("Auto", "Role", GridLength.Auto);
			starColumn = MakeTextColumn("Star", "Name", new GridLength(1.0, GridUnitType.Star));

			tableView.Columns.Add(pixelColumn);
			tableView.Columns.Add(autoColumn);
			tableView.Columns.Add(starColumn);

			host = new Grid { Width = 0.0, Height = 0.0 };
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var column in tableView.Columns)
			{
				var width = column.ActualWidth;
				Log.Comment($"Zero-sized host, column '{column.Header}': ActualWidth {width}.");

				Verify.IsFalse(double.IsNaN(width), "A zero-sized pass must not resolve a width to NaN.");
				Verify.IsFalse(double.IsInfinity(width), "A zero-sized pass must not resolve a width to infinity.");
				Verify.IsGreaterThanOrEqual(width, 0.0, "A resolved width can never be negative.");
			}

			host.Width = 600.0;
			host.Height = 300.0;
			host.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Restored host: pixel {pixelColumn.ActualWidth}, auto {autoColumn.ActualWidth}, star {starColumn.ActualWidth}.");

			Verify.AreEqual(120.0, pixelColumn.ActualWidth,
				"A pixel column must return to its authored width once the host has size.");
			Verify.IsGreaterThan(starColumn.ActualWidth, 0.0,
				"A star column must take a real share of the viewport after the zero-sized pass.");
			Verify.IsGreaterThan(autoColumn.ActualWidth, 0.0,
				"An auto column must measure its content once the host has size.");

			var rows = ProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must realize once the host has real size.");
		});
	}

	#endregion

	#region 16.2 Mutation from inside an event handler

	[TestMethod]
	[TestProperty("Description", "Verifies removing a source item from inside Sorting or Sorted leaves the projection matching the surviving items in sorted order.")]
	[TestProperty("data:SortEvent", "{Sorting, Sorted}")]
	[DataRow("Sorting")]
	[DataRow("Sorted")]
	public async Task VerifySourceMutationDuringSortEventIsSafe(string sortEvent)
	{
		// TODO Uno: TAEF data row mapped to an MSTest [DataRow] parameter.
		Log.Comment($"Mutating the source from inside: {sortEvent}.");

		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;
		ShapedPerson removed = null;
		var handlerRan = 0;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateSortTable(items);

			if (sortEvent == "Sorting")
			{
				tableView.Sorting += (s, e) =>
				{
					handlerRan++;
					if (handlerRan == 1)
					{
						removed = items[0];
						items.RemoveAt(0);
					}
				};
			}
			else
			{
				tableView.Sorted += (s, e) =>
				{
					handlerRan++;
					if (handlerRan == 1)
					{
						removed = items[0];
						items.RemoveAt(0);
					}
				};
			}

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, handlerRan, $"The {sortEvent} handler should have run exactly once.");
			Verify.IsNotNull(removed, "The handler should have removed an item.");
			Verify.IsFalse(items.Contains(removed), "The removal should have taken effect on the source.");

			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[0].SortDirection,
				"The requested sort direction must survive a mutation raised from its own event.");

			var expected = items.Select(person => person.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();
			var actual = RowValues(tableView, person => person.Name);

			VerifySequence(expected, actual,
				$"the projection after removing an item from inside {sortEvent}");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a source item, then a column, from inside SelectionChanged leaves SelectedIndex and SelectedItem in agreement.")]
	public async Task VerifySourceMutationDuringSelectionChangedIsSafe()
	{
		TableView tableView = null;
		ObservableCollection<Person> items = null;
		var handlerRan = 0;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<Person>(MakeManyItems(8));
			tableView = CreateSelectionTable(items);

			tableView.Columns.Add(MakeTextColumn("Role"));

			tableView.SelectionChanged += (s, e) =>
			{
				handlerRan++;
				if (handlerRan == 1)
				{
					// Removing above the selection is the sharp case: any index captured before
					// the callout is stale by the time the control writes selection state back.
					items.RemoveAt(0);
				}
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(3);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(handlerRan, 0, "SelectionChanged should have fired for the Select call.");
			Verify.AreEqual(7, items.Count, "The handler's removal should have taken effect.");

			VerifySelectionIsCoherent(tableView, items, "after removing an item from inside SelectionChanged");

			// Phase two: the same invariant, mutated through the other collection the app owns.
			// Headers and row cells are rebuilt by two independent reactions to the same
			// Columns.VectorChanged, and both need a layout pass before they can be compared.
			tableView.Columns.RemoveAt(1);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			VerifySelectionIsCoherent(tableView, items, "after removing a column");

			var headerColumns = GetHeaderColumns(tableView);
			Verify.AreEqual(1, headerColumns.Count, "The header host should rebuild with the surviving column only.");

			var rows = ProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows must still realize after the column removal.");

			foreach (var row in rows)
			{
				Verify.AreEqual(headerColumns.Count, GetRowCellColumns(row).Count,
					"Every row must carry exactly one cell per surviving column.");
			}
		});
	}

	#endregion

	#region 16.3 Consumer failure

	[TestMethod]
	[TestProperty("Description", "Verifies an exception thrown by a SelectionChanged handler reaches the Select caller and leaves the control usable.")]
	public async Task VerifyConsumerThrowInSelectionChangedSurfacesAtTheCaller()
	{
		TableView tableView = null;
		List<Person> items = null;
		TypedEventHandler<TableView, SelectionChangedEventArgs> throwingHandler = null;
		Exception caught = null;
		var handlerRan = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(6);
			tableView = CreateSelectionTable(items);

			throwingHandler = (s, e) =>
			{
				handlerRan++;
				throw new InvalidOperationException("Deliberate failure from a SelectionChanged handler.");
			};

			tableView.SelectionChanged += throwingHandler;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			try
			{
				tableView.Select(1);
			}
			catch (Exception e)
			{
				caught = e;
			}
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, handlerRan, "The throwing handler should have been invoked once.");
			Verify.IsNotNull(caught,
				"An app exception must surface at the Select caller rather than being swallowed by the control.");
			Log.Comment($"Caught at the call site: {caught.GetType().Name}: {caught.Message}");

			tableView.SelectionChanged -= throwingHandler;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Recovery is the half that is really TableView's problem: a handler that threw must
			// not leave the selection model half-written.
			tableView.Select(4);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(4, tableView.SelectedIndex, "Selection must work normally once the throwing handler is gone.");
			VerifySelectionIsCoherent(tableView, items, "after a handler threw and was detached");

			var rows = ProjectedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "The control must still render rows after a handler threw.");
		});
	}

	#endregion
}

internal static class TableViewNegativePathTestHelpers
{

	// Rows in projection order, skipping the recycle pool. GetRealizedRows walks the visual tree,
	// and a cleared container stays parented to the repeater's panel (arranged off-screen) until
	// it is prepared again - so after any source mutation the visual tree holds more rows than
	// the source has items, and the stale ones still carry their old cell wrappers.
	internal static List<TableViewRow> ProjectedRows(TableView tableView)
		=> GetProjectedElements(tableView, requireAllRealized: false).OfType<TableViewRow>().ToList();
}
