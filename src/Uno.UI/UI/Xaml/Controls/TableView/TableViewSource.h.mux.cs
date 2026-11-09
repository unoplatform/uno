// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// The control-facing face of the shaping stack: a fluent verb surface, and the one place that
// decides what a projected row MEANS to a TableView.
//
// It owns no projection. Filtering, sorting, grouping, source subscriptions, incremental change
// application, and identity tracking all live in ShapedItemsSource below it; this class
// translates public WinRT delegates into that engine's vocabulary, turns the shape the engine
// produced into an ItemsSourceView plus a row-metadata provider, and tells the owning TableView
// when the projection it cached is no longer the one being shown.
//
// Reference-tracked (like ItemsRepeater's ItemsSourceView) rather than a plain projected class.
// This is the standard WinUI mechanism for a WinRT type that holds references to app-supplied
// (possibly managed) objects: ReferenceTracker gives two things this type needs --
//   1. final_release marshals the delete to the DispatcherQueue captured at construction, so the
//      whole teardown cascade (~TableViewSource -> m_engine drop -> ~ShapedItemsSource -> revoke)
//      always runs on the owning UI thread even when the CLR finalizes this source off-thread.
//      That is why ShapedItemsSource needs no thread guard of its own.
//   2. tracker_ref members let the GC walk this native object's strong refs to managed objects and
//      collect reference cycles.
// Costs: abi_enter enforces UI-thread affinity on every projected call, and each instance composes
// a DependencyObject inner. Both are acceptable for a UI-thread-affine items source.
partial class TableViewSource
{
	// No parameterless constructor: TableViewSource is not activatable in IDL (only the static
	// From(items) factory is projected). Instances are created via winrt::make with the items ctor.
	// ~TableViewSource();
	// explicit TableViewSource(winrt::IInspectable const& items);

	// static winrt::TableViewSource From(winrt::IInspectable const& items);

	// Single, always-first predicate: each Filter() replaces the previous predicate and runs
	// before sort/group shaping.
	// winrt::TableViewSource Filter(winrt::TableViewPredicate const& predicate);
	// winrt::TableViewSource GroupBy(winrt::TableViewKeySelector const& key);
	// winrt::TableViewSource GroupBy(winrt::TableViewKeySelector const& key, winrt::TableViewIdentitySelector const& groupIdentitySelector);
	// winrt::TableViewSource ParentBy(winrt::TableViewKeySelector const& keySelector, winrt::TableViewKeySelector const& parentKeySelector);
	// winrt::TableViewSource Sort(winrt::TableViewKeySelector const& key, winrt::SortDirection direction);
	// winrt::TableViewSource Sort(winrt::hstring const& sortMemberPath, winrt::SortDirection direction);
	// winrt::TableViewSource SortReplacing(winrt::hstring const& previousSortAxisToken, winrt::hstring const& sortAxisToken, winrt::TableViewKeySelector const& key, winrt::hstring const& sortMemberPath, winrt::SortDirection direction);
	// winrt::TableViewSource ClearFilter();
	// winrt::TableViewSource ClearGroupBy();
	// winrt::TableViewSource ClearParentBy();
	// winrt::TableViewSource ClearSort();
	// winrt::TableViewSource ClearSort(winrt::hstring const& sortAxisToken);
	// Internal, for the owning control: make sortAxisToken the ONLY sort axis, dropping any the
	// app declared through the fluent Sort verb (which is untokenized and so unaddressable by
	// token). Keeps the control's single-axis contract true when both front-ends are used.
	// winrt::TableViewSource ClearSortsExcept(winrt::hstring const& sortAxisToken);
	// Internal: the active sort axes in precedence order, so the owner can detect an axis it does
	// not own and, when that axis names a property, attribute it to a column. Restated here
	// rather than surfacing the engine's own struct: this class is the translation layer, and
	// layer 2 stays out of the control's headers.
	internal struct ActiveSortAxisInfo
	{
		public string AxisToken = "";
		// Empty when the axis was declared with a key selector no property path expresses.
		public string SortMemberPath = "";
		public SortDirection Direction = SortDirection.None;

