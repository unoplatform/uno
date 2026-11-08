// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;
// ThemeSettings (Microsoft.UI.System) is used below for UI-thread High Contrast change notifications.
// Included here (not the shared CppWinRTIncludes.h) to keep the rebuild scope local to TableView.
using ThemeSettings = Microsoft.UI.System.ThemeSettings;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class TableViewDetails
{
	// A UIElement header has no text, and a templated header shows its template rather than the
	// object's ToString (CsWinRT makes every managed object IStringable, yielding its type name).
	// A ToString that is just the runtime class name is not text either: the object is typically
	// rendered by an implicit DataType template, so callers fall back to the realized header's name.
	internal static string? TryGetHeaderContentText(object? header, bool isTemplated)
	{
		if (header is null || header is UIElement)
		{
			return null;
		}
		if (TryGetString(header) is { } propString)
		{
			return propString;
		}
		if (!isTemplated)
		{
			if (SharedHelpers.IsStringable(header))
			{
				var text = SharedHelpers.StringableToString(header);
				// TODO Uno: winrt::get_class_name, whose failure the C++ swallows; Type.FullName cannot fail.
				var className = header.GetType().FullName;
				if (string.IsNullOrEmpty(text) || text == className)
				{
					return null;
				}
				return text;
			}
		}

		return null;
	}

	internal static string? TryGetColumnHeaderText(TableViewColumn? column)
	{
		if (column is null)
		{
			return null;
		}

		return TryGetHeaderContentText(
			column.Header, column.HeaderTemplate is not null || column.HeaderTemplateSelector is not null);
	}

	// Shared by the header cell's automation name and the gripper's OwnerName.
	internal static string GetColumnHeaderText(TableViewColumn? column)
	{
		return TryGetColumnHeaderText(column) ?? "";
	}

	// Whether the app derived the column's class, so the column runs the app's overrides; TableView's own column
	// classes run only TableView's code. The class name is compared, never reported. One that cannot be read is
	// treated as the app's.
	// TODO Uno: winrt::get_class_name becomes GetType(), which cannot fail, so the C++ catch-all is not needed.
	internal static bool IsAppDerivedColumn(TableViewColumn column)
	{
		var type = column.GetType();
		return type != typeof(TableViewTextColumn) &&
			type != typeof(TableViewTemplateColumn) && type != typeof(TableViewColumn);
	}
}

internal struct ColumnResizeBounds
{
	public ColumnResizeBounds()
	{
	}

	public double Min = 0.0;
	public double Max = double.PositiveInfinity;
}

// A Star column before the dragged one, held at the width it already renders. The local Width
// value is kept -- not the effective one -- so a canceled gesture restores a binding rather than
// overwriting it with a local value.
internal struct ColumnResizeFrozenColumn
{
	public ColumnResizeFrozenColumn()
	{
	}

	public WeakReference<TableViewColumn>? column = null;
	public object? width = null;
}

// A locked Star column before a resized one, held at the width it rendered when the resize began.
internal struct LockedColumnHold
{
	public LockedColumnHold()
	{
	}

	public WeakReference<TableViewColumn>? column = null;
	public double width = 0.0;
	// What the Auto columns took when the hold was latched; growth past it can make the hold stale.
	public double autoTotal = 0.0;
}

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
	// The column's Width as a local value: reverting a canceled drag to startValue would rewrite an
	// Auto or Star column as fixed pixels, and restoring the effective value would overwrite a binding.
	public object? startWidth = null;
	// Captured once per gesture: the drag does not change the other columns' widths.
	public ColumnResizeBounds bounds = new();
	// Set once a DragDelta has actually written Width, so a canceled press that never moved
	// leaves the column completely untouched.
	public bool didWrite = false;
	// Set once any DragDelta arrived, even one the bounds swallowed, so a step held at a bound
	// still announces its width instead of going silent.
	public bool didDelta = false;
	// Filled on the first write, not at DragStarted, so a press that never moves mutates nothing.
	public readonly List<ColumnResizeFrozenColumn> frozen = new();
	// The table's locked-column holds before the first write, restored if the drag is canceled.
	// TODO Uno: std::optional<std::pair<std::vector<LockedColumnHold>, size_t>>; size_t hashes are ulong here.
	public (List<LockedColumnHold> first, ulong second)? lockedHoldsBefore = null;
	// A double-click fit writes a Pixel width even when an Auto or Star column already has its size.
	public bool commitPixelWidth = false;
	public ulong lastPressTimestamp = 0;
	public Point lastPressPosition = default;
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
	}
	public readonly FontInfo font = new();

	// Cached hierarchy geometry. Runtime resource swaps raise no notification, so they apply only
	// after cache invalidation (density/theme/HC/re-template) and row re-prepare.
	internal sealed class HierarchyInfo
	{
		public bool hasRowIndentSize = false;
		public double rowIndentSize = 0.0;
		public bool hasRowExpanderSize = false;
		public double rowExpanderSize = 0.0;
		public bool hasGroupHeaderContentOffset = false;
		public double groupHeaderContentOffset = 0.0;
	}
	public readonly HierarchyInfo hierarchy = new();

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
// class GroupedEntry;
// (forward declarations only; the types are ShapingHelpers.CustomSortRankAdapter and GroupedEntry
// in TabularShaping / GroupedSourceAdapter).

// A class rather than a struct: it is only ever used in place as TableView's member and
// its methods mutate it, so reference semantics avoid accidental copies. ResetCustomSort and Clear
// are defined in TableView.Sort.mux.cs.
internal sealed partial class TableViewSourceSortBinding
{
	public string MemberPath = "";
	public string AxisToken = "";
	public TableViewKeySelector? KeySelector = null;
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
	// void OnGroupHeaderTemplatePropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
	// void OnDensityPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// Density resources fall back to Standard defaults; rows and columns call these via get_self.
	// double GetDensityRowMinHeight();
	// winrt::Thickness GetDensityCellPadding();
	// winrt::Thickness GetDensityHeaderCellPadding();
	// double GetCellFontSize();
	// void AttachFocusCornerRadius(winrt::FrameworkElement const& target);

	// Per-level indent from the TableViewRowIndentSize resource, cached for rows.
	// double GetRowIndentSize();

	// Chevron gutter width from TableViewRowExpanderSize (same key the template binds).
	// double GetRowExpanderSize();
	// double GetGroupHeaderContentOffset();

	// Resolved grid-line brush (theme/HC-aware, cached); rows call this via get_self, like the
	// density/font accessors above.
	// winrt::Brush GetGridLineBrush();

	// static winrt::SortIndicatorDirection ToSortIndicatorDirection(winrt::SortDirection direction);

	// void OnColumnHeaderTemplateApplied(winrt::TableViewColumnHeader const& header);
	// void OnColumnHeaderStylePropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// Per-instance resource cache (density metrics + gridline brush); accessed by the
	// file-scope resource helpers in TableView.cpp through this owner pointer.
	internal TableViewResourceCache GetResourceCacheInternal() => m_resourceCache;

	// ActualTheme cannot report HC; cached AccessibilitySettings selects HC grid-line resources.
	// bool IsHighContrast();

	// TableViewColumn calls this when header templates change so realized headers refresh.
	// void RebuildHeaders();
	// Toggling it adds or removes every gripper, so the header band is rebuilt.
	// void OnCanUserResizeColumnsPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);

	// The gripper owns the resize mechanics; this only sizes it and forwards the pointer drag.
	// winrt::ResizeGripper CreateResizeGripper(
	//     const winrt::FrameworkElement& headerCell,
	//     const winrt::TableViewColumn& column,
	//     double gripperWidth,
	//     const winrt::hstring& headerText);

	// winrt::ResizeGripper FindResizeGripperInCell(const winrt::FrameworkElement& headerCell) const;

	// Idempotent; safe from any pointer end-event, the gripper's Unloaded, or TableView's own.
	// void CancelColumnResizeDrag();

