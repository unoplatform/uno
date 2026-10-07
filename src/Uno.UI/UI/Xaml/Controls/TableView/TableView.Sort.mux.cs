// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Sort.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

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
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

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
		ResetCustomSort();
	}
}

partial class TableView
{
	// TODO Uno: duplicated anon-namespace helper (TableView_Grouping.cpp defines the same function; see
	// LocalizedOrFallback in TableView.Grouping.mux.cs).
	private static string SortLocalizedOrFallback(string resourceName, string fallback)
	{
		try
		{
			if (ResourceAccessor.GetLocalizedStringResource(resourceName) is { } resolved && !string.IsNullOrEmpty(resolved))
			{
				return resolved;
			}
		}
		catch (Exception)
		{
		}

		return fallback;
	}

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

	private bool IsSortClearStillValid()
	{
		return m_sortedColumns.Count != 0;
	}

	private bool IsSortRequestStillValid(TableViewColumn? column)
	{
		if (column is null || !CanSortColumn(column))
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

	public bool SortByColumn(TableViewColumn column, SortDirection direction)
	{
		if (!CanSortColumn(column))
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

		// An open editor is showing a row at an index that is about to move. Close it first; if it
		// cannot close synchronously, replay this request once it does.
		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending && !m_isApplyingCoalescedEditReshape)
			{
				QueueCoalescedEditReshape(() =>
				{
					SortByColumn(column, direction);
				});
			}
			return false;
		}
		if (!IsSortRequestStillValid(column))
		{
			return false;
		}

		if (RaiseSortingAndCheckCanceled(column, direction))
		{
			return false;
		}

		if (!IsSortRequestStillValid(column))
		{
			return false;
		}

