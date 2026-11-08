// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedItemsSource.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Uno.Disposables;
using Windows.Foundation.Collections;
using NotifyCollectionChangedEventArgs = System.Collections.Specialized.NotifyCollectionChangedEventArgs;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Layer 2 of the shaping stack: the LIVE PROJECTION.
//
// Layer 1 decides what a shape is and how to apply it to a vector of items. This class owns the
// shape that currently EXISTS -- the projected rows, the group buckets behind them, the identity
// index that makes a change locatable, and the subscriptions that keep all of it true as the
// underlying source mutates. It is the difference between "sort these items" and "stay sorted".
//
// It deliberately knows nothing about a control. It produces vectors and reports what kind of
// projection it produced; an owner above decides what a row means, how to present it, and what
// to cache against it. Everything that used to make this logic control-specific -- constructing
// an ItemsSourceView, minting a row-metadata provider, notifying a TableView that its cached
// projection went stale -- is now delivered through the three handlers below, so the same engine
// can back any consumer.
//
// Threading: UI-thread-affine after construction, the same contract every XAML items source has
// (ItemsRepeater's InspectingDataSource makes no thread check either). Shaping verbs, source
// notifications and projection mutation must all run on the owning thread. A source that raises
// change notifications from a background thread is app misuse and is not supported; no attempt is
// made to marshal. Marshaling was tried and removed -- deferring to a rebuild that re-reads the
// app's collection from the UI thread leaves the app's own collection racing anyway, so it bought
// an illusion of safety while forcing a silent drop path and an exception swallow.
// TODO Uno: std::enable_shared_from_this becomes a plain class; weak_from_this() captures become WeakReference<ShapedItemsSource>.
// The constructor ShapedItemsSource(object? source) and the commented-out destructor are in ShapedItemsSource.mux.cs;
// the remaining methods declared here are partial methods implemented there.
internal sealed partial class ShapedItemsSource
{
	// What the last rebuild actually produced -- the EFFECTIVE shape, not the requested one.
	// Consumers read this to decide how to interpret a row, so it must never report intent.
	//
	// Only ONE shaping request degrades: a source with no usable ROW identity degrades to
	// Unshaped, a plain 1:1 mirror (RebuildUnshapedRows). A GROUPING request does NOT degrade --
	// an unresolvable, unstable or colliding group identity throws hresult_invalid_argument out of
	// RebuildGroupedRows instead of quietly producing Flat. The asymmetry is deliberate: a row
	// identity the engine cannot derive is a property of the app's data that the app may not be
	// able to change, and an unshaped mirror still shows every row; a bad group identity comes
	// from the GroupBy(...) selector the app just wrote, and silently rendering ungrouped is a bug
	// an app ships without ever noticing.
	internal enum ProjectionKind
	{
		// No projection has been built yet.
		None,
		// A 1:1 mirror of the source, no shaping applied.
		Unshaped,
		// Filtered and/or sorted rows -- raw items, no group headers.
		Flat,
		// Group headers interleaved with their items, produced through the group adapter.
		Grouped,
		// A tree via the hierarchy adapter; no synthetic rows, a parent IS a data row.
		Hierarchical,
		// Both axes: ROOTS are bucketed into groups (headers at depth 0 only); descendants stay
		// under their parent.
		GroupedHierarchical,
	}

	// explicit ShapedItemsSource(object? source); (ShapedItemsSource.mux.cs)
	// ~ShapedItemsSource(); (ShapedItemsSource.mux.cs)

	// Subscribes to the source and builds the first projection. Separate from the constructor so
	// the owner can install its handlers first and therefore observe the very first projection.
	public partial void Start();

	// Fired whenever the projection vector has been replaced or re-shaped, i.e. whenever an owner
	// that caches anything derived from it must re-derive. Always fired on the UI thread.
	public void SetProjectionRebuiltHandler(Action? handler) => m_projectionRebuilt = handler;

	// Fired only when the projection KIND changed, which is the case where an owner's cached view
	// and row metadata describe a shape that no longer exists.
	public void SetShapeSwappedHandler(Action? handler) => m_shapeSwapped = handler;

	// Fired after a shaping verb rewrote the projection. `reorderOnly` distinguishes a pure
	// re-order, which preserves membership, from a change that may have altered which rows exist.
	public void SetShapingChangedHandler(Action<bool>? handler) => m_shapingChanged = handler;

