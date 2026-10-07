// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Globalization.NumberFormatting;
// ThemeSettings (Microsoft.UI.System) is used below for UI-thread High Contrast change notifications.
// Included here (not the shared CppWinRTIncludes.h) to keep the rebuild scope local to TableView.
using ThemeSettings = Microsoft.UI.System.ThemeSettings;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// One in-flight resize drag. The gripper owns the gesture; this is only the host's anchor for it,
// kept reachable so Escape can cancel the drag in flight.
// A class rather than a struct: C++ only ever holds it through std::shared_ptr and compares
// those pointers by identity, which is reference semantics.
internal sealed class ColumnResizeDragState
{
	// Weak: the handlers that own this state are registered on the gripper itself, so a strong
	// reference here is a cycle the XAML reference tracker cannot see.
	public WeakReference<ResizeGripper>? gripper = null;
	public double startValue = 0.0;
	// The column's Width as authored: reverting a canceled drag to startValue would rewrite an
	// Auto or Star column as fixed pixels.
	public GridLength startWidth = default;
	// Set once a DragDelta has actually written Width, so a canceled press that never moved
	// leaves the column completely untouched.
	public bool didWrite = false;
	// Set once any DragDelta arrived, even one the bounds swallowed. Drives the announcement, so a
	// press that never moved stays silent while a step held at a bound still reports the width.
	public bool didDelta = false;
}

// Per-instance cache of density metrics and the resolved gridline brush. Held as a
// TableView member (not a process-global map keyed by `this`) so instances on
// different UI threads never share or concurrently mutate one container. Grouped into
// small nested structs (DensityInfo/GridLineInfo) so each cached concern reads as one
// cohesive unit; nested-struct naming follows the controls/dev `*Info` convention
// (e.g. LinedFlowLayout::ItemsInfo, WebView2::XamlFocusChangeInfo).
// Classes rather than structs: C++ only ever reaches the cache (and its nested groups)
// by reference (`auto& cache = ...`), so a value-type copy would silently drop writes.
internal sealed partial class TableViewResourceCache
{
	// Density-dependent metrics: resolved from Density-suffixed ThemeResource keys (see
	// DensitySuffix), so they change with the Density property. Cleared with the rest of the
	// cache by InvalidateTableViewResourceCache (density / theme / high-contrast changes).
	internal sealed class DensityInfo
	{
		public bool hasRowMinHeight = false;
		public double rowMinHeight = 0.0;
		public bool hasCellPadding = false;
		public Thickness cellPadding = default;
		public bool hasHeaderCellPadding = false;
		public Thickness headerCellPadding = default;
	}
	public readonly DensityInfo density = new();

	// Font sizes resolved from fixed (non-density-suffixed) ThemeResource keys, so they do NOT
	// vary with Density; kept out of DensityInfo to avoid implying otherwise. Still cached and
	// cleared by InvalidateTableViewResourceCache alongside the other resolved resources.
	internal sealed class FontInfo
	{
		public bool hasCellFontSize = false;
		public double cellFontSize = 0.0;
		public bool hasHeaderFontSize = false;
		public double headerFontSize = 0.0;
	}
	public readonly FontInfo font = new();

	// Resolved gridline brush; re-resolved when the theme or high-contrast state changes.
	internal sealed class GridLineInfo
	{
		public bool hasBrush = false;
		public ElementTheme theme = ElementTheme.Default;
		public bool highContrast = false;
		public Brush? brush = null;
	}
	public readonly GridLineInfo gridLine = new();

	// Cached horizontal scroll offset used to reposition frozen columns (not a theme resource,
	// so it is intentionally left out of the density/gridline groups above).
	public bool hasLastFrozenColumnsHorizontalOffset = false;
	public double lastFrozenColumnsHorizontalOffset = 0.0;
}

// namespace ShapingHelpers { class CustomSortRankAdapter; }
// (forward declaration only; the type is ShapingHelpers.CustomSortRankAdapter in TabularShaping).

// The control's half of the TableViewSource sort axis. The projection is addressed by an opaque
// axis token, so re-sorting the same column replaces its axis rather than stacking a second one.
// A class rather than a struct: it is only ever used in place as TableView's member and
// its methods mutate it, so reference semantics avoid accidental copies. ResetCustomSort and Clear
// are defined in TableView.Sort.mux.cs.
internal sealed partial class TableViewSourceSortBinding
{
	public string MemberPath = "";
	public string AxisToken = "";
	public TableViewKeySelector? KeySelector = null;
	// The rank adapter for a CustomSortComparer column. Owned by the control but implemented in the
	// shaping engine: the control feeds it the column's comparer, the engine turns that into the
	// integer sort keys the projection consumes.
	public ShapingHelpers.CustomSortRankAdapter? CustomSortState;

	// Drops any comparer and its ranks without discarding the selector: the selector closes over
	// the state by shared_ptr, so replacing it would orphan the live closure.
	// void ResetCustomSort();
	// void Clear();
}

partial class TableView
{
	// TableView();
	// Drops the recycle pools the row-template selector caches. Those pools close a
	// repeater -> template-wrapper -> selector -> template -> pool -> repeater cycle through plain
	// C++ references the reference tracker cannot walk, so nothing here is collected unless the
	// one edge we own is cut. See TableViewRowTemplateSelector::Detach.
	// ~TableView();

	// IFrameworkElement overrides
	// void OnApplyTemplate();
	// winrt::AutomationPeer OnCreateAutomationPeer();

	// Property-changed callbacks (from TableViewProperties)
	// void OnItemsSourcePropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnIsReadOnlyPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnColumnsPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnHeadersVisibilityPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnGridLinesVisibilityPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnRowBackgroundPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnAlternatingRowBackgroundPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnEmptyTemplatePropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnDensityPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// Density resources fall back to Standard defaults; rows and columns call these via get_self.
	// double GetDensityRowMinHeight();
	// winrt::Thickness GetDensityCellPadding();
	// winrt::Thickness GetDensityHeaderCellPadding();
	// double GetCellFontSize();
	// double GetHeaderFontSize();