	// UIA Transform support for the column header peer. Driven through the column's gripper so the
	// bounds, Star handling and announcement match a pointer or Alt+Arrow resize exactly.
	// bool CanResizeColumnFromAutomation(const winrt::TableViewColumn& column);
	// bool TryResizeColumnFromAutomation(const winrt::TableViewColumn& column, double width, bool commitPixelWidth = false);
	// bool AutoFitColumn(const winrt::TableViewColumn& column);
	// static double DoubleClickSlop(const winrt::UIElement& element);
	// static uint64_t DoubleClickIntervalMicroseconds();
	// void AnnounceColumnWidth(const winrt::IInspectable& announcer, const winrt::TableViewColumn& column);

	// Coalesces bursts of column-driven header rebuilds (bulk Columns edits, per-column Header /
	// FrozenEdge changes) into a single RebuildHeaders on the next dispatcher tick, so N column
	// mutations cost O(N) header builds instead of O(N^2). Skips entirely before the template is
	// applied (OnApplyTemplate performs the initial build). Falls back to a synchronous rebuild when
	// no dispatcher is available or the enqueue fails.
	// void QueueRebuildHeaders();

	// Coalesces a burst of column changes into one StructureChanged on the next tick, so Narrator
	// does not re-read the grid once per mutation.
	// void QueueRaiseColumnsStructureChanged();
	// void RaiseColumnsStructureChanged();

	// Internal — invoked by TableViewColumn when its Visibility changes so
	// realized header and row cells stay in sync without rebuilding Columns.
	// void OnColumnVisibilityChanged(const winrt::TableViewColumn& column);

	// Internal — invoked by TableViewColumn when Width / MinWidth / MaxWidth changes so the next
	// measure pass re-resolves column widths (ResolveColumnWidths then re-pins frozen columns and
	// refreshes gridline visuals from the resolved widths).
	// void OnColumnWidthChanged(const winrt::TableViewColumn& column);

	// Internal - ends the locked-column holds; called when an input they were latched against changes.
	// void ExpireLockedColumnHolds();

	// TableViewTemplateColumn calls this when CellTemplate changes so realized rows regenerate cells.
	// void OnColumnCellTemplateChanged(const winrt::TableViewColumn& column, bool resetAutoWidth = true);

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
	internal bool AreColumnHeadersShownInternal() => ShouldShowColumnHeaders();
	internal int GetRowCountInternal() => GetItemsSourceCount();
	// int32_t GetDataRowPositionInSetInternal(int32_t rowIndex) const;
	// int32_t GetDataRowSizeOfSetInternal() const;
	internal ScrollViewer? GetBodyScrollerInternal() => m_bodyScroller;
	// Releases the rows repeater's layout anchor, made by GetOrCreateElement(anchor's index), without scrolling.
	// void ReleaseRowsRepeaterAnchor(winrt::UIElement const& anchor);

	// Test hook for moving keyboard focus into the body at a row index; false for invalid indexes
	// or before rows exist. Focus lands at whichever LEVEL the cursor is on - the ROW container by
	// default, or that row's current cell once the user has drilled in with Right.
	// bool FocusRow(int32_t index);

	// void HandleBodyKeyFromRowInternal(const winrt::KeyRoutedEventArgs& args);


	// Moves keyboard focus to a cell by row index and VISIBLE column index, realizing and scrolling
	// the row into view first. A negative column means "keep the current one". Group-header rows
	// have no cells and fall back to focusing the header container. Drills the cursor to CELL level.
	// bool FocusCell(int32_t rowIndex, int32_t visibleColumnIndex, std::optional<double> alignRatio = std::nullopt);

	// Moves keyboard focus to the row CONTAINER at a flat row index, realizing and scrolling it
	// into view first, and leaves the cursor at ROW level. This is the body's entry level and what
	// Up/Down move between while the user has not drilled into cells.
	// bool FocusRowContainer(int32_t rowIndex, std::optional<double> alignRatio = std::nullopt);

	// No side effects; the caller arms the level it redirects to.
	internal struct FocusEntryTarget
	{
		public TableViewRow? Row;
		// A UIElement, or a Hyperlink in the empty-state content.
		public DependencyObject? Element;
		public bool CellLevel;
		public bool Remembered;
	}

	// static bool IsEntryRedirectTargetFocusable(winrt::DependencyObject const& target, winrt::DependencyObject const& owner);
	// allowNoViewport: without a body scroller, every realized row counts as in view (entry only; paging keeps its estimate).
	// winrt::UIElement FindViewportEdgeElementInternal(bool last, bool includeGroupHeaders, bool allowNoViewport = false) const;

	// A redirect re-raises GettingFocus with the same CorrelationId; redirect once to avoid a loop.
	internal bool HasRedirectedFocusEntry(Guid correlationId) =>
		m_hasFocusEntryRedirect && m_focusEntryRedirectCorrelationId == correlationId;

	internal void NoteFocusEntryRedirect(Guid correlationId)
	{
		m_focusEntryRedirectCorrelationId = correlationId;
		m_hasFocusEntryRedirect = true;
	}

	// void ArmCellLevelRowInternal(winrt::TableViewRow const& row);

	internal string GetContainerIdentityInternal(UIElement? container) => TryGetContainerIdentity(container);

	// Two-level cursor state. False = ROW level, true = CELL level. Owned here rather than on the
	// row because it has to survive row recycling and follow the cursor from row to row.
	// void SetCellCursorActiveInternal(bool active);

	// void OnRowCellFocusChanged(winrt::TableViewRow const& row, winrt::DependencyObject const& focusedElement);

	// bool TryGetFocusedCell(int32_t& rowIndex, int32_t& columnIndex, bool requireExactCell) const;

	// Focuses the cell at a visible-column index inside an ALREADY realized container. Group
	// headers share the repeater and have no cells, so they keep taking container focus.
	// bool FocusRealizedRowCell(
	//     winrt::UIElement const& element,
	//     int32_t visibleColumnIndex,
	//     winrt::FocusState focusState = winrt::FocusState::Keyboard);

	// Actionable headers and cells are the only header/body keyboard focus targets.
	// bool IsColumnUserSortableInternal(winrt::TableViewColumn const& column);
	// bool IsHeaderCellActionableInternal(winrt::FrameworkElement const& header);
	// bool IsColumnCellEditableInternal(winrt::TableViewColumn const& column);
	// void RefreshCellFocusLevelsInternal();
	// void RefreshHeaderTabStopsInternal();
	// The inactive-header IsTabStop stamp; an app's local IsTabStop value or binding is never ours.
	// bool HasHeaderTabStopStampInternal(winrt::FrameworkElement const& header);
	// void ClearHeaderTabStopStampInternal(winrt::FrameworkElement const& header);

	// True when handled (menu shown, or suppressed: editing, nested or stale target); false lets the
	// request bubble to the native ContextFlyout chain.
	// bool ProcessContextMenuRequest(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     winrt::ContextRequestedEventArgs const& args);

	// bool FocusContextMenuTargetFromPointer(TableViewDetails::ContextMenuTarget const& target);

	// The one context menu policy check: no TableView menu opens while a cell is editing.
	internal bool AreContextMenusSuppressed() => IsEditing;

	// Marks an unhandled request handled while a cell is editing, so no ContextFlyout on the
	// element that raised it or on any ancestor opens. Hooked below every element that can carry a
	// TableView menu the request handlers do not reach (group headers, the table itself).
	// void SuppressContextRequestWhileEditing(winrt::ContextRequestedEventArgs const& args);

	// A column a context menu request can target: in this table's Columns, owned by it, Visible.
	// bool IsContextMenuColumn(winrt::TableViewColumn const& column) const;