	// -- shaping verbs --------------------------------------------------------------------
	// Filters are conjunctive. The untokenized overloads are the single-filter shorthand; the
	// tokenized ones let independent filter sources (a column filter and a search box, say) be
	// declared and retracted without knowing about each other.
	public partial void SetFilter(ShapingHelpers.Predicate? predicate);
	public partial void SetFilter(string axisToken, ShapingHelpers.Predicate? predicate);
	public partial void ClearFilter();
	public partial void ClearFilter(string axisToken);
	public partial void SetGroup(
		ShapingHelpers.KeySelector? key,
		RowIdentity.IdentitySelector? groupIdentitySelector);
	public partial void ClearGroup();

	// Declares a self-referencing hierarchy: `key` is each item's unique identity, `parentKey` its
	// parent's (null/empty/unknown = root). Invalid data (duplicate/null key, self-parent, cycle)
	// throws E_INVALIDARG, leaving the previous projection. Re-declaring resets expansion intent.
	// With grouping, the group key buckets ROOTS only.
	public partial void SetParent(ShapingHelpers.KeySelector? key, ShapingHelpers.KeySelector? parentKey);
	public partial void ClearParentBy();
	public partial void SetSort(
		string previousAxisToken,
		string axisToken,
		ShapingHelpers.KeySelector? key,
		object? keyIdentity,
		string sortMemberPath,
		SortDirection direction);
	public partial void ClearSorts();
	public partial void ClearSort(string axisToken);
	// Drops every sort axis except axisToken. Lets a consumer that owns ONE axis assert itself as
	// the only sort without having to know the tokens of axes it did not declare.
	public partial void ClearSortsExcept(string axisToken);

	// What an active sort axis looks like from outside the engine. Enough for a consumer to tell
	// an axis it declared from one it did not, and to say which property a foreign axis sorts on.
	internal struct ActiveSortAxisInfo
	{
		public ActiveSortAxisInfo()
		{
		}

		public string AxisToken = "";
		// Empty when the axis was declared with a delegate no property path expresses.
		public string SortMemberPath = "";
		public SortDirection Direction;
	}

	// The active sort axes in precedence order (index 0 is the primary sort). An untokenized axis
	// reports an empty token, so a consumer can tell "an axis I do not own exists" from "only mine
	// exists".
	public partial List<ActiveSortAxisInfo> ActiveSortAxisInfos();
	public partial bool HasActiveSort();
	// Whether a clear verb has anything to clear, so an owner can tell a no-op clear from a real one.
	public bool HasFilter() => m_pipeline.HasFilter();
	// The app predicates and key selectors that threw since the tally was last swapped; puts `faults` in its place.
	public ShapingHelpers.SwallowedFaults ExchangeSwallowedFaults(ShapingHelpers.SwallowedFaults faults)
	{
		// TODO Uno: SwallowedFaults is a shared class instance, so std::exchange copies the values out and in.
		var tally = m_pipeline.SwallowedFaultsTally();
		ShapingHelpers.SwallowedFaults previous = new() { count = tally.count, firstError = tally.firstError };
		tally.count = faults.count;
		tally.firstError = faults.firstError;
		return previous;
	}
	public bool HasGroup() => m_groupSelector is not null;
	public bool HasParentBy() => m_parentKeySelector is not null;
	public bool HasSortAxis(string axisToken) => m_pipeline.HasSortAxis(axisToken);
	public bool HasSortOtherThan(string axisToken) => m_pipeline.HasSortOtherThan(axisToken);
	public bool HasMatchingSort(string axisToken, object? keyIdentity)
	{
		return m_pipeline.HasMatchingSort(axisToken, keyIdentity);
	}