	// Resolved grid-line brush (theme/HC-aware, cached); rows call this via get_self, like the
	// density/font accessors above.
	// winrt::Brush GetGridLineBrush();

	// Per-instance resource cache (density metrics + gridline brush); accessed by the
	// file-scope resource helpers in TableView.cpp through this owner pointer.
	internal TableViewResourceCache GetResourceCacheInternal() => m_resourceCache;

	// ActualTheme cannot report HC; cached AccessibilitySettings selects HC grid-line resources.
	// bool IsHighContrast();

	// TableViewColumn calls this when header templates change so realized headers refresh.
	// void RebuildHeaders();
	// Toggling it adds or removes every gripper, so the header band is rebuilt.
	// void OnCanUserResizeColumnsPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// The gripper owns the resize mechanics; this only positions it and forwards the pointer drag.
	// void AppendResizeGripperVisual(
	//     const winrt::Grid& headerCell,
	//     const winrt::TableViewColumn& column,
	//     double gripperWidth,
	//     const winrt::hstring& headerText,
	//     winrt::HorizontalAlignment logicalEndAlignment);

	// winrt::ResizeGripper FindResizeGripperInCell(const winrt::FrameworkElement& headerCell) const;

	// Idempotent; safe from any pointer end-event, the gripper's Unloaded, or TableView's own.
	// void CancelColumnResizeDrag();
	// void AnnounceColumnWidth(const winrt::IInspectable& announcer, const winrt::TableViewColumn& column);

	// Coalesces bursts of column-driven header rebuilds (bulk Columns edits, per-column Header /
	// FrozenEdge changes) into a single RebuildHeaders on the next dispatcher tick, so N column
	// mutations cost O(N) header builds instead of O(N^2). Skips entirely before the template is
	// applied (OnApplyTemplate performs the initial build). Falls back to a synchronous rebuild when
	// no dispatcher is available or the enqueue fails.
	// void QueueRebuildHeaders();

	// Internal — invoked by TableViewColumn when its Visibility changes so
	// realized header and row cells stay in sync without rebuilding Columns.
	// void OnColumnVisibilityChanged(const winrt::TableViewColumn& column);

	// Internal — invoked by TableViewColumn when Width / MinWidth / MaxWidth changes so the next
	// measure pass re-resolves column widths (ResolveColumnWidths then re-pins frozen columns and
	// refreshes gridline visuals from the resolved widths).
	// void OnColumnWidthChanged(const winrt::TableViewColumn& column);

	// TableViewTemplateColumn calls this when CellTemplate changes so realized rows regenerate cells.
	// void OnColumnCellTemplateChanged(const winrt::TableViewColumn& column);

	// TableViewColumn calls this when Header / HeaderTemplate / HeaderTemplateSelector changes so
	// headers re-render and Auto columns recompute their content width.
	// void OnColumnHeaderChanged(const winrt::TableViewColumn& column);

	// Re-applied in place: a rebuild would drop header focus and re-stamp the sort affordance.
	// void OnColumnHeaderToolTipChanged(const winrt::TableViewColumn& column);

	// TableViewColumn calls this when FrozenEdge changes so headers re-render and the leading-frozen
	// band re-pins.
	// void OnColumnFrozenEdgeChanged(const winrt::TableViewColumn& column);

	// Pin rebuilt rows immediately when leading-frozen columns are active.
	// void PinFrozenColumnsForRow(const winrt::TableViewRow& row);

	// Requested by a cell panel (header/row) during measure when a realized cell's own measured width
	// changed (grow or shrink). Invalidates our measure synchronously so ResolveColumnWidths re-runs in
	// the same layout tick (no deferral -> no one-frame lag). Bridges the body ScrollViewer, which
	// absorbs the cell's own measure invalidation. Converges without a debounce (see the definition).
	// void RequestColumnWidthResolve();

	// Automation peer accessors; weak refs may be null before OnApplyTemplate.
	internal ItemsRepeater? GetRowsRepeaterInternal() => m_rowsRepeater;
	internal Panel? GetHeaderHostInternal() => m_headerHost;
	internal int GetRowCountInternal() => GetItemsSourceCount();
	internal ScrollViewer? GetBodyScrollerInternal() => m_bodyScroller;

	// Test hook for moving keyboard focus to a row; false for invalid indexes or before rows exist.
	// bool FocusRow(int32_t index);

	// IFrameworkElement override. Must be PUBLIC: C++/WinRT dispatches overrides through a base
	// subobject that can only reach public members; a protected override is silently never called.
	// winrt::Size MeasureOverride(winrt::Size const& availableSize);

	// ----- Editing (TableView_Editing.cpp) -----

	// Scope of an edit close. Internal only: the public surface is cell-scoped in this release, but
	// the row scope is real - moving to a different item must end that item's transaction - and the
	// plumbing is kept so row editing can be added without re-threading every signature.
	internal enum EditingUnit
	{
		Cell,
		Row,
	}

	// winrt::IInspectable CurrentItem();
	// winrt::TableViewColumn CurrentColumn();
	// void SetCurrentCell(winrt::IInspectable const& item, winrt::TableViewColumn const& column);

	// ----- Editing -----

	// True while an edit is in flight on a cell: from the edit being accepted, through the open
	// editor, until the matching close completes.
	//
	// It also covers the brief windows where an edit is still opening and where a close is already
	// underway, because the control uses this to reject re-entrant edit operations from inside
	// consumer callbacks. An app querying it from a BeginningEdit handler therefore sees true even
	// if that handler goes on to cancel.
	//
	// Cell scope: this is "a cell editor is open". Row-scoped editing is not part of this release.
	/// <summary>
	/// Gets a value that indicates whether an edit is in flight on a cell: from the edit being accepted,
	/// through the open editor, until the matching close completes.
	/// </summary>
	/// <remarks>
	/// It also covers the brief windows where an edit is still opening and where a close is already
	/// underway, because the control uses this to reject re-entrant edit operations from inside
	/// consumer callbacks. An app querying it from a BeginningEdit handler therefore sees true even
	/// if that handler goes on to cancel.
	/// Cell scope: this is "a cell editor is open". Row-scoped editing is not part of this release.
	/// </remarks>
	public bool IsEditing => m_editState != EditState.None;