		// TODO Uno: Needed so the field initializers run (a winrt::hstring member defaults to empty, not null).
		public ActiveSortAxisInfo()
		{
		}
	}
	// std::vector<ActiveSortAxisInfo> ActiveSortAxisInfos() const;

	// The axis token a path-declared sort owns. Exposed so the owning control can recognize its
	// own path-based axis, and so re-sorting one path replaces it instead of stacking.
	// static winrt::hstring SortAxisTokenForPath(winrt::hstring const& sortMemberPath);

	// bool IsLiveShaping() const;
	// void IsLiveShaping(bool value);

	// UI-thread affine after construction/binding: shaping verbs and projection mutation must
	// run on the owning UI thread. Only source change notifications are marshaled back here.
	// winrt::ItemsSourceView GetItemsSourceView();
	// TableViewRowMetadataProvider GetRowMetadata();
	// True when the source is ACTUALLY projected as grouped rows. This is the effective shape,
	// not the caller's intent: grouping is only in effect once a GroupBy selector produced a
	// usable projection, and until then the rows are raw items, not GroupedEntry. Consumers use
	// this to decide how to interpret a row DataContext, so reporting intent here would make them
	// read a flat item as a GroupedEntry.
	// bool IsGrouped() const;
	// What the app declared on this source, not the projection in effect (unlike IsGrouped), read without calling
	// app code (TableView_Instance).
	// bool HasFilter() const;
	// bool HasGroupBy() const;
	// bool HasParentBy() const;
	// Rows before the filter (every node for a tree), and data rows after it, without group headers and whatever is
	// expanded. Neither reads app code.
	// uint32_t SourceRowCount() const;
	// uint32_t DataRowCount() const;
	// True when `item` (by object identity) is in the shaped, filtered rows, regardless of group or
	// node expansion. Header rows never match.
	// bool ContainsShapedItem(winrt::IInspectable const& item) const;

	// Internal: the owning TableView caches the projection (ItemsSourceView, row metadata and
	// grouped-ness) when it binds, so it must be told when a shaping verb swaps that projection
	// for a different one. Held weakly - the owner holds this source via ItemsSource.
	//
	// The owner is taken as IInspectable because only its IDENTITY matters here, for the
	// single-owner check below. Naming TableView would invert the dependency (rule 5): this type
	// sits below the control in the shaping stack. For the same reason the owner INSTALLS the two
	// handlers below rather than being called back into by name - the same shape as the handlers
	// this class installs on its own engine.
	//
	// Returns false, and leaves the existing owner bound, when a different live TableView owns it.
	// bool SetOwningTableView(winrt::IInspectable const& owner);
	// Detaches `owner`; a no-op when another TableView owns this source.
	// void ReleaseOwningTableView(winrt::IInspectable const& owner);
	// True when a TableView other than `candidate` owns this source.
	internal bool IsOwnedByOther(object? candidate)
	{
		var owner = GetOwningTableView();
		return owner is not null && !ReferenceEquals(owner, candidate);
	}
	// A refused TableView waits here. On release, waiters run in order until one claims the
	// source; each waiter that runs is dropped. Keyed by the waiter's identity, one entry each.
	// void AddOwnerReleasedHandler(winrt::IInspectable const& waiter, std::function<void()> handler);
	// void RemoveOwnerReleasedHandler(winrt::IInspectable const& waiter);
	// Raised after a rebuild changed the projected shape (grouped <-> flat), which swaps the
	// ItemsSourceView and row-metadata provider the owner cached.
	internal void SetProjectionChangedHandler(Action? handler) => m_projectionChanged = handler;
	// Raised when a shaping verb rewrote the projection. `reorderOnly` is true when membership is
	// unchanged and only the order moved.
	internal void SetShapingChangedHandler(Action<bool>? handler) => m_shapingChanged = handler;
	// using ShapingTipCompleted = std::function<void(TableViewTip::Operation, uint64_t, bool failed,
	//     std::optional<TableViewTip::IgnoreReason>, std::optional<HRESULT>, TableViewTip::AppFaultKind,
	//     TableViewTip::ContainedFaults)>;
	// TODO Uno: std::function becomes a named delegate.
	internal delegate void ShapingTipCompleted(
		TableViewTip.Operation operation,
		ulong generation,
		bool failed,
		TableViewTip.IgnoreReason? ignored,
		int? error,
		TableViewTip.AppFaultKind appFaultKind,
		TableViewTip.ContainedFaults contained);
	// void SetShapingTipHandlers(
	//     winrt::IInspectable const& owner,
	//     std::function<uint64_t(TableViewTip::Operation, bool)> begin,
	//     ShapingTipCompleted complete);
	// The owner holds live restores while its cell editor is open and resumes them when it
	// closes (see ShapedItemsSource::SetLiveShapingHold).
	// void SetLiveShapingHold(std::function<bool()> isHeld);
	// void ResumeHeldLiveShaping();
	// Lets the owner refresh a custom comparer's ranks as items change (see
	// ShapedItemsSource::SetLiveItemChangedHook).
	// void SetLiveItemChangedHook(std::function<bool(winrt::IInspectable const&, bool)> hook);
	// Lets the owner drop comparer ranks for rows that left the source (see
	// ShapedItemsSource::SetRowsReleasedHook).
	// void SetRowsReleasedHook(std::function<void(winrt::IInspectable const&, std::vector<winrt::IInspectable> const&)> hook);
	// Lets the owner learn that live shaping reacted to an item change (see
	// ShapedItemsSource::SetLiveItemReshapeHandler).
	// void SetLiveItemReshapeHandler(std::function<void()> handler);
	// False when sorting would fail because the rows include a null item or one object twice.
	// bool CanApplySort() const;

