// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\ShapedItemsSource\ShapedItemsSource.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
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
	public GroupedSourceAdapter? GroupedAdapter() => m_groupedAdapter;
	// The selector every identity consumer must use. Derives identity from each item's object
	// identity, so shaping never depends on the app having a unique domain key.
	public ShapingHelpers.KeySelector IdentitySelector() => EffectiveIdentitySelector();

	public partial void Refresh();

	private partial void SubscribeToSourceCollectionChanges();
	private partial void UnsubscribeFromSourceCollectionChanges();
	private partial void OnSourceCollectionChanged();
	private partial void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs args);
	private partial void OnSourceVectorChanged(IVectorChangedEventArgs args);
	private partial void ApplyIncrementalChange(NotifyCollectionChangedEventArgs args);
	private partial void ApplyIncrementalVectorChange(IVectorChangedEventArgs args);
	private partial bool TryApplyIncrementalSortedChange(NotifyCollectionChangedEventArgs args);
	private partial void ApplyShapingChange();
	private partial bool TryApplyShapingDeltaInPlace(ShapingHelpers.ShapingDelta delta);
	private partial void InvalidateShapingState();
	private static partial List<object?> Materialize(object? source);
	private void ApplyFilter(List<object?> rows) => m_pipeline.ApplyFilter(rows);
	private void ApplySort(List<object?> rows, int afterOrder = -1, int beforeOrder = -1) => m_pipeline.ApplySort(rows, afterOrder, beforeOrder);
	private partial void RebuildFlat(List<object?> rows);
	private partial void RebuildGrouped(List<object?> rows);
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
	private partial bool HasActiveSort();
	private partial bool IsSourceMutable();
	private partial bool TryGetRequiredRowIdentity(object? item, out string identity, ref string? reason);
	private partial bool TryGetGroupIdentity(object? key, out string identity, ref string? reason);
	private partial void ClearFlatRowIdentityTracking();
	private partial void RebuildFlatRowIdentityTracking(List<object?> rows);
	private partial bool TryGetTrackedFlatRowIndex(string identity, ref uint index);
	private partial void ShiftTrackedFlatRowIndicesForInsert(uint insertedIndex);
	private partial void ShiftTrackedFlatRowIndicesForRemove(uint removedIndex);
	// Prefixes a caller-facing message with the consumer's diagnostic name.
	private partial string Diagnostic(string text);
	private partial ShapingHelpers.ShapingPipeline.SortedInsertPlacement SortedInsertPlacementFor(object? item);
	private partial bool TryGetSourceItemCount(ref uint count);
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
	// HasProjection is true; every non-Refresh mutation of m_rows clears it.
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
	// Guards re-entrant Refresh (a source notification arriving while a rebuild's ReplaceAll is
	// already mutating the projection).
	private bool m_isRefreshing;
	// Guards re-entrant incremental application: a synchronous VectorChanged handler that mutates
	// the source must not interleave a nested update against a half-updated projection.
	private bool m_isApplyingIncrementalChange;
	private bool m_pendingRefresh;
	private Dictionary<string, ShapedGroup> m_groupCache = new();
	private GroupedSourceAdapter? m_groupedAdapter;

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
}