	// The editor currently in the tree, or null. Internal: the cell automation peer needs it to
	// route IValueProvider.SetValue through the public edit lifecycle.
	// winrt::FrameworkElement CurrentEditingElement() const;

	// Turns a row's container-level item into the object an edit writes to. Identity today; the
	// seam a wrapping layer (grouping) changes in one place. Public so TableViewRow's gesture
	// handler resolves the edit target exactly the way the control does - when those disagreed,
	// begin-edit failed on every row.
	// winrt::IInspectable UnwrapEditingDataItem(winrt::IInspectable const& item) const;

	// Identity comparison that survives boxing and re-projection. Raw IInspectable equality is not
	// reliable for boxed values, so anything comparing data items must use this.
	// static bool SameInspectableIdentity(winrt::IInspectable const& lhs, winrt::IInspectable const& rhs);

	// bool BeginEdit();
	// bool BeginEdit(winrt::IInspectable const& item, winrt::TableViewColumn const& column);
	// bool CommitEdit();
	// bool CommitEditInternal(EditingUnit unit);
	// bool CancelEdit();
	// bool CancelEditInternal(EditingUnit unit);

	// Forced teardown (source-driven reset / ItemsSource replacement / unload). Cannot be vetoed.
	// bool TerminateEditForReset(bool force);
	// Same, but leaves the edited row's visuals alone. For callers inside a layout pass, where
	// restoring the display child would mutate the tree during measure.
	//
	// insideLayoutPass additionally defers the app-visible NOTIFICATIONS (CellEditEnding /
	// EditEnded) onto the dispatcher. Raising them synchronously runs app code inside
	// ItemsRepeater's measure, and a handler that touches the tree or invalidates layout
	// re-enters the pass we are standing in and fail-fasts. Pass true only from a genuine layout
	// pass - the DP-change callers are re-entrant for FOCUS reasons, not layout ones.
	// bool TerminateEditWithoutVisualRestore(bool insideLayoutPass = false);

	// Runs an app-visible notification now, or on the dispatcher when we are inside a layout pass.
	// void PostEditNotification(std::function<void()> notify);

	// Closes an open edit when the column it is on becomes read-only. Called by
	// TableViewColumn::OnPropertyChanged, which cannot reach the edit state itself.
	// void OnColumnIsReadOnlyChanged(winrt::TableViewColumn const& column);
	// Control-initiated reshape (sort / group / expand). Stays cancelable.
	// bool TryTerminateEditForControlInitiatedReshape();

	// Defer a source-reshaping operation until the open edit closes. Opaque action, so editing
	// needs no compile-time knowledge of sorting, grouping or expansion.
	// void QueueCoalescedEditReshape(std::function<void()> operation);
	// void ClearCoalescedEditReshape();
	// void DrainCoalescedEditReshape();

	// ----- Editing input gestures (TableView_EditingInput.cpp) -----
	// The pointer gesture lives on TableViewRow: the row owns its cells, so it is the level that
	// can resolve which cell a press landed on.

	// void OnKeyDownForEditing(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);
	// void OnLosingFocusForEditing(
	//     const winrt::IInspectable& sender,
	//     const winrt::Microsoft::UI::Xaml::Input::LosingFocusEventArgs& args);
	// void CompleteFocusLossCommit();

	// ----- Selection (TableView_Selection.cpp) -----
	// SelectionModel owns the selected index and reconciles it across collection changes; this
	// control keeps the DP projections, the row chrome and the gestures. The DPs are pushed and
	// never read back, because a DP write notifies synchronously.

	// void OnSelectionModePropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// void Select(int32_t index);
	// void Deselect(int32_t index);
	// bool IsSelected(int32_t index);
	// void DeselectAll();

	// Centralises the mode gate only. Modifier-aware routing (interacted vs focused, per
	// SelectorBase) is what Multiple/Extended will add on top.
	// bool CanSelectRows();

	// Shared by the pointer gesture, keyboard navigation and the row peer. No-op when off.
	// The one-arg form reads Ctrl live and toggles; automation passes toggle=false, because UIA
	// Select() means "make this the selection", never "clear it".
	// void SelectRowIndexFromInteraction(int32_t index);
	// void SelectRowIndexFromInteraction(int32_t index, bool toggle);

	// The row sees the press first (it owns its cells); selection state lives on the control.
	// void OnRowPointerSelect(winrt::TableViewRow const& row);

	// Re-derives IsSelected for a realized or re-indexed row; it never survives recycling.
	// void RefreshRowSelectionState(winrt::TableViewRow const& row);
	// void RefreshRowSelectionState(winrt::TableViewRow const& row, int32_t selectedIndex);

	// For the automation peers, which cannot reach the private members. Both read the model.
	// int32_t SelectedIndexInternal() const;
	// winrt::IInspectable SelectedItemInternal() const;
	// --- Grouped projections (TableView_Grouping.cpp) ---
	//
	// Which container type a row-source item realizes as. Item-based rather than index-based
	// because the element factory is only ever handed the item.
	// TableViewRowKind GetRowKindForItem(winrt::IInspectable const& item) const;
	// bool TryGetTableViewSourceRowInfo(int32_t rowIndex, TableViewRowInfo& rowInfo) const;
	// bool IsTableViewSourceGrouped() const;
	// True when the flat row at `index` is a group header rather than a data row. Group headers
	// share the flat projection (and its index space) with data rows, but are not selectable and
	// must be recognized by keyboard navigation as valid focus anchors.
	// bool IsGroupHeaderRow(int32_t index) const;

	// Band gesture (directionless) and the ExpandCollapse pattern (directional). Both resolve the
	// target group's identity immediately and apply the mutation on a later turn.
	// void ToggleGroupExpansion(winrt::UIElement const& container);
	// void SetGroupExpansion(winrt::UIElement const& container, bool expand);

	// Public grouping commands (from TableView IDL).
	// void ExpandAllGroups();
	// void CollapseAllGroups();

	// The peer resolves the row index of its header through the repeater rather than a tree walk.
	internal ItemsRepeater? GetRowsRepeaterForPeer() => m_rowsRepeater;

