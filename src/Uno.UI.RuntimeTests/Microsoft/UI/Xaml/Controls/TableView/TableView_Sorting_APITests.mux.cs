// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Sorting_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
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
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSortingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 7 of the TableView test plan: sorting.
//
// SUBJECT. The control-level sort API - the state that lives on TableViewColumn.SortDirection,
// the three verbs and their documented Boolean returns, the Sorting/Sorted events, the two
// click-to-sort gates, and the reconciliation rule with TableViewSource.Sort. Category 8 owns
// TableViewSource.Sort itself. Plan section 7.0 writes the split up in full; the short version
// is that a test belongs here if it would still be meaningful with TableViewSource never
// mentioned in the test body.
//
// KEY RESOLUTION. Several tests reach GetSortMemberPathCore(), which is `overridable` in the
// IDL and therefore projects as `protected virtual`. A test class cannot call it, so the probe
// columns at the bottom of this file expose a public passthrough to base.GetSortMemberPathCore().
[TestClass]
public class TableViewSortingTests : TableViewApiTestBase
{
	#region 7.1 Column sort surface

	[TestMethod]
	[TestProperty("Description", "Verifies a fresh column reports the sort defaults declared in TableView.idl.")]
	public void VerifyColumnSortDefaults()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewColumn();

			Verify.IsTrue(column.CanSort, "CanSort defaults to true (TableView.idl:157).");
			Verify.AreEqual(TableViewSortCycle.AscendingDescending, column.SortCycle,
				"SortCycle defaults to AscendingDescending (TableView.idl:165).");
			Verify.AreEqual(string.Empty, column.SortMemberPath, "SortMemberPath defaults to empty.");
			Verify.IsNull(column.CustomSortComparer, "CustomSortComparer defaults to null.");
			Verify.AreEqual(SortDirection.None, column.SortDirection,
				"SortDirection defaults to None (TableView.idl:185).");

			var textColumn = new TableViewTextColumn();

