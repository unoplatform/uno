// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Sort.cpp, tag winui3/main, commit 79209e041

// Single-column sort for TableView.
//
// The control owns the ordering for every source it can display: an ItemsSource that is not
// already a TableViewSource is projected through one, so a header click installs a sort axis on
// that projection and the rows re-project. Sorting is raised first as a cancellable pre-event; an
// app that would rather order its own collection cancels it and owns both the rows and the
// column's sort state from there.
//
// A column supplies its key either as a property path (SortMemberPath, or the cell binding for a
// text column) or as a CustomSortComparer. A comparer cannot be pushed into the projection - the
// projection sorts by key - so comparer columns are ranked up front and the rank becomes the key.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewSourceSortBinding
{
	public void ResetCustomSort()
	{
		if (CustomSortState is not null)
		{
			CustomSortState.Reset();
		}
	}

	public void Clear()
	{
		MemberPath = "";
		AxisToken = "";
		KeySelector = null;
		// Dropped, not reset: the axis left on a source this table no longer owns still ranks through
		// it. The next sort mints a fresh adapter and selector.
		CustomSortState = null;
	}
}

partial class TableView
{
	private bool RepositionCustomSortItem(object? item, bool reshapePending)
	{
		var state = m_tableViewSourceSort.CustomSortState;
		if (state is null || !state.HasComparer())
		{
			return false;
		}
		if (item is null)
		{
			state.InvalidateRanks();
			return false;
		}
		return state.Reposition(item, reshapePending);
	}

	private void ReleaseCustomSortRows(object? item, List<object?> liveRows)
	{
		var state = m_tableViewSourceSort.CustomSortState;
		if (state is null || !state.HasComparer())
		{
			return;
		}
		if (item is not null)
		{
			state.Forget(item);
		}
		else
		{
			state.RetainOnly(liveRows);
		}
	}

	// Rank() starts from the current rows, so it drops ranks rooted while released and re-reads values
	// edited meanwhile; re-declaring the axis makes the projection read the new keys.
	private void RerankCustomSortAfterReclaim()
	{
		var state = m_tableViewSourceSort.CustomSortState;
		if (state is null || !state.HasComparer() || m_sortedColumns.Count != 1)
		{
			return;
		}
		var column = m_sortedColumns[0];
		if (column is null || column.CustomSortComparer is null)
		{
			return;
		}
		var direction = column.SortDirection;
		if (direction == SortDirection.None || !CanApplySortTo(direction))
		{
			return;
		}

		var preservedSelection = SelectedItemInternal();
		try
		{
			// A rerank after reclaim is control-driven, not a user sort, so it carries no TiP operation.
			if (SyncTableViewSourceSort(column, direction, 0) && preservedSelection is not null)
			{
				ApplySelection(FindEntryIndexForDataItem(preservedSelection));
			}
		}
		catch (Exception)
		{
			// Dispatcher callback: best-effort, the previous order stands.
			TVDiag.LogRetailF("[TableView] Re-ranking the custom sort after reclaim failed.");
		}
	}

	// TODO Uno: TableView_Sort.cpp's anonymous-namespace LocalizedOrFallback is identical to TableView_Grouping.cpp's; both share the definition in TableView.Grouping.mux.cs.

	// Evaluates a property path against a row item; see SortMemberPathResolver.h. The same
	// evaluator backs TableViewSource's path-based Sort verb, so a column and a fluent sort on
	// the same path produce the same keys.

	private static bool HasResolvedSortMemberPath(TableViewColumn? column)
	{
		// Dispatch on the projection, not winrt::get_self: GetSortMemberPathCore is `overridable`,
		// so get_self would call the C++ implementation's virtual directly and bypass WinRT
		// composition. A column authored in C# that overrides it to compute a sort path would then
		// be treated as having none, and silently become unsortable.
		return column is not null && !string.IsNullOrEmpty(column.GetSortMemberPathCore());
	}

	private static bool CanSortColumn(TableViewColumn? column)
	{
		if (column is null || !column.CanSort)
		{
			return false;
		}

		return column.CustomSortComparer is not null || HasResolvedSortMemberPath(column);
	}

	// Identity-derived so two columns with the same SortMemberPath still own separate axes; falls
	// back to the path only if the column has no usable identity.
	private static string GetTableViewSourceSortAxisToken(TableViewColumn? column, string sortMemberPath)
	{
		if (column is not null)
		{
			// TODO Uno: Original C++ formats the IUnknown address:
			// wchar_t buffer[48];
			// swprintf_s(buffer, L"column:%p", unknown.get());
			// Uno has no stable object address, so ObjectIdentityHelper supplies a per-object id,
			// printed the way MSVC prints a 64-bit %p.
			return "column:" + ObjectIdentityHelper.GetId(column).ToString("X16", CultureInfo.InvariantCulture);
		}

		return "path:" + sortMemberPath;
	}

	private TableViewSource? ShapingSourceInternal()
	{
		// AdoptItemsSource is the single place that decides what the source is; everything else
		// reads the answer here rather than re-deriving it from ItemsSource.
		return m_activeSource;
	}

	internal bool IsColumnUserSortableInternal(TableViewColumn? column)
	{
		return column is not null && CanUserSortColumns && CanSortColumn(column);
	}