	// --- Sorting (TableView_Sort.cpp) ---
	// Sorting is single-column, and the active state lives on the column: read
	// TableViewColumn.SortDirection, which is the column a Sorted handler is handed. There is
	// deliberately no control-level SortColumn/SortDirection pair mirroring it, matching WPF's
	// DataGrid, which also keeps sort state on DataGridColumn and exposes no control-level
	// equivalent.
	// bool SortByColumn(const winrt::TableViewColumn& column, winrt::SortDirection direction);
	// bool ToggleSortDirection(const winrt::TableViewColumn& column);
	// bool ClearSort();

	// Republishes every realized header chevron from its column's SortDirection DP. A push rather
	// than a binding: SortIndicatorDirection and SortDirection are distinct WinRT enums, so a
	// {Binding} between them silently does nothing. Called by TableViewColumn.
	// void RefreshSortIndicators();
	// CanSort gates whether the chevron is built at all, so a runtime flip needs a header rebuild.
	// void OnColumnCanSortChanged(const winrt::TableViewColumn& column);
	// void OnCanUserSortColumnsPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// Drops a column that has left Columns from the active sort state. Returns true when the sort
	// state changed.
	// bool PurgeColumnFromSortState(const winrt::TableViewColumn& removedColumn);

	// private:
	// Drives the SelectionModel; its SelectionChanged is the single funnel that publishes.
	// void ApplySelection(int32_t index);

	// void ReleaseHeaderToolTips(const winrt::Panel& host);

	// Created lazily so a TableView that never selects pays nothing.
	// void EnsureSelectionModel();
	// SelectionModel::Source has no identity short-circuit - re-setting the same source would drop
	// the selection - so this only writes when it actually differs.
	// void UpdateSelectionModelSource();
	// void OnSelectionModelSelectionChanged(
	//     const winrt::SelectionModel& sender,
	//     const winrt::SelectionModelSelectionChangedEventArgs& args);

	// Index of `item` in the ItemsSource, by identity; -1 when absent or the source is not set.
	// Still needed because SelectionModel indexes but does not look items up.
	// int32_t IndexOfItem(winrt::IInspectable const& item) const;

	// The selected item for an already-read index, so a caller that has one does not rebuild the
	// model's IndexPath just to resolve it again.
	// winrt::IInspectable SelectedItemForIndex(int32_t index) const;

	// The realized container for a data index, or null.
	// winrt::TableViewRow FindRealizedRowForIndex(int32_t index);

	// void PushSelectionProperties();
	// void RaiseSelectionChanged(winrt::IInspectable const& addedItem);
	// void RaiseSelectionAutomationEvents(winrt::TableViewRow const& deselectedRow, winrt::TableViewRow const& selectedRow);
	// Re-derives IsSelected on every realized row. Needed after a collection change, where the
	// repeater has already re-indexed its containers.
	// void RestampAllRealizedRowSelection();
	// void RestampAllRealizedRowSelection(int32_t selectedIndex);

	// Subscribed only to restamp rows: SelectionModel handles the index reconciliation itself, and
	// does not raise when an insert merely shifts the selected index.
	// void UpdateSelectionCollectionChangedSubscription();
	// void OnSelectionItemsSourceCollectionChanged(const winrt::IInspectable& sender, const winrt::IInspectable& args);

	// Subscribed AHEAD of the SelectionModel so it runs first on a projection Reset (filter/sort/
	// regroup re-materializing the rows in place). SelectionModel clears on a Reset because the
	// indices it holds are gone; this detector captures the still-selected item by object identity
	// BEFORE that clear and arms an identity-based restore, so a selected row that survives the
	// reshape keeps its selection instead of being dropped.
	// void UpdateSelectionResetDetectorSubscription();
	// void OnSelectionSourceReset(const winrt::IInspectable& sender, const winrt::NotifyCollectionChangedEventArgs& args);

	// Re-points the model at a new ItemsSource and re-selects anything held across a reload.
	// void ResolveSelectionAfterSourceChange();
	// Unload drains the repeater's source; re-sourcing on load clears the model. Hold the selected
	// item across that round trip so an unload/reload cycle does not drop the selection.
	// void StashSelectionForReload();
	// bool HasRowsSource() const;

	// True when selection cannot be resolved yet - selection off, or no source.
	// bool ShouldDeferSelectionRequest();
	// void ClearPendingSelection();
	// bool DrainPendingSelection();

	private SelectionModel? m_selectionModel = null;
	private readonly SerialDisposable m_selectionModelChangedRevoker = new();

	// Last index published to the rows, so a change knows which container to unstamp and which to
	// raise the UIA "removed from selection" event on.
	private int m_lastPublishedIndex = -1;

	// Last item REPORTED through SelectionChanged; the delta derives from this.
	private object? m_lastRaisedSelectedItem;

	// The selected item, held across an unload/reload round trip so re-sourcing the repeater does
	// not drop it. Nothing else defers.
	private object? m_pendingSelectedItem;

	// The last INTENTIONALLY selected item (user gesture, programmatic set, or a restore), captured
	// by object identity in ApplySelection - the single selection writer. Unlike SelectedItem, it
	// is NOT cleared by a projection Reset that drops the model's indices, so it is the anchor an
	// identity-based restore re-selects against after a filter/sort/regroup reshape. A genuine
	// deselect flows through ApplySelection(-1) and nulls it, so an intentional clear is never
	// "restored".
	private object? m_stickySelectedItem;

	// Armed by OnSelectionSourceReset when a Reset drops the selection with a surviving sticky item,
	// consumed by OnSelectionItemsSourceCollectionChanged to run the identity restore once the
	// model has reconciled.
	private bool m_resetSelectionRestorePending = false;

	// Bumped on every publish so a re-entrant one can tell that a newer selection overtook it and
	// it must not finish its own (now stale) notification.
	private uint m_selectionVersion = 0;

	// Set while a stashed selection is being restored after a reload, so the clear-then-reselect
	// that SelectionModel::Source forces is published once at the end rather than as two events.
	private bool m_isRestoringSelection = false;

	private readonly SerialDisposable m_selectionCollectionChangedRevoker = new();