			Verify.IsTrue(textColumn.CanSort, "A text column shares the base defaults.");
			Verify.AreEqual(SortDirection.None, textColumn.SortDirection, "A text column starts unsorted.");
			Verify.AreEqual(string.Empty, textColumn.SortMemberPath, "A text column starts with no explicit path.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the base column returns SortMemberPath verbatim from GetSortMemberPathCore.")]
	public void VerifyGetSortMemberPathReturnsExplicitPath()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new SortPathProbeColumn { SortMemberPath = "DepartmentName" };

			Verify.AreEqual("DepartmentName", column.ReadSortMemberPath(),
				"The base column returns SortMemberPath verbatim (TableView.idl:192-193).");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a text column with no explicit SortMemberPath falls back to its Binding path.")]
	public void VerifyGetSortMemberPathFallsBackToTextColumnBindingPath()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new SortPathProbeTextColumn
			{
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			};

			Verify.AreEqual("Name", column.ReadSortMemberPath(),
				"An empty SortMemberPath falls back to Binding.Path.Path (TableView.idl:175, :192).");

			column.SortMemberPath = "Role";

			Verify.AreEqual("Role", column.ReadSortMemberPath(),
				"An explicit path takes priority over the binding fallback.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a base column with no path and no comparer reports no sort key.")]
	public void VerifyBaseColumnWithNoPathAndNoComparerHasNoSortKey()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new SortPathProbeColumn();

			Verify.AreEqual(string.Empty, column.ReadSortMemberPath(),
				"The base column reports an empty key when nothing was configured (TableView.idl:175-177).");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the sort pipeline consults a custom column's GetSortMemberPathCore override.")]
	public async Task VerifyCustomColumnSortMemberPathOverrideDrivesSort()
	{
		TableView tableView = null;
		ProbeColumn column = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeShapedPeople(), 500, 700);

			// The column never mentions Role in a Binding: the only route to it is the override.
			column = new ProbeColumn { Header = "Custom", ValuePath = "Role" };
			tableView.Columns.Add(column);

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(column, SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var roles = RowValues(tableView, person => person.Role);

			VerifySequence(
				new[] { "Architect", "Designer", "Designer", "Engineer", "Engineer", "Engineer" },
				roles,
				"an overridden GetSortMemberPathCore must drive the sort key");
		});
	}

	#endregion

	#region 7.2 Programmatic sort state

	[TestMethod]
	[TestProperty("Description", "Verifies SortByColumn sets the requested direction on the column.")]
	[TestProperty("data:Direction", "{Ascending, Descending, None}")]
	[DataRow("Ascending")]
	[DataRow("Descending")]
	[DataRow("None")]
	public async Task VerifySortByColumnSetsDirection(string directionName)
	{
		// TODO Uno: TAEF data row mapped to an MSTest [DataRow] parameter.
		var direction = (SortDirection)Enum.Parse(typeof(SortDirection), directionName);
		Log.Comment($"Direction under test: {directionName}.");

		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], direction));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual(direction, tableView.Columns[0].SortDirection,
				$"{directionName}: the column must report the direction that was requested."));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies SortByColumn returns true only when the sort state actually changed.")]
	public async Task VerifySortByColumnReturnsWhetherStateChanged()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];

			Verify.IsFalse(tableView.SortByColumn(column, SortDirection.None),
				"Clearing an already-unsorted column changes nothing (TableView.idl:587).");
			Verify.IsTrue(tableView.SortByColumn(column, SortDirection.Ascending),
				"The first real sort changes the state.");
			Verify.IsFalse(tableView.SortByColumn(column, SortDirection.Ascending),
				"Re-applying the same direction to the same column changes nothing.");
			Verify.IsTrue(tableView.SortByColumn(column, SortDirection.Descending),
				"Flipping the direction changes the state.");
			Verify.IsTrue(tableView.SortByColumn(column, SortDirection.None),
				"Clearing a sorted column changes the state.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies sorting a second column clears the first column's sort state.")]
	public async Task VerifySortByColumnReplacesPreviousColumnSort()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[1], SortDirection.Descending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection,
				"Sorting is single-column: the previous column must be cleared (TableView.idl:180-181).");
			Verify.AreEqual(SortDirection.Descending, tableView.Columns[1].SortDirection,
				"The newly sorted column holds the active state.");

			var roles = RowValues(tableView, person => person.Role);

			VerifySequence(
				new[] { "Engineer", "Engineer", "Engineer", "Designer", "Designer", "Architect" },
				roles,
				"the rows must follow the column that currently owns the sort");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies repeated ToggleSortDirection calls walk the sequence named by the column's SortCycle, and wrap.")]
	[TestProperty("data:Cycle", "{AscendingDescending, AscendingDescendingNone, DescendingAscending, DescendingAscendingNone}")]
	[DataRow("AscendingDescending")]
	[DataRow("AscendingDescendingNone")]
	[DataRow("DescendingAscending")]
	[DataRow("DescendingAscendingNone")]
	public async Task VerifyToggleSortDirectionCyclesPerSortCycle(string cycleName)
	{
		// TODO Uno: TAEF data row mapped to an MSTest [DataRow] parameter.
		var cycle = (TableViewSortCycle)Enum.Parse(typeof(TableViewSortCycle), cycleName);
		Log.Comment($"Sort cycle under test: {cycleName}.");

		var expected = cycle switch
		{
			TableViewSortCycle.AscendingDescending => new[]
			{
				SortDirection.Ascending, SortDirection.Descending, SortDirection.Ascending,
			},
			TableViewSortCycle.AscendingDescendingNone => new[]
			{
				SortDirection.Ascending, SortDirection.Descending, SortDirection.None, SortDirection.Ascending,
			},
			TableViewSortCycle.DescendingAscending => new[]
			{
				SortDirection.Descending, SortDirection.Ascending, SortDirection.Descending,
			},
			_ => new[]
			{
				SortDirection.Descending, SortDirection.Ascending, SortDirection.None, SortDirection.Descending,
			},
		};

		TableView tableView = null;
		var actual = new List<SortDirection>();

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.Columns[0].SortCycle = cycle;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];

			for (var i = 0; i < expected.Length; i++)
			{
				tableView.ToggleSortDirection(column);
				actual.Add(column.SortDirection);
			}
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var detail = $"Expected [{string.Join(", ", expected)}], saw [{string.Join(", ", actual)}].";

			for (var i = 0; i < expected.Length; i++)
			{
				// The final step is the wrap: it proves the cycle repeats rather than terminating.
				Verify.AreEqual(expected[i], actual[i],
					$"{cycleName}: toggle {i + 1} of {expected.Length}. {detail}");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ToggleSortDirection does nothing to a column whose CanSort is false.")]
	public async Task VerifyToggleSortDirectionRespectsCanSort()
	{
		TableView tableView = null;
		var returned = true;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.Columns[0].CanSort = false;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => returned = tableView.ToggleSortDirection(tableView.Columns[0]));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(returned, "Toggling a column that opted out of click-to-sort changes no state.");
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection,
				"A column with CanSort = false must not acquire a sort direction.");

			VerifySequence(SourceOrderNames, RowValues(tableView, person => person.Name),
				"a column that opted out of sorting must leave the rows in source order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearSort resets every column and restores source order.")]
	public async Task VerifyClearSortClearsEveryColumnAndRestoresSourceOrder()
	{
		TableView tableView = null;
		var returned = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => returned = tableView.ClearSort());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(returned, "ClearSort returns true when it cleared something (TableView.idl:592-593).");

			foreach (var column in tableView.Columns)
			{
				Verify.AreEqual(SortDirection.None, column.SortDirection,
					$"Column '{column.Header}' must report no sort direction after ClearSort.");
			}

			VerifySequence(SourceOrderNames, RowValues(tableView, person => person.Name),
				"ClearSort must restore source order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearSort is a safe no-op that returns false when nothing is sorted.")]
	public async Task VerifyClearSortReturnsFalseWhenNothingIsSorted()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsFalse(tableView.ClearSort(), "Clearing a never-sorted table changes nothing."));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.ClearSort(), "The first clear after a real sort changes state.");
			Verify.IsFalse(tableView.ClearSort(), "A second consecutive clear is a no-op.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CanUserSortColumns gates only the gesture, never the programmatic API.")]
	public async Task VerifyCanUserSortColumnsFalseStillAllowsProgrammaticSort()
	{
		TableView tableView = null;
		var returned = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.CanUserSortColumns = false;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			returned = tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(returned, "SortByColumn must work while CanUserSortColumns is false (TableView.idl:577-579).");
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[0].SortDirection,
				"The column must hold the programmatically applied direction.");

			VerifySequence(NameAscendingOrder, RowValues(tableView, person => person.Name),
				"a programmatic sort must reorder rows even with the gesture gate closed");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies no sort affordance is built while CanUserSortColumns is false.")]
	public async Task VerifyCanUserSortColumnsFalseBuildsNoSortAffordance()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.CanUserSortColumns = false;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var indicator = FindSortIndicator(tableView, 0);

			// "No affordance" is not spelled out as absent-versus-collapsed, so either satisfies it.
			var absent = indicator == null || indicator.Visibility == Visibility.Collapsed;
			Verify.IsTrue(absent,
				"No sort affordance may be offered while CanUserSortColumns is false (TableView.idl:577-579).");

			tableView.CanUserSortColumns = true;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var indicator = FindSortIndicator(tableView, 0);
			if (indicator == null)
			{
				Verify.Fail("Re-opening the gate must build the sort affordance.");
				return;
			}

			Verify.AreEqual(Visibility.Visible, indicator.Visibility,
				"The affordance must be visible once the gate is open again.");
		});
	}

	#endregion

	#region 7.3 Sort ordering results

	[TestMethod]
	[TestProperty("Description", "Verifies sorting by a column orders rows by the column's sort member path.")]
	public async Task VerifySortBySortMemberPathOrdersRows()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.Columns[0].SortMemberPath = "Name";
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(NameAscendingOrder, RowValues(tableView, person => person.Name),
				"a column sort must order the rows by its sort member path"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies descending order is the exact reverse of ascending order.")]
	public async Task VerifySortDescendingReversesOrder()
	{
		TableView tableView = null;
		List<string> ascending = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => ascending = RowValues(tableView, person => person.Name));

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var expected = Enumerable.Reverse(ascending).ToList();
			VerifySequence(expected, RowValues(tableView, person => person.Name),
				"descending must be the exact reverse of ascending");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a CustomSortComparer drives the row order and is actually called.")]
	public async Task VerifyCustomSortComparerOrdersRows()
	{
		TableView tableView = null;
		LengthThenOrdinalComparer comparer = null;

		RunOnUIThread.Execute(() =>
		{
			comparer = new LengthThenOrdinalComparer();

			tableView = CreateSortTable();
			tableView.Columns[0].CustomSortComparer = comparer;

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(comparer.CompareCount, 0,
				"A supplied comparer must be the thing doing the comparing.");

			// Deliberately not alphabetical, so a default comparison cannot pass by coincidence.
			VerifySequence(
				new[] { "Mei", "Asha", "Ines", "Owen", "Rafa", "Diego" },
				RowValues(tableView, person => person.Name),
				"the comparer's order must be the row order");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CustomSortComparer takes precedence over SortMemberPath.")]
	public async Task VerifyCustomSortComparerTakesPrecedenceOverSortMemberPath()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();

			// The path and the comparer produce different orders on this data, so exactly one of
			// them can be the explanation for what the test sees.
			tableView.Columns[0].SortMemberPath = "Name";
			tableView.Columns[0].CustomSortComparer = new LengthThenOrdinalComparer();

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(
				new[] { "Mei", "Asha", "Ines", "Owen", "Rafa", "Diego" },
				RowValues(tableView, person => person.Name),
				"the comparer must win over SortMemberPath (TableView.idl:179-180)"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies items with equal sort keys keep their source-relative order.")]
	public async Task VerifySortIsStableForEqualKeys()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		// Role has three ties: one Architect, two Designers, three Engineers.
		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[1], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(
				new[] { "Mei", "Asha", "Ines", "Diego", "Rafa", "Owen" },
				RowValues(tableView, person => person.Name),
				"tied items must keep their source-relative order"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an item added while sorted keeps the sort and lands at its sorted position.")]
	public async Task VerifySortSurvivesItemAddition()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			tableView = CreateSortTable(items);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		// "Nadia" sorts between "Mei" and "Owen", so appending at the end would be visible.
		RunOnUIThread.Execute(() => items.Add(new ShapedPerson("Nadia", "Engineer", "Platform")));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[0].SortDirection,
				"A collection change must not drop the active sort.");

			VerifySequence(
				new[] { "Asha", "Diego", "Ines", "Mei", "Nadia", "Owen", "Rafa" },
				RowValues(tableView, person => person.Name),
				"an item added while sorted must land at its sorted position");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies sorting a column with neither a path nor a comparer leaves the rows alone.")]
	public async Task VerifyColumnWithNoSortKeyDoesNotReorderRows()
	{
		TableView tableView = null;
		TableViewColumn keyless = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();

			// A base column with no Binding to fall back on, so it has no sort key at all.
			keyless = new TableViewColumn { Header = "Keyless" };
			tableView.Columns.Add(keyless);

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(keyless, SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(SourceOrderNames, RowValues(tableView, person => person.Name),
				"a column with no sort key must not reorder anything (TableView.idl:175-177)");

			// What SortDirection should read here is not specified. Logged, not asserted.
			Log.Comment($"SortDirection on a keyless column after SortByColumn: {keyless.SortDirection}.");
		});
	}

	#endregion

	#region 7.4 Sort events

	[TestMethod]
	[TestProperty("Description", "Verifies Sorting is raised before the state changes, carrying the column and the pending direction.")]
	public async Task VerifySortingFiresBeforeStateChangeWithColumnAndDirection()
	{
		TableView tableView = null;
		TableViewColumn seenColumn = null;
		var seenDirection = SortDirection.None;
		var directionInsideHandler = SortDirection.Descending;
		var raiseCount = 0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();

			tableView.Sorting += (sender, args) =>
			{
				raiseCount++;
				seenColumn = args.Column;
				seenDirection = args.Direction;
				directionInsideHandler = tableView.Columns[0].SortDirection;
			};

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, raiseCount, "Sorting must be raised exactly once for one sort.");
			Verify.AreEqual(tableView.Columns[0], seenColumn, "The args must carry the trigger column.");
			Verify.AreEqual(SortDirection.Ascending, seenDirection, "The args must carry the pending direction.");
			Verify.AreEqual(SortDirection.None, directionInsideHandler,
				"Sorting is raised BEFORE the state change, so the handler must still see the old direction (TableView.idl:595-596).");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies cancelling Sorting leaves both the row order and every column's sort state untouched.")]
	public async Task VerifySortingCancelLeavesOrderAndSortStateUnchanged()
	{
		TableView tableView = null;
		var returned = true;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.Sorting += (sender, args) => args.Cancel = true;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			returned = tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(returned, "A cancelled sort changed no state, so it must not report that it did.");

			foreach (var column in tableView.Columns)
			{
				Verify.AreEqual(SortDirection.None, column.SortDirection,
					$"Column '{column.Header}' must be untouched by a cancelled sort (TableView.idl:251-254).");
			}

			VerifySequence(SourceOrderNames, RowValues(tableView, person => person.Name),
				"a cancelled sort must leave the row order alone");

			var indicator = FindSortIndicator(tableView, 0);
			if (indicator != null)
			{
				Verify.AreEqual(SortIndicatorDirection.None, indicator.Direction,
					"The header glyph is suppressed along with the sort state.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies Sorted is raised once after the new order has been applied.")]
	public async Task VerifySortedFiresAfterSuccessfulSort()
	{
		TableView tableView = null;
		TableViewColumn seenColumn = null;
		var seenDirection = SortDirection.None;
		var directionInsideHandler = SortDirection.None;
		List<string> orderInsideHandler = null;
		var raiseCount = 0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();

			tableView.Sorted += (sender, args) =>
			{
				raiseCount++;
				seenColumn = args.Column;
				seenDirection = args.Direction;
				directionInsideHandler = tableView.Columns[0].SortDirection;
				orderInsideHandler = RowValues(tableView, person => person.Name);
			};

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, raiseCount, "Sorted must be raised exactly once per successful sort.");
			Verify.AreEqual(tableView.Columns[0], seenColumn, "The args must carry the column that changed.");
			Verify.AreEqual(SortDirection.Ascending, seenDirection, "The args must carry the applied direction.");
			Verify.AreEqual(SortDirection.Ascending, directionInsideHandler,
				"Sorted is raised AFTER the change, so the handler must see the new direction (TableView.idl:598-599).");

			VerifySequence(NameAscendingOrder, orderInsideHandler,
				"the new order must already be in place when Sorted runs");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cancelled sort raises no Sorted event.")]
	public async Task VerifySortedDoesNotFireWhenCanceled()
	{
		TableView tableView = null;
		var sortedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			tableView.Sorting += (sender, args) => args.Cancel = true;
			tableView.Sorted += (sender, args) => sortedCount++;
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending);
			tableView.ToggleSortDirection(tableView.Columns[1]);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual(0, sortedCount,
				"A cancelled sort must not report completion (TableView.idl:252-253)."));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ClearSort raises both sort events with the null-column clear-all sentinel.")]
	public async Task VerifySortEventsUseNullColumnSentinelForClearSort()
	{
		TableView tableView = null;
		var sortingRaises = 0;
		var sortedRaises = 0;
		TableViewColumn sortingColumn = null;
		TableViewColumn sortedColumn = null;
		var sortingDirection = SortDirection.Ascending;
		var sortedDirection = SortDirection.Ascending;
		var handlersArmed = false;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();

			tableView.Sorting += (sender, args) =>
			{
				if (!handlersArmed)
				{
					return;
				}

				sortingRaises++;
				sortingColumn = args.Column;
				sortingDirection = args.Direction;
			};

			tableView.Sorted += (sender, args) =>
			{
				if (!handlersArmed)
				{
					return;
				}

				sortedRaises++;
				sortedColumn = args.Column;
				sortedDirection = args.Direction;
			};

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			handlersArmed = true;
			tableView.ClearSort();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, sortingRaises, "ClearSort must raise Sorting once.");
			Verify.AreEqual(1, sortedRaises, "ClearSort must raise Sorted once.");
			Verify.IsNull(sortingColumn, "A null Column is the clear-all sentinel on Sorting (TableView.idl:245).");
			Verify.IsNull(sortedColumn, "A null Column is the clear-all sentinel on Sorted (TableView.idl:261).");
			Verify.AreEqual(SortDirection.None, sortingDirection, "A clear-all is a change to None.");
			Verify.AreEqual(SortDirection.None, sortedDirection, "A clear-all is a change to None.");
		});
	}

	#endregion

	#region 7.5 Sort edge cases

	[TestMethod]
	[TestProperty("Description", "Verifies two columns naming the same sort member path keep independent sort state.")]
	public async Task VerifyDuplicateSortMemberPathsTrackColumnIdentity()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeShapedPeople(), 600, 700);

			// The IDL calls this shape out explicitly: one column formats a value, another ranks
			// the same one. Both name the same path.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = "Name",
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
				SortMemberPath = "Name",
			});

			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = "Name again",
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
				SortMemberPath = "Name",
			});

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[1], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection,
				"The sorted column holds the state.");
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection,
				"Sort state is keyed on column identity, not on the path (TableView.idl:177-181).");

			VerifySequence(NameAscendingOrder, RowValues(tableView, person => person.Name),
				"a duplicate path must still sort correctly");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing the sorted column leaves the control coherent and still sortable.")]
	public async Task VerifyRemovingSortedColumnClearsSortState()
	{
		TableView tableView = null;
		TableViewColumn removed = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			removed = tableView.Columns[0];
			tableView.SortByColumn(removed, SortDirection.Ascending);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.Columns.Remove(removed));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			foreach (var column in tableView.Columns)
			{
				Verify.AreEqual(SortDirection.None, column.SortDirection,
					$"No remaining column may claim the sort that left with column '{removed.Header}'.");
			}

			// Whether the rows revert to source order or keep the departed column's ordering is
			// not specified anywhere. Logged for the spec question, not asserted.
			Log.Comment($"Row order after removing the sorted column: [{string.Join(", ", RowValues(tableView, person => person.Name))}].");

			// The real risk is a dangling reference to the removed column on the next sort.
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending),
				"The table must still be sortable after losing the sorted column.");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual(SortDirection.Descending, tableView.Columns[0].SortDirection,
				"The post-removal sort must take effect normally."));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column sort and a TableViewSource sort reconcile, last writer winning.")]
	public async Task VerifySortReconcilesWithTableViewSourceSort()
	{
		TableView tableView = null;
		TableViewSource source = null;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			tableView = CreateSortTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(NameAscendingOrder, RowValues(tableView, person => person.Name),
				"the column sort is the last writer so far"));

		// The source now writes the other axis. Exactly one axis is ever in force.
		RunOnUIThread.Execute(() => source.Sort("Role", SortDirection.Descending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(
				new[] { "Engineer", "Engineer", "Engineer", "Designer", "Designer", "Architect" },
				RowValues(tableView, person => person.Role),
				"the source sort must replace the column sort, not stack with it");

			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection,
				"A source-owned sort must clear the column indicator (TableView.idl:571-573).");
		});

		// And back the other way: the column writes last and must win again.
		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(
				Enumerable.Reverse(NameAscendingOrder).ToList(),
				RowValues(tableView, person => person.Name),
				"the column sort must replace the source sort when it writes last");

			Verify.AreEqual(SortDirection.Descending, tableView.Columns[0].SortDirection,
				"The column that wrote last owns the indicator.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies invoking a column header toggles the sort and updates the sort indicator.")]
	public async Task VerifyHeaderInvokeTogglesSortAndUpdatesIndicator()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateSortTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var provider = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0])
				.GetPattern(PatternInterface.Invoke) as IInvokeProvider;

			if (provider == null)
			{
				Verify.Fail("A sortable header must offer the Invoke pattern.");
				return;
			}

			provider.Invoke();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[0].SortDirection,
				"Invoking a header must advance the column's SortCycle by one step.");

			VerifySequence(NameAscendingOrder, RowValues(tableView, person => person.Name),
				"the gesture must drive the same pipeline the API drives");

			var indicator = FindSortIndicator(tableView, 0);
			if (indicator == null)
			{
				Verify.Fail("A sortable header must carry a SortIndicator.");
				return;
			}

			Verify.AreEqual(SortIndicatorDirection.Ascending, indicator.Direction,
				"The glyph must follow the sort state.");
		});
	}

	#endregion
}

