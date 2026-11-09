// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedItemsSource.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Interop;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;
using NotifyCollectionChangedAction = System.Collections.Specialized.NotifyCollectionChangedAction;
using NotifyCollectionChangedEventArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;
using NotifyCollectionChangedEventHandler = System.Collections.Specialized.NotifyCollectionChangedEventHandler;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class ShapedItemsSource
{
	// First segment of a binding path ("Address.City" -> "Address"): the name an item raises
	// PropertyChanged for. Empty for "(ns:Type.Prop)" or "[0]", meaning "may depend on anything".
	private static string LeadingPropertyName(string path)
	{
		var begin = path.AsSpan().IndexOfAnyExcept(' ', '\t');
		if (begin < 0)
		{
			return "";
		}
		path = path.Substring(begin);

		var end = path.AsSpan().IndexOfAny(".[ \t");
		var name = end < 0 ? path : path.Substring(0, end);
		if (name.Length == 0 || name[0] == '(')
		{
			return "";
		}
		return name;
	}

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

	// The caller's selector or data broke a documented contract: the caller's fault, so owners can tell it from
	// shaping failures.
	[DoesNotReturn]
	private static void ThrowCallerContract(string message)
	{
		throw ShapingHelpers.TagCallerError(new ArgumentException(message), ShapingHelpers.c_callerDataFault);
	}

	public ShapedItemsSource(object? source)
	{
		m_source = source;
		m_rows = new ObservableVector<object?>();
		m_ownerQueue = DispatcherQueue.GetForCurrentThread();
		m_ownerThreadId = Environment.CurrentManagedThreadId;
		m_weakThis = new WeakReference<ShapedItemsSource>(this);
	}

#if HAS_UNO
	// TODO Uno: .NET has no deterministic destructor, so these revokes cannot run when the engine dies.
	// Instead, a handler whose engine is gone revokes its own subscription on the next notification
	// (see Start, TrackFlatItemPropertyChanged and SubscribeToSourceCollectionChanges).

	// Original destructor logic (not executed):
	// ShapedItemsSource::~ShapedItemsSource()
	// {
	//     UnsubscribeFromSourceCollectionChanges();
	//     ClearLiveShapingSubscriptions();
	// }
#endif

	public partial void Start()
	{
		// Not in the constructor: needs weak_from_this so an off-thread handler cannot outlive us.
		var weakThis = m_weakThis;
		var queue = m_ownerQueue;
		var ownerThreadId = m_ownerThreadId;
		var dropped = m_liveShapingChangeDropped;
		// TODO Uno: there is no destructor to unsubscribe the items, so a handler that outlives this object
		// revokes every subscription on the next notification instead of staying registered forever.
		var tracker = m_liveShaping;
		m_liveShaping.SetChangeHandler(
			(object? item, string propertyName) =>
			{
				if (queue is not null && !queue.HasThreadAccess)
				{
					// Off-thread: all touched state belongs to the owning thread, so marshal it.
					var posted = queue.TryEnqueue(() =>
						{
							if (weakThis.TryGetTarget(out var strongThis))
							{
								strongThis.OnLiveShapedItemChanged(item, propertyName);
							}
							else
							{
								tracker.UnsubscribeAll();
							}
						});
					if (!posted)
					{
						Volatile.Write(ref dropped.Value, 1);
					}
					return;
				}

				if (queue is null && Environment.CurrentManagedThreadId != ownerThreadId)
				{
					// No queue to marshal to. Flag it; the owner's next live change forces a full
					// restore, which re-reads every item.
					Volatile.Write(ref dropped.Value, 1);
					return;
				}

				if (weakThis.TryGetTarget(out var strongThisOnOwnerThread))
				{
					strongThisOnOwnerThread.OnLiveShapedItemChanged(item, propertyName);
				}
				else
				{
					tracker.UnsubscribeAll();
				}
			});

		SubscribeToSourceCollectionChanges();
		Refresh();
	}

	public partial void SetLiveShaping(bool enabled)
	{
		if (m_liveShapingEnabled == enabled)
		{
			return;
		}

		m_liveShapingEnabled = enabled;

		if (enabled)
		{
			// Live shaping takes over staleness detection; anything the sort-key subscriptions had
			// pending is covered by the restore posted below.
			ClearFlatItemPropertyChangedTracking();

			// Untracked in-place edits are invisible to the fresh snapshots, so drop the retained
			// structure and post one restore to catch up.
			InvalidateRetainedHierarchyStructure();
			ResubscribeLiveShapingFromSource();
			if (m_liveItemChangedHook is not null)
			{
				m_liveItemChangedHook(null, true);
			}

			MarkLiveShapingDirty();
		}
		else
		{
			ClearLiveShapingSubscriptions();

			// Sort-key tracking owns staleness again. The projection is current (live shaping kept it
			// so), hence tracking restarts with nothing dirty.
			if (m_kind == ProjectionKind.Flat && m_shapingState.HasProjection)
			{
				RebuildFlatItemPropertyChangedTracking(m_shapingState.Items);
			}
		}
	}

	public partial void SetFilter(ShapingHelpers.Predicate? predicate)
	{
		m_pipeline.SetFilter(predicate);
		ResetHierarchyFilterOverlay();
		ApplyShapingChange();
	}

	public partial void SetFilter(string axisToken, ShapingHelpers.Predicate? predicate)
	{
		m_pipeline.SetFilter(axisToken, predicate);
		ResetHierarchyFilterOverlay();
		ApplyShapingChange();
	}

	public partial void ClearFilter()
	{
		m_pipeline.ClearFilter();
		ResetHierarchyFilterOverlay();
		ApplyShapingChange();
	}

	public partial void ClearFilter(string axisToken)
	{
		m_pipeline.ClearFilter(axisToken);
		ResetHierarchyFilterOverlay();
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

	public partial void SetParent(ShapingHelpers.KeySelector? key, ShapingHelpers.KeySelector? parentKey)
	{
		m_keySelector = key;
		m_parentKeySelector = parentKey;
		// An in-flight index build (we ran from a selector) discards its result.
		++m_parentDeclarationGeneration;

		// A different relation is a different tree: clear intent at next publish.
		m_parentRelationRedeclared = true;

		// The hierarchy axis is invisible to the spec diff; without this a parent verb is a no-op.
		m_hierarchyAxisDirty = true;

		// Grouping is NOT retracted: GroupBy buckets the roots (see RebuildGroupedHierarchical).
		ApplyShapingChange();
	}

	public partial void ClearParentBy()
	{
		// Nothing declared: no-op, avoid a needless Reset.
		if (m_parentKeySelector is null)
		{
			// A deferred teardown may still be owed; finish it now.
			if (m_pendingHierarchyTeardown)
			{
				CompleteDeferredHierarchyTeardown();
			}
			return;
		}

		m_keySelector = null;
		m_parentKeySelector = null;
		++m_parentDeclarationGeneration;
		m_parentRelationRedeclared = false;

		// Release now only if no publication (rebuild or re-slice) is on the stack; otherwise the outer
		// frame is still handing this adapter to consumers. The teardown stays owed until
		// non-hierarchical metadata publishes (the deferred rebuild may fail). Set before release.
		m_pendingHierarchyTeardown = true;
		if (!m_isRefreshing && !m_reslicingGroups)
		{
			ReleaseHierarchyProjection();
		}
		m_hierarchyAxisDirty = true;
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
		// Commit unconditionally, even when the in-place path is not taken: the committed spec is
		// the baseline the NEXT verb diffs against, so skipping it would make that diff report a
		// change that has already been applied.
		var delta = m_pipeline.CommitSpec();
		UpdateSortKeyPropertyFilter();

		// Consumed regardless of delta: the diff cannot see the hierarchy axis.
		var hierarchyChanged = m_hierarchyAxisDirty;
		m_hierarchyAxisDirty = false;

		if (delta.IsNoOp() && !hierarchyChanged)
		{
			// Re-declaring the identical shape. The projection already satisfies it, and a rebuild
			// would fire a Reset that drops every realized row for nothing. Reachable only from a
			// Clear* verb against a shape that has nothing to clear — every declaration re-mints its
			// description id, so a re-declaration always reads as a change.
			return;
		}

		// In-place paths cannot switch flat <-> hierarchical; a hierarchy change always rebuilds.
		if (!hierarchyChanged && TryApplyShapingDeltaInPlace(delta))
		{
			// Skips Refresh(), so re-capture live-shaping keys against the new spec (no COM churn).
			if (IsLiveShapingEnabled())
			{
				ResubscribeLiveShapingFromSource();
			}
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
	private partial bool TryApplyShapingDeltaInPlace(ShapingHelpers.ShapingDelta delta, bool allowDuringIncrementalChange)
	{
		// Only a pure re-order is safe to do against rows already in hand. Anything touching
		// membership or bucketing needs the source, the group cache and the projection swap that
		// only Refresh performs.
		if (delta.RequiredWork != ShapingHelpers.ShapingWork.ReSortWithinBuckets)
		{
			return false;
		}

		// A grouped projection is rebuilt through the group adapter, and sorts declared before the
		// group verb reorder the GROUPS — which layer 1's within-bucket sort cannot express. A
		// hierarchy re-sorts every sibling set through a rebuilt parent-key index.
		if (m_groupSelector is not null || m_projectedAsGrouped || m_parentKeySelector is not null)
		{
			return false;
		}

		if (m_rows is null || !m_shapingState.HasProjection || m_shapingState.IsGrouped)
		{
			return false;
		}

		// A rebuild is already going to run and will subsume this change.
		if (m_isRefreshing || (!allowDuringIncrementalChange && m_isApplyingIncrementalChange) || m_pendingRefresh)
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
			if (!ReferenceEquals(m_shapingState.Items[i], m_rows[i]))
			{
				return false;
			}
		}


		// Re-seats Items on FilteredSource (source order) and re-sorts, so ties break exactly as a
		// full reshape of the same spec would.
		ShapingHelpers.Reshape(m_shapingState, m_pipeline.CommittedSpec(), delta, m_pipeline.SwallowedFaultsTally());

		++m_projectionGeneration;
		m_rows.ReplaceAll(m_shapingState.Items);
		RebuildFlatRowIdentityTracking(m_shapingState.Items);

		// Membership is unchanged and subscriptions are keyed by object, so only start/stop observing
		// when a sort appears or disappears. Every row was just re-sorted, so nothing is dirty.
		if (!ShouldTrackSortKeyChanges())
		{
			ClearFlatItemPropertyChangedTracking();
		}
		else if (!m_flatItemPropertyChangedTrackingActive)
		{
			RebuildFlatItemPropertyChangedTracking(m_shapingState.Items);
		}
		else
		{
			ClearSortDirtyRows();
		}
		return true;
	}

	private partial void InvalidateShapingState()
	{
		++m_projectionGeneration;
		m_shapingState.HasProjection = false;
		m_shapingState.FilteredSource.Clear();
		m_shapingState.Items.Clear();
		m_shapingState.Buckets.Clear();
		m_shapingState.IsGrouped = false;
		ClearFlatItemPropertyChangedTracking();
	}

	private partial void SubscribeToSourceCollectionChanges()
	{
		UnsubscribeFromSourceCollectionChanges();

		// Classify the source once, here, where it is bound. Every later indexed read, count and
		// observability check reads off this resolution instead of re-probing.
		m_sourceAccessor = new ShapingHelpers.CollectionAccessor(m_source, ShapingHelpers.CollectionOwner.Caller);

		if (m_source is null)
		{
			return;
		}

		var weakThis = m_weakThis;
		// TODO Uno: there is no destructor to revoke the subscription below, so a handler that outlives
		// this object revokes it itself on the next notification instead of staying registered forever.
		IDisposable? revokeWhenDead = null;
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
			else
			{
				revokeWhenDead?.Dispose();
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
			else
			{
				revokeWhenDead?.Dispose();
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
				else
				{
					revokeWhenDead?.Dispose();
				}
			};
			collection.CollectionChanged += handler;
			revokeWhenDead = Disposable.Create(() => collection.CollectionChanged -= handler);
			m_sourceCollectionChangedRevoker.Disposable = revokeWhenDead;
		}
		else if (m_sourceAccessor.AsObservableVector() is { } vector)
		{
			// VectorChanged carries a single index + verb. For flat 1:1 projections, keep this
			// incremental instead of collapsing to ReplaceAll.
			vector.VectorChanged += applyVectorChange;
			revokeWhenDead = Disposable.Create(() => vector.VectorChanged -= applyVectorChange);
			m_sourceVectorChangedRevoker.Disposable = revokeWhenDead;
		}
		else if (m_sourceAccessor.AsBindableObservableVector() is { } bindableVector)
		{
			bindableVector.VectorChanged += applyBindableVectorChange;
			revokeWhenDead = Disposable.Create(() => bindableVector.VectorChanged -= applyBindableVectorChange);
			m_sourceBindableVectorChangedRevoker.Disposable = revokeWhenDead;
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
		++m_sourceChangeStamp;
		Refresh();
	}

	private partial void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		// See the UI-thread contract on OnSourceCollectionChanged(): incremental InsertAt/RemoveAt/
		// SetAt below mutate the UI-affine projection directly, so they must run on the owning thread.
		++m_sourceChangeStamp;

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

		try
		{
			m_isApplyingIncrementalChange = true;
			m_raiseReorderAfterIncrementalChange = false;
			try
			{
				ApplyIncrementalChange(args);
			}
			finally
			{
				m_isApplyingIncrementalChange = false;
			}
		}
		catch
		{
			// Post (not run) any request deferred behind the failed pass; the caller gets this error.
			PostPendingRefresh();
			throw;
		}

		var raiseReorder = m_raiseReorderAfterIncrementalChange;
		m_raiseReorderAfterIncrementalChange = false;

		// A notification re-entered while we were applying: now that the projection/identity
		// invariants are consistent again, run exactly one deferred rebuild against live source.
		if (m_pendingRefresh)
		{
			m_pendingRefresh = false;
			Refresh();
		}

		// A stale-sort repair re-ordered the projection; raise ShapingChanged(reorderOnly) so the owner
		// treats the Reset as a re-shape. Raised last so its handler may safely touch the source.
		if (raiseReorder)
		{
			RaiseShapingChanged(true /* reorderOnly */);
		}
	}

	private partial void OnSourceVectorChanged(IVectorChangedEventArgs args)
	{
		// See the UI-thread contract on OnSourceCollectionChanged().
		++m_sourceChangeStamp;

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

		try
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
		catch
		{
			// Post (not run) any request deferred behind the failed pass; the caller gets this error.
			PostPendingRefresh();
			throw;
		}

		if (m_pendingRefresh)
		{
			m_pendingRefresh = false;
			Refresh();
		}
	}

	private partial void ApplyIncrementalChange(NotifyCollectionChangedEventArgs args)
	{
		// Subscription DELTA from the args, here rather than per splice site, so every branch is
		// covered at cost proportional to the change.
		ApplyLiveShapingDelta(args);

		// Full rebuild unless there is a live flat projection. A hierarchy always re-indexes: any
		// change can move rows anywhere in the tree.
		if (m_isRefreshing || m_groupSelector is not null || m_parentKeySelector is not null || m_rows is null || m_kind == ProjectionKind.None)
		{
			Refresh();
			return;
		}

		// SORTED flat projection: apply a single-item change in place (binary-search insert / tracked
		// removal) instead of an O(n) rebuild per change; anything else falls back to Refresh.
		//
		// Invariant: re-sorting is driven by collection notifications on this path. An in-place
		// mutation of a row's sort-key field that raises only INotifyPropertyChanged leaves the row
		// at its old sort position until the next collection change repairs it first. With a filter,
		// always rebuild: a filter-key INPC change can make the retained FilteredSource stale.
		if (HasActiveSort())
		{
			var canTrySortedIncremental =
				!m_pipeline.HasFilter() &&
				((args.Action == NotifyCollectionChangedAction.Add &&
						args.NewItems is not null && args.NewItems.Count == 1) ||
					(args.Action == NotifyCollectionChangedAction.Remove &&
						args.OldItems is not null && args.OldItems.Count == 1) ||
					(args.Action == NotifyCollectionChangedAction.Replace &&
						args.OldItems is not null && args.OldItems.Count == 1 &&
						args.NewItems is not null && args.NewItems.Count == 1) ||
					(args.Action == NotifyCollectionChangedAction.Move &&
						args.NewItems is not null && args.NewItems.Count == 1));

			// Only take the in-place sorted fast-path when a shaped flat projection is active
			// (a Flat projection). After a safe-degrade (an Unshaped projection leaves an UNSORTED
			// mirror in m_rows while a sort is still configured), a binary-search insert would splice
			// into an unsorted list at a bogus index, so fall through to a full Refresh() that
			// re-shapes (or re-degrades) coherently.
			if (canTrySortedIncremental &&
				m_kind == ProjectionKind.Flat &&
				TryReshapeStaleSortedFlatProjection() &&
				TryApplyIncrementalSortedChange(args))
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

		// The paths below mutate m_rows outside Refresh, which would leave the retained layer-1 state
		// stale, so drop it (Refresh repopulates it). The sorted path above keeps it in lockstep instead.
		InvalidateShapingState();

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
		// VectorChanged carries no items, so the subscription delta is applied at each splice site;
		// Refresh() paths reconcile themselves.

		// Same reasoning as ApplyIncrementalChange: the retained membership stops describing m_rows
		// the moment this splices it.
		InvalidateShapingState();

		// VectorChanged has only a verb + index (no OldItems/NewItems). Keep the low-risk fast path
		// to flat 1:1 projections, where the source index is the projection index and the current
		// source/projection can provide the one item needed to splice m_rows and identity tracking.
		if (m_isRefreshing || m_groupSelector is not null || m_parentKeySelector is not null || HasActiveSort() || m_pipeline.HasFilter() ||
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
					AddLiveShapingSubscription(item);
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
					RemoveLiveShapingSubscription(item);
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

					var outgoing = m_rows[(int)index];
					m_rows[(int)index] = newItem;
					// Remove before add, so reassigning the SAME object stays subscribed.
					RemoveLiveShapingSubscription(outgoing);
					AddLiveShapingSubscription(newItem);
					return;
				}
			case CollectionChange.Reset:
			default:
				break;
		}

		Refresh();
	}

	private partial bool TryReshapeStaleSortedFlatProjection()
	{
		if (!HasActiveSort() ||
			m_kind != ProjectionKind.Flat ||
			m_groupSelector is not null ||
			m_projectedAsGrouped ||
			m_rows is null)
		{
			return true;
		}

		// Live shaping owns staleness here, but its repair is posted, so a change arriving before that
		// turn can see an order its own edit already invalidated. Refuse the in-place splice while a
		// restore is pending and let the caller rebuild instead of binary-searching an unsorted list.
		if (IsLiveShapingEnabled())
		{
			return !m_liveShapingDirty;
		}

		var dirtyRows = m_sortDirtyRows;
		m_sortDirtyRows = new();
		var overflow = m_sortDirtyOverflow;
		m_sortDirtyOverflow = false;

		if (dirtyRows.Count == 0 && !overflow)
		{
			return true;
		}

		// The projection was sorted before these keys changed, so any out-of-order adjacent pair has a
		// dirty row: checking dirty rows' neighbours is complete. Above 1/8 dirty, a full scan is cheaper.
		var sorted = true;
		if (overflow || dirtyRows.Count * 8 > m_rows.Count)
		{
			sorted = IsFlatProjectionSorted();
		}
		else
		{
			foreach (var address in dirtyRows)
			{
				var identity = RowIdentity.FormatObjectIdentity(address);
				if (!m_flatRowIdentityToIndex.ContainsKey(identity))
				{
					continue;
				}

				uint index = 0;
				if (!TryGetTrackedFlatRowIndex(identity, ref index))
				{
					sorted = IsFlatProjectionSorted();
					break;
				}

				if (!IsFlatRowInSortOrderWithNeighbours(index))
				{
					sorted = false;
					break;
				}
			}
		}

		if (sorted)
		{
			return true;
		}

		ShapingHelpers.ShapingDelta delta = default;
		delta.SortChanged = true;
		delta.RequiredWork = ShapingHelpers.ShapingWork.ReSortWithinBuckets;
		if (!TryApplyShapingDeltaInPlace(delta, true /* allowDuringIncrementalChange */))
		{
			return false;
		}

		m_raiseReorderAfterIncrementalChange = true;
		return true;
	}

	private partial bool IsFlatProjectionSorted()
	{
		if (m_rows is null || m_rows.Count < 2)
		{
			return true;
		}

		for (var i = 1; i < m_rows.Count; ++i)
		{
			if (m_pipeline.CompareItemToRow(m_rows[i - 1], m_rows[i]) > 0)
			{
				return false;
			}
		}
		return true;
	}

	private partial bool IsFlatRowInSortOrderWithNeighbours(uint index)
	{
		var rowCount = m_rows is not null ? (uint)m_rows.Count : 0;
		if (index >= rowCount)
		{
			return false;
		}

		var row = m_rows![(int)index];
		if (index > 0 && m_pipeline.CompareItemToRow(m_rows[(int)(index - 1)], row) > 0)
		{
			return false;
		}
		if (index + 1 < rowCount && m_pipeline.CompareItemToRow(row, m_rows[(int)(index + 1)]) > 0)
		{
			return false;
		}
		return true;
	}

	private partial void UpdateSortKeyPropertyFilter()
	{
		HashSet<string> names = new();
		var anyProperty = false;
		foreach (var axis in m_pipeline.ActiveSortAxes(-1, -1))
		{
			// No usable path (delegate key/comparer): any property change may move a row.
			var name = LeadingPropertyName(axis.SortMemberPath);
			if (name.Length == 0)
			{
				anyProperty = true;
				break;
			}
			names.Add(name);
		}

		m_sortKeyPropertyNames = names;
		m_anyPropertyMayAffectSort = anyProperty;
	}

	private partial void OnFlatItemPropertyChanged(ulong itemAddress, string propertyName)
	{
		// Beyond this, one linear check is cheaper than per-row lookups.
		const int c_maxIndividuallyTrackedDirtyRows = 256;


		// An empty name is INotifyPropertyChanged's "all properties changed".
		if (propertyName.Length != 0 &&
			!m_anyPropertyMayAffectSort &&
			!m_sortKeyPropertyNames.Contains(propertyName))
		{
			return;
		}

		if (m_sortDirtyOverflow)
		{
			return;
		}

		if (m_sortDirtyRows.Count >= c_maxIndividuallyTrackedDirtyRows)
		{
			m_sortDirtyRows.Clear();
			m_sortDirtyOverflow = true;
			return;
		}

		m_sortDirtyRows.Add(itemAddress);
	}

	private partial void TrackFlatItemPropertyChanged(object? item)
	{
		var propertyChangedItem = item as INotifyPropertyChanged;
		if (propertyChangedItem is null)
		{
			m_flatItemsMayChangeSortWithoutNotification = true;
			return;
		}

		var address = RowIdentity.GetObjectAddress(item);
		if (m_flatItemPropertyChangedRevokers.ContainsKey(address))
		{
			return;
		}

		var weakThis = m_weakThis;
		var queue = m_ownerQueue;
		var ownerThreadId = m_ownerThreadId;
		// TODO Uno: there is no destructor to revoke the subscription below, so a handler that outlives
		// this object revokes it itself on the next notification instead of staying registered forever.
		IDisposable? revokeWhenDead = null;
		PropertyChangedEventHandler handler = (object? sender, PropertyChangedEventArgs args) =>
		{
			var propertyName = args.PropertyName ?? "";

			// Never lock off-thread: that lock could be the last strong reference, running
			// the destructor (and its UI-affine revokers) on the app's worker thread. Same
			// marshalling as the live-shaping handler in Start().
			if (queue is not null && !queue.HasThreadAccess)
			{
				queue.TryEnqueue(() =>
					{
						if (weakThis.TryGetTarget(out var strongThis))
						{
							strongThis.OnFlatItemPropertyChanged(address, propertyName);
						}
						else
						{
							revokeWhenDead?.Dispose();
						}
					});
				return;
			}

			if (queue is null && Environment.CurrentManagedThreadId != ownerThreadId)
			{
				// Nothing to marshal to. Dropping the mark leaves the row stale until a
				// rebuild, the documented contract for unobserved sort-key edits.
				return;
			}

			if (weakThis.TryGetTarget(out var strongThisOnOwnerThread))
			{
				strongThisOnOwnerThread.OnFlatItemPropertyChanged(address, propertyName);
			}
			else
			{
				revokeWhenDead?.Dispose();
			}
		};
		propertyChangedItem.PropertyChanged += handler;
		revokeWhenDead = Disposable.Create(() => propertyChangedItem.PropertyChanged -= handler);
		m_flatItemPropertyChangedRevokers.TryAdd(
			address,
			new FlatItemPropertyChangedSubscription { Item = item, Revoker = revokeWhenDead });
	}

	private partial void UntrackFlatItemPropertyChanged(object? item)
	{
		if (item is not null && m_flatItemPropertyChangedRevokers.Count != 0)
		{
			if (m_flatItemPropertyChangedRevokers.Remove(RowIdentity.GetObjectAddress(item), out var subscription))
			{
				subscription.Revoke();
			}
		}
	}

	private partial void ClearSortDirtyRows()
	{
		m_sortDirtyRows.Clear();
		m_sortDirtyOverflow = false;
	}

	private partial void ClearFlatItemPropertyChangedTracking()
	{
		if (m_flatItemPropertyChangedRevokers.Count != 0)
		{
			var subscriptions = m_flatItemPropertyChangedRevokers;
			m_flatItemPropertyChangedRevokers = new();
			foreach (var subscription in subscriptions.Values)
			{
				subscription.Revoke();
			}
		}
		m_flatItemPropertyChangedTrackingActive = false;
		m_flatItemsMayChangeSortWithoutNotification = false;
		ClearSortDirtyRows();
	}

	private partial void RebuildFlatItemPropertyChangedTracking(List<object?> rows)
	{
		// Surviving rows keep their subscription; the rest are revoked when `previous` is destroyed.
		var previous = m_flatItemPropertyChangedRevokers;
		m_flatItemPropertyChangedRevokers = new();
		try
		{
			ClearFlatItemPropertyChangedTracking();

			if (!ShouldTrackSortKeyChanges())
			{
				return;
			}

			m_flatItemPropertyChangedTrackingActive = true;
			m_flatItemPropertyChangedRevokers.EnsureCapacity(rows.Count);
			foreach (var item in rows)
			{
				if (item is not null && previous.Count != 0)
				{
					var address = RowIdentity.GetObjectAddress(item);
					if (previous.Remove(address, out var subscription))
					{
						m_flatItemPropertyChangedRevokers.Add(address, subscription);
						continue;
					}
				}
				TrackFlatItemPropertyChanged(item);
			}
		}
		finally
		{
			foreach (var subscription in previous.Values)
			{
				subscription.Revoke();
			}
		}
	}

	// TODO Uno: a C++ local struct; C# has no local types, and a class gives `RowChange const*` its null state.
	private sealed class RowChange
	{
		public object? Item;
		public string Identity = "";
		public uint RowIndex;
		public int SourceIndex;
	}

	// Sorted, unfiltered, ungrouped flat projection only. Nothing is mutated until every precondition
	// passes, so a false return leaves all state untouched and the commit cannot fail half-way.
	private partial bool TryApplyIncrementalSortedChange(NotifyCollectionChangedEventArgs args)
	{
		// With no filter, FilteredSource is the source in source order, so the change's own index
		// addresses it directly; an identity mismatch there falls back to Refresh.
		if (m_rows is null ||
			m_pipeline.HasFilter() ||
			!m_shapingState.HasProjection ||
			m_shapingState.IsGrouped ||
			m_shapingState.Items.Count != m_rows.Count)
		{
			return false;
		}

		var rowCount = (uint)m_rows.Count;
		var generation = m_projectionGeneration;

		// Planning calls app code (sort keys, comparers), and committing a removal raises m_rows
		// listeners, either of which can rebuild the projection. Re-check before every raw vector
		// splice; a stale plan returns false and the caller rebuilds.
		bool isPlanCurrent()
		{
			return generation == m_projectionGeneration &&
				m_rows is not null && m_shapingState.HasProjection &&
				m_shapingState.Items.Count == m_rows.Count;
		}

		// A count mismatch means a change was missed; only a rebuild recovers.
		bool liveSourceCountIs(int expected)
		{
			uint liveCount = 0;
			return !TryGetSourceItemCount(ref liveCount) || liveCount == expected;
		}

		bool planRemove(object? item, int oldStartingIndex, ref RowChange removal)
		{
			if (item is null || oldStartingIndex < 0)
			{
				return false;
			}

			var sourceIndex = oldStartingIndex;
			if (sourceIndex >= m_shapingState.FilteredSource.Count || !ReferenceEquals(m_shapingState.FilteredSource[sourceIndex], item))
			{
				return false;
			}

			string? reason = null;
			uint rowIndex = 0;
			if (!TryGetRequiredRowIdentity(item, out var identity, ref reason) ||
				!TryGetTrackedFlatRowIndex(identity, ref rowIndex) ||
				rowIndex >= rowCount)
			{
				return false;
			}

			removal = new RowChange { Item = item, Identity = identity, RowIndex = rowIndex, SourceIndex = sourceIndex };
			return true;
		}

		// Indices are as they will be after `removal` (if any) is committed.
		bool planInsert(object? item, int newStartingIndex, RowChange? removal, ref RowChange insertion)
		{
			if (item is null || newStartingIndex < 0)
			{
				return false;
			}

			var sourceSize = m_shapingState.FilteredSource.Count - (removal is not null ? 1 : 0);
			var sourceIndex = newStartingIndex;
			if (sourceIndex > sourceSize || !liveSourceCountIs(sourceSize + 1))
			{
				return false;
			}

			string? reason = null;
			if (!TryGetRequiredRowIdentity(item, out var identity, ref reason))
			{
				return false;
			}
			if (m_flatRowIdentities.Contains(identity) &&
				!(removal is not null && removal.Identity == identity))
			{
				return false;
			}

			var skippedRow = removal is not null ? removal.RowIndex : rowCount;
			var remainingCount = rowCount - (removal is not null ? 1u : 0u);
			object? rowAt(uint index) => m_rows[(int)(index < skippedRow ? index : index + 1)];
			var placement = m_pipeline.SortedInsertPlacementFor(item, remainingCount, rowAt);
			if (placement.Index > remainingCount)
			{
				return false;
			}

			// Ties break by source order, which rows don't carry; only a source-last item can be placed
			// in a tie group (after every equal-key row).
			if (placement.TiedWithExistingRow && sourceIndex != sourceSize)
			{
				return false;
			}

			// Non-INPC rows may be stale unseen; at least verify the landing neighbourhood is ordered.
			if (m_flatItemsMayChangeSortWithoutNotification &&
				((placement.Index > 0 && m_pipeline.CompareItemToRow(rowAt(placement.Index - 1), item) > 0) ||
					(placement.Index < remainingCount && m_pipeline.CompareItemToRow(item, rowAt(placement.Index)) > 0)))
			{
				return false;
			}

			insertion = new RowChange { Item = item, Identity = identity, RowIndex = placement.Index, SourceIndex = sourceIndex };
			return true;
		}

		// m_rows is mutated last so all other state is consistent when listeners are notified.
		bool commitRemove(RowChange removal)
		{
			if (!isPlanCurrent() ||
				removal.SourceIndex >= m_shapingState.FilteredSource.Count ||
				removal.RowIndex >= m_shapingState.Items.Count ||
				!ReferenceEquals(m_shapingState.Items[(int)removal.RowIndex], removal.Item))
			{
				return false;
			}

			m_flatRowIdentities.Remove(removal.Identity);
			m_flatRowIdentityToIndex.Remove(removal.Identity);
			ShiftTrackedFlatRowIndicesForRemove(removal.RowIndex);
			m_shapingState.FilteredSource.RemoveAt(removal.SourceIndex);
			m_shapingState.Items.RemoveAt((int)removal.RowIndex);
			UntrackFlatItemPropertyChanged(removal.Item);
			if (m_rowsReleasedHook is not null)
			{
				m_rowsReleasedHook(removal.Item, new List<object?>());
			}
			m_rows.RemoveAt((int)removal.RowIndex);
			return true;
		}

		bool commitInsert(RowChange insertion)
		{
			if (!isPlanCurrent() ||
				insertion.SourceIndex > m_shapingState.FilteredSource.Count ||
				insertion.RowIndex > m_shapingState.Items.Count ||
				m_flatRowIdentities.Contains(insertion.Identity))
			{
				return false;
			}

			m_flatRowIdentities.Add(insertion.Identity);
			ShiftTrackedFlatRowIndicesForInsert(insertion.RowIndex);
			m_flatRowIdentityToIndex[insertion.Identity] = insertion.RowIndex;
			m_shapingState.FilteredSource.Insert(insertion.SourceIndex, insertion.Item);
			m_shapingState.Items.Insert((int)insertion.RowIndex, insertion.Item);
			if (m_flatItemPropertyChangedTrackingActive)
			{
				TrackFlatItemPropertyChanged(insertion.Item);
			}
			m_rows.Insert((int)insertion.RowIndex, insertion.Item);
			return true;
		}

		// An app callout that rebuilds mid-plan leaves the binary search reading rows against bounds it
		// computed earlier, so the failure can surface as a throw (out-of-range GetAt) rather than a
		// refused commit. Either way the plan is void; rebuild instead of surfacing it to the app.
		bool apply()
		{
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						var newItems = args.NewItems;
						RowChange insertion = new();
						if (newItems is null || newItems.Count != 1 ||
							!planInsert(newItems[0], args.NewStartingIndex, null, ref insertion))
						{
							return false;
						}
						return commitInsert(insertion);
					}
				case NotifyCollectionChangedAction.Remove:
					{
						var oldItems = args.OldItems;
						RowChange removal = new();
						if (oldItems is null || oldItems.Count != 1 ||
							!planRemove(oldItems[0], args.OldStartingIndex, ref removal) ||
							!liveSourceCountIs(m_shapingState.FilteredSource.Count - 1))
						{
							return false;
						}
						return commitRemove(removal);
					}
				case NotifyCollectionChangedAction.Replace:
					{
						var oldItems = args.OldItems;
						var newItems = args.NewItems;
						RowChange removal = new();
						RowChange insertion = new();
						if (oldItems is null || newItems is null || oldItems.Count != 1 || newItems.Count != 1 ||
							args.OldStartingIndex != args.NewStartingIndex ||
							!planRemove(oldItems[0], args.OldStartingIndex, ref removal) ||
							!planInsert(newItems[0], args.NewStartingIndex, removal, ref insertion))
						{
							return false;
						}
						// The removal's m_rows notification can rebuild before the insert lands; if it did, the
						// insert refuses and the caller's rebuild supersedes the half-applied replace.
						return commitRemove(removal) && commitInsert(insertion);
					}
				case NotifyCollectionChangedAction.Move:
					{
						// A sorted projection is independent of source order EXCEPT for tie order: rows the sort
						// cannot distinguish keep their source order, so moving one of them past another really
						// does reorder the projection. Ties are contiguous, so only the moved row's two sorted
						// neighbours have to be probed; if it has none, the move is genuinely invisible here.
						var movedItems = args.NewItems;
						if (movedItems is null || movedItems.Count != 1 ||
							args.OldStartingIndex < 0 || args.NewStartingIndex < 0)
						{
							return false;
						}

						var moved = movedItems[0];
						RowChange removal = new();
						var newSourceIndex = args.NewStartingIndex;
						if (!planRemove(moved, args.OldStartingIndex, ref removal) ||
							newSourceIndex >= m_shapingState.FilteredSource.Count ||
							!liveSourceCountIs(m_shapingState.FilteredSource.Count))
						{
							return false;
						}

						var index = removal.RowIndex;
						if (index > 0 && m_pipeline.CompareItemToRow(moved, m_rows[(int)(index - 1)]) == 0)
						{
							return false;
						}
						if (index + 1 < rowCount && m_pipeline.CompareItemToRow(moved, m_rows[(int)(index + 1)]) == 0)
						{
							return false;
						}

						// The comparisons above call app code.
						if (!isPlanCurrent() ||
							removal.SourceIndex >= m_shapingState.FilteredSource.Count ||
							newSourceIndex >= m_shapingState.FilteredSource.Count)
						{
							return false;
						}

						m_shapingState.FilteredSource.RemoveAt(removal.SourceIndex);
						m_shapingState.FilteredSource.Insert(newSourceIndex, moved);
						return true;
					}
				default:
					return false; // Reset / multi-item -> full rebuild
			}
		}

		try
		{
			return apply();
		}
		catch
		{
			if (!isPlanCurrent())
			{
				return false;
			}
			throw;
		}
	}


	public partial bool HasActiveSort() => m_pipeline.HasActiveSort();

	private partial bool IsSourceMutable() => m_sourceAccessor.IsObservable();

	private partial bool IsIdentityRequired()
		=> m_groupSelector is not null || m_parentKeySelector is not null || m_pipeline.HasFilter() || HasActiveSort() || IsSourceMutable();

	private partial bool HasAnyShapingVerb()
		=> m_pipeline.HasFilter() || m_groupSelector is not null || m_parentKeySelector is not null || HasActiveSort();

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

	public partial bool CanProjectWithRowIdentity()
	{
		// A shaped projection validated its rows when it was built.
		if (m_kind != ProjectionKind.None && m_kind != ProjectionKind.Unshaped &&
			m_validatedSourceStamp == m_sourceChangeStamp)
		{
			return true;
		}

		// Mirrors Refresh: identity is validated over the rows a shaped projection would publish.
		var rows = Materialize(m_source);
		if (m_parentKeySelector is null)
		{
			ApplyFilter(rows);
		}

		string? reason = null;
		if (!ValidateRowIdentities(rows, ref reason))
		{
			LogIdentityProjectionDisabled(reason);
			return false;
		}
		return true;
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

	private partial List<ShapingHelpers.KeyedBucket> BucketizeGroupsOrThrow(List<object?> items)
	{
		List<ShapingHelpers.KeyedBucket> keyedBuckets = new();
		string? rejectReason = null;
		var grouped = ShapingHelpers.BucketizeToGroups(
			items,
			(object? item) =>
			{
				try { return m_groupSelector!(item); }
				catch (Exception error) { m_pipeline.SwallowedFaultsTally().Note(error); return null; }
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
			ref rejectReason);

		if (!grouped)
		{
			// Spec contract (same shape as the row-identity fail-fast): an unresolvable,
			// unstable, or colliding *group* identity is a caller bug — the GroupBy(...) key
			// selector (and optional groupIdentitySelector) must produce a stable, non-empty
			// string identity per bucket, without two distinct group-key instances collapsing
			// to the same identity unless the app opted in via groupIdentitySelector. Silently
			// flattening the projection would let the app ship with grouping mysteriously "not
			// working" and no diagnostic.
			//
			// Thrown, not asserted: caller data, and an assert would skip the unwind in checked builds.
			LogIdentityProjectionDisabled(rejectReason);
			var message = Diagnostic("GroupBy key selector produced an invalid group identity");
			if (rejectReason is not null)
			{
				message = message + ": " + rejectReason;
			}
			ThrowCallerContract(message);
		}

		return keyedBuckets;
	}

	private partial ShapedGroup AcquireCachedGroup(ShapingHelpers.KeyedBucket bucket)
	{
		ShapedGroup? group;
		if (m_groupCache.TryGetValue(bucket.Identity, out var cachedGroup))
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
			m_groupCache.TryAdd(bucket.Identity, group);
		}

		group.GroupKey(bucket.Identity);
		return group;
	}

	private partial void PruneGroupCache(HashSet<string> liveKeys)
	{
		foreach (var key in m_groupCache.Keys)
		{
			if (!liveKeys.Contains(key))
			{
				m_groupCache.Remove(key);
			}
		}
	}

	private partial void AttachGroupSource(List<object?> groups)
	{
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
		m_groupSource!.ReplaceAll(groups);
		m_groupedAdapter.Source(m_groupSource);
	}

	private partial void ReattachGroupedAdapterAfterFailedRefill()
	{
		// Reattach so a refill that threw after the quiet detach does not orphan the adapter.
		if (m_groupedAdapter is not null && m_groupSource is not null && m_groupedAdapter.Source() is null)
		{
			m_groupedAdapter.Source(m_groupSource);
		}
	}

	private partial void ReleaseGroupedProjection()
	{
		if (m_groupSource is not null)
		{
			// Detach the adapter BEFORE clearing the internal group source. Otherwise Clear() fires
			// the adapter's outer-source subscription, which rebuilds synchronously and raises an
			// empty Reset into the ItemsRepeater still bound to the old grouped Entries (with realized
			// rows) -- an assertion failure / fault mid-teardown. The caller's PublishProjection is the
			// single controlled swap that moves the row axis to the new projection.
			if (m_groupedAdapter is not null)
			{
				m_groupedAdapter.DetachSourceQuietly();
			}
			m_groupSource.Clear();
		}
		m_groupCache.Clear();
	}

	private partial void RebuildUnshapedRows(List<object?> rows, string? reason)
	{
		LogIdentityProjectionDisabled(reason);

		// An unshaped mirror is not a shaped projection: no filter or sort was applied, so there is
		// no layer-1 membership to re-sort in place later.
		InvalidateShapingState();

		m_rows.ReplaceAll(rows);
		m_kind = ProjectionKind.Unshaped;

		// A grouped/degraded projection does not use the flat incremental fast-path.
		ClearFlatRowIdentityTracking();

		ReleaseHierarchyProjection();
		ReleaseGroupedProjection();
		m_projectedAsGrouped = false;
		PublishProjection();
	}

	public partial void Refresh()
	{

		// Re-entrancy guard: a source notification that arrives while a rebuild is in flight
		// (e.g. an app mutating the source from a filter/sort/group callback) must not re-enter
		// ReplaceAll on the projection. Remember it and run one coalesced rebuild after the outer
		// rebuild unwinds so changes after materialization are not lost.
		//
		// A group re-slice and a teardown completion are guarded the same way: a nested rebuild would
		// replace groups under their loops.
		if (m_isRefreshing || m_reslicingGroups)
		{
			m_pendingRefresh = true;
			return;
		}

		var wasProjectedAsGrouped = m_projectedAsGrouped;
		var runPendingRefresh = false;
		++m_projectionGeneration;
		try
		{
			m_isRefreshing = true;
			m_pendingRefresh = false;
			try
			{
				var authoritativeSource = m_source;
				m_refreshSourceStamp = m_sourceChangeStamp;

				// Cleared before the re-read: an off-thread change dropped while the items are being read
				// keeps the flag set, so the next live change still forces a rebuild.
				Volatile.Write(ref m_liveShapingChangeDropped.Value, 0);
				var rows = Materialize(authoritativeSource);
				m_refreshSourceRowCount = (uint)rows.Count;
				if (m_rowsReleasedHook is not null)
				{
					m_rowsReleasedHook(null, rows);
				}
				RefreshLiveShapingSubscriptions(rows);

				// All snapshots were just recaptured, so any pending live restore is satisfied -- unless
				// the rebuild below throws, in which case the catch re-marks it.
				m_liveShapingDirty = false;

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
					// A hierarchy filters in its index build (needs the unfiltered parent chain).
					if (m_parentKeySelector is null)
					{
						ApplyFilter(rows);
					}

					// A shaping verb is in force here (the branch above took the no-verb case), and a verb
					// always requires identity, so there is nothing to gate on.
					// A reused hierarchy structure was already validated.
					var structure = m_parentKeySelector is not null
						? TryReuseHierarchyStructure(rows, m_parentDeclarationGeneration, m_refreshSourceStamp)
						: null;
					string? reason = null;
					if (structure is null && !ValidateRowIdentities(rows, ref reason))
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
							ThrowCallerContract(
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
						ThrowCallerContract(message);
					}

					if (m_parentKeySelector is not null && m_groupSelector is not null)
					{
						RebuildGroupedHierarchical(rows, structure);
					}
					else if (m_parentKeySelector is not null)
					{
						RebuildHierarchical(rows, structure);
					}
					else if (m_groupSelector is not null)
					{
						RebuildGrouped(rows);
					}
					else
					{
						RebuildFlat(rows);
					}
					m_validatedSourceStamp = m_refreshSourceStamp;
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
		catch
		{
			// The snapshots may already describe data the kept projection does not show (a transient
			// duplicate key, say). Stay dirty without posting, so the next live change retries the
			// full rebuild instead of matching the fresh snapshots and dropping the change.
			if (IsLiveShapingEnabled())
			{
				m_liveShapingDirty = true;
			}
			m_validatedSourceStamp = ulong.MaxValue;

			// A request that arrived during the failed pass describes newer state; post it (inline
			// would throw into the same app call). Only such requests replay, so bad data cannot loop.
			// Contained: a scheduling failure must not replace the actionable exception rethrown below.
			if (runPendingRefresh)
			{
				try { ScheduleRefreshReplay(); } catch { }
			}

			// An in-pass ClearParentBy must still take effect; its rebuild just failed.
			CompleteDeferredHierarchyTeardown();
			throw;
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

		// Usually a no-op: the non-hierarchical rebuild already released the hierarchy.
		CompleteDeferredHierarchyTeardown();
	}

	private partial void PostPendingRefresh()
	{
		if (!m_pendingRefresh)
		{
			return;
		}

		try
		{
			ScheduleRefreshReplay();
		}
		catch
		{
		}

		// Kept if nothing was posted, so the next change rebuilds in full.
		if (m_refreshReplayScheduled)
		{
			m_pendingRefresh = false;
		}
	}

	private partial void ScheduleRefreshReplay()
	{
		if (m_refreshReplayScheduled)
		{
			return;
		}

		// No dispatcher (test host): nothing to post to; the next reshape re-materializes anyway.
		var queue = DispatcherQueue.GetForCurrentThread();
		if (queue is null)
		{
			return;
		}

		var weakThis = m_weakThis;
		m_refreshReplayScheduled = queue.TryEnqueue(() =>
		{
			if (!weakThis.TryGetTarget(out var strongThis))
			{
				return;
			}

			strongThis.m_refreshReplayScheduled = false;
			try
			{
				strongThis.Refresh();
			}
			catch
			{
				// No app call to receive this; the previous projection stays and the next verb or
				// source change re-throws.
			}
		});
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

		m_rows.ReplaceAll(rows);
		m_kind = ProjectionKind.Flat;

		// Seed the identity tracking that the incremental fast-path maintains, so it can detect
		// duplicate/empty identities and locate sorted removes without O(n) WinRT IndexOf scans.
		RebuildFlatRowIdentityTracking(rows);
		RebuildFlatItemPropertyChangedTracking(rows);

		ReleaseHierarchyProjection();

		// Releasing any prior grouped projection: switching grouped->flat must not retain the stale
		// group observable/cache. They are rebuilt from scratch by RebuildGrouped on the next GroupBy,
		// so holding them here only leaks the previous grouping (and its cached ShapedGroups).
		ReleaseGroupedProjection();
		m_projectedAsGrouped = false;
		PublishProjection();
	}

	private static partial List<object?> Materialize(object? source)
		=> ShapingHelpers.EnumerateInspectableItems(source, true, ShapingHelpers.CollectionOwner.Caller);

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

		var keyedBuckets = BucketizeGroupsOrThrow(rows);

		// Release any leftover hierarchy adapter, but only after bucketize: if that throws, the
		// previous projection must stay whole.
		ReleaseHierarchyProjection();

		if (m_groupSource is null)
		{
			m_groupSource = new ObservableVector<object?>();
		}

		List<object?> groups = new(keyedBuckets.Count);
		HashSet<string> liveKeys = new();

		// The flat shaped rows kept under grouping: every kept row, in group order, with the
		// per-bucket sort applied and no header entries. Accumulated while the per-bucket sort walks
		// the buckets in their final order. This keeps
		// Rows() coherent as the flat shaped projection even while the presented row axis is the
		// grouped adapter.
		List<object?> flatRows = new(rows.Count);

		foreach (var bucket in keyedBuckets)
		{
			ApplySort(bucket.Items, m_pipeline.GroupOrder(), -1);
			flatRows.AddRange(bucket.Items);
		}

		// The presented row axis under grouping is the adapter's computed ItemsSourceView, which has
		// no vector form. Keep Rows() maintained as the flat shaped projection anyway: without this
		// write m_rows would keep whatever the last FLAT rebuild left -- for the canonical
		// From(...).GroupBy(...) chain that is the raw, unshaped source -- so shaping verbs applied
		// AFTER GroupBy would not be reflected in the flat projection.
		//
		// Expansion is deliberately not applied: this is the shaped DATA. Collapsing a group hides
		// rows from the presented row axis without removing them from the projection, and a flat
		// vector whose size changed when a chevron is clicked would be reporting UI state.
		//
		// Written before the groups are filled: the adapter's Reset below closes an open edit, which
		// asks Rows() whether the edited item is still present.
		m_rows.ReplaceAll(flatRows);

		// Detach first: a live adapter would rebuild per changed group, briefly publishing a moved row
		// in no group, which drops its selection.
		if (m_groupedAdapter is not null)
		{
			m_groupedAdapter.DetachSourceQuietly();
		}

		try
		{
			foreach (var bucket in keyedBuckets)
			{
				var group = AcquireCachedGroup(bucket);
				group.SetItems(bucket.Items);
				groups.Add(group);
				liveKeys.Add(bucket.Identity);
			}
		}
		catch
		{
			ReattachGroupedAdapterAfterFailedRefill();
			throw;
		}

		PruneGroupCache(liveKeys);
		AttachGroupSource(groups);

		m_kind = ProjectionKind.Grouped;
		m_projectedAsGrouped = true;
		PublishProjection();
	}

	private partial void RebuildHierarchical(List<object?> rows, ShapingHelpers.ParentStructure? structure)
	{
		// `rows` is UNFILTERED, in source order: the index build filters and sorts per sibling list.
		// Capture the generation before any selector (app code) can re-declare the relation.
		var declarationGeneration = m_parentDeclarationGeneration;

		// May throw on invalid data; nothing is mutated yet.
		var index = BuildHierarchyIndex(rows, structure, true /* sortRoots */, declarationGeneration);
		if (index is null)
		{
			// Relation changed mid-build; the queued Refresh handles it.
			return;
		}

		// Rows come from an adapter now, so the in-place path must not re-sort stale layer-1 state.
		InvalidateShapingState();
		ClearFlatRowIdentityTracking();

		// Release any prior grouped projection so its adapter stops publishing headers.
		ReleaseGroupedProjection();
		m_hierarchyGroups.Clear();
		++m_hierarchyGeneration;
		m_rootBucketIndex.Clear();

		// Ungrouped: one segment spanning every root.
		PublishHierarchyIndex(index, new List<int>());

		// Rows() mirrors the visible rows as a SNAPSHOT; it does not track later expand/collapse.
		// Consumers needing live visible rows read the adapter.
		m_rows.ReplaceAll(VisibleHierarchicalRows());

		m_kind = ProjectionKind.Hierarchical;
		// No header rows, so not grouped.
		m_projectedAsGrouped = false;
		PublishProjection();
	}

	// Both axes. Row stream:
	//   [Header A] [rootA1] [rootA1's expanded subtree...] [rootA2] ... [Header B] [rootB1] ...
	// The group key applies to ROOTS only; a descendant keeps its depth under its parent.
	private partial void RebuildGroupedHierarchical(List<object?> rows, ShapingHelpers.ParentStructure? structure)
	{
		// Roots stay in source order for bucketing; sibling sets below are fully sorted.
		var declarationGeneration = m_parentDeclarationGeneration;
		var index = BuildHierarchyIndex(rows, structure, false /* sortRoots */, declarationGeneration);
		if (index is null)
		{
			return;
		}

		// Bucket like RebuildGrouped: pre-GroupBy sorts set header order; later sorts apply per bucket.
		List<object?> roots = new(index.Roots);
		ApplySort(roots, -1, m_pipeline.GroupOrder());

		// Same fail-fast contract as RebuildGrouped.
		var keyedBuckets = BucketizeGroupsOrThrow(roots);

		// Roots bucket by bucket. Each bucket's roots must be contiguous: the adapter walks one
		// segment per bucket and the re-slice cuts entries at depth-0 rows.
		List<object?> orderedRoots = new(roots.Count);
		List<int> segments = new(keyedBuckets.Count);
		Dictionary<object, int> rootBucketIndex = new(ReferenceEqualityComparer.Instance);

		for (var bucketIndex = 0; bucketIndex < keyedBuckets.Count; ++bucketIndex)
		{
			var bucket = keyedBuckets[bucketIndex];
			ApplySort(bucket.Items, m_pipeline.GroupOrder(), -1);
			foreach (var root in bucket.Items)
			{
				// Keyed by raw COM pointer: the re-slice looks up the same objects.
				if (root is not null)
				{
					rootBucketIndex[root] = bucketIndex;
				}
				orderedRoots.Add(root);
			}
			segments.Add(bucket.Items.Count);
		}

		index.Roots = orderedRoots;

		InvalidateShapingState();
		ClearFlatRowIdentityTracking();
		m_rootBucketIndex = rootBucketIndex;
		// Keep the publish below from re-slicing stale groups.
		m_hierarchyGroups.Clear();
		++m_hierarchyGeneration;

		PublishHierarchyIndex(index, segments);

		if (m_groupSource is null)
		{
			m_groupSource = new ObservableVector<object?>();
		}

		// Reuse cached groups as RebuildGrouped does, so Group() objects do not churn.
		List<object?> groups = new(keyedBuckets.Count);
		HashSet<string> liveKeys = new();
		m_hierarchyGroups.EnsureCapacity(keyedBuckets.Count);

		foreach (var bucket in keyedBuckets)
		{
			var group = AcquireCachedGroup(bucket);
			// Items (roots plus visible descendants) are filled by ResliceGroupsFromHierarchy.
			m_hierarchyGroups.Add(group);
			groups.Add(group);
			liveKeys.Add(bucket.Identity);
		}

		PruneGroupCache(liveKeys);

		// Fill groups BEFORE attaching the grouped adapter so it publishes one coherent Reset.
		if (m_groupedAdapter is not null)
		{
			m_groupedAdapter.DetachSourceQuietly();
		}
		try
		{
			ResliceGroupsFromHierarchy();
		}
		catch
		{
			ReattachGroupedAdapterAfterFailedRefill();
			throw;
		}
		AttachGroupSource(groups);

		// Rows(): visible rows in group order, headers excluded.
		m_rows.ReplaceAll(VisibleHierarchicalRows());

		m_kind = ProjectionKind.GroupedHierarchical;
		// Presented via the grouped adapter, so header rows exist.
		m_projectedAsGrouped = true;
		PublishProjection();
	}

	private partial ShapingHelpers.ParentKeyIndex? BuildHierarchyIndex(
		List<object?> rows,
		ShapingHelpers.ParentStructure? structure,
		bool sortRoots,
		ulong declarationGeneration)
	{
		// Relation changed while rows were prepared; selectors may be cleared, so do not run them.
		if (declarationGeneration != m_parentDeclarationGeneration)
		{
			m_pendingRefresh = true;
			return null;
		}

		if (structure is null)
		{
			// Selectors may re-declare the relation mid-build: copy them, then check the generation.
			var keySelector = m_keySelector;
			var parentKeySelector = m_parentKeySelector;

			var valid = ShapingHelpers.BuildParentStructure(rows, keySelector!, parentKeySelector!, out var built, out var error);

			if (declarationGeneration != m_parentDeclarationGeneration)
			{
				// Obsolete: drop the result and any error; ensure the replacing verb's Refresh runs.
				m_pendingRefresh = true;
				return null;
			}

			if (!valid)
			{
				ThrowCallerContract(Diagnostic(error));
			}

			structure = built!;
			m_hierarchyStructure = structure;
			m_hierarchyStructureGeneration = declarationGeneration;
			m_hierarchyStructureSourceStamp = m_refreshSourceStamp;
		}

		ShapingHelpers.ParentKeyFilter? filter = null;
		if (m_pipeline.HasFilter())
		{
			filter = (object? item) => m_pipeline.PassesFilter(item);
		}

		ShapingHelpers.SiblingSorter? sort = null;
		if (HasActiveSort())
		{
			sort = (List<object?> siblings) => ApplySort(siblings);
		}

		ShapingHelpers.BuildParentKeyIndex(structure, filter, sort, sortRoots, out var index);

		if (declarationGeneration != m_parentDeclarationGeneration)
		{
			// A filter or sort key selector re-declared the relation; as above.
			m_pendingRefresh = true;
			return null;
		}

		return index;
	}

	private partial ShapingHelpers.ParentStructure? TryReuseHierarchyStructure(
		List<object?> rows,
		ulong declarationGeneration,
		ulong sourceChangeStamp)
	{
		// Strong ref: selectors below may re-declare the relation and drop the member.
		var structure = m_hierarchyStructure;
		if (structure is null ||
			m_hierarchyStructureGeneration != declarationGeneration ||
			m_hierarchyStructureSourceStamp != sourceChangeStamp ||
			structure.Rows.Count != rows.Count)
		{
			return null;
		}

		// Silent sources can change too; O(n) pointer compares are cheaper than selector calls.
		for (var i = 0; i < rows.Count; ++i)
		{
			if (!ReferenceEquals(structure.Rows[i], rows[i]))
			{
				return null;
			}
		}

		// Live shaping already compared every edge this Refresh; otherwise re-read the keys (cheaper
		// than a rebuild). Copy selectors: a re-declaring selector reassigns the members.
		var keySelector = m_keySelector;
		var parentKeySelector = m_parentKeySelector;
		if (!IsLiveShapingEnabled() &&
			!ShapingHelpers.ParentStructureStillMatches(structure, keySelector!, parentKeySelector!))
		{
			return null;
		}

		// Re-declared while selectors ran; let the rebuild queue the replay.
		if (declarationGeneration != m_parentDeclarationGeneration)
		{
			return null;
		}
		return structure;
	}

	private partial void PublishHierarchyIndex(ShapingHelpers.ParentKeyIndex index, List<int> rootSegments)
	{
		if (m_hierarchicalAdapter is null)
		{
			m_hierarchicalAdapter = new HierarchicalSourceAdapter();
		}

		// Drop the old callback first: it would re-slice groups this rebuild is replacing.
		m_hierarchicalAdapter.ProjectionChanged(null);

		// A re-declared relation is a different tree: its intent starts from the collapsed baseline.
		var redeclared = m_parentRelationRedeclared;
		m_parentRelationRedeclared = false;
		if (redeclared)
		{
			m_hierarchicalAdapter.ResetIntentQuietly();
		}

		m_hierarchicalAdapter.SetIndex(index, rootSegments);

		// Under grouping, node toggles splice only the hierarchy adapter; this carries them to the
		// groups. Uses the COHERENT edge: raw vector events fire per row, mid-repair.
		if (m_groupSelector is not null)
		{
			var weakThis = m_weakThis;
			m_hierarchicalAdapter.ProjectionChanged(
				() =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						strongThis.ResliceGroupsFromHierarchy();
					}
				});
		}
	}

	private partial void ResetHierarchyFilterOverlay()
	{
		// New filter: context rows return to the default (expanded) state.
		if (m_hierarchicalAdapter is not null)
		{
			m_hierarchicalAdapter.ResetFilterOverlay();
		}
	}

	private partial void ReleaseHierarchyProjection()
	{
		if (m_hierarchicalAdapter is not null)
		{
			// Replacement may fail: keep every row for the inert fallback (see
			// CompleteDeferredHierarchyTeardown). Grouped trees keep rows in the slices.
			if (m_pendingHierarchyTeardown && m_kind == ProjectionKind.Hierarchical)
			{
				m_releasedHierarchyRows = HierarchyFallbackRows();
			}

			// Callback first: nothing below may drive a re-slice on the way out.
			m_hierarchicalAdapter.ProjectionChanged(null);
			// Also detaches: we may be inside its own notification, which must stop here.
			m_hierarchicalAdapter.ClearIndex();

			// A still-bound provider must not act on retracted intent.
			m_hierarchicalAdapter.ResetIntentQuietly();

			// Don't pin rows; a still-bound provider shares ownership until the swap.
			m_hierarchicalAdapter = null;
		}

		m_hierarchyGroups.Clear();
		++m_hierarchyGeneration;
		m_rootBucketIndex.Clear();
		// Holds every source row alive; a later relation rebuilds it anyway.
		m_hierarchyStructure = null;
	}

	private partial List<object?> HierarchyFallbackRows()
	{
		// The flat view of the last good structure: every row in source order, so descendants of a
		// collapsed node are not lost with the tree. Filtered as a flat source would be, best-effort.
		if (m_hierarchyStructure is null)
		{
			return VisibleHierarchicalRows();
		}

		List<object?> rows = new(m_hierarchyStructure.Rows);
		try
		{
			ApplyFilter(rows);
		}
		catch
		{
			rows = new List<object?>(m_hierarchyStructure.Rows);
		}
		return rows;
	}

	private partial List<object?> VisibleHierarchicalRows()
	{
		List<object?> visibleRows = new();
		if (m_hierarchicalAdapter is null)
		{
			return visibleRows;
		}

		var entries = m_hierarchicalAdapter.Entries();
		if (entries is not null)
		{
			var count = entries.Count;
			visibleRows.EnsureCapacity(count > 0 ? count : 0);
			for (var i = 0; i < count; ++i)
			{
				visibleRows.Add(entries.GetAt(i));
			}
		}
		return visibleRows;
	}

	private partial void ResliceGroupsFromHierarchy()
	{
		// Pin/snapshot state up front: SetItems below runs app handlers that may tear down or rebuild.
		var adapter = m_hierarchicalAdapter;
		if (adapter is null || m_hierarchyGroups.Count == 0)
		{
			return;
		}

		// SetItems triggers a grouped rebuild that must not re-enter. UI-thread only, so a plain bool.
		if (m_reslicingGroups)
		{
			return;
		}

		{
			m_reslicingGroups = true;
			try
			{
				try
				{
					// A generation bump (rebuild/teardown) ends the publish below.
					List<ShapedGroup> groups = new(m_hierarchyGroups);
					var generation = m_hierarchyGeneration;

					var slices = new List<object?>[groups.Count];
					for (var i = 0; i < slices.Length; ++i)
					{
						slices[i] = new List<object?>();
					}
					// Header counts roots, not the descendant rows a slice also holds.
					var rootCounts = new int[groups.Count];

					var entries = adapter.Entries();
					var count = entries is not null ? entries.Count : 0;

					// Each depth-0 row opens its bucket; following rows belong to it until the next root.
					var currentBucket = 0;
					var haveBucket = false;
					for (var i = 0; i < count; ++i)
					{
						var node = adapter.TryGetNodeRow(i);
						if (node is null)
						{
							continue;
						}

						if (node.Depth == 0)
						{
							if (node.Item is null || !m_rootBucketIndex.TryGetValue(node.Item, out var bucket))
							{
								// Unknown root (source mutated; rebuild queued): drop its subtree.
								haveBucket = false;
								continue;
							}
							currentBucket = bucket;
							haveBucket = true;
							if (currentBucket < rootCounts.Length)
							{
								++rootCounts[currentBucket];
							}
						}

						if (haveBucket && currentBucket < slices.Length)
						{
							slices[currentBucket].Add(node.Item);
						}
					}

					for (var i = 0; i < groups.Count; ++i)
					{
						// Stale: an earlier SetItems handler replaced the hierarchy.
						if (generation != m_hierarchyGeneration)
						{
							break;
						}

						// Count before items: SetItems re-mints the header from the count.
						groups[i].GroupChildCount(rootCounts[i]);
						groups[i].SetItems(slices[i]);
					}
				}
				catch
				{
					// On a throwing handler, still complete deferred work: teardown now, Refresh posted.
					m_reslicingGroups = false;
					if (!m_isRefreshing && !m_isApplyingIncrementalChange)
					{
						var pendingRefresh = m_pendingRefresh;
						m_pendingRefresh = false;
						if (pendingRefresh)
						{
							try { ScheduleRefreshReplay(); } catch { }
						}
						CompleteDeferredHierarchyTeardown();
					}
					throw;
				}
			}
			finally
			{
				m_reslicingGroups = false;
			}
		}

		// Replay a deferred Refresh unless an outer Refresh/incremental pass will.
		if (!m_isRefreshing && !m_isApplyingIncrementalChange)
		{
			var pendingRefresh = m_pendingRefresh;
			m_pendingRefresh = false;
			if (pendingRefresh)
			{
				Refresh();
			}
			else
			{
				CompleteDeferredHierarchyTeardown();
			}
		}
	}

	private partial void CompleteDeferredHierarchyTeardown()
	{
		// An in-flight publication calls back here as it unwinds.
		if (!m_pendingHierarchyTeardown || m_isRefreshing || m_reslicingGroups)
		{
			return;
		}

		// Publication runs app code, so guard it like a rebuild; nested verbs coalesce or defer. On
		// failure the obligation stays for the next ClearParentBy/Refresh.
		var refreshAlreadyPending = m_pendingRefresh;
		{
			m_isRefreshing = true;
			try
			{
				TryCompleteDeferredHierarchyTeardown();
			}
			finally
			{
				m_isRefreshing = false;
			}
		}

		// Post (not run) a Refresh deferred during publication: often reached from an unwind.
		if (!refreshAlreadyPending && m_pendingRefresh && !m_isApplyingIncrementalChange)
		{
			m_pendingRefresh = false;
			try { ScheduleRefreshReplay(); } catch { }
		}
	}

	private partial void TryCompleteDeferredHierarchyTeardown()
	{
		// Done only once non-hierarchical metadata was actually published (a rebuild may stage a flat
		// kind then fail), or the relation was re-declared.
		if (!m_pendingHierarchyTeardown || m_parentKeySelector is not null || !m_hierarchyPublished)
		{
			m_pendingHierarchyTeardown = false;
			m_releasedHierarchyRows.Clear();
			return;
		}

		// The replacing rebuild failed: republish the previous rows without the tree. Reached from
		// unwinds, so nothing escapes; the flag stays set for one retry per later call (no loop).
		try
		{
			if (m_kind == ProjectionKind.GroupedHierarchical)
			{
				// Same headers and slices; only the hierarchy goes.
				ReleaseHierarchyProjection();
				m_kind = ProjectionKind.Grouped;
			}
			else if (m_kind == ProjectionKind.Hierarchical)
			{
				// Release captures the fallback rows while the tree is still alive; an earlier release
				// already captured them otherwise.
				ReleaseHierarchyProjection();
				var flatRows = m_releasedHierarchyRows;
				m_releasedHierarchyRows = new();
				InvalidateShapingState();
				ClearFlatRowIdentityTracking();
				m_rows.ReplaceAll(flatRows);
				// Degraded: later changes re-shape through Refresh.
				m_kind = ProjectionKind.Unshaped;
			}
			// Else a staged non-hierarchical projection failed to publish; republishing tears down.

			PublishProjection();
			m_pendingHierarchyTeardown = false;
			m_releasedHierarchyRows.Clear();
		}
		catch
		{
		}
	}

	private partial void PublishProjection()
	{
		// Marked "published" before the callback: a consumer may throw after swapping providers. Only
		// a completed non-hierarchical publication clears it.
		var hierarchical =
			m_kind == ProjectionKind.Hierarchical || m_kind == ProjectionKind.GroupedHierarchical;
		if (hierarchical)
		{
			m_hierarchyPublished = true;
		}

		RaiseProjectionRebuilt();

		if (!hierarchical)
		{
			m_hierarchyPublished = false;
		}
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

	public partial uint SourceRowCount()
	{
		if (m_hierarchicalAdapter is not null && m_hierarchicalAdapter.HasIndex())
		{
			return m_hierarchicalAdapter.NodeCount();
		}
		if (m_pipeline.HasFilter())
		{
			return m_refreshSourceRowCount;
		}
		return m_rows is not null ? (uint)m_rows.Count : 0;
	}

	private partial string Diagnostic(string text) => m_diagnosticName + ": " + text;

	private static partial ulong LiveShapingKeyFor(object? item)
	{
		if (item is null)
		{
			return 0;
		}

		return ObjectIdentityHelper.GetId(item);
	}

	private partial LiveShapeSnapshot CaptureLiveShapeSnapshot(
		object? item)
	{
		LiveShapeSnapshot snapshot = new();

		// A throwing selector counts as null, as in the rebuild, so it can't escape the app's setter.
		string keyOf(ShapingHelpers.KeySelector? selector)
		{
			if (selector is null)
			{
				return "";
			}
			try { return RowIdentity.StringifyKey(selector(item)); } catch { return RowIdentity.StringifyKey(null); }
		}

		// Called only with live shaping on, so every shape input is captured.
		foreach (var axis in m_pipeline.ActiveSortAxes(-1, -1))
		{
			snapshot.SortKeys.Add(keyOf(axis.Key));
		}

		if (m_groupSelector is not null)
		{
			// Compare by group identity, not display text (keys often print alike, hiding a regroup).
			object? groupKey = null;
			try { groupKey = m_groupSelector(item); } catch { }
			string? reason = null;
			if (TryGetGroupIdentity(groupKey, out var identity, ref reason))
			{
				snapshot.GroupKey = "id:" + identity;
			}
			else
			{
				try { snapshot.GroupKey = ShapingHelpers.MakeNodeKey(groupKey); } catch { }
			}
		}

		// Keyed as the tree keys it; display strings can collide and hide a reparent.
		if (m_parentKeySelector is not null)
		{
			string nodeKeyOf(ShapingHelpers.KeySelector? selector)
			{
				try { return ShapingHelpers.MakeNodeKey(selector!(item)); } catch { return ""; }
			}
			snapshot.NodeKey = nodeKeyOf(m_keySelector);
			snapshot.ParentKey = nodeKeyOf(m_parentKeySelector);
		}

		snapshot.PassesFilter = m_pipeline.PassesFilter(item);

		return snapshot;
	}

	private partial void InvalidateRetainedHierarchyStructure()
	{
		// Drops only the cache; an in-flight Refresh holds its own reference.
		m_hierarchyStructure = null;
	}

	private static partial bool LiveShapeSnapshotsDiffer(
		LiveShapeSnapshot left,
		LiveShapeSnapshot right)
	{
		return left.PassesFilter != right.PassesFilter ||
			left.GroupKey != right.GroupKey ||
			left.NodeKey != right.NodeKey ||
			left.ParentKey != right.ParentKey ||
			!left.SortKeys.SequenceEqual(right.SortKeys);
	}

	private partial void RefreshLiveShapingSubscriptions(
		List<object?> items)
	{
		if (!IsLiveShapingEnabled())
		{
			ClearLiveShapingSubscriptions();
			return;
		}

		// Mark-and-sweep: only arrivals subscribe and departures revoke, avoiding per-row COM churn.
		HashSet<ulong> live = new(items.Count);

		// Snapshots are rebuilt, not reconciled: the committed spec may have changed.
		Dictionary<ulong, LiveShapeSnapshot> snapshots = new(items.Count);

		// Comparing fresh edge keys with previous ones validates the retained structure for free,
		// even for edits without PropertyChanged. Arrivals are covered by the source change stamp.
		var edgeMoved = false;
		var checkEdges = m_parentKeySelector is not null && m_hierarchyStructure is not null;

		foreach (var item in items)
		{
			if (item is null)
			{
				continue;
			}

			var key = LiveShapingKeyFor(item);
			live.Add(key);
			m_liveShaping.Subscribe(item);
			var snapshot = CaptureLiveShapeSnapshot(item);
			snapshots[key] = snapshot;

			if (checkEdges && !edgeMoved)
			{
				// Object-keyed edges prove nothing (see IsObjectNodeKey); always rebuild.
				if (ShapingHelpers.IsObjectNodeKey(snapshot.NodeKey) || ShapingHelpers.IsObjectNodeKey(snapshot.ParentKey))
				{
					edgeMoved = true;
				}
				else if (m_liveShapeSnapshots.TryGetValue(key, out var previous))
				{
					edgeMoved = previous.NodeKey != snapshot.NodeKey || previous.ParentKey != snapshot.ParentKey;
				}
			}
		}

		if (edgeMoved)
		{
			InvalidateRetainedHierarchyStructure();
		}

		m_liveShaping.RetainOnly(live);
		m_liveShapeSnapshots = snapshots;
	}

	private partial void AddLiveShapingSubscription(object? item)
	{
		if (!IsLiveShapingEnabled() || item is null)
		{
			return;
		}

		m_liveShaping.Subscribe(item);
		m_liveShapeSnapshots[LiveShapingKeyFor(item)] = CaptureLiveShapeSnapshot(item);

		// A re-inserted item may still hold a rank from before it was edited while out of the source.
		if (m_liveItemChangedHook is not null)
		{
			m_liveItemChangedHook(item, true);
		}
	}

	private partial void RemoveLiveShapingSubscription(object? item)
	{
		// Not gated on IsLiveShapingEnabled: pruning leftovers is always correct. Gated on there being
		// anything to prune, so a source that never went live pays no identity QI per removal.
		if (item is null || (m_liveShaping.Empty() && m_liveShapeSnapshots.Count == 0))
		{
			return;
		}

		m_liveShaping.Unsubscribe(item);
		m_liveShapeSnapshots.Remove(LiveShapingKeyFor(item));
	}

	private partial void ApplyLiveShapingDelta(
		NotifyCollectionChangedEventArgs args)
	{
		if (!IsLiveShapingEnabled())
		{
			return;
		}

		void unsubscribeAll(IList? items)
		{
			if (items is null)
			{
				return;
			}
			for (var i = 0; i < items.Count; ++i)
			{
				RemoveLiveShapingSubscription(items[i]);
			}
		}

		void subscribeAll(IList? items)
		{
			if (items is null)
			{
				return;
			}
			for (var i = 0; i < items.Count; ++i)
			{
				AddLiveShapingSubscription(items[i]);
			}
		}

		switch (args.Action)
		{
			case NotifyCollectionChangedAction.Add:
				subscribeAll(args.NewItems);
				break;
			case NotifyCollectionChangedAction.Remove:
				unsubscribeAll(args.OldItems);
				break;
			case NotifyCollectionChangedAction.Replace:
				// Remove before add, so a replace that reuses the same object ends up subscribed.
				unsubscribeAll(args.OldItems);
				subscribeAll(args.NewItems);
				break;
			case NotifyCollectionChangedAction.Move:
				// Ordering only; subscriptions unaffected.
				break;
			case NotifyCollectionChangedAction.Reset:
			default:
				// Callers route resets to Refresh(), which reconciles.
				break;
		}
	}

	private partial void ClearLiveShapingSubscriptions()
	{
		m_liveShaping.UnsubscribeAll();
		m_liveShapeSnapshots.Clear();
		// Nothing tracked, so nothing live is pending: a posted restore finds the flag clear and
		// returns, and a later re-enable starts from a fresh restore.
		m_liveShapingDirty = false;
		m_liveShapingHeld = false;
		Volatile.Write(ref m_liveShapingChangeDropped.Value, 0);
	}

	private partial void ResubscribeLiveShapingFromSource()
	{
		if (!IsLiveShapingEnabled())
		{
			ClearLiveShapingSubscriptions();
			return;
		}

		RefreshLiveShapingSubscriptions(Materialize(m_source));
	}

	private partial void OnLiveShapedItemChanged(
		object? item,
		string propertyName)
	{
		if (!IsLiveShapingEnabled() || item is null)
		{
			return;
		}

		// A change the owner could not be told about off-thread: snapshots cannot reveal it, so force
		// the full restore, which re-reads every item.
		if (Interlocked.Exchange(ref m_liveShapingChangeDropped.Value, 0) != 0)
		{
			MarkLiveShapingDirtyForItem();
		}

		// Before the coalescing early-out: the owner must see every changed item.
		if (m_liveItemChangedHook is not null && m_liveItemChangedHook(item, m_liveShapingDirty))
		{
			MarkLiveShapingDirtyForItem();
			return;
		}

		// A full restore is already pending; skip snapshot capture so bulk edits cost one capture.
		// Still re-arm it: an earlier post may have been refused, failed, or parked behind a hold
		// that has since lifted.
		if (m_liveShapingDirty)
		{
			EnsureLiveShapingRestore();
			return;
		}

		// Discards changes no verb reads. Compare, don't store: the cached snapshot must keep matching
		// the projection until the restore runs.
		var key = LiveShapingKeyFor(item);
		if (m_liveShapeSnapshots.TryGetValue(key, out var existing) &&
			!LiveShapeSnapshotsDiffer(existing, CaptureLiveShapeSnapshot(item)))
		{
			return;
		}

		MarkLiveShapingDirtyForItem();
	}

	private partial void MarkLiveShapingDirty()
	{
		m_liveShapingDirty = true;
		EnsureLiveShapingRestore();
	}

	private partial void MarkLiveShapingDirtyForItem()
	{
		if (!m_liveShapingDirty && m_liveItemReshape is not null)
		{
			m_liveItemReshape();
		}
		MarkLiveShapingDirty();
	}

	private partial void EnsureLiveShapingRestore()
	{
		if (!m_liveShapingDirty || m_liveShapingRestorePosted)
		{
			return;
		}

		if (m_liveShapingHeld)
		{
			// A held restore normally waits for ResumeHeldLiveShaping. If the hold has since lifted
			// without one (the owner was destroyed mid-edit), re-post so live shaping does not stall.
			if (m_liveShapingHold is not null && m_liveShapingHold())
			{
				return;
			}
			m_liveShapingHeld = false;
		}

		// Posted: coalesces a turn's writes into one rebuild and stays out of the app's
		// PropertyChanged handler.
		PostLiveShapingRestore();
	}

	private partial void PostLiveShapingRestore()
	{
		if (m_liveShapingRestorePosted)
		{
			return;
		}

		if (m_ownerQueue is not null)
		{
			var weakThis = m_weakThis;
			// A refused post means the owner's queue is shutting down. Stay dirty rather than rebuild
			// inside the app's PropertyChanged; the next change re-arms it.
			m_liveShapingRestorePosted = m_ownerQueue.TryEnqueue(() =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						strongThis.m_liveShapingRestorePosted = false;
						try
						{
							strongThis.RestoreLiveShaping();
						}
						catch
						{
							// No app call to receive this; Refresh left the source dirty, so the
							// next live change retries.
						}
					}
				});
			return;
		}

		// The owner thread has no dispatcher at all (test host), so there is no later turn to post to.
		// This is the one case that restores inside the caller; contain failures as the posted path does.
		try
		{
			RestoreLiveShaping();
		}
		catch
		{
		}
	}

	private partial void RestoreLiveShaping()
	{
		if (!m_liveShapingDirty)
		{
			// Another refresh already recaptured every snapshot.
			return;
		}

		if (!IsLiveShapingEnabled())
		{
			// Live shaping turned off while in flight.
			m_liveShapingDirty = false;
			return;
		}

		if (m_liveShapingHold is not null && m_liveShapingHold())
		{
			// Moving rows would end the edit; stay dirty until the owner resumes.
			m_liveShapingHeld = true;
			return;
		}

		Refresh();

		// No verb ran, so notify UIA; rows may have regrouped or filtered, not just reordered.
		RaiseShapingChanged(false /* reorderOnly */);
	}

	public partial void ResumeHeldLiveShaping()
	{
		var held = m_liveShapingHeld;
		m_liveShapingHeld = false;
		if (!held)
		{
			return;
		}

		// Posted: caller is closing an edit, often inside app callbacks or layout. Skips the post when a
		// refresh in the meantime already satisfied the restore.
		EnsureLiveShapingRestore();
	}
}
