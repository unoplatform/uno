// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Rows_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewRowTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 5.1 of the TableView test plan: row structure.
//
// TableView.idl:399-417 carries the row template contract in full - the four required parts, what
// each is for, and the CommonStates list - because "MIDL3 does not surface [TemplatePart], so this
// block is it". These tests cite it rather than reasoning from the template.
[TestClass]
public class TableViewRowStructureTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies the default row template exposes all four required named parts with their contract types.")]
	public async Task VerifyRowTemplatePartsExist()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewWithColumns();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = RequireFirstRow(tableView);

			// Every one of these is a Storyboard target for the Selected* states, so a missing part
			// does not fail at parse - it fails the first time a user selects a row.
			Verify.IsNotNull(row.FindVisualChildByName("PART_RootBorder") as Border,
				"PART_RootBorder must exist and be a Border; the CommonStates animate its Background.");
			Verify.IsNotNull(row.FindVisualChildByName("PART_CellsHost") as Panel,
				"PART_CellsHost must exist and be a Panel; it holds one cell wrapper per column.");
			Verify.IsNotNull(row.FindVisualChildByName("PART_CellForegroundPresenter") as ContentPresenter,
				"PART_CellForegroundPresenter must exist and be a ContentPresenter; the Selected* states animate its Foreground.");
			Verify.IsNotNull(row.FindVisualChildByName("PART_SelectionIndicator") as UIElement,
				"PART_SelectionIndicator must exist; the Selected* states animate its Opacity from 0 to 1.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies nothing on the cell path takes a local DataContext, so cells inherit the row's across recycle.")]
	public async Task VerifyCellPathInheritsRowDataContext()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateTableViewWithColumns(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = RequireFirstRow(tableView);
			Verify.AreEqual(items[0], row.DataContext,
				"A realized row must carry its backing source item as DataContext.");

			var host = GetCellsHost(row);
			var wrapper = GetRowCell(row, 0);
			var cellElement = GetCellContent(wrapper) as FrameworkElement;
			Verify.IsNotNull(cellElement, "The cell wrapper should host a generated cell element.");

			// The product states this as a load-bearing invariant: a local DataContext anywhere on
			// this path shadows inheritance, and the cell would then show the *previous* item's
			// data after recycle with nothing in the recycling path to correct it.
			//
			// This asserts the mechanism, not the symptom. 5.3 covers the symptom. Both are kept
			// because a change that re-pushed data per recycle would satisfy 5.3 and still be the
			// regression this guards - pushing layout-affecting data onto live in-tree cells during
			// the repeater's measure pass is what previously tripped a re-entrancy assert on scroll.
			VerifyNoLocalDataContext(host, "PART_CellsHost");
			VerifyNoLocalDataContext(wrapper, "the cell wrapper Border");
			VerifyNoLocalDataContext(cellElement, "the generated cell element");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell responds to hit testing across its whole area, not only where its text is.")]
	public async Task VerifyCellWrapperIsHitTestableAcrossItsPadding()
	{
		// Pins a fixed bug. A Border with a null Background does not hit-test, so before the fix a
		// click anywhere in a cell outside the text itself resolved no column at all: no current
		// cell, and double-click-to-edit silently did nothing across most of a wide cell.
		//
		// Asserted through a real hit test rather than by reading Background: the transparent brush
		// is the current mechanism, and a test that asserted the mechanism would have to be
		// rewritten if the fix ever moved to IsHitTestVisible or to a different element.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// A column far wider than its short text leaves a large slice of the wrapper with no
			// TextBlock over it, which is exactly the area that used to be dead.
			tableView = CreateTableViewWithColumns(columns: new[] { ("Name", new GridLength(320.0, GridUnitType.Pixel)) });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = RequireFirstRow(tableView);
			var wrapper = GetRowCell(row, 0);
			var text = GetCellContent(wrapper) as FrameworkElement;
			Verify.IsNotNull(text, "The cell wrapper should host a generated cell element.");

			Verify.IsGreaterThan(wrapper.ActualWidth, text.ActualWidth + 40.0,
				"Precondition: the cell must be meaningfully wider than its text, or there is no dead area to probe.");

			// Near the trailing edge of the cell, vertically centred - inside the wrapper, well clear
			// of the text.
			var probe = wrapper.TransformToVisual(null).TransformPoint(
				new Point(wrapper.ActualWidth - 8.0, wrapper.ActualHeight / 2.0));

			var textBounds = text.TransformToVisual(null).TransformPoint(new Point(0.0, 0.0));
			Verify.IsGreaterThan(probe.X, textBounds.X + text.ActualWidth,
				"Precondition: the probe point must lie outside the text element, or the test would pass without proving anything.");

			var hits = VisualTreeHelper.FindElementsInHostCoordinates(probe, tableView).ToList();
			Log.Comment($"Hit test at ({probe.X}, {probe.Y}) returned {hits.Count} elements.");

			Verify.IsTrue(hits.Contains(wrapper),
				"A press in a cell's empty area must reach that cell, or row selection and double-click-to-edit are dead across most of the cell.");
		});
	}
}