// A probe exposing the protected GetSortMemberPathCore so a test can assert path resolution
// directly rather than inferring it from a resulting row order.
internal partial class SortPathProbeColumn : TableViewColumn
{
	public string ReadSortMemberPath() => GetSortMemberPathCore();
}

internal partial class SortPathProbeTextColumn : TableViewTextColumn
{
	public string ReadSortMemberPath() => GetSortMemberPathCore();
}

// Orders by name length first, then ordinally. Deliberately not alphabetical, so a default
// comparison cannot produce this order by coincidence.
internal sealed class LengthThenOrdinalComparer : ITableViewSortComparer
{
	public int CompareCount { get; private set; }

	public int Compare(object left, object right)
	{
		CompareCount++;

		var leftName = (left as ShapedPerson)?.Name ?? string.Empty;
		var rightName = (right as ShapedPerson)?.Name ?? string.Empty;

		if (leftName.Length != rightName.Length)
		{
			return leftName.Length - rightName.Length;
		}

		return string.CompareOrdinal(leftName, rightName);
	}
}

internal static class TableViewSortingTestHelpers
{
	// The six-person fixture in source order. Named so a test asserting "unchanged" says what
	// unchanged means.
	internal static readonly string[] SourceOrderNames =
		{ "Asha", "Diego", "Mei", "Rafa", "Ines", "Owen" };