	// -- projection -----------------------------------------------------------------------
	// Name this engine uses to prefix caller-facing diagnostics. Layer 2 must not hardcode a
	// layer-4 type name, but the messages are contractual for apps that already ship against
	// TableViewSource, so the consumer supplies its own name instead of the text changing.
	public void DiagnosticName(string value) => m_diagnosticName = value;
	public string DiagnosticName() => m_diagnosticName;
	public ProjectionKind Kind() => m_kind;
	public bool IsProjectedAsGrouped() => m_projectedAsGrouped;
	// The flat shaped row vector: the presented row axis for flat and unshaped projections. Under
	// a Grouped projection the presented row axis is the GroupedSourceAdapter's computed
	// ItemsSourceView instead, but this vector is still maintained as the flat shaped projection
	// (group order, headers excluded) so Rows() stays coherent regardless of grouping.
	public IObservableVector<object?> Rows() => m_rows;
	// The source's rows before the filter (every node for a tree), without reading the source: a filtered
	// projection rebuilds on every source change, so the count its last Refresh read is current, and without a
	// filter Rows() holds every source row.
	public partial uint SourceRowCount();
	public GroupedSourceAdapter? GroupedAdapter() => m_groupedAdapter;
	public HierarchicalSourceAdapter? HierarchicalAdapter() => m_hierarchicalAdapter;
	// The selector every identity consumer must use. Derives identity from each item's object
	// identity, so shaping never depends on the app having a unique domain key.
	public ShapingHelpers.KeySelector IdentitySelector() => EffectiveIdentitySelector();

	public partial void Refresh();

	// False when a shaped projection could not identify every row (a null item, or one object on
	// two rows), so a shaping verb that would throw from Refresh can be refused up front.
	public partial bool CanProjectWithRowIdentity();
	// One switch for sort keys, group key, filter result and hierarchy edge together.
	public partial void SetLiveShaping(bool enabled);
	public bool IsLiveShapingEnabled() => m_liveShapingEnabled;

	// Lets the owner hold live restores (e.g. while a cell editor is open). A held restore stays
	// dirty and runs the turn after ResumeHeldLiveShaping.
	public void SetLiveShapingHold(Func<bool>? isHeld) => m_liveShapingHold = isHeld;
	public partial void ResumeHeldLiveShaping();

	// Runs per live item change before snapshot comparison, so the owner can refresh state the
	// selectors read (comparer ranks). Arg: restore already posted. True forces a reshape. Also
	// runs for each arriving item, and once with a null item when live shaping turns on (every
	// item may have changed untracked).
	public void SetLiveItemChangedHook(Func<object?, bool, bool>? hook) => m_liveItemChangedHook = hook;

	// Lets the owner drop per-row state (comparer ranks) for rows that left the source: called with the
	// removed item after an in-place sorted removal, or with null and every source row after a rebuild.
	public void SetRowsReleasedHook(Action<object?, List<object?>>? hook) => m_rowsReleasedHook = hook;

	// Lets the owner learn that live shaping is reacting to the app's items: called when an item change leaves the
	// projection stale, at most once until the projection is next rebuilt, and not when live shaping turns on.
	public void SetLiveItemReshapeHandler(Action? handler) => m_liveItemReshape = handler;