// Category 5.2 of the TableView test plan: generated cell content.
[TestClass]
public class TableViewCellContentTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies generated text cells are vertically centred and trim rather than wrap.")]
	public async Task VerifyGeneratedTextCellMetrics()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// Row 0 is short, row 1 is far too long for the column. Comparing the two isolates
			// trimming from every other height contributor.
			var items = new List<Person>
			{
				new Person { Name = "Al", Role = "R" },
				new Person { Name = VeryLongText, Role = "R" },
			};

			tableView = CreateTableViewWithColumns(items, columns: new[] { ("Name", new GridLength(90.0, GridUnitType.Pixel)) });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 1, "Both rows should be realized.");

			var shortText = GetCellContent(GetRowCell(rows[0], 0)) as TextBlock;
			var longText = GetCellContent(GetRowCell(rows[1], 0)) as TextBlock;
			Verify.IsNotNull(shortText, "A text column should generate a TextBlock.");
			Verify.IsNotNull(longText, "A text column should generate a TextBlock.");

			Verify.AreEqual(VerticalAlignment.Center, longText.VerticalAlignment,
				"Cell text is vertically centred within the row for the standard grid look.");
			Verify.AreEqual(TextTrimming.CharacterEllipsis, longText.TextTrimming,
				"Cell text must trim with an ellipsis rather than wrap.");
			Verify.AreEqual(Microsoft.UI.Text.FontWeights.Normal.Weight, longText.FontWeight.Weight,
				"Cell text uses the Fluent body weight.");

			// The point of CharacterEllipsis: without it the over-long value wraps and grows the
			// row, so every row with long text becomes a different height and the table stops
			// looking like a grid.
			Log.Comment($"Short cell height {shortText.ActualHeight}; long cell height {longText.ActualHeight}.");
			Verify.AreEqual(shortText.ActualHeight, longText.ActualHeight,
				"A value too wide for its column must be trimmed to one line, not wrapped onto several.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a template cell is hosted by the cell wrapper and renders the row's data item.")]
	public async Task VerifyTemplateCellRendersRowItem()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeItems();
			tableView = CreateTemplateColumnTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = RequireFirstRow(tableView);
			var wrapper = GetRowCell(row, 0);

			var presenter = GetCellContent(wrapper) as ContentPresenter;
			Verify.IsNotNull(presenter, "A template column generates a ContentPresenter hosted by the cell wrapper.");
			Verify.IsNotNull(presenter.ContentTemplate, "The presenter should carry the column's CellTemplate.");

			// The item reaches the template through the presenter's inherited DataContext. That is
			// asserted directly rather than through ContentPresenter.Content - see the remark in the
			// test plan: the Content binding the product installs never evaluates, and rendering
			// works entirely by inheritance. Asserting Content here would pin a mechanism the
			// product does not actually use.
			Verify.AreEqual(items[0], presenter.DataContext,
				"A template cell must resolve the row's data item, or its template binds against nothing.");

			var inner = presenter.FindVisualChildByType<TextBlock>();
			Verify.IsNotNull(inner, "The CellTemplate should have inflated inside the presenter.");
			Verify.AreEqual(items[0].Name, inner.Text,
				"A template cell must render the values of the row's data item.");
		});
	}
}

