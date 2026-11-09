// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRow.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	internal struct TerminalGridLineSuppressionState
	{
		public bool suppressTrailing;
		public bool suppressBottom;
	}

	// TableViewRow();

	// IFrameworkElement overrides
	// void OnApplyTemplate();
	// winrt::AutomationPeer OnCreateAutomationPeer();

	// Use the Control virtual signatures; event-handler overloads break the ABI shim.
	// void OnPointerEntered(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerExited(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerPressed(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerReleased(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCaptureLost(winrt::PointerRoutedEventArgs const& args);
	// void OnPointerCanceled(winrt::PointerRoutedEventArgs const& args);
	// Keeps the body one tab stop, and expands a tree row's subtree; see the definition.
	// void OnKeyDown(winrt::KeyRoutedEventArgs const& args);

	// Updates the weak owner ref, column subscription, and realized cells. Both return true when the
	// cell rebuild also refreshed the row chrome (grid lines and row background).
	// rebuildCells=false skips the rebuild (recycle-out keeps the realized cells for the restamp).
	// bool SetOwningTableViewInternal(winrt::TableView const& owner, bool rebuildCells = true);
	// skipIfCellsCurrent: skip when the DataContext change that just re-bound this row rebuilt the cells.
	// bool EnsureOwningTableViewInternal(winrt::TableView const& owner, bool skipIfCellsCurrent = false);

	// Rewire realized rows when the owner keeps the same identity but Columns changes.
	// void RefreshColumnsSubscriptionInternal();

	// Typed accessor for the owning TableView.
	// winrt::TableView GetOwningTableView();
	// bool IsContextMenuTargetCurrent(TableViewDetails::ContextMenuTarget const& target) const;
	// Row automation peer ShowContextMenu. False when TableView left it alone, so the peer falls
	// back to the framework (TableView.ContextFlyout and ancestors).
	// bool ShowContextMenuFromPeer();

	// void RefreshGridLines();
	// void SetTerminalGridLineSuppression(TerminalGridLineSuppressionState state, bool refresh = true);
	// void RefreshRowBackground();

	// Owner-only writer for the read-only IsSelected DP. Selection is owned by the TableView, so
	// this is the main entry point that both publishes the DP and re-enters the CommonStates VSM
	// with transitions. The only other writer is the recycle path in SetOwningTableViewInternal,
	// which clears it without transitions.
	// void SetIsSelectedInternal(bool isSelected);

	// Owner-only writer for the read-only hierarchy DPs. Level 0 clears (flat/grouped sources).
	// void SetHierarchyStateInternal(int32_t level, bool isExpandable, bool isExpanded);

	// Re-applies indent and chevron; template apply and cell rebuild both discard prior layout.
	// void ApplyHierarchyAffordance();
	// void ApplyHierarchyIndentToCells();
	// Overload taking a pre-resolved indent, avoiding a second TableViewRowIndentSize lookup.
	// void ApplyHierarchyIndentToCells(double indent);
	// double HierarchyIndent();

	// ExpandCollapsePattern state: LeafNode unless an expandable tree row.
	// winrt::ExpandCollapseState HierarchyExpandCollapseState();

	// Used by automation peers to enumerate live cells after template application.
	internal Panel? GetCellsHostPanelInternal() => m_cellsHost;
	// winrt::FrameworkElement GetLastVisibleCellInternal() const;
	// winrt::TableViewColumn GetCellOwningColumn(const winrt::UIElement& cellElement) const;

	// ----- Cell-level keyboard focus -----
	//
	// Cells are the UIA focus targets; these are the row-side helpers the control drives.

	// Visible-cell coordinates match TableViewCellAutomationPeer::Column.
	// int32_t GetVisibleCellCountInternal() const;

	// winrt::UIElement GetVisibleCellInternal(int32_t visibleColumnIndex) const;

	// int32_t GetVisibleCellIndexInternal(const winrt::UIElement& cell) const;

	// winrt::UIElement FindOwnCellInternal(const winrt::DependencyObject& element, bool requireExact) const;

	// Focuses a visible cell after drilling to cell level; falls back to the row if needed.
	// bool FocusVisibleCellInternal(int32_t visibleColumnIndex, winrt::FocusState state);

	// Editable, hosting focusable content, or carrying a tooltip. Only these take keyboard focus.
	// bool IsCellActionableInternal(const winrt::UIElement& cell) const;
	// First actionable cell from startVisibleIndex (inclusive) in step's direction; -1 when none.
	// int32_t FindActionableCellInternal(int32_t startVisibleIndex, int32_t step) const;

	// Switches the ARIA treegrid body between row level and cell level; see the definition for why
	// this has to move IsTabStop.
	// void SetCellLevelInternal(bool isCellLevel);
	internal bool IsCellLevelInternal() => m_isCellLevel;
	// Makes the row focusable before the pop-out path disarms cells.
	// void EnableRowFocusInternal();

	// void RefreshFrozenColumnLayout(double horizontalOffset, double leadingFrozenWidth);
	// void SyncExpanderGutterWithLeadCell(winrt::Panel const& host);
	// Cells panel reports the lead cell's arranged slot to confine the chevron. leadWidth < 0: none.
	// void OnCellsArrangedInternal(double leadLeft, double leadWidth, double height);
	// void RefreshDensity();
	// void RefreshCells();

	// Releases control-owned cell tooltips (recycle-out).
	// void ReleaseCellToolTips();
	// void RefreshCellToolTips();
	// A live tooltip value change restamps the cell level when the cell's actionability flipped.
	// void OnCellToolTipChangedInternal(const winrt::UIElement& cell);
	// Raises NameProperty changed on cell peers that already exist; creates none.
	// void RaiseCellAutomationNamesChangedInternal(winrt::UIElement const& skipCell = nullptr);

	// void RefreshColumnVisibility(const winrt::TableViewColumn& column, winrt::Visibility visibility);
	// double MeasuredWidthForColumn(const winrt::TableViewColumn& column) const;
	// void InvalidateCells();


	// bool BeginCellEdit(const winrt::TableViewColumn& column, const winrt::IInspectable& dataItem);

	// Restores the display visual. Safe to call when no cell edit is open.
	// void EndCellEdit(winrt::TableViewEditAction action, std::optional<HRESULT>* displayFailure = nullptr, bool* applicationFault = nullptr);

	internal FrameworkElement? GetEditingElement() => m_editingElement;

	internal UIElement? GetEditingCellWrapper() => m_editingCellWrapper;
	internal UIElement? GetDisplayElementForAutomation(UIElement? cell)
	{
		return ReferenceEquals(m_editingCellWrapper, cell) || (m_pendingEditingCell is not null && m_pendingEditingCell.TryGetTarget(out var pending) && ReferenceEquals(pending, cell))
			? m_editingDisplayElement : null;
	}

	// Drops the edit bookkeeping and puts the display visual back, WITHOUT moving focus. Used on the
	// recycle / rebuild paths, which run inside the measure pass where changing focus would trip
	// XAML's re-entrancy guard (0xc0000420).
	// void AbandonCellEdit();

	// void OnPointerPressedForEditing(
	//     const winrt::IInspectable& sender,
	//     const winrt::PointerRoutedEventArgs& args);

	// private:
	// void ResetCellAutomationNames();
	// void UpdateCellPeerItems(winrt::IInspectable const& dataItem);
	// bool HasCellAutomationPeers() const;

	// Restamps the current level after rebuilds because new/recycled cells arrive as tab stops.
	// void ApplyFocusLevelInternal();
	// bool SetCellsTabStopInternal(bool isTabStop);

	// bool IsGridLevelFocusInternal();

	// Temporarily removes cell content from the tab order for one Tab walk; see the definition.
	// void SuppressCellContentTabStopsForTabWalkInternal();
	// void RestoreCellContentTabStopsInternal();

	// void OnRowGotFocus(winrt::DependencyObject const& focusedElement);
	// Routes an expansion-state transition to this row's automation peer, if a client is listening.
	// void RaiseExpandCollapseStateChanged(winrt::ExpandCollapseState oldState, winrt::ExpandCollapseState newState);

	// True when focus is on the row itself, not cell content (which keeps its own arrow keys).
	// bool IsRowItselfFocused();

	// Directional expand/collapse for the keyboard, routed through the owner like the chevron.

	// Installs a generated display element as a cell's content, wiring the ContentPresenter Content
	// binding a template column needs. GenerateElement alone is not a complete cell.
	// void AttachCellContent(const winrt::Grid& cellWrapper, const winrt::FrameworkElement& cellElement);

	// Drops begin-edit gesture state (recycle, owner change).
	// void ResetPressState();

	// winrt::TableViewColumn ResolvePressedColumn(
	//     const winrt::IInspectable& originalSource,
	//     const winrt::Point& hostPoint);
	// void OnCellsHostContextRequested(winrt::ContextRequestedEventArgs const& args);
	// void OnTemplateRootContextRequested(winrt::ContextRequestedEventArgs const& args);
	// void OnRowContextRequested(winrt::ContextRequestedEventArgs const& args);
	// void ProcessContextRequest(
	//     TableViewDetails::ContextMenuTarget const& target,
	//     winrt::ContextRequestedEventArgs const& args);
	// std::optional<TableViewDetails::ContextMenuTarget> MakeRowContextMenuTarget();
	// std::optional<TableViewDetails::ContextMenuTarget> ResolveContextMenuTarget(
	//     winrt::ContextRequestedEventArgs const& args);
	// std::optional<TableViewDetails::ContextMenuTarget> ResolveContextMenuTarget(
	//     winrt::DependencyObject const& source);

	// void OnDataContextChanged(
	//     const winrt::FrameworkElement& sender,
	//     const winrt::DataContextChangedEventArgs& args);

	// void OnColumnsVectorChanged(
	//     const winrt::IObservableVector<winrt::TableViewColumn>& sender,
	//     const winrt::IVectorChangedEventArgs& args);

	// Detach from / attach to the owning TableView's Columns vector. Shared by
	// SetOwningTableViewInternal (owner add/clear) and RefreshColumnsSubscriptionInternal
	// (same owner, Columns replaced). AttachColumnsSubscription is a no-op when owner is null.
	// void DetachColumnsSubscription();
	// void AttachColumnsSubscription(winrt::TableView const& owner);

	// Routes IsEnabled changes into UpdateVisualState so the row's
	// Disabled VSM activates when consumers toggle row IsEnabled at runtime.
	// void OnIsEnabledChanged(
	//     const winrt::Windows::Foundation::IInspectable& sender,
	//     const winrt::Microsoft::UI::Xaml::DependencyPropertyChangedEventArgs& args);

	// void OnExpanderGutterPointerPressed(
	//     const winrt::IInspectable& sender,
	//     const winrt::PointerRoutedEventArgs& args);
	// void OnExpanderGutterPointerReleased(
	//     const winrt::IInspectable& sender,
	//     const winrt::PointerRoutedEventArgs& args);
	// void OnExpanderGutterPointerCanceled(
	//     const winrt::IInspectable& sender,
	//     const winrt::PointerRoutedEventArgs& args);
	// bool IsWithinExpanderGutter(const winrt::IInspectable& source) const;
	// void UpdateExpanderGutterClip();

	// Returns true when the rebuild reached the row-chrome refresh (grid lines and row background).
	// bool RebuildCells(bool updateExistingCellPeerItems = true);
	// void ClearOwnedCellToolTips(const winrt::Panel& host);

	// Coalesces a burst of Columns-collection changes into a single cell rebuild on the next
	// dispatcher tick (each realized row observes Columns, so N bulk edits would otherwise cost N
	// full RebuildCells per row). Recycle / owner / DataContext paths still rebuild synchronously.
	// void QueueRebuildCells();
	// void InvalidateCellsCurrent();
	// void UpdateVisualState(bool useTransitions);

	private Panel? m_cellsHost;
	private Border? m_gridLineBorder;
	// Chevron hit target. Null if a re-template drops the part (indent still applies).
	private FrameworkElement? m_rowExpanderGutter;
	// Transparent cell wrapper fill, created on first use and shared by this row's cells.
	private Brush? m_cellWrapperBackground = null;
	private readonly SerialDisposable m_gutterPointerPressedRevoker = new();
	private readonly SerialDisposable m_gutterPointerReleasedRevoker = new();
	private readonly SerialDisposable m_gutterPointerExitedRevoker = new();
	private readonly SerialDisposable m_gutterPointerCaptureLostRevoker = new();
	// Chevron toggles on release-inside, like TableViewGroupHeader.
	private bool m_isGutterPressed = false;
	// Lead cell's last arranged slot in the cells host (negative width: none yet / none visible).
	private double m_leadCellLeft = 0.0;
	private double m_leadCellWidth = -1.0;
	private double m_cellsArrangedHeight = 0.0;
	// Distinguishes "not arranged yet" from "arranged with no visible cell" (both report width -1).
	private bool m_cellsArranged = false;
	// Use auto_revoke for self-event subscriptions instead of manual token cleanup.
	private readonly SerialDisposable m_dataContextChangedRevoker = new();
	private readonly SerialDisposable m_cellsHostContextRequestedRevoker = new();
	private readonly SerialDisposable m_templateRootContextRequestedRevoker = new();
	private readonly SerialDisposable m_contextRequestedRevoker = new();
	private readonly SerialDisposable m_contextFlyoutOpeningRevoker = new();
	private readonly SerialDisposable m_isEnabledChangedRevoker = new();
	private readonly SerialDisposable m_gotFocusRevoker = new();
	private readonly SerialDisposable m_unloadedRevoker = new();
	private WeakReference<TableView>? m_owningTableView = null;
	private readonly SerialDisposable m_columnsVectorChangedRevoker = new();
	private WeakReference<IObservableVector<TableViewColumn>>? m_observedColumns;

	private bool m_isPointerOver = false;
	private bool m_isPressed = false;
	private bool m_suppressTrailingGridLine = false;
	private bool m_suppressBottomGridLine = false;

	// Selection commits on pointer-release (ListViewBaseItem parity), so a pan or a cancelled
	// press does not select the row it started on. The id pins it to the arming pointer.
	private bool m_selectOnPointerRelease = false;
	private uint m_selectPointerId = 0;

	// False = row-level focus; true = cell-level focus. Recycled rows return at row level.
	private bool m_isCellLevel = false;

	// originalLocalValue: raw local value (UnsetValue, boxed bool, or BindingExpression).
	private struct SuppressedContentTabStop
	{
		public WeakReference<UIElement>? element;
		public object? originalLocalValue;
	}

	private readonly List<SuppressedContentTabStop> m_suppressedContentTabStops = new();

	// Prevent DataContextChanged re-entry while RebuildCells updates child DCs.
	private bool m_isRebuildingCells = false;

	private bool m_rebuildCellsQueued = false;

	// One-shot: set by the DataContext rebuild, consumed by the next ElementPrepared, cleared by any other rebuild.
	private bool m_cellsRebuiltForDataContext = false;
	// Cells were cleared or a rebuild was dropped/queued while the DataContext rebuild ran.
	private bool m_cellsInvalidated = false;
	// Between ElementClearing (SetOwningTableViewInternal without rebuild) and the next prepare.
	private bool m_inRecyclePool = false;

	private TableViewColumn? m_editingColumn;
	private Grid? m_editingCellWrapper;
	private WeakReference<UIElement>? m_pendingEditingCell = null;
	private FrameworkElement? m_editingElement;
	private UIElement? m_editingDisplayElement;
	private WeakReference<TableViewCellAutomationPeer>? m_editingAutomationPeer = null;
	private object? m_editingAutomationItem;
	private string m_editingAutomationValue = "";
	private string m_editingAutomationName = "";
	// Row Name before the edit, so a commit that changes a derived value in other cells can publish it.
	private string? m_editingRowAutomationName;

	// Begin-edit gesture state. Held per row rather than on the control: a double-click that starts
	// on one row and finishes on another is not a double-click, and per-row state makes that fall
	// out for free. Registered handler is kept alive so it can be removed on teardown.
	private PointerEventHandler? m_editingPointerPressedHandler = null;
	private ulong m_lastPressTimestamp = 0;
	private Point m_lastPressPosition = default;
	private PointerDeviceType m_lastPressDeviceType = PointerDeviceType.Mouse;
	private TableViewColumn? m_lastPressColumn;
	private object? m_lastPressItem;
}