	private partial void SubscribeToSourceCollectionChanges();
	private partial void UnsubscribeFromSourceCollectionChanges();
	private partial void OnSourceCollectionChanged();
	private partial void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs args);
	private partial void OnSourceVectorChanged(IVectorChangedEventArgs args);
	private partial void ApplyIncrementalChange(NotifyCollectionChangedEventArgs args);
	private partial void ApplyIncrementalVectorChange(IVectorChangedEventArgs args);
	private partial bool TryApplyIncrementalSortedChange(NotifyCollectionChangedEventArgs args);
	private partial bool TryReshapeStaleSortedFlatProjection();
	private partial bool IsFlatProjectionSorted();
	private partial bool IsFlatRowInSortOrderWithNeighbours(uint index);
	private partial void TrackFlatItemPropertyChanged(object? item);
	private partial void UntrackFlatItemPropertyChanged(object? item);
	private partial void ClearFlatItemPropertyChangedTracking();
	private partial void RebuildFlatItemPropertyChangedTracking(List<object?> rows);
	// Single owner for sort-key staleness: live shaping already subscribes to every item and
	// re-shapes on any shaping-relevant edit, so the sort-key subscriptions would be a second
	// handler per row observing the same change.
	private bool ShouldTrackSortKeyChanges() => HasActiveSort() && !IsLiveShapingEnabled();
	private partial void ClearSortDirtyRows();
	private partial void OnFlatItemPropertyChanged(ulong itemAddress, string propertyName);
	private partial void UpdateSortKeyPropertyFilter();
	private partial void ApplyShapingChange();
	private partial bool TryApplyShapingDeltaInPlace(ShapingHelpers.ShapingDelta delta, bool allowDuringIncrementalChange = false);
	private partial void InvalidateShapingState();
	// Reads the caller's source (CollectionOwner::Caller). Only ever given m_source.
	private static partial List<object?> Materialize(object? source);
	private void ApplyFilter(List<object?> rows) => m_pipeline.ApplyFilter(rows);
	private void ApplySort(List<object?> rows, int afterOrder = -1, int beforeOrder = -1) => m_pipeline.ApplySort(rows, afterOrder, beforeOrder);
	private partial void RebuildFlat(List<object?> rows);
	private partial void RebuildGrouped(List<object?> rows);
	private partial void RebuildHierarchical(List<object?> rows, ShapingHelpers.ParentStructure? structure);
	// Both axes: buckets the roots, one adapter segment and one group per bucket.
	private partial void RebuildGroupedHierarchical(List<object?> rows, ShapingHelpers.ParentStructure? structure);

	// Builds the index over UNFILTERED rows (builds and retains `structure` if null). Throws
	// E_INVALIDARG before mutating anything. Returns null and queues a Refresh when a selector
	// changed the relation mid-build; caller then publishes nothing.
	private partial ShapingHelpers.ParentKeyIndex? BuildHierarchyIndex(
		List<object?> rows,
		ShapingHelpers.ParentStructure? structure,
		bool sortRoots,
		ulong declarationGeneration);

	// Retained structure if it still describes `rows` (same declaration, no source change, same
	// rows/order, keys and parents unchanged per live snapshots or re-run selectors); else null.
	private partial ShapingHelpers.ParentStructure? TryReuseHierarchyStructure(
		List<object?> rows,
		ulong declarationGeneration,
		ulong sourceChangeStamp);
	// The tree structure last built, and what it was built from.
	private ShapingHelpers.ParentStructure? m_hierarchyStructure;
	private ulong m_hierarchyStructureGeneration;
	private ulong m_hierarchyStructureSourceStamp;
	// Bumped by every source change notification, including ones deferred behind a rebuild.
	private ulong m_sourceChangeStamp;
	// Stamp captured when the current Refresh materialized the source.
	private ulong m_refreshSourceStamp;
	// The rows the last Refresh materialized, before the filter (SourceRowCount).
	private uint m_refreshSourceRowCount;
	// Stamp whose rows the last successful shaped Refresh validated; never a real stamp until then.
	private ulong m_validatedSourceStamp = ulong.MaxValue;

	// Creates the adapter if needed, applies any pending intent reset, and hands it the index.
	private partial void PublishHierarchyIndex(ShapingHelpers.ParentKeyIndex index, List<int> rootSegments);

	private partial List<object?> VisibleHierarchicalRows();
	// Every row of the last good hierarchy structure, flat-filtered: the failed-teardown fallback.
	private partial List<object?> HierarchyFallbackRows();

	// Re-derives group Items from the hierarchy adapter; also on node toggles, since nothing else
	// tells the grouped adapter the tree changed.
	private partial void ResliceGroupsFromHierarchy();

	// Root -> bucket, by object identity.
	private Dictionary<object, int> m_rootBucketIndex = new(ReferenceEqualityComparer.Instance);
	private List<ShapedGroup> m_hierarchyGroups = new();

	// Tears down the hierarchical projection and adapter. Every non-hierarchical rebuild calls it,
	// else the adapter keeps re-slicing groups and pinning rows.
	private partial void ReleaseHierarchyProjection();
	// Completes a ClearParentBy deferred by an in-flight publication, when it unwinds.
	private partial void CompleteDeferredHierarchyTeardown();
	// Cleared only once non-hierarchical metadata is actually published.
	private bool m_pendingHierarchyTeardown;
	// Consumer may hold hierarchical row metadata. Separate from m_kind, which is set BEFORE
	// publishing.
	private bool m_hierarchyPublished;
	// Raises ProjectionRebuilt for the staged m_kind and maintains m_hierarchyPublished.
	private partial void PublishProjection();
	// One completion attempt; CompleteDeferredHierarchyTeardown guards it.
	private partial void TryCompleteDeferredHierarchyTeardown();
	// The visible rows of a released, ungrouped tree while its teardown is owed.
	private List<object?> m_releasedHierarchyRows = new();
	// Drops the adapter's collapse overrides on filter context rows. Called by every filter verb.
	private partial void ResetHierarchyFilterOverlay();
	// Re-entrancy guard for the re-slice; Refresh defers while set.
	private bool m_reslicingGroups;
	// Bumped when m_hierarchyGroups changes, so an in-flight re-slice detects staleness.
	private ulong m_hierarchyGeneration;
	private partial void RebuildUnshapedRows(List<object?> rows, string? reason);
	private partial bool IsIdentityRequired();
	// True when any of Filter / Sort / GroupBy is in force. Distinct from IsIdentityRequired,
	// which is also true for a merely mutable source: a mutable source with no verbs still wants
	// no identity, because there is no projection to anchor.
	private partial bool HasAnyShapingVerb();
	// Every identity consumer funnels through here. Identity comes from each item's object
	// identity, so a row ALWAYS has one and no shaping verb has to refuse to run for want of one.
	private ShapingHelpers.KeySelector EffectiveIdentitySelector() => m_intrinsicKeySelector;
	// Confirm every row in the set a rebuild is about to publish has a usable, distinct identity.
	private partial bool ValidateRowIdentities(List<object?> rows, ref string? reason);
	private partial bool IsSourceMutable();
	private partial bool TryGetRequiredRowIdentity(object? item, out string identity, ref string? reason);
	private partial bool TryGetGroupIdentity(object? key, out string identity, ref string? reason);
	// Buckets by m_groupSelector; throws E_INVALIDARG on an invalid group identity.
	private partial List<ShapingHelpers.KeyedBucket> BucketizeGroupsOrThrow(List<object?> items);
	// Finds or creates the cached group for the bucket and sets its GroupKey.
	private partial ShapedGroup AcquireCachedGroup(ShapingHelpers.KeyedBucket bucket);
	private partial void PruneGroupCache(HashSet<string> liveKeys);
	// Loads m_groupSource and (re-)attaches the grouped adapter, publishing one Reset.
	private partial void AttachGroupSource(List<object?> groups);
	private partial void ReattachGroupedAdapterAfterFailedRefill();
	// Detaches the grouped adapter, then clears m_groupSource and m_groupCache.
	private partial void ReleaseGroupedProjection();
	private partial void ClearFlatRowIdentityTracking();
	private partial void RebuildFlatRowIdentityTracking(List<object?> rows);
	private partial bool TryGetTrackedFlatRowIndex(string identity, ref uint index);
	private partial void ShiftTrackedFlatRowIndicesForInsert(uint insertedIndex);
	private partial void ShiftTrackedFlatRowIndicesForRemove(uint removedIndex);
	// Prefixes a caller-facing message with the consumer's diagnostic name.
	private partial string Diagnostic(string text);
	private partial bool TryGetSourceItemCount(ref uint count);
	// TODO Uno: a class rather than a struct; a snapshot is immutable once captured, and the dictionary shares it.
	private sealed class LiveShapeSnapshot
	{
		public List<string> SortKeys = new();
		public string GroupKey = "";
		// Hierarchy edge, when a ParentBy relation is declared. A change posts a live reparent and
		// invalidates the retained structure.
		public string NodeKey = "";
		public string ParentKey = "";
		public bool PassesFilter = true;
	}
	private partial LiveShapeSnapshot CaptureLiveShapeSnapshot(object? item);
	// Drops the retained tree structure so the next Refresh re-reads every key and parent key.
	private partial void InvalidateRetainedHierarchyStructure();
	private static partial bool LiveShapeSnapshotsDiffer(
		LiveShapeSnapshot left,
		LiveShapeSnapshot right);
	// TODO Uno: void const* becomes the id ObjectIdentityHelper hands out per live object.
	private static partial ulong LiveShapingKeyFor(object? item);
	// Mark-and-sweep reconcile; surviving items keep their subscription.
	private partial void RefreshLiveShapingSubscriptions(List<object?> items);
	private partial void ClearLiveShapingSubscriptions();
	private partial void ResubscribeLiveShapingFromSource();
	// O(1) per-item delta for incremental paths.
	private partial void AddLiveShapingSubscription(object? item);
	private partial void RemoveLiveShapingSubscription(object? item);
	// Subscription delta driven by the args, not the projection, so every apply path is covered.
	private partial void ApplyLiveShapingDelta(NotifyCollectionChangedEventArgs args);
	private partial void OnLiveShapedItemChanged(
		object? item,
		string propertyName);
	// Marks stale and posts ONE restore; N changes in a turn cost one reshape.
	private partial void MarkLiveShapingDirty();
	// MarkLiveShapingDirty for an item change; the first since the last rebuild also notifies the owner.
	private partial void MarkLiveShapingDirtyForItem();
	// Posts a restore for a dirty projection unless one is queued or the hold still applies.
	private partial void EnsureLiveShapingRestore();
	// Posts RestoreLiveShaping to the owner's queue. Runs it now only when the owner has no
	// dispatcher at all (test host); a refused post leaves it dirty for the next change.
	private partial void PostLiveShapingRestore();
	private partial void RestoreLiveShaping();
	private void RaiseProjectionRebuilt()
	{
		if (m_projectionRebuilt is not null)
		{
			m_projectionRebuilt();
		}
	}
	private void RaiseShapeSwapped()
	{
		if (m_shapeSwapped is not null)
		{
			m_shapeSwapped();
		}
	}
	private void RaiseShapingChanged(bool reorderOnly)
	{
		if (m_shapingChanged is not null)
		{
			m_shapingChanged(reorderOnly);
		}
	}

	// Single authoritative source. Filtering, sorting, and grouping derive the projection without
	// mutating it.
	private object? m_source;
	// Per-object identity, derived from each item's canonical IUnknown pointer. Built once and
	// retained rather than minted per call so the selector's own identity is stable, and so the
	// cost is paid once instead of on every row projection.
	private ShapingHelpers.KeySelector m_intrinsicKeySelector = RowIdentity.MakeObjectIdentitySelector();
	// The recipe: which verbs are in force and in what order. This class keeps the projection.
	private ShapingHelpers.ShapingPipeline m_pipeline = new();
	// Retained layer-1 projection state for the FLAT path: the post-filter rows in source order
	// plus the shaped output. Holding FilteredSource is what makes an in-place re-sort produce
	// exactly what a full rebuild would -- a stable sort seeded from the previously sorted order
	// would break ties in the OLD sort's order instead of source order. Only valid while
	// HasProjection is true. The sorted incremental path keeps it in lockstep with m_rows; every
	// other non-Refresh mutation of m_rows clears it.
	private ShapingHelpers.ShapingState m_shapingState = new();
	private ShapingHelpers.KeySelector? m_groupSelector;
	private RowIdentity.IdentitySelector? m_groupIdentitySelector;
	private ProjectionKind m_kind;
	private bool m_projectedAsGrouped;
	private string m_diagnosticName = "ShapedItemsSource";
	// TODO Uno: typed as the concrete ObservableVector<T> (single_threaded_observable_vector) so ReplaceAll raises one Reset.
	private readonly ObservableVector<object?> m_rows;
	private ObservableVector<object?>? m_groupSource;
	// Identities of the rows currently in the flat projection, and each one's index in it.
	// Maintained incrementally so the sorted fast-path can detect duplicate/empty identities and
	// locate a removed row without an O(n) WinRT ABI scan.
	private HashSet<string> m_flatRowIdentities = new();
	private Dictionary<string, uint> m_flatRowIdentityToIndex = new();
	// Sort-key changes raise no collection notification, so while sorted, INPC rows are observed
	// and a change to a sort-key property marks the row dirty for the next collection change.
	// Keyed by IUnknown address (stable across re-orders); holding Item keeps the address unique.
	private sealed class FlatItemPropertyChangedSubscription
	{
		public object? Item;
		public IDisposable? Revoker;

		// Uno specific: destroying the C++ entry runs its auto-revoker; here the revoke is explicit.
		public void Revoke()
		{
			var revoker = Revoker;
			Revoker = null;
			try
			{
				revoker?.Dispose();
			}
			catch
			{
			}
		}
	}
	private Dictionary<ulong, FlatItemPropertyChangedSubscription> m_flatItemPropertyChangedRevokers = new();
	private bool m_flatItemPropertyChangedTrackingActive;
	private bool m_flatItemsMayChangeSortWithoutNotification;
	// First path segment of each active sort axis' SortMemberPath.
	private HashSet<string> m_sortKeyPropertyNames = new();
	// Some axis has no path (delegate key / comparer), so any property may affect the sort.
	private bool m_anyPropertyMayAffectSort;
	// Owner thread only: off-thread PropertyChanged is marshalled before it reaches these.
	private HashSet<ulong> m_sortDirtyRows = new();
	private bool m_sortDirtyOverflow;
	private bool m_raiseReorderAfterIncrementalChange;
	// Bumped by every rebuild and every invalidation of the retained shaping state. The sorted
	// incremental path plans indices, then calls into app code (sort keys, comparers, m_rows
	// listeners) before splicing; a changed value means those indices describe a projection that
	// no longer exists.
	private ulong m_projectionGeneration;
	// Guards re-entrant Refresh (a source notification arriving while a rebuild's ReplaceAll is
	// already mutating the projection). Also held while a deferred hierarchy teardown publishes, so
	// requests from that publication's handlers are deferred the same way.
	private bool m_isRefreshing;
	// Guards re-entrant incremental application: a synchronous VectorChanged handler that mutates
	// the source must not interleave a nested update against a half-updated projection.
	private bool m_isApplyingIncrementalChange;
	private bool m_pendingRefresh;
	// Coalesces posted replays of a Refresh requested during a pass that threw.
	private partial void ScheduleRefreshReplay();
	// Posts a pending Refresh after a failed incremental application; keeps it if nothing posted.
	private partial void PostPendingRefresh();
	private bool m_refreshReplayScheduled;
	private Dictionary<string, ShapedGroup> m_groupCache = new();
	private GroupedSourceAdapter? m_groupedAdapter;
	private readonly LiveShapingTracker m_liveShaping = new();
	// Owning thread (m_rows is bound to it); PropertyChanged is always handled here. Null off a
	// dispatcher thread: changes then run inline on the owner thread, flagged on others.
	private readonly DispatcherQueue? m_ownerQueue;
	// TODO Uno: DWORD from GetCurrentThreadId becomes Environment.CurrentManagedThreadId.
	private readonly int m_ownerThreadId;
	// Set off-thread when a PropertyChanged could not be marshalled to the owner; the owner's next
	// live change forces a full restore. Shared so the handler never has to lock `this` off-thread.
	// TODO Uno: shared_ptr<atomic<bool>> becomes a shared box holding 0 or 1, read and written with Volatile/Interlocked.
	private readonly StrongBox<int> m_liveShapingChangeDropped = new(0);
	private bool m_liveShapingEnabled;
	private Dictionary<ulong, LiveShapeSnapshot> m_liveShapeSnapshots = new();
	// Pending live reshape: the projection may not reflect the captured snapshots.
	private bool m_liveShapingDirty;
	// A restore is queued on m_ownerQueue (so it implies m_ownerQueue is set). Separate from dirty
	// so a refused post or a failed restore is retried by the next change instead of stranding the
	// dirty flag.
	private bool m_liveShapingRestorePosted;
	private Func<bool>? m_liveShapingHold;
	// A restore found the hold on and parked; ResumeHeldLiveShaping re-posts it if still dirty.
	// Set only while dirty, but a refresh may clear dirty first, so it alone does not imply dirty.
	private bool m_liveShapingHeld;
	private Func<object?, bool, bool>? m_liveItemChangedHook;
	private Action<object?, List<object?>>? m_rowsReleasedHook;
	private Action? m_liveItemReshape;

	// Both set or both null; m_parentKeySelector is the "hierarchy declared" test.
	private ShapingHelpers.KeySelector? m_keySelector;
	private ShapingHelpers.KeySelector? m_parentKeySelector;
	// Bumped by SetParent/ClearParentBy; an index build that sees it move discards its result.
	private ulong m_parentDeclarationGeneration;
	// Set by SetParent, consumed by the next publish: re-declaring the relation clears intent.
	private bool m_parentRelationRedeclared;
	// Set by parent verbs, consumed by ApplyShapingChange: the pipeline spec diff cannot see the
	// hierarchy axis. Survives a shaping batch.
	private bool m_hierarchyAxisDirty;
	private HierarchicalSourceAdapter? m_hierarchicalAdapter;

	private Action? m_projectionRebuilt;
	private Action? m_shapeSwapped;
	private Action<bool>? m_shapingChanged;

	// Resolved once per bound source: what shape the source is and how to read it. Every indexed
	// read, count and observability question in this class goes through it, so no two of them can
	// disagree about what the source supports.
	private ShapingHelpers.CollectionAccessor m_sourceAccessor;

	private readonly SerialDisposable m_sourceCollectionChangedRevoker = new();
	private readonly SerialDisposable m_sourceVectorChangedRevoker = new();
	private readonly SerialDisposable m_sourceBindableVectorChangedRevoker = new();

	// Uno specific: weak_from_this() allocates nothing in C++, so one reference is shared rather than one per row.
	private readonly WeakReference<ShapedItemsSource> m_weakThis;
}