	private bool IsSortClearStillValid()
	{
		return m_sortedColumns.Count != 0;
	}

	// Refused a source another table owns: there is nothing to sort until it is released, and the
	// columns keep their state for the reconcile that follows.
	private bool CanSortColumnNow(TableViewColumn? column, bool requireCanSort = true)
	{
		// Unadopted after an unload, too, which no longer awaits it.
		if (m_awaitedSource is not null && m_awaitedSource.TryGetTarget(out _) ||
			(ShapingSourceInternal() is null && IsActiveSourceOwnedByOther()))
		{
			return false;
		}
		return requireCanSort ? CanSortColumn(column) : column is not null;
	}

	private bool CanApplySortTo(SortDirection direction)
	{
		if (direction == SortDirection.None)
		{
			return true;
		}
		var source = ShapingSourceInternal();
		if (source is not null && m_activeSourceReleased && source.IsOwnedByOther(this))
		{
			return false;
		}
		return source is null || source.CanApplySort();
	}

	private bool IsSortRequestStillValid(TableViewColumn? column, bool requireCanSort = true)
	{
		if (column is null || !CanSortColumnNow(column, requireCanSort))
		{
			return false;
		}

		// A clear that skips the CanSort gate clears only the column's own sort. A replayed or
		// re-entered one may find another column sorted since, and must not clear that sort.
		if (!requireCanSort && column.SortDirection == SortDirection.None)
		{
			return false;
		}

		// The column must still be one of ours: a Sorting handler is free to remove it.
		if (Columns is { } columns)
		{
			foreach (var ownedColumn in columns)
			{
				if (ownedColumn == column)
				{
					return true;
				}
			}
		}

		return false;
	}

	// ----- Sorting -----
	// Single-column sort. The control reshapes the rows itself: an ItemsSource that is not already
	// a TableViewSource is projected through one internally, so sorting works over any collection
	// the control can display. Sorting is raised first; cancel it to own the ordering instead.
	// The active sort state lives on the column, as TableViewColumn.SortDirection; Sorted hands
	// the app the column that changed, and a null column there means the sort was cleared.
	//
	// This and TableViewSource.Sort reconcile rather than stack: exactly one sort axis is ever in
	// force, and the last writer wins. See the dev spec for the full ownership rules.

	// Clears any other sort state, then sets this column's SortDirection. Pass SortDirection.None
	// to clear. Returns true when the sort state changed.
	public bool SortByColumn(TableViewColumn column, SortDirection direction)
	{
		// A column that opted out of sorting can still have the sort it holds cleared.
		bool clearsOwnSort = direction == SortDirection.None &&
			column is not null && column.SortDirection != SortDirection.None;
		return SortByColumnInternal(column, direction, true, !clearsOwnSort);
	}

