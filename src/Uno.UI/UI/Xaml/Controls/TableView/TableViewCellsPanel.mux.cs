// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewCellsPanel.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewCellsPanel
{
	// A change of more than this (device-independent px) in a cell's own measured width between passes
	// triggers a deferred column-width re-resolve, so an Auto column re-sizes (grows OR shrinks) when a
	// *realized* cell's content changes during interaction (slider/expander/edit). The threshold
	// absorbs sub-pixel / layout-rounding noise so steady state doesn't churn.
	private const double c_columnMeasureChangeThreshold = 0.5;

	private static TableViewColumn? ColumnForCell(UIElement child)
	{
		if (child is FrameworkElement fe)
		{
			return fe.Tag as TableViewColumn;
		}
		return null;
	}

	internal static FrameworkElement? CellForColumn(Panel? host, TableViewColumn? column)
	{
		if (host is null || column is null)
		{
			return null;
		}

		var children = host.Children;
		int count = children.Count;
		for (int i = 0; i < count; ++i)
		{
			if (children[i] is FrameworkElement cell)
			{
				if (ReferenceEquals(cell.Tag as TableViewColumn, column))
				{
					return cell;
				}
			}
		}
		return null;
	}

	internal void SetOwningRowInternal(TableViewRow? row)
	{
		m_owningRow = row is not null ? new WeakReference<TableViewRow>(row) : null;
	}

	internal double MeasuredWidthForColumn(TableViewColumn? column)
	{
		if (column is null)
		{
			return 0.0;
		}

		// Non-owning identity key (see m_measuredWidthsByColumn note) -- a raw pointer instead of a
		// per-lookup weak_ref resolve on the hot layout path; O(1) hash lookup.
		var key = column;
		return m_measuredWidthsByColumn.TryGetValue(key, out var value) ? value : 0.0;
	}

	private void CacheMeasuredWidthForColumn(TableViewColumn? column, double measuredWidth)
	{
		if (column is null)
		{
			return;
		}

		measuredWidth = StdMath.Max(0.0, measuredWidth);
		var key = column;
		// Grow-only within the pass (the map is cleared each MeasureOverride); operator[] default-inserts
		// 0.0 on first sight of the column this pass, so max() yields the measured width.
		m_measuredWidthsByColumn.TryGetValue(key, out var cachedWidth);
		m_measuredWidthsByColumn[key] = StdMath.Max(cachedWidth, measuredWidth);
	}

	private bool RecordAndDetectMeasuredWidthChange(
		TableViewColumn column, double measuredWidth,
		Dictionary<TableViewColumn, double> newLastMeasured)
	{
		var key = column;

		bool hadPrevious = m_lastMeasuredWidthsByColumn.TryGetValue(key, out var it);
		double previous = hadPrevious ? it : 0.0;

		// Record for next pass regardless (the caller swaps newLastMeasured into the member after the loop).
		newLastMeasured[key] = measuredWidth;

		// First time this panel sees the column: no history, so no change signal (a fresh/realized row's
		// initial sizing is already driven by ElementPrepared -> InvalidateMeasure).
		return hadPrevious && Math.Abs(measuredWidth - previous) > c_columnMeasureChangeThreshold;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		const float infinity = float.PositiveInfinity;
		float width = 0.0f;
		float height = 0.0f;

		m_measuredWidthsByColumn.Clear();

		// Rebuilt below from only the columns measured this pass, then swapped into
		// m_lastMeasuredWidthsByColumn -- so removed/collapsed columns are pruned automatically.
		Dictionary<TableViewColumn, double> newLastMeasured = new(m_lastMeasuredWidthsByColumn.Count, ReferenceEqualityComparer.Instance);

		// If a realized Auto cell's own measured width changed since the last pass (grow or shrink), the
		// column may need to re-resolve. Remember the owning TableView and request a deferred re-resolve
		// after the loop -- the body ScrollViewer otherwise absorbs the cell's measure invalidation, so
		// TableView::MeasureOverride (and ResolveColumnWidths) would never re-run for live content changes.
		TableView? ownerNeedingResolve = null;

		// The cell currently hosting an editor, if any. An editor is a different control from the display
		// element it replaced - a TextBox carries border, padding and a default MinWidth a TextBlock does
		// not - so letting it feed the Auto-width pass makes the column visibly jump wider the instant the
		// user starts editing, and jump back on commit. The column's width is a property of the DATA, not
		// of which control happens to be showing it, so the editing cell is measured at the width the
		// column already has and contributes nothing to the Auto calculation. WPF's DataGrid behaves the
		// same way: entering edit mode does not resize the column.
		UIElement? editingCell = null;
		if (m_owningRow is not null && m_owningRow.TryGetTarget(out var row))
		{
			editingCell = row.GetEditingCellWrapper();
		}

		foreach (UIElement child in Children)
		{
			var column = ColumnForCell(child);

			// Cells with no owning column (defensive): measure unconstrained and take their natural width.
			if (column is null)
			{
				child.Measure(new Size(infinity, availableSize.Height));
				var childDesired = child.DesiredSize;
				height = (float)StdMath.Max(height, (float)childDesired.Height);
				width += (float)childDesired.Width;
				continue;
			}

			// Collapsed columns occupy no width; still measure once (at zero width) so the child has a
			// valid measure, consistent with being arranged at zero width.
			if (column.Visibility != Visibility.Visible)
			{
				child.Measure(new Size(0.0f, availableSize.Height));
				continue;
			}

			float columnWidth = (float)StdMath.Max(0.0, column.ActualWidth);

			if (column.Width.GridUnitType == GridUnitType.Auto)
			{
				// The editing cell is pinned to the column's current width and excluded from the Auto
				// calculation entirely - no cached contribution, and no change signal, so it cannot ask
				// the owner to re-resolve either. Normal measurement resumes for this cell as soon as the
				// edit closes and the display element is back.
				if (editingCell is not null && ReferenceEquals(child, editingCell))
				{
					child.Measure(new Size(columnWidth > 0.0f ? columnWidth : infinity, availableSize.Height));
					height = (float)StdMath.Max(height, (float)child.DesiredSize.Height);

					// Carry a width forward so the column does not collapse while the edit is open.
					// Contributing nothing would let an Auto column resolve from the header and other
					// rows alone - and with a single realized row that means resolving to near zero,
					// which churns layout and can rebuild the row out from under the live editor.
					// Prefer this cell's previous measured width; fall back to the column's resolved
					// width when the editor opened before this panel ever measured the cell.
					var key = column;
					double carried = m_lastMeasuredWidthsByColumn.TryGetValue(key, out var previous)
						? previous
						: (double)columnWidth;

					newLastMeasured[key] = carried;
					CacheMeasuredWidthForColumn(column, carried);
					continue;
				}

				// Only Auto columns depend on content width. Measure unconstrained to discover the cell's
				// natural width and cache it; TableView pulls the max across header + realized rows once per
				// pass to resolve the Auto column width.
				child.Measure(new Size(infinity, availableSize.Height));
				var childDesired = child.DesiredSize;
				// TODO Uno: winrt::Size is float; Uno's DesiredSize is double, narrowed here so the fit guard compares like C++.
				float desiredWidth = (float)childDesired.Width;
				float desiredHeight = (float)childDesired.Height;
				height = (float)StdMath.Max(height, desiredHeight);
				CacheMeasuredWidthForColumn(column, desiredWidth);

				// Change signal: this cell's own measured width differs from the previous pass (grow OR
				// shrink). Comparing to the cell's own history -- not the column width -- is convergent: a
				// stably-narrower cell reports the same width each pass (no signal), while a cell whose
				// content actually changed reports a delta and asks the owner to re-resolve. Once the
				// re-resolve settles the column and the cell re-measures unchanged, the delta is zero and it
				// stops firing (MaxWidth-clamped columns also converge, since the compare is history-based,
				// not against the clamped column width -- so no ping-pong).
				if (RecordAndDetectMeasuredWidthChange(column, desiredWidth, newLastMeasured) &&
					ownerNeedingResolve is null)
				{
					ownerNeedingResolve = column.GetOwningTableView();
				}

				// Re-measure at the resolved width ONLY when the column is narrower than the content (e.g.
				// clamped by MaxWidth). XAML's arrange would otherwise expand the cell's render size to its
				// desired width and clip it to the column slot -- clipping away the cell's right border (the
				// vertical gridline); the constrained measure keeps ellipsized content and the border within
				// the column, and lets width-sensitive content report the height it wants at the column
				// width. When the column is at least as wide as the content, the unconstrained measure
				// already fits (no clip, no wrap, stable height), so the second measure is skipped -- this
				// mirrors CGrid, which measures unclamped Auto cells only once and does not re-measure them.
				if (columnWidth > 0.0f && columnWidth < desiredWidth)
				{
					child.Measure(new Size(columnWidth, availableSize.Height));
					height = (float)StdMath.Max(height, (float)child.DesiredSize.Height);
				}

				width += columnWidth;
			}
			else
			{
				// Pixel / Star: the width is independent of content, so skip the unconstrained probe and
				// measure once directly at the resolved column width (CGrid measures Star cells once, after
				// star resolution, at the resolved width; Pixel cells once at the fixed width). Before the
				// width is resolved (first pass, ActualWidth == 0), fall back to an unconstrained measure for
				// a provisional height; the panel is re-measured after ResolveColumnWidths sets ActualWidth.
				child.Measure(new Size(columnWidth > 0.0f ? columnWidth : infinity, availableSize.Height));
				height = (float)StdMath.Max(height, (float)child.DesiredSize.Height);
				width += columnWidth;
			}
		}

		// Adopt this pass's measured widths as the baseline for the next pass (prunes removed/collapsed
		// columns since only columns measured above were recorded).
		m_lastMeasuredWidthsByColumn = newLastMeasured;

		// Deferred (not during this measure) so it runs after the current layout pass; TableView debounces
		// so repeated change signals collapse to a single re-resolve.
		if (ownerNeedingResolve is not null)
		{
			ownerNeedingResolve.RequestColumnWidthResolve();
		}

		return new Size(width, height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		float x = 0.0f;

		foreach (UIElement child in Children)
		{
			var column = ColumnForCell(child);
			float w = column is not null
				? (column.Visibility == Visibility.Visible ? (float)StdMath.Max(0.0, column.ActualWidth) : 0.0f)
				: (float)child.DesiredSize.Width;

			// Cells are arranged at the resolved column width; content wider than the column clips/ellipsizes.
			child.Arrange(new Rect(x, 0.0f, w, finalSize.Height));
			x += w;
		}

		return new Size(x, finalSize.Height);
	}

	internal static void ApplyFrozenColumnLayout(Panel? host, double horizontalOffset, double leadingFrozenWidth)
	{
		if (host is null)
		{
			return;
		}

		// The pin math below runs entirely in logical (pre-mirror) LTR coordinates: children arrange
		// left-to-right regardless of FlowDirection, and XAML applies RTL as a single mirror transform
		// at the FlowDirection boundary above this panel. That mirror flips the counter-translation and
		// the clip geometry uniformly, so a Leading-frozen prefix pinned at logical-left lands on the
		// visual right under RTL - exactly where FrozenEdge.Leading must pin. No RTL special-casing is
		// needed here; the same code pins the correct edge in both flow directions.

		// Accumulate each cell's panel-space left edge from column ActualWidth.
		double panelX = 0.0;
		// Only the contiguous leading prefix (from the first cell) is pinned. A Leading flag on a
		// non-prefix column is treated as non-frozen so it cannot overlap scrolled cells.
		bool inLeadingPrefix = true;
		var children = host.Children;
		int count = children.Count;
		for (int i = 0; i < count; ++i)
		{
			if (children[i] is not FrameworkElement element)
			{
				continue;
			}

			var column = element.Tag as TableViewColumn;
			double cellWidth = column is not null
				? (column.Visibility == Visibility.Visible ? StdMath.Max(0.0, column.ActualWidth) : 0.0)
				: StdMath.Max(0.0, element.ActualWidth);
			bool columnIsLeading = column is not null && column.FrozenEdge == TableViewFrozenEdge.Leading;
			bool isLeadingFrozen = inLeadingPrefix && columnIsLeading;
			if (column is not null && !columnIsLeading)
			{
				// First non-Leading column ends the frozen prefix; later Leading columns are not pinned.
				inLeadingPrefix = false;
			}

			if (isLeadingFrozen)
			{
				// Counter-translate by the scroll offset and paint above scrolled cells.
				// Translation X/Y has no visual effect unless enabled on the element first.
				ElementCompositionPreview.SetIsTranslationEnabled(element, true);
				element.Translation = new Vector3((float)horizontalOffset, 0.0f, 0.0f);
				Canvas.SetZIndex(element, 1);
				element.Clip = null;
			}
			else
			{
				element.Translation = new Vector3(0.0f, 0.0f, 0.0f);
				Canvas.SetZIndex(element, 0);

				// Hide local x below leadingFrozenWidth - panelX + offset.
				double clipLeft = (leadingFrozenWidth > 0.0)
					? StdMath.Max(0.0, leadingFrozenWidth + horizontalOffset - panelX)
					: 0.0;
				if (clipLeft > 0.0)
				{
					// A zero-width clip hides cells fully covered by the pinned region.
					double clipWidth = StdMath.Max(0.0, cellWidth - clipLeft);
					double actualHeight = element.ActualHeight;
					if (actualHeight <= 0.0)
					{
						// Skip clipping until measured instead of using a magic tall sentinel.
						element.Clip = null;
						panelX += cellWidth;
						continue;
					}

					float clipHeight = (float)actualHeight;
					var geometry = element.Clip as RectangleGeometry;
					if (geometry is null)
					{
						geometry = new RectangleGeometry();
						element.Clip = geometry;
					}
					geometry.Rect = new Rect(
						(float)clipLeft,
						0.0f,
						(float)clipWidth,
						clipHeight);
				}
				else
				{
					element.Clip = null;
				}
			}

			panelX += cellWidth;
		}
	}
}