// Category 5.3 of the TableView test plan: recycling.
[TestClass]
public class TableViewRowRecyclingTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies recycled text and template cells show the values of the items they now represent.")]
	public async Task VerifyRecycledCellsShowNewItemValues()
	{
		// Both column types are checked in one table because they reach their content by genuinely
		// different routes - a Text binding set through BindingOperations on the TextBlock, versus a
		// Content binding sourced from the cell wrapper - so a regression in one would not break the
		// other. Category 3 covers the third route, an app-authored GenerateElementCore override.
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(120);
			tableView = CreateMixedColumnTable(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var before = GetRealizedRows(tableView).Select(row => row.DataContext).ToList();
			Verify.IsGreaterThan(before.Count, 0, "Rows should be realized before scrolling.");
			Log.Comment($"{before.Count} rows realized before scrolling.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 1600.0);

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows should still be realized after scrolling.");

			bool sawRecycledItem = false;

			foreach (var row in rows)
			{
				var item = row.DataContext as Person;
				Verify.IsNotNull(item, "Every realized row must carry a data item after scrolling.");

				if (items.IndexOf(item) > 4)
				{
					sawRecycledItem = true;
				}

				var text = GetCellContent(GetRowCell(row, 0)) as TextBlock;
				Verify.IsNotNull(text, "The text column should generate a TextBlock.");
				Verify.AreEqual(item.Name, text.Text,
					"A recycled text cell must show the value of the item its row now represents.");

				var presenter = GetCellContent(GetRowCell(row, 1)) as ContentPresenter;
				Verify.IsNotNull(presenter, "The template column should generate a ContentPresenter.");

				// Asserted on the rendered output rather than on ContentPresenter.Content: the
				// Content binding the product installs never evaluates (see 5.2), so the item
				// reaches the template purely through the presenter's inherited DataContext. The
				// rendered text is what the user sees either way.
				Verify.AreEqual(item, presenter.DataContext,
					"A recycled template cell must resolve the item its row now represents.");

				var templateText = presenter.FindVisualChildByType<TextBlock>();
				Verify.IsNotNull(templateText, "The CellTemplate should have inflated inside the presenter.");
				Verify.AreEqual(item.Name, templateText.Text,
					"A recycled template cell must render the values of the item its row now represents.");
			}

			// Without this the loop above would pass on a table that never scrolled and never
			// recycled anything.
			Verify.IsTrue(sawRecycledItem,
				"Precondition: scrolling must have brought later items into realization.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies banding follows the index a recycled row now occupies, not the one it had before.")]
	public async Task VerifyRecycledRowRebandsForItsNewIndex()
	{
		// Banding is the one row visual that is index-dependent rather than item-dependent, which is
		// why it needs a recycle test of its own while the rest are covered by the data assertions.
		TableView tableView = null;
		List<Person> items = null;
		// Brushes are DependencyObjects, so they must be created on the UI thread.
		SolidColorBrush baseBrush = null;
		SolidColorBrush alternateBrush = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(120);
			tableView = CreateTableViewWithColumns(items);
			baseBrush = new SolidColorBrush(Colors.LightGray);
			alternateBrush = new SolidColorBrush(Colors.LightBlue);
			tableView.RowBackground = baseBrush;
			tableView.AlternatingRowBackground = alternateBrush;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => VerifyBanding(tableView, items, baseBrush, alternateBrush, "before scrolling"));

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 1600.0);

		RunOnUIThread.Execute(() =>
		{
			var indices = GetRealizedRows(tableView)
				.Select(row => items.IndexOf(row.DataContext as Person))
				.ToList();
			Log.Comment($"Realized item indices after scrolling: {string.Join(", ", indices)}.");

			Verify.IsTrue(indices.Any(index => index > 4),
				"Precondition: scrolling must have brought later items into realization.");

			// Both parities must appear, or a banding bug that ignored the index entirely could
			// still pass by accident.
			Verify.IsTrue(indices.Any(index => index % 2 == 0) && indices.Any(index => index % 2 != 0),
				"Precondition: both even and odd rows must be realized for the parity check to mean anything.");

			VerifyBanding(tableView, items, baseBrush, alternateBrush, "after scrolling");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.
	[Ignore("WinUI ignores this test: Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.")]
	[TestProperty("Description", "Verifies a recycled row does not carry the selection visual of the row it previously hosted.")]
	public async Task VerifyRecycledRowDoesNotInheritPreviousSelectionVisual()
	{
		TableView tableView = null;
		List<Person> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(120);
			tableView = CreateTableViewWithColumns(items);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(0);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var row = RequireFirstRow(tableView);
			Verify.IsTrue(row.IsSelected, "Precondition: the selected row must report IsSelected before scrolling.");
			Verify.AreEqual("Selected", GetCommonState(row),
				"Precondition: the selected row must be in the Selected state before scrolling.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 1600.0);

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "Rows should still be realized after scrolling.");

			Verify.IsFalse(rows.Any(row => ReferenceEquals(row.DataContext, items[0])),
				"Precondition: the selected item must have scrolled out of realization.");

			// Selection belongs to the item, not to the container. If the container kept the visual,
			// rows would highlight and un-highlight as the user scrolls, tracking position rather
			// than data.
			foreach (var row in rows)
			{
				Verify.IsFalse(row.IsSelected,
					"A recycled row hosting an unselected item must not report IsSelected.");

				// The DP and the visual state are updated by two separate calls, so a regression can
				// leave one correct and the other stale. Both are asserted.
				Verify.AreEqual("Normal", GetCommonState(row),
					"A recycled row hosting an unselected item must not keep a Selected* visual state.");
			}
		});
	}
}

