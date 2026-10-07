// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Columns.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Column DP wiring, per-column updates, and frozen-column layout live here.
// The column-width layout engine (Pixel/Auto/Star resolution) lives in TableView_Layout.cpp.

partial class TableView
{
	private static bool IsColumnOwnedBy(TableView owner, TableViewColumn? column) =>
		column is not null && column.GetOwningTableView() == owner;

	private static bool TrySetColumnOwnerForTracking(TableView owner, TableViewColumn? column)
	{
		if (column is null)
		{
			return false;
		}

		if (column.SetOwningTableViewInternal(owner))
		{
			return true;
		}

		TVDiag.LogRetailF("[TableView] A column already owned by another TableView was ignored.");
		return false;
	}

	// Leading-frozen columns must be a contiguous prefix so the pinned band aligns.
	private double ComputeLeadingFrozenWidth()
	{
		double width = 0.0;
		if (Columns is { } columns)
		{
			// Only a contiguous leading prefix (from column 0) is frozen. A Leading flag on a
			// non-prefix column is ignored so it cannot corrupt the pinned-band layout.
			foreach (var column in columns)
			{
				if (!IsColumnOwnedBy(this, column))
				{
					continue;
				}
				if (column.FrozenEdge != TableViewFrozenEdge.Leading)
				{
					break;
				}
				width += column.Visibility == Visibility.Visible ? StdMath.Max(0.0, column.ActualWidth) : 0.0;
			}
		}
		return width;
	}

	private void RefreshFrozenColumns()
	{
		double leadingFrozenWidth = ComputeLeadingFrozenWidth();

		// No active frozen columns; avoid walking realized rows on every scroll.
		if (leadingFrozenWidth <= 0.0 && !m_frozenColumnsActive)
		{
			return;
		}

		// Run once after deactivation to clear prior transforms and clips.
		m_frozenColumnsActive = leadingFrozenWidth > 0.0;

		double horizontalOffset = 0.0;
		if (m_bodyScroller is { } bodyScroller)
		{
			horizontalOffset = bodyScroller.HorizontalOffset;
		}

		// Header and row cells share pinning so they stay aligned.
		if (m_headerHost is { } headerHost)
		{
			TableViewCellsPanel.ApplyFrozenColumnLayout(headerHost, horizontalOffset, leadingFrozenWidth);
		}

		// Re-pin realized rows.
		ForEachRealizedRow(row =>
		{
			row.RefreshFrozenColumnLayout(horizontalOffset, leadingFrozenWidth);
		});
	}

	internal void PinFrozenColumnsForRow(TableViewRow? row)
	{
		if (row is null)
		{
			return;
		}

		double leadingFrozenWidth = ComputeLeadingFrozenWidth();
		if (leadingFrozenWidth <= 0.0)
		{
			return;
		}

		double horizontalOffset = 0.0;
		if (m_bodyScroller is { } bodyScroller)
		{
			horizontalOffset = bodyScroller.HorizontalOffset;
		}

		// The row owns its cell panel; ask it to pin its own cells (the same path RefreshFrozenColumns
		// uses) instead of having TableView reach into and lay out the row's panel.
		row.RefreshFrozenColumnLayout(horizontalOffset, leadingFrozenWidth);
	}

	private void DetachAllColumnOwners()
	{
		bool purgedSortState = false;
		foreach (var weakCol in m_trackedColumns)
		{
			if (weakCol is { } col)
			{
				col.SetOwningTableViewInternal(null);
				purgedSortState |= PurgeColumnFromSortState(col);
			}
		}
		m_trackedColumns.Clear();

		if (purgedSortState)
		{
			QueueClearSortAfterColumnRemoval();
		}
	}

	private void TrackColumnsFromVector(IObservableVector<TableViewColumn>? columns)
	{
		if (columns is null)
		{
			return;
		}

		int size = columns.Count;
		m_trackedColumns.EnsureCapacity(size);
		for (int i = 0; i < size; ++i)
		{
			var col = columns[i];
			bool ownsColumn = TrySetColumnOwnerForTracking(this, col);
			m_trackedColumns.Add(ownsColumn ? col : null);
		}
	}

	private void OnColumnsPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// Avoid rehooking the same vector.
		if (ReferenceEquals(args.OldValue, args.NewValue))
		{
			return;
		}

		// Detach the old vector so replaced Columns do not leak subscriptions or owners.
		if (m_columnsVectorChangedToken.Disposable is not null)
		{
			m_columnsVectorChangedToken.Disposable = null;
		}