	// Fires ahead of the SelectionModel on a projection Reset; see OnSelectionSourceReset.
	private readonly SerialDisposable m_selectionResetDetectorRevoker = new();

	// The views the two subscriptions above are attached to. Both re-register only when the view
	// actually changes, because SelectionModel::Source cannot be re-assigned for an unchanged view
	// (it clears the selection unconditionally) and so always keeps its original registration.
	// Re-registering these two against the same view would move them behind the model's and invert
	// the detector -> model -> restamp order ResolveSelectionAfterSourceChange documents.
	private ItemsSourceView? m_selectionCollectionChangedView = null;
	private ItemsSourceView? m_selectionResetDetectorView = null;

	// private:
	// Explicit edit lifecycle, replacing four independent booleans whose 16 nominal combinations
	// encoded the real invariants only in the ordering of guards spread across five methods.
	//
	//   None      -> no edit open.
	//   Beginning -> inside BeginningEdit; reentrant edit operations are rejected.
	//   Editing   -> edit open and interactive.
	//   Ending    -> inside CellEditEnding/RowEditEnding, or waiting on a deferral one took.
	//
	// Legal: None->Beginning->{None, Editing}; Editing->Ending->{Editing, None}, where
	// Ending->Editing is the veto/validation-failure path that keeps the edit open.
	private enum EditState
	{
		None,
		Beginning,
		Editing,
		Ending,
	}

	private EditState m_editState = EditState.None;

	// True once this edit has written to the data item. A validation failure keeps the edit open
	// AFTER the write, so a later cancel has to push the pre-edit value back to the source rather
	// than only restoring the editor.
	private bool m_editSourceWritten = false;

	// True while a forced teardown arrived during the Beginning window, where EndCurrentEdit cannot
	// close the edit. BeginEdit re-checks it and unwinds instead of promoting to Editing over a row
	// that has already been recycled onto a different item.
	private bool m_abandonPendingBeginEdit = false;

	// True only while a queued reshape is replaying, so the drain is not re-entered by it.
	private bool m_isApplyingCoalescedEditReshape = false;

	// Bumped when a forced teardown closes an edit awaiting a deferral, so a late completion can
	// recognise itself as stale.
	private uint m_editGeneration = 0;

	// Pending source-reshaping operations. A deque, not a vector: the drain pops from the front so
	// replay order matches arrival order.
	// std::deque<std::function<void()>>; the C++ only uses push_back / front + pop_front /
	// empty / clear, which Queue<Action> covers 1:1.
	private readonly Queue<Action> m_pendingEditReshapes = new();

	// Keyboard/focus position. Deliberately NOT redefined while an edit is open, so a value read
	// inside an EditEnding handler stays valid once the edit closes.
	private object? m_currentItem;
	private TableViewColumn? m_currentColumn;

	// True while a teardown must not touch the edited row's visuals (recycle / rebuild paths).
	private bool m_suppressEditVisualRestore = false;
	// True only while tearing down from inside ItemsRepeater's measure/arrange. Distinct from
	// m_suppressEditVisualRestore, which is also set by DP-change callbacks where the hazard is
	// focus re-entrancy rather than layout re-entrancy.
	private bool m_insideLayoutPass = false;

	// void SetCurrentItem(winrt::IInspectable const& item);
	// void UpdateCurrentColumn(winrt::TableViewColumn const& column);

	// Shared tail of the synchronous and deferred edit-close paths.
	// bool CompleteEditEnd(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel, bool vetoed);
	// The cell being edited. Distinct from m_currentItem/m_currentColumn, which track focus.
	private object? m_currentEditItem;
	private TableViewColumn? m_currentEditColumn;
	private TableViewRow? m_currentEditRow;

	// Whatever the column's PrepareCellForEdit handed back, returned to it on cancel.
	private object? m_editUneditedValue;

	// bool RaiseBeginningEdit(winrt::IInspectable const& item, winrt::TableViewColumn const& column);
	// Returns Vetoed or Completed. Synchronous: there is no deferral in this release, so a handler
	// must set Cancel before it returns.
	private enum EditEndingResult { Vetoed, Completed }
	// EditEndingResult RaiseEditEnding(
	//     EditingUnit unit,
	//     winrt::TableViewEditAction action,
	//     bool honorCancel);

	// bool TryResolveFocusedCell(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryResolveCurrentCell(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryResolveCurrentCellForEdit(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryGetItemAtRowIndex(int32_t rowIndex, winrt::IInspectable& item) const;
	// winrt::TableViewRow FindRealizedRowForItem(winrt::IInspectable const& item);
	// bool TryBeginEditVisual(winrt::IInspectable const& item, winrt::TableViewColumn const& column);

	// void EndEditVisual(winrt::TableViewEditAction action);

	// Applies the outcome of an edit close: writes or reverts, tears down the visual, clears state.
	// Shared by the synchronous and deferred paths so they cannot drift.
	// bool FinishEditTeardown(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel);
	// bool EndCurrentEdit(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel);

	// Scoped to the property the edited column writes; falls back to the object-level check only
	// when the column reports no single editing property path.
	// bool HasBlockingValidationErrors(
	//     winrt::IInspectable const& item,
	//     winrt::TableViewColumn const& column) const;

	// private:
	// void OnColumnsVectorChanged(
	//     const winrt::IObservableVector<winrt::TableViewColumn>& sender,
	//     const winrt::IVectorChangedEventArgs& args);

	// void OnRowElementPrepared(
	//     const winrt::ItemsRepeater& sender,
	//     const winrt::ItemsRepeaterElementPreparedEventArgs& args);

	// void OnRowElementClearing(
	//     const winrt::ItemsRepeater& sender,
	//     const winrt::ItemsRepeaterElementClearingEventArgs& args);

	// void OnRowElementIndexChanged(
	//     const winrt::ItemsRepeater& sender,
	//     const winrt::ItemsRepeaterElementIndexChangedEventArgs& args);

	// Body horizontal scrolling drives the header ScrollViewer so headers track row cells.
	// void OnBodyScrollerViewChanged(
	//     const winrt::IInspectable& sender,
	//     const winrt::ScrollViewerViewChangedEventArgs& args);

