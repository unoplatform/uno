// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Layout.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
// Only realized rows contribute, so Auto sizes to the widest cell realized so far. Auto is grow-only
// within a data set, so a column never narrows as rows scroll out (ResetColumnDesiredWidths).
//
// Resolved widths (Pixel / Auto / Star) are snapped to device pixels when UseLayoutRounding is set,
// using the XamlRoot rasterization scale -- again mirroring CGrid.
// -----------------------------------------------------------------------------

partial class TableView
{
	private static bool IsFixedWidthColumn(TableViewColumn column)
	{
		var lo = TableViewColumn.NonNegativeFiniteOrZero(column.MinWidth);
		return TableViewColumn.MaxWidthForColumn(column, lo) <= lo;
	}

	private static double MinWidthForStarFactor(TableViewColumn column, double factor)
	{
		if (factor > 0.0)
		{
			return TableViewColumn.NonNegativeFiniteOrZero(column.MinWidth);
		}

		// WPF gives 0* a zero share. Preserve that for the default MinWidth, but still honor an
		// explicitly-set MinWidth on a 0* column.
		var localMinWidth = column.ReadLocalValue(TableViewColumn.MinWidthProperty);
		return localMinWidth == DependencyProperty.UnsetValue
			? 0.0
			: TableViewColumn.NonNegativeFiniteOrZero(column.MinWidth);
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
				total += TableViewColumn.NonNegativeFiniteOrZero(factorOf(c));
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
				var factor = TableViewColumn.NonNegativeFiniteOrZero(factorOf(c));
				var desired = unit * factor;
				var lo = MinWidthForStarFactor(c, factor);
				var hi = TableViewColumn.MaxWidthForColumn(c, lo);
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
				var factor = TableViewColumn.NonNegativeFiniteOrZero(factorOf(c));
				var lo = MinWidthForStarFactor(c, factor);
				var hi = TableViewColumn.MaxWidthForColumn(c, lo);
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

	internal bool HasVisibleAutoColumn()
	{
		if (Columns is { } columns)
		{
			foreach (var column in columns)
			{
				if (column is not null &&
					column.Visibility == Visibility.Visible &&
					column.Width.GridUnitType == GridUnitType.Auto &&
					column.GetOwningTableView() == this)
				{
					return true;
				}
			}
		}
		return false;
	}

	// A newly prepared row's first measure raises no width-change signal, so Auto columns need this
	// table pass to pick its cells up.
	internal void InvalidateMeasureForRowRealizationChange()
	{
		if (HasVisibleAutoColumn())
		{
			InvalidateMeasure();
		}
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
				// them while Density / Min / Max / HeadersVisibility changes (which do not call this)
				// preserve the max within a data set.
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
		// Before any early return: a change this pass doesn't size (e.g. a held column collapsing) must still end the holds.
		if (m_lockedColumnHolds.Count != 0 && LockedColumnHoldsSignature() != m_lockedColumnHoldsSignature)
		{
			m_lockedColumnHolds.Clear();
		}

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
		var autoTotal = 0.0;
		var changed = false;
		List<TableViewColumn> starColumns = new();

		// Collect the currently realized, BOUND rows ONCE (a single repeater walk) and reuse the list for
		// every Auto column below. Lazy, so tables without an Auto column skip the walk entirely.
		List<TableViewRow> realizedRows = new();
		var realizedRowsCollected = false;
		void ensureRealizedRows()
		{
			if (realizedRowsCollected)
			{
				return;
			}
			realizedRowsCollected = true;
			if (m_rowsRepeater is { } repeater)
			{
				ForEachRealizedRow(row =>
				{
					// Skip pooled rows (GetElementIndex < 0): their stale widths would re-pin Auto
					// columns after ItemsSource shrinks.
					if (repeater.GetElementIndex(row) >= 0)
					{
						realizedRows.Add(row);
					}
				});
			}
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
			var lo = TableViewColumn.NonNegativeFiniteOrZero(column.MinWidth);
			var hi = TableViewColumn.MaxWidthForColumn(column, lo);

			switch (width.GridUnitType)
			{
				case GridUnitType.Star:
					// Sized below once the fixed total is known.
					starColumns.Add(column);
					break;

				case GridUnitType.Auto:
					{
						// Pull measured widths from the header and currently realized row panels. Rows and header
						// are tracked separately, so toggling the headers never drops the rows' width.
						var headerMeasured = 0.0;
						// Only consume the header host's cache when headers are actually shown: a hidden header is
						// not measured this pass, so its per-pass cache is stale (and, with the raw-identity key,
						// a freed column's address could be reused by a new column and false-match a stale entry).
						// A hidden header must not drive column width regardless.
						if (ShouldShowColumnHeaders())
						{
							headerMeasured = TableViewColumn.NonNegativeFiniteOrZero(GetHeaderMeasuredWidthForColumn(column));
						}

						var rowsMeasured = 0.0;
						ensureRealizedRows();
						foreach (var row in realizedRows)
						{
							// Ask the row for its cell's measured width instead of reaching into its panel.
							rowsMeasured = StdMath.Max(rowsMeasured, row.MeasuredWidthForColumn(column));
						}

						var columnImpl = column;
						// Rows are grow-only within a data set (WPF / WCT DataGrid parity), so scrolling never
						// narrows the column. Headers are never virtualized, so the current measurement is used.
						columnImpl.SetDesiredWidthInternal(
							StdMath.Max(columnImpl.RowsDesiredWidthInternal(), TableViewColumn.NonNegativeFiniteOrZero(rowsMeasured)),
							headerMeasured);
						var desired = columnImpl.DesiredWidthInternal();

						var resolved = layoutRound(
							StdMath.Clamp(desired > 0.0 ? desired : TableViewColumn.c_widthDefault.Value, lo, hi));
						changed |= setResolvedActualWidth(column, resolved);
						fixedTotal += resolved;
						autoTotal += resolved;
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

						var desired = TableViewColumn.NonNegativeFiniteOrDefault(width.Value);
						var resolved = layoutRound(
							StdMath.Clamp(desired, lo, hi));
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
			// A locked Star column keeps its authored share while that fits beside the other Star columns'
			// MinWidth, else shares by factor; a hold from FreezeColumnsBeforeResize overrides both.
			// A copy: resolving a width runs app callbacks that can end the holds partway through this pass.
			List<LockedColumnHold> holds = new(m_lockedColumnHolds);
			double? heldWidth(TableViewColumn c)
			{
				foreach (var hold in holds)
				{
					if (hold.column is { } weakColumn && weakColumn.TryGetTarget(out var heldColumn) && heldColumn == c)
					{
						return hold.width;
					}
				}
				return null;
			}
			bool isHeld(TableViewColumn c) => isLocked(c) && heldWidth(c).HasValue;
			double starFactorOf(TableViewColumn c)
			{
				return StdMath.Max(0.0, isLocked(c) ? authoredOf(c).Value : c.Width.Value);
			}

			// A device pixel of slack: rounding can leave the room just short of a clamped drag's reservation.
			var tolerance = 1.0 / StdMath.Max(layoutRoundFactor, 1.0);

			// An Auto column can grow (scrolling, a header or template change) without changing the hold
			// signature, so a hold that no longer fits once the Auto columns outgrow what they took when
			// it was latched is stale. Only growth counts: a resize may start from an overflowing table.
			if (holds.Count != 0 &&
				holds.Exists(hold => autoTotal > hold.autoTotal + tolerance))
			{
				var needed = fixedTotal;
				foreach (var c in starColumns)
				{
					needed += isHeld(c) ? heldWidth(c)!.Value : MinWidthForStarFactor(c, starFactorOf(c));
				}
				if (needed > layoutRound(viewport) + tolerance)
				{
					holds.Clear();
					m_lockedColumnHolds.Clear();
					if (m_activeColumnResizeDrag is not null)
					{
						m_activeColumnResizeDrag.lockedHoldsBefore = null;
					}
				}
			}

			foreach (var c in starColumns)
			{
				if (isHeld(c))
				{
					changed |= setResolvedActualWidth(c, heldWidth(c)!.Value);
					fixedTotal += c.ActualWidth;
				}
			}
			starColumns.RemoveAll(isHeld);

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
				var lo = TableViewColumn.NonNegativeFiniteOrZero(c.MinWidth);
				var hi = TableViewColumn.MaxWidthForColumn(c, lo);
				if (authored.GridUnitType == GridUnitType.Auto)
				{
					// The content width, not the width a resize gave it: a dragged Auto column must not
					// change what the authored layout leaves for the Star columns.
					var desired = c.DesiredWidthInternal();
					authoredFixedTotal += layoutRound(StdMath.Clamp(desired > 0.0 ? desired : c.ActualWidth, lo, hi));
				}
				else
				{
					var desired = TableViewColumn.NonNegativeFiniteOrDefault(authored.Value);
					authoredFixedTotal += layoutRound(
						StdMath.Clamp(desired, lo, hi));
				}
			}

			List<(TableViewColumn column, double width)> lockedShares = new();
			var lockedTotal = 0.0;
			DistributeStarWidths(
				authoredPool,
				StdMath.Max(0.0, layoutRound(viewport) - authoredFixedTotal),
				c => StdMath.Max(0.0, authoredOf(c).Value),
				layoutRound,
				(c, width) =>
				{
					if (isLocked(c) && !isHeld(c))
					{
						lockedShares.Add((c, width));
						lockedTotal += width;
					}
				});

			// The authored share ignores earlier resizes, so it can exceed the room left now.
			var room = layoutRound(viewport) - fixedTotal;
			foreach (var c in starColumns)
			{
				if (!isLocked(c))
				{
					room -= MinWidthForStarFactor(c, StdMath.Max(0.0, c.Width.Value));
				}
			}

			if (lockedTotal <= StdMath.Max(0.0, room) + tolerance)
			{
				var excess = StdMath.Max(0.0, lockedTotal - StdMath.Max(0.0, room));
				for (var i = lockedShares.Count - 1; i >= 0; --i)
				{
					var share = lockedShares[i];
					// Never below the minimum (rounded up, so the snap can't undercut it): when the
					// minimums don't fit, the table overflows instead.
					var minimum = MinWidthForStarFactor(share.column, starFactorOf(share.column));
					if (layoutRoundFactor > 0.0)
					{
						minimum = Math.Ceiling(minimum * layoutRoundFactor - 0.0001) / layoutRoundFactor;
					}
					minimum = StdMath.Min(share.width, minimum);
					var width = StdMath.Max(minimum, share.width - excess);
					excess -= share.width - width;
					changed |= setResolvedActualWidth(share.column, width);
					fixedTotal += share.column.ActualWidth;
				}
				starColumns.RemoveAll(isLocked);
			}
			else
			{
				// One pass with the other Star columns, so a Min/MaxWidth clamp hands its excess to them.
				DistributeStarWidths(
					starColumns,
					StdMath.Max(0.0, layoutRound(viewport) - fixedTotal),
					starFactorOf,
					layoutRound,
					(c, width) =>
					{
						changed |= setResolvedActualWidth(c, width);
					});
				starColumns.Clear();
			}
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
	// redistribution pass without changing what the user sees. A locked column can't take a pixel
	// Width (the authored pass would count it twice), so it gets a hold that layout honors instead.
	internal void FreezeColumnsBeforeResize(TableViewColumn column, ColumnResizeDragState state)
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
		List<LockedColumnHold> holds = new();
		for (var i = 0; i < index && i < columns.Count; ++i)
		{
			var other = columns[i];
			if (other is null ||
				other.GetOwningTableView() != this ||
				other.Visibility != Visibility.Visible)
			{
				continue;
			}
			if (CanUserResizeColumns && !other.CanResize &&
				other.AuthoredWidthInternal().GridUnitType == GridUnitType.Star)
			{
				holds.Add(new() { column = new(other), width = other.ActualWidth });
				continue;
			}
			if (other.Width.GridUnitType != GridUnitType.Star || !other.CanResize)
			{
				continue;
			}
			candidates.Add(other);
		}

		// The resized column turns Pixel on this write, so it leaves the Auto total the holds compare against.
		var resizingAuto = column.Width.GridUnitType == GridUnitType.Auto;
		if (holds.Count != 0 || (resizingAuto && m_lockedColumnHolds.Count != 0))
		{
			state.lockedHoldsBefore = (new List<LockedColumnHold>(m_lockedColumnHolds), m_lockedColumnHoldsSignature);
		}
		if (resizingAuto)
		{
			for (var i = 0; i < m_lockedColumnHolds.Count; ++i)
			{
				var hold = m_lockedColumnHolds[i];
				hold.autoTotal -= column.ActualWidth;
				m_lockedColumnHolds[i] = hold;
			}
		}

		if (holds.Count != 0)
		{
			var autoTotal = 0.0;
			foreach (var c in columns)
			{
				if (c is not null && c != column &&
					c.GetOwningTableView() == this &&
					c.Visibility == Visibility.Visible &&
					c.Width.GridUnitType == GridUnitType.Auto)
				{
					autoTotal += c.ActualWidth;
				}
			}
			for (var i = 0; i < holds.Count; ++i)
			{
				var hold = holds[i];
				hold.autoTotal = autoTotal;
				holds[i] = hold;
			}

			var signature = LockedColumnHoldsSignature();
			if (signature != m_lockedColumnHoldsSignature)
			{
				m_lockedColumnHolds.Clear();
			}
			foreach (var hold in holds)
			{
				TableViewColumn? heldColumn = null;
				hold.column?.TryGetTarget(out heldColumn);
				m_lockedColumnHolds.RemoveAll(existing =>
				{
					TableViewColumn? existingColumn = null;
					existing.column?.TryGetTarget(out existingColumn);
					return existingColumn == heldColumn;
				});
				m_lockedColumnHolds.Add(hold);
			}
			m_lockedColumnHoldsSignature = signature;
		}

		var frozen = state.frozen;
		foreach (var other in candidates)
		{
			frozen.Add(new() { column = new(other), width = other.ReadLocalValue(TableViewColumn.WidthProperty) });

			var columnImpl = other;
			using var resizeScope = columnImpl.BeginUserResizeScope();
			other.Width = GridLengthHelper.FromPixels(other.ActualWidth);
		}
	}

	private ulong LockedColumnHoldsSignature()
	{
		ulong seed = 0;
		void mix(ulong value) => seed ^= unchecked(value + 0x9e3779b9UL + (seed << 6) + (seed >> 2));
		static ulong hashDouble(double value) => unchecked((ulong)value.GetHashCode());
		static ulong hashBool(bool value) => value ? 1UL : 0UL;
		var bodyScroller = m_bodyScroller;
		mix(hashDouble(bodyScroller is not null ? bodyScroller.ViewportWidth : 0.0));
		mix(hashBool(CanUserResizeColumns));
		if (Columns is { } columns)
		{
			foreach (var c in columns)
			{
				mix(unchecked((ulong)RuntimeHelpers.GetHashCode(c)));
				if (c is not null)
				{
					// The authored Width, not Width: a user resize writes Width and must not drop the holds.
					var authored = c.AuthoredWidthInternal();
					mix(hashDouble(authored.Value));
					mix((ulong)authored.GridUnitType);
					mix(hashDouble(c.MinWidth));
					// A local MinWidth equal to the default still changes a 0* column's minimum.
					mix(hashBool(c.ReadLocalValue(TableViewColumn.MinWidthProperty) != DependencyProperty.UnsetValue));
					mix(hashDouble(c.MaxWidth));
					mix((ulong)c.Visibility);
					mix(hashBool(c.CanResize));
				}
			}
		}
		return seed;
	}

	// Layout alone can miss an input that changes and changes back before it runs (the signature then
	// matches again), so the inputs end the holds as they change.
	internal void ExpireLockedColumnHolds()
	{
		if (m_lockedColumnHolds.Count != 0)
		{
			m_lockedColumnHolds.Clear();
			InvalidateMeasure();
		}
		if (m_activeColumnResizeDrag is not null)
		{
			m_activeColumnResizeDrag.lockedHoldsBefore = null;
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
				resizeEnabled && other.CanResize &&
				!IsFixedWidthColumn(other);

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
		if (IsColumnResizePinned(column))
		{
			bounds.Min = column.ActualWidth;
			bounds.Max = bounds.Min;
			return bounds;
		}

		bounds.Max = StdMath.Max(0.0, viewport - reservedForOthers);
		return bounds;
	}

	// MinWidth meets MaxWidth, or a Star column with no later resizable Star column to trade with.
	// Shared by the drag bounds and gripper visibility so the two cannot disagree.
	internal bool IsColumnResizePinned(TableViewColumn? column)
	{
		if (column is null)
		{
			return false;
		}

		if (IsFixedWidthColumn(column))
		{
			return true;
		}

		if (column.Width.GridUnitType != GridUnitType.Star)
		{
			return false;
		}

		var columns = Columns;
		var columnIndex = columns?.IndexOf(column) ?? -1;
		if (columns is null || columnIndex < 0)
		{
			return false;
		}

		if (!CanUserResizeColumns)
		{
			return true;
		}

		for (var i = columnIndex + 1; i < columns.Count; ++i)
		{
			var other = columns[i];
			if (other is not null &&
				other.GetOwningTableView() == this &&
				other.Visibility == Visibility.Visible &&
				other.Width.GridUnitType == GridUnitType.Star &&
				other.CanResize &&
				!IsFixedWidthColumn(other))
			{
				return false;
			}
		}

		return true;
	}

	private void RefreshResizeGripperAvailability()
	{
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		// Snapshot: restoring a header tooltip runs app code that can rebuild the headers.
		List<FrameworkElement> headerCells = new();
		{
			var children = host.Children;
			var count = children.Count;
			headerCells.EnsureCapacity(count);
			for (var i = 0; i < count; ++i)
			{
				if (children[i] is FrameworkElement headerCell)
				{
					headerCells.Add(headerCell);
				}
			}
		}

		bool isHosted(FrameworkElement headerCell)
		{
			return m_headerHost == host && VisualTreeHelper.GetParent(headerCell) == host;
		}

		var flipped = false;
		foreach (var headerCell in headerCells)
		{
			if (!isHosted(headerCell))
			{
				continue;
			}

			var gripper = FindResizeGripperInCell(headerCell);
			var column = headerCell.Tag as TableViewColumn;
			if (gripper is null || column is null)
			{
				continue;
			}

			var visibility = IsColumnResizePinned(column) ? Visibility.Collapsed : Visibility.Visible;
			if (gripper.Visibility == visibility)
			{
				continue;
			}

			if (visibility == Visibility.Collapsed)
			{
				// A collapsed gripper under the pointer raises no PointerExited.
				TableViewDetails.RestoreOwnedToolTip(headerCell);
				if (!isHosted(headerCell) ||
					headerCell.Tag as TableViewColumn != column ||
					!IsColumnResizePinned(column) ||
					gripper.Visibility == visibility)
				{
					continue;
				}
			}

			// Only a peer a client already holds is notified; FromElement would create one.
			var header = headerCell as TableViewColumnHeader;
			var headerPeer = header is not null && AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged)
				? header.GetExistingAutomationPeerInternal() : null;
			// The pin state is already updated, so the old value comes from the visibility being replaced.
			var couldResize = gripper.Visibility == Visibility.Visible && IsColumnResizeAllowed(column);
			gripper.Visibility = visibility;

			if (headerPeer is not null)
			{
				var canResize = CanResizeColumnFromAutomation(column);
				if (canResize != couldResize)
				{
					headerPeer.RaisePropertyChangedEvent(
						TransformPatternIdentifiers.CanResizeProperty,
						couldResize,
						canResize);
				}
			}
			flipped = true;
		}

		// Resizing can be a header's only action.
		if (flipped)
		{
			RefreshHeaderTabStopsInternal();
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
