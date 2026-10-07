// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Layout.cpp, tag winui3/main, commit dc28206ea35

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

	// Divides `available` in proportion to each column's Star factor. A column that would clamp at
	// its Min/MaxWidth is fixed there and dropped, then the rest re-divide what is left (the WPF
	// ComputeStarColumnWidths shape).
	private static void DistributeStarWidths(
		List<TableViewColumn> pool,
		double available,
		Func<TableViewColumn, double> factorOf,
		Func<double, double> layoutRound,
		Action<TableViewColumn, double> resolve)
	{
		double totalFactor()
		{
			var total = 0.0;
			foreach (var c in pool)
			{
				total += factorOf(c);
			}
			return total;
		}

		for (var adjusted = true; adjusted && pool.Count != 0;)
		{
			adjusted = false;

			var factorSum = totalFactor();
			var unit = factorSum > 0.0 ? available / factorSum : 0.0;

			for (var i = 0; i < pool.Count; ++i)
			{
				var c = pool[i];
				var factor = factorOf(c);
				var desired = unit * factor;
				var lo = MinWidthForStarFactor(c, factor);
				var hi = StdMath.Max(lo, c.MaxWidth);
				var clamped = StdMath.Clamp(desired, lo, hi);
				// std::clamp returns desired exactly when it is already in [lo, hi], so any
				// inequality is a real Min/MaxWidth clamp.
				if (clamped != desired)
				{
					resolve(c, clamped);
					available -= clamped;
					pool.RemoveAt(i);
					adjusted = true;
					break;
				}
			}
		}

		{
			var factorSum = totalFactor();
			var unit = factorSum > 0.0 ? StdMath.Max(0.0, available) / factorSum : 0.0;
			// Rounded on the running total rather than per column, so the widths still sum to
			// `available` once snapped; rounding each independently can overshoot the viewport and
			// leave a permanent one-pixel scrollbar.
			var exactConsumed = 0.0;
			var roundedConsumed = 0.0;
			foreach (var c in pool)
			{
				var factor = factorOf(c);
				var lo = MinWidthForStarFactor(c, factor);
				var hi = StdMath.Max(lo, c.MaxWidth);
				exactConsumed += StdMath.Clamp(unit * factor, lo, hi);
				var edge = layoutRound(exactConsumed);
				resolve(c, edge - roundedConsumed);
				roundedConsumed = edge;
			}
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var desired = base.MeasureOverride(availableSize);
		ResolveColumnWidths();
		QueueTerminalGridLineRefresh();
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
						// A locked column the authored pass below sizes from its Star share must not also be
						// booked as fixed here, or it lands in fixedTotal twice and starves the real donors.
						if (CanUserResizeColumns && !column.CanResize &&
							column.AuthoredWidthInternal().GridUnitType == GridUnitType.Star)
						{
							starColumns.Add(column);
							break;
						}

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

		var resizeEnabled = CanUserResizeColumns;
		static GridLength authoredOf(TableViewColumn c) => c.AuthoredWidthInternal();
		// Only while a resize can actually happen; otherwise no column's width is at risk and the
		// ordinary pool already clamps correctly.
		bool isLocked(TableViewColumn c) => resizeEnabled && !c.CanResize;

		if (starColumns.Exists(isLocked))
		{
			// A locked Star column takes its share of the authored layout rather than of whatever a
			// neighbor's resize left behind, so a drag cannot change its width.
			var authoredFixedTotal = 0.0;
			List<TableViewColumn> authoredPool = new();
			foreach (var c in columns)
			{
				if (c is null ||
					c.GetOwningTableView() != this ||
					c.Visibility != Visibility.Visible)
				{
					continue;
				}

				var authored = authoredOf(c);
				if (authored.GridUnitType == GridUnitType.Star)
				{
					authoredPool.Add(c);
					continue;
				}

				// Clamped and rounded exactly as the resolved pass above does, so the authored basis
				// and the real layout agree on what the fixed columns take.
				var lo = c.MinWidth;
				var hi = StdMath.Max(lo, c.MaxWidth);
				if (authored.GridUnitType == GridUnitType.Auto)
				{
					// The content width, not the width a resize gave it: a dragged Auto column must not
					// change what the authored layout leaves for the Star columns.
					var desired = c.DesiredWidthInternal();
					authoredFixedTotal += layoutRound(StdMath.Clamp(desired > 0.0 ? desired : c.ActualWidth, lo, hi));
				}
				else
				{
					authoredFixedTotal += layoutRound(StdMath.Clamp(authored.Value, lo, hi));
				}
			}

			DistributeStarWidths(
				authoredPool,
				StdMath.Max(0.0, layoutRound(viewport) - authoredFixedTotal),
				c => StdMath.Max(0.0, authoredOf(c).Value),
				layoutRound,
				(c, width) =>
				{
					if (isLocked(c))
					{
						changed |= setResolvedActualWidth(c, width);
						fixedTotal += c.ActualWidth;
					}
				});

			starColumns.RemoveAll(isLocked);
		}

		// The viewport basis is layout-rounded so the divided space is snapped consistently with the
		// fixed columns (CGrid rounds availableSize before distribution); per-column Star widths are
		// then snapped in setResolvedActualWidth.
		DistributeStarWidths(
			starColumns,
			StdMath.Max(0.0, layoutRound(viewport) - fixedTotal),
			c => StdMath.Max(0.0, c.Width.Value),
			layoutRound,
			(c, width) =>
			{
				changed |= setResolvedActualWidth(c, width);
			});

		if (changed)
		{
			InvalidateCellPanels();
			RefreshFrozenColumns();
		}
	}

	// Puts Width back the way the app left it. Restoring the effective value would convert a binding
	// or an inherited default into a local value the app never set.
	internal static void RestoreColumnWidth(TableViewColumn column, object? localWidth)
	{
		var columnImpl = column;
		using var resizeScope = columnImpl.BeginUserResizeScope();
		if (localWidth is null || localWidth == DependencyProperty.UnsetValue)
		{
			column.ClearValue(TableViewColumn.WidthProperty);
		}
		else
		{
			column.SetValue(TableViewColumn.WidthProperty, localWidth);
		}
	}

	// A resize takes space only from the columns after the dragged one (the WPF DataGrid contract).
	// Holding the earlier Star columns at the width they already render keeps them out of the
	// redistribution pass without changing what the user sees. A locked column is skipped: the
	// authored pass below already pins it, and freezing it would double-count it there.
	internal void FreezeColumnsBeforeResize(TableViewColumn column, List<ColumnResizeFrozenColumn> frozen)
	{
		var columns = Columns;
		var index = columns?.IndexOf(column) ?? -1;
		if (columns is null || index < 0)
		{
			return;
		}

		// Collected before any write: writing Width runs app callbacks that may mutate Columns, and
		// indexing a live vector across that would throw out of the manipulation.
		List<TableViewColumn> candidates = new();
		for (var i = 0; i < index && i < columns.Count; ++i)
		{
			var other = columns[i];
			if (other is null ||
				other.GetOwningTableView() != this ||
				other.Visibility != Visibility.Visible ||
				other.Width.GridUnitType != GridUnitType.Star ||
				!other.CanResize)
			{
				continue;
			}
			candidates.Add(other);
		}

		foreach (var other in candidates)
		{
			frozen.Add(new() { column = new(other), width = other.ReadLocalValue(TableViewColumn.WidthProperty) });

			var columnImpl = other;
			using var resizeScope = columnImpl.BeginUserResizeScope();
			other.Width = GridLengthHelper.FromPixels(other.ActualWidth);
		}
	}

	// How far a drag may take this column. A table whose columns divide the viewport may not grow past
	// it, and a column the user may not resize neither gives width away nor takes any.
	internal ColumnResizeBounds ResizeBoundsForColumn(TableViewColumn column)
	{
		ColumnResizeBounds bounds = new();

		var columns = Columns;
		var bodyScroller = m_bodyScroller;
		var viewport = bodyScroller is not null ? bodyScroller.ViewportWidth : 0.0;
		if (columns is null || !(viewport > 0.0) || double.IsInfinity(viewport))
		{
			return bounds;
		}

		static GridUnitType authoredType(TableViewColumn c) => c.AuthoredWidthInternal().GridUnitType;

		var resizeEnabled = CanUserResizeColumns;
		var draggedIndex = columns.IndexOf(column);
		if (draggedIndex < 0)
		{
			return bounds;
		}

		var reservedForOthers = 0.0;
		var dividesViewport = authoredType(column) == GridUnitType.Star;
		var hasParticipant = false;

		for (var i = 0; i < columns.Count; ++i)
		{
			var other = columns[i];
			if (other is null ||
				i == draggedIndex ||
				other.GetOwningTableView() != this ||
				other.Visibility != Visibility.Visible)
			{
				continue;
			}

			dividesViewport |= authoredType(other) == GridUnitType.Star;

			// Only a column that is Star *now* and sits after the dragged one can yield space: the
			// layout pass re-divides by current Width, and a resize never takes from its left.
			var participates =
				i > draggedIndex &&
				other.Width.GridUnitType == GridUnitType.Star &&
				resizeEnabled && other.CanResize;
			hasParticipant |= participates;

			if (!participates)
			{
				reservedForOthers += other.ActualWidth;
			}
			else
			{
				reservedForOthers += MinWidthForStarFactor(other, StdMath.Max(0.0, other.Width.Value));
			}
		}

		// Without a Star column anywhere the extent is meant to grow and scroll.
		if (!dividesViewport)
		{
			return bounds;
		}

		// Pinned in both directions: bounding only growth would let the drag hand width to a column
		// that is then not allowed to give it back. A Pixel or Auto column owns its width outright, so
		// it may still shrink -- that only makes the table narrower and needs nothing from a neighbour.
		if (!hasParticipant && column.Width.GridUnitType == GridUnitType.Star)
		{
			bounds.Min = column.ActualWidth;
			bounds.Max = bounds.Min;
			return bounds;
		}

		bounds.Max = StdMath.Max(0.0, viewport - reservedForOthers);
		return bounds;
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
