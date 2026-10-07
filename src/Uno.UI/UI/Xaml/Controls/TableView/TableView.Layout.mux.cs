// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Layout.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// -----------------------------------------------------------------------------
// Column-width layout engine (all of it lives here).
//
// TableView owns the column-width policy; rows and headers are thin plumbing. Each cell host panel
// measures its Auto-column children unconstrained and caches those measured widths by column;
// Pixel/Star cells are measured once at their resolved width (no unconstrained probe), mirroring
// CGrid. After the template subtree measures, TableView::MeasureOverride pulls those cached values
// from the header host and realized rows once, then resolves Pixel / Auto / Star widths into
// TableViewColumn.ActualWidth.
//
// Only realized rows contribute, so Auto sizes to the widest *realized* cell. Auto is shrink-capable:
// each measure pass re-derives the width from the currently pulled measured max (header + realized
// rows), so a column narrows when its widest content shrinks (CGrid parity) rather than latching a
// grow-only maximum. Because only realized rows contribute under virtualization, the Auto width still
// reflects the widest realized cell and can change as rows scroll into / out of realization.
//
// Resolved widths (Pixel / Auto / Star) are snapped to device pixels when UseLayoutRounding is set,
// using the XamlRoot rasterization scale -- again mirroring CGrid.
// -----------------------------------------------------------------------------

partial class TableView
{
	private static double MinWidthForStarFactor(TableViewColumn column, double factor)
	{
		if (factor > 0.0)
		{
			return column.MinWidth;
		}

		// WPF gives 0* a zero share. Preserve that for the default MinWidth, but still honor an
		// explicitly-set MinWidth on a 0* column.
		var localMinWidth = column.ReadLocalValue(TableViewColumn.MinWidthProperty);
		return localMinWidth == DependencyProperty.UnsetValue ? 0.0 : column.MinWidth;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var desired = base.MeasureOverride(availableSize);
		ResolveColumnWidths();
		return desired;
	}

	// Requested from a cell panel's MeasureOverride when a realized cell's own measured width changed
	// (grow or shrink). The request comes from below the body ScrollViewer, which absorbs the child's
	// measure invalidation, so TableView::MeasureOverride (and ResolveColumnWidths) would not otherwise
	// re-run. Invalidate our measure SYNCHRONOUSLY (not via the DispatcherQueue): InvalidateMeasure only
	// marks us dirty, so the layout manager re-measures the TableView within the SAME layout tick -- the
	// column resizes in the same frame as the content change (no one-frame lag, so drags stay smooth).
	// It converges without a debounce: the delta detector records each cell's new width in the pass that
	// detects it, so the follow-up measure sees no delta and stops. Calling InvalidateMeasure during a
	// descendant's measure is safe -- it schedules, it does not re-enter layout.
	internal void RequestColumnWidthResolve()
	{
		InvalidateMeasure();
	}

	// Reset the monotonic Auto desired widths. The next table measure pass remeasures the template
	// subtree, pulls header + realized-row measured widths, and resolves once from those caches.
	private void ResetColumnDesiredWidths()
	{
		if (Columns is { } columns)
		{
			foreach (var column in columns)
			{
				// Reset EVERY column's grow-only accumulator regardless of its current Width mode. A
				// column that was Auto, temporarily switched to Pixel/Star, then switched back must not
				// retain a stale max from a previous data set. This runs only on real data-set boundaries
				// (ItemsSource / Columns replaced / CellTemplate / Header), so it correctly clears across
				// them while Density / Min / Max changes (which do not call this) preserve the max within a
				// data set.
				if (column is not null && column.GetOwningTableView() == this)
				{
					column.ResetDesiredWidthInternal();
				}
			}
		}

		InvalidateMeasure();
	}

