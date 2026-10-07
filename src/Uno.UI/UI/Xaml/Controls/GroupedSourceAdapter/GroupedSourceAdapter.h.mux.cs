// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\GroupedSourceAdapter\GroupedSourceAdapter.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Interop;
using Windows.Foundation.Collections;
using NotifyCollectionChangedEventHandler = System.Collections.Specialized.NotifyCollectionChangedEventHandler;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Single-responsibility grouped adapter.
//
// The adapter has ONE job: take the shaped grouped source and MATERIALIZE it into one flat list
// of rows — header, items, header, items — held in an ordinary IObservableVector. That vector
// can then be wrapped in an ItemsSourceView and handed to the ItemsRepeater by the control.
// TODO Uno: std::enable_shared_from_this becomes a plain class; weak_from_this() captures become WeakReference<GroupedSourceAdapter>.
internal sealed partial class GroupedSourceAdapter
{
	// UI-thread-affine: construct on a UI thread with a DispatcherQueue. Source notifications are
	// required to arrive on this thread and are applied synchronously; the queue is captured only
	// to assert that affinity in chk. Constructing off a UI thread throws RPC_E_WRONG_THREAD.
	// GroupedSourceAdapter();  (GroupedSourceAdapter.mux.cs)
	// ~GroupedSourceAdapter(); (GroupedSourceAdapter.mux.cs)

	// The flat row axis the repeater consumes: a single ItemsSourceView wrapping the materialized
	// observable vector. It is created ONCE over m_entries (whose object identity is stable for the
	// adapter's lifetime — Rebuild ReplaceAll's its contents and a single-group toggle splices them
	// in place, but the vector instance never changes), so the identity a consumer captures stays
	// valid across rebuilds — the same contract the computed adapter's Entries() had. Because the
	// wrapped object is a plain IObservableVector, the repeater reaches it through
	// InspectingDataSource, which reports no key mapping — this is the container-preservation cost
	// of this design.
	public ItemsSourceView? Entries() => m_entriesView;

	public object? Source() => m_source;
	public partial void Source(object? value);
	public partial void DetachSourceQuietly();

	// Per-group expand / collapse intent. The group OBJECT is translated to the model's stable
	// string key here; the model raises Changed, which rebuilds.
	public partial bool IsGroupExpanded(object? group);
	public partial void SetGroupExpanded(object? group, bool isExpanded);
	public partial void ExpandAll();
	public partial void CollapseAll();

	// The live group object for a declared key, so a control holding only a key can address a group.
	public partial object? ResolveLiveGroupByIdentity(string groupKey);

	private partial void Rebuild();
	private partial bool OnUiThread();
	private partial void AssertRebuildOnUiThread();
	private partial void OnExpansionChanged(ShapingHelpers.RowExpansionModel.Change change);

	// Incremental expand/collapse of a SINGLE group: flips the group's header slot and splices just
	// that group's data rows into/out of the flat projection, instead of dropping and re-realizing
	// every container via a full Rebuild + Reset. Returns false when the group/header can't be
	// resolved incrementally (mid-reshape, header not materialized, count desync); the caller then
	// falls back to Rebuild, which is authoritative. Bulk changes (ExpandAll/CollapseAll, baseline
	// moves) never take this path — they stay a single Reset.
	private partial bool TryApplyExpansionSplice(string intentKey, bool expand);

	private partial void AttachToSource();
	private partial void DetachFromSource();
	private partial void SubscribeToGroup(object? group);
	private partial void UnsubscribeFromAllGroups();

	private static partial string GetGroupIntentKey(object? group);

	private object? m_source;

	// THE flat projection. Materialized in full on every Rebuild via a single ReplaceAll; a single-
	// group expand/collapse splices that group's rows in place (SetAt/InsertAt/RemoveAt).
	// TODO Uno: typed as the concrete ObservableVector<T> (single_threaded_observable_vector) so ReplaceAll raises one Reset.
	private ObservableVector<object?> m_entries = new();

	// One ItemsSourceView over m_entries, created once (m_entries identity is stable for life).
	private ItemsSourceView? m_entriesView;

	// Expand/collapse intent, keyed by group identity string so it survives reshapes.
	private ShapingHelpers.RowExpansionModel m_expansion = new();

	// Key->group map of the last Rebuild's live groups, for ResolveLiveGroupByIdentity.
	private Dictionary<string, object?> m_liveGroupsByIdentityKey = new();
	private HashSet<string> m_ambiguousLiveGroupIdentityKeys = new();

	// Affinity assertion only (chk); the adapter never marshals. Teardown runs on the owning UI
	// thread because every strong owner (TableViewSource / TableView) is a ReferenceTracker whose
	// final_release marshals destruction there, so no revoke thread guard is needed.
	private WeakReference<DispatcherQueue>? m_uiQueue;

	// TODO Uno: a winrt::event_token becomes the subscribed handler itself; null is the empty token
	// (token.value == 0), and revoking it is `event -= token`.
	private object? m_attachedSourceForRevocation;
	private NotifyCollectionChangedEventHandler? m_outerCollectionChangedToken;
	private VectorChangedEventHandler<object?>? m_outerVectorChangedToken;
	private BindableVectorChangedEventHandler? m_outerBindableVectorChangedToken;

	private struct InnerGroupSubscription
	{
		public object? ItemsForRevocation;
		public NotifyCollectionChangedEventHandler? CollectionToken;
		public VectorChangedEventHandler<object?>? Token;
		public BindableVectorChangedEventHandler? BindableToken;
	}
	private List<InnerGroupSubscription> m_innerSubscriptions = new();

	// Simple re-entrancy guard: a change notification raised synchronously inside ReplaceAll must
	// not start a nested Rebuild. The re-entrant request is remembered and run once after unwind.
	// Plain bool, not atomic: the adapter is UI-thread-affine and every mutation asserts that.
	private bool m_rebuildInFlight;
	private bool m_pendingRebuild;
}

// TODO Uno: std::shared_ptr<GroupedSourceAdapter> is the class reference itself, so the alias is not ported.
// using GroupedSourceAdapterPtr = std::shared_ptr<GroupedSourceAdapter>;