	// winrt::Size MeasureOverride(winrt::Size const& availableSize);


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
	// sourceReplaced: the edited item belongs to a data set the control is leaving.
	// bool TerminateEditForReset(bool force, bool sourceReplaced = false);
	// Same, but leaves the edited row's visuals alone. For callers inside a layout pass, where
	// restoring the display child would mutate the tree during measure.
	//
	// insideLayoutPass additionally defers the app-visible NOTIFICATIONS (CellEditEnding /
	// EditEnded) onto the dispatcher. Raising them synchronously runs app code inside
	// ItemsRepeater's measure, and a handler that touches the tree or invalidates layout
	// re-enters the pass we are standing in and fail-fasts. Pass true only from a genuine layout
	// pass - the DP-change callers are re-entrant for FOCUS reasons, not layout ones.
	// bool TerminateEditWithoutVisualRestore(bool insideLayoutPass = false, bool sourceReplaced = false);

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
	// Unlike the queue above, survives a vetoed close and replays only once no edit is open.
	// void QueueReshapeAfterEditCloses(std::function<void()> operation);
	// void ClearCoalescedEditReshape();
	// void DrainCoalescedEditReshape();
	// void QueueDeferredEditReshapeDrain();
	// void FlushDeferredEditReshapes();
	// bool IsActiveSourceOwnedByOther() const;

	// ----- Editing input gestures (TableView_EditingInput.cpp) -----
	// The pointer gesture lives on TableViewRow: the row owns its cells, so it is the level that
	// can resolve which cell a press landed on.

	// void OnKeyDownForEditing(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);
	// void OnLosingFocusForEditing(
	//     const winrt::IInspectable& sender,
	//     const winrt::Microsoft::UI::Xaml::Input::LosingFocusEventArgs& args);
	// void QueueFocusLossCommit();
	// void CompleteFocusLossCommit();
	// Keeps the Escape that closed an editor's flyout from also cancelling the edit.
	// void MarkEditorFlyoutClosing();

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
	// Always false while only None and Single exist; Multiple must derive it from SelectionMode.
	internal bool CanSelectMultipleRows() => false;

	// Shared by the pointer gesture, keyboard navigation and the row peer. No-op when off.
	// The one-arg form reads Ctrl live and toggles; automation passes toggle=false, because UIA
	// Select() means "make this the selection", never "clear it".
	// void SelectRowIndexFromInteraction(int32_t index);
	// void SelectRowIndexFromInteraction(int32_t index, bool toggle);
	// void SelectRowIndexFromKeyboardFocus(int32_t index);

	// void OnRowPointerSelect(winrt::TableViewRow const& row);

	// void RefreshRowSelectionState(winrt::TableViewRow const& row);
	// void RefreshRowSelectionState(winrt::TableViewRow const& row, int32_t selectedIndex);
	// void RefreshRowHierarchyState(winrt::TableViewRow const& row, int32_t index);

	// Restamps realized rows after a reshape; rows that kept their index are not re-prepared.
	// void RefreshRealizedRowHierarchyState();

	// Deferred and coalesced: element indices are only trustworthy after the repeater reconciles.
	// void QueueRefreshRealizedRowHierarchyState();

	// Rewired whenever the pipeline re-reads the row view.
	// void UpdateRowHierarchyResetSubscription();
	// void OnRowsSourceResetForHierarchy(
	//     const winrt::IInspectable& sender,
	//     const winrt::NotifyCollectionChangedEventArgs& args);

	// For the automation peers, which cannot reach the private members. Both read the model.
	// int32_t SelectedIndexInternal() const;
	// winrt::IInspectable SelectedItemInternal() const;

	// --- Grouped projections (TableView_Grouping.cpp) ---
	//
	// Item-based because the element factory is only handed the item.
	// TableViewRowKind GetRowKindForItem(winrt::IInspectable const& item) const;
	// winrt::hstring GetGroupHeaderNameCandidate(GroupedEntry const& entry);
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
	// Expands the row and all descendants (Multiply key). Same deferral as above.
	// void ExpandRowSubtree(winrt::UIElement const& container);

	// Tree keyboard navigation. Each moves row focus and returns whether it did.
	// bool TryFocusParentRow(winrt::UIElement const& container);

	// Public bulk expansion commands (from TableView IDL), one pair per axis.
	// void ExpandAllGroups();
	// void CollapseAllGroups();
	// void ExpandAllRows();
	// void CollapseAllRows();

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
	// class ActionTip; (TableView.Tip.uno.cs)
	// Drives the SelectionModel; its SelectionChanged is the single funnel that publishes.
	// void ApplySelection(int32_t index, bool measureTip = false);

	private enum HeaderRelease { All, DetachedColumns, Column }
	// void ReleaseHeaderCells(const winrt::Panel& host, HeaderRelease scope = HeaderRelease::All, const winrt::TableViewColumn& column = nullptr);
	// void MoveHeaderFocusOffRemovedColumn(const winrt::Panel& host, const winrt::FrameworkElement& removedCell, const winrt::TableViewColumn& removedColumn, winrt::FocusState focusState);
	// void RebuildHeadersCore();
	// winrt::Style CompatibleHeaderStyle(const winrt::Style& style);
	// winrt::TableViewColumn GetFocusedHeaderColumn(const winrt::Panel& host, winrt::FocusState& focusState) const;
	// Refocuses the rebuilt header for column; retries once after layout if it cannot take focus yet.
	// void RestoreHeaderFocus(const winrt::TableViewColumn& column, winrt::FocusState focusState);
	// bool TryFocusHeaderCell(const winrt::TableViewColumn& column, winrt::FocusState focusState);
	// bool CanRestoreHeaderFocus(const winrt::DependencyObject& focusAtSchedule) const;
	// void CancelHeaderFocusRestore();
	// Visible-header index that headerCell's successor takes once it collapses, or -1 when focus is
	// not inside headerCell.
	// int32_t GetCollapsingHeaderFocusIndex(const winrt::Panel& host, const winrt::FrameworkElement& headerCell) const;

	// Created lazily so a TableView that never selects pays nothing.
	// void EnsureSelectionModel();
	// SelectionModel::Source has no identity short-circuit - re-setting the same source would drop
	// the selection - so this only writes when it actually differs.
	// void UpdateSelectionModelSource();
	// void OnSelectionModelSelectionChanged(
	//     const winrt::SelectionModel& sender,
	//     const winrt::SelectionModelSelectionChangedEventArgs& args);
	// Calls into SelectionModel, which raises SelectionChanged back into OnSelectionModelSelectionChanged; an app
	// fault that left that handler gets its tag back here.
	// template <typename Call>
	// void CallSelectionModel(Call&& call);

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
	// void RaiseSelectionAutomationEvents(
	//     winrt::TableViewRow const& deselectedRow,
	//     winrt::IInspectable const& deselectedItem,
	//     winrt::TableViewRow const& selectedRow,
	//     winrt::IInspectable const& selectedItem);
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
	// The app fault that left OnSelectionModelSelectionChanged during the CallSelectionModel in progress (depth).
	private TableViewTip.AppFaultHop m_selectionModelAppFault;
	private uint m_selectionModelCallDepth = 0;

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

	// Tree sources only: node key of m_stickySelectedItem, so a re-created row keeps selection
	// across the Reset. Used only for Reset restore, never across a source swap.
	private string m_stickySelectedIdentity = "";
	private string m_pendingSelectedIdentity = "";
	// winrt::hstring HierarchyIdentityForIndex(int32_t index) const;

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
	private readonly Queue<Action> m_reshapesAfterEditCloses = new();
	private bool m_editReshapeDrainQueued = false;

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
	// bool CompleteEditEnd(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel, bool vetoed, ActionTip* tip = nullptr);
	// The cell being edited. Distinct from m_currentItem/m_currentColumn, which track focus.
	private object? m_currentEditItem;
	private TableViewColumn? m_currentEditColumn;
	private TableViewRow? m_currentEditRow;

	// Whatever the column's PrepareCellForEdit handed back, returned to it on cancel.
	private object? m_editUneditedValue;