		ApplySingleColumnSortState(column, direction);
		RecomputeSortDPsAndRaiseInternal(column);
		DrainCoalescedEditReshape();
		return true;
	}

	public bool ToggleSortDirection(TableViewColumn column)
	{
		if (!CanSortColumn(column))
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

		if (!IsSortRequestStillValid(column))
		{
			return false;
		}

		ApplySingleColumnSortState(column, next);
		RecomputeSortDPsAndRaiseInternal(column);
		DrainCoalescedEditReshape();
		return true;
	}

	public bool ClearSort()
	{
		if (m_sortedColumns.Count == 0)
		{
			return false;
		}

		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending && !m_isApplyingCoalescedEditReshape)
			{
				QueueCoalescedEditReshape(() =>
				{
					ClearSort();
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
		RecomputeSortDPsAndRaiseInternal(null);
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
		try
		{
			Sorting?.Invoke(this, args);
		}
		catch (Exception)
		{
		}

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
		SortDirection direction)
	{
		var tableViewSource = ShapingSourceInternal();
		if (tableViewSource is null)
		{
			return false;
		}

		TableViewKeySelector? keySelector = null;
		string sortAxisToken = "";
		// What this axis sorts on, purely descriptive. Stays empty for a CustomSortComparer: the
		// ordering is the comparer's, not the path's, so nothing else may claim the axis sorts by a
		// property.
		string axisSortMemberPath = "";
		if (trigger is not null)
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

					if (m_tableViewSourceSort.CustomSortState is { } rankAdapter)
					{
						ShapingHelpers.PairwiseComparer comparer =
							(object? a, object? b) =>
							{
								return customComparer.Compare(a, b);
							};
						rankAdapter.Rank(comparer, rows);
					}
				}
			}
			else
			{
				// Projected dispatch - see the note in HasResolvedSortMemberPath.
				var sortMemberPath = trigger.GetSortMemberPathCore();
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
			// Clear-all: reuse whatever axis is currently installed so it can be removed by token.
			keySelector = m_tableViewSourceSort.KeySelector;
			sortAxisToken = m_tableViewSourceSort.AxisToken;
			if (direction == SortDirection.None)
			{
				m_tableViewSourceSort.ResetCustomSort();
			}
		}

		if (keySelector is null)
		{
			return false;
		}

		// Limitation: this reshapes on explicit sort and on collection notifications only.
		// INotifyPropertyChanged-only mutations of the active sort key do not live re-sort until the
		// next collection change.
		var tableViewSourceImpl = tableViewSource;
		// The control is the last writer here, so its axis becomes the only one. Without this an
		// app-declared fluent Sort axis would survive alongside it and, being declared earlier, would
		// outrank it as the primary sort while the chevron advertised this column.
		using var reconcileGuard = BeginControlInitiatedSortScope();
		if (direction == SortDirection.None)
		{
			// A control-initiated clear means "nothing is sorted", so it clears EVERY axis, not just
			// the one this control installed. Clearing only its own token would leave an app-declared
			// fluent axis ordering the rows while every chevron said the sort was off.
			tableViewSourceImpl.ClearSort();
		}
		else
		{
			// Replacing rather than adding: re-sorting a column must swap its axis, not stack a
			// second one on top of the first.
			tableViewSourceImpl.SortReplacing(m_tableViewSourceSort.AxisToken, sortAxisToken, keySelector, axisSortMemberPath, direction);
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
		// TODO Uno: try_as<::IUnknown>() succeeds for any non-null object, and the IUnknown pointer
		// comparison is ReferenceEquals.
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
		TableViewColumn? trigger)
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

		// Reshapes through whichever source is active. A cancelled Sorting never reaches this point,
		// so anything that does gets both the reshape and the published state.
		var requestedDirection = trigger is not null ? trigger.SortDirection : SortDirection.None;

		// Selection is index-based, and a re-sort moves every index. Capture the item, then re-find it
		// afterwards so the selection follows the row rather than the slot.
		var preservedSelection = SelectedItemInternal();
		bool reshaped = SyncTableViewSourceSort(trigger, requestedDirection);

		if (reshaped && preservedSelection is not null)
		{
			// A row that fell out of the projection surfaces as a real deselect, which is the honest
			// answer - the selected item is no longer displayed.
			ApplySelection(FindEntryIndexForDataItem(preservedSelection));
		}

		if (Sorted is not null)
		{
			var appliedDirection = trigger is not null ? trigger.SortDirection : SortDirection.None;
			var args = new TableViewSortedEventArgs(trigger, appliedDirection);
			try
			{
				Sorted?.Invoke(this, args);
			}
			catch (Exception)
			{
			}
		}

		// A programmatic or header-driven re-sort has no input event behind it, so without an
		// announcement a screen-reader user has no way to learn the order changed.
		if (trigger is not null)
		{
			string header = "";
			if (trigger.Header is { } headerContent)
			{
				// TODO Uno: IPropertyValue projection; Type() == PropertyType::String becomes a type check
				// through ValueConversionHelpers.GetPropertyType.
				if (ValueConversionHelpers.GetPropertyType(headerContent.GetType()) == PropertyType.String)
				{
					header = (string)headerContent;
				}
			}

			switch (trigger.SortDirection)
			{
				case SortDirection.Ascending:
					AnnounceSortChange(FormatOrEmpty(
						SortLocalizedOrFallback(ResourceAccessor.SR_TableViewSortedAscending, "Sorted by %1!s! ascending."), header));
					break;
				case SortDirection.Descending:
					AnnounceSortChange(FormatOrEmpty(
						SortLocalizedOrFallback(ResourceAccessor.SR_TableViewSortedDescending, "Sorted by %1!s! descending."), header));
					break;
				case SortDirection.None:
				default:
					AnnounceSortChange(FormatOrEmpty(
						SortLocalizedOrFallback(ResourceAccessor.SR_TableViewSortCleared, "Sorting cleared for %1!s!."), header));
					break;
			}
		}
		else
		{
			AnnounceSortChange(SortLocalizedOrFallback(ResourceAccessor.SR_TableViewSortClearedAll, "All sorting cleared."));
		}

		// The chevrons are already current: SetSortStateInternal republished them through
		// RefreshSortIndicators as each column's DP was written.

		// TODO Uno: StringUtil::FormatString returns an empty string when FormatMessage fails, which
		// AnnounceSortChange then skips; the managed FormatString throws instead, so map that back.
		static string FormatOrEmpty(string format, string arg)
		{
			try
			{
				return StringUtil.FormatString(format, arg);
			}
			catch (Exception)
			{
				return "";
			}
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
					strongThis.m_sortReconcileQueued = false;
					strongThis.ReconcileSortStateWithSource();
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
		if (tableViewSource is null)
		{
			return;
		}

		var ownToken = m_tableViewSourceSort.AxisToken;
		var tableViewSourceImpl = tableViewSource;
		var axes = tableViewSourceImpl.ActiveSortAxisInfos();

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
			return;
		}

		if (primaryForeignAxis is null && string.IsNullOrEmpty(ownToken) && m_sortedColumns.Count == 0)
		{
			// Nothing sorted anywhere and no chevron lit. Reached on every shaping notification -
			// a filter or group change also lands here - so it must not raise a spurious Sorted.
			return;
		}

		if (primaryForeignAxis is not null && ownAxisPresent)
		{
			// The app declared a sort while ours was installed. Last writer wins, so ours goes: left
			// alone it would keep whichever axis was declared FIRST as the primary sort, which is the
			// hidden-precedence case this reconciliation exists to prevent.
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
			try
			{
				Sorted?.Invoke(this, args);
			}
			catch (Exception)
			{
			}
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
					// RecomputeSortDPsAndRaiseInternal's stale-clear guard drops this if the app
					// applied a new sort on a different column in the meantime.
					strongThis.RecomputeSortDPsAndRaiseInternal(null);
				}
			});
		}
		else
		{
			m_clearSortAfterColumnRemovalQueued = false;
		}
	}

	private void AnnounceSortChange(string announcement)
	{
		if (string.IsNullOrEmpty(announcement))
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