	// Defer ScrollViewer ancestor lookup until Loaded because ScrollViewer template names are shadowed.
	// void OnHeaderHostLoaded(const winrt::IInspectable& sender, const winrt::RoutedEventArgs& args);
	// void OnRowsRepeaterLoaded(const winrt::IInspectable& sender, const winrt::RoutedEventArgs& args);

	// ThemeSettings must be created once a XamlRoot/WindowId is available (Loaded); its Changed handler
	// refreshes HC-dependent resources directly on the UI thread (Changed is raised there).
	// void OnTableViewLoaded(const winrt::IInspectable& sender, const winrt::RoutedEventArgs& args);
	// void OnThemeSettingsChanged(const winrt::Microsoft::UI::System::ThemeSettings& sender, const winrt::IInspectable& args);

	// void UpdateHeaderVisibility();
	// void ApplyGridLinesToHeader();
	// void RefreshGridLinesOnRealizedRows();
	// void RefreshRowBackgroundsOnRealizedRows();

	// Invoke fn for each realized row in PART_RowsRepeater. Centralizes the "enumerate realized
	// rows" walk shared by the density / gridline / frozen-column / column-change update paths
	// (and the future grouping seam), so those callers don't each re-implement the repeater walk.
	// void ForEachRealizedRow(std::function<void(winrt::TableViewRow const&)> const& fn);

	// Reset/refill the tracked-column owner back-pointers (shared by the Columns-replaced and
	// vector-Reset paths). Detach clears m_trackedColumns; Track refills it (no-op if null).
	// void DetachAllColumnOwners();
	// void TrackColumnsFromVector(winrt::IObservableVector<winrt::TableViewColumn> const& columns);
	// bool ShouldShowColumnHeaders();
	// int32_t GetItemsSourceCount() const;

	// void PrepareGroupHeaderElement(winrt::TableViewGroupHeader const& header, int32_t index);
	// void ClearGroupHeaderElement(winrt::TableViewGroupHeader const& header);
	// void UpdateGroupHeaderWidth(winrt::TableViewGroupHeader const& header);

	// Split responsibilities driven off the ItemsSource DP:
	//   AdoptItemsSource   - source lifetime. Runs only when ItemsSource actually changes: normalize
	//                        a plain collection into a control-owned TableViewSource, detach the
	//                        previous source, adopt the new one, and wire its owner + handlers.
	//   RefreshRowsPipeline- pushes the active source's view into the repeater (identity-guarded),
	//                        re-reads its row-metadata provider, and re-resolves empty-state +
	//                        selection. Runs on every re-entry (template applied, repeater
	//                        reloaded, shaping verb) with no lifetime work.
	// void AdoptItemsSource();
	// void RefreshRowsPipeline();
	// Raised by the bound TableViewSource when a shaping verb swapped its projection
	// (grouped <-> flat), so the cached view / row metadata are re-read.
	// void OnTableViewSourceProjectionChanged();
	// Raised by the bound TableViewSource after a shaping verb rewrote the projection. A
	// programmatic reshape has no input event behind it, so this is the only thing that tells a
	// UIA client its cached rows are stale. reorderOnly separates a pure re-sort (same children,
	// new order) from a membership change.
	// void OnTableViewSourceShapingChanged(bool reorderOnly);
	// Last writer wins between the two sort front-ends. The control owns ONE axis and publishes
	// the chevron from it; TableViewSource.Sort declares an untokenized axis the control cannot
	// address. When the app declares or clears a sort directly on the source, the control stands
	// down: it drops its own axis and every column's SortDirection, so the rows are ordered by
	// exactly one sort and no chevron claims an axis that is not primary (or no longer exists).
	// void ReconcileSortStateWithSource();
	// void QueueReconcileSortStateWithSource();
	// Suppresses ReconcileSortStateWithSource for the duration of a control-initiated verb, whose
	// own source mutations would otherwise read as the app taking over.
	private ControlInitiatedSortScope BeginControlInitiatedSortScope()
	{
		m_isApplyingControlInitiatedSort = true;
		return new(this);
	}

	// gsl::finally semantics: writes false on dispose rather than restoring the previous value.
	private readonly struct ControlInitiatedSortScope(TableView owner) : IDisposable
	{
		public void Dispose() => owner.m_isApplyingControlInitiatedSort = false;
	}
	// EmptyTemplate shows only for null or empty row sources.
	// void UpdateEmptyState();
	// void UpdateEmptyStateCollectionChangedSubscription();
	// void OnEmptyStateItemsSourceCollectionChanged(const winrt::IInspectable& sender, const winrt::IInspectable& args);

	private readonly SerialDisposable m_columnsVectorChangedToken = new();

	// --- TableViewSource binding ---
	//
	// The projection the rows are driven from. For a bound TableViewSource this IS the source's
	// ItemsSourceView; otherwise it is the raw ItemsSource wrapped. Cached because the shape can
	// be swapped underneath us by a shaping verb.
	private ItemsSourceView? m_rowsItemsSourceView = null;

	// The single active source, whether the app assigned it or the control synthesized it over a
	// plain ItemsSource. Held strongly through a tracker_ref: TableViewSource is a ReferenceTracker,
	// so this edge is visible to the GC's cross-boundary cycle walker (the same reason ItemsRepeater
	// holds its ItemsSourceView by tracker_ref) and double-retention of an app-assigned source
	// alongside the ItemsSource DP cannot leak. Also used to detach the previous source on a swap:
	// binding a different source must clear the old one's owner and handlers, or a source still
	// subscribed to the app's collection keeps driving a control it no longer belongs to. Released
	// when ItemsSource changes to null / a different source; the source keeps only a weak
	// back-pointer to the owner, so this is not a hard cycle. Reassigned exclusively by
	// AdoptItemsSource, so every other path reads it as a stable answer rather than re-deriving it.
	private TableViewSource? m_activeSource;