	// Resolve every visible column into ActualWidth. Fixed columns (Pixel + measured Auto) are sized
	// first; Star columns then split the viewport width left over between them.
	private void ResolveColumnWidths()
	{
		var columns = Columns;
		if (columns is null)
		{
			return;
		}

		// Snap resolved column widths to device pixels when layout rounding is enabled, mirroring CGrid's
		// use of the rasterization scale. A zero factor leaves values unrounded.
		var layoutRoundFactor = 0.0;
		if (UseLayoutRounding)
		{
			if (XamlRoot is { } xamlRoot)
			{
				layoutRoundFactor = xamlRoot.RasterizationScale;
			}
		}
		double layoutRound(double value)
		{
			return layoutRoundFactor > 0.0 ? StdMath.Round(value * layoutRoundFactor) / layoutRoundFactor : value;
		}

		bool setResolvedActualWidth(TableViewColumn target, double resolved)
		{
			resolved = layoutRound(resolved);
			var changed = Math.Abs(resolved - target.ActualWidth) > 0.0001;
			target.SetResolvedActualWidthInternal(resolved);
			return changed;
		}

		var fixedTotal = 0.0;
		var changed = false;
		List<TableViewColumn> starColumns = new();

		// Collect the currently realized, BOUND rows ONCE (a single repeater walk) and reuse the list for
		// every Auto column below, instead of re-walking the visual tree (GetChild + try_as per row) once
		// per Auto column.
		List<TableViewRow> realizedRows = new();
		if (m_rowsRepeater is { } repeater)
		{
			ForEachRealizedRow(row =>
			{
				// Skip pooled/recycled rows: ItemsRepeater keeps them parented in the pool, but they carry
				// stale measured widths (GetElementIndex < 0). Consuming them would re-pin Auto columns at
				// the previous width after ItemsSource shrinks, making ResetColumnDesiredWidths a no-op
				// (grow-only keeps the max). Only genuinely bound rows contribute to Auto sizing.
				if (repeater.GetElementIndex(row) >= 0)
				{
					realizedRows.Add(row);
				}
			});
		}

		foreach (var column in columns)
		{
			// Rejected columns still appear in the app-owned vector; do not resolve widths for another
			// TableView's column.
			if (column is null ||
				column.GetOwningTableView() != this ||
				column.Visibility != Visibility.Visible)
			{
				continue;
			}

			var width = column.Width;
			var lo = column.MinWidth;
			var hi = StdMath.Max(lo, column.MaxWidth);

			switch (width.GridUnitType)
			{
				case GridUnitType.Star:
					// Sized below once the fixed total is known.
					starColumns.Add(column);
					break;

				case GridUnitType.Auto:
					{
						// Pull measured widths from the header and currently realized row panels. Preserve v1's
						// grow-only policy by storing max(previous desired, pulled measured) on the column.
						var pulledMeasuredMax = 0.0;
						// Only consume the header host's cache when headers are actually shown: a hidden header is
						// not measured this pass, so its per-pass cache is stale (and, with the raw-identity key,
						// a freed column's address could be reused by a new column and false-match a stale entry).
						// A hidden header must not drive column width regardless.
						if (ShouldShowColumnHeaders())
						{
							pulledMeasuredMax = StdMath.Max(pulledMeasuredMax, GetHeaderMeasuredWidthForColumn(column));
						}

						foreach (var row in realizedRows)
						{
							// Ask the row for its cell's measured width instead of reaching into its panel.
							pulledMeasuredMax = StdMath.Max(pulledMeasuredMax, row.MeasuredWidthForColumn(column));
						}

						var columnImpl = column;
						// Shrink-capable Auto: size to the CURRENT measured content max rather than a monotonic
						// grow-only max, so the column narrows when its widest content shrinks (CGrid parity).
						var desired = pulledMeasuredMax;
						columnImpl.SetDesiredWidthInternal(desired);

						var resolved = layoutRound(StdMath.Clamp(desired > 0.0 ? desired : TableViewColumn.c_widthDefault.Value, lo, hi));
						changed |= setResolvedActualWidth(column, resolved);
						fixedTotal += resolved;
						break;
					}

				case GridUnitType.Pixel:
				default:
					{
						var resolved = layoutRound(StdMath.Clamp(width.Value, lo, hi));
						changed |= setResolvedActualWidth(column, resolved);
						fixedTotal += resolved;
						break;
					}
			}
		}

		if (starColumns.Count == 0)
		{
			if (changed)
			{
				InvalidateCellPanels();
				RefreshFrozenColumns();
			}
			return;
		}

		// Star needs a finite viewport to divide. The horizontally-scrolling body panel is measured at
		// infinite width, so we pull the ScrollViewer viewport explicitly. Before it is known, leave the
		// Star columns at their provisional width; the body-scroller SizeChanged re-resolves later.
		var bodyScroller = m_bodyScroller;
		var viewport = bodyScroller is not null ? bodyScroller.ViewportWidth : 0.0;
		// A non-finite or not-yet-known viewport has no finite space to divide (e.g. the table hosted
		// in a width-to-content parent); leave Star columns at their provisional width rather than
		// arranging an infinite cell. The body scroller's SizeChanged re-resolves once a real width lands.
		if (!(viewport > 0.0) || double.IsInfinity(viewport))
		{
			if (changed)
			{
				InvalidateCellPanels();
				RefreshFrozenColumns();
			}
			return;
		}

		// Distribute the remaining width proportional to each Star factor. A column that would clamp to
		// its Min/MaxWidth is fixed at the clamp and removed from the pool, then the rest re-divide the
		// space that is left (the WPF ComputeStarColumnWidths shape). The viewport basis is layout-rounded
		// so the divided space is snapped consistently with the fixed columns (CGrid rounds availableSize
		// before distribution); per-column Star widths are then snapped in setResolvedActualWidth.
		var available = StdMath.Max(0.0, layoutRound(viewport) - fixedTotal);
		List<TableViewColumn> pool = new(starColumns);
		var adjusted = true;

		while (adjusted && pool.Count != 0)
		{
			adjusted = false;

			var totalFactor = 0.0;
			foreach (var c in pool)
			{
				totalFactor += StdMath.Max(0.0, c.Width.Value);
			}
			var unit = totalFactor > 0.0 ? available / totalFactor : 0.0;

			for (var i = 0; i < pool.Count; ++i)
			{
				var c = pool[i];
				var factor = StdMath.Max(0.0, c.Width.Value);
				var desired = unit * factor;
				var lo = MinWidthForStarFactor(c, factor);
				var hi = StdMath.Max(lo, c.MaxWidth);
				var clamped = StdMath.Clamp(desired, lo, hi);
				// std::clamp returns desired exactly when it is already in [lo, hi], so any inequality is a
				// real Min/MaxWidth clamp: fix this column at its bound, drop it, and re-divide the rest.
				if (clamped != desired)
				{
					changed |= setResolvedActualWidth(c, clamped);
					available -= clamped;
					pool.RemoveAt(i);
					adjusted = true;
					break;
				}
			}
		}

		// Whatever survived without clamping splits the remaining space at the final proportional rate.
		if (pool.Count != 0)
		{
			var totalFactor = 0.0;
			foreach (var c in pool)
			{
				totalFactor += StdMath.Max(0.0, c.Width.Value);
			}
			var unit = totalFactor > 0.0 ? StdMath.Max(0.0, available) / totalFactor : 0.0;
			foreach (var c in pool)
			{
				var factor = StdMath.Max(0.0, c.Width.Value);
				var lo = MinWidthForStarFactor(c, factor);
				var hi = StdMath.Max(lo, c.MaxWidth);
				changed |= setResolvedActualWidth(
					c,
					StdMath.Clamp(unit * factor, lo, hi));
			}
		}

		if (changed)
		{
			InvalidateCellPanels();
			RefreshFrozenColumns();
		}
	}

	// Re-run the cell panels' measure/arrange so the header band and all rows reflect the newly resolved
	// column widths (Auto growth in one row must widen the header and every other row).
	private void InvalidateCellPanels()
	{
		if (m_headerHost is { } headerHost)
		{
			headerHost.InvalidateMeasure();
		}

		if (m_rowsRepeater is { } repeater)
		{
			var childCount = VisualTreeHelper.GetChildrenCount(repeater);
			for (var i = 0; i < childCount; ++i)
			{
				if (VisualTreeHelper.GetChild(repeater, i) is TableViewRow row)
				{
					// Ask the row to invalidate its own cell panel instead of reaching into it.
					row.InvalidateCells();
				}
				else if (VisualTreeHelper.GetChild(repeater, i) is TableViewGroupHeader header)
				{
					// The group-header band spans the same columns and sizes itself during measure, but
					// it is a direct repeater child rather than a cells panel, so the row walk above
					// never reaches it. Handled in this one walk so the two cannot drift apart.
					UpdateGroupHeaderWidth(header);
				}
			}
		}
	}
}