	// bool RaiseBeginningEdit(winrt::IInspectable const& item, winrt::TableViewColumn const& column, ActionTip& tip);
	// Returns Vetoed or Completed. Synchronous: there is no deferral in this release, so a handler
	// must set Cancel before it returns.
	private enum EditEndingResult { Vetoed, Completed }
	// EditEndingResult RaiseEditEnding(
	//     EditingUnit unit,
	//     winrt::TableViewEditAction action,
	//     bool honorCancel,
	//     ActionTip* tip);

	// bool TryResolveFocusedCell(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryResolveCurrentCell(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryResolveCurrentCellForEdit(winrt::IInspectable& item, winrt::TableViewColumn& column);
	// bool TryGetItemAtRowIndex(int32_t rowIndex, winrt::IInspectable& item) const;
	// winrt::TableViewRow FindRealizedRowForItem(winrt::IInspectable const& item);
	// bool TryBeginEditVisual(winrt::IInspectable const& item, winrt::TableViewColumn const& column);
	// Beginning -> None without touching the row's visual or the item transaction.
	// void ResetPendingBeginEdit();

	// void EndEditVisual(winrt::TableViewEditAction action, ActionTip* tip);

	// Applies the outcome of an edit close: writes or reverts, tears down the visual, clears state.
	// Shared by the synchronous and deferred paths so they cannot drift.
	// bool FinishEditTeardown(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel, ActionTip* tip);
	// bool EndCurrentEdit(EditingUnit unit, winrt::TableViewEditAction action, bool honorCancel);

	// Scoped to the property the edited column writes; falls back to the object-level check only
	// when the column reports no single editing property path.
	// bool HasBlockingValidationErrors(
	//     winrt::IInspectable const& item,
	//     winrt::TableViewColumn const& column,
	//     ActionTip* tip = nullptr);

	// private:
	// class ActionTip: see TableView.Tip.uno.cs.

	// void AttachSourceTip(winrt::TableViewSource const& source);
	// Whether a synchronous action is tracked: only while a loaded session is active and the rows are not drained.
	private bool TracksTipActions() => m_telemetrySession.active && !m_rowsSourceDrained;
	// uint64_t BeginTipOperation(TableViewTip::Operation operation, bool admitted = true) noexcept;
	// void EndTipOperation() noexcept;
	// void SupersedeTipOperation() noexcept;
	// App code failed (threw, or its write did not land) and TableView carried on without rethrowing.
	// void NoteRecoveredAppFault(TableViewTip::Operation operation, TableViewTip::Stage stage,
	//     TableViewTip::AppFaultKind kind, std::optional<HRESULT> error) noexcept;
	// A column's editing hook threw and TableView carried on. The hooks are TableView's code, so this fails the action
	// (or, without one, is reported as TableView's recovered failure), unless app code it ran threw (appFault): then it
	// is recovered as the app's.
	// void NoteColumnFault(ActionTip* tip, TableViewTip::Operation operation, HRESULT error,
	//     std::optional<TableViewTip::AppFaultKind> appFault = std::nullopt) noexcept;
	// ActionTip* CalloutAction() const noexcept;
	// An app fault ignores the operation; any other failure fails it.
	// void FailTipOperation(uint64_t generation, TableViewTip::Stage stage, std::optional<TableViewTip::Fault> fault) noexcept;
	// void CompleteTipOperation(uint64_t generation) noexcept;
	// void IgnoreTip(TableViewTip::IgnoreReason reason) noexcept;
	// TableViewTip::MetricsInputs TipMetricsInputs() noexcept;
	// TableViewTip::MetricsInputs TipMetricsInputs(TableViewTip::Operation operation) noexcept;
	// Initialize: starts once the template is applied and a source is attached, and ends at the layout pass of the
	// first realized row or group header, or at once for an empty view.
	// void StartTipInitialization() noexcept;
	// void CompleteTipInitializationIfEmpty() noexcept;
	// After a source is attached or reclaimed: starts Initialize once the template is applied, and completes it for
	// an empty view. Before Initialize has its verdict, a source swap is part of it rather than a ReplaceSource
	// operation.
	// void NoteTipInitializationSource() noexcept;
	// Setup (Stage::Template) or the source attach (Stage::Source) threw: Initialize's failure, or ignored when app
	// code threw, whether or not the test was running yet.
	// void FailTipInitialization(TableViewTip::Stage stage, TableViewTip::Fault fault) noexcept;
	// void ArmTipInitializationLayout() noexcept;
	// void RevokeTipInitializationLayout() noexcept;
	// At unload: closes a waiting Initialize as UnloadedBeforeRender when the table had rows to lay out and a viewport,
	// yet laid none out.
	// void IgnoreTipInitializationUnloadedBeforeRender() noexcept;
	// Whether the rows had room: the body scroller's viewport, or the rows repeater's size when no scroller hosts it.
	// bool HasRowsViewport();
	// TableView_Instance's two sample points, Loaded and Unloaded (TableView_Telemetry.cpp).
	// void SampleInstanceTelemetry() noexcept;

	// Telemetry: the loaded session's TableView_Instance. TiP: the initialization and operation tests.
	private readonly TableViewTelemetry.Session m_telemetrySession = new();
	private readonly TableViewTip.State m_tip = new();
	// TODO Uno: Telemetry and TiP are Microsoft-only, intentionally no-op. The members below that only
	// TableView_Tip.cpp reads have no C# counterpart: m_activeActionTip, m_tipCancellationReason,
	// m_tipMutationDepth, m_tipSourceMutationDepth, CalloutAction and TipMetricsInputs.
	private ulong m_tipLifetimeGeneration = 0;
	private readonly SerialDisposable m_tipInitializationLayoutToken = new();
	private bool m_templateApplied = false;
	// The applied template lacks PART_RowsRepeater, PART_HeaderRow or PART_HeaderHost (TableView_Instance).
	private bool m_templateIncomplete = false;
	// TableView_Instance: how the current ItemsSource arrived (TableViewTelemetry::SourceKind), 0 with none.
	private uint m_telemetrySourceKinds = 0;

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
	// void QueueTerminalGridLineRefresh(bool isGeometryRetry = false);
	// void RefreshTerminalGridLines();
	// std::optional<bool> ShouldSuppressTrailingGridLine();
	// std::optional<bool> ShouldSuppressBottomGridLine(
	//     const winrt::FrameworkElement& element,
	//     bool hasBottomGridLine);
	// void RefreshRowBackgroundsOnRealizedRows();
	// void QueueGroupExpansionRowRefresh();
	// void RefreshRealizedRowsAfterGroupExpansion();

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
	// reclaimActiveSource: re-bind the source this table released on Unloaded, or retry a refused one.
	// void AdoptItemsSource(bool reclaimActiveSource = false);
	// void ReclaimItemsSource();
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
	private ControlInitiatedSortScope BeginControlInitiatedSortScope(ulong tipGeneration = 0)
	{
		m_isApplyingControlInitiatedSort = true;
		var previousGeneration = m_controlSortTipGeneration;
		m_controlSortTipGeneration = tipGeneration != 0 ? tipGeneration : ulong.MaxValue;
		return new(this, previousGeneration);
	}

	// gsl::finally semantics: writes false on dispose rather than restoring the previous value.
	private readonly struct ControlInitiatedSortScope(TableView owner, ulong previousGeneration) : IDisposable
	{
		public void Dispose()
		{
			owner.m_isApplyingControlInitiatedSort = false;
			owner.m_controlSortTipGeneration = previousGeneration;
		}
	}
	// EmptyTemplate shows only for null or empty row sources.
	// void UpdateEmptyState();
	// void UpdateItemsSourceCollectionChangedSubscription();
	// void OnItemsSourceCollectionChanged(const winrt::IInspectable& sender, const winrt::IInspectable& args);

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

	// Which axis a bulk command drives; both share the coalescing/focus/restamp machinery.
	private enum BulkExpansionAxis
	{
		Groups,
		Rows,
	}