	// Row semantics for the current projection (row kind, identity, group expansion). Null when no
	// TableViewSource is bound, or when the projection is degraded and carries no shaped identity.
	private ITableViewRowMetadataProvider? m_tableViewSourceRowMetadata;
	// Bumped on every metadata swap so a request captured against the previous provider can tell
	// that the provider which produced its identity is gone. Identities are value-based strings,
	// so without this a queued group toggle could resolve against a same-named group in a
	// brand-new source.
	private ulong m_rowMetadataGeneration = 0;

	// void RequestGroupExpansion(winrt::UIElement const& container, std::optional<bool> desired);    void QueueGroupExpansionByIdentity(winrt::hstring const& identity, std::optional<bool> desired);
	// void ApplyGroupExpansionByIdentity(winrt::hstring const& identity, std::optional<bool> desired, uint64_t generation);
	// void RaiseGroupStructureChanged();
	// void SetAllGroupsExpansion(bool expand);

	// Keyboard-driven group toggle loses focus without this: the Enter/Space toggle defers a
	// structural reshape that recycles the focused header container, dropping focus (and its
	// visual) to nothing. Capture the header's identity + FocusState at gesture time, then restore
	// focus to the same group's header once the reshape's relayout has settled. Only keyboard /
	// programmatic focus is restored -- a pointer toggle carries no focus visual.
	// void CaptureGroupHeaderFocusForRestore(winrt::UIElement const& container, winrt::hstring const& identity);
	// winrt::hstring CaptureFocusedGroupHeaderForRestore();
	// void RestoreGroupHeaderFocusIfPending(winrt::hstring const& identity);
	// void FocusGroupHeaderByIdentity(winrt::hstring const& identity, winrt::FocusState focusState);
	// Row identity for a realized container. Identity is index-independent once captured.
	// winrt::hstring TryGetContainerIdentity(winrt::UIElement const& container);

	// winrt::hstring StringifyGroupKey(winrt::IInspectable const& key);
	// Cached because resolving the culture formatter is measurably expensive and group-key text is
	// produced during measure, once per realized header.
	// winrt::DecimalFormatter GetGroupKeyDecimalFormatter();
	private DecimalFormatter? m_groupKeyDecimalFormatter = null;
	private int m_groupKeyDefaultFractionDigits = 0;

	// Chooses between the row and group-header container templates. Held so ~TableView can drop
	// the recycle pools it caches; see TableViewRowTemplateSelector::Detach.
	private TableViewRowTemplateSelector? m_rowTemplateSelector;

	// --- Sorting ---
	//
	// Re-validated after every point where app code could have run (a Sorting handler, or an
	// edit-ending handler): the columns collection may have changed underneath the request that is
	// still in flight.
	// bool IsSortRequestStillValid(const winrt::TableViewColumn& column) const;
	// bool IsSortClearStillValid() const;
	// The source the rows are projected through, app-assigned or synthesized. Non-null means the
	// control can reshape the rows itself.
	// winrt::TableViewSource ShapingSourceInternal() const;
	// Writes the single-column sort state into the columns; does not reshape.
	// void ApplySingleColumnSortState(const winrt::TableViewColumn& column, winrt::SortDirection direction);
	// Applies the current sort state to the bound TableViewSource. Returns false when there is no
	// TableViewSource, or the trigger column resolves no sort key.
	// bool SyncTableViewSourceSort(const winrt::TableViewColumn& trigger, winrt::SortDirection direction);
	// winrt::TableViewKeySelector GetTableViewSourceSortKeySelector(const winrt::hstring& sortMemberPath);
	// bool RaiseSortingAndCheckCanceled(const winrt::TableViewColumn& trigger, winrt::SortDirection direction);
	// Single funnel for "the sort state has been written to the columns": reshapes, restores the
	// selection, raises Sorted, and announces.
	// void RecomputeSortDPsAndRaiseInternal(const winrt::TableViewColumn& trigger);

	// Silently drops the active sort when the data set is replaced. See the definition for why this
	// is not ClearSort.
	// void ResetSortStateForNewItemsSource();
	// Projection index of a data item, or -1. Used to carry the selection across a re-sort.
	// int32_t FindEntryIndexForDataItem(const winrt::IInspectable& item) const;
	// void AnnounceSortChange(const winrt::hstring& announcement);
	// The active sort column left Columns. Reshaping inside the VectorChanged callback would
	// re-enter the collection that is still mutating, so the reshape runs on the next turn.
	// void QueueClearSortAfterColumnRemoval();
	private bool m_clearSortAfterColumnRemovalQueued = false;
	// void AppendSortIndicatorVisual(const winrt::Panel& host, const winrt::TableViewColumn& column);
	// The chevron is code-created into a nested host panel, so its programmatic Name is not in any
	// XAML namescope and FindName cannot resolve it. Locate it by type via a child walk instead.
	// static winrt::SortIndicator FindSortIndicator(const winrt::Panel& root);
	// static winrt::SortIndicatorDirection ToSortIndicatorDirection(winrt::SortDirection direction);

	// v1 is single-column sort, so this holds at most one entry. It stays a vector because the
	// clear/purge walks are written against the collection and multi-column sort is the expected
	// next step. Weak refs: a column removed from Columns must not be kept alive by sort state.
	private readonly List<TableViewColumn?> m_sortedColumns = new();
	private readonly TableViewSourceSortBinding m_tableViewSourceSort = new();

	private ItemsRepeater? m_rowsRepeater;
	private ContentControl? m_emptyStatePresenter;
	private FrameworkElement? m_headerRow;
	private Panel? m_headerHost;
	private ScrollViewer? m_headerScroller;
	// Keeps the header band locked to the body when focus moves to an off-screen header.
	private readonly SerialDisposable m_headerBringIntoViewRevoker = new();
	private ScrollViewer? m_bodyScroller;

