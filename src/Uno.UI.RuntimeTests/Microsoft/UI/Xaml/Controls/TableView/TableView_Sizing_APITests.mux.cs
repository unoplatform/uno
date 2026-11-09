// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Sizing_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Private.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;

using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewSizingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 4.2 of the TableView test plan: ActualWidth resolution.
//
// TableView.idl:121-123 is the contract these tests pin: "Width carries GridLength intent;
// ActualWidth is the resolved, MinWidth/MaxWidth-clamped pixels ... Pixel = exact; Auto = widest
// realized cell ...; Star = a proportional share of the body viewport after fixed columns."
//
// None of these tests load a visual tree, and that is deliberate rather than a shortcut: Width,
// MinWidth and MaxWidth share one branch of TableViewColumn::OnPropertyChanged that resolves
// ActualWidth synchronously with no owner and no layout pass. Proving resolution does not
// require being loaded is part of the contract, because a column that reported a stale
// provisional width until it was attached would mis-size its very first arrange.
[TestClass]
public class TableViewColumnActualWidthTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a pixel Width resolves to exactly that many pixels on an unattached column.")]
	public void VerifyActualWidthFollowsPixelWidth()
	{
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewTextColumn();

			// Chosen above the documented MinWidth default of 20.0 (TableView.idl:130) so this
			// test observes the pixel path and not the lower clamp.
			column.Width = new GridLength(160.0, GridUnitType.Pixel);

			Verify.AreEqual(160.0, column.ActualWidth,
				"A pixel Width must resolve to exactly that value: TableView.idl:122 says 'Pixel = exact'.");

			// Resolution must track the property, not latch the first value it ever saw.
			column.Width = new GridLength(275.0, GridUnitType.Pixel);
			Verify.AreEqual(275.0, column.ActualWidth, "ActualWidth must follow a later pixel Width change.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ActualWidth clamps up to MinWidth from both the Width and the MinWidth side.")]
	public void VerifyActualWidthClampsToMinWidth()
	{
		RunOnUIThread.Execute(() =>
		{
			// Route 1: Width authored below an existing MinWidth.
			var column = new TableViewTextColumn { MinWidth = 90.0 };
			column.Width = new GridLength(30.0, GridUnitType.Pixel);

			Verify.AreEqual(90.0, column.ActualWidth, "ActualWidth must clamp up to MinWidth.");

			// Width is intent and ActualWidth is resolution; conflating the two is the likely
			// regression, so assert the authored value survived the clamp untouched.
			Verify.AreEqual(new GridLength(30.0, GridUnitType.Pixel), column.Width,
				"Clamping must not rewrite the authored Width.");

			// Route 2: MinWidth raised above an already-authored Width.
			var other = new TableViewTextColumn();
			other.Width = new GridLength(50.0, GridUnitType.Pixel);
			Verify.AreEqual(50.0, other.ActualWidth, "Precondition: the pixel width should resolve before MinWidth is raised.");

			other.MinWidth = 140.0;
			Verify.AreEqual(140.0, other.ActualWidth, "Raising MinWidth above Width must re-resolve ActualWidth.");
			Verify.AreEqual(new GridLength(50.0, GridUnitType.Pixel), other.Width,
				"Raising MinWidth must not rewrite the authored Width.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ActualWidth clamps down to MaxWidth from both the Width and the MaxWidth side.")]
	public void VerifyActualWidthClampsToMaxWidth()
	{
		// Kept separate from the MinWidth case: the two bounds are computed by different
		// expressions - the upper one additionally applies std::max(lo, MaxWidth) - so one can
		// regress without the other.
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewTextColumn { MaxWidth = 100.0 };
			column.Width = new GridLength(400.0, GridUnitType.Pixel);

			Verify.AreEqual(100.0, column.ActualWidth, "ActualWidth must clamp down to MaxWidth.");
			Verify.AreEqual(new GridLength(400.0, GridUnitType.Pixel), column.Width,
				"Clamping must not rewrite the authored Width.");

			var other = new TableViewTextColumn();
			other.Width = new GridLength(300.0, GridUnitType.Pixel);
			Verify.AreEqual(300.0, other.ActualWidth, "Precondition: the pixel width should resolve before MaxWidth is lowered.");

			other.MaxWidth = 120.0;
			Verify.AreEqual(120.0, other.ActualWidth, "Lowering MaxWidth below Width must re-resolve ActualWidth.");
			Verify.AreEqual(new GridLength(300.0, GridUnitType.Pixel), other.Width,
				"Lowering MaxWidth must not rewrite the authored Width.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies MinWidth wins when MaxWidth is authored below it.")]
	public void VerifyInvertedMinMaxResolvesToMinWidth()
	{
		// This is not a cosmetic edge case. A clamp written as std::clamp(v, MinWidth, MaxWidth)
		// with lo > hi is undefined behavior, not merely a wrong number, so the contradiction has
		// to resolve to a stated value. The stated invariant is in UpdateActualWidth ("Keep
		// std::clamp well-defined even when MinWidth exceeds MaxWidth", via hi = max(lo, MaxWidth)),
		// which makes MinWidth the winner by construction.
		//
		// Remark: TableView.idl does not say which bound wins when they contradict. That is a
		// spec gap - the behavior is defensible but undocumented, and the IDL should state it.
		RunOnUIThread.Execute(() =>
		{
			var column = new TableViewTextColumn
			{
				MinWidth = 200.0,
				MaxWidth = 50.0,
			};
			column.Width = new GridLength(120.0, GridUnitType.Pixel);

			Verify.AreEqual(200.0, column.ActualWidth,
				"With MaxWidth below MinWidth, MinWidth must win and ActualWidth must be well-defined.");

			// Both orderings of assignment must agree; a guard applied on only one property's
			// changed-callback would pass one and fail the other.
			var reversed = new TableViewTextColumn();
			reversed.Width = new GridLength(120.0, GridUnitType.Pixel);
			reversed.MaxWidth = 50.0;
			reversed.MinWidth = 200.0;

			Verify.AreEqual(200.0, reversed.ActualWidth,
				"The inverted-bounds result must not depend on the order MinWidth and MaxWidth were set.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies non-finite MinWidth/MaxWidth cannot put NaN or infinity into ActualWidth.")]
	public void VerifyNonFiniteMinMaxWidthDoesNotCorruptActualWidth()
	{
		// Width itself cannot carry a pathological value: GridLength rejects NaN, infinity and
		// negatives at construction, so asserting that would be testing XAML rather than
		// TableView. MinWidth and MaxWidth are plain Double DPs with no validation callback, so
		// they accept anything - that is the reachable, TableView-owned hazard.
		//
		// Remark: the product clamps in two places under two different rules. The resize path
		// guards explicitly (isfinite(MinWidth) && >= 0.0) while UpdateActualWidth does not guard
		// at all. This test is expected to expose that asymmetry; per the test protocol the
		// divergence is the product's to resolve, not the test's.
		RunOnUIThread.Execute(() =>
		{
			foreach (var (property, badValue, label) in NonFiniteBoundCases())
			{
				var column = new TableViewTextColumn();
				column.Width = new GridLength(150.0, GridUnitType.Pixel);

				if (property == "MinWidth")
				{
					column.MinWidth = badValue;
				}
				else
				{
					column.MaxWidth = badValue;
				}

				var resolved = column.ActualWidth;
				Log.Comment($"{property} = {label} produced ActualWidth {resolved}.");

				Verify.IsFalse(double.IsNaN(resolved),
					$"A {label} {property} must never put NaN into ActualWidth ({label}).");
				Verify.IsFalse(double.IsInfinity(resolved),
					$"A {label} {property} must never put infinity into ActualWidth ({label}).");
				Verify.IsTrue(resolved >= 0.0,
					$"A {label} {property} must never make ActualWidth negative ({label}).");

				// The authored intent must survive a rejected bound.
				Verify.AreEqual(new GridLength(150.0, GridUnitType.Pixel), column.Width,
					$"A {label} {property} must not destroy the authored Width.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an unresolved Auto or Star column reports a finite provisional ActualWidth and keeps its unit type.")]
	public void VerifyActualWidthIsProvisionalForAutoAndStar()
	{
		// The GridLength roundtrip half of this is WinRT struct boxing, not TableView behavior.
		// What TableView owns is the provisional value it reports before a real resolve can run:
		// real Auto and Star resolution need measured content or a viewport, and neither exists
		// on an unattached column. A degenerate provisional (0, NaN, infinity) would be arranged
		// to by the header host and every row panel.
		//
		// Remark: the IDL documents what Auto and Star mean but is silent on the provisional
		// value. The 120.0 asserted here is grounded in MUX_DEFAULT_VALUE("120.0") on ActualWidth
		// (TableView.idl:145), which at least makes the provisional consistent with the declared
		// default - but it is a spec gap.
		RunOnUIThread.Execute(() =>
		{
			var auto = new TableViewTextColumn();
			auto.Width = GridLength.Auto;

			Verify.AreEqual(GridUnitType.Auto, auto.Width.GridUnitType,
				"A non-pixel Width must not be coerced back to Pixel; that would destroy authored sizing intent.");
			Verify.AreEqual(c_provisionalWidth, auto.ActualWidth,
				"An unresolved Auto column must report the finite provisional width.");

			var star = new TableViewTextColumn();
			star.Width = new GridLength(2.0, GridUnitType.Star);

			Verify.AreEqual(GridUnitType.Star, star.Width.GridUnitType, "A star Width must stay a star Width.");
			Verify.AreEqual(2.0, star.Width.Value, "The star factor must survive.");
			Verify.AreEqual(c_provisionalWidth, star.ActualWidth,
				"An unresolved Star column must report the finite provisional width.");
		});
	}

	private static IEnumerable<(string Property, double Value, string Label)> NonFiniteBoundCases()
	{
		yield return ("MinWidth", double.NaN, "NaN");
		yield return ("MinWidth", double.PositiveInfinity, "+infinity");
		yield return ("MaxWidth", double.NaN, "NaN");
		// MaxWidth = +infinity is the documented default and is deliberately not a bad case.
		yield return ("MaxWidth", double.NegativeInfinity, "-infinity");
	}
}

// Category 4.3 of the TableView test plan: Auto and Star sizing.
//
// Unlike 4.2 these need a loaded, laid-out control with a finite viewport: ResolveColumnWidths
// runs only from TableView::MeasureOverride, only for owned and Visible columns, and Star
// additionally needs PART_BodyScroller.ViewportWidth().
//
// These tests never assert an exact resolved pixel value. Resolved widths are snapped to device
// pixels against XamlRoot.RasterizationScale() when UseLayoutRounding is set, so exact numbers
// are DPI-dependent and would be flaky on a HiDPI test machine. Relationships and tolerances
// only.
//
// Category 4.3 of the TableView test plan: Auto and Star resolution against a real layout pass.
//
// Unlike 4.2 these must be loaded: Auto needs realized cells to measure and Star needs a viewport
// to divide, so an unattached column can only ever report the provisional width.
//
// Resolved values are never asserted as exact pixels. The viewport basis is layout-rounded before
// distribution (TableView_Layout.cpp:241) and each resulting width is rounded again against the
// current RasterizationScale, so exact equality would encode the test machine's DPI. Ratios and
// relative comparisons carry the same contract without that fragility.
[TestClass]
public class TableViewColumnAutoAndStarSizingTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies an Auto column sizes to its widest realized cell rather than to the provisional default.")]
	public async Task VerifyAutoWidthFitsWidestRealizedCell()
	{
		TableView narrow = null;
		TableView wide = null;

		RunOnUIThread.Execute(() =>
		{
			// Two tables differing only in cell content length. Comparing them isolates the
			// effect of content width from every other contributor (padding, gridlines, density),
			// which an absolute assertion could not do.
			narrow = CreateAutoColumnTable("Name", new[] { "Al", "Bo", "Cy" });
			wide = CreateAutoColumnTable("Name", new[] { "Al", VeryLongText, "Cy" });

			Content = StackTables(narrow, wide);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var narrowWidth = narrow.Columns[0].ActualWidth;
			var wideWidth = wide.Columns[0].ActualWidth;
			Log.Comment($"Auto width with short cells: {narrowWidth}; with one long cell: {wideWidth}.");

			Verify.IsGreaterThan(wideWidth, narrowWidth,
				"An Auto column must size to its widest realized cell, so a long cell must widen the column.");

			// Guards against the failure where Auto silently degrades to the 120px provisional:
			// both tables would then report the same width and the comparison above would fail,
			// but this makes the diagnosis explicit.
			Verify.AreNotEqual(c_provisionalWidth, wideWidth,
				"An Auto column over realized content must not still be reporting the provisional width.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the header participates in Auto sizing, and stops participating when headers are hidden.")]
	public async Task VerifyAutoWidthAccountsForHeaderWidth()
	{
		TableView tableView = null;
		double withHeader = 0.0;

		RunOnUIThread.Execute(() =>
		{
			// Header far longer than any cell, so the header is unambiguously the widest
			// contributor and the column width is attributable to it alone.
			tableView = CreateAutoColumnTable(VeryLongText, new[] { "Al", "Bo", "Cy" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			withHeader = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with a long header: {withHeader}.");

			Verify.IsGreaterThan(withHeader, c_provisionalWidth,
				"A header wider than every cell must widen an Auto column; otherwise long headers clip.");

			// The header contributes only when headers are actually shown. That is a correctness
			// rule, not an optimization: a hidden header is not measured this pass, so its cached
			// measured width is stale and must not drive the column.
			tableView.HeadersVisibility = TableViewHeadersVisibility.None;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var withoutHeader = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width after hiding headers: {withoutHeader}.");

			Verify.IsLessThan(withoutHeader, withHeader,
				"A hidden header must stop contributing to Auto width; a stale header cache must not keep the column wide.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an Auto column widens when a wider row realizes and keeps that width when the row is recycled.")]
	public async Task VerifyAutoWidthGrowsAsWiderRowsRealize()
	{
		// Growth and shrink look alike on the way up, so the wide row must also scroll back out.
		TableView tableView = null;
		double initialWidth = 0.0;
		double grownWidth = 0.0;
		Person wideItem = null;

		RunOnUIThread.Execute(() =>
		{
			// The wide row sits far enough down that it cannot be realized at offset zero, so its
			// absence at the start and its absence again at the end are both genuine.
			var names = Enumerable.Range(0, 120).Select(i => $"Row {i}").ToArray();
			names[c_wideRowIndex] = VeryLongText;

			tableView = CreateAutoColumnTable("Name", names);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			wideItem = ((IReadOnlyList<Person>)tableView.ItemsSource)[c_wideRowIndex];

			Verify.IsFalse(IsItemRealized(tableView, wideItem),
				"Precondition: the wide row must not be realized at the top of the source, or there is nothing to scroll into view.");

			initialWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with only short rows realized: {initialWidth}.");
		});

		await ScrollBodyToItem(tableView, c_wideRowIndex);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(IsItemRealized(tableView, wideItem),
				"Precondition: the wide row must be realized after scrolling to it, or the growth assertion would be vacuous.");

			grownWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with the wide row realized: {grownWidth}.");

			Verify.IsGreaterThan(grownWidth, initialWidth,
				"An Auto column must widen when a wider cell realizes, otherwise the new content clips.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 0.0);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(IsItemRealized(tableView, wideItem),
				"Precondition: the wide row must be recycled after scrolling back, or the grow-only assertion would be vacuous.");

			var settledWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width after the wide row was recycled: {settledWidth}.");

			Verify.AreEqual(grownWidth, settledWidth,
				"An Auto column must keep its grown width when its widest realized cell is recycled; Auto grows within a data set.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies showing column headers mid data set does not shrink a grown Auto column to the on-screen rows.")]
	public async Task VerifyShowingHeadersKeepsGrownAutoWidth()
	{
		await VerifyChangeKeepsGrownAutoWidth(
			t => t.HeadersVisibility = TableViewHeadersVisibility.None,
			t => t.HeadersVisibility = TableViewHeadersVisibility.Column,
			"Showing a header narrower than the grown width must not reset the Auto column to the on-screen rows.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies hiding column headers mid data set does not shrink a grown Auto column to the on-screen rows.")]
	public async Task VerifyHidingHeadersKeepsGrownAutoWidth()
	{
		await VerifyChangeKeepsGrownAutoWidth(
			t => t.HeadersVisibility = TableViewHeadersVisibility.Column,
			t => t.HeadersVisibility = TableViewHeadersVisibility.None,
			"Hiding the headers drops only the header's width; the width grown by a recycled row must be kept.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies changing a column's tooltip binding does not shrink its grown Auto width.")]
	public async Task VerifyCellToolTipBindingChangeKeepsGrownAutoWidth()
	{
		await VerifyChangeKeepsGrownAutoWidth(
			t => { },
			t => t.Columns[0].CellToolTipBinding = new Binding { Path = new PropertyPath("Name") },
			"A tooltip does not change what the cell displays, so the grown Auto width must be kept.");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies changing a column's editing template while no edit is open does not shrink its grown Auto width.")]
	public async Task VerifyCellEditingTemplateChangeKeepsGrownAutoWidth()
	{
		await VerifyChangeKeepsGrownAutoWidth(
			t => { },
			t => t.Columns[0].CellEditingTemplate = (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""><TextBox /></DataTemplate>"),
			"An editing template does not change what the cell displays, so the grown Auto width must be kept.");
	}

	private async Task VerifyChangeKeepsGrownAutoWidth(Action<TableView> setup, Action<TableView> change, string message)
	{
		TableView tableView = null;
		double initialWidth = 0.0;
		double grownWidth = 0.0;
		Person wideItem = null;

		RunOnUIThread.Execute(() =>
		{
			var names = Enumerable.Range(0, 120).Select(i => $"Row {i}").ToArray();
			names[c_wideRowIndex] = VeryLongText;

			tableView = CreateAutoColumnTable("Name", names);
			setup(tableView);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			wideItem = ((IReadOnlyList<Person>)tableView.ItemsSource)[c_wideRowIndex];
			initialWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with only short rows realized: {initialWidth}.");
		});

		await ScrollBodyToItem(tableView, c_wideRowIndex);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(IsItemRealized(tableView, wideItem),
				"Precondition: the wide row must be realized after scrolling to it.");

			grownWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with the wide row realized: {grownWidth}.");

			Verify.IsGreaterThan(grownWidth, initialWidth,
				"Precondition: the wide row must widen the Auto column, or the kept-width assertion would be vacuous.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 0.0);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(IsItemRealized(tableView, wideItem),
				"Precondition: the wide row must be recycled before the change, or only on-screen rows are tested.");

			change(tableView);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var widthAfterChange = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width after the change: {widthAfterChange}.");

			Verify.AreEqual(grownWidth, widthAfterChange, message);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a new ItemsSource starts a new data set, so an Auto column can narrow to the new content.")]
	public async Task VerifyAutoWidthResetsWhenItemsSourceChanges()
	{
		TableView tableView = null;
		double wideWidth = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutoColumnTable("Name", new[] { "Al", VeryLongText, "Cy" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			wideWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with a long cell: {wideWidth}.");

			tableView.ItemsSource = new[] { "Al", "Bo", "Cy" }
				.Select(name => new Person { Name = name, Role = "Role" })
				.ToList();
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var narrowWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width after replacing ItemsSource with short cells: {narrowWidth}.");

			Verify.IsLessThan(narrowWidth, wideWidth,
				"A new ItemsSource is a new data set: the grow-only Auto width must reset so the column fits the new content.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an Auto column follows its current header width, so turning off sorting gives back the sort indicator's space.")]
	public async Task VerifyAutoWidthFollowsCurrentHeaderWidth()
	{
		TableView tableView = null;
		double sortableWidth = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutoColumnTable("A column header wider than its cells", new[] { "Al", "Bo", "Cy" });
			tableView.CanUserSortColumns = true;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			sortableWidth = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width with a sortable header: {sortableWidth}.");
			tableView.CanUserSortColumns = false;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var width = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width after turning off sorting: {width}.");
			Verify.IsLessThan(width, sortableWidth,
				"The header no longer reserves the sort indicator, so a header-driven Auto column must narrow.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies star columns split the viewport left over after fixed columns, in proportion to their factors.")]
	public async Task VerifyStarWidthDividesRemainingViewport()
	{
		TableView tableView = null;
		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewWithColumns(
				width: 600,
				columns: new[]
				{
					("Fixed", new GridLength(100.0, GridUnitType.Pixel)),
					("One", new GridLength(1.0, GridUnitType.Star)),
					("Two", new GridLength(2.0, GridUnitType.Star)),
				});

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var fixedWidth = tableView.Columns[0].ActualWidth;
			var one = tableView.Columns[1].ActualWidth;
			var two = tableView.Columns[2].ActualWidth;
			var viewport = GetBodyViewportWidth(tableView);
			Log.Comment($"viewport={viewport}, fixed={fixedWidth}, 1*={one}, 2*={two}.");

			Verify.AreEqual(100.0, fixedWidth, "A pixel column must keep its exact width while star columns divide the rest.");

			Verify.IsGreaterThan(one, 0.0, "A star column must receive a share of the viewport.");
			Verify.AreNotEqual(c_provisionalWidth, one,
				"A star column in a bounded host must actually resolve, not sit at the provisional width.");

			// Ratio rather than absolute pixels: the viewport basis is layout-rounded before
			// distribution and each resolved width is snapped again, so exact values are
			// DPI-dependent. A whole device pixel of tolerance absorbs both roundings.
			VerifyNear(2.0 * one, two, 2.0,
				"A 2* column must be about twice a 1* column.");

			// The point of star sizing is that the visible columns fill the viewport.
			VerifyNear(viewport, fixedWidth + one + two, 2.0,
				"Fixed plus star columns must fill the body viewport.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a star column that would violate a bound is fixed at that bound and the rest re-divide the remainder.")]
	public async Task VerifyStarWidthClampsAtBoundAndRedistributes()
	{
		// This covers the most intricate logic in the whole sizing engine: an iterative
		// drop-and-redistribute loop explicitly modeled on WPF's ComputeStarColumnWidths. The
		// failure this catches is applying bounds as a final clamp *after* distribution, which
		// leaves the columns no longer filling the viewport - a visible gap or an overflow.
		//
		// Remark: TableView.idl says only "a proportional share of the body viewport after fixed
		// columns" and says nothing about how bounds interact with distribution. The expectation
		// asserted here is the WPF/CGrid convention that TableView_Layout.cpp names as its model.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			// Two equal star columns in a 600px-wide table: each would take about half. A
			// MinWidth well above half on the first forces the clamp-and-redivide path.
			tableView = CreateTableViewWithColumns(
				width: 600,
				columns: new[]
				{
					("Bounded", new GridLength(1.0, GridUnitType.Star)),
					("Free", new GridLength(1.0, GridUnitType.Star)),
				});

			tableView.Columns[0].MinWidth = 420.0;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var bounded = tableView.Columns[0].ActualWidth;
			var free = tableView.Columns[1].ActualWidth;
			var viewport = GetBodyViewportWidth(tableView);
			Log.Comment($"viewport={viewport}, bounded={bounded}, free={free}.");

			Verify.AreEqual(420.0, bounded,
				"A star column whose proportional share violates its MinWidth must be fixed at MinWidth.");

			// The redistribution assertion: the unconstrained column takes everything that is
			// left, not its original proportional share.
			VerifyNear(viewport - 420.0, free, 2.0,
				"The remaining star columns must re-divide the space left after the clamped column, so the viewport stays exactly filled.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a 0* column takes no width by default but honors an explicitly-set MinWidth.")]
	// TODO Uno: resolved widths snap to device pixels, so the exact 75 only holds at 100% (the scale WinUI's test runs at).
	[Uno.UI.RuntimeTests.RequiresScaling(1.0f)]
	public async Task VerifyZeroStarColumnTakesNoWidthUnlessMinWidthSet()
	{
		// The distinction being tested is invisible through the ordinary property getter: the
		// engine tells a default MinWidth from an explicitly-set one by probing
		// ReadLocalValue(...) == DependencyProperty.UnsetValue. Nothing else in the suite can
		// catch a regression here.
		//
		// Remark: entirely undocumented in TableView.idl - a spec gap. The expectation is the
		// WPF convention the code names ("WPF gives 0* a zero share").
		TableView defaulted = null;
		TableView explicitMin = null;

		RunOnUIThread.Execute(() =>
		{
			defaulted = CreateTableViewWithColumns(
				width: 600,
				columns: new[]
				{
					("Zero", new GridLength(0.0, GridUnitType.Star)),
					("Rest", new GridLength(1.0, GridUnitType.Star)),
				});

			explicitMin = CreateTableViewWithColumns(
				width: 600,
				columns: new[]
				{
					("Zero", new GridLength(0.0, GridUnitType.Star)),
					("Rest", new GridLength(1.0, GridUnitType.Star)),
				});
			explicitMin.Columns[0].MinWidth = 75.0;

			Content = StackTables(defaulted, explicitMin);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"0* with default MinWidth resolved to {defaulted.Columns[0].ActualWidth}.");
			Verify.AreEqual(0.0, defaulted.Columns[0].ActualWidth,
				"A 0* column with the default MinWidth must take no width at all.");

			Log.Comment($"0* with MinWidth=75 resolved to {explicitMin.Columns[0].ActualWidth}.");
			Verify.AreEqual(75.0, explicitMin.Columns[0].ActualWidth,
				"An explicitly-set MinWidth must still be honored on a 0* column.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a star column in a width-to-content host still resolves to a finite, usable width.")]
	public async Task VerifyStarWidthInUnboundedHostFallsBackDeterministically()
	{
		// The contract being pinned is a graceful degradation: with no ordinary finite space to
		// divide, a star column must still end up finite and usable rather than resolving to 0 or
		// arranging an infinite cell.
		//
		// Remark on what is NOT asserted, because it turned out not to be observable. The
		// implementation describes the fallback as "leave Star columns at their provisional
		// width", guarded by a viewport that is zero or non-finite. But PART_BodyScroller is an
		// ordinary ScrollViewer, and once arranged it always reports a *finite* ViewportWidth -
		// in a width-to-content host the table simply self-sizes to its columns' provisional
		// widths and the star division then runs normally against that. So the guarded branch is
		// effectively only the pre-layout "not yet known" case, and asserting the provisional
		// value here would pass for the wrong reason (with a single 1* column, dividing the
		// viewport and falling back to the provisional produce the same number). This test
		// therefore asserts the observable contract - finite, positive, not degenerate - and the
		// mismatch between the stated mechanism and the reachable behavior is flagged in the plan.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewWithColumns(
				width: double.NaN,
				columns: new[]
				{
					("Star", new GridLength(1.0, GridUnitType.Star)),
					("WiderStar", new GridLength(3.0, GridUnitType.Star)),
				});

			// Width-to-content host: a horizontal StackPanel measures its child at infinite width,
			// so nothing above the table supplies a width for star columns to divide.
			var host = new StackPanel { Orientation = Orientation.Horizontal };
			host.Children.Add(tableView);

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var viewport = GetBodyViewportWidth(tableView);
			var first = tableView.Columns[0].ActualWidth;
			var second = tableView.Columns[1].ActualWidth;
			Log.Comment($"Unbounded host: viewport={viewport}, 1*={first}, 3*={second}, table width={tableView.ActualWidth}.");

			foreach (var (value, label) in new[] { (first, "1*"), (second, "3*") })
			{
				Verify.IsFalse(double.IsNaN(value), $"An unbounded host must not produce a NaN width for the {label} column.");
				Verify.IsFalse(double.IsInfinity(value), $"An unbounded host must not produce an infinite width for the {label} column.");
				Verify.IsGreaterThan(value, 0.0, $"An unbounded host must not collapse the {label} column to nothing.");
			}

			// The table itself must stay finite: an infinite column width would propagate straight
			// into the control's own desired size.
			Verify.IsFalse(double.IsInfinity(tableView.ActualWidth),
				"A star column in a width-to-content host must not give the table an infinite width.");
		});
	}
}

// Category 4.4 of the TableView test plan: layout invalidation.
//
// Header and CellTemplate changes route through two different branches of
// TableViewColumn::OnPropertyChanged, so they can regress independently. Density is deliberately
// not tested here - it is already covered by category 13, where the density resource keys live,
// and the observable ("read the height after setting Density") would be identical.
//
// There is intentionally no test of the invalidation machinery itself. RequestColumnWidthResolve
// calls InvalidateMeasure synchronously and converges within the same layout tick, so there is no
// queued state to observe mid-flight; asserting InvalidateMeasure call counts would test the
// implementation rather than the contract.
[TestClass]
public class TableViewColumnLayoutInvalidationTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies replacing a loaded Auto column's Header re-runs the width resolve.")]
	public async Task VerifyHeaderChangeRemeasuresAutoColumn()
	{
		TableView tableView = null;
		double before = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateAutoColumnTable("Name", new[] { "Al", "Bo", "Cy" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			before = tableView.Columns[0].ActualWidth;

			// The change under test. Nothing else is touched: no explicit invalidation, no
			// re-set ItemsSource, no re-add of the column.
			tableView.Columns[0].Header = VeryLongText;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var after = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width before header change: {before}; after: {after}.");

			Verify.IsGreaterThan(after, before,
				"Changing Header on an Auto column must re-run the width resolve; otherwise the new header clips until something else invalidates measure.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing a loaded template column's CellTemplate re-runs the width resolve.")]
	public async Task VerifyCellTemplateChangeRemeasuresAutoColumn()
	{
		TableView tableView = null;
		double before = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableViewShell(MakeItems(), 500, 300);

			tableView.Columns.Add(MakeTemplateColumn("Cell", CreateFixedWidthTemplate(60.0), GridLength.Auto));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			before = tableView.Columns[0].ActualWidth;

			((TableViewTemplateColumn)tableView.Columns[0]).CellTemplate = CreateFixedWidthTemplate(260.0);
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var after = tableView.Columns[0].ActualWidth;
			Log.Comment($"Auto width before CellTemplate change: {before}; after: {after}.");

			Verify.IsGreaterThan(after, before,
				"Changing CellTemplate on an Auto column must re-run the width resolve; app-driven template swaps must not clip.");
		});
	}
}

// Category 4.5 of the TableView test plan: resize gating and the resize gesture.
//
// These are API tests, not interaction tests. ResizeGripper exposes the whole gesture
// programmatically - BeginDrag / TryDrag(totalDelta) / EndDrag(canceled) / TryKeyboardStep(key) -
// and ResizeGripper.idl states the intent outright: "Raise the same events a pointer drag would,
// so a keyboard step is indistinguishable from the gesture." Repo precedent for testing an
// interaction primitive this way is exact: Interactions\ButtonInteraction\APITests and
// Interactions\SliderInteraction\APITests both drive Microsoft.UI.Private.Controls types directly.
//
// The anchoring contract (TableView::AppendResizeGripperVisual): DragStarted captures
// startValue = column.ActualWidth() and startWidth = column.Width(); DragDelta writes
// clamp(startValue + TotalDelta, lo, hi) as a *pixel* GridLength; DragCompleted reverts to
// startWidth when canceled. The column owns the width; the gripper owns no value and no bounds.
//
// NOT IMPLEMENTED HERE: VerifyKeyboardResizeShiftMultiplierAppliesLargerDelta. The multiplier is
// selected by IsShiftKeyDown(), which reads *physical* keyboard state via
// InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift). TryKeyboardStep takes only a
// VirtualKey and offers no way to inject a modifier, so an API test would silently exercise the
// unshifted path and pass while proving nothing. It is the one genuinely input-bound item in this
// subcategory and belongs in category 11. VerifyKeyboardStepHonorsKeyboardIncrement below covers
// the step-size arithmetic; only the modifier read is left uncovered.
[TestClass]
public class TableViewColumnResizeTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies CanUserResizeColumns=false removes the gripper from every column while every header stays a tab stop.")]
	public async Task VerifyCanUserResizeColumnsFalseHidesGripper()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role", "City" });
			tableView.CanUserResizeColumns = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			for (int i = 0; i < tableView.Columns.Count; i++)
			{
				var headerCell = GetHeaderCell(tableView, i);

				Verify.IsNull(FindGripper(headerCell),
					$"Column {i} must have no resize gripper when the table opts out of resizing.");

				// Every visible header is a tab stop by product decision, whatever its capabilities,
				// so the header band has one uniform keyboard model. Opting out of resizing removes
				// only the gripper; it must not take the header out of the tab order.
				Verify.IsTrue(headerCell.IsTabStop,
					$"Column {i}'s header must stay a tab stop when the table opts out of resizing.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies per-column CanResize=false removes only that column's gripper and leaves every header a tab stop.")]
	public async Task VerifyPerColumnCanResizeFalseHidesGripper()
	{
		// TableViewColumn.idl:136: "Gates the resize affordance for this column only; the owner's
		// CanUserResizeColumns gates all of them." Both properties gate the *affordance*; neither
		// is documented to block a programmatic Width assignment, so this deliberately does not
		// assert that CanResize=false prevents setting Width.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role", "City" });
			tableView.Columns[1].CanResize = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(FindGripper(GetHeaderCell(tableView, 0)), "A resizable column must keep its gripper.");
			Verify.IsNull(FindGripper(GetHeaderCell(tableView, 1)), "The opted-out column must lose its gripper.");
			Verify.IsNotNull(FindGripper(GetHeaderCell(tableView, 2)), "The opt-out must not apply table-wide.");

			// CanResize gates only the gripper: every visible header stays a tab stop.
			for (int i = 0; i < tableView.Columns.Count; i++)
			{
				Verify.IsTrue(GetHeaderCell(tableView, i).IsTabStop,
					$"Column {i}'s header must stay a tab stop whether or not the column can be resized.");
			}
		});
	}

	// ProtectedCursor is protected, so it is read by reflection.
	private static InputSystemCursor GetGripperCursor(ResizeGripper gripper)
	{
		var property = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		Verify.IsNotNull(property, "UIElement.ProtectedCursor must be reachable by reflection.");
		return property.GetValue(gripper) as InputSystemCursor;
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the resize gripper holds one size cursor matching its drag axis, keeps it across pointer states and drags, and drops it while disabled.")]
	public async Task VerifyResizeGripperHoldsResizeCursor()
	{
		TableView tableView = null;
		ResizeGripper standalone = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(headers: new[] { "Name", "Role" });
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var gripper = FindGripper(GetHeaderCell(tableView, 0));
			Verify.IsNotNull(gripper, "A resizable column must have a gripper.");

			var cursor = GetGripperCursor(gripper);
			Verify.IsNotNull(cursor, "A loaded column gripper must carry a resize cursor.");
			Verify.AreEqual(InputSystemCursorShape.SizeWestEast, cursor.CursorShape,
				"A column gripper drags horizontally, so it must show the west-east size cursor.");

			foreach (var state in new[] { "PointerOver", "Pressed", "Normal" })
			{
				VisualStateManager.GoToState(gripper, state, false);
			}
			gripper.BeginDrag();
			gripper.TryDrag(10);
			gripper.EndDrag(true /* canceled */);
			Verify.AreSame(cursor, GetGripperCursor(gripper),
				"The cursor must be held, not recreated, across pointer states and a drag.");

			standalone = new ResizeGripper { Width = 8, Height = 24 };
			LoadContent(standalone);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// IsEnabledChanged is raised asynchronously; the gripper's own handler was attached first.
		using (var enabledChanged = new global::System.Threading.AutoResetEvent(false))
		{
			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(InputSystemCursorShape.SizeWestEast, GetGripperCursor(standalone)?.CursorShape,
					"A standalone gripper defaults to a horizontal drag.");

				standalone.DragOrientation = Orientation.Vertical;
				Verify.AreEqual(InputSystemCursorShape.SizeNorthSouth, GetGripperCursor(standalone)?.CursorShape,
					"A vertical drag must switch to the north-south size cursor.");

				standalone.IsEnabledChanged += (s, e) => SetIfNotDisposed(enabledChanged);
				standalone.IsEnabled = false;
			});

			Verify.IsTrue(enabledChanged.WaitOne(TimeSpan.FromSeconds(5)), "IsEnabledChanged must be raised.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsNull(GetGripperCursor(standalone), "A disabled gripper must not advertise resizing.");
				standalone.IsEnabled = true;
			});

			Verify.IsTrue(enabledChanged.WaitOne(TimeSpan.FromSeconds(5)), "IsEnabledChanged must be raised.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.AreEqual(InputSystemCursorShape.SizeNorthSouth, GetGripperCursor(standalone)?.CursorShape,
					"Re-enabling must restore the size cursor for the current axis.");
			});
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag applies the total offset against the width captured when the drag started.")]
	public async Task VerifyResizeDragUpdatesColumnWidth()
	{
		TableView tableView = null;
		double startWidth = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			startWidth = column.ActualWidth;
			var gripper = RequireGripper(tableView, 0);

			// Several steps inside one BeginDrag/EndDrag pair. A single-step drag would pass
			// whether the handler applies TotalDelta against the captured anchor or accumulates
			// each Delta incrementally; only a multi-step gesture separates the two. TotalDelta is
			// measured from where the drag began, so the final width must be anchored to the
			// ORIGINAL width, not to the intermediate one.
			gripper.BeginDrag();
			gripper.TryDrag(20.0);
			gripper.TryDrag(45.0);
			gripper.TryDrag(70.0);
			gripper.EndDrag(false);

			Log.Comment($"Width after drag: {column.Width.Value} (started at {startWidth}).");

			Verify.AreEqual(GridUnitType.Pixel, column.Width.GridUnitType, "A resize writes a pixel width.");
			Verify.AreEqual(startWidth + 70.0, column.Width.Value,
				"The final width must be the starting width plus the TOTAL offset, not the sum of the incremental deltas.");

			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(startWidth + 70.0, tableView.Columns[0].ActualWidth,
				"ActualWidth must follow the width the drag wrote.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag narrower than MinWidth stops at MinWidth.")]
	public async Task VerifyResizeDragRespectsMinWidth()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", new GridLength(200.0, GridUnitType.Pixel)));
			tableView.Columns[0].MinWidth = 80.0;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);

			gripper.BeginDrag();
			gripper.TryDrag(-5000.0);
			gripper.EndDrag(false);

			Log.Comment($"Width after dragging far past MinWidth: {column.Width.Value}.");

			Verify.AreEqual(80.0, column.Width.Value,
				"A drag must stop at MinWidth; going below it makes Width and ActualWidth disagree, which the user sees as a stuck gripper.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag wider than MaxWidth stops at MaxWidth.")]
	public async Task VerifyResizeDragRespectsMaxWidth()
	{
		// Kept separate from the MinWidth case: lo and hi are computed by different expressions,
		// so one bound can regress alone.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", new GridLength(200.0, GridUnitType.Pixel)));
			tableView.Columns[0].MaxWidth = 320.0;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);

			gripper.BeginDrag();
			gripper.TryDrag(5000.0);
			gripper.EndDrag(false);

			Log.Comment($"Width after dragging far past MaxWidth: {column.Width.Value}.");

			Verify.AreEqual(320.0, column.Width.Value, "A drag must stop at MaxWidth.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies cancelling a drag restores the authored GridLength including its unit type.")]
	public async Task VerifyResizeCancelRestoresAuthoredWidth()
	{
		// The unit-type half is the whole point and is easy to miss: the handler restores the
		// captured GridLength, not the resolved pixel number. Starting from Auto is what makes
		// the distinction observable - reverting a pixel width to itself proves nothing.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", GridLength.Auto));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);

			gripper.BeginDrag();
			gripper.TryDrag(90.0);

			Verify.AreEqual(GridUnitType.Pixel, column.Width.GridUnitType,
				"Precondition: the in-flight drag should have written a pixel width to revert from.");

			gripper.EndDrag(true /* canceled */);

			Log.Comment($"Width after cancel: {column.Width.GridUnitType} {column.Width.Value}.");

			Verify.AreEqual(GridUnitType.Auto, column.Width.GridUnitType,
				"Cancelling must restore the authored unit type; pressing Escape must not permanently convert an Auto column to Pixel.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a press that never moves cannot pin an Auto column to a pixel width.")]
	public async Task VerifyResizePressWithoutMovementDoesNotPinAutoColumn()
	{
		// A cancel-path test that starts from a Pixel column cannot detect a regression here,
		// because reverting a pixel width to itself is invisible. Nothing was written, so nothing
		// should be reverted or pinned.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", GridLength.Auto));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);

			gripper.BeginDrag();
			gripper.EndDrag(true /* canceled */);

			Verify.AreEqual(GridUnitType.Auto, column.Width.GridUnitType,
				"A press with no movement must leave Auto sizing intact; a stray click must not silently destroy it.");

			// The same must hold for a released press, not only a cancelled one.
			gripper.BeginDrag();
			gripper.EndDrag(false);

			Verify.AreEqual(GridUnitType.Auto, column.Width.GridUnitType,
				"Releasing without moving must also leave Auto sizing intact.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a second gripper cannot start a drag while another column's drag is in flight.")]
	public async Task VerifyConcurrentResizeDragIsRejected()
	{
		// Reachable only programmatically: with real input this needs genuine multi-touch, so an
		// API test covers a case an interaction test realistically cannot. The stated consequence
		// of allowing it is worse than a double write - the control tracks exactly one active
		// resize, so Escape could no longer reach the orphaned gesture.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var second = tableView.Columns[1];
			var firstGripper = RequireGripper(tableView, 0);
			var secondGripper = RequireGripper(tableView, 1);

			firstGripper.BeginDrag();
			Verify.IsTrue(firstGripper.IsDragging, "Precondition: the first drag should be in flight.");

			secondGripper.BeginDrag();
			secondGripper.TryDrag(100.0);

			Verify.IsFalse(secondGripper.IsDragging,
				"A second gripper must not run a concurrent drag while another is active.");
			Verify.AreEqual(150.0, second.Width.Value,
				"The rejected gesture must not have resized its column.");
			Verify.IsTrue(firstGripper.IsDragging,
				"The original drag must survive the rejected one, so Escape can still reach it.");

			firstGripper.EndDrag(true);
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies disabling a gripper mid-drag cancels the drag, reverts the width and lets another column resize.")]
	public async Task VerifyDisablingGripperMidDragCancelsResize()
	{
		TableView tableView = null;
		ResizeGripper gripper = null;
		using (var enabledChanged = new global::System.Threading.AutoResetEvent(false))
		{
			var completions = new List<bool>();

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateResizableTable(
					("First", new GridLength(150.0, GridUnitType.Pixel)),
					("Second", new GridLength(150.0, GridUnitType.Pixel)));
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				gripper = RequireGripper(tableView, 0);
				gripper.DragCompleted += (s, e) => completions.Add(e.Canceled);
				gripper.IsEnabledChanged += (s, e) => SetIfNotDisposed(enabledChanged);

				gripper.BeginDrag();
				gripper.TryDrag(40.0);
				Verify.AreEqual(190.0, tableView.Columns[0].Width.Value, "Precondition: the drag wrote a width.");

				gripper.IsEnabled = false;
			});

			Verify.IsTrue(enabledChanged.WaitOne(TimeSpan.FromSeconds(5)), "IsEnabledChanged must be raised.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Verify.IsFalse(gripper.IsDragging, "Disabling the gripper must end its drag.");
				Verify.AreEqual(1, completions.Count, "The drag must complete exactly once.");
				Verify.IsTrue(completions.Count == 1 && completions[0], "A drag ended by disabling is canceled, not committed.");
				Verify.AreEqual(150.0, tableView.Columns[0].Width.Value, "The canceled drag must revert the width.");

				var second = RequireGripper(tableView, 1);
				second.BeginDrag();
				Verify.IsTrue(second.IsDragging, "Another column must accept a resize once the disabled drag has ended.");
				second.TryDrag(30.0);
				second.EndDrag(false);
				Verify.IsGreaterThan(tableView.Columns[1].Width.Value, 150.0, "The subsequent resize must apply.");

				gripper.IsEnabled = true;
				gripper.EndDrag(true /* canceled */);
				Verify.AreEqual(1, completions.Count, "EndDrag after the drag ended must not raise DragCompleted again.");
			});
		}
	}

	// IsEnabledChanged is raised asynchronously, so it can arrive after the test disposed its event.
	private static void SetIfNotDisposed(global::System.Threading.EventWaitHandle handle)
	{
		try
		{
			handle.Set();
		}
		catch (ObjectDisposedException)
		{
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies re-parenting a gripper mid-drag keeps the drag: the stale Unloaded after the new Loaded must not cancel it.")]
	public async Task VerifyReparentingGripperMidDragKeepsDrag()
	{
		ResizeGripper gripper = null;
		StackPanel first = null;
		StackPanel second = null;
		var completions = new List<bool>();

		RunOnUIThread.Execute(() =>
		{
			gripper = new ResizeGripper { Width = 8, Height = 24 };
			gripper.DragCompleted += (s, e) => completions.Add(e.Canceled);
			first = new StackPanel();
			second = new StackPanel();
			first.Children.Add(gripper);
			var root = new StackPanel();
			root.Children.Add(first);
			root.Children.Add(second);
			LoadContent(root);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			gripper.BeginDrag();
			Verify.IsTrue(gripper.IsDragging, "Precondition: the drag starts.");
			first.Children.Remove(gripper);
			second.Children.Add(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(gripper.IsLoaded, "Precondition: the gripper is loaded in its new parent.");
			Verify.IsTrue(gripper.IsDragging, "A re-parent must not end the drag.");
			Verify.AreEqual(0, completions.Count, "A re-parent must not raise DragCompleted.");

			second.Children.Remove(gripper);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(gripper.IsDragging, "A real unload must end the drag.");
			Verify.IsTrue(completions.Count == 1 && completions[0], "A real unload cancels the drag once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag on a gripper that was never loaded completes without touching pointer capture.")]
	public void VerifyEndDragOnUnloadedGripperRaisesDragCompleted()
	{
		RunOnUIThread.Execute(() =>
		{
			var gripper = new ResizeGripper();
			var completions = new List<bool>();
			gripper.DragCompleted += (s, e) => completions.Add(e.Canceled);

			gripper.BeginDrag();
			Verify.IsTrue(gripper.IsDragging, "Precondition: an enabled gripper accepts BeginDrag before it is loaded.");
			gripper.TryDrag(20.0);
			gripper.EndDrag(false);

			Verify.IsFalse(gripper.IsDragging, "EndDrag must end the drag.");
			Verify.AreEqual(1, completions.Count, "EndDrag must raise DragCompleted once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a TableView from the tree mid-drag cancels the resize and leaves it resizable when reloaded.")]
	public async Task VerifyRemovingTableMidDragCancelsResize()
	{
		TableView tableView = null;
		ResizeGripper gripper = null;
		var completions = new List<bool>();

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			gripper = RequireGripper(tableView, 0);
			gripper.DragCompleted += (s, e) => completions.Add(e.Canceled);

			gripper.BeginDrag();
			gripper.TryDrag(40.0);
			Verify.AreEqual(190.0, tableView.Columns[0].Width.Value, "Precondition: the drag wrote a width.");

			Content = new Grid();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(gripper.IsDragging, "Unloading must end the drag.");
			Verify.AreEqual(1, completions.Count, "The drag must complete exactly once.");
			Verify.IsTrue(completions.Count == 1 && completions[0], "A drag ended by unloading is canceled.");
			Verify.AreEqual(150.0, tableView.Columns[0].Width.Value, "The canceled drag must revert the width.");

			gripper.EndDrag(true /* canceled */);
			Verify.AreEqual(1, completions.Count, "EndDrag on the detached gripper must not complete again.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var second = RequireGripper(tableView, 1);
			second.BeginDrag();
			Verify.IsTrue(second.IsDragging, "Another column must accept a resize after the unloaded drag ended.");
			second.TryDrag(30.0);
			second.EndDrag(false);
			Verify.AreEqual(180.0, tableView.Columns[1].Width.Value, "The subsequent resize must apply.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies resizing converts Auto to Pixel while preserving Star sizing intent.")]
	public async Task VerifyResizeConvertsAutoToPixelAndPreservesStar()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("Auto", GridLength.Auto),
				("Star", new GridLength(1.0, GridUnitType.Star)));

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			foreach (var index in new[] { 0, 1 })
			{
				var column = tableView.Columns[index];
				var authored = column.Width.GridUnitType;
				var gripper = RequireGripper(tableView, index);

				gripper.BeginDrag();
				gripper.TryDrag(40.0);
				gripper.EndDrag(false);

				Log.Comment($"{authored} column resolved to {column.Width.GridUnitType} {column.Width.Value} after a completed drag.");

				var expected = authored == GridUnitType.Auto ? GridUnitType.Pixel : GridUnitType.Star;
				Verify.AreEqual(expected, column.Width.GridUnitType,
					$"A completed drag on a {authored} column must preserve its intended post-resize sizing mode.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a locked Star column shares the viewport with the other Star columns once its authored share no longer fits after resizes and a MaxWidth change.")]
	public async Task VerifyLockedStarColumnFitsAfterResizeAndMaxWidthChange()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);

			// The clamp frees authored space; the locked share then no longer fits.
			tableView.Columns[3].MaxWidth = 50.0;
			tableView.UpdateLayout();

			VerifyLockedStarLayout(tableView, "after resizes and a MaxWidth change");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a resize after the locked Star column stopped holding its share never moves the locked column before the resized one, during or after the resize, by pointer, keyboard or UI Automation.")]
	public async Task VerifyDragAfterLockedStarCapHoldsEarlierLockedColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);
			tableView.Columns[3].MaxWidth = 50.0;
			tableView.UpdateLayout();
			VerifyLockedStarLayout(tableView, "before the resizes");

			var gripper = RequireGripper(tableView, 2);
			gripper.KeyboardIncrement = 20.0;
			var transform = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[2])
				.GetPattern(global::Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Transform) as global::Microsoft.UI.Xaml.Automation.Provider.ITransformProvider;
			var scale = tableView.XamlRoot.RasterizationScale;

			foreach (var (input, delta) in new[] { ("pointer", -20.0), ("keyboard", 20.0), ("UI Automation", -20.0) })
			{
				// End any hold an earlier step latched, so each input has to latch its own.
				tableView.Columns[3].MaxWidth = 51.0;
				tableView.UpdateLayout();
				tableView.Columns[3].MaxWidth = 50.0;
				tableView.UpdateLayout();
				VerifyLockedStarLayout(tableView, $"before the {input} resize");

				var before = tableView.Columns.Select(c => c.ActualWidth).ToArray();
				switch (input)
				{
					case "pointer":
						gripper.BeginDrag();
						gripper.TryDrag(delta);
						tableView.UpdateLayout();
						var during = tableView.Columns.Select(c => c.ActualWidth).ToArray();
						Log.Comment($"during={string.Join(", ", during)}.");
						VerifyNear(before[1], during[1], 0.5, "The drag must not move the locked column before the dragged one while it is in flight");
						gripper.EndDrag(false);
						break;
					case "keyboard":
						Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Right), "The keyboard step must be acted on.");
						break;
					default:
						transform.Resize((before[2] + delta) * scale, 0.0);
						break;
				}
				tableView.UpdateLayout();

				var after = tableView.Columns.Select(c => c.ActualWidth).ToArray();
				Log.Comment($"{input} {delta}: before={string.Join(", ", before)}, after={string.Join(", ", after)}.");
				VerifyNear(before[2] + delta, after[2], 1.0, $"The {input} resize must take effect");
				VerifyNear(before[0], after[0], 0.5, $"The {input} resize must not move the column before the resized one");
				VerifyNear(before[1], after[1], 0.5, $"The {input} resize must not move the locked column before the resized one, after it ends");
				// The last column is at most at its MaxWidth, so width a resize frees beyond that is left empty.
				var viewport = GetBodyViewportWidth(tableView);
				VerifyNear(Math.Min(50.0, viewport - after[0] - after[1] - after[2]), after[3], 1.0,
					$"The column after the {input} resize must give or take the width");
			}

			tableView.Columns[3].MaxWidth = 60.0;
			tableView.UpdateLayout();
			VerifyLockedStarLayout(tableView, "after a MaxWidth change ends the hold");
		});
	}
	[TestMethod]
	[TestProperty("Description", "Verifies collapsing a held locked Star column ends its hold, so resizes while it is hidden cannot leave a stale hold that overflows the viewport when it returns.")]
	public async Task VerifyCollapsingHeldLockedColumnEndsItsHold()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);
			tableView.Columns[3].MaxWidth = 50.0;
			tableView.UpdateLayout();
			DragAndVerify(tableView, 2, -20.0);

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			tableView.UpdateLayout();
			DragAndVerify(tableView, 0, 100.0);
			tableView.Columns[1].Visibility = Visibility.Visible;
			tableView.UpdateLayout();

			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");
			Verify.IsTrue(Math.Abs(widths.Sum() - viewport) <= 1.0, "The columns must fill the viewport without overflowing once the column returns.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag clamped at its bound does not knock a later locked Star column out of its authored share through rounding.")]
	public async Task VerifyDragClampedAtBoundKeepsLockedStarShare()
	{
		// The column before the locked one, so the locked column is not held and keeps its share
		// only while it fits the room the clamped drag left.
		await VerifyDragClampedAtBoundKeepsLockedColumn(0);
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a drag clamped at its bound does not end a held locked Star column's hold through rounding.")]
	public async Task VerifyDragClampedAtBoundKeepsHeldLockedStarShare()
	{
		// From a layout where the hold differs from the width the locked column would take without it.
		await VerifyDragClampedAtBoundKeepsLockedColumn(2);
	}

	private async Task VerifyDragClampedAtBoundKeepsLockedColumn(int draggedIndex)
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			// Off the pixel grid, so the bound the drag clamps at and the room layout measures round differently.
			tableView.Columns[3].MinWidth = 20.3;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			if (draggedIndex > 1)
			{
				DragLockedStarTableColumns(tableView);
				tableView.Columns[3].MaxWidth = 50.0;
				tableView.UpdateLayout();
			}

			var locked = tableView.Columns[1].ActualWidth;
			var gripper = RequireGripper(tableView, draggedIndex);
			gripper.BeginDrag();
			gripper.TryDrag(1000.0);
			gripper.EndDrag(false);
			tableView.UpdateLayout();

			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");
			VerifyNear(locked, widths[1], 1.0, "A drag clamped at its bound must leave the locked column at its share");
			Verify.IsTrue(Math.Abs(widths.Sum() - viewport) <= 1.0, "The columns must fill the viewport.");
		});
	}
	[TestMethod]
	[TestProperty("Description", "Verifies a MaxWidth that does not bind leaves a resized table with a locked Star column unchanged.")]
	public async Task VerifyNonBindingMaxWidthKeepsLockedStarLayout()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);
			var before = tableView.Columns.Select(c => c.ActualWidth).ToArray();

			tableView.Columns[3].MaxWidth = GetBodyViewportWidth(tableView);
			tableView.UpdateLayout();

			var after = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"before={string.Join(", ", before)}, after={string.Join(", ", after)}.");
			for (int i = 0; i < before.Length; ++i)
			{
				VerifyNear(before[i], after[i], 0.5, $"Column {i} must not change for a MaxWidth that does not bind");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a locked Star column takes its authored share when a later Star column clamps at MaxWidth with no prior resize.")]
	public async Task VerifyLockedStarColumnHonorsMaxWidthWithoutResize()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Columns[3].MaxWidth = 50.0;
			tableView.UpdateLayout();

			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");

			var expected = ExpectedStarWidths(viewport, new[] { 1.0, 2.0, 1.0, 3.0 },
				new[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 50.0 });
			for (int i = 0; i < expected.Length; ++i)
			{
				VerifyNear(expected[i], widths[i], 1.0, $"Column {i} must take its Star share");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a locked Star column trimmed to fit the room is never made narrower than its MinWidth.")]
	public async Task VerifyLockedStarTrimStopsAtMinWidth()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(100.0, GridUnitType.Pixel)),
				("Locked", new GridLength(0.0, GridUnitType.Star)),
				("Last", new GridLength(1.0, GridUnitType.Star)));
			tableView.Columns[1].CanResize = false;
			tableView.Columns[2].MinWidth = 20.0;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// One device pixel, inside the trim's rounding slack at any scale.
			var pixel = 1.0 / tableView.XamlRoot.RasterizationScale;
			tableView.Columns[1].MinWidth = pixel;
			// Leaves no room for the locked column's one-pixel share.
			tableView.Columns[0].Width = new GridLength(GetBodyViewportWidth(tableView) - 20.0, GridUnitType.Pixel);
			tableView.UpdateLayout();

			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={GetBodyViewportWidth(tableView)}, widths={string.Join(", ", widths)}.");
			Verify.IsTrue(widths[1] >= pixel - 0.0001, $"The locked column must not be trimmed below its MinWidth (got {widths[1]}).");
			Verify.IsTrue(widths[2] >= 20.0 - 0.0001, $"The last column must keep its MinWidth (got {widths[2]}).");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a held locked Star column lets go of its width when an Auto column grows so the hold no longer fits.")]
	public async Task VerifyAutoColumnGrowthEndsLockedStarHold()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(1.0, GridUnitType.Star)),
				("Locked", new GridLength(2.0, GridUnitType.Star)),
				("Third", new GridLength(1.0, GridUnitType.Star)),
				("Fourth", new GridLength(1.0, GridUnitType.Star)));
			tableView.Columns[1].CanResize = false;
			tableView.Columns.Add(MakeTemplateColumn("A", CreateFixedWidthTemplate(80.0), GridLength.Auto));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		double growth = 0.0;
		double autoBefore = 0.0;
		RunOnUIThread.Execute(() =>
		{
			DragAndVerify(tableView, 2, 20.0);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"held: widths={string.Join(", ", widths)}.");
			autoBefore = widths[4];

			// More than the last Star column can give up, but less than it and the locked column can.
			growth = (widths[3] + widths[1]) / 2.0;
			((TableViewTemplateColumn)tableView.Columns[4]).CellTemplate = CreateFixedWidthTemplate(80.0 + growth);
			tableView.UpdateLayout();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");
			Verify.IsTrue(widths[4] - autoBefore >= growth - 1.0, "The Auto column must have grown.");
			Verify.IsTrue(Math.Abs(widths.Sum() - viewport) <= 1.0, "The columns must fill the viewport without overflowing once the Auto column grows.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies setting a 0* column's MinWidth to its default value ends a held locked Star column's hold, even when the hold still fits.")]
	public async Task VerifyLocalDefaultMinWidthOnZeroStarEndsLockedStarHold()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			tableView.Columns.Add(MakeTextColumn("Zero", "Name", new GridLength(0.0, GridUnitType.Star)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// More empty space than the 0* column's explicit MinWidth takes, so the hold still fits.
			LatchLockedStarHold(tableView, -40.0);

			var zero = tableView.Columns[4];
			zero.MinWidth = zero.MinWidth;
			tableView.InvalidateMeasure();
			tableView.UpdateLayout();

			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");
			VerifyNear(zero.MinWidth, widths[4], 0.5, "The 0* column must take its explicitly set MinWidth");
			Verify.IsTrue(Math.Abs(widths.Sum() - viewport) <= 1.0, "The locked column must take the space left once its hold ends.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing a held locked Star column, resizing the first column through UI Automation and showing it again, all before layout runs, does not leave a stale hold.")]
	public async Task VerifyHoldEndsWhenHeldColumnCollapsesAndReturnsBeforeLayout()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			LatchLockedStarHold(tableView);

			var transform = new TableViewColumnHeaderAutomationPeer(tableView, tableView.Columns[0])
				.GetPattern(global::Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Transform) as global::Microsoft.UI.Xaml.Automation.Provider.ITransformProvider;
			var first = tableView.Columns[0].ActualWidth;

			tableView.Columns[1].Visibility = Visibility.Collapsed;
			transform.Resize((first + 50.0) * tableView.XamlRoot.RasterizationScale, 0.0);
			tableView.Columns[1].Visibility = Visibility.Visible;
			tableView.UpdateLayout();

			var viewport = GetBodyViewportWidth(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");
			VerifyNear(first + 50.0, widths[0], 1.0, "The UI Automation resize must take effect");
			VerifyLockedStarLayout(tableView, "once the column returns");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies changing any input a locked Star column's hold depends on ends the hold, even when the input is put back before the table is laid out again.")]
	public async Task VerifyLockedStarHoldEndsWhenAnInputChanges()
	{
		var inputs = new (string Name, Action<TableView> Change)[]
		{
			("MinWidth", t => { t.Columns[3].MinWidth = 30.0; t.Columns[3].ClearValue(TableViewColumn.MinWidthProperty); }),
			("authored Width", t => { t.Columns[3].Width = new GridLength(1.0, GridUnitType.Star); t.Columns[3].Width = new GridLength(3.0, GridUnitType.Star); }),
			("CanResize", t => { t.Columns[2].CanResize = false; t.Columns[2].CanResize = true; }),
			("Columns", t => { var c = t.Columns[3]; t.Columns.RemoveAt(3); t.Columns.Add(c); }),
			("CanUserResizeColumns", t => { t.CanUserResizeColumns = false; t.CanUserResizeColumns = true; }),
			("viewport", t => { t.Width = 500.0; t.UpdateLayout(); t.Width = 600.0; }),
		};

		foreach (var (name, change) in inputs)
		{
			Log.Comment($"Changing {name}.");
			TableView tableView = null;

			RunOnUIThread.Execute(() =>
			{
				tableView = CreateLockedStarTable();
				LoadContent(tableView);
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				LatchLockedStarHold(tableView);
				change(tableView);
				tableView.UpdateLayout();
			});

			await SettleLayout(tableView);

			RunOnUIThread.Execute(() => VerifyLockedStarLayout(tableView, $"after a {name} change"));
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a canceled resize puts the locked Star column holds back the way they were before it.")]
	public async Task VerifyCanceledResizeRestoresLockedStarHolds()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);
			tableView.Columns[3].MaxWidth = 50.0;
			tableView.UpdateLayout();

			// With no hold before it, a canceled resize must leave none: a later resize of an
			// earlier column then moves the locked column to its share.
			var gripper = RequireGripper(tableView, 2);
			gripper.BeginDrag();
			gripper.TryDrag(-20.0);
			tableView.UpdateLayout();
			gripper.EndDrag(true);
			tableView.UpdateLayout();
			DragAndVerify(tableView, 0, 20.0);
			VerifyLockedStarLayout(tableView, "after a canceled resize with no hold before it");

			// With a hold before it, a canceled resize must keep it.
			DragAndVerify(tableView, 2, -20.0);
			gripper = RequireGripper(tableView, 2);
			gripper.BeginDrag();
			gripper.TryDrag(10.0);
			tableView.UpdateLayout();
			gripper.EndDrag(true);
			tableView.UpdateLayout();
			var held = tableView.Columns[1].ActualWidth;
			DragAndVerify(tableView, 0, -20.0);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"held={held}, widths={string.Join(", ", widths)}.");
			VerifyNear(held, widths[1], 0.5, "The locked column must stay held after a canceled resize");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies shrinking a column in a table that already overflows its viewport does not move a locked Star column before it.")]
	public async Task VerifyShrinkInOverflowingTableHoldsLockedStarColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateLockedStarTable();
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			DragLockedStarTableColumns(tableView);
			var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			// Ten pixels more than the viewport leaves, so the table overflows with the locked column at its MinWidth.
			tableView.Columns[3].MinWidth = GetBodyViewportWidth(tableView) - widths[0] - widths[2] - tableView.Columns[1].MinWidth + 10.0;
			tableView.UpdateLayout();

			var before = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"viewport={GetBodyViewportWidth(tableView)}, before={string.Join(", ", before)}.");
			// The first step still overflows; the second frees space the locked column must not take.
			var gripper = RequireGripper(tableView, 2);
			gripper.BeginDrag();
			gripper.TryDrag(-5.0);
			tableView.UpdateLayout();
			gripper.TryDrag(-40.0);
			tableView.UpdateLayout();
			gripper.EndDrag(false);
			tableView.UpdateLayout();

			var after = tableView.Columns.Select(c => c.ActualWidth).ToArray();
			Log.Comment($"after={string.Join(", ", after)}.");
			VerifyNear(before[2] - 40.0, after[2], 1.0, "The resize must take effect");
			VerifyNear(before[1], after[1], 0.5, "Shrinking a later column must not move the locked column");
		});
	}

	// Leaves the locked column 1 held by a resize of column 2 while column 3 sits at its MaxWidth,
	// so the hold leaves empty space and VerifyLockedStarLayout fails until it ends.
	private static void LatchLockedStarHold(TableView tableView, double delta = -20.0)
	{
		DragLockedStarTableColumns(tableView);
		tableView.Columns[3].MaxWidth = 50.0;
		tableView.UpdateLayout();
		DragAndVerify(tableView, 2, delta);
		var viewport = GetBodyViewportWidth(tableView);
		var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
		Log.Comment($"held: viewport={viewport}, widths={string.Join(", ", widths)}.");
		Verify.IsTrue(widths.Sum() < viewport - 1.0, "The hold must leave empty space for the test to observe it ending.");
	}

	private static TableView CreateLockedStarTable()
	{
		var tableView = CreateResizableTable(
			("First", new GridLength(1.0, GridUnitType.Star)),
			("Locked", new GridLength(2.0, GridUnitType.Star)),
			("Third", new GridLength(1.0, GridUnitType.Star)),
			("Last", new GridLength(3.0, GridUnitType.Star)));
		tableView.Columns[1].CanResize = false;
		return tableView;
	}

	private static void DragLockedStarTableColumns(TableView tableView)
	{
		DragAndVerify(tableView, 0, 114.0);
		DragAndVerify(tableView, 2, 100.0);
	}

	private static void DragAndVerify(TableView tableView, int index, double delta)
	{
		var column = tableView.Columns[index];
		var start = column.ActualWidth;
		var gripper = RequireGripper(tableView, index);
		gripper.BeginDrag();
		gripper.TryDrag(delta);
		gripper.EndDrag(false);
		tableView.UpdateLayout();
		VerifyNear(start + delta, column.ActualWidth, 1.0, $"The drag on column {index} must take effect");
	}

	// Locked column 1 (2*) and column 3 (3*, MaxWidth) divide what the dragged columns 0 and 2 leave.
	private static void VerifyLockedStarLayout(TableView tableView, string when)
	{
		var viewport = GetBodyViewportWidth(tableView);
		var widths = tableView.Columns.Select(c => c.ActualWidth).ToArray();
		Log.Comment($"viewport={viewport}, widths={string.Join(", ", widths)}.");

		var expected = ExpectedStarWidths(viewport - widths[0] - widths[2], new[] { 2.0, 3.0 },
			new[] { double.PositiveInfinity, tableView.Columns[3].MaxWidth });
		Verify.IsTrue(Math.Abs(widths.Sum() - viewport) <= 1.0, $"The columns must fill the viewport {when}.");
		VerifyNear(expected[1], widths[3], 1.0, $"The MaxWidth column must sit at its Star share {when}");
		VerifyNear(expected[0], widths[1], 1.0, $"The locked column must take the rest of the Star space {when}");
	}

	// WPF star division: a column whose share passes its MaxWidth is fixed there and the rest re-divide.
	private static double[] ExpectedStarWidths(double available, double[] factors, double[] maxWidths)
	{
		var widths = new double[factors.Length];
		var open = Enumerable.Range(0, factors.Length).ToList();
		for (bool clamped = true; clamped && open.Count > 0;)
		{
			clamped = false;
			var unit = available / open.Sum(i => factors[i]);
			foreach (var i in open)
			{
				if (unit * factors[i] > maxWidths[i])
				{
					widths[i] = maxWidths[i];
					available -= maxWidths[i];
					open.Remove(i);
					clamped = true;
					break;
				}
			}
		}

		foreach (var i in open)
		{
			widths[i] = available * factors[i] / open.Sum(j => factors[j]);
		}
		return widths;
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the last Star column, which has no later Star column to trade width with, shows no gripper.")]
	public async Task VerifyPinnedStarColumnHidesGripper()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(1.0, GridUnitType.Star)),
				("Second", new GridLength(1.0, GridUnitType.Star)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(Visibility.Visible, RequireGripper(tableView, 0).Visibility,
				"Column 0 can trade width with the Star column after it, so its gripper must show.");
			Verify.AreEqual(Visibility.Collapsed, RequireGripper(tableView, 1).Visibility,
				"The last Star column cannot be dragged, so it must not offer a gripper.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column whose MinWidth equals its MaxWidth shows no gripper.")]
	public async Task VerifyFixedWidthColumnHidesGripper()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(120.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			column.MinWidth = 120.0;
			column.MaxWidth = 120.0;

			Verify.AreEqual(Visibility.Collapsed, RequireGripper(tableView, 0).Visibility,
				"A column whose MinWidth equals its MaxWidth can never change width, so it must not offer a gripper.");

			column.MaxWidth = double.PositiveInfinity;

			Verify.AreEqual(Visibility.Visible, RequireGripper(tableView, 0).Visibility,
				"Lifting MaxWidth must restore the gripper.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a fixed-width Star column does not count as a donor for the Star column before it.")]
	public async Task VerifyFixedWidthStarDonorPinsPreviousColumn()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(1.0, GridUnitType.Star)),
				("Second", new GridLength(1.0, GridUnitType.Star)));
			tableView.Columns[1].MinWidth = 150.0;
			tableView.Columns[1].MaxWidth = 150.0;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(Visibility.Collapsed, RequireGripper(tableView, 0).Visibility,
				"The only later Star column is fixed at 150 and cannot give width, so column 0 has no donor.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies gripper availability is re-evaluated when a column's Width changes.")]
	public async Task VerifyGripperAvailabilityTracksWidthChange()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(1.0, GridUnitType.Star)),
				("Second", new GridLength(1.0, GridUnitType.Star)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Columns[1].Width = new GridLength(100.0, GridUnitType.Pixel);

			Verify.AreEqual(Visibility.Visible, RequireGripper(tableView, 1).Visibility,
				"A Pixel column owns its width, so its gripper must show.");
			Verify.AreEqual(Visibility.Collapsed, RequireGripper(tableView, 0).Visibility,
				"Column 0 is now the last Star column and has nothing to trade width with.");

			tableView.Columns[1].Width = new GridLength(1.0, GridUnitType.Star);

			Verify.AreEqual(Visibility.Visible, RequireGripper(tableView, 0).Visibility,
				"Restoring the Star column must restore column 0's gripper.");
			Verify.AreEqual(Visibility.Collapsed, RequireGripper(tableView, 1).Visibility,
				"Restoring the Star column must pin it again.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the last column keeps its gripper when no column is Star, because the table may grow.")]
	public async Task VerifyLastColumnKeepsGripperWhenTableHasNoStar()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(
				("First", new GridLength(150.0, GridUnitType.Pixel)),
				("Second", new GridLength(150.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(Visibility.Visible, RequireGripper(tableView, 1).Visibility,
				"Without a Star column the extent grows, so the last column must stay resizable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a keyboard step resizes by the increment, ignores off-axis keys, and mirrors under RTL.")]
	public async Task VerifyKeyboardStepAdjustsWidthAndMirrorsInRightToLeft()
	{
		// Merged from two planned items. The RTL mirror is three lines inside TryKeyboardStep, so
		// it cannot regress independently of the LTR path, and an RTL assertion is meaningless
		// without the LTR baseline measured in the same test.
		//
		// Remark on what is asserted under RTL: ResizeGripper.idl says only "Only horizontal
		// mirrors under RTL" - it does not state which visual direction grows. The cpp comment at
		// the mirror claims "Left always shrinks visually", which does not obviously match the
		// code (Left is negated to +1 under RTL, and the host adds that to the width). Rather than
		// freeze either reading, this asserts the one thing both the IDL and the code agree on:
		// the RTL outcome for a given key is the opposite of the LTR outcome. The concrete visual
		// direction is a spec gap and is flagged in the test plan.
		TableView tableView = null;
		double ltrLeftDelta = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", new GridLength(200.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);
			gripper.KeyboardIncrement = 16.0;

			// Off-axis keys must be refused rather than swallowed: a swallowed key is reported as
			// handled and would block focus navigation.
			var before = column.Width.Value;
			Verify.IsFalse(gripper.TryKeyboardStep(VirtualKey.Up),
				"A vertical key must return false on a horizontal gripper.");
			Verify.IsFalse(gripper.TryKeyboardStep(VirtualKey.Down),
				"A vertical key must return false on a horizontal gripper.");
			Verify.AreEqual(before, column.Width.Value, "An off-axis key must not change the width.");

			// LTR baseline. Right grows, Left shrinks - the universal convention, and the only
			// reading consistent with the gripper sitting on the column's trailing edge.
			var start = column.Width.Value;
			Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Right), "Right is on the horizontal axis.");
			Verify.AreEqual(start + 16.0, column.Width.Value,
				"One Right step must widen the column by exactly KeyboardIncrement.");

			start = column.Width.Value;
			Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Left), "Left is on the horizontal axis.");
			ltrLeftDelta = column.Width.Value - start;
			Verify.AreEqual(-16.0, ltrLeftDelta,
				"Under LTR, one Left step must narrow the column by exactly KeyboardIncrement.");

			tableView.FlowDirection = FlowDirection.RightToLeft;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);
			gripper.KeyboardIncrement = 16.0;

			// Stated as a precondition so a mirror failure cannot be misread when the real cause
			// is FlowDirection not reaching the gripper's subtree.
			Verify.AreEqual(FlowDirection.RightToLeft, gripper.FlowDirection,
				"Precondition: FlowDirection must reach the gripper for the mirror to apply.");

			var start = column.Width.Value;
			Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Left), "Left is still on the horizontal axis under RTL.");
			var rtlLeftDelta = column.Width.Value - start;
			Log.Comment($"Left step delta: LTR {ltrLeftDelta}, RTL {rtlLeftDelta}.");

			Verify.AreEqual(16.0, Math.Abs(rtlLeftDelta),
				"An RTL keyboard step must still move by exactly KeyboardIncrement.");
			Verify.AreEqual(-ltrLeftDelta, rtlLeftDelta,
				"The horizontal axis must mirror under RTL, so the same key must move the width the opposite way.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the keyboard step uses KeyboardIncrement and rejects degenerate increments.")]
	public async Task VerifyKeyboardStepHonorsKeyboardIncrement()
	{
		// The API-testable substitute for the Shift-multiplier item.
		//
		// Remark: none of the sanitization is in the IDL beyond "Step size is host policy, so it
		// is settable" - a spec gap, though the behavior is defensible. The stated rationale is
		// that a non-finite or sub-deadband increment would be swallowed by TryDrag, leaving the
		// keyboard silently dead while still raising a start/complete pair that a host announces
		// to a screen reader as an unchanged width on every arrow press.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateResizableTable(("Name", new GridLength(300.0, GridUnitType.Pixel)));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var column = tableView.Columns[0];
			var gripper = RequireGripper(tableView, 0);

			gripper.KeyboardIncrement = 37.0;
			var start = column.Width.Value;
			Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Right));
			Verify.AreEqual(start + 37.0, column.Width.Value,
				"A host-set KeyboardIncrement must be the step size.");

			// A degenerate increment must fall back to a usable default, not produce a zero-width
			// or NaN step.
			foreach (var (increment, label) in new[]
			{
				(0.0, "zero"),
				(double.NaN, "NaN"),
				(double.PositiveInfinity, "infinity"),
			})
			{
				gripper.KeyboardIncrement = increment;
				start = column.Width.Value;
				Verify.IsTrue(gripper.TryKeyboardStep(VirtualKey.Right),
					$"A {label} increment must not stop the key from being acted on.");

				var moved = column.Width.Value - start;
				Log.Comment($"KeyboardIncrement={label} moved the width by {moved}.");

				Verify.IsFalse(double.IsNaN(moved), $"A {label} increment must not put NaN into the width.");
				Verify.IsFalse(double.IsInfinity(moved), $"A {label} increment must not put infinity into the width.");
				Verify.IsGreaterThan(moved, 0.0,
					$"A {label} increment must fall back to a usable default step, not leave the keyboard silently dead.");
			}
		});
	}
}