	// Bumped when a bulk expand/collapse is REQUESTED (not when a deferred one replays), per axis,
	// so a queued per-item request that predates the bulk is dropped instead of re-creating the
	// exception the bulk set erases, while one made after it - or on the other axis - survives.
	private ulong m_groupBulkExpansionGeneration = 0;
	private ulong m_rowBulkExpansionGeneration = 0;
	private ref ulong BulkExpansionGeneration(BulkExpansionAxis axis)
	{
		return ref (axis == BulkExpansionAxis.Rows ? ref m_rowBulkExpansionGeneration : ref m_groupBulkExpansionGeneration);
	}

	// void RequestGroupExpansion(winrt::UIElement const& container, std::optional<bool> desired, bool subtree = false);
	// void QueueGroupExpansionByIdentity(
	//     winrt::hstring const& identity,
	//     std::optional<bool> desired,
	//     BulkExpansionAxis axis,
	//     bool subtree = false);
	// void ApplyGroupExpansionByIdentity(
	//     winrt::hstring const& identity,
	//     std::optional<bool> desired,
	//     uint64_t generation,
	//     BulkExpansionAxis axis,
	//     uint64_t bulkGeneration,
	//     bool subtree = false);
	// void RaiseGroupStructureChanged();
	// void AnnounceRowExpansionChange(winrt::hstring const& identity);

	// void SetBulkExpansion(bool expand, BulkExpansionAxis axis);
	// void ApplyBulkExpansion(bool expand, BulkExpansionAxis axis);

	// void ForEachRealizedGroupHeader(std::function<void(winrt::TableViewGroupHeader const&, int32_t)> const& fn);

	// A reshape can rebind a pooled header without ElementPrepared/ElementIndexChanged, leaving its
	// expansion DPs describing the previous projection.
	// void RefreshRealizedGroupHeadersAfterExpansion();

	// Keyboard-driven group toggle loses focus without this: the Enter/Space toggle defers a
	// structural reshape that recycles the focused header container, dropping focus (and its
	// visual) to nothing. Capture the header's identity + FocusState at gesture time, then restore
	// focus to the same group's header after the relayout settles, unless focus left the table.
	// Also covers focused hierarchical rows a reset recycles (keyboard / programmatic focus only).
	// void CaptureContainerFocusForRestore(
	//     winrt::UIElement const& container,
	//     winrt::hstring const& identity,
	//     winrt::FocusState gestureState = winrt::FocusState::Unfocused);
	// winrt::hstring CaptureFocusedContainerForRestore();
	// void RestoreContainerFocusIfPending(winrt::hstring const& identity);
	// void ArmContainerFocusRestore(
	//     winrt::hstring const& identity, winrt::FocusState focusState, bool fromPointerGesture, int32_t attemptsLeft);
	// bool FocusContainerByIdentity(
	//     winrt::hstring const& identity, winrt::FocusState focusState, bool fromPointerGesture, bool finalAttempt);
	private const int s_containerFocusRestoreAttempts = 4;
	// bool CanRestoreContainerFocus();
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
	// requireCanSort=false is only for clearing a column that has just opted out of sorting.
	// bool IsSortRequestStillValid(const winrt::TableViewColumn& column, bool requireCanSort = true) const;
	// bool IsSortClearStillValid() const;
	// bool CanSortColumnNow(const winrt::TableViewColumn& column, bool requireCanSort = true) const;
	// False when the active source cannot order its rows (a null item or one object twice).
	// bool CanApplySortTo(winrt::SortDirection direction) const;
	// The source the rows are projected through, app-assigned or synthesized. Non-null means the
	// control can reshape the rows itself.
	// winrt::TableViewSource ShapingSourceInternal() const;
	// Writes the single-column sort state into the columns; does not reshape.
	// void ApplySingleColumnSortState(const winrt::TableViewColumn& column, winrt::SortDirection direction);
	// Applies the current sort state to the bound TableViewSource. Returns false when there is no
	// TableViewSource, or the trigger column resolves no sort key.
	// explicitClear: the app or user asked to clear, so a clear also removes app-declared axes.
	// Any other clear (a column leaving, CanUserSortColumns turning off) removes only this table's axis.
	// bool SyncTableViewSourceSort(const winrt::TableViewColumn& trigger, winrt::SortDirection direction, uint64_t tipGeneration, bool explicitClear = false);
	// bool SortByColumnInternal(const winrt::TableViewColumn& column, winrt::SortDirection direction, bool measureTip, bool requireCanSort = true);
	// bool ClearSortInternal(bool measureTip, bool explicitClear = false);
	// The lit column describes the app's source sort, not one this table installed.
	// bool IsSortLitOnlyByApp() const;
	// winrt::TableViewKeySelector GetTableViewSourceSortKeySelector(const winrt::hstring& sortMemberPath);
	// Re-places a live-tracked item among CustomSortComparer ranks. True if any sort key moved.
	// bool RepositionCustomSortItem(const winrt::IInspectable& item, bool reshapePending);
	// Drops CustomSortComparer ranks for rows that left the source: `item` when non-null,
	// otherwise every ranked row not in `liveRows`.
	// void ReleaseCustomSortRows(const winrt::IInspectable& item, const std::vector<winrt::IInspectable>& liveRows);
	// Re-ranks the control's CustomSortComparer axis after a reclaim; see m_rerankCustomSortOnReconcile.
	// void RerankCustomSortAfterReclaim();
	// bool RaiseSortingAndCheckCanceled(const winrt::TableViewColumn& trigger, winrt::SortDirection direction);
	// Single funnel for "the sort state has been written to the columns": reshapes, restores the
	// selection, raises Sorted, and announces.
	// A non-null trigger is always an explicit request; a null one is explicit only when asked.
	// void RecomputeSortDPsAndRaiseInternal(const winrt::TableViewColumn& trigger, bool measureTip = true, bool explicitClear = false);

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
	// Removes `column` from the sort state; queues the clear when it was sorted.
	// void PurgeLeavingColumnFromSortState(const winrt::TableViewColumn& column);
	// IObservableVector has no Move, so RemoveAt + InsertAt of the same instance before the queued
	// clear runs is a move: the column gets its sort back and the clear finds nothing to do.
	// void RestoreSortOfMovedColumn(const winrt::TableViewColumn& column);
	// Any other change to the sort or to the columns while the column is out makes the restore stale.
	private void CancelPendingSortRestore() => m_sortedColumnPendingRemoval = null;
	private WeakReference<TableViewColumn>? m_sortedColumnPendingRemoval;
	private SortDirection m_sortedColumnPendingRemovalDirection = SortDirection.None;

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
	private readonly SerialDisposable m_headerContextRequestedRevoker = new();
	private readonly SerialDisposable m_templateRootContextRequestedRevoker = new();
	// Keeps the header band locked to the body when focus moves to an off-screen header.
	private readonly SerialDisposable m_headerBringIntoViewRevoker = new();
	private readonly SerialDisposable m_bodyBringIntoViewRevoker = new();
	// Targets of the requests ReleaseRowsRepeaterAnchor raised; OnBodyBringIntoViewRequested swallows them.
	private readonly List<WeakReference<UIElement>> m_anchorReleaseTargets = new();
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
	private List<LockedColumnHold> m_lockedColumnHolds = new();
	private ulong m_lockedColumnHoldsSignature = 0;
	// True while a control-initiated sort verb is mutating the source. Its own mutations must not
	// be mistaken for the app declaring a sort behind the control's back.
	private bool m_isApplyingControlInitiatedSort = false;
	private ulong m_controlSortTipGeneration = 0;
	private ulong m_controlSortPublishedGeneration = 0;
	private bool m_sortReconcileQueued = false;
	private readonly SerialDisposable m_rowsRepeaterLoadedToken = new();
	// Terminal-edge geometry is evaluated once after layout settles; gridline rendering then
	// changes without changing row/cell layout thickness.
	private readonly SerialDisposable m_terminalGridLinesLayoutToken = new();
	private readonly SerialDisposable m_headerFocusRestoreLayoutToken = new();
	// Focus left a removed column's header, for a remaining one or parked on the removed cell; the
	// rebuild returns it to the column if the column came back (a move), else moves it to the body.
	private WeakReference<TableViewColumn>? m_headerFocusRemovedColumn = null;
	private WeakReference<TableViewColumn>? m_headerFocusFallbackColumn = null;
	private FocusState m_headerFocusRemovedState = FocusState.Unfocused;
	private WeakReference<TableViewRow>? m_terminalGridLineRow;
	private WeakReference<TableViewGroupHeader>? m_terminalGridLineGroupHeader;
	private readonly SerialDisposable m_terminalGridLineRowSizeChangedRevoker = new();
	private bool m_suppressTrailingGridLine = false;
	private bool m_suppressBottomGridLine = false;
	private bool m_terminalGridLineGeometryRetryAvailable = true;
	private int m_terminalGridLineColumnIndex = -1;
	private double m_terminalGridLineHorizontalOffset = double.NaN;
	private double m_terminalGridLineVerticalOffset = double.NaN;
	// Deferred restore of keyboard focus to a group header after a toggle reshape recycles it.
	private readonly SerialDisposable m_pendingGroupFocusLayoutToken = new();
	private string m_pendingGroupFocusIdentity = "";
	private FocusState m_pendingGroupFocusState = FocusState.Unfocused;
	private bool m_pendingGroupFocusFromPointerGesture = false;
	private readonly SerialDisposable m_pendingGroupRowRefreshLayoutToken = new();
	// Count changes refresh both empty state and the terminal row separator.
	private readonly SerialDisposable m_itemsSourceCollectionChangedRevoker = new();
	// Only edge that restamps realized rows when hierarchy metadata changes without the row set
	// changing (e.g. a node losing its last child).
	private readonly SerialDisposable m_rowHierarchyResetRevoker = new();
	private ItemsSourceView? m_rowHierarchyResetView = null;
	private bool m_rowHierarchyRefreshQueued = false;