	internal static readonly string[] NameAscendingOrder =
		{ "Asha", "Diego", "Ines", "Mei", "Owen", "Rafa" };

	internal static TableView CreateSortTable() => CreateSortTable(MakeShapedPeople());

	// Two text columns with bindings but no explicit SortMemberPath, so the common authoring
	// shape (sort key resolved through the binding fallback) is what most tests exercise.
	internal static TableView CreateSortTable(object itemsSource)
		=> CreateTableView(itemsSource, headers: new[] { "Name", "Role" }, width: 600, height: 700);

	// One projected value per row, in projected order, read from the repeater's view so the
	// result describes the projection rather than whatever happens to be realized.
	internal static List<string> RowValues(TableView tableView, Func<ShapedPerson, string> selector)
		=> GetProjectedItems(tableView)
			.Select(item => item is ShapedPerson person ? selector(person) : "<not a ShapedPerson>")
			.ToList();

	// The SortIndicator built into a header, or null when no affordance was built. Named
	// "TableViewSortIndicator" by TableView::AppendSortIndicatorVisual.
	internal static SortIndicator FindSortIndicator(TableView tableView, int columnIndex)
		=> FindVisualChildrenByType<SortIndicator>(GetHeaderCell(tableView, columnIndex)).FirstOrDefault();
}