// Category 5.4 of the TableView test plan: row backgrounds and gridlines.
[TestClass]
public class TableViewRowVisualsTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies RowBackground alone fills every row and AlternatingRowBackground then overrides only odd rows.")]
	public async Task VerifyRowBackgroundAloneFillsEveryRowAndAlternatingStripesOddRows()
	{
		// The interesting behavior is the precedence between the two brushes, which neither of them
		// exhibits alone: RowBackground is the base for every row, and AlternatingRowBackground
		// overrides odd rows only when it is also set (WPF DataGrid parity).
		TableView tableView = null;
		List<Person> items = null;
		// Brushes are DependencyObjects, so they must be created on the UI thread.
		SolidColorBrush baseBrush = null;
		SolidColorBrush alternateBrush = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(6);
			tableView = CreateTableViewWithColumns(items);
			baseBrush = new SolidColorBrush(Colors.LightGray);
			alternateBrush = new SolidColorBrush(Colors.LightBlue);
			tableView.RowBackground = baseBrush;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 1, "Several rows should be realized.");

			// The regression this catches: treating RowBackground as the *even* brush, which stripes
			// the table with the theme background on odd rows even though the app asked for one
			// uniform colour.
			foreach (var row in rows)
			{
				Verify.AreEqual(baseBrush, row.Background,
					"RowBackground alone must fill every row uniformly, odd rows included.");
			}

			tableView.AlternatingRowBackground = alternateBrush;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => VerifyBanding(tableView, items, baseBrush, alternateBrush, "after setting AlternatingRowBackground"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies background brush changes after load repaint realized rows, including clearing back to the theme brush.")]
	public async Task VerifyRowBackgroundChangeAfterLoadUpdatesLive()
	{
		TableView tableView = null;
		List<Person> items = null;
		// Brushes are DependencyObjects, so they must be created on the UI thread.
		SolidColorBrush firstBrush = null;
		SolidColorBrush secondBrush = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeManyItems(6);
			tableView = CreateTableViewWithColumns(items);
			firstBrush = new SolidColorBrush(Colors.LightGray);
			secondBrush = new SolidColorBrush(Colors.Khaki);
			tableView.RowBackground = firstBrush;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.AreEqual(firstBrush, row.Background, "Precondition: the initial brush must be applied.");
			}

			tableView.RowBackground = secondBrush;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.AreEqual(secondBrush, row.Background,
					"Changing RowBackground on a loaded control must repaint already-realized rows.");
			}

			tableView.RowBackground = null;
			tableView.AlternatingRowBackground = null;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Clearing runs a different path from setting - the local value is cleared before the
			// brush is chosen - so it is the only way to exercise the restore-the-theme-brush branch.
			foreach (var row in GetRealizedRows(tableView))
			{
				Verify.AreNotEqual(secondBrush, row.Background,
					"Clearing both brushes must release the banding fill and let the theme row background show.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies each GridLinesVisibility value draws exactly the separators it names.")]
	public async Task VerifyGridLinesVisibilityDrawsExpectedSeparators()
	{
		// The two axes are driven by two different mechanisms - the row's own BorderThickness for the
		// horizontal line, a per-cell Border for the vertical ones - so getting one right and the
		// other wrong is the likely regression.
		//
		// The enum is deliberately not in alphabetical or visual order (All=0, Horizontal=1, None=2,
		// Vertical=3) because it mirrors WPF's DataGridGridLinesVisibility for persisted casts, so
		// these cases are walked by name and never by numeric order.
		var cases = new[]
		{
			(Value: TableViewGridLinesVisibility.All, Horizontal: true, Vertical: true),
			(Value: TableViewGridLinesVisibility.Horizontal, Horizontal: true, Vertical: false),
			(Value: TableViewGridLinesVisibility.Vertical, Horizontal: false, Vertical: true),
			(Value: TableViewGridLinesVisibility.None, Horizontal: false, Vertical: false),
		};

		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewWithColumns();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		foreach (var testCase in cases)
		{
			var current = testCase;

			RunOnUIThread.Execute(() =>
			{
				tableView.GridLinesVisibility = current.Value;
				Content.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var row = RequireFirstRow(tableView);
				var wrapper = GetRowCell(row, 0);

				Log.Comment($"{current.Value}: row border {row.BorderThickness.Bottom}, cell border {wrapper.BorderThickness.Right}.");

				// "On" is asserted as non-zero rather than as an exact thickness: it comes from
				// ClearValue falling back to the TableViewRow style, which an app may legitimately
				// restyle. Pinning the value would make a restyle look like a product regression.
				// "Off" is a genuine zero and is asserted exactly.
				if (current.Horizontal)
				{
					Verify.IsGreaterThan(row.BorderThickness.Bottom, 0.0,
						$"{current.Value} must draw the horizontal row separator.");
				}
				else
				{
					Verify.AreEqual(0.0, row.BorderThickness.Bottom,
						$"{current.Value} must draw no horizontal row separator.");
				}

				if (current.Vertical)
				{
					Verify.IsGreaterThan(wrapper.BorderThickness.Right, 0.0,
						$"{current.Value} must draw the vertical cell separator.");
					Verify.IsNotNull(wrapper.BorderBrush,
						$"{current.Value} must give the vertical separator a brush, or it draws nothing.");
				}
				else
				{
					Verify.AreEqual(0.0, wrapper.BorderThickness.Right,
						$"{current.Value} must draw no vertical cell separator.");
				}
			});
		}
	}
}