	// --- Re-shape focus restore (dev-spec Keyboard, "Re-shape while focused"); see TableView_Keyboard.cpp ---
	// void CaptureBodyFocusForReshape();
	// void RestoreBodyFocusAfterReshape();
	// bool TryResolveLiveBodyFocusPosition(int32_t& row, int32_t& column, bool& cellLevel) const;
	// bool IsElementInBody(const winrt::DependencyObject& element) const;
	// bool IsFocusOnNonBodyElement() const;
	// void ForgetBodyFocusPosition();
	// static winrt::FocusState GetRestoreFocusStateFor(const winrt::IInspectable& focusedElement);
	// void TrackBodyFocusPosition();
	// void OnTableViewGotFocusForBodyTracking(
	//     const winrt::IInspectable& sender,
	//     const winrt::RoutedEventArgs& args);
	// void OnTableViewLosingFocusForBodyTracking(
	//     const winrt::IInspectable& sender,
	//     const winrt::Microsoft::UI::Xaml::Input::LosingFocusEventArgs& args);
	// void SuppressBodyFocusTrackingForThisTurn();
	private bool m_lastBodyFocusValid = false;
	private int m_lastBodyFocusRow = -1;
	private int m_lastBodyFocusColumn = 0;
	private bool m_lastBodyFocusCellLevel = false;
	private FocusState m_lastBodyFocusState = FocusState.Keyboard;
	private bool m_suppressBodyFocusTracking = false;
	private readonly SerialDisposable m_bodyFocusTrackerGotFocusRevoker = new();
	private readonly SerialDisposable m_bodyFocusTrackerLosingFocusRevoker = new();
	// void ArmReshapeFocusAttempt();
	// void OnReshapeFocusAttempt();
	// void CancelReshapeFocusRestore();
	// void CancelReshapeFocusRestoreForUserInput();
	// bool TryLandReshapeFocus();
	// bool IsBodyFocusAtRow(int32_t rowIndex) const;
	// bool IsBodyFocusInContainer(winrt::UIElement const& container) const;
	// bool IsInUnrealizedBodyContainer(winrt::DependencyObject const& element) const;
	// Keyboard moves to a row that is not arranged yet finish through the same attempt loop.
	// void BeginPendingBodyFocus(
	//     int32_t row, int32_t column, bool cellLevel, std::optional<double> alignRatio, winrt::IInspectable const& item = nullptr);
	// bool IsPendingBodyFocusItemAt(int32_t row) const;
	// winrt::IInspectable GetRowsSourceItemAt(int32_t index) const;
	// bool ArrangeAndRevalidate(winrt::UIElement const& element, int32_t index, winrt::IInspectable const& expectedItem);
	// static bool IsRowElementArranged(winrt::UIElement const& element);
	// bool IsRowElementInViewport(winrt::UIElement const& element) const;
	// bool BringRowElementIntoView(winrt::UIElement const& element, std::optional<double> alignRatio);
	// bool ScrollBodyToRowElement(winrt::UIElement const& element, std::optional<double> alignRatio);
	// bool ScrollBodyToIndex(int32_t index, std::optional<double> alignRatio);
	// void RequestBodyVerticalOffset(double offset);
	// bool IsBodyScrollInFlight();
	private double? m_requestedBodyVerticalOffset;
	// void RevealFocusedBodyElement(winrt::UIElement const& element);
	private bool IsKeyboardBodyFocusPending() => m_pendingBodyFocusFromKeyboard && m_pendingReshapeFocusRow >= 0;
	// void QueueReshapeFocusCaptureDisarm();
	// void UpdateReshapeFocusDetectorSubscription(const winrt::ItemsSourceView& view);
	// void OnRowsSourceResetForFocus(
	//     const winrt::IInspectable& sender,
	//     const winrt::NotifyCollectionChangedEventArgs& args);
	private bool m_reshapeFocusValid = false;
	private bool m_reshapeFocusDisarmQueued = false;
	private int m_reshapeFocusRow = -1;
	private int m_reshapeFocusColumn = 0;
	private bool m_reshapeFocusCellLevel = false;
	private FocusState m_reshapeFocusState = FocusState.Keyboard;
	// The consumed capture, replayed until it sticks.
	private int m_pendingReshapeFocusRow = -1;
	private int m_pendingReshapeFocusColumn = 0;
	private bool m_pendingReshapeFocusCellLevel = false;
	private FocusState m_pendingReshapeFocusState = FocusState.Keyboard;
	private double? m_pendingReshapeFocusAlign;
	// Keyboard pending focus follows its item, so source edits above it cannot retarget the move.
	private object? m_pendingBodyFocusItem = null;
	// Set by BeginPendingBodyFocus: a keyboard move scrolling to its row, not a re-shape restore.
	private bool m_pendingBodyFocusFromKeyboard = false;
	// Where a pending keyboard move started (the header for Down); focus still there does not cancel it.
	private WeakReference<DependencyObject>? m_pendingBodyFocusOrigin = null;
	private int m_reshapeFocusAttemptsLeft = 0;
	// Set for the turn a restore is armed in; that turn's input triggered the re-shape.
	private bool m_reshapeFocusArmedThisTurn = false;
	private bool m_reshapeFocusTurnQueued = false;
	private readonly SerialDisposable m_reshapeFocusLayoutToken = new();
	// Covers realize -> arrange -> deferred re-shape work, yet bounded so a restore never fights
	// the user for focus.
	private const int s_reshapeFocusRestoreAttempts = 24;
	// A far keyboard jump over estimated row heights can take a few scroll-and-realize rounds.
	private const int s_keyboardFocusAttempts = 48;
	// Set while an attempt waits for a body scroll to apply; only layout passes re-run it then.
	private bool m_reshapeFocusWaitForLayout = false;
	// Subscribed before the repeater so a Reset is observed while the rows are still realized.
	private readonly SerialDisposable m_reshapeFocusDetectorRevoker = new();
	private ItemsSourceView? m_reshapeFocusDetectorView = null;
	// ActualThemeChanged refreshes imperatively-resolved brushes that ItemsRepeater rows do not re-pump.
	private readonly SerialDisposable m_actualThemeChangedToken = new();