	// private:
	// struct ShapingTip;
	private sealed class ShapingTipHandlers
	{
		public Func<TableViewTip.Operation, bool, ulong>? begin;
		public ShapingTipCompleted? complete;
	}
	// winrt::TableViewSource SortCore(winrt::hstring const& previousSortAxisToken, winrt::hstring const& sortAxisToken, winrt::TableViewKeySelector const& key, winrt::hstring const& sortMemberPath, winrt::SortDirection direction);
	// Re-derives everything this class caches from the shape the engine just produced.
	// void OnProjectionRebuilt();
	// TableViewRowItemKeySelector MakeIdentitySelector() const;
	// Raised after a rebuild changes the projected shape (grouped <-> flat), which swaps the
	// ItemsSourceView and row-metadata provider the owner cached.
	// void NotifyOwnerProjectionChanged();
	// Tells the owner a shaping verb rewrote the projection, so it can raise the UIA
	// structure-changed event that a programmatic reshape has no other trigger for.
	// void NotifyOwnerShapingChanged(bool reorderOnly);

	private readonly ShapedItemsSource m_engine;
	// Weak, and typed as IInspectable rather than TableView: this slot exists only to detect a
	// second owner binding, and the owning TableView references this source through its
	// ItemsSource property, so a strong back-pointer would be a cycle.
	private WeakReference<object>? m_owningTableView = null;
	// Installed by the owner in SetOwningTableView's caller and cleared when it detaches, so a
	// source that has been swapped out cannot drive its former owner.
	private Action? m_projectionChanged;
	private Action<bool>? m_shapingChanged;
	private ShapingTipHandlers? m_shapingTipHandlers;
	private ShapingTip? m_shapingTip;
	private ulong m_ownerGeneration;
	private uint m_notificationDepth;
	private sealed class OwnerReleasedWaiter
	{
		public WeakReference<object> Waiter = null!;
		public Action? Handler;
	}
	private List<OwnerReleasedWaiter> m_ownerReleasedWaiters = new();
	// tracker_ref so the GC can walk this strong reference into the (possibly managed) projected
	// rows and collect any cycle; also swaps to null safely during finalization.
	private ItemsSourceView? m_itemsSourceView;
	private ITableViewRowMetadataProvider? m_rowMetadata;

	// TODO Uno: the ReferenceTracker base's m_owningThreadId (RuntimeClassHelpers.h); abi_enter is emulated by an
	// explicit CheckThread() at the top of each public member.
	private readonly ReferenceTrackerThreadAffinity m_threadAffinity;

	// TODO Uno: winrt::weak_ref::get().
	private object? GetOwningTableView() =>
		m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner) ? owner : null;
}