// Category 4.6 of the TableView test plan: frozen columns.
//
// These are API tests: pinning is applied as UIElement.Translation plus Canvas.ZIndex plus Clip on
// the cell elements, all of which are readable properties, and the body scroller is an ordinary
// ScrollViewer that ChangeView drives programmatically. No pointer input is involved.
//
// Spec basis: TableView.idl:4 "Column pinning edge; None scrolls naturally, Leading pins left,
// Trailing is reserved", and TableViewColumn.idl:147 "Pins the column during horizontal scroll;
// Leading is implemented, Trailing reserved."
//
// Category 4.6 of the TableView test plan: leading frozen columns.
[TestClass]
public class TableViewFrozenColumnTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a leading frozen column holds its viewport position while the rest scrolls.")]
	public async Task VerifyLeadingFrozenColumnStaysPinnedDuringHorizontalScroll()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(columnCount: 5);
			tableView.Columns[0].FrozenEdge = TableViewFrozenEdge.Leading;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ScrollBodyTo(tableView, ScrollAxis.Horizontal, c_scrollOffset);

		RunOnUIThread.Execute(() =>
		{
			var offset = GetBodyHorizontalOffset(tableView);
			Log.Comment($"Scrolled to horizontal offset {offset}.");
			Verify.IsGreaterThan(offset, 0.0, "Precondition: the body must actually have scrolled horizontally.");

			// Header and rows are pinned by two different call sites against one shared offset,
			// and the stated reason they share it is so they stay aligned. Divergence between the
			// two tears the column apart visually, so both are asserted in the same test.
			VerifyPinned(GetHeaderCell(tableView, 0), offset, "the header cell");
			VerifyNotPinned(GetHeaderCell(tableView, 1), "the first non-frozen header cell");

			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "At least one row should be realized.");

			foreach (var row in rows)
			{
				VerifyPinned(GetRowCell(row, 0), offset, "a frozen row cell");
				VerifyNotPinned(GetRowCell(row, 1), "a non-frozen row cell");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a template without its own scrollers does not latch onto an app ScrollViewer above the TableView.")]
	public async Task VerifyScrollerLookupStopsAtTheTableView()
	{
		TableView tableView = null;
		ScrollViewer appScroller = null;

		RunOnUIThread.Execute(() =>
		{
			EnsureTabularControlsResources();

			tableView = CreateScrollableTable(columnCount: 5);
			tableView.Width = 800;
			tableView.Columns[0].FrozenEdge = TableViewFrozenEdge.Leading;
			tableView.Template = (ControlTemplate)XamlReader.Load(
				@"<ControlTemplate
					xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
					xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					xmlns:tabular='using:Microsoft.UI.Xaml.Controls.Tabular'
					TargetType='tabular:TableView'>
					<Grid>
						<Grid.RowDefinitions>
							<RowDefinition Height='Auto' />
							<RowDefinition Height='*' />
						</Grid.RowDefinitions>
						<Border x:Name='PART_HeaderRow'>
							<tabular:TableViewCellsPanel x:Name='PART_HeaderHost' HorizontalAlignment='Left' />
						</Border>
						<ItemsRepeater x:Name='PART_RowsRepeater' Grid.Row='1'>
							<ItemsRepeater.Layout>
								<StackLayout Orientation='Vertical' />
							</ItemsRepeater.Layout>
						</ItemsRepeater>
					</Grid>
				</ControlTemplate>");

			appScroller = new ScrollViewer
			{
				Width = 300,
				Height = 300,
				HorizontalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
				Content = tableView,
			};
			LoadContent(appScroller);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(appScroller.ScrollableWidth, c_scrollOffset,
				"Precondition: the app scroller must be able to scroll the table horizontally.");
			appScroller.ChangeView(c_scrollOffset, null, null, true);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsGreaterThan(appScroller.HorizontalOffset, 0.0, "Precondition: the app scroller must have scrolled.");

			// Pinning against the app's scroller would counter-translate the frozen cell by its offset.
			Verify.AreEqual(0.0f, GetHeaderCell(tableView, 0).Translation.X,
				"A frozen header cell must not be pinned against an app-owned scroller.");
			var rows = GetRealizedRows(tableView);
			Verify.IsGreaterThan(rows.Count, 0, "At least one row should be realized.");
			foreach (var row in rows)
			{
				Verify.AreEqual(0.0f, GetRowCell(row, 0).Translation.X,
					"A frozen row cell must not be pinned against an app-owned scroller.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies several leading frozen columns stay pinned side by side, including after a sibling is collapsed.")]
	public async Task VerifyMultipleLeadingFrozenColumnsStayPinnedInOrder()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(columnCount: 6);
			for (int i = 0; i < 3; i++)
			{
				tableView.Columns[i].FrozenEdge = TableViewFrozenEdge.Leading;
			}

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ScrollBodyTo(tableView, ScrollAxis.Horizontal, c_scrollOffset);

		RunOnUIThread.Execute(() =>
		{
			var offset = GetBodyHorizontalOffset(tableView);
			Verify.IsGreaterThan(offset, 0.0, "Precondition: the body must actually have scrolled horizontally.");

			// All three pinned by the same offset is what keeps them side by side and non-
			// overlapping: a wrong per-column accumulation would show up as differing translations.
			for (int i = 0; i < 3; i++)
			{
				VerifyPinned(GetHeaderCell(tableView, i), offset, $"frozen header cell {i}");
			}

			VerifyNotPinned(GetHeaderCell(tableView, 3), "the first non-frozen header cell");

			// A frozen column being hidden must not break the pinning of its surviving siblings.
			//
			// Deliberately NOT asserted here: that the collapsed column contributes no width to
			// the pinned band. ComputeLeadingFrozenWidth does skip non-Visible columns, but the
			// header cell element itself keeps its width when a column is collapsed after the
			// headers are built - the same product bug that already fails
			// VerifyCollapsedColumnRemovesCellsFromLayout and
			// VerifyRestoringColumnVisibilityRestoresCells in category 3.5. Repeating the
			// assertion here would add a third failure signal for one bug without adding
			// information, and the band-width contribution cannot be observed separately while
			// the cell width is wrong. It belongs to 3.5 and is tracked there.
			tableView.Columns[1].Visibility = Visibility.Collapsed;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var offset = GetBodyHorizontalOffset(tableView);

			VerifyPinned(GetHeaderCell(tableView, 0), offset, "the first frozen header cell after collapsing a sibling");
			VerifyPinned(GetHeaderCell(tableView, 2), offset, "the third frozen header cell after collapsing a sibling");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies only the contiguous leading prefix freezes; a later Leading column scrolls normally.")]
	public async Task VerifyNonContiguousLeadingFrozenOnlyFreezesPrefix()
	{
		// Remark: the contiguity rule is stated twice in the implementation as deliberate, but
		// TableView.idl does not mention it at all - a spec gap, and a surprising one for an app
		// author who sets Leading on an arbitrary column and sees it ignored.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(columnCount: 5);
			tableView.Columns[0].FrozenEdge = TableViewFrozenEdge.Leading;
			tableView.Columns[1].FrozenEdge = TableViewFrozenEdge.None;
			tableView.Columns[2].FrozenEdge = TableViewFrozenEdge.Leading;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ScrollBodyTo(tableView, ScrollAxis.Horizontal, c_scrollOffset);

		RunOnUIThread.Execute(() =>
		{
			var offset = GetBodyHorizontalOffset(tableView);
			Verify.IsGreaterThan(offset, 0.0, "Precondition: the body must actually have scrolled horizontally.");

			VerifyPinned(GetHeaderCell(tableView, 0), offset, "the prefix frozen header cell");
			VerifyNotPinned(GetHeaderCell(tableView, 1), "the non-frozen header cell that ends the prefix");

			// The whole point: a stray Leading flag past the prefix must be ignored, or it would
			// pin a column over scrolled content and overlap cells.
			VerifyNotPinned(GetHeaderCell(tableView, 2),
				"the out-of-prefix Leading header cell, which must be treated as non-frozen");

			foreach (var row in GetRealizedRows(tableView))
			{
				VerifyPinned(GetRowCell(row, 0), offset, "the prefix frozen row cell");
				VerifyNotPinned(GetRowCell(row, 2), "the out-of-prefix Leading row cell");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies FrozenEdge.Trailing roundtrips but has no layout effect while it is reserved.")]
	public async Task VerifyTrailingFrozenEdgeIsReservedNoOp()
	{
		// Asserting the absence of behavior is the right test here precisely because the value is
		// public API today: if a reserved enum value quietly acquires behavior, that becomes a
		// compatibility burden the moment Trailing is actually implemented.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(columnCount: 5);
			tableView.Columns[4].FrozenEdge = TableViewFrozenEdge.Trailing;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await ScrollBodyTo(tableView, ScrollAxis.Horizontal, c_scrollOffset);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(TableViewFrozenEdge.Trailing, tableView.Columns[4].FrozenEdge,
				"The reserved value must roundtrip on the property.");

			Verify.IsGreaterThan(GetBodyHorizontalOffset(tableView), 0.0,
				"Precondition: the body must actually have scrolled horizontally.");

			VerifyNotPinned(GetHeaderCell(tableView, 4), "a Trailing header cell, which must behave exactly like None");

			foreach (var row in GetRealizedRows(tableView))
			{
				VerifyNotPinned(GetRowCell(row, 4), "a Trailing row cell, which must behave exactly like None");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies leading frozen columns mirror under RTL and pin to the right edge of the viewport.")]
	public async Task VerifyFrozenColumnsMirrorInRightToLeft()
	{
		// FrozenEdge.Leading names a logical edge, not a physical one, so under RightToLeft it must
		// mirror: the frozen column pins to the visual *right* edge, which is where the leading
		// edge of a mirrored layout is. Anything else makes FrozenEdge quietly LTR-only and leaves
		// RTL users with a column that scrolls away when it was declared frozen.
		//
		// Measuring this correctly requires care. XAML applies RTL as a single mirroring transform
		// at the element where FlowDirection *changes*, not per element: inside an RTL subtree all
		// children still arrange in ordinary left-to-right logical coordinates, and
		// TransformToVisual between two elements of the same FlowDirection returns those logical
		// coordinates unmirrored. So the table is hosted in an explicitly LeftToRight wrapper and
		// every position is measured against that wrapper, which puts the mirror boundary between
		// the measurement frame and the cells and makes "right edge" mean what it says.
		TableView tableView = null;
		FrameworkElement wrapper = null;
		double frozenRightBefore = 0.0;
		double scrolledRightBefore = 0.0;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateScrollableTable(columnCount: 5);
			tableView.FlowDirection = FlowDirection.RightToLeft;
			tableView.Columns[0].FrozenEdge = TableViewFrozenEdge.Leading;

			var host = new Grid { FlowDirection = FlowDirection.LeftToRight };
			host.Children.Add(tableView);
			wrapper = host;

			LoadContent(host);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Without this the whole test could pass trivially on a table that never went RTL at
			// all, and a genuine mirroring failure would be indistinguishable from a setup bug.
			var cellsHost = GetCellsHost(GetRealizedRows(tableView).First());
			Verify.AreEqual(FlowDirection.RightToLeft, cellsHost.FlowDirection,
				"Precondition: FlowDirection must reach the cells panel, or nothing downstream can mirror.");

			frozenRightBefore = GetVisualRight(GetRowCell(GetRealizedRows(tableView).First(), 0), wrapper);
			scrolledRightBefore = GetVisualRight(GetRowCell(GetRealizedRows(tableView).First(), 1), wrapper);
			Log.Comment($"Before scrolling: frozen cell right edge at {frozenRightBefore}, neighbour right edge at {scrolledRightBefore}.");

			// The first column must be arranged at the visual right under RTL. If this fails the
			// subtree never mirrored at all and every assertion below would be describing an LTR
			// layout wearing an RTL flag.
			Verify.IsGreaterThan(frozenRightBefore, scrolledRightBefore,
				"Precondition: under RTL the first column must be arranged to the right of the second.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Horizontal, c_scrollOffset);

		RunOnUIThread.Execute(() =>
		{
			var offset = GetBodyHorizontalOffset(tableView);
			Log.Comment($"Scrolled to horizontal offset {offset}.");
			Verify.IsGreaterThan(offset, 0.0, "Precondition: the body must actually have scrolled horizontally.");

			var row = GetRealizedRows(tableView).First();
			var frozenRightAfter = GetVisualRight(GetRowCell(row, 0), wrapper);
			var scrolledRightAfter = GetVisualRight(GetRowCell(row, 1), wrapper);
			Log.Comment($"After scrolling: frozen cell right edge at {frozenRightAfter}, neighbour right edge at {scrolledRightAfter}.");

			// The neighbour moving is what makes the frozen cell's stillness meaningful.
			Verify.AreNotEqual(scrolledRightBefore, scrolledRightAfter,
				"Precondition: a non-frozen cell must move when the body scrolls.");

			VerifyNear(frozenRightBefore, frozenRightAfter, c_positionTolerance,
				"A leading frozen column must hold its viewport position under RTL exactly as it does under LTR");

			// And it must hold that position against the *right* edge: the leading edge of a
			// mirrored layout. A frozen column pinned to the visual left under RTL would be pinned
			// to the trailing edge, the opposite of what FrozenEdge.Leading asks for.
			var viewportRight = GetVisualRight(GetBodyScroller(tableView), wrapper, GetBodyViewportWidth(tableView));
			Log.Comment($"Viewport right edge at {viewportRight}.");

			VerifyNear(viewportRight, frozenRightAfter, c_positionTolerance,
				"Under RTL a leading frozen column must pin against the right edge of the viewport");

			// The header is pinned by a separate call site against the same offset, and the stated
			// reason they share it is so the two stay aligned. Under RTL that alignment matters
			// just as much, so it is asserted rather than assumed.
			var headerRight = GetVisualRight(GetHeaderCell(tableView, 0), wrapper);
			VerifyNear(frozenRightAfter, headerRight, c_positionTolerance,
				"The frozen header cell must stay aligned with its frozen body cells under RTL");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the header grid line, sort indicator and resize gripper share the trailing (visual left) edge under RTL.")]
	public async Task VerifyHeaderTrailingAdornmentsShareEdgeInRightToLeft()
	{
		// Header cells are already mirrored by the framework, so a hand-swapped alignment would
		// put adornments on the wrong edge. Measure in an explicit LeftToRight wrapper.
		TableView tableView = null;
		FrameworkElement wrapper = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = TableViewSortingTestHelpers.CreateSortTable();
			tableView.CanUserResizeColumns = true;
			tableView.GridLinesVisibility = TableViewGridLinesVisibility.All;
			tableView.FlowDirection = FlowDirection.RightToLeft;

			var host = new Grid { FlowDirection = FlowDirection.LeftToRight };
			host.Children.Add(tableView);
			wrapper = host;

			LoadContent(host);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var headerCell = GetHeaderCell(tableView, 0);
			Verify.AreEqual(FlowDirection.RightToLeft, headerCell.FlowDirection,
				"Precondition: FlowDirection must reach the header cell.");
			Verify.IsGreaterThan(
				GetVisualRight(headerCell, wrapper),
				GetVisualRight(GetHeaderCell(tableView, 1), wrapper),
				"Precondition: under RTL the first header must be arranged to the right of the second.");

			var cellLeft = GetVisualLeft(headerCell, wrapper);
			var cellRight = GetVisualRight(headerCell, wrapper);
			Log.Comment($"Header cell spans {cellLeft}..{cellRight}.");

			var gridLine = headerCell.FindVisualChildByName("PART_GridLine") as FrameworkElement;
			Verify.IsNotNull(gridLine, "The header cell must contain its vertical grid line.");
			Verify.AreEqual(Visibility.Visible, gridLine.Visibility,
				"Precondition: a non-terminal header grid line must be visible with GridLinesVisibility.All.");
			var gridLineLeft = GetVisualLeft(gridLine, wrapper);

			var gripper = RequireGripper(tableView, 0);
			var gripperLeft = GetVisualLeft(gripper, wrapper);

			var indicator = TableViewSortingTestHelpers.FindSortIndicator(tableView, 0);
			Verify.IsNotNull(indicator, "A sorted, sortable column must build a sort indicator.");
			var indicatorLeft = GetVisualLeft(indicator, wrapper);
			var indicatorRight = GetVisualRight(indicator, wrapper);
			Log.Comment($"Grid line left {gridLineLeft}, gripper left {gripperLeft}, indicator {indicatorLeft}..{indicatorRight}.");

			VerifyNear(cellLeft, gridLineLeft, c_positionTolerance,
				"Under RTL the header grid line must sit on the cell's trailing (visual left) edge");
			VerifyNear(cellLeft, gripperLeft, c_positionTolerance,
				"Under RTL the resize gripper must sit on the cell's trailing (visual left) edge");
			Verify.IsLessThan(indicatorLeft - cellLeft, cellRight - indicatorRight,
				"Under RTL the sort indicator must sit toward the cell's trailing (visual left) edge, with the grid line and gripper.");
		});
	}
}

internal static class TableViewSizingTestHelpers
{
	// TableViewColumn.ActualWidth's declared default (TableView.idl:145), which is also the
	// provisional value an unresolved Auto or Star column reports.
	internal const double c_provisionalWidth = 120.0;

	// Far enough into the scroll range to be unambiguous, and small enough to stay well short of
	// the maximum so a clamped ChangeView cannot silently make the assertions vacuous.
	internal const double c_scrollOffset = 200.0;

	internal const string VeryLongText = "A considerably longer piece of cell text than the others";

	// Far enough down a 120-row source that it cannot be realized while the first rows are in view.
	internal const int c_wideRowIndex = 60;

	// Positions are compared after two independent arrange passes; the same rounding applies.
	internal const double c_positionTolerance = 2.0;

	internal static bool IsItemRealized(TableView tableView, object item)
		=> GetRealizedRows(tableView).Any(row => ReferenceEquals(row.DataContext, item));

	// The visual right edge of an element in an ancestor's coordinate space. Both horizontal
	// corners are transformed and the larger taken, because an RTL subtree maps an element's local
	// left edge to its visual right - taking local x + width would silently report the wrong edge.
	internal static double GetVisualRight(FrameworkElement element, FrameworkElement ancestor, double width = double.NaN)
	{
		var extent = double.IsNaN(width) ? element.ActualWidth : width;
		var transform = element.TransformToVisual(ancestor);

		return Math.Max(
			transform.TransformPoint(new Point(0.0, 0.0)).X,
			transform.TransformPoint(new Point(extent, 0.0)).X);
	}

	internal static double GetVisualLeft(FrameworkElement element, FrameworkElement ancestor)
	{
		var transform = element.TransformToVisual(ancestor);

		return Math.Min(
			transform.TransformPoint(new Point(0.0, 0.0)).X,
			transform.TransformPoint(new Point(element.ActualWidth, 0.0)).X);
	}

	// Scrolls far enough to realize the row at the given index, derived from a measured row height
	// rather than a hard-coded one so it does not depend on density or theme metrics.
	// TODO Uno: async because IdleSynchronizer.Wait maps to await TestServices.WindowHelper.WaitForIdle().
	internal static async Task ScrollBodyToItem(TableView tableView, int index)
	{
		double target = 0.0;

		RunOnUIThread.Execute(() =>
		{
			var row = GetRealizedRows(tableView).First();
			Verify.IsGreaterThan(row.ActualHeight, 0.0, "Precondition: a realized row must have a measured height.");

			var scroller = GetBodyScroller(tableView);

			// Aim to land the target row near the middle of the viewport, so the test does not
			// depend on whether the row at an exact offset counts as realized.
			target = Math.Max(0.0, (index * row.ActualHeight) - (scroller.ViewportHeight / 2.0));

			Verify.IsGreaterThan(scroller.ScrollableHeight, target,
				"Precondition: the source must be long enough to scroll the target row into view.");
		});

		await ScrollBodyTo(tableView, ScrollAxis.Vertical, target);
	}

	internal static UIElement StackTables(params TableView[] tables)
	{
		var panel = new StackPanel();
		foreach (var table in tables)
		{
			panel.Children.Add(table);
		}
		return panel;
	}

	// One Auto column bound to Name, over items whose Name values are supplied by the caller, so
	// a test can control exactly how wide the widest realized cell is.
	internal static TableView CreateAutoColumnTable(string header, string[] names)
	{
		var tableView = CreateTableViewShell(
			names.Select(name => new Person { Name = name, Role = "Role" }).ToList(), 600, 260);

		tableView.Columns.Add(MakeTextColumn(header, "Name", GridLength.Auto));

		return tableView;
	}

	internal static TableView CreateResizableTable(params (string Header, GridLength Width)[] columns)
	{
		var tableView = CreateTableViewWithColumns(width: 600, columns: columns);
		tableView.CanUserResizeColumns = true;
		return tableView;
	}

	// A table whose columns are collectively far wider than its viewport, so the body scroller has
	// a real horizontal scroll range to drive.
	internal static TableView CreateScrollableTable(int columnCount)
	{
		var columns = Enumerable
			.Range(0, columnCount)
			.Select(i => ($"Column {i}", new GridLength(160.0, GridUnitType.Pixel)))
			.ToArray();

		var tableView = CreateTableViewWithColumns(width: 360, columns: columns);
		tableView.Height = 260;
		return tableView;
	}

	internal static DataTemplate CreateFixedWidthTemplate(double width) => (DataTemplate)XamlReader.Load(
		$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
			   <Border Width=""{width.ToString(CultureInfo.InvariantCulture)}"" Height=""20"" />
		   </DataTemplate>");

	internal static double GetBodyViewportWidth(TableView tableView) => GetBodyScroller(tableView).ViewportWidth;

	internal static double GetBodyHorizontalOffset(TableView tableView) => GetBodyScroller(tableView).HorizontalOffset;

	// A pinned cell counter-translates by the scroll offset so it holds its viewport position, and
	// paints above the scrolled cells.
	internal static void VerifyPinned(FrameworkElement element, double horizontalOffset, string what)
	{
		Verify.AreEqual((float)horizontalOffset, element.Translation.X,
			$"{what} should counter-translate by the scroll offset to stay pinned.");
		Verify.AreEqual(1, Canvas.GetZIndex(element),
			$"{what} should paint above the scrolled cells.");
	}

	internal static void VerifyNotPinned(FrameworkElement element, string what)
	{
		Verify.AreEqual(0.0f, element.Translation.X, $"{what} should not be translated.");
		Verify.AreEqual(0, Canvas.GetZIndex(element), $"{what} should not be raised above the scrolled cells.");
	}

	internal static void VerifyNear(double expected, double actual, double tolerance, string message)
	{
		Verify.IsTrue(Math.Abs(expected - actual) <= tolerance,
			$"{message} (expected about {expected}, got {actual}, tolerance {tolerance}).");
	}
}
