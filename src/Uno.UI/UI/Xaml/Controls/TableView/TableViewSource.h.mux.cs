// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewSource.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TODO Uno: Original C++: class ShapedItemsSource; (forward declaration only)

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
	// winrt::TableViewSource Sort(winrt::TableViewKeySelector const& key, winrt::SortDirection direction);
	// winrt::TableViewSource Sort(winrt::hstring const& sortMemberPath, winrt::SortDirection direction);
	// winrt::TableViewSource SortReplacing(winrt::hstring const& previousSortAxisToken, winrt::hstring const& sortAxisToken, winrt::TableViewKeySelector const& key, winrt::hstring const& sortMemberPath, winrt::SortDirection direction);
	// winrt::TableViewSource ClearFilter();
	// winrt::TableViewSource ClearGroupBy();
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

	// Internal: the owning TableView caches the projection (ItemsSourceView, row metadata and
	// grouped-ness) when it binds, so it must be told when a shaping verb swaps that projection
	// for a different one. Held weakly - the owner holds this source via ItemsSource.
	//
	// The owner is taken as IInspectable because only its IDENTITY matters here, for the
	// single-owner check below. Naming TableView would invert the dependency (rule 5): this type
	// sits below the control in the shaping stack. For the same reason the owner INSTALLS the two
	// handlers below rather than being called back into by name - the same shape as the handlers
	// this class installs on its own engine.
	// void SetOwningTableView(winrt::IInspectable const& owner);
	// Raised after a rebuild changed the projected shape (grouped <-> flat), which swaps the
	// ItemsSourceView and row-metadata provider the owner cached.
	internal void SetProjectionChangedHandler(Action? handler) => m_projectionChanged = handler;
	// Raised when a shaping verb rewrote the projection. `reorderOnly` is true when membership is
	// unchanged and only the order moved.
	internal void SetShapingChangedHandler(Action<bool>? handler) => m_shapingChanged = handler;

	// private:
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
	// tracker_ref so the GC can walk this strong reference into the (possibly managed) projected
	// rows and collect any cycle; also swaps to null safely during finalization.
	private ItemsSourceView? m_itemsSourceView;
	private ITableViewRowMetadataProvider? m_rowMetadata;

	// TODO Uno: the ReferenceTracker base's m_owningThreadId (RuntimeClassHelpers.h); abi_enter is emulated by an
	// explicit CheckThread() at the top of each public member.
	private readonly ReferenceTrackerThreadAffinity m_threadAffinity;
}