	private readonly SerialDisposable m_rowElementPreparedToken = new();
	private readonly SerialDisposable m_rowElementClearingToken = new();
	private readonly SerialDisposable m_rowElementIndexChangedToken = new();
	private readonly SerialDisposable m_bodyScrollerViewChangedToken = new();
	// Body-viewport resize invalidates measure so Star columns resolve during the table layout pass.
	private readonly SerialDisposable m_bodyScrollerSizeChangedRevoker = new();
	private readonly SerialDisposable m_headerHostLoadedToken = new();
	// Set while a drag is in flight, so Escape can reach the gripper that owns it.
	private ColumnResizeDragState? m_activeColumnResizeDrag;
	// True while a control-initiated sort verb is mutating the source. Its own mutations must not
	// be mistaken for the app declaring a sort behind the control's back.
	private bool m_isApplyingControlInitiatedSort = false;
	private bool m_sortReconcileQueued = false;
	private readonly SerialDisposable m_rowsRepeaterLoadedToken = new();
	private readonly SerialDisposable m_pendingFocusLayoutToken = new();
	// Deferred restore of keyboard focus to a group header after a toggle reshape recycles it.
	// Separate from m_pendingFocusLayoutToken (row focus) so a row-focus request and a group-focus
	// restore in flight at once cannot clobber each other's one-shot LayoutUpdated token.
	private readonly SerialDisposable m_pendingGroupFocusLayoutToken = new();
	private string m_pendingGroupFocusIdentity = "";
	private FocusState m_pendingGroupFocusState = FocusState.Unfocused;
	private readonly SerialDisposable m_emptyStateCollectionChangedRevoker = new();
	// ActualThemeChanged refreshes imperatively-resolved brushes that ItemsRepeater rows do not re-pump.
	private readonly SerialDisposable m_actualThemeChangedToken = new();

	// ThemeSettings (lifted WinUI3) reports the system High Contrast setting and raises Changed on the
	// control's UI thread -- unlike AccessibilitySettings.HighContrastChanged, which could be delivered
	// off-thread. It requires a WindowId, so it is created on Loaded (once a XamlRoot exists), not in
	// the constructor, and torn down on Unloaded.
	private ThemeSettings? m_themeSettings = null;
	private readonly SerialDisposable m_themeSettingsChangedRevoker = new(); // Runtime HC toggles must refresh cached HC-dependent brushes.

	// Cached HC state: kept fresh by ThemeSettings.Changed while loaded and read by IsHighContrast().
	// Only touched on the UI thread now, so no atomic is required.
	private bool m_isHighContrast = false;
	private readonly SerialDisposable m_loadedRevoker = new();

	// Unloaded drains repeater and body-scroller state before deferred callbacks hit a detached subtree.
	private readonly SerialDisposable m_unloadedRevoker = new();
	// void OnTableViewUnloaded();
	private bool m_rowsSourceDrained = false;

	// Leading-frozen columns are offset against horizontal scroll and clipped out of non-frozen cells.
	// double ComputeLeadingFrozenWidth();
	// void RefreshFrozenColumns();

	// Column-width layout engine internals (TableView_Layout.cpp).
	// GetHeaderMeasuredWidthForColumn encapsulates the header host's concrete panel type so the
	// layout engine pulls the header's measured width through a TableView seam (symmetric with
	// TableViewRow::MeasuredWidthForColumn) instead of casting to TableViewCellsPanel itself.
	// double GetHeaderMeasuredWidthForColumn(const winrt::TableViewColumn& column) const;
	// ResolveColumnWidths runs the Pixel/Auto/Star pass (invoked from MeasureOverride once the
	// template subtree has measured), pulling cached measured widths from the header host and realized
	// rows before writing ActualWidth to each column.
	// void ResolveColumnWidths();
	// ResetColumnDesiredWidths clears the grow-only Auto desired-width accumulators on data-set
	// boundaries (ItemsSource / Columns replaced / CellTemplate / Header) and invalidates measure so
	// the next table-level pass re-pulls fresh measured widths.
	// void ResetColumnDesiredWidths();
	// Re-invalidate the header + realized row cells panels so they re-measure/arrange after a resolve.
	// void InvalidateCellPanels();

	// Latches frozen-column state so transforms and clips clear exactly once when disabled.
	private bool m_frozenColumnsActive = false;

	// Set while a coalesced RebuildHeaders is pending on the dispatcher; collapses a burst of column
	// changes into one rebuild. UI-thread only (all column callbacks arrive on the UI thread).
	private bool m_rebuildHeadersQueued = false;

	// Per-instance resource cache; replaces the former process-global map keyed by `this`.
	private readonly TableViewResourceCache m_resourceCache = new();

	// Mirrors Columns so removals can clear a column's OwningTableView back-pointer.
	private readonly List<TableViewColumn?> m_trackedColumns = new();

	// Bubbling KeyDown lets focused descendants handle input before row navigation.
	private KeyEventHandler? m_keyDownHandler = null;  // Root KeyDown (handledEventsToo); registration is released with the element, no explicit RemoveHandler needed.

	// void OnKeyDownForNavigation(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);

	// Left/Right resize for the column whose header has focus; the gripper is a pointer
	// affordance here, not a tab stop.
	// bool TryHandleHeaderColumnResizeKey(const winrt::KeyRoutedEventArgs& args);
	// Redirects a header's bring-into-view onto the body scroller, so the header cannot scroll
	// independently of the columns it labels.
	// void OnHeaderBringIntoViewRequested(const winrt::BringIntoViewRequestedEventArgs& args);

	// Tunneling PreviewKeyDown captures the focused row BEFORE the framework's built-in focus
	// navigation moves it (and marks the key Handled), so OnKeyDownForNavigation can anchor on the
	// pre-move row and advance exactly one row instead of doubling up with the built-in move.
	private KeyEventHandler? m_previewKeyDownHandler = null;

	// Editing gesture handlers; the registration is released with the element, so no RemoveHandler.
	private KeyEventHandler? m_editingKeyDownHandler = null;
	private readonly SerialDisposable m_editingLosingFocusRevoker = new();

	// Set while a focus-loss commit check is queued, so a burst of focus changes produces one
	// re-evaluation rather than one commit attempt each.
	private bool m_focusLossCommitQueued = false;

	private int m_navAnchorRow = -1;
	// void OnPreviewKeyDownForNavigation(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);

	// Keyboard navigation helpers.
	// int32_t GetFocusedRowIndex() const;
	// int32_t GetEstimatedRowsPerPage(); // Non-const — GetDensityRowMinHeight() mutates the resource cache.
}