// Category 5.5 of the TableView test plan: visual states.
//
// State is read from VisualStateManager.GetVisualStateGroups(PART_RootBorder) rather than by
// comparing brushes: the groups live on the template root, and several states share a brush, so the
// state name is the only unambiguous observable.
[TestClass]
public class TableViewRowVisualStateTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Ignore", "True")] // Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.
	[Ignore("WinUI ignores this test: Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.")]
	[TestProperty("Description", "Verifies selection enters the Selected state and that disabling a selected row gives SelectedDisabled.")]
	public async Task VerifyRowSelectedVisualStateAndDisabledPrecedence()
	{
		// The contract worth testing is the precedence between two orthogonal conditions, which
		// neither a selection-only nor a disabled-only test covers. Collapsing them would render a
		// selected row in a disabled table either as interactive-looking, or as plain disabled with
		// the selection invisible.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewWithColumns();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(0);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 1, "Several rows should be realized.");

			Verify.AreEqual("Selected", GetCommonState(rows[0]), "A selected row enters Selected.");
			Verify.AreEqual("Normal", GetCommonState(rows[1]), "An unselected row stays Normal.");

			// Set on the TableView, not on the rows: that is what an app does, and it additionally
			// asserts IsEnabled reaches the rows at all.
			tableView.IsEnabled = false;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Second settle. An ancestor IsEnabled change reaches the rows and updates their visual state
		// one full layout + idle pass LATER than the change itself. With only one settle this test
		// failed in isolation and passed when run after its sibling classes (whose extra pumping hid
		// the gap) - which briefly looked like a product bug. It is not: the state does converge, and
		// a real app simply renders it on the next frame. Do not remove this settle.
		RunOnUIThread.Execute(() => Content.UpdateLayout());
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var rows = GetRealizedRows(tableView);

			// Logged permanently: this separates "IsEnabled never reached the rows" from "the rows
			// know they are disabled but never updated their state".
			Log.Comment($"tableView.IsEnabled={tableView.IsEnabled}; row0.IsEnabled={rows[0].IsEnabled}; row1.IsEnabled={rows[1].IsEnabled}.");
			Verify.IsFalse(rows[0].IsEnabled, "Precondition: disabling the TableView must reach its rows.");

			Verify.AreEqual("SelectedDisabled", GetCommonState(rows[0]),
				"Disabling a table with a selected row must give SelectedDisabled - not Disabled, which would lose the selection, and not Selected, which would still look interactive.");
			Verify.AreEqual("Disabled", GetCommonState(rows[1]),
				"An unselected row in a disabled table enters Disabled.");
		});
	}
}