		// Re-hook observable vectors; Columns is ABI-projected as IVector<>.
		if (args.NewValue is IObservableVector<TableViewColumn> observableNewColumns)
		{
			WeakReference<TableView> weakThis = new(this);
			VectorChangedEventHandler<TableViewColumn> handler = (sender, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnColumnsVectorChanged(sender, e);
				}
			};
			observableNewColumns.VectorChanged += handler;
			m_columnsVectorChangedToken.Disposable = Disposable.Create(() => observableNewColumns.VectorChanged -= handler);
		}

		// Treat replacement as a reset so ownership and headers match the new vector.
		if (args.NewValue is IObservableVector<TableViewColumn> newColumns)
		{
			// Inline Reset semantics because IVectorChangedEventArgs cannot be synthesized here.
			DetachAllColumnOwners();
			TrackColumnsFromVector(newColumns);
			RebuildHeaders();
			// Columns replaced: recompute Auto widths for the new set and re-pin frozen columns.
			ResetColumnDesiredWidths();
			// Realized rows keep old revokers when the owner is unchanged; re-subscribe them.
			ForEachRealizedRow(row =>
			{
				row.RefreshColumnsSubscriptionInternal();
			});
		}
		else
		{
			// Non-observable value: drop column tracking.
			DetachAllColumnOwners();
			RebuildHeaders();
			// Flush realized rows' prior revokers; no non-observable vector is attached.
			ForEachRealizedRow(row =>
			{
				row.RefreshColumnsSubscriptionInternal();
			});
		}
	}

	private void OnColumnsVectorChanged(
		IObservableVector<TableViewColumn> sender,
		IVectorChangedEventArgs args)
	{
		// Track columns incrementally so owner back-pointers stay in sync without diffing.
		var change = args.CollectionChange;
		var index = args.Index;

		switch (change)
		{
			case CollectionChange.ItemInserted:
				{
					if (index < (uint)sender.Count)
					{
						var col = sender[(int)index];
						bool ownsColumn = false;
						if (col is not null)
						{
							ownsColumn = col.SetOwningTableViewInternal(this);
							if (ownsColumn)
							{
								// A re-inserted / recycled column instance may carry a stale grow-only accumulator
								// from a prior position or table; clear it so it re-grows from its content here.
								col.ResetDesiredWidthInternal();
							}
							else
							{
								TVDiag.LogRetailF("[TableView] A column already owned by another TableView was ignored.");
							}
						}
						m_trackedColumns.Insert(
							(int)Math.Min(index, (uint)m_trackedColumns.Count),
							ownsColumn ? col : null);
					}
					break;
				}
			case CollectionChange.ItemRemoved:
				{
					if (index < (uint)m_trackedColumns.Count)
					{
						if (m_trackedColumns[(int)index] is { } col)
						{
							col.SetOwningTableViewInternal(null);
							// A column that has left Columns must not stay the active sort. Reshaping here
							// would run inside the VectorChanged callback, so the clear is deferred until the
							// collection has settled.
							if (PurgeColumnFromSortState(col))
							{
								QueueClearSortAfterColumnRemoval();
							}
						}
						m_trackedColumns.RemoveAt((int)index);
					}
					break;
				}
			case CollectionChange.ItemChanged:
				{
					if (index < (uint)m_trackedColumns.Count)
					{
						var oldCol = m_trackedColumns[(int)index];
						if (oldCol is not null)
						{
							oldCol.SetOwningTableViewInternal(null);
							if (PurgeColumnFromSortState(oldCol))
							{
								QueueClearSortAfterColumnRemoval();
							}
						}
						if (index < (uint)sender.Count)
						{
							var newCol = sender[(int)index];
							bool ownsColumn = false;
							if (newCol is not null)
							{
								ownsColumn = newCol.SetOwningTableViewInternal(this);
								if (ownsColumn)
								{
									// Clear any stale grow-only accumulator the incoming instance carried from
									// elsewhere, but preserve it when the SAME instance is re-notified in place (a
									// no-op change must not drop the column's accumulated Auto max).
									if (!ReferenceEquals(newCol, oldCol))
									{
										newCol.ResetDesiredWidthInternal();
									}
								}
								else
								{
									TVDiag.LogRetailF("[TableView] A column already owned by another TableView was ignored.");
								}
							}
							m_trackedColumns[(int)index] = ownsColumn ? newCol : null;
						}
					}
					break;
				}
			case CollectionChange.Reset:
			default:
				{
					// Reset owner back-pointers against the current vector contents.
					DetachAllColumnOwners();
					TrackColumnsFromVector(sender);
					break;
				}
		}

		QueueRebuildHeaders();

		// Realized rows observe Columns directly; no TableView broadcast is needed.
		if (change == CollectionChange.Reset)
		{
			// The whole vector was replaced: clear every grow-only accumulator and re-resolve.
			ResetColumnDesiredWidths();
		}
		else
		{
			// An incremental add / remove / change does not alter the row data, so pre-existing columns
			// keep their accumulated Auto max (resetting them would shrink unrelated columns to only the
			// currently realized rows). A newly inserted column starts fresh at 0 and grows from content;
			// re-resolve to pick up the new fixed total and star share.
			InvalidateMeasure();
		}
	}

	internal void OnColumnVisibilityChanged(TableViewColumn? column)
	{
		if (column is null)
		{
			return;
		}

		var visibility = column.Visibility;

		if (m_headerHost is { } headerHost)
		{
			if (TableViewCellsPanel.CellForColumn(headerHost, column) is { } headerCell)
			{
				headerCell.Visibility = visibility;
			}
		}

		// The row owns its cells; ask each to apply the column's visibility instead of reaching
		// through the row into its cell panel and mutating each cell here.
		ForEachRealizedRow(row =>
		{
			row.RefreshColumnVisibility(column, visibility);
		});

		// Keep a synchronous RefreshFrozenColumns here: collapsing a column changes the leading
		// frozen-band width without changing any surviving column's ActualWidth, so ResolveColumnWidths'
		// `changed` gate stays false and would never re-pin. Refresh it directly.
		InvalidateMeasure();
		RefreshFrozenColumns();
	}

	internal void OnColumnWidthChanged(TableViewColumn? column)
	{
		if (column is null)
		{
			return;
		}

		// A Width-mode, MinWidth, or MaxWidth change alters the fixed total and star factors, but none of
		// them is a data-set change, so do NOT reset the grow-only Auto accumulator (that would let an
		// Auto column shrink to only the currently realized rows). Re-resolve on the next measure pass.
		//
		// For a Pixel column, TableViewColumn::OnPropertyChanged already wrote the final ActualWidth (via
		// UpdateActualWidth) BEFORE this runs, so ResolveColumnWidths computes an unchanged width
		// (changed == false) and skips BOTH its cell-panel re-arrange and its frozen re-pin. Compensate
		// directly: re-arrange the header + row cell panels at the new width and re-pin frozen columns.
		// (For Auto/Star the deferred resolve also does both on the real width; these direct calls are
		// idempotent there.)
		InvalidateMeasure();
		InvalidateCellPanels();
		RefreshFrozenColumns();
	}

	internal void OnColumnCellTemplateChanged(TableViewColumn? column)
	{
		if (column is null)
		{
			return;
		}

		// Rebuild realized rows so the new CellTemplate is applied; virtualized rows pick it up on realization.
		ForEachRealizedRow(row =>
		{
			row.RefreshCells();
		});

		// The new template can be narrower or wider than the old cells: recompute only THIS column's Auto
		// content width. Reset only this column's grow-only accumulator -- a global reset would shrink
		// unrelated Auto columns to only the currently-realized rows (the same reason OnColumnsVectorChanged
		// does not reset pre-existing columns).
		column.ResetDesiredWidthInternal();
		InvalidateMeasure();
	}

	internal void OnColumnHeaderChanged(TableViewColumn? column)
	{
		// Header content changed: re-render headers, then recompute only the CHANGED column's Auto width
		// (a changed header can be wider or narrower than that column's cells). Reset only this column's
		// grow-only accumulator -- a global reset would shrink UNRELATED Auto columns to only the
		// currently-realized rows. Resetting even when the column is currently Pixel/Star preserves the
		// "switched back to Auto" stale-width safeguard for that one column.
		QueueRebuildHeaders();
		if (column is not null)
		{
			column.ResetDesiredWidthInternal();
		}
		InvalidateMeasure();
	}

	internal void OnColumnHeaderToolTipChanged(TableViewColumn? column)
	{
		if (column is null)
		{
			return;
		}

		// Only the realized header carries a tooltip; RebuildHeaders reads the current value when it runs.
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		if (TableViewCellsPanel.CellForColumn(host, column) is { } headerCell)
		{
			TableViewDetails.ApplyHeaderToolTip(headerCell, column.HeaderToolTip);
		}
	}

	internal void OnColumnFrozenEdgeChanged(TableViewColumn? column)
	{
		if (column is null)
		{
			return;
		}

		// Re-render headers to re-tag cells for the new frozen prefix; RebuildHeaders ends with
		// RefreshFrozenColumns, which re-pins the leading-frozen band on the header and realized rows.
		QueueRebuildHeaders();
	}
}
