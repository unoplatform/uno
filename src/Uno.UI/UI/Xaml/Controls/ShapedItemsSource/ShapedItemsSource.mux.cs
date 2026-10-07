// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedItemsSource.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Interop;
using Uno.Disposables;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using NotifyCollectionChangedEventArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using NotifyCollectionChangedEventHandler = System.Collections.Specialized.NotifyCollectionChangedEventHandler;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class ShapedItemsSource
{
	private static IVectorChangedEventArgs? TryAsVectorChangedArgs(
		IVectorChangedEventArgs args)
	{
		return args;
	}

	private static IVectorChangedEventArgs? TryAsVectorChangedArgs(
		object? args)
	{
		return args as IVectorChangedEventArgs;
	}

	private static void LogIdentityProjectionDisabled(string? reason)
	{
#if DEBUG
		TVDiag.DbgLogF("[ShapedItemsSource] Stable identity is required; disabling shaped identity projection (%ls).\n", reason ?? "unspecified reason");
#endif
	}

	public ShapedItemsSource(object? source)
	{
		m_source = source;
		m_rows = new ObservableVector<object?>();
	}

#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
	// TODO Uno: Investigate potential leak: the app's source collection keeps its CollectionChanged /
	// VectorChanged handler (which holds only a WeakReference to this object) until the owner revokes it.

	// Original destructor logic (not executed):
	// ShapedItemsSource::~ShapedItemsSource()
	// {
	//     UnsubscribeFromSourceCollectionChanges();
	// }
#endif

	public partial void Start()
	{
		SubscribeToSourceCollectionChanges();
		Refresh();
	}

	private partial void BeginShapingBatch() => ++m_shapingBatchDepth;

	public partial DeferRefreshScope DeferRefresh()
	{
		BeginShapingBatch();
		return new DeferRefreshScope(this);
	}

	private partial void EndShapingBatch()
	{
		MUX_ASSERT(m_shapingBatchDepth > 0);
		if (m_shapingBatchDepth == 0 || --m_shapingBatchDepth > 0)
		{
			return;
		}

		var rebuild = m_shapingBatchHasRefresh;
		var shapingChange = m_shapingBatchHasShapingChange;
		m_shapingBatchHasRefresh = false;
		m_shapingBatchHasShapingChange = false;

		// A pending identity-selector change is the stronger of the two: it invalidates the whole
		// projection, and the spec diff that ApplyShapingChange would commit is still owed either
		// way, so commit it first and let the rebuild publish the result.
		if (shapingChange)
		{
			ApplyShapingChange();
		}
		if (rebuild)
		{
			Refresh();
		}
	}

	public partial void SetFilter(ShapingHelpers.Predicate? predicate)
	{
		m_pipeline.SetFilter(predicate);
		ApplyShapingChange();
	}

	public partial void SetFilter(string axisToken, ShapingHelpers.Predicate? predicate)
	{
		m_pipeline.SetFilter(axisToken, predicate);
		ApplyShapingChange();
	}

	public partial void ClearFilter()
	{
		m_pipeline.ClearFilter();
		ApplyShapingChange();
	}

	public partial void ClearFilter(string axisToken)
	{
		m_pipeline.ClearFilter(axisToken);
		ApplyShapingChange();
	}

	public partial void SetGroup(
		ShapingHelpers.KeySelector? key,
		RowIdentity.IdentitySelector? groupIdentitySelector)
	{
		m_groupSelector = key;
		m_groupIdentitySelector = groupIdentitySelector;
		m_pipeline.MarkGroupVerb(key);
		ApplyShapingChange();
	}

	public partial void ClearGroup()
	{
		m_groupSelector = null;
		m_groupIdentitySelector = null;
		m_pipeline.ClearGroupVerb();
		ApplyShapingChange();
	}

	public partial void SetSort(
		string previousAxisToken,
		string axisToken,
		ShapingHelpers.KeySelector? key,
		object? keyIdentity,
		string sortMemberPath,
		SortDirection direction)
	{
		m_pipeline.SetSort(previousAxisToken, axisToken, key, keyIdentity, sortMemberPath, direction);
		ApplyShapingChange();
	}

	public partial void ClearSorts()
	{
		m_pipeline.ClearSorts();
		ApplyShapingChange();
	}

	public partial void ClearSort(string axisToken)
	{
		m_pipeline.ClearSort(axisToken);
		ApplyShapingChange();
	}

	public partial void ClearSortsExcept(string axisToken)
	{
		m_pipeline.ClearSortsExcept(axisToken);
		ApplyShapingChange();
	}

	public partial List<ActiveSortAxisInfo> ActiveSortAxisInfos()
	{
		List<ActiveSortAxisInfo> infos = new();
		foreach (var axis in m_pipeline.ActiveSortAxes(-1, -1))
		{
			infos.Add(new ActiveSortAxisInfo { AxisToken = axis.AxisToken, SortMemberPath = axis.SortMemberPath, Direction = axis.Direction });
		}
		return infos;
	}

	private partial void ApplyShapingChange()
	{
		if (m_shapingBatchDepth > 0)
		{
			// Deliberately do NOT commit the spec here: the pipeline diffs against the last
			// committed spec, so deferring the commit is what lets the whole batch read as one delta.
			m_shapingBatchHasShapingChange = true;
			return;
		}

		// Commit unconditionally, even when the in-place path is not taken: the committed spec is
		// the baseline the NEXT verb diffs against, so skipping it would make that diff report a
		// change that has already been applied.
		var delta = m_pipeline.CommitSpec();

		if (delta.IsNoOp())
		{
			// Re-declaring the identical shape. The projection already satisfies it, and a rebuild
			// would fire a Reset that drops every realized row for nothing. Reachable only from a
			// Clear* verb against a shape that has nothing to clear — every declaration re-mints its
			// description id, so a re-declaration always reads as a change.
			return;
		}

		if (TryApplyShapingDeltaInPlace(delta))
		{
			RaiseShapingChanged(true /* reorderOnly */);
			return;
		}

		Refresh();
		RaiseShapingChanged(false /* reorderOnly */);
	}

	// Scope: UNGROUPED sort-only changes. Grouped sort still rebuilds through the group adapter,
	// because a sort declared before the group verb reorders the GROUPS, which layer 1's
	// within-bucket sort cannot express. Doing that incrementally would mean teaching the adapter to
	// consume a ReBucket / ReSortWithinBuckets delta, which this does not attempt.
	//
	// What it saves is model-side work: re-materializing the source, re-running the filter predicate
	// over every row, and constructing a new ItemsSourceView and RowMetadataProvider. It does NOT
	// avoid re-querying row identity — the identity/index map is ordinal, so a re-order invalidates
	// it and RebuildFlatRowIdentityTracking re-projects identity over every row below. It
	// also does NOT preserve realized containers — ReplaceAll is still a Reset, so the repeater
	// re-realizes exactly as it would after a Refresh. Preserving containers across a re-order would
	// require emitting Move notifications instead.
	private partial bool TryApplyShapingDeltaInPlace(ShapingHelpers.ShapingDelta delta)
	{
		// Only a pure re-order is safe to do against rows already in hand. Anything touching
		// membership or bucketing needs the source, the group cache and the projection swap that
		// only Refresh performs.
		if (delta.RequiredWork != ShapingHelpers.ShapingWork.ReSortWithinBuckets)
		{
			return false;
		}

		// A grouped projection is rebuilt through the group adapter, and sorts declared before the
		// group verb reorder the GROUPS — which layer 1's within-bucket sort cannot express.
		if (m_groupSelector is not null || m_projectedAsGrouped)
		{
			return false;
		}

		if (m_rows is null || !m_shapingState.HasProjection || m_shapingState.IsGrouped)
		{
			return false;
		}

		// A rebuild is already going to run and will subsume this change.
		if (m_isRefreshing || m_isApplyingIncrementalChange || m_pendingRefresh)
		{
			return false;
		}

		// An unshaped mirror is not a shaped projection at all; re-sorting it in place would apply
		// shaping that Refresh deliberately refuses to apply. The m_shapingState.HasProjection test
		// above already rejects that case, and identity is never the blocker now --
		// EffectiveIdentitySelector always yields one -- so no further gate is needed here.

		// Prove the retained state still describes the live projection instead of trusting that every
		// mutation site remembered to invalidate it. The invalidation calls are the cheap first line
		// of defence; this is the one that makes a missed call — including from a splice site added
		// later — degrade to a full rebuild rather than silently re-sort a membership the projection
		// no longer has. O(n) reference comparisons against an O(n log n) sort that follows.
		if (m_shapingState.Items.Count != m_rows.Count)
		{
			return false;
		}
		for (var i = 0; i < m_rows.Count; ++i)
		{
			// TODO Uno: IInspectable operator!= compares COM identity, i.e. reference equality.
			if (!ReferenceEquals(m_shapingState.Items[i], m_rows[i]))
			{
				return false;
			}
		}


		// Re-seats Items on FilteredSource (source order) and re-sorts, so ties break exactly as a
		// full reshape of the same spec would.
		ShapingHelpers.Reshape(m_shapingState, m_pipeline.CommittedSpec(), delta);

		m_rows.ReplaceAll(m_shapingState.Items);
		RebuildFlatRowIdentityTracking(m_shapingState.Items);
		return true;
	}

	private partial void InvalidateShapingState()
	{
		m_shapingState.HasProjection = false;
		m_shapingState.FilteredSource.Clear();
		m_shapingState.Items.Clear();
		m_shapingState.Buckets.Clear();
		m_shapingState.IsGrouped = false;
	}

	private partial void SubscribeToSourceCollectionChanges()
	{
		UnsubscribeFromSourceCollectionChanges();

		// Classify the source once, here, where it is bound. Every later indexed read, count and
		// observability check reads off this resolution instead of re-probing.
		m_sourceAccessor = new ShapingHelpers.CollectionAccessor(m_source);

		if (m_source is null)
		{
			return;
		}

		WeakReference<ShapedItemsSource> weakThis = new(this);
		// TODO Uno: the C++ generic lambda applyVectorChange is instantiated once per delegate type; C# needs one lambda per type.
		VectorChangedEventHandler<object?> applyVectorChange = (s, args) =>
		{
			if (weakThis.TryGetTarget(out var strongThis))
			{
				if (TryAsVectorChangedArgs(args) is { } vectorArgs)
				{
					strongThis.OnSourceVectorChanged(vectorArgs);
				}
				else
				{
					strongThis.OnSourceCollectionChanged();
				}
			}
		};
		BindableVectorChangedEventHandler applyBindableVectorChange = (s, args) =>
		{
			if (weakThis.TryGetTarget(out var strongThis))
			{
				if (TryAsVectorChangedArgs(args) is { } vectorArgs)
				{
					strongThis.OnSourceVectorChanged(vectorArgs);
				}
				else
				{
					strongThis.OnSourceCollectionChanged();
				}
			}
		};

		if (m_sourceAccessor.AsNotifyCollectionChanged() is { } collection)
		{
			// INotifyCollectionChanged carries per-change details (.NET ObservableCollection<T>),
			// so route it through the incremental fast-path.
			NotifyCollectionChangedEventHandler handler = (s, args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnSourceCollectionChanged(args);
				}
			};
			collection.CollectionChanged += handler;
			m_sourceCollectionChangedRevoker.Disposable = Disposable.Create(() => collection.CollectionChanged -= handler);
		}
		else if (m_sourceAccessor.AsObservableVector() is { } vector)
		{
			// VectorChanged carries a single index + verb. For flat 1:1 projections, keep this
			// incremental instead of collapsing to ReplaceAll.
			vector.VectorChanged += applyVectorChange;
			m_sourceVectorChangedRevoker.Disposable = Disposable.Create(() => vector.VectorChanged -= applyVectorChange);
		}
		else if (m_sourceAccessor.AsBindableObservableVector() is { } bindableVector)
		{
			bindableVector.VectorChanged += applyBindableVectorChange;
			m_sourceBindableVectorChangedRevoker.Disposable = Disposable.Create(() => bindableVector.VectorChanged -= applyBindableVectorChange);
		}
	}

	private partial void UnsubscribeFromSourceCollectionChanges()
	{
		// Teardown runs on the owning UI thread: the projected owner (TableViewSource) is
		// reference-tracked, so its final_release marshals the whole delete -- and thus this
		// destructor-driven revoke -- back to the DispatcherQueue it was constructed on. The revoke
		// therefore never lands on the GC finalizer thread, so no thread guard is needed here.
		// SafeRevoke still swallows the benign failure an app can cause by tearing the publisher down
		// first. (Every handler also holds a weak reference, so an un-revoked subscription is already
		// inert once this object dies.)
		ShapingHelpers.SafeRevoke(m_sourceCollectionChangedRevoker);
		ShapingHelpers.SafeRevoke(m_sourceVectorChangedRevoker);
		ShapingHelpers.SafeRevoke(m_sourceBindableVectorChangedRevoker);
	}

	private partial void OnSourceCollectionChanged()
	{
		// UI-thread contract: like every XAML items source, this engine requires its underlying
		// collection to raise change notifications on the UI thread -- m_rows is an observable the
		// control binds to, and the identity index must stay in lockstep with it. A background-thread
		// notification is app misuse and is not supported.
		Refresh();
	}

	private partial void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		// See the UI-thread contract on OnSourceCollectionChanged(): incremental InsertAt/RemoveAt/
		// SetAt below mutate the UI-affine projection directly, so they must run on the owning thread.

		// Re-entrant during a full rebuild: the in-flight Refresh() re-materializes the live source
		// when it completes, but changes after its initial materialization still need one coalesced
		// follow-up rebuild after the outer rebuild unwinds.
		if (m_isRefreshing)
		{
			m_pendingRefresh = true;
			return;
		}

		// Re-entrant during an incremental application: a synchronous VectorChanged handler (fired by
		// the InsertAt/RemoveAt/SetAt below) mutated the source. We cannot safely interleave a nested
		// incremental update against the half-updated projection/identity set (e.g. mid-Replace, after
		// the remove but before the insert). Defer a single full rebuild to run once the outer
		// application unwinds; Refresh() re-materializes live source and reseeds the identity set,
		// producing a consistent final projection.
		if (m_isApplyingIncrementalChange)
		{
			m_pendingRefresh = true;
			return;
		}

		{
			m_isApplyingIncrementalChange = true;
			try
			{
				ApplyIncrementalChange(args);
			}
			finally
			{
				m_isApplyingIncrementalChange = false;
			}
		}

		// A notification re-entered while we were applying: now that the projection/identity
		// invariants are consistent again, run exactly one deferred rebuild against live source.
		if (m_pendingRefresh)
		{
			m_pendingRefresh = false;
			Refresh();
		}
	}

	private partial void OnSourceVectorChanged(IVectorChangedEventArgs args)
	{
		// See the UI-thread contract on OnSourceCollectionChanged().

		if (m_isRefreshing)
		{
			m_pendingRefresh = true;
			return;
		}

		if (m_isApplyingIncrementalChange)
		{
			m_pendingRefresh = true;
			return;
		}

		{
			m_isApplyingIncrementalChange = true;
			try
			{
				ApplyIncrementalVectorChange(args);
			}
			finally
			{
				m_isApplyingIncrementalChange = false;
			}
		}

		if (m_pendingRefresh)
		{
			m_pendingRefresh = false;
			Refresh();
		}
	}

	private partial void ApplyIncrementalChange(NotifyCollectionChangedEventArgs args)
	{

		// Every path below either mutates m_rows without going through Refresh or falls back to
		// Refresh. The first leaves the retained layer-1 membership describing a projection that no
		// longer exists, so drop it up front rather than at each of the mutation sites; Refresh
		// repopulates it, and the in-place shaping path refuses to run without it.
		InvalidateShapingState();

		// A rebuild in flight, a grouped projection, or a not-yet-materialized projection -> full
		// rebuild (the incremental paths need a live flat projection + its view/metadata).
		if (m_isRefreshing || m_groupSelector is not null || m_rows is null || m_kind == ProjectionKind.None)
		{
			Refresh();
			return;
		}

		// SORTED flat projection (the Task Manager scenario): a single-item Add/Remove/Replace is
		// applied by binary-search insertion / find-remove in place, avoiding a full O(n)
		// re-materialize + re-sort + re-hash on every underlying change. Optional filter is applied
		// as an admission gate. Falls back to a full rebuild for anything it can't apply exactly.
		//
		// Invariant: re-sorting is driven by collection notifications on this path. An in-place
		// mutation of a row's sort-key field that raises only INotifyPropertyChanged leaves the row
		// at its old sort position until the next collection change, matching XAML ItemsControl
		// sources.
		if (HasActiveSort())
		{
			// Only take the in-place sorted fast-path when a shaped flat projection is active
			// (a Flat projection). After a safe-degrade (an Unshaped projection leaves an UNSORTED
			// mirror in m_rows while a sort is still configured), a binary-search insert would splice
			// into an unsorted list at a bogus index, so fall through to a full Refresh() that
			// re-shapes (or re-degrades) coherently.
			if (m_kind == ProjectionKind.Flat && TryApplyIncrementalSortedChange(args))
			{
				return;
			}
			Refresh();
			return;
		}

		// Filter-only (no sort): the projection is source-order-among-kept, so an incremental insert
		// position isn't a simple source index; rebuild for correctness.
		if (m_pipeline.HasFilter())
		{
			Refresh();
			return;
		}

		// UNSHAPED flat projection: mirrors the source 1:1, so a single-item change maps directly
		// to the same index in the projection observable. When stable row metadata is active, keep
		// the identity set/index map in sync; degraded/no-identity flat projections can still splice
		// by index because no identity metadata is exposed.
		var identityRequired = IsIdentityRequired() && m_kind != ProjectionKind.Unshaped;
		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				if (args.NewItems is { } addedItems && addedItems.Count == 1 && args.NewStartingIndex >= 0)
				{
					var index = (uint)args.NewStartingIndex;
					if (index <= m_rows.Count)
					{
						var item = addedItems[0];
						if (identityRequired)
						{
							string? reason = null;
							if (!TryGetRequiredRowIdentity(item, out var identity, ref reason) ||
								!m_flatRowIdentities.Add(identity))
							{
								break; // empty/duplicate identity -> Refresh() re-validates and fails fast
							}
							ShiftTrackedFlatRowIndicesForInsert(index);
							if (!m_flatRowIdentityToIndex.TryAdd(identity, index))
							{
								break; // stale identity/index tracking -> Refresh() reseeds it
							}
						}
						m_rows.Insert((int)index, item);
						return;
					}
				}
				break;
			case NotifyCollectionChangedAction.Remove:
				if (args.OldStartingIndex >= 0)
				{
					var index = (uint)args.OldStartingIndex;
					if (args.OldItems is { } oldItems && oldItems.Count == 1 && index < m_rows.Count)
					{
						if (identityRequired)
						{
							string? reason = null;
							uint trackedIndex = 0;
							if (!TryGetRequiredRowIdentity(oldItems[0], out var identity, ref reason) ||
								!TryGetTrackedFlatRowIndex(identity, ref trackedIndex) ||
								trackedIndex != index)
							{
								break; // stale identity/index tracking -> Refresh() reseeds it
							}
							m_flatRowIdentities.Remove(identity);
							m_flatRowIdentityToIndex.Remove(identity);
							ShiftTrackedFlatRowIndicesForRemove(index);
						}
						m_rows.RemoveAt((int)index);
						return;
					}
				}
				break;
			case NotifyCollectionChangedAction.Replace:
				if (args.NewItems is { } newItems && newItems.Count == 1 && args.NewStartingIndex >= 0)
				{
					var index = (uint)args.NewStartingIndex;
					if (index < m_rows.Count)
					{
						var newItem = newItems[0];
						if (identityRequired)
						{
							// Retire the outgoing row's identity, then admit the incoming one. An empty or
							// duplicate incoming identity falls back to a full rebuild (which reseeds the
							// set, so the transient erase below is harmless).
							string? oldReason = null;
							if (TryGetRequiredRowIdentity(m_rows[(int)index], out var oldIdentity, ref oldReason))
							{
								uint trackedIndex = 0;
								if (!TryGetTrackedFlatRowIndex(oldIdentity, ref trackedIndex) ||
									trackedIndex != index)
								{
									break; // stale identity/index tracking -> Refresh() reseeds it
								}
								m_flatRowIdentities.Remove(oldIdentity);
								m_flatRowIdentityToIndex.Remove(oldIdentity);
							}
							else
							{
								break; // missing outgoing identity -> Refresh() re-validates
							}
							string? newReason = null;
							if (!TryGetRequiredRowIdentity(newItem, out var newIdentity, ref newReason) ||
								!m_flatRowIdentities.Add(newIdentity))
							{
								break; // empty/duplicate identity -> Refresh() re-validates and fails fast
							}
							if (!m_flatRowIdentityToIndex.TryAdd(newIdentity, index))
							{
								break; // stale identity/index tracking -> Refresh() reseeds it
							}
						}
						m_rows[(int)index] = newItem;
						return;
					}
				}
				break;
			default:
				break;
		}

		// Move / Reset / multi-item / out-of-range: atomic full rebuild.
		Refresh();
	}

	private partial void ApplyIncrementalVectorChange(IVectorChangedEventArgs args)
	{

		// Same reasoning as ApplyIncrementalChange: the retained membership stops describing m_rows
		// the moment this splices it.
		InvalidateShapingState();

		// VectorChanged has only a verb + index (no OldItems/NewItems). Keep the low-risk fast path
		// to flat 1:1 projections, where the source index is the projection index and the current
		// source/projection can provide the one item needed to splice m_rows and identity tracking.
		if (m_isRefreshing || m_groupSelector is not null || HasActiveSort() || m_pipeline.HasFilter() ||
			m_rows is null || m_kind == ProjectionKind.None)
		{
			Refresh();
			return;
		}

		var index = args.Index;
		var identityRequired = IsIdentityRequired() && m_kind != ProjectionKind.Unshaped;
		switch (args.CollectionChange)
		{
			case CollectionChange.ItemInserted:
				{
					if (index > m_rows.Count)
					{
						break;
					}

					if (!m_sourceAccessor.TryGetAt(index, out var item))
					{
						break;
					}

					if (identityRequired)
					{
						string? reason = null;
						if (!TryGetRequiredRowIdentity(item, out var identity, ref reason) ||
							!m_flatRowIdentities.Add(identity))
						{
							break;
						}
						ShiftTrackedFlatRowIndicesForInsert(index);
						if (!m_flatRowIdentityToIndex.TryAdd(identity, index))
						{
							break;
						}
					}

					m_rows.Insert((int)index, item);
					return;
				}
			case CollectionChange.ItemRemoved:
				{
					if (index >= m_rows.Count)
					{
						break;
					}

					var item = m_rows[(int)index];
					if (identityRequired)
					{
						string? reason = null;
						uint trackedIndex = 0;
						if (!TryGetRequiredRowIdentity(item, out var identity, ref reason) ||
							!TryGetTrackedFlatRowIndex(identity, ref trackedIndex) ||
							trackedIndex != index)
						{
							break;
						}
						m_flatRowIdentities.Remove(identity);
						m_flatRowIdentityToIndex.Remove(identity);
						ShiftTrackedFlatRowIndicesForRemove(index);
					}

					m_rows.RemoveAt((int)index);
					return;
				}
			case CollectionChange.ItemChanged:
				{
					if (index >= m_rows.Count)
					{
						break;
					}

					if (!m_sourceAccessor.TryGetAt(index, out var newItem))
					{
						break;
					}

					if (identityRequired)
					{
						string? oldReason = null;
						uint trackedIndex = 0;
						if (!TryGetRequiredRowIdentity(m_rows[(int)index], out var oldIdentity, ref oldReason) ||
							!TryGetTrackedFlatRowIndex(oldIdentity, ref trackedIndex) ||
							trackedIndex != index)
						{
							break;
						}
						m_flatRowIdentities.Remove(oldIdentity);
						m_flatRowIdentityToIndex.Remove(oldIdentity);

						string? newReason = null;
						if (!TryGetRequiredRowIdentity(newItem, out var newIdentity, ref newReason) ||
							!m_flatRowIdentities.Add(newIdentity))
						{
							break;
						}
						if (!m_flatRowIdentityToIndex.TryAdd(newIdentity, index))
						{
							break;
						}
					}

					m_rows[(int)index] = newItem;
					return;
				}
			case CollectionChange.Reset:
			default:
				break;
		}

		Refresh();
	}

	private partial bool TryApplyIncrementalSortedChange(NotifyCollectionChangedEventArgs args)
	{

		var identityRequired = IsIdentityRequired();
		var rows = m_rows!;

		// A stable sort breaks ties by SOURCE order, and the sorted projection does not carry the
		// source index of each row, so an item that lands inside a tie group cannot be placed
		// incrementally — except when it is the last item of the source, where "after every equal-key
		// row" is exactly what source order demands. Anything else falls back to a full rebuild,
		// which re-derives the tie order from the retained source.
		bool isLastSourceIndex(int startingIndex)
		{
			uint sourceCount = 0;
			return startingIndex >= 0 &&
				TryGetSourceItemCount(ref sourceCount) &&
				sourceCount > 0 &&
				(uint)startingIndex == sourceCount - 1;
		}

		// Insert one item into the sorted projection (filter-gated, identity-checked). Returns
		// false to force a full rebuild (empty/duplicate identity, or an ambiguous tie position ->
		// re-validate + safe-degrade).
		bool tryInsert(object? item, bool tiesResolvableByAppend)
		{
			if (item is null)
			{
				return false;
			}
			if (!m_pipeline.PassesFilter(item))
			{
				return true; // filtered out: projection unchanged
			}

			var placement = SortedInsertPlacementFor(item);
			if (placement.TiedWithExistingRow && !tiesResolvableByAppend)
			{
				return false; // source order decides this position -> full rebuild
			}

			if (identityRequired)
			{
				string? reason = null;
				if (!TryGetRequiredRowIdentity(item, out var identity, ref reason))
				{
					return false; // missing identity -> full rebuild will safe-degrade
				}
				if (!m_flatRowIdentities.Add(identity))
				{
					return false; // duplicate identity -> full rebuild will safe-degrade
				}
				ShiftTrackedFlatRowIndicesForInsert(placement.Index);
				if (!m_flatRowIdentityToIndex.TryAdd(identity, placement.Index))
				{
					return false; // stale identity/index map -> full rebuild will reseed it
				}
				rows.Insert((int)placement.Index, item);
				return true;
			}
			rows.Insert((int)placement.Index, item);
			return true;
		}

		// Remove one item from the projection by tracked identity; keep the identity index map in
		// sync. Returns false to force a full rebuild when consistency can't be proven.
		bool tryRemove(object? item)
		{
			if (item is null)
			{
				return false;
			}
			if (identityRequired)
			{
				string? reason = null;
				uint index = 0;
				// The identity is recomputed from the (current) item. If the item's identity key was
				// mutated in place before this notification, the recomputed identity won't match the
				// tracked identity/index; force a full rebuild, which reseeds the tracking from
				// scratch. Only remove incrementally when the map still points at a row with the same
				// identity, avoiding m_rows.IndexOf(item)'s O(n) WinRT ABI scan.
				if (!TryGetRequiredRowIdentity(item, out var identity, ref reason) ||
					!TryGetTrackedFlatRowIndex(identity, ref index))
				{
					return false;
				}
				m_flatRowIdentities.Remove(identity);
				m_flatRowIdentityToIndex.Remove(identity);
				ShiftTrackedFlatRowIndicesForRemove(index);
				rows.RemoveAt((int)index);
				return true;
			}

			return false;
		}

		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				if (args.NewItems is { } addedItems && addedItems.Count == 1)
				{
					return tryInsert(addedItems[0], isLastSourceIndex(args.NewStartingIndex));
				}
				return false;
			case NotifyCollectionChangedAction.Remove:
				if (args.OldItems is { } removedItems && removedItems.Count == 1)
				{
					return tryRemove(removedItems[0]);
				}
				return false;
			case NotifyCollectionChangedAction.Replace:
				{
					var oldItems = args.OldItems;
					var newItems = args.NewItems;
					if (oldItems is not null && newItems is not null && oldItems.Count == 1 && newItems.Count == 1)
					{
						// Remove the old row, then re-insert the new value at its (possibly changed) sort
						// position — this also covers a same-object value change that re-orders the row.
						if (!tryRemove(oldItems[0]))
						{
							return false;
						}
						return tryInsert(newItems[0], isLastSourceIndex(args.NewStartingIndex));
					}
					return false;
				}
			case NotifyCollectionChangedAction.Move:
				{
					// A sorted projection is independent of source order EXCEPT for tie order: rows the sort
					// cannot distinguish keep their source order, so moving one of them past another really
					// does reorder the projection. Ties are contiguous, so only the moved row's two sorted
					// neighbours have to be probed; if it has none, the move is genuinely invisible here.
					var movedItems = args.NewItems;
					if (movedItems is null || movedItems.Count != 1)
					{
						return false;
					}
					var moved = movedItems[0];
					if (moved is null || !identityRequired)
					{
						return false; // can't locate the row cheaply -> full rebuild
					}

					string? reason = null;
					uint index = 0;
					if (!TryGetRequiredRowIdentity(moved, out var identity, ref reason) ||
						!TryGetTrackedFlatRowIndex(identity, ref index))
					{
						return false;
					}

					var rowCount = (uint)rows.Count;
					if (index >= rowCount)
					{
						return false;
					}
					if (index > 0 && m_pipeline.CompareItemToRow(moved, rows[(int)(index - 1)]) == 0)
					{
						return false;
					}
					if (index + 1 < rowCount && m_pipeline.CompareItemToRow(moved, rows[(int)(index + 1)]) == 0)
					{
						return false;
					}
					return true;
				}
			default:
				return false; // Reset / multi-item -> full rebuild
		}
	}


	private partial bool HasActiveSort() => m_pipeline.HasActiveSort();

	private partial bool IsSourceMutable() => m_sourceAccessor.IsObservable();

	private partial bool IsIdentityRequired()
		=> m_groupSelector is not null || m_pipeline.HasFilter() || HasActiveSort() || IsSourceMutable();

	private partial bool HasAnyShapingVerb()
		=> m_pipeline.HasFilter() || m_groupSelector is not null || HasActiveSort();

	private partial bool TryGetRequiredRowIdentity(
		object? item,
		out string identity,
		ref string? reason)
	{
		return RowIdentity.TryGetRequiredRowIdentity(item, EffectiveIdentitySelector(), out identity, ref reason);
	}

	private partial bool ValidateRowIdentities(
		List<object?> rows,
		ref string? reason)
	{
		return RowIdentity.ValidateRowIdentities(rows, EffectiveIdentitySelector(), ref reason);
	}

	private partial void ClearFlatRowIdentityTracking()
		=> RowIdentity.ClearFlatRowIdentityTracking(m_flatRowIdentities, m_flatRowIdentityToIndex);

	private partial void RebuildFlatRowIdentityTracking(List<object?> rows)
	{
		RowIdentity.RebuildFlatRowIdentityTracking(
			rows,
			IsIdentityRequired(),
			EffectiveIdentitySelector(),
			m_flatRowIdentities,
			m_flatRowIdentityToIndex);
	}

	private partial bool TryGetTrackedFlatRowIndex(string identity, ref uint index)
	{
		return RowIdentity.TryGetTrackedFlatRowIndex(
			identity,
			m_rows,
			EffectiveIdentitySelector(),
			m_flatRowIdentityToIndex,
			ref index);
	}

	private partial void ShiftTrackedFlatRowIndicesForInsert(uint insertedIndex)
	{
		// Pure-append fast path. Callers invoke this BEFORE m_rows.InsertAt(insertedIndex, item), so
		// m_rows.Size() reflects the pre-insert row count and every tracked identity's index lies in
		// [0, m_rows.Size()). When insertedIndex >= m_rows.Size() the change is a tail append and
		// nothing existing satisfies entry.second >= insertedIndex — so the O(n) walk is guaranteed
		// to be a no-op. Skipping avoids the sweep on every append and prevents bulk-load (N sequential
		// appends) from degrading to O(N^2). Using m_rows.Size() rather than m_flatRowIdentityToIndex
		// .size() is intentional: the identity map can be strictly smaller than m_rows when a row was
		// skipped by RebuildFlatRowIdentityTracking, so the map's size is NOT a safe upper bound.
		if (m_rows is not null && insertedIndex >= m_rows.Count)
		{
			return;
		}
		RowIdentity.ShiftTrackedFlatRowIndicesForInsert(m_flatRowIdentityToIndex, insertedIndex);
	}

	private partial void ShiftTrackedFlatRowIndicesForRemove(uint removedIndex)
	{
		// Pure-tail-remove fast path. Callers invoke this BEFORE m_rows.RemoveAt(removedIndex), so
		// m_rows.Size() reflects the pre-remove row count and every tracked identity's index lies in
		// [0, m_rows.Size()). When removedIndex + 1 >= m_rows.Size() the removal is at the tail — no
		// remaining entry can have entry.second > removedIndex, so the O(n) walk is a guaranteed
		// no-op. As with the Insert path, m_rows.Size() (not the identity map's size) is the safe
		// upper bound because untracked rows can leave holes in the identity map.
		if (m_rows is not null && removedIndex + 1 >= m_rows.Count)
		{
			return;
		}
		RowIdentity.ShiftTrackedFlatRowIndicesForRemove(m_flatRowIdentityToIndex, removedIndex);
	}

	private partial bool TryGetGroupIdentity(
		object? key,
		out string identity,
		ref string? reason)
	{
		return RowIdentity.TryGetGroupIdentity(key, m_groupIdentitySelector, out identity, ref reason);
	}

	private partial void RebuildUnshapedRows(List<object?> rows, string? reason)
	{
		LogIdentityProjectionDisabled(reason);

		// An unshaped mirror is not a shaped projection: no filter or sort was applied, so there is
		// no layer-1 membership to re-sort in place later.
		InvalidateShapingState();

		m_rows!.ReplaceAll(rows);
		m_kind = ProjectionKind.Unshaped;

		// A grouped/degraded projection does not use the flat incremental fast-path.
		ClearFlatRowIdentityTracking();

		if (m_groupSource is not null)
		{
			// Detach the adapter BEFORE clearing the internal group source. Otherwise Clear() fires
			// the adapter's outer-source subscription, which rebuilds synchronously and raises an
			// empty Reset into the ItemsRepeater still bound to the old grouped Entries (with realized
			// rows) -- an assertion failure / fault mid-teardown. RaiseProjectionRebuilt below is the
			// single controlled swap that moves the row axis to the flat projection.
			if (m_groupedAdapter is not null)
			{
				m_groupedAdapter.DetachSourceQuietly();
			}
			m_groupSource.Clear();
		}
		m_groupCache.Clear();
		m_projectedAsGrouped = false;
		RaiseProjectionRebuilt();
	}

	public partial void Refresh()
	{

		// Re-entrancy guard: a source notification that arrives while a rebuild is in flight
		// (e.g. an app mutating the source from a filter/sort/group callback) must not re-enter
		// ReplaceAll on the projection. Remember it and run one coalesced rebuild after the outer
		// rebuild unwinds so changes after materialization are not lost.
		if (m_isRefreshing)
		{
			m_pendingRefresh = true;
			return;
		}

		var wasProjectedAsGrouped = m_projectedAsGrouped;
		var runPendingRefresh = false;
		{
			m_isRefreshing = true;
			m_pendingRefresh = false;
			try
			{
				var authoritativeSource = m_source;
				var rows = Materialize(authoritativeSource);

				if (!HasAnyShapingVerb())
				{
					// Nothing is being shaped, so this is a plain mirror of the source. Identity buys
					// nothing here -- there is no reordering to anchor against and no membership change to
					// splice surgically -- and minting it would cost a QI plus a string format per row on
					// every refresh of a table that asked for none of it.
					RebuildUnshapedRows(rows, "no shaping verb");
				}
				else
				{
					ApplyFilter(rows);

					// A shaping verb is in force here (the branch above took the no-verb case), and a verb
					// always requires identity, so there is nothing to gate on.
					string? reason = null;
					if (!ValidateRowIdentities(rows, ref reason))
					{
						LogIdentityProjectionDisabled(reason);

						// Identity is derived from each item's object identity, which is unique among live
						// objects, so the expected failure is one object occupying more than one row --
						// there is no app-authored selector to blame and nothing to disambiguate with.
						// A row that cannot produce an identity at all lands here too (a null item, say),
						// and must not be reported as a duplicate.
						const string c_duplicateObjectReason = "the same item object appears on more than one row";
						if (reason is not null && c_duplicateObjectReason == reason)
						{
							throw new ArgumentException(
								Diagnostic(
									"The same item object appears in the source more than once. Rows are " +
									"identified by object identity, so two rows backed by one object cannot " +
									"be told apart. Use a distinct object per row."));
						}

						var message = Diagnostic("A row could not be given a stable identity");
						if (reason is not null)
						{
							message = message + ": " + reason;
						}
						throw new ArgumentException(message);
					}

					if (m_groupSelector is not null)
					{
						RebuildGrouped(rows);
					}
					else
					{
						RebuildFlat(rows);
					}
				}
				MUX_ASSERT(ReferenceEquals(m_source, authoritativeSource));
			}
			finally
			{
				m_isRefreshing = false;
				runPendingRefresh = m_pendingRefresh;
				m_pendingRefresh = false;
			}
		}

		if (runPendingRefresh)
		{
			if (m_projectedAsGrouped != wasProjectedAsGrouped)
			{
				RaiseShapeSwapped();
			}
			Refresh();
			return;
		}

		// Grouped <-> flat swaps the ItemsSourceView and the row-metadata provider. The owning
		// TableView cached both (plus grouped-ness) when it bound, so without this it would keep
		// projecting the previous shape - and would read a raw item as a GroupedEntry, or miss the
		// group-header rows entirely.
		if (m_projectedAsGrouped != wasProjectedAsGrouped)
		{
			RaiseShapeSwapped();
		}
	}

	private partial void RebuildFlat(List<object?> rows)
	{

		// Retain the post-filter membership in SOURCE order before sorting. A later sort-only change
		// re-seats on this rather than on the already-sorted output, so its stable sort breaks ties
		// the same way a full rebuild of that spec would.
		m_shapingState.FilteredSource = new List<object?>(rows);

		ApplySort(rows);

		m_shapingState.Items = new List<object?>(rows);
		m_shapingState.Buckets.Clear();
		m_shapingState.IsGrouped = false;
		m_shapingState.HasProjection = true;

		m_rows!.ReplaceAll(rows);
		m_kind = ProjectionKind.Flat;

		// Seed the identity tracking that the incremental fast-path maintains, so it can detect
		// duplicate/empty identities and locate sorted removes without O(n) WinRT IndexOf scans.
		RebuildFlatRowIdentityTracking(rows);

		// Releasing any prior grouped projection: switching grouped->flat must not retain the stale
		// group observable/cache. They are rebuilt from scratch by RebuildGrouped on the next GroupBy,
		// so holding them here only leaks the previous grouping (and its cached ShapedGroups).
		if (m_groupSource is not null)
		{
			// Detach the adapter before Clear() so its subscription does not re-enter Rebuild()
			// synchronously and Reset the ItemsRepeater still bound to the old grouped Entries.
			// RaiseProjectionRebuilt below performs the single controlled swap to the flat row axis.
			if (m_groupedAdapter is not null)
			{
				m_groupedAdapter.DetachSourceQuietly();
			}
			m_groupSource.Clear();
		}
		m_groupCache.Clear();
		m_projectedAsGrouped = false;
		RaiseProjectionRebuilt();
	}

	private static partial List<object?> Materialize(object? source)
		=> ShapingHelpers.EnumerateInspectableItems(source, true);

	private partial void RebuildGrouped(List<object?> rows)
	{
		// The grouped projection is materialized through the group adapter, not from the retained
		// layer-1 state, so leaving that state live would let the in-place path re-sort a flat
		// projection that is no longer the one being shown.
		InvalidateShapingState();
		// A grouped projection does not use the flat incremental fast-path.
		ClearFlatRowIdentityTracking();
		// Sorts requested before GroupBy establish the group order. Sorts requested after GroupBy
		// are applied per bucket below, preserving the group order while sorting within each group.
		ApplySort(rows, -1, m_pipeline.GroupOrder());

		List<ShapingHelpers.KeyedBucket> keyedBuckets = new();
		string? degradeReason = null;
		var grouped = ShapingHelpers.BucketizeToGroups(
			rows,
			(object? item) =>
			{
				try { return m_groupSelector!(item); }
				catch { return null; }
			},
			(object? key, ref string identity, ref string? reason) =>
			{
				return TryGetGroupIdentity(key, out identity, ref reason);
			},
			(object? existingKey, object? newKey) =>
			{
				// Not a collision when the app supplied a groupIdentitySelector (the identity is
				// authoritative, so two distinct key instances mapping to the same identity is
				// intentional — e.g. a per-item composite key) or the keys are genuinely equal.
				return m_groupIdentitySelector is not null || RowIdentity.GroupKeysEqual(existingKey, newKey);
			},
			keyedBuckets,
			ref degradeReason);

		if (!grouped)
		{
			// Spec contract (same shape as the row-identity fail-fast): an unresolvable,
			// unstable, or colliding *group* identity is a caller bug — the GroupBy(...) key
			// selector (and optional groupIdentitySelector) must produce a stable, non-empty
			// string identity per bucket, without two distinct group-key instances collapsing
			// to the same identity unless the app opted in via groupIdentitySelector. Silently
			// flattening the projection would let the app ship with grouping mysteriously "not
			// working" and no diagnostic.
			MUX_ASSERT(false,
				"GroupBy key selector produced an invalid group identity " +
				"(empty, non-string, throwing, or two distinct group keys collapsing to the " +
				"same identity without a groupIdentitySelector opt-in). Fix the GroupBy(...) " +
				"selector so every group has a stable non-empty unique string identity, or " +
				"supply a groupIdentitySelector that resolves the collision intentionally. " +
				"See the per-bucket reason string logged via LogIdentityProjectionDisabled.");
			LogIdentityProjectionDisabled(degradeReason);
			var message = Diagnostic("GroupBy key selector produced an invalid group identity");
			if (degradeReason is not null)
			{
				message = message + ": " + degradeReason;
			}
			throw new ArgumentException(message);
		}

		if (m_groupSource is null)
		{
			m_groupSource = new ObservableVector<object?>();
		}

		List<object?> groups = new(keyedBuckets.Count);
		HashSet<string> liveKeys = new();

		// The flat shaped rows kept under grouping: every kept row, in group order, with the
		// per-bucket sort applied and no header entries. Accumulated here rather than re-derived
		// afterwards because this loop already walks the buckets in their final order. This keeps
		// Rows() coherent as the flat shaped projection even while the presented row axis is the
		// grouped adapter.
		List<object?> flatRows = new(rows.Count);

		foreach (var bucket in keyedBuckets)
		{
			var keyString = bucket.Identity;
			ApplySort(bucket.Items, m_pipeline.GroupOrder(), -1);
			flatRows.AddRange(bucket.Items);

			ShapedGroup? group;
			if (m_groupCache.TryGetValue(keyString, out var cachedGroup))
			{
				// Deliberately keep the cached group's existing key object. The cache is keyed by
				// identity, so the incoming key is identity-equivalent to the one already held, but it
				// is a different object whenever the key selector minted a fresh one or the bucket
				// merged several equal keys. Rebinding it would churn the object ICollectionViewGroup
				// publishes as Group() on every reshape, for no gain.
				group = cachedGroup;
			}
			else
			{
				group = new ShapedGroup(bucket.Key, bucket.Identity);
				m_groupCache.TryAdd(keyString, group);
			}

			group.GroupKey(bucket.Identity);
			group.SetItems(bucket.Items);
			groups.Add(group);
			liveKeys.Add(keyString);
		}

		// TODO Uno: erase-while-iterating becomes a snapshot of the keys, then Remove.
		foreach (var key in new List<string>(m_groupCache.Keys))
		{
			if (!liveKeys.Contains(key))
			{
				m_groupCache.Remove(key);
			}
		}

		if (m_groupedAdapter is null)
		{
			m_groupedAdapter = new GroupedSourceAdapter();
		}
		else
		{
			// A prior grouped projection already attached the adapter to m_groupSource. Detach it
			// quietly so the ReplaceAll below fires NO subscription: the Source() re-attach then
			// rebuilds the adapter exactly once against the fully-populated groups. Leaving it
			// attached would rebuild twice (ReplaceAll's subscription -> synchronous Rebuild, then a
			// forced Refresh) and briefly publish the intermediate group set.
			m_groupedAdapter.DetachSourceQuietly();
		}

		// Populate m_groupSource BEFORE (re-)attaching so the adapter's subscribe + synchronous
		// Rebuild inside Source(value) sees a fully-loaded m_groupSource and publishes m_entries in
		// one coherent Reset. Attaching to an empty (or stale) m_groupSource and then loading it
		// caused an observable intermediate state: consumers saw the wrong entries, then a second
		// Reset after the rebuild.
		m_groupSource.ReplaceAll(groups);
		m_groupedAdapter.Source(m_groupSource);

		// The presented row axis under grouping is the adapter's computed ItemsSourceView, which has
		// no vector form. Keep Rows() maintained as the flat shaped projection anyway: without this
		// write m_rows would keep whatever the last FLAT rebuild left -- for the canonical
		// From(...).GroupBy(...) chain that is the raw, unshaped source -- so shaping verbs applied
		// AFTER GroupBy would not be reflected in the flat projection.
		//
		// Expansion is deliberately not applied: this is the shaped DATA. Collapsing a group hides
		// rows from the presented row axis without removing them from the projection, and a flat
		// vector whose size changed when a chevron is clicked would be reporting UI state.
		m_rows!.ReplaceAll(flatRows);

		m_kind = ProjectionKind.Grouped;
		m_projectedAsGrouped = true;
		RaiseProjectionRebuilt();
	}

	private partial ShapingHelpers.ShapingPipeline.SortedInsertPlacement SortedInsertPlacementFor(object? item)
	{
		return m_pipeline.SortedInsertPlacementFor(
			item,
			m_rows is not null ? (uint)m_rows.Count : 0,
			(uint index) => m_rows![(int)index]);
	}

	private partial bool TryGetSourceItemCount(ref uint count)
	{
		if (!m_sourceAccessor.IsIndexable())
		{
			return false;
		}
		count = m_sourceAccessor.Count();
		return true;
	}

	private static partial string StringifyKey(object? key) => RowIdentity.StringifyKey(key);

	private partial string Diagnostic(string text) => m_diagnosticName + ": " + text;
}
