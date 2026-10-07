// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellsPanel.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Shared cell layout. Used as PART_HeaderHost and each row's PART_CellsHost. It measures every cell
// unconstrained and caches each cell's measured width per column for the owning TableView to pull
// during its table-level column-width resolve. It then arranges each cell at the column's resolved
// ActualWidth, so the header band and all rows stay column-aligned by construction — no per-cell
// Width binding is needed. No dependency properties, so (like TableViewRow) it does not inherit the
// generated Properties class.
partial class TableViewCellsPanel
{
	// TableViewCellsPanel() = default;

	// IFrameworkElement layout overrides.
	// winrt::Size MeasureOverride(winrt::Size const& availableSize);
	// winrt::Size ArrangeOverride(winrt::Size const& finalSize);

	// Measured (unconstrained) width for this column from the last MeasureOverride pass (0 if absent).
	// double MeasuredWidthForColumn(winrt::TableViewColumn const& column) const;

	// Pins the contiguous leading-frozen prefix of a cell host (header or row) against the horizontal
	// scroll offset and clips the scrolled cells beneath it. Shared by the header host and every row's
	// cell host (both are TableViewCellsPanel).
	// static void ApplyFrozenColumnLayout(const winrt::Panel& host, double horizontalOffset, double leadingFrozenWidth);

	// Returns the first cell in host whose Tag is the given column (or null). Cells are Tagged with
	// their owning TableViewColumn; this is the inverse of ColumnForCell, shared by the header/row
	// column-visibility updates and the column-header automation peer.
	// static winrt::FrameworkElement CellForColumn(const winrt::Panel& host, winrt::TableViewColumn const& column);

	// Set by TableViewRow when it captures PART_CellsHost. Lets the Auto-width pass recognise the cell
	// that is currently being edited; null for the header host, which has no row.
	// void SetOwningRowInternal(winrt::TableViewRow const& row);

	// private:
	// Each cell is Tagged with its owning TableViewColumn; this reads it (or null).
	// static winrt::TableViewColumn ColumnForCell(winrt::UIElement const& child);
	// void CacheMeasuredWidthForColumn(winrt::TableViewColumn const& column, double measuredWidth);

	// Records this panel's freshly measured width for a column into newLastMeasured and returns true if
	// it differs (grow OR shrink) from the panel's previous-pass width for that column. Comparing a
	// cell to its OWN history (not the column width) is convergent: a stably-narrower cell reports the
	// same width each pass (no signal), while a cell whose content actually changed reports a delta and
	// asks the owner to re-resolve -- so live shrink (edit/collapse) narrows the column, not just grow.
	// bool RecordAndDetectMeasuredWidthChange(
	//     winrt::TableViewColumn const& column, double measuredWidth,
	//     std::unordered_map<void*, double>& newLastMeasured) const;

	// Per-pass measured widths keyed by non-owning column identity (the implementation pointer). The
	// cache is cleared at the top of every MeasureOverride and consumed within the same synchronous
	// table measure pass (TableView::MeasureOverride -> ResolveColumnWidths), during which Columns is
	// not mutated -- so a raw identity key is safe and avoids a weak_ref resolve (IWeakReference::
	// Resolve: interlocked AddRef + QueryInterface + Release) per lookup on the hot layout/scroll path.
	// An unordered_map keeps pull/cache O(1) (vs a linear scan) so ResolveColumnWidths' per-column
	// pull across all realized rows stays O(columns * rows), not O(columns^2 * rows).
	//
	// Invariant the void* key relies on: a column's width may only be *pulled* from this cache in a
	// pass in which that column's cells were actually *measured* (and thus keyed) this pass. A host
	// that is consumed but not measured -- e.g. a hidden header host, which layout skips -- must not
	// be read from here: its entries are stale, and with a raw pointer a freed column's reused address
	// could false-match a stale entry (a weak_ref key would have absorbed that). ResolveColumnWidths
	// guards the header-host pull with ShouldShowColumnHeaders() for exactly this reason.
	// TODO Uno: std::unordered_map<void*, double> keyed by the column's implementation pointer. C# keys
	// by the column reference under reference equality, which is the same identity; the key keeps the
	// column alive until the entry is dropped, so the freed-address reuse hazard above cannot occur.
	// RecordAndDetectMeasuredWidthChange takes the same Dictionary type for newLastMeasured.
	private Dictionary<TableViewColumn, double> m_measuredWidthsByColumn = new(ReferenceEqualityComparer.Instance);

	// This panel's measured width per column from the PREVIOUS pass (persisted across passes, unlike
	// m_measuredWidthsByColumn which is per-pass). Used to detect that a cell's own content changed
	// (grow or shrink) so the owning TableView re-resolves the column. Rebuilt each pass from only the
	// columns measured this pass, so entries for removed/collapsed columns are pruned automatically
	// (bounding growth and limiting the void*-reuse window the per-pass cache note describes).
	private Dictionary<TableViewColumn, double> m_lastMeasuredWidthsByColumn = new(ReferenceEqualityComparer.Instance);

	// The row that owns this panel, or null for the header host. Weak: the row owns the panel.
	private WeakReference<TableViewRow>? m_owningRow = null;
}