	// ThemeSettings (lifted WinUI3) reports the system High Contrast setting and raises Changed on the
	// control's UI thread -- unlike AccessibilitySettings.HighContrastChanged, which could be delivered
	// off-thread. It requires a WindowId, so it is created on Loaded (once a XamlRoot exists), not in
	// the constructor, and released on Unloaded, AppWindow.Destroying, or destruction.
	private ThemeSettings? m_themeSettings = null;
	private readonly SerialDisposable m_themeSettingsChangedRevoker = new(); // Runtime HC toggles must refresh cached HC-dependent brushes.
	// remove_Changed fail-fasts once the window is gone, hence the Destroying release. A token, not
	// auto_revoke: AppWindow has no weak reference support.
	// TODO Uno: AppWindow.Destroying is not implemented, so ThemeSettings is released on Unloaded only.
#if !HAS_UNO
	private Microsoft.UI.Windowing.AppWindow? m_appWindow = null;
	private readonly SerialDisposable m_appWindowDestroyingToken = new();
#endif

	// Cached HC state: kept fresh by ThemeSettings.Changed while loaded and read by IsHighContrast().
	// Only touched on the UI thread now, so no atomic is required.
	private bool m_isHighContrast = false;
	private readonly SerialDisposable m_loadedRevoker = new();

	private readonly SerialDisposable m_unloadedRevoker = new();
	// void OnTableViewUnloaded();
	// void ReleaseThemeSettings() noexcept;
	private bool m_rowsSourceDrained = false;
	// Unloaded released ownership of m_activeSource; Loaded reclaims it.
	private bool m_activeSourceReleased = false;
	// While released the rank hooks were off: edits went unseen and removed rows stayed ranked.
	private bool m_rerankCustomSortOnReconcile = false;
	// The source whose owner refused this table, which waits to be offered it on release.
	private WeakReference<TableViewSource>? m_awaitedSource = null;
	// void StopAwaitingSource();

	// bool IsContextMenuTargetCurrent(TableViewDetails::ContextMenuTarget const& target) const;
	// IsContextMenuTargetCurrent plus, for body targets, an unchanged source and no edit in progress.
	// bool IsContextMenuRequestCurrent(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     winrt::IInspectable const& sourceSnapshot);
	// Moves focus (and, for a cell, the current cell) to a context menu target. Returns false when
	// app code run by the move made the request stale.
	// bool FocusContextMenuTarget(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     winrt::IInspectable const& sourceSnapshot);
	// void OnHeaderContextRequested(winrt::ContextRequestedEventArgs const& args);
	// std::optional<TableViewDetails::ContextMenuTarget> ResolveHeaderContextMenuTarget(
	//     winrt::ContextRequestedEventArgs const& args);
	// TableViewDetails::ContextMenuCandidate ResolveContextFlyout(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     bool includeTableFlyout);
	// void ShowContextFlyout(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     TableViewDetails::ContextMenuCandidate const& candidate,
	//     winrt::ContextRequestedEventArgs const& args);
	private bool m_isProcessingContextMenu = false;

	// double ComputeLeadingFrozenWidth();
	// void RefreshFrozenColumns();

	// double GetHeaderMeasuredWidthForColumn(const winrt::TableViewColumn& column) const;
	// void ResolveColumnWidths();
	// Only Auto columns read realized-row widths, so row prepare/clear re-resolves only when one exists.
	// bool HasVisibleAutoColumn();
	// void InvalidateMeasureForRowRealizationChange();
	// void ResetColumnDesiredWidths();
	// Limits a resize gesture to the width the other columns can absorb. Unbounded when nothing
	// constrains it.
	// ColumnResizeBounds ResizeBoundsForColumn(const winrt::TableViewColumn& column);
	// bool IsColumnResizePinned(const winrt::TableViewColumn& column);
	// Collapses the gripper of every pinned column; it stays in the tree for keyboard resize lookup.
	// void RefreshResizeGripperAvailability();
	// void FreezeColumnsBeforeResize(const winrt::TableViewColumn& column, ColumnResizeDragState& state);
	// Hash of every input that sizes the columns other than user resizes; holds die when it changes.
	// size_t LockedColumnHoldsSignature();
	// winrt::ResizeGripper GetAvailableResizeGripper(const winrt::TableViewColumn& column);
	// bool IsColumnResizeAllowed(const winrt::TableViewColumn& column);
	// static void RestoreColumnWidth(const winrt::TableViewColumn& column, const winrt::IInspectable& localWidth);
	// The drag's width write, clamped; false when the width is already there.
	// static bool ApplyColumnResizeDrag(TableView* owner, const winrt::TableViewColumn& col, ColumnResizeDragState& state, double totalDelta);
	// Re-invalidate the header + realized row cells panels so they re-measure/arrange after a resolve.
	// void InvalidateCellPanels();

	private bool m_frozenColumnsActive = false;

	private bool m_rebuildHeadersQueued = false;
	private bool m_rebuildingHeaders = false;
	private bool m_inRebuildHeaders = false;
	private bool m_rebuildHeadersRequested = false;
	// Incompatible header Styles already reported, so each is logged once.
	private readonly List<WeakReference<Style>> m_loggedIncompatibleHeaderStyles = new();

	private bool m_columnsStructureChangedQueued = false;

	// Per-instance resource cache; replaces the former process-global map keyed by `this`.
	private readonly TableViewResourceCache m_resourceCache = new();

	private readonly List<TableViewColumn?> m_trackedColumns = new();

	private KeyEventHandler? m_keyDownHandler = null;  // Root KeyDown (handledEventsToo); registration is released with the element, no explicit RemoveHandler needed.
	// Set when a row already ran this key through OnKeyDownForNavigation.
	private bool m_navKeyHandledEarly = false;
	// void OnKeyDownForNavigation(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);

	// bool TryHandleHeaderColumnResizeKey(const winrt::KeyRoutedEventArgs& args);
	// Header-row arrows and clamping; Tab leaves the table.
	// bool TryHandleHeaderNavigationKey(const winrt::KeyRoutedEventArgs& args);
	// bool TryHandleHeaderVerticalKey(const winrt::KeyRoutedEventArgs& args);
	// Moves focus to a visible header by visible-column index; step 0 means exact only.
	// int32_t FocusVisibleHeaderFrom(int32_t visibleIndex, int32_t step);
	// Visible-column index of the header that currently has focus, or -1 when focus is elsewhere.
	// int32_t GetFocusedVisibleHeaderIndex() const;
	// Header entry resolves to the remembered column, or the nearest focusable header around it.
	// int32_t ResolveHeaderEntryIndex(const std::vector<winrt::FrameworkElement>& cells);
	// The table is one tab stop: Tab/Shift+Tab entry from outside lands on the header row, else the
	// body; focus aimed at a non-actionable header is redirected or cancelled.
	// void OnTableGettingFocus(
	//     const winrt::IInspectable& sender,
	//     const winrt::Microsoft::UI::Xaml::Input::GettingFocusEventArgs& args);
	// bool TryRedirectTableEntry(const winrt::Microsoft::UI::Xaml::Input::GettingFocusEventArgs& args);
	// bool TrySetBodyEntryTarget(const winrt::Microsoft::UI::Xaml::Input::GettingFocusEventArgs& args);
	// FocusEntryTarget ResolveBodyEntryTarget(bool last);
	// bool FocusBodyEntryTarget(bool select, winrt::FocusState focusState = winrt::FocusState::Keyboard);
	// void QueueFocusTableEntry(winrt::DependencyObject const& expected, bool bodyOnly);
	// void MoveFocusOffCollapsingHeaders();
	private readonly SerialDisposable m_headerCollapseFocusRevoker = new();
	// winrt::FrameworkElement FindOwnHeaderCell(winrt::DependencyObject const& element) const;
	// int32_t FindActionableHeaderIndex(const std::vector<winrt::FrameworkElement>& cells, int32_t visibleIndex, int32_t step);
	private readonly SerialDisposable m_tableGettingFocusRevoker = new();
	// void OnHeaderHostGotFocus(
	//     const winrt::IInspectable& sender,
	//     const winrt::RoutedEventArgs& args);
	private readonly SerialDisposable m_headerHostGotFocusRevoker = new();
	// Enter / Space on a focused, sortable column header: the keyboard path to sorting.
	// bool TryHandleHeaderSortKey(const winrt::KeyRoutedEventArgs& args);
	// bool TryHandleHeaderSortKeyUp(const winrt::KeyRoutedEventArgs& args);
	// winrt::TableViewColumn ResolveHeaderSortKeyTarget(const winrt::KeyRoutedEventArgs& args);
	private WeakReference<TableViewColumn>? m_headerSortSpaceArmedColumn = null;
	private readonly SerialDisposable m_headerSortLostFocusRevoker = new();
	private KeyEventHandler? m_keyUpHandler = null;
	// void OnKeyUpForHeaderSort(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);