	private bool SortByColumnInternal(
		TableViewColumn? column, SortDirection direction, bool measureTip, bool requireCanSort = true)
	{
		FlushDeferredEditReshapes();
		// Only a clear may skip the CanSort gate.
		requireCanSort = requireCanSort || direction != SortDirection.None;
		if (!CanSortColumnNow(column, requireCanSort))
		{
			return false;
		}

		if (direction == SortDirection.None && m_sortedColumns.Count == 0)
		{
			return false;
		}

		if (m_sortedColumns.Count == 1)
		{
			if (m_sortedColumns[0] is { } sortedColumn)
			{
				if (sortedColumn == column && sortedColumn.SortDirection == direction)
				{
					return false;
				}
			}
		}

		// A null item or one object on two rows can't be sorted; refuse before any state changes.
		if (!CanApplySortTo(direction))
		{
			return false;
		}

		// An open editor is showing a row at an index that is about to move. Close it first; if it
		// cannot close synchronously, replay this request once it does.
		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending && !m_isApplyingCoalescedEditReshape)
			{
				QueueCoalescedEditReshape(() =>
				{
					SortByColumnInternal(column, direction, measureTip, requireCanSort);
				});
			}
			return false;
		}
		if (!IsSortRequestStillValid(column, requireCanSort))
		{
			return false;
		}

		if (RaiseSortingAndCheckCanceled(column, direction))
		{
			return false;
		}

		// The edit commit and the Sorting handler are app code that can change the rows.
		if (!IsSortRequestStillValid(column, requireCanSort) || !CanApplySortTo(direction))
		{
			return false;
		}

		ApplySingleColumnSortState(column!, direction);
		RecomputeSortDPsAndRaiseInternal(column, measureTip);
		DrainCoalescedEditReshape();
		return true;
	}

	// Cycles the trigger column's direction per SortCycle, clearing any other sort state first.
	// Returns true when the sort state changed.
	public bool ToggleSortDirection(TableViewColumn column)
	{
		FlushDeferredEditReshapes();
		if (!CanSortColumnNow(column))
		{
			return false;
		}

		// The column's SortCycle names both halves of the policy: which direction an unsorted column
		// opens in, and whether a trailing unsorted step exists. Read from the column being clicked,
		// so columns in one table can carry different cycle policies.
		//
		// Opening Ascending:  None -> Ascending -> Descending -> (None | Ascending)
		// Opening Descending: None -> Descending -> Ascending -> (None | Descending)
		var cycle = column.SortCycle;
		bool opensDescending =
			cycle == TableViewSortCycle.DescendingAscending ||
			cycle == TableViewSortCycle.DescendingAscendingNone;
		bool cyclesToUnsorted =
			cycle == TableViewSortCycle.AscendingDescendingNone ||
			cycle == TableViewSortCycle.DescendingAscendingNone;

		var opening = opensDescending
			? SortDirection.Descending
			: SortDirection.Ascending;
		var second = opensDescending
			? SortDirection.Ascending
			: SortDirection.Descending;

		// Compared against the cycle's own directions rather than switching on Ascending/Descending,
		// so the same walk serves both opening directions.
		var current = column.SortDirection;
		SortDirection next;
		if (current == opening)
		{
			next = second;
		}
		else if (current == second)
		{
			next = cyclesToUnsorted ? SortDirection.None : opening;
		}
		else
		{
			next = opening;
		}

		if (!CanApplySortTo(next))
		{
			return false;
		}

		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending && !m_isApplyingCoalescedEditReshape)
			{
				QueueCoalescedEditReshape(() =>
				{
					ToggleSortDirection(column);
				});
			}
			return false;
		}
		if (!IsSortRequestStillValid(column))
		{
			return false;
		}

		if (RaiseSortingAndCheckCanceled(column, next))
		{
			return false;
		}

		if (!IsSortRequestStillValid(column) || !CanApplySortTo(next))
		{
			return false;
		}

		ApplySingleColumnSortState(column, next);
		RecomputeSortDPsAndRaiseInternal(column);
		DrainCoalescedEditReshape();
		return true;
	}

	// Clears every column's sort state in one batch. Returns true when anything was cleared.
	public bool ClearSort()
	{
		return ClearSortInternal(true, true);
	}

	private bool IsActiveSourceOwnedByOther()
	{
		var source = ShapingSourceInternal();
		if (source is null)
		{
			// A refused reclaim leaves the source unadopted, also once unloaded and no longer awaiting it.
			return ItemsSource is TableViewSource;
		}
		return m_activeSourceReleased && source.IsOwnedByOther(this);
	}

	private bool IsSortLitOnlyByApp()
	{
		if (!string.IsNullOrEmpty(m_tableViewSourceSort.AxisToken))
		{
			return false;
		}
		var source = ShapingSourceInternal();
		if (source is null)
		{
			return false;
		}
		// Released but unowned (unloaded): the axes are still the app's.
		if (m_activeSourceReleased && source.IsOwnedByOther(this))
		{
			return false;
		}
		// A column: axis is one a table installed, never the app's.
		var axes = source.ActiveSortAxisInfos();
		return axes.Any(axis => !axis.AxisToken.StartsWith("column:", StringComparison.Ordinal));
	}

	private bool ClearSortInternal(bool measureTip, bool explicitClear = false)
	{
		CancelPendingSortRestore();
		FlushDeferredEditReshapes();
		if (m_sortedColumns.Count == 0)
		{
			return false;
		}

		if (!explicitClear && IsSortLitOnlyByApp())
		{
			return false;
		}

		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending && !m_isApplyingCoalescedEditReshape)
			{
				QueueCoalescedEditReshape(() =>
				{
					ClearSortInternal(measureTip, explicitClear);
				});
			}
			return false;
		}
		if (!IsSortClearStillValid())
		{
			return false;
		}

		if (RaiseSortingAndCheckCanceled(null, SortDirection.None))
		{
			return false;
		}

		if (!IsSortClearStillValid())
		{
			return false;
		}

		foreach (var weakColumn in m_sortedColumns)
		{
			if (weakColumn is { } column)
			{
				column.SetSortStateInternal(SortDirection.None);
			}
		}
		m_sortedColumns.Clear();

		// Null trigger: Sorted fires with Column=null to signal a full clear.
		RecomputeSortDPsAndRaiseInternal(null, measureTip, explicitClear);
		DrainCoalescedEditReshape();
		return true;
	}

	private void ResetSortStateForNewItemsSource()
	{
		// A new data set invalidates the sort, and the projection the sort was applied to is discarded
		// with it. Left alone, the control keeps each column reporting a SortDirection and drawing the
		// chevron for an order the new rows are not actually in. WPF DataGrid does exactly this in
		// ClearSortDescriptionsOnItemsSourceChange, down to clearing each column's SortDirection.
		//
		// Silent, unlike ClearSort: there is no reshape to perform because the source is being rebuilt
		// from scratch, and the data set is already gone by the time a handler could react, so this is
		// not a cancellable Sorting nor a Sorted the app could act on.
		CancelPendingSortRestore();
		if (m_sortedColumns.Count == 0 && string.IsNullOrEmpty(m_tableViewSourceSort.AxisToken))
		{
			return;
		}

		foreach (var weakColumn in m_sortedColumns)
		{
			if (weakColumn is { } column)
			{
				column.SetSortStateInternal(SortDirection.None);
			}
		}

		m_sortedColumns.Clear();

		// The axis token and key selector belong to the projection being discarded, so carrying them
		// into the next one would address an axis that no longer exists.
		m_tableViewSourceSort.Clear();
	}

	// v1 is single-column sort, so applying a direction always clears every other column first.
	private void ApplySingleColumnSortState(TableViewColumn column, SortDirection direction)
	{
		CancelPendingSortRestore();
		foreach (var weakColumn in m_sortedColumns)
		{
			if (weakColumn is { } sortedColumn && sortedColumn != column)
			{
				sortedColumn.SetSortStateInternal(SortDirection.None);
			}
		}
		m_sortedColumns.Clear();

		column.SetSortStateInternal(direction);
		if (direction != SortDirection.None)
		{
			m_sortedColumns.Add(column);
		}
	}

	private bool RaiseSortingAndCheckCanceled(
		TableViewColumn? trigger,
		SortDirection direction)
	{
		if (Sorting is null)
		{
			return false;
		}

		var args = new TableViewSortingEventArgs(trigger, direction);
		try { Sorting?.Invoke(this, args); }
		catch (Exception ex) { TableViewTip.RethrowAppFault(ex, TableViewTip.AppFaultKind.Callback); throw; }

		return args.Cancel;
	}

	private TableViewKeySelector GetTableViewSourceSortKeySelector(
		string sortMemberPath)
	{
		bool sortMemberPathChanged = m_tableViewSourceSort.MemberPath != sortMemberPath;
		if (m_tableViewSourceSort.CustomSortState is null)
		{
			m_tableViewSourceSort.CustomSortState = new ShapingHelpers.CustomSortRankAdapter();
		}
		m_tableViewSourceSort.MemberPath = sortMemberPath;
		m_tableViewSourceSort.ResetCustomSort();

		// Reused across re-sorts of the same path so the projection is not handed a new delegate
		// identity every time. The selector closes over the state by shared_ptr, which is what lets
		// ResetCustomSort swap the comparer underneath a live selector.
		if (m_tableViewSourceSort.KeySelector is null || sortMemberPathChanged)
		{
			var customSortState = m_tableViewSourceSort.CustomSortState;
			var sortMemberPathResolver = new SortMemberPathResolver(sortMemberPath);
			m_tableViewSourceSort.KeySelector = new TableViewKeySelector(
				(object? item) =>
				{
					if (customSortState is not null && customSortState.HasComparer())
					{
						return customSortState.KeyFor(item);
					}
					return sortMemberPathResolver.Resolve(item);
				});
		}

		return m_tableViewSourceSort.KeySelector;
	}

	private bool SyncTableViewSourceSort(
		TableViewColumn? trigger,
		SortDirection direction,
		ulong tipGeneration,
		bool explicitClear = false)
	{
		var tableViewSource = ShapingSourceInternal();
		if (tableViewSource is null)
		{
			return false;
		}

		// Released on Unloaded or before the first Loaded. While another table owns the source, this
		// table changes only its own state; a path axis left behind re-lights the column on reclaim.
		if (m_activeSourceReleased && tableViewSource.IsOwnedByOther(this))
		{
			if (direction == SortDirection.None)
			{
				m_tableViewSourceSort.Clear();
			}
			return false;
		}

		// Comparer faults contained before this sort (a live re-sort) are not its own.
		if (m_tableViewSourceSort.CustomSortState is { } priorRankAdapter) { priorRankAdapter.TakeSwallowedFaults(); }

		// A custom comparer that threw was contained as "equal": the sort stands, and the fault is the app's. Reported
		// however this sort ends, from the adapter it ranked with (a reentrant sort can install another).
		ShapingHelpers.CustomSortRankAdapter? rankedWith = null;
		using var reportContained = Disposable.Create(() =>
		{
			if (rankedWith is null) { return; }
			var contained = rankedWith.TakeSwallowedFaults();
			if (contained.count != 0)
			{
				TableViewTelemetry.ReportError(m_telemetrySession, 0, TableViewTip.Operation.Sort, TableViewTip.Stage.Sort,
					true, TableViewTip.Attribution.Application, contained.firstError, TableViewTip.AppFaultKind.Callback,
					contained.count);
			}
		});

		TableViewKeySelector? keySelector = null;
		string sortAxisToken = "";
		// What this axis sorts on, purely descriptive. Stays empty for a CustomSortComparer: the
		// ordering is the comparer's, not the path's, so nothing else may claim the axis sorts by a
		// property.
		string axisSortMemberPath = "";
		// A clear removes the installed axis by its token, so it must not depend on the trigger's sort
		// path, which a Sorting handler may have changed or emptied since the axis was installed.
		if (trigger is not null && direction != SortDirection.None)
		{
			if (trigger.CustomSortComparer is { } customComparer)
			{
				keySelector = GetTableViewSourceSortKeySelector("");
				sortAxisToken = GetTableViewSourceSortAxisToken(trigger, "");

				if (direction != SortDirection.None)
				{
					var rowsView = m_rowsItemsSourceView;
					if (rowsView is null)
					{
						rowsView = tableViewSource.GetItemsSourceView();
					}

					// Snapshot the rows for the engine, which ranks over a plain vector and never sees
					// an ItemsSourceView. The comparer is wrapped into a neutral pairwise functor; the
					// engine catches a throw and degrades it to "equal", so this lambda stays trivial.
					List<object?> rows = new();
					if (rowsView is not null)
					{
						var count = rowsView.Count;
						rows.Capacity = count > 0 ? count : 0;
						for (int i = 0; i < count; ++i)
						{
							var row = rowsView.GetAt(i);
							// In a grouped projection the headers are the only synthesized rows; data
							// rows are the app's own items. Rank the data items only - a header is not
							// something the app's comparer has ever seen.
							if (row is not null && GroupedEntry.TryGetGroupedEntry(row) is null)
							{
								rows.Add(row);
							}
						}
					}

					rankedWith = m_tableViewSourceSort.CustomSortState;
					if (rankedWith is not null)
					{
						ShapingHelpers.PairwiseComparer comparer =
							(object? a, object? b) =>
							{
								return customComparer.Compare(a, b);
							};
						rankedWith.Rank(comparer, rows);
					}
				}
			}
			else
			{
				// Projected dispatch - see the note in HasResolvedSortMemberPath.
				string sortMemberPath;
				try { sortMemberPath = trigger.GetSortMemberPathCore(); }
				catch (Exception ex)
				{
					// An app-derived column's override is the app's code; TableView's own columns are TableView's.
					if (TableViewDetails.IsAppDerivedColumn(trigger)) { TableViewTip.RethrowAppFault(ex, TableViewTip.AppFaultKind.Callback); }
					throw;
				}
				if (string.IsNullOrEmpty(sortMemberPath))
				{
					return false;
				}
				keySelector = GetTableViewSourceSortKeySelector(sortMemberPath);
				sortAxisToken = GetTableViewSourceSortAxisToken(trigger, sortMemberPath);
				axisSortMemberPath = sortMemberPath;
			}
		}
		else
		{
			// A clear: reuse whatever axis is currently installed so it can be removed by token.
			keySelector = m_tableViewSourceSort.KeySelector;
			sortAxisToken = m_tableViewSourceSort.AxisToken;
			if (direction == SortDirection.None)
			{
				m_tableViewSourceSort.ResetCustomSort();
			}
		}

		// Limitation: this reshapes on explicit sort and on collection notifications only.
		// INotifyPropertyChanged-only mutations of the active sort key do not live re-sort until the
		// next collection change.
		var tableViewSourceImpl = tableViewSource;
		// An explicit clear needs no key selector: an app-declared axis lit on a column was never
		// installed by this control, yet clearing that column must still remove it. Any other clear
		// removes only the axis this control installed.
		bool clearsAppAxes = direction == SortDirection.None && explicitClear;
		if (clearsAppAxes
			? keySelector is null && tableViewSourceImpl.ActiveSortAxisInfos().Count == 0
			: keySelector is null || (direction == SortDirection.None && string.IsNullOrEmpty(sortAxisToken)))
		{
			return false;
		}
		// The control is the last writer here, so its axis becomes the only one. Without this an
		// app-declared fluent Sort axis would survive alongside it and, being declared earlier, would
		// outrank it as the primary sort while the chevron advertised this column.
		using var reconcileGuard = BeginControlInitiatedSortScope(tipGeneration);
		if (clearsAppAxes)
		{
			// An explicit clear means "nothing is sorted", so it clears EVERY axis, not just the one
			// this control installed. Clearing only its own token would leave an app-declared fluent
			// axis ordering the rows while every chevron said the sort was off.
			tableViewSourceImpl.ClearSort();
		}
		else if (direction == SortDirection.None)
		{
			tableViewSourceImpl.ClearSort(sortAxisToken);
		}
		else
		{
			// Replacing rather than adding: re-sorting a column must swap its axis, not stack a
			// second one on top of the first.
			try
			{
				tableViewSourceImpl.SortReplacing(m_tableViewSourceSort.AxisToken, sortAxisToken, keySelector!, axisSortMemberPath, direction);
			}
			catch (Exception)
			{
				// The axis is declared before the projection fails; keep owning it so the next sort
				// replaces it instead of stacking on an orphan.
				m_tableViewSourceSort.AxisToken = sortAxisToken;
				throw;
			}
			tableViewSourceImpl.ClearSortsExcept(sortAxisToken);
		}

		if (direction == SortDirection.None)
		{
			m_tableViewSourceSort.Clear();
		}
		else
		{
			m_tableViewSourceSort.AxisToken = sortAxisToken;
		}

		// The verb mutated the active source in place, so the source lifetime is unchanged - just push
		// its rewritten projection into the repeater.
		RefreshRowsPipeline();
		UpdateEmptyState();
		return true;
	}

	internal bool PurgeColumnFromSortState(TableViewColumn? removedColumn)
	{
		// Clear the leaving column's own SortDirection DP explicitly. The walk below drops it from
		// m_sortedColumns, but the DP on the instance is sticky - a consumer who kept a strong
		// reference and re-attached the column to another TableView would carry a phantom sort state
		// across with it.
		if (removedColumn is not null)
		{
			removedColumn.SetSortStateInternal(SortDirection.None);
		}

		bool removedAny = false;
		int it = 0;
		while (it != m_sortedColumns.Count)
		{
			var column = m_sortedColumns[it];
			bool isDead = (column is null);
			bool isRemoved = (removedColumn is not null && column == removedColumn);
			if (isDead || isRemoved)
			{
				m_sortedColumns.RemoveAt(it);
				removedAny = true;
			}
			else
			{
				++it;
			}
		}

		return removedAny;
	}

	private int FindEntryIndexForDataItem(object? item)
	{
		if (item is null || m_rowsItemsSourceView is null)
		{
			return -1;
		}

		// Identity, not equality: the projection holds the app's own objects, and a value-based match
		// would re-select the wrong row whenever two rows compare equal.
		// try_as<::IUnknown>() never fails for a non-null item, and comparing IUnknown pointers is ReferenceEquals.
		object target = item;

		var count = m_rowsItemsSourceView.Count;
		for (int i = 0; i < count; ++i)
		{
			if (m_rowsItemsSourceView.GetAt(i) is { } candidate &&
				ReferenceEquals(candidate, target))
			{
				return i;
			}
		}

		return -1;
	}

	private void RecomputeSortDPsAndRaiseInternal(
		TableViewColumn? trigger, bool measureTip = true, bool explicitClear = false)
	{
		// Stale-deferred-clear guard. A null trigger means "clear", and every legitimate null-trigger
		// caller - ClearSort, SortByColumn(col, None), ToggleSortDirection cycling to None - clears
		// m_sortedColumns BEFORE calling this. So a null trigger arriving with a non-empty
		// m_sortedColumns can only be a clear that was queued earlier and overtaken by a newer sort;
		// running it would tear down the sort the app just asked for. If a future edit adds a
		// null-trigger path that does not clear first, this guard will silently swallow a legitimate
		// clear - update the invariant before adding one.
		if (trigger is null && m_sortedColumns.Count != 0)
		{
			return;
		}

		var invalidatedSort = !measureTip && m_tip.operation == TableViewTip.Operation.Sort
			? m_tip.operationGeneration : 0;
		var tipGeneration = BeginTipOperation(TableViewTip.Operation.Sort, measureTip);
		m_controlSortPublishedGeneration = 0;
		bool completed = false;
		TableViewTip.Fault? failure = null;
		using var tipCompletion = Disposable.Create(() =>
		{
			if (measureTip && !completed)
			{
				FailTipOperation(tipGeneration, TableViewTip.Stage.Sort, failure);
			}
			else if (measureTip && m_controlSortPublishedGeneration == tipGeneration)
			{
				CompleteTipOperation(tipGeneration);
			}
			else if (measureTip)
			{
				TableViewTip.IgnoreOperation(m_tip, tipGeneration, TableViewTip.IgnoreReason.DeferredOrUnproven);
			}
			EndTipOperation();
		});

		try
		{
			// Reshapes through whichever source is active. A cancelled Sorting never reaches this point,
			// so anything that does gets both the reshape and the published state.
			var requestedDirection = trigger is not null ? trigger.SortDirection : SortDirection.None;

			// Selection is index-based, and a re-sort moves every index. Capture the item, then re-find it
			// afterwards so the selection follows the row rather than the slot.
			var preservedSelection = SelectedItemInternal();
			bool reshaped = SyncTableViewSourceSort(trigger, requestedDirection, tipGeneration, explicitClear || trigger is not null);
			if (reshaped && invalidatedSort != 0)
			{
				TableViewTip.IgnoreOperation(m_tip, invalidatedSort, TableViewTip.IgnoreReason.Stale);
			}
			if (!reshaped && tipGeneration == m_tip.operationGeneration)
			{
				TableViewTip.IgnoreOperation(m_tip, tipGeneration, TableViewTip.IgnoreReason.Stale);
			}

			if (reshaped && preservedSelection is not null)
			{
				// A row that fell out of the projection surfaces as a real deselect, which is the honest
				// answer - the selected item is no longer displayed.
				ApplySelection(FindEntryIndexForDataItem(preservedSelection));
			}

			// The app's sort remains on the source, so an implicit clear is not reported as a clear.
			bool keptSourceSort = false;
			if (trigger is null && !explicitClear && !m_activeSourceReleased)
			{
				if (ShapingSourceInternal() is { } source)
				{
					keptSourceSort = source.ActiveSortAxisInfos().Count != 0;
				}
			}

			if (Sorted is not null && !keptSourceSort)
			{
				var appliedDirection = trigger is not null ? trigger.SortDirection : SortDirection.None;
				var args = new TableViewSortedEventArgs(trigger, appliedDirection);
				try
				{
					Sorted?.Invoke(this, args);
				}
				catch (Exception ex)
				{
					TableViewTip.RethrowAppFault(ex, TableViewTip.AppFaultKind.Callback);
					throw;
				}
			}

			if (!keptSourceSort && AutomationPeer.ListenerExists(AutomationPeer.NotificationEvent))
			{
				// A programmatic or header-driven re-sort has no input event behind it, so without an
				// announcement a screen-reader user has no way to learn the order changed.
				if (trigger is not null)
				{
					// The realized header peer also names template headers, which carry no string.
					string header = "";
					if (GetRealizedColumnHeaderPeer(this, trigger) is { } headerPeer)
					{
						header = headerPeer.GetName();
					}
					if (string.IsNullOrEmpty(header))
					{
						header = TableViewDetails.GetColumnHeaderText(trigger);
					}

					switch (trigger.SortDirection)
					{
						case SortDirection.Ascending:
							AnnounceSortChange(StringUtil.FormatString(
								LocalizedOrFallback(ResourceAccessor.SR_TableViewSortedAscending, "Sorted by %1!s! ascending."), header));
							break;
						case SortDirection.Descending:
							AnnounceSortChange(StringUtil.FormatString(
								LocalizedOrFallback(ResourceAccessor.SR_TableViewSortedDescending, "Sorted by %1!s! descending."), header));
							break;
						case SortDirection.None:
						default:
							AnnounceSortChange(StringUtil.FormatString(
								LocalizedOrFallback(ResourceAccessor.SR_TableViewSortCleared, "Sorting cleared for %1!s!."), header));
							break;
					}
				}
				else
				{
					AnnounceSortChange(LocalizedOrFallback(ResourceAccessor.SR_TableViewSortClearedAll, "All sorting cleared."));
				}
			}
		}
		catch (Exception ex)
		{
			// App code's exceptions (the collection, its contract, a sort callout) carry their kind.
			failure = TableViewTip.CurrentFault(ex);
			throw;
		}

		// The chevrons are already current: SetSortStateInternal republished them through
		// RefreshSortIndicators as each column's DP was written.
		completed = true;

		// An app-declared axis that survived the clear re-lights its column (for example a moved one).
		if (trigger is null && !explicitClear)
		{
			QueueReconcileSortStateWithSource();
		}
	}

	private void QueueReconcileSortStateWithSource()
	{
		// Deferred, never inline: the notification that brings us here is raised from inside the
		// engine's own reshape, and reconciling clears an axis, which reshapes again.
		if (m_sortReconcileQueued)
		{
			return;
		}
		m_sortReconcileQueued = true;

		var weakThis = new WeakReference<TableView>(this);
		if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is { } queue)
		{
			_ = queue.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					// An opt-out replayed in the meantime may have reconciled already.
					if (strongThis.m_sortReconcileQueued)
					{
						strongThis.m_sortReconcileQueued = false;
						strongThis.ReconcileSortStateWithSource();
					}
				}
			});
		}
		else
		{
			m_sortReconcileQueued = false;
		}
	}

	private void ReconcileSortStateWithSource()
	{
		var tableViewSource = ShapingSourceInternal();
		// Released on Unloaded: another table may own it now, and Loaded reconciles on reclaim.
		if (tableViewSource is null || m_activeSourceReleased)
		{
			return;
		}
		bool rerankCustomSort = m_rerankCustomSortOnReconcile;
		m_rerankCustomSortOnReconcile = false;

		var ownToken = m_tableViewSourceSort.AxisToken;
		var tableViewSourceImpl = tableViewSource;
		var axes = tableViewSourceImpl.ActiveSortAxisInfos();

		// A comparer axis another table (or this one, before an ItemsSource change) installed ranks
		// through an adapter nothing here can update, so it would order rows on stale ranks.
		bool clearedForeignComparerAxis = false;
		foreach (var axis in axes)
		{
			if (axis.AxisToken != ownToken && string.IsNullOrEmpty(axis.SortMemberPath) &&
				axis.AxisToken.StartsWith("column:", StringComparison.Ordinal))
			{
				using var reconcileGuard = BeginControlInitiatedSortScope();
				tableViewSourceImpl.ClearSort(axis.AxisToken);
				clearedForeignComparerAxis = true;
			}
		}
		if (clearedForeignComparerAxis)
		{
			axes = tableViewSourceImpl.ActiveSortAxisInfos();
		}

		bool ownAxisPresent = false;
		// Infos arrive in precedence order, so the first foreign axis is the one that outranks ours
		// and is therefore the only one a single-column chevron could honestly describe.
		TableViewSource.ActiveSortAxisInfo? primaryForeignAxis = null;
		foreach (var axis in axes)
		{
			if (!string.IsNullOrEmpty(ownToken) && axis.AxisToken == ownToken)
			{
				ownAxisPresent = true;
			}
			else if (primaryForeignAxis is null)
			{
				primaryForeignAxis = axis;
			}
		}

		if (ownAxisPresent && primaryForeignAxis is null)
		{
			// The control's axis is still the only sort: chevrons already describe it.
			if (rerankCustomSort)
			{
				RerankCustomSortAfterReclaim();
			}
			return;
		}

		if (primaryForeignAxis is not null && ownAxisPresent)
		{
			// The app declared a sort while ours was installed. Last writer wins, so ours goes: left
			// alone it would keep whichever axis was declared FIRST as the primary sort, which is the
			// hidden-precedence case this reconciliation exists to prevent.
			CancelPendingSortRestore();
			using var reconcileGuard = BeginControlInitiatedSortScope();
			tableViewSourceImpl.ClearSort(ownToken);
		}

		// A path-declared axis names the property it sorts on, so the chevron can follow the app's own
		// sort instead of going dark. A key-selector axis names nothing, and guessing which column it
		// meant would be worse than showing no chevron at all.
		TableViewColumn? matchedColumn = null;
		if (primaryForeignAxis is not null && !string.IsNullOrEmpty(primaryForeignAxis.Value.SortMemberPath))
		{
			if (Columns is { } columns)
			{
				foreach (var column in columns)
				{
					if (column is not null && column.GetSortMemberPathCore() == primaryForeignAxis.Value.SortMemberPath)
					{
						matchedColumn = column;
						break;
					}
				}
			}
		}

		// matchedColumn can only be set from a non-null primaryForeignAxis, but that coupling is not
		// something static analysis can prove at each use. Reading the direction once here keeps the
		// dereference next to the null check it depends on.
		var foreignDirection = primaryForeignAxis is not null
			? primaryForeignAxis.Value.Direction
			: SortDirection.None;

		// No axis of ours, nothing a column can describe and no chevron lit. Reached on every shaping
		// notification - a filter or group change also lands here - so it must not raise a spurious Sorted.
		if (string.IsNullOrEmpty(ownToken) && matchedColumn is null && m_sortedColumns.Count == 0)
		{
			return;
		}

		// Already reconciled to exactly this state. Reached whenever an unrelated shaping change - a
		// filter or a group - renotifies while an app-owned sort is standing; re-raising Sorted for it
		// would report a sort change that did not happen.
		if (string.IsNullOrEmpty(ownToken) && matchedColumn is not null && m_sortedColumns.Count == 1)
		{
			if (m_sortedColumns[0] is { } lit &&
				lit == matchedColumn && lit.SortDirection == foreignDirection)
			{
				return;
			}
		}

		// Silent, like ResetSortStateForNewItemsSource: the app already owns the ordering by the time
		// we get here, so there is nothing to cancel and no reshape of our own to perform. Sorted is
		// still raised so an app tracking sort state sees the chevrons move.
		CancelPendingSortRestore();
		foreach (var weakColumn in m_sortedColumns)
		{
			if (weakColumn is { } column && column != matchedColumn)
			{
				column.SetSortStateInternal(SortDirection.None);
			}
		}
		m_sortedColumns.Clear();

		// m_tableViewSourceSort stays cleared even when a column is lit: the axis belongs to the app,
		// and recording it as ours would make the next control-initiated sort try to replace an axis
		// it never installed.
		m_tableViewSourceSort.Clear();

		if (matchedColumn is not null)
		{
			matchedColumn.SetSortStateInternal(foreignDirection);
			m_sortedColumns.Add(matchedColumn);
		}

		// Raised directly rather than through RecomputeSortDPsAndRaiseInternal: that path would reshape
		// through SyncTableViewSourceSort and re-declare the axis as the control's own, undoing the
		// app's ownership - and its stale-clear guard would swallow the event outright now that
		// m_sortedColumns can be non-empty here.
		if (Sorted is not null)
		{
			var args = new TableViewSortedEventArgs(
				matchedColumn,
				matchedColumn is not null ? foreignDirection : SortDirection.None);
			Sorted?.Invoke(this, args);
		}
	}

	private void QueueClearSortAfterColumnRemoval()
	{
		if (m_clearSortAfterColumnRemovalQueued)
		{
			return;
		}
		m_clearSortAfterColumnRemovalQueued = true;

		var weakThis = new WeakReference<TableView>(this);
		if (Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is { } queue)
		{
			_ = queue.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_clearSortAfterColumnRemovalQueued = false;
					strongThis.m_sortedColumnPendingRemoval = null;
					// RecomputeSortDPsAndRaiseInternal's stale-clear guard drops this if the app
					// applied a new sort on a different column in the meantime.
					strongThis.RecomputeSortDPsAndRaiseInternal(null, false);
				}
			});
		}
		else
		{
			m_clearSortAfterColumnRemovalQueued = false;
			m_sortedColumnPendingRemoval = null;
		}
	}

	private void PurgeLeavingColumnFromSortState(TableViewColumn column)
	{
		bool wasSorted = m_sortedColumns.Contains(column);
		var direction = column.SortDirection;
		if (!PurgeColumnFromSortState(column))
		{
			return;
		}

		if (wasSorted && direction != SortDirection.None)
		{
			m_sortedColumnPendingRemoval = new WeakReference<TableViewColumn>(column);
			m_sortedColumnPendingRemovalDirection = direction;
		}
		QueueClearSortAfterColumnRemoval();
	}

	private void RestoreSortOfMovedColumn(TableViewColumn column)
	{
		if (!m_clearSortAfterColumnRemovalQueued || column is null ||
			m_sortedColumnPendingRemoval is null || !m_sortedColumnPendingRemoval.TryGetTarget(out var pending) || pending != column)
		{
			return;
		}
		m_sortedColumnPendingRemoval = null;

		// A sort applied since the removal replaced this one. A column that opted out while detached
		// never told the table; one lit only by the app's source sort keeps it, as when attached.
		if (m_sortedColumns.Count != 0 || (!CanSortColumn(column) && !IsSortLitOnlyByApp()))
		{
			return;
		}

		column.SetSortStateInternal(m_sortedColumnPendingRemovalDirection);
		m_sortedColumns.Add(column);
	}

	private void AnnounceSortChange(string announcement)
	{
		if (string.IsNullOrEmpty(announcement))
		{
			return;
		}

		if (!AutomationPeer.ListenerExists(AutomationPeer.NotificationEvent))
		{
			return;
		}

		try
		{
			// RaiseNotificationEvent rather than a live region: there is no announcement-only element
			// in the template, and a notification carries its own text without one.
			if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
			{
				peer.RaiseNotificationEvent(
					AutomationNotificationKind.ActionCompleted,
					AutomationNotificationProcessing.MostRecent,
					announcement,
					"TableViewSortChanged");
			}
		}
		catch (Exception)
		{
			// No peer, or UIA is unavailable on this host: the sort itself already applied.
		}
	}
}