	// Redirects a header's bring-into-view onto the body scroller, so the header cannot scroll
	// independently of the columns it labels.
	// void OnHeaderBringIntoViewRequested(const winrt::BringIntoViewRequestedEventArgs& args);
	// void OnBodyBringIntoViewRequested(const winrt::BringIntoViewRequestedEventArgs& args);

	// Pre-key focus snapshots are needed because XAML directional navigation can move focus before
	// bubbling handlers run.
	private KeyEventHandler? m_previewKeyDownHandler = null;

	private KeyEventHandler? m_editingKeyDownHandler = null;
	private readonly SerialDisposable m_editingLosingFocusRevoker = new();

	private bool m_focusLossCommitQueued = false;
	// Set while a focus-loss commit waits for the editor's own flyout to close.
	private readonly SerialDisposable m_editorFlyoutClosedRevoker = new();
	private readonly SerialDisposable m_editorFlyoutClosingRevoker = new();
	// Same, for any other popup that took focus from the editor (a picker, a button flyout).
	private readonly SerialDisposable m_editorPopupClosedRevoker = new();
	// True from the editor flyout's Closing until the current input has been dispatched.
	private bool m_editorFlyoutClosing = false;

	private int m_navAnchorRow = -1;
	private int m_navAnchorCellRow = -1;
	private int m_navAnchorCellColumn = -1;
	private int m_navAnchorHeaderColumn = -1;
	// Shared body/header visible-column cursor; stored as an index so it survives row recycling.
	private int m_currentCellColumn = 0;
	private int m_currentCellRow = -1;

	// Distinguishes column 0 from "no column chosen yet" so first header entry does not relocate.
	private bool m_columnCursorEstablished = false;
	// void SetColumnCursorInternal(int32_t visibleColumnIndex);
	// void ResetColumnCursorInternal();

	// Two-level ARIA treegrid body cursor: row level or cell level.
	private bool m_cellCursorActive = false;
	// Up/Down at cell level keep this column across rows where it lands on a neighbor or the row.
	private int m_cellIntentColumn = -1;
	private int m_cellIntentRow = -1;
	// Horizontal offset before row-level Right drilled in, so Left can undo it in one step.
	private double m_drillInOffset = -1.0;
	private int m_drillInRow = -1;
	// The offset our own reveals left after the drill-in; any other horizontal scroll (a touchpad pan,
	// app code) moves the body off it and expires the restore.
	private double m_drillSettledOffset = -1.0;
	private bool m_drillRevealPending = false;
	// The popped-out row's own focus bring-into-view arrives later and would undo the restore.
	private WeakReference<UIElement>? m_drillRestoreRow;
	private double m_drillRestoreOffset = -1.0;
	// Selection changes counted, so entry can tell a selection made after the remembered row.
	private uint m_selectionSequence = 0;
	private uint m_rememberedSelectionSequence = 0;
	private WeakReference<DependencyObject>? m_lastFocusedWithin = null;
	// True while Enter has moved focus into hosted cell content; Escape restores grid navigation
	// after hosted controls, such as ComboBox, handle their own Escape.
	private bool m_cellInteractionActive = false;
	// Weak: cells are recycled on every scroll.
	private WeakReference<FrameworkElement>? m_cellInteractionCell = null;
	// The row currently drilled in, so the cursor can pop it back to row level when it moves on.
	// Weak: rows are recycled on every scroll.
	private WeakReference<TableViewRow>? m_cellLevelRow = null;

	private Guid m_focusEntryRedirectCorrelationId = default;
	private bool m_hasFocusEntryRedirect = false;

	// ListView paging: the viewport edge row first, then one page past it.
	// int32_t GetPageTargetRow(int32_t currentRow, bool pageDown, int32_t rowCount);
	// Focus is exactly on one of this table's rows, group headers or cells, not on hosted content.
	// bool IsFocusExactlyOnBodyItem() const;
	// bool IsFocusInsideEditingElement() const;

	// Row-level Left/Right drill between row and cells; group headers use Left/Right for expand/collapse.
	// bool TryHandleRowLevelDrillKey(const winrt::KeyRoutedEventArgs& args);
	// bool ScrollBodyHorizontallyInternal(bool forward, bool toEdge = false, winrt::UIElement const& keepInView = nullptr);
	// bool TryHandleGroupHeaderExpandCollapseKey(const winrt::KeyRoutedEventArgs& args);
	// Enter from a focused cell moves into interactive display content; Escape moves back out.
	// bool TryHandleCellInteractionEnterKey(const winrt::KeyRoutedEventArgs& args);
	// bool TryHandleCellInteractionEscapeKey(const winrt::KeyRoutedEventArgs& args);
	// Swallows navigation keys the hosted control did not want, so XAML's directional navigation
	// cannot wander out of the cell while interaction mode is active.
	// bool TryHandleCellInteractionNavigationKey(const winrt::KeyRoutedEventArgs& args);
	// Focus helpers and pre-key anchors for the row/group-header levels.
	// bool FocusRowContainerInternal(
	//     winrt::UIElement const& element,
	//     winrt::FocusState focusState = winrt::FocusState::Keyboard);
	// bool FocusRowElementInternal(int32_t rowIndex, int32_t targetColumn, bool cellLevel, std::optional<double> alignRatio);
	// int32_t GetFocusedRowContainerIndex() const;
	// int32_t GetFocusedGroupHeaderIndex() const;
	// bool TryMoveFocusToHeaderBandFromBody(int32_t visibleColumnIndex);
	private int m_navAnchorRowContainer = -1;
	private int m_navAnchorGroupHeader = -1;

	// void OnPreviewKeyDownForNavigation(
	//     const winrt::IInspectable& sender,
	//     const winrt::KeyRoutedEventArgs& args);

	// bool TryHandleCellNavigationKey(const winrt::KeyRoutedEventArgs& args);

	// Moves the cell cursor from a pre-key anchor; consumes handled cell-navigation keys, including
	// boundaries. Pass -1, -1 when no routed event is in flight.
	// bool TryMoveCellCursorFromAnchor(
	//     winrt::Windows::System::VirtualKey key, bool isControlDown,
	//     int32_t anchorRow, int32_t anchorColumn);

	private const int c_lastColumnSentinel = 0x7ffffffe;

	// winrt::TableViewRow GetRealizedRowAt(int32_t rowIndex) const;

	// int32_t GetFocusedRowIndex() const;
	// int32_t GetEstimatedRowsPerPage(); // Non-const — GetDensityRowMinHeight() mutates the resource cache.
}
