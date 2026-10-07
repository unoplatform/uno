// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRow.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using static Microsoft.UI.Xaml.Controls._Tracing;
using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	private const string s_CellsHostPartName = "PART_CellsHost";
	private const string s_GridLineBorderPartName = "PART_GridLineBorder";

	private static readonly Thickness s_verticalThickness = new(0, 0, 1, 0);
	// The cell's trailing edge is its LEFT edge under RTL. The cells panel arranges at explicit
	// physical coordinates, so this subtree is not auto-mirrored and a right-sided thickness would
	// draw every body grid line one full column away from the header grid line above it.
	private static readonly Thickness s_verticalThicknessRtl = new(1, 0, 0, 0);
	private static readonly Thickness s_zeroThickness = new(0, 0, 0, 0);

	private static SolidColorBrush? s_transparent;

	// Shared transparent fill for cell wrappers. Cached because rows and cells are rebuilt on every
	// scroll, and a fresh brush per cell is pure allocation for a value that never varies.
	private static Brush TransparentBrush()
	{
		return s_transparent ??= new SolidColorBrush(Colors.Transparent);
	}

	private static bool WantsHorizontalLines(TableViewGridLinesVisibility visibility)
	{
		return visibility == TableViewGridLinesVisibility.Horizontal ||
			visibility == TableViewGridLinesVisibility.All;
	}

	private static bool WantsVerticalLines(TableViewGridLinesVisibility visibility)
	{
		return visibility == TableViewGridLinesVisibility.Vertical ||
			visibility == TableViewGridLinesVisibility.All;
	}

	// Slop allowed between the two presses of a double-click, in DIPs.
	//
	// SM_CXDOUBLECLK is in PHYSICAL pixels, so it must be divided by the element's rasterization
	// scale to be comparable with the DIP-space positions this gesture works in. Without that the
	// effective tolerance halves at 200% - a user whose two presses land 6 DIPs apart is inside the
	// system tolerance but outside ours, and the double-click is silently dropped.
	//
	// Read per press rather than cached: the metric changes with the mouse control panel and the
	// scale changes when the window moves between monitors, and a cached value never sees either.
	// GetSystemMetrics is a cheap cached read in user32, not a round trip.
	private static double GetDoubleClickSlop(UIElement? element)
	{
		// TODO Uno: 4 is the Windows default for GetSystemMetrics(SM_CXDOUBLECLK / SM_CYDOUBLECLK); no per-host source.
		// const auto cx = static_cast<double>(::GetSystemMetrics(SM_CXDOUBLECLK));
		// const auto cy = static_cast<double>(::GetSystemMetrics(SM_CYDOUBLECLK));
		const double cx = 4.0;
		const double cy = 4.0;
		double physical = Math.Max(Math.Max(cx, cy), 4.0) / 2.0;

		double scale = 1.0;
		if (element is not null)
		{
			if (element.XamlRoot is { } root)
			{
				var rasterization = root.RasterizationScale;
				if (rasterization > 0.0)
				{
					scale = rasterization;
				}
			}
		}

		return physical / scale;
	}

	// PointerPoint.Timestamp is in microseconds; the system double-click time is in milliseconds.
	// Not cached, so a change made in the mouse control panel takes effect immediately.
	private static ulong GetDoubleClickIntervalMicroseconds()
	{
		// return static_cast<uint64_t>(::GetDoubleClickTime()) * 1000ull;
		return (ulong)UISettings.GetDoubleClickTime() * 1000UL;
	}

	public TableViewRow()
	{
		this.SetTabularDefaultStyleKey();

		var weakRow = new WeakReference<TableViewRow>(this);

		// handledEventsToo: a row that participates in selection marks the press handled, which also
		// suppresses XAML's gesture recognizer - this registration is what keeps begin-edit reachable.
		m_editingPointerPressedHandler = new PointerEventHandler(
			(object sender, PointerRoutedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnPointerPressedForEditing(sender, args);
				}
			});
		AddHandler(UIElement.PointerPressedEvent, m_editingPointerPressedHandler, true /* handledEventsToo */);

		// auto_revoke owns this self-event subscription until row destruction.
		TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> dataContextChangedHandler =
			(FrameworkElement sender, DataContextChangedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnDataContextChanged(sender, args);
				}
			};
		DataContextChanged += dataContextChangedHandler;
		m_dataContextChangedRevoker.Disposable = Disposable.Create(() => DataContextChanged -= dataContextChangedHandler);

		// Keep CommonStates VSM in sync with IsEnabled so the Disabled
		// state activates when consumers toggle row IsEnabled at runtime.
		// auto_revoke owns this self-event subscription until row destruction.
		DependencyPropertyChangedEventHandler isEnabledChangedHandler =
			(object sender, DependencyPropertyChangedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnIsEnabledChanged(sender, args);
				}
			};
		IsEnabledChanged += isEnabledChangedHandler;
		m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= isEnabledChangedHandler);

		TypedEventHandler<UIElement, GettingFocusEventArgs> gettingFocusHandler =
			(UIElement sender, GettingFocusEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnRowGettingFocus(sender, args);
				}
			};
		GettingFocus += gettingFocusHandler;
		m_gettingFocusRevoker.Disposable = Disposable.Create(() => GettingFocus -= gettingFocusHandler);

		RoutedEventHandler gotFocusHandler =
			(object sender, RoutedEventArgs args) =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.OnRowGotFocus();
				}
			};
		GotFocus += gotFocusHandler;
		m_gotFocusRevoker.Disposable = Disposable.Create(() => GotFocus -= gotFocusHandler);
	}

	private void OnIsEnabledChanged(
		object sender,
		DependencyPropertyChangedEventArgs args)
	{
		UpdateVisualState(true /* useTransitions */);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		ResetCellAutomationNames();
		var host = GetTemplateChild(s_CellsHostPartName) as Panel;
		m_cellsHost = host;
		m_gridLineBorder = GetTemplateChild(s_GridLineBorderPartName) as Border;

		if (host is not null)
		{
			// Match PART_HeaderHost: scope TabFocusNavigation at the host, not the shared
			// TableViewCellsPanel primitive.
			host.TabFocusNavigation = KeyboardNavigationMode.Once;
		}

		if (host is TableViewCellsPanel cellsPanel)
		{
			cellsPanel.SetOwningRowInternal(this);
		}

		RebuildCells();

		UpdateVisualState(false /* useTransitions */);
	}

	internal void SetTerminalGridLineSuppression(TerminalGridLineSuppressionState state)
	{
		m_suppressTrailingGridLine = state.suppressTrailing;
		m_suppressBottomGridLine = state.suppressBottom;
		RefreshGridLines();
	}

	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new TableViewRowAutomationPeer(this);
	}

	// Typed accessor for the owning TableView.
	internal TableView? GetOwningTableView()
	{
		// Keep the owner weak; callers acquire a strong ref only for synchronous work.
		return m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner) ? owner : null;
	}

	internal TableViewColumn? GetCellOwningColumn(UIElement? cellElement)
	{
		if (cellElement is FrameworkElement cellFE)
		{
			if (cellFE.Tag is TableViewColumn column)
			{
				return column;
			}
		}

		return null;
	}

	// ----- Cell-level keyboard focus -----

	internal int GetVisibleCellCountInternal()
	{
		int count = 0;
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				// Same predicate as TableViewCellAutomationPeer::Column and the row peer's children, so
				// the keyboard coordinate space and the UIA one cannot drift apart.
				if (child is not null && IsVisibleColumn(GetCellOwningColumn(child)))
				{
					++count;
				}
			}
		}
		return count;
	}

	internal UIElement? GetVisibleCellInternal(int visibleColumnIndex)
	{
		if (visibleColumnIndex < 0)
		{
			return null;
		}

		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			int visible = 0;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				if (child is null || !IsVisibleColumn(GetCellOwningColumn(child)))
				{
					continue;
				}
				if (visible == visibleColumnIndex)
				{
					return child;
				}
				++visible;
			}
		}
		return null;
	}

	internal int GetVisibleCellIndexInternal(UIElement? cell)
	{
		if (cell is null)
		{
			return -1;
		}

		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			int visible = 0;
			for (int i = 0; i < size; ++i)
			{
				var child = children[i];
				if (child is null || !IsVisibleColumn(GetCellOwningColumn(child)))
				{
					continue;
				}
				if (ReferenceEquals(child, cell))
				{
					return visible;
				}
				++visible;
			}
		}
		return -1;
	}

	internal UIElement? FindOwnCellInternal(
		DependencyObject? element, bool requireExact)
	{
		var host = m_cellsHost;
		if (element is null || host is null)
		{
			return null;
		}

		DependencyObject? current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, host))
			{
				return null;
			}

			if (current is UIElement candidate)
			{
				if (ReferenceEquals(VisualTreeHelper.GetParent(candidate), host) &&
					IsVisibleColumn(GetCellOwningColumn(candidate)))
				{
					return (!requireExact || ReferenceEquals(candidate, element))
						? candidate : null;
				}
			}

			if (requireExact)
			{
				return null;
			}

			current = VisualTreeHelper.GetParent(current);
		}

		return null;
	}

	internal FrameworkElement? GetLastVisibleCellInternal()
	{
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			for (int i = children.Count; i > 0; --i)
			{
				if (children[i - 1] is FrameworkElement cell &&
					cell.Visibility == Visibility.Visible &&
					cell.ActualWidth > 0.0)
				{
					return cell;
				}
			}
		}

		return null;
	}

	internal bool FocusVisibleCellInternal(int visibleColumnIndex, FocusState state)
	{
		if (GetVisibleCellInternal(visibleColumnIndex) is { } cell)
		{
			if (cell is FrameworkElement cellFE)
			{
				cellFE.StartBringIntoView();
			}

			// Drill in BEFORE focusing: at row level the cells are not tab stops, and
			// CUIElement::IsFocusable requires IsTabStop even for a programmatic Focus().
			SetCellLevelInternal(true);

			if (cell.Focus(state))
			{
				return true;
			}

			// The cell refused (collapsed column, disabled subtree, a focus operation already in
			// flight). Undo the drill-in rather than leaving the row in a state where neither level is
			// a tab stop, which would strand the body with no reachable focus target at all.
			SetCellLevelInternal(false);
		}

		return Focus(state);
	}

	// ----- Two-level focus: ROW level vs CELL level -----
	//
	// Body focus is either the row or one of its cells, never both. XAML tab search enters children
	// before consulting TabFocusNavigation, so a focusable row with focusable cells creates extra
	// forward/reverse tab stops.
	//
	// Gating IsTabStop at both ends keeps the body one tab stop while still allowing row/cell arrow
	// navigation. Cells default to IsTabStop(true); row policy stamps the current level.
	internal void SetCellLevelInternal(bool isCellLevel)
	{
		m_isCellLevel = isCellLevel;
		ApplyFocusLevelInternal();
	}

	// Re-applies the current level to the live cells. Called after any rebuild, because new cell
	// wrappers arrive with IsTabStop(true) and would otherwise re-open the row-level Tab leak.
	private void ApplyFocusLevelInternal()
	{
		// Make the incoming level focusable before clearing the outgoing one, or the row has no focus
		// target during the handoff.
		if (m_isCellLevel)
		{
			SetCellsTabStopInternal(true);
			IsTabStop = false;
		}
		else
		{
			IsTabStop = true;
			SetCellsTabStopInternal(false);
		}
	}

	private void SetCellsTabStopInternal(bool isTabStop)
	{
		if (m_cellsHost is { } host)
		{
			var children = host.Children;
			int size = children.Count;
			for (int i = 0; i < size; ++i)
			{
				if (children[i] is { } child)
				{
					child.IsTabStop = isTabStop;
				}
			}
		}
	}

	// Pop-out must make the row focusable before clearing the focused cell from tab order.
	internal void EnableRowFocusInternal()
	{
		IsTabStop = true;
	}

	private void OnRowGettingFocus(
		UIElement sender,
		GettingFocusEventArgs args)
	{
		DependencyObject selfObject = this;
		var newFocus = args.NewFocusedElement;
		if (newFocus is null)
		{
			return;
		}

		// Tab / Shift+Tab only. Arrow navigation, an edit-close restore and a pointer press all name
		// the row they mean; redirecting those would move the user somewhere they did not ask for.
		var direction = args.Direction;
		bool isTabEntry =
			direction == FocusNavigationDirection.Next ||
			direction == FocusNavigationDirection.Previous;
		if (!isTabEntry)
		{
			return;
		}

		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		var ownerImpl = owner;

		// An open editor owns focus; redirecting the row focus the editor teardown performs would
		// fight the row's own "move focus off the editor before it leaves the tree" step.
		if (ownerImpl.IsEditing && m_editingElement is not null)
		{
			return;
		}

		// Focus LEAVING this row must never be pulled back, or Tab can never exit the table. Only
		// focus arriving from outside the row is an entry that wants resolving.
		var oldFocus = args.OldFocusedElement;
		if (ReferenceEquals(oldFocus, selfObject) ||
			SharedHelpers.IsAncestor(oldFocus!, selfObject, false /* checkVisibility */))
		{
			return;
		}

		// When returning from another band inside the table, stale cell-level state can make XAML aim
		// the body's single tab stop at a cell. Body band entry is still row-level; outside re-entry is
		// left alone so it can resume the previously focused cell.
		if (!ReferenceEquals(newFocus, selfObject))
		{
			DependencyObject ownerObject = owner;
			bool focusCameFromWithinTable =
				ReferenceEquals(oldFocus, ownerObject) ||
				SharedHelpers.IsAncestor(oldFocus!, ownerObject, false /* checkVisibility */);
			if (!focusCameFromWithinTable || FindOwnCellInternal(newFocus, false /* requireExact */) is null)
			{
				return;
			}

			SetCellLevelInternal(false);
			ownerImpl.SetCellCursorActiveInternal(false);
			args.TrySetNewFocusedElement(selfObject);
			return;
		}

		// Redirect body Tab entry from the first repeater row to the remembered row.
		var target = ownerImpl.ResolveFocusEntryRow(this, oldFocus);
		if (target is null || ReferenceEquals(target, this))
		{
			return;
		}

		DependencyObject? targetObject = target;
		if (targetObject is null || ReferenceEquals(targetObject, newFocus))
		{
			return;
		}

		// Body entry is row-level; reset the remembered row before redirecting or it is not focusable.
		target.SetCellLevelInternal(false);
		ownerImpl.SetCellCursorActiveInternal(false);

		// TrySetNewFocusedElement is refused during some focus operations (a programmatic move already
		// in flight, for one). Failing is fine - focus simply stays on the row XAML aimed at, which is
		// still a row-level landing in the body.
		args.TrySetNewFocusedElement(targetObject);
	}

	private void OnRowGotFocus()
	{
		if (GetOwningTableView() is { } owner)
		{
			owner.OnRowCellFocusChanged(this);
		}
	}

	private void DetachColumnsSubscription()
	{
		if (m_observedColumns is not null && m_observedColumns.TryGetTarget(out _))
		{
			m_columnsVectorChangedRevoker.Disposable = null;
			m_observedColumns = null;
		}
	}

	private void AttachColumnsSubscription(TableView? owner)
	{
		if (owner is null)
		{
			return;
		}

		// Cast the ABI vector to its observable backing type for VectorChanged.
		if (owner.Columns is IObservableVector<TableViewColumn> observable)
		{
			var weakRow = new WeakReference<TableViewRow>(this);
			VectorChangedEventHandler<TableViewColumn> handler =
				(IObservableVector<TableViewColumn> sender, IVectorChangedEventArgs args) =>
				{
					if (weakRow.TryGetTarget(out var strongRow))
					{
						strongRow.OnColumnsVectorChanged(sender, args);
					}
				};
			observable.VectorChanged += handler;
			m_columnsVectorChangedRevoker.Disposable = Disposable.Create(() => observable.VectorChanged -= handler);
			m_observedColumns = new WeakReference<IObservableVector<TableViewColumn>>(observable);
		}
	}

	internal void SetOwningTableViewInternal(TableView? owner)
	{
		var currentOwner = GetOwningTableView();
		// Compare through the typed accessor so unchanged weak refs hit the no-op path.
		if (currentOwner == owner)
		{
			return;
		}

		// Single-writer: the recycle cycle clears the owner to null between tables; a direct
		// table-A -> table-B switch (no intervening clear) is a cross-table leak, so reject it.
		if (currentOwner is not null && owner is not null)
		{
			// Runs inside an ItemsRepeater callback; an escaping throw would failfast.
			// Assert in debug, no-op in retail (keep current owner, skip re-attach).
			MUX_ASSERT(false, "TableViewRow re-owned without an intervening clear");
			return;
		}

		DetachColumnsSubscription();

		if (owner is not null)
		{
			m_owningTableView = new WeakReference<TableView>(owner);
		}
		else
		{
			ResetCellAutomationNames();
			m_owningTableView = null;
			// Reset transient interaction state so a row recycled while hovered/pressed
			// re-enters the pool in Normal state (ListViewItem parity), not a stale tint.
			m_isPointerOver = false;
			m_isPressed = false;
			m_selectOnPointerRelease = false;

			// Selection belongs to the item, not the container: a pooled row must not carry selected
			// chrome onto its next item. RefreshRowSelectionState restamps it on the way back in.
			IsSelected = false;

			// Begin-edit gesture state must go too. A row recycled away and back to the SAME item
			// inside the double-click interval would otherwise turn the next single click into an
			// edit, and the stale trackers keep the previous item and column alive.
			ResetPressState();

			// Recycled rows return at row level; otherwise a drilled row can reappear unreachable by Tab.
			m_isCellLevel = false;

			UpdateVisualState(false);
		}

		// Observe the new owner's Columns vector (no-op on recycle-out when owner is null).
		AttachColumnsSubscription(owner);

		RebuildCells();
	}

	internal void EnsureOwningTableViewInternal(TableView? owner)
	{
		if (owner is null)
		{
			return;
		}

		if (GetOwningTableView() != owner)
		{
			SetOwningTableViewInternal(owner);
			return;
		}

		var rowPeer = FrameworkElementAutomationPeer.FromElement(this) as TableViewRowAutomationPeer;
		var peerImpl = rowPeer;
		if (peerImpl is not null && !peerImpl.CanReuseForRowItem(this, owner))
		{
			if (m_cellsHost is { } host)
			{
				ResetCellAutomationNames();
				host.Children.Clear();
			}
			peerImpl.DropCellPeerCache();
			RebuildCells(false /* updateExistingCellPeerItems */);
			peerImpl.TrackCurrentRowItem(this, owner);
			return;
		}

		RebuildCells();

		if (peerImpl is not null)
		{
			peerImpl.TrackCurrentRowItem(this, owner);
		}
	}

	// Rewire realized rows when Columns changes but the owner identity does not.
	internal void RefreshColumnsSubscriptionInternal()
	{
		DetachColumnsSubscription();

		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		AttachColumnsSubscription(owner);

		if (m_cellsHost is { } host)
		{
			ResetCellAutomationNames();
			host.Children.Clear();
		}

		RebuildCells();
	}

	private void OnDataContextChanged(
		FrameworkElement sender,
		DataContextChangedEventArgs args)
	{
		// On recycle ItemsRepeater updates the row's DataContext; cells pick up the new item reactively
		// via inheritance (they are not restamped). RebuildCells is still called so index-dependent visuals
		// (alternating-row banding, frozen pinning) refresh for the new position. Guard against re-entry
		// from child DataContext propagation.
		if (m_isRebuildingCells)
		{
			return;
		}
		RebuildCells();
	}

	private void OnColumnsVectorChanged(
		IObservableVector<TableViewColumn> sender,
		IVectorChangedEventArgs args)
	{
		// Mutating Columns triggers a cell rebuild against the new column set. Coalesce a burst of column
		// changes into a single rebuild on the next tick. No explicit Children().Clear() is needed:
		// RebuildCells' restamp fast-path forces a full rebuild (which clears) whenever the column set
		// actually changed, and keeping the old cells until the tick avoids an empty-cell flash.
		QueueRebuildCells();
	}

	private void QueueRebuildCells()
	{
		// No cell host yet (template not applied); the owner-set / ApplyTemplate path builds cells.
		if (m_cellsHost is null)
		{
			return;
		}

		// A rebuild is already scheduled for this tick -- collapse the burst into one.
		if (m_rebuildCellsQueued)
		{
			return;
		}

		var dispatcher = DispatcherQueue;
		if (dispatcher is null)
		{
			// No dispatcher (teardown) -- rebuild synchronously so cells are not left stale.
			RebuildCells();
			return;
		}

		m_rebuildCellsQueued = true;
		var weakRow = new WeakReference<TableViewRow>(this);
		if (!dispatcher.TryEnqueue(() =>
			{
				if (weakRow.TryGetTarget(out var strongRow))
				{
					strongRow.m_rebuildCellsQueued = false;
					try
					{
						strongRow.RebuildCells();
					}
					catch (Exception)
					{
						// Coalesced cell rebuild is best-effort; never fail-fast the dispatcher.
					}
				}
			}))
		{
			// Enqueue failed -- fall back to a synchronous rebuild so cells are not left stale.
			m_rebuildCellsQueued = false;
			RebuildCells();
		}
	}

	protected override void OnPointerPressed(PointerRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(this);
		var props = pointerPoint.Properties;

		// Mouse/pen: primary button only - a right-click opens a context menu and must not select.
		// Touch reports no pressed button, so it is admitted on device type instead, matching the
		// begin-edit gesture. Without this a touch press never arms the release-time selection.
		var deviceType = args.Pointer.PointerDeviceType;
		if (deviceType != PointerDeviceType.Touch &&
			!props.IsLeftButtonPressed)
		{
			return;
		}

		m_isPressed = true;
		UpdateVisualState(true);

		// A cell press enters cell level so keyboard/UIA focus names that cell, not the whole row.
		UIElement? pressedCell = null;
		if (args.OriginalSource is DependencyObject source)
		{
			pressedCell = FindOwnCellInternal(source, false /* requireExact */);
		}
		if (pressedCell is not null)
		{
			// Drill in before focusing; row-level cells are not focusable.
			SetCellLevelInternal(true);
			if (!pressedCell.Focus(FocusState.Pointer))
			{
				SetCellLevelInternal(false);
				Focus(FocusState.Pointer);
			}
		}
		else
		{
			// Empty strip clicks land on the row, the body's row-level focus target.
			SetCellLevelInternal(false);
			Focus(FocusState.Pointer);
		}

		m_selectOnPointerRelease = true;
		m_selectPointerId = args.Pointer.PointerId;
	}

	protected override void OnPointerEntered(PointerRoutedEventArgs args)
	{
		m_isPointerOver = true;
		UpdateVisualState(true);
	}

	protected override void OnPointerExited(PointerRoutedEventArgs args)
	{
		m_isPointerOver = false;
		m_isPressed = false;

		// The contact left the row, so a deferred selection is no longer a tap on it. Only the pointer
		// that armed it may disarm it: with two contacts on the same row, the other one leaving must
		// not cancel this one's pending selection. Matches the check in OnPointerReleased.
		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerReleased(PointerRoutedEventArgs args)
	{
		m_isPressed = false;

		// Deferred selection: the pointer came up on this row without a pan or a capture loss taking
		// it away, so it was a tap. Only the pointer that armed it can commit or disarm it.
		bool isArmingPointer = args.Pointer.PointerId == m_selectPointerId;
		bool selectNow = m_selectOnPointerRelease && isArmingPointer;
		if (isArmingPointer)
		{
			m_selectOnPointerRelease = false;
		}

		if (selectNow)
		{
			if (GetOwningTableView() is { } owner)
			{
				owner.OnRowPointerSelect(this);
			}
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
	{
		m_isPressed = false;

		// A ScrollViewer took the pointer for a pan; the gesture was a scroll, not a tap. Only the
		// arming pointer disarms, so a second contact panning does not cancel the first one's tap.
		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs args)
	{
		// Palm rejection or a system gesture. Can arrive without a preceding PointerCaptureLost, which
		// would otherwise leave the latch set and let the next unrelated release select this row.
		m_isPressed = false;

		if (args.Pointer.PointerId == m_selectPointerId)
		{
			m_selectOnPointerRelease = false;
		}

		UpdateVisualState(true);
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		// One GoToState into one group: the selected states share CommonStates so nothing depends on
		// call order (TreeViewItem / ItemContainer pattern). Disabled wins, for ListViewItem parity.
		string state;
		if (IsSelected)
		{
			state = !IsEnabled ? "SelectedDisabled"
				: m_isPressed ? "SelectedPressed"
				: m_isPointerOver ? "SelectedPointerOver"
				: "Selected";
		}
		else
		{
			state = !IsEnabled ? "Disabled"
				: m_isPressed ? "Pressed"
				: m_isPointerOver ? "PointerOver"
				: "Normal";
		}

		VisualStateManager.GoToState(this, state, useTransitions);
	}

	internal void SetIsSelectedInternal(bool isSelected)
	{
		if (IsSelected == isSelected)
		{
			return;
		}

		IsSelected = isSelected;
		UpdateVisualState(true /* useTransitions */);
	}

	internal void RefreshDensity()
	{
		// Re-read density metrics and restamp built-in cell padding.
		RebuildCells();
	}

	internal void RefreshCells()
	{
		if (m_cellsHost is { } host)
		{
			ResetCellAutomationNames();
			host.Children.Clear();
		}

		RebuildCells();
	}

	internal void RefreshColumnVisibility(TableViewColumn? column, Visibility visibility)
	{
		if (column is null)
		{
			return;
		}

		if (TableViewCellsPanel.CellForColumn(m_cellsHost, column) is { } cell)
		{
			cell.Visibility = visibility;
		}
	}

	internal double MeasuredWidthForColumn(TableViewColumn? column)
	{
		if (m_cellsHost is { } host)
		{
			if (host is TableViewCellsPanel cellsPanel)
			{
				return cellsPanel.MeasuredWidthForColumn(column);
			}
		}

		return 0.0;
	}

	internal void InvalidateCells()
	{
		if (m_cellsHost is { } host)
		{
			host.InvalidateMeasure();
		}
	}

	private void ResetCellAutomationNames()
	{
		if (m_cellsHost is { } host)
		{
			foreach (var cell in host.Children)
			{
				try
				{
					if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
					{
						peer.ResetEditName();
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Optional released-cell name state could not be reset.");
				}
			}
		}
	}

	private void RebuildCells(bool updateExistingCellPeerItems = true)
	{
		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		// Re-entry guard. See OnDataContextChanged.
		if (m_isRebuildingCells)
		{
			return;
		}
		m_isRebuildingCells = true;
		// Synchronous RAII guard: captures this only until RebuildCells returns.
		try
		{
			var owner = GetOwningTableView();
			if (owner is null)
			{
				// Keep realized cells on recycle-out so the next ElementPrepared restamps
				// instead of re-generating every cell (the restamp fast-path exists for this).
				return;
			}

			// Density: apply the owning TableView's row min-height (Fluent Standard = 40,
			// Compact = 30, Comfortable = 48; resolved from the TableViewRowMinHeight* resources).
			var ownerImpl = owner;
			var rowMinHeight = ownerImpl.GetDensityRowMinHeight();
			MinHeight = rowMinHeight;

			var columns = owner.Columns;
			bool isOwnedColumn(TableViewColumn? column)
			{
				return column is not null && column.GetOwningTableView() == owner;
			}
			if (columns is null)
			{
				// Columns went away entirely. Clearing here without closing the edit first would rip a live
				// editor out of the tree while the control still believed it was editing - and it wedges
				// permanently: the next pass sees zero children AND zero columns, takes the restamp
				// fast-path below, and returns before ever reaching the teardown. IsEditing would stay true
				// for the lifetime of the control, with no editor and no way for the app to recover.
				if (m_editingCellWrapper is not null)
				{
					if (GetOwningTableView() is { } editOwner)
					{
						editOwner.TerminateEditWithoutVisualRestore(true /* insideLayoutPass */);
					}
					else
					{
						AbandonCellEdit();
					}

					// Refused to close (an edit still in its Beginning window). Leave the cells alone
					// rather than orphan the editor; the next pass retries once the edit has closed.
					if (m_editingCellWrapper is not null)
					{
						return;
					}
				}

				ResetCellAutomationNames();
				host.Children.Clear();
				return;
			}

			var dataContext = DataContext;
			object? dataItem = dataContext;
			var children = host.Children;
			if (updateExistingCellPeerItems)
			{
				try
				{
					foreach (var cell in children)
					{
						if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
						{
							peer.UpdateNameItem(dataItem);
						}
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Optional recycled-cell name state could not be refreshed.");
				}
			}

			uint nonNullColumnCount = 0;
			foreach (var column in columns)
			{
				if (isOwnedColumn(column))
				{
					++nonNullColumnCount;
				}
			}

			bool canRestampCells = children.Count == nonNullColumnCount;

			// The fast-path reuses cells and deliberately does not close an open edit. That is only sound
			// while the editor is still one of those cells. If it has been orphaned - its wrapper is no
			// longer a child - reusing them would leave the control editing an element outside the tree,
			// so force the rebuild path, which tears the edit down.
			if (canRestampCells)
			{
				if (m_editingCellWrapper is { } editingWrapper)
				{
					bool stillAttached = false;
					for (int i = 0; i < children.Count; ++i)
					{
						if (ReferenceEquals(children[i], editingWrapper))
						{
							stillAttached = true;
							break;
						}
					}

					if (!stillAttached)
					{
						canRestampCells = false;
					}
				}
			}

			int childIndex = 0;
			if (canRestampCells)
			{
				foreach (var column in columns)
				{
					if (!isOwnedColumn(column))
					{
						continue;
					}

					var cellWrapper = children[childIndex] as Grid;
					if (cellWrapper is null || !ReferenceEquals(cellWrapper.Tag as TableViewColumn, column))
					{
						canRestampCells = false;
						break;
					}
					++childIndex;
				}
			}

			if (canRestampCells)
			{
				var cellPadding = ownerImpl.GetDensityCellPadding();
				childIndex = 0;
				foreach (var column in columns)
				{
					if (!isOwnedColumn(column))
					{
						continue;
					}

					var cellWrapper = (Grid)children[childIndex];
					// Do NOT re-push data during ItemsRepeater measure: live DataContext/Content mutation re-entered layout and hit 0xc0000420 on scroll.
					// Cells must update reactively from inherited DataContext; only equal-valued visual restamps are safe here.
					cellWrapper.Visibility = column.Visibility;
					cellWrapper.MinHeight = rowMinHeight;

					if (TableViewCell.Child(cellWrapper) is FrameworkElement cellElement)
					{
						if (cellElement is TextBlock textBlock)
						{
							textBlock.Padding = cellPadding;
						}
					}

					++childIndex;
				}

				// Pin immediately so recycled frozen cells do not wait for the next scroll.
				if (GetOwningTableView() is { } owningView)
				{
					owningView.PinFrozenColumnsForRow(this);
				}

				ApplyFocusLevelInternal();

				RefreshGridLines();
				RefreshRowBackground();
				return;
			}

			// Column adds/removes/reorders or template changes rebuild cells; width-only changes are picked
			// up automatically by the cell panel's arrange (it reads each column's resolved ActualWidth), so
			// no cell rebuild is needed for those.
			//
			// This path destroys every cell, so an open editor cannot survive it. The restamp fast-path above
			// deliberately does NOT tear the edit down: it reuses the cells, so an editor opened on this row
			// stays valid through the routine re-measure that installing it provokes.
			//
			// The teardown must not restore the display child - RebuildCells can run inside the repeater's
			// measure pass. See AbandonCellEdit.
			if (m_editingCellWrapper is not null)
			{
				if (GetOwningTableView() is { } editOwner)
				{
					editOwner.TerminateEditWithoutVisualRestore(true /* insideLayoutPass */);
				}
				else
				{
					AbandonCellEdit();
				}

				// If the edit still refuses to close, clearing the children would rip the live editor out
				// of the tree while the control still believes it is editing. Leave the cells alone; the
				// rebuild happens on the next pass once the edit has actually closed.
				if (m_editingCellWrapper is not null)
				{
					return;
				}
			}

			ResetCellAutomationNames();
			host.Children.Clear();

			int visibleColumnIndex = 0;
			foreach (var column in columns)
			{
				if (!isOwnedColumn(column))
				{
					continue;
				}

				var cellWrapper = TableViewCell.Create(this, column, visibleColumnIndex);
				if (column.Visibility == Visibility.Visible)
				{
					++visibleColumnIndex;
				}
				cellWrapper.Tag = column;
				cellWrapper.Visibility = column.Visibility;
				cellWrapper.MinHeight = rowMinHeight;

				// A null Background does not hit-test; Transparent keeps the full cell pressable so clicks
				// in padding still set current cell and support double-click-to-edit.
				cellWrapper.Background = TransparentBrush();

				// No local DataContext anywhere on the cell path: cells inherit the row item so recycled
				// rows update reactively. Custom columns must also bind to inherited DataContext, not bake
				// in the initial dataItem.
				if (column.GenerateElement(dataItem) is { } cellElement)
				{
					AttachCellContent(cellWrapper, cellElement);
				}

				// Opt-in per-cell tooltip. Set once; the binding then tracks the row's inherited DataContext.
				if (column.CellToolTipBinding is { } toolTipBinding)
				{
					TableViewDetails.ApplyCellToolTipBinding(cellWrapper, toolTipBinding);
				}

				host.Children.Add(cellWrapper);
			}

			// Pin immediately so rebuilt frozen cells do not wait for the next scroll.
			if (GetOwningTableView() is { } rebuiltOwningView)
			{
				rebuiltOwningView.PinFrozenColumnsForRow(this);
			}

			// New wrappers default to IsTabStop(true); restamp the current level so rebuilds do not reopen
			// the row-to-first-cell Tab leak.
			ApplyFocusLevelInternal();

			RefreshGridLines();
			RefreshRowBackground();
		}
		finally
		{
			m_isRebuildingCells = false;
		}
	}

	// Recycle-out. Always walks: the restamp fast-path revives tooltips through the binding, so a
	// cached per-row flag would go stale and strand app content in the pool.
	internal void ReleaseCellToolTips()
	{
		if (m_cellsHost is { } host)
		{
			ClearOwnedCellToolTips(host);
		}
	}

	private void ClearOwnedCellToolTips(Panel host)
	{
		var children = host.Children;
		int count = children.Count;
		for (int i = 0; i < count; ++i)
		{
			if (children[i] is Grid cellWrapper)
			{
				TableViewDetails.ClearOwnedToolTip(cellWrapper);
			}
		}
	}

	// Installs a generated display element as a cell's content, including the ContentPresenter wiring a
	// template column needs. Shared by the cell rebuild and by the post-commit refresh, because
	// GenerateElement alone is NOT a complete cell - forgetting the second half leaves a template
	// column's Content unbound and the cell blank.
	private void AttachCellContent(Grid? cellWrapper, FrameworkElement? cellElement)
	{
		if (cellWrapper is null || cellElement is null)
		{
			return;
		}

		TableViewCell.Child(cellWrapper, cellElement);

		// Bind Content to the wrapper's inherited DataContext so recycled template cells track the new
		// item; binding to the presenter itself would freeze stale content.
		if (cellElement is ContentPresenter presenter)
		{
			if (presenter.ContentTemplate is not null)
			{
				Binding contentBinding = new();
				contentBinding.Source = cellWrapper;
				contentBinding.Path = new PropertyPath("DataContext");
				BindingOperations.SetBinding(
					presenter,
					ContentPresenter.ContentProperty,
					contentBinding);
			}
		}
	}

	// Grid lines only: the row's horizontal bottom line and the vertical per-cell separators (driven by
	// GridLinesVisibility). Row background / alternating banding lives in RefreshRowBackground.
	internal void RefreshGridLines()
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		var visibility = owner.GridLinesVisibility;
		if (WantsHorizontalLines(visibility))
		{
			ClearValue(Control.BorderThicknessProperty);
		}
		else
		{
			BorderThickness = s_zeroThickness;
		}

		if (m_gridLineBorder is { } gridLineBorder)
		{
			var thickness = BorderThickness;
			if (m_suppressBottomGridLine)
			{
				thickness.Bottom = 0.0;
			}
			gridLineBorder.BorderThickness = thickness;
		}

		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		bool wantVertical = WantsVerticalLines(visibility);
		Brush? gridLineBrush = null;
		// Read the direction from the owner, the same source RebuildHeaders stamps the header grid line
		// from, so the two edges cannot disagree.
		var verticalThickness = owner.FlowDirection == FlowDirection.RightToLeft
			? s_verticalThicknessRtl
			: s_verticalThickness;
		if (wantVertical)
		{
			gridLineBrush = owner.GetGridLineBrush();
		}

		var children = host.Children;
		int childCount = children.Count;
		int lastVisibleCell = childCount;
		// The cell wrapper is a composed Grid. Border is sealed and cannot host a custom
		// automation peer, so both loops in RefreshGridLines must cast cell wrappers to Grid.
		for (int i = childCount; i > 0; --i)
		{
			if (children[i - 1] is Grid lastCellWrapper)
			{
				var column = lastCellWrapper.Tag as TableViewColumn;
				if (lastCellWrapper.Visibility == Visibility.Visible &&
					column is not null &&
					column.ActualWidth > 0.0)
				{
					lastVisibleCell = i - 1;
					break;
				}
			}
		}

		for (int i = 0; i < childCount; ++i)
		{
			if (children[i] is Grid cellWrapper)
			{
				if (wantVertical)
				{
					// RTL-aware thickness, not the LTR-only static: the separator must sit on the
					// trailing edge in both flow directions.
					cellWrapper.BorderThickness = verticalThickness;
					// Keep the separator's layout thickness stable and suppress only its brush when
					// the terminal cell actually meets the outer border.
					cellWrapper.BorderBrush =
						m_suppressTrailingGridLine &&
						cellWrapper.Visibility == Visibility.Visible &&
						i == lastVisibleCell
							? null
							: gridLineBrush;
				}
				else
				{
					cellWrapper.ClearValue(Grid.BorderThicknessProperty);
					cellWrapper.ClearValue(Grid.BorderBrushProperty);
				}
			}
		}
	}

	// Row background / alternating banding only. Index-dependent (parity), so it must refresh when the
	// row's position changes on recycle, and when RowBackground / AlternatingRowBackground change.
	internal void RefreshRowBackground()
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		// Clear first so recycled rows do not keep stale banding fills.
		ClearValue(Control.BackgroundProperty);
		if (owner.RowBackground is not null || owner.AlternatingRowBackground is not null)
		{
			var rowIndex = -1;
			if (owner.GetRowsRepeaterInternal() is { } repeater)
			{
				rowIndex = repeater.GetElementIndex(this);
			}
			if (rowIndex >= 0)
			{
				// RowBackground is the base for every row; AlternatingRowBackground overrides
				// odd rows only when set (WPF DataGrid parity). Setting RowBackground alone
				// must fill all rows uniformly, not stripe odd rows transparent.
				var background = owner.RowBackground;
				if ((rowIndex % 2) != 0 && owner.AlternatingRowBackground is not null)
				{
					background = owner.AlternatingRowBackground;
				}
				if (background is not null)
				{
					Background = background;
				}
			}
		}
	}

	internal void RefreshFrozenColumnLayout(double horizontalOffset, double leadingFrozenWidth)
	{
		if (m_cellsHost is { } host)
		{
			TableViewCellsPanel.ApplyFrozenColumnLayout(host, horizontalOffset, leadingFrozenWidth);
		}
	}

	// ----- Editing -----
	//
	// The row owns its cells, so the control delegates the display/editor swap here. The swap replaces
	// Child on the existing cell wrapper Border: keeping the wrapper means column width, visibility,
	// frozen pinning and grid lines keep applying while the cell is edited, with no layout re-plumbing.

	internal bool BeginCellEdit(TableViewColumn? column, object? dataItem)
	{
		if (column is null)
		{
			return false;
		}

		// An edit already open on this row is closed first. Committing it is the control's job, not
		// the row's; by this point the control has already ended it, so anything still open here is
		// stale visual state.
		EndCellEdit(TableViewEditAction.Cancel);

		var host = m_cellsHost;
		if (host is null)
		{
			return false;
		}

		Grid? cellWrapper = null;
		foreach (var child in host.Children)
		{
			if (child is Grid cell)
			{
				if (ReferenceEquals(cell.Tag as TableViewColumn, column))
				{
					cellWrapper = cell;
					break;
				}
			}
		}

		if (cellWrapper is null)
		{
			return false;
		}

		var editingElement = column.GenerateEditingElement(dataItem);
		if (editingElement is null)
		{
			return false;
		}

		// No local DataContext on the editing element, for the same reason the display cell sets none:
		// it inherits from the wrapper, which tracks the item across row recycle.
		// Observe only a provider a client already obtained. Creating peers here would turn every
		// ordinary edit into a UIA-tree allocation and could give the event a different identity.
		try
		{
			// Gate the snapshot work, not the edit: this function's return value starts the edit.
			var peer = AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged)
				? TableViewCell.TryGetExistingPeer(cellWrapper) as TableViewCellAutomationPeer
				: null;
			if (peer is not null && peer.GetPattern(PatternInterface.Value) is not null)
			{
				var value = peer.Value;
				var name = peer.ReadNameForEdit();
				var weakPeer = new WeakReference<TableViewCellAutomationPeer>(peer);
				m_editingAutomationItem = dataItem;
				m_editingAutomationValue = value;
				m_editingAutomationName = name;
				m_editingAutomationPeer = weakPeer;
			}
		}
		catch (Exception)
		{
			m_editingAutomationPeer = null;
			m_editingAutomationItem = null;
			m_editingAutomationValue = "";
			m_editingAutomationName = "";
			TVDiag.LogRetailF("[TableView] Optional pre-edit UIA snapshot could not be captured.");
		}
		try
		{
			if (TableViewCell.TryGetExistingPeer(cellWrapper) is TableViewCellAutomationPeer peer)
			{
				peer.BeginEditName();
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional stable cell-name capture failed.");
		}
		m_editingDisplayElement = TableViewCell.Child(cellWrapper);
		m_pendingEditingCell = new WeakReference<UIElement>(cellWrapper);
		try
		{
			TableViewCell.Child(cellWrapper, editingElement);

			m_editingColumn = column;
			m_editingCellWrapper = cellWrapper;
			TableViewDetails.ClearOwnedToolTip(cellWrapper);
			m_editingElement = editingElement;


			return true;
		}
		finally
		{
			m_pendingEditingCell = null;
		}
	}

	internal void EndCellEdit(TableViewEditAction action)
	{
		var cellWrapper = m_editingCellWrapper;
		var weakPeer = m_editingAutomationPeer;
		object? originalItem = null;
		string oldValue = "";
		string oldName = "";
		try
		{
			originalItem = m_editingAutomationItem;
			oldValue = m_editingAutomationValue;
			oldName = m_editingAutomationName;
		}
		catch (Exception)
		{
			originalItem = null;
			TVDiag.LogRetailF("[TableView] Optional edit UIA snapshot could not be retrieved.");
		}
		m_editingAutomationPeer = null;
		m_editingAutomationItem = null;
		m_editingAutomationValue = "";
		m_editingAutomationName = "";
		if (cellWrapper is null)
		{
			m_editingColumn = null;
			m_editingElement = null;
			m_editingDisplayElement = null;
			return;
		}

		// Move focus off the editor before it leaves the tree. Dropping a focused element causes XAML
		// to fall back to whatever it can find, which can scroll the list; the row is the correct
		// landing spot and is where keyboard navigation expects focus to be.
		if (m_editingElement is { } editingElement)
		{
			bool editorHasFocus = false;
			if (XamlRoot is { } root)
			{
				if (FocusManager.GetFocusedElement(root) is DependencyObject focused)
				{
					DependencyObject? current = focused;
					while (current is not null)
					{
						if (ReferenceEquals(current, editingElement))
						{
							editorHasFocus = true;
							break;
						}
						current = VisualTreeHelper.GetParent(current);
					}
				}
			}

			if (editorHasFocus)
			{
				// Restore focus to the cell, not row, and preserve Keyboard focus state so the focus
				// rectangle survives Enter-commit.
				var restoreState = editingElement.FocusState == FocusState.Unfocused
					? FocusState.Programmatic
					: editingElement.FocusState;

				// Closing edit returns to cell level, so re-arm cells before restoring focus.
				SetCellLevelInternal(true);

				if (!cellWrapper.Focus(restoreState))
				{
					SetCellLevelInternal(false);
					Focus(restoreState);
				}
			}
		}

		// Regenerate the display cell on commit rather than re-parenting the one that was parked when
		// the edit opened.
		//
		// The parked element still holds the OneWay binding it was created with, and a OneWay binding
		// only re-reads its source when that source raises PropertyChanged. For an item that does not
		// implement INotifyPropertyChanged the committed value would therefore never appear - the user
		// types, presses Enter, the item is updated, and the cell keeps showing the old text. A freshly
		// generated element evaluates its binding immediately against the current value, so the commit
		// is visible for plain POCOs too. This is what WPF's DataGrid does for the same reason.
		//
		// Only on Commit: a cancel wrote nothing, so the parked element is still correct and
		// regenerating it would be pure churn.
		UIElement? displayElement = m_editingDisplayElement;
		if (action == TableViewEditAction.Commit)
		{
			if (m_editingColumn is { } column)
			{
				try
				{
					if (column.GenerateElement(DataContext) is { } refreshed)
					{
						AttachCellContent(cellWrapper, refreshed);
						displayElement = null;
					}
				}
				catch (Exception)
				{
					// A column that throws while regenerating must not strand the row in edit mode;
					// fall back to the parked element, which is stale but present.
					displayElement = m_editingDisplayElement;
				}
			}
		}

		if (displayElement is not null)
		{
			TableViewCell.Child(cellWrapper, displayElement);
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The bound value did not change, so only an explicit re-apply restores what the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);

		try
		{
			if (TableViewCell.TryGetExistingPeer(cellWrapper) is TableViewCellAutomationPeer peer)
			{
				peer.EndEditName();
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional final cell-name invalidation could not be prepared.");
		}

		try
		{
			if (action == TableViewEditAction.Commit && originalItem is not null &&
				TableView.SameInspectableIdentity(DataContext, originalItem))
			{
				if (weakPeer is not null && weakPeer.TryGetTarget(out var peer))
				{
					var newValue = peer.Value;
					var newName = peer.ReadNameForEdit();
					if (oldValue != newValue || oldName != newName)
					{
						// Publish after Ending; reject recycled cells and superseded values.
						if (DispatcherQueue is { } queue)
						{
							var weakThis = new WeakReference<TableViewRow>(this);
							var weakCell = new WeakReference<Grid>(cellWrapper);
							if (!queue.TryEnqueue(() =>
							{
								try
								{
									TableViewRow? row = null;
									TableViewCellAutomationPeer? currentPeer = null;
									Grid? cell = null;
									if (!weakThis.TryGetTarget(out row) || !weakPeer.TryGetTarget(out currentPeer) || !weakCell.TryGetTarget(out cell) ||
										!ReferenceEquals(currentPeer.Owner, cell) ||
										!ReferenceEquals(VisualTreeHelper.GetParent(cell) as Panel, row.GetCellsHostPanelInternal()) ||
										!TableView.SameInspectableIdentity(row.DataContext, originalItem))
									{
										return;
									}
									var peerImpl = currentPeer;
									if (peerImpl.Row < 0 || peerImpl.Value != newValue || peerImpl.ReadNameForEdit() != newName)
									{
										return;
									}
									if (oldValue != newValue)
									{
										currentPeer.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,
											oldValue, newValue);
									}
								}
								catch (Exception)
								{
									TVDiag.LogRetailF("[TableView] A committed cell's UIA notification could not be delivered.");
								}
							}))
							{
								TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification queue rejected delivery.");
							}
						}
						else
						{
							TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification has no dispatcher.");
						}
					}
				}
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional committed-cell UIA notification could not be prepared.");
		}
	}

	internal void AbandonCellEdit()
	{
		try
		{
			if (m_editingCellWrapper is { } cell)
			{
				if (TableViewCell.TryGetExistingPeer(cell) is TableViewCellAutomationPeer peer)
				{
					peer.ResetEditName();
				}
			}
		}
		catch (Exception)
		{
			TVDiag.LogRetailF("[TableView] Optional abandoned-cell name state could not be reset.");
		}
		m_editingAutomationPeer = null;
		m_editingAutomationItem = null;
		m_editingAutomationValue = "";
		m_editingAutomationName = "";
		// Restores the display child, but deliberately does NOT touch focus. Callers run inside a layout
		// pass, where moving focus re-enters the framework and trips the re-entrancy guard. Replacing
		// the child does not - and it must happen, or the row keeps showing a TextBox after the edit
		// closed, and over a different item once recycled.
		var cellWrapper = m_editingCellWrapper;
		if (cellWrapper is not null)
		{
			TableViewCell.Child(cellWrapper, m_editingDisplayElement);
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The cell is a display cell again; restore the tooltip the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);
	}

	private void ResetPressState()
	{
		m_lastPressTimestamp = 0;
		m_lastPressPosition = default;
		m_lastPressColumn = null;
		m_lastPressItem = null;
	}

	// Pointer entry point for editing, and the only place a pointer establishes the current cell.
	//
	// Lives on the row because the row owns its cells: resolving which cell a press landed on is a
	// question only the row can answer cheaply. The control keeps the edit state machine, so this
	// handler translates a gesture into SetCurrentCell / BeginEdit and nothing more. Mirrors WPF,
	// where DataGridCell handles the gesture and calls DataGrid.BeginEdit.
	//
	// PointerPressed with click counting, not DoubleTapped: marking a press handled suppresses XAML's
	// gesture recognizer entirely, and a row that participates in selection must mark it handled. A
	// DoubleTapped handler would work today and silently break when selection lands.
	internal void OnPointerPressedForEditing(
		object sender,
		PointerRoutedEventArgs args)
	{
		var owner = GetOwningTableView();
		if (owner is null)
		{
			return;
		}

		var ownerImpl = owner;

		// Editing is opt-in and read-only by default, so a read-only table pays nothing beyond this.
		if (ownerImpl.IsReadOnly)
		{
			return;
		}

		var pointerPoint = args.GetCurrentPoint(this);
		if (pointerPoint is null)
		{
			return;
		}

		// Mouse/pen: primary button only - a right-click opens a context menu and must not begin an
		// edit. Touch reports no pressed button, so it is admitted on device type instead.
		var deviceType = args.Pointer.PointerDeviceType;
		if (deviceType != PointerDeviceType.Touch &&
			!pointerPoint.Properties.IsLeftButtonPressed)
		{
			return;
		}

		var column = ResolvePressedColumn(args.OriginalSource, args.GetCurrentPoint(null).Position);
		if (column is null)
		{
			return;
		}

		var item = ownerImpl.UnwrapEditingDataItem(DataContext);
		if (item is null)
		{
			return;
		}

		var timestamp = pointerPoint.Timestamp;
		var position = pointerPoint.Position;

		double slop = GetDoubleClickSlop(this);

		// Device type is part of the repeat identity so a mouse click following a pen tap is not read
		// as one double-click. Pointer ID deliberately is NOT: every touch contact gets a fresh id, so
		// requiring equality would mean the second tap never matches and touch could never begin an
		// edit at all.
		bool isRepeatPress =
			ReferenceEquals(m_lastPressColumn, column) &&
			TableView.SameInspectableIdentity(m_lastPressItem, item) &&
			m_lastPressDeviceType == deviceType &&
			timestamp >= m_lastPressTimestamp &&
			(timestamp - m_lastPressTimestamp) <= GetDoubleClickIntervalMicroseconds() &&
			Math.Abs(position.X - m_lastPressPosition.X) <= slop &&
			Math.Abs(position.Y - m_lastPressPosition.Y) <= slop;

		// A press inside the cell already being edited belongs to the editor: it is the user placing
		// the caret, and must not be read as a navigation move or a fresh edit.
		bool pressInsideOpenEdit =
			ownerImpl.IsEditing &&
			ReferenceEquals(m_editingColumn, column) &&
			m_editingElement is not null;

		if (!pressInsideOpenEdit)
		{
			// Move the current cell even when the edit is refused (read-only, or BeginningEdit cancels):
			// the user pointed here, so a later F2 must not edit a different cell.
			//
			// This is also what makes keyboard editing reachable at all - pointer focus lands on the
			// row, not a tagged cell, so without this the current column stays null.
			ownerImpl.SetCurrentCell(item, column);

			if (isRepeatPress)
			{
				ownerImpl.BeginEdit(item, column);

				// Consume the press so a third click starts a fresh first press rather than
				// re-entering begin-edit on a cell that is already open.
				m_lastPressTimestamp = 0;
				m_lastPressColumn = null;
				m_lastPressItem = null;
				return;
			}
		}

		m_lastPressTimestamp = timestamp;
		m_lastPressPosition = position;
		m_lastPressColumn = column;
		m_lastPressItem = item;
		m_lastPressDeviceType = deviceType;
	}

	// Which of this row's cells a press landed on. Walks up from OriginalSource to the cell wrapper
	// Grid, whose Tag carries the owning column (set in RebuildCells). Once the row itself has focus
	// a press can arrive with the row as OriginalSource and no tagged cell on the chain, so fall back
	// to hit-testing this row's subtree.
	private TableViewColumn? ResolvePressedColumn(
		object? originalSource,
		Point hostPoint)
	{
		// A nested TableView's press bubbles through this row, and the walk would reach the INNER
		// table's tagged Border before it ever reached this row - so filter on ownership rather than
		// trying to stop the walk. A column this table does not own is not ours to act on.
		var owner = GetOwningTableView();
		bool ownedByThisTable(TableViewColumn? candidate)
		{
			if (candidate is null || owner is null)
			{
				return false;
			}

			if (owner.Columns is { } columns)
			{
				foreach (var column in columns)
				{
					if (ReferenceEquals(column, candidate))
					{
						return true;
					}
				}
			}

			return false;
		}

		var current = originalSource as DependencyObject;
		while (current is not null)
		{
			if (current is Grid cell)
			{
				if (cell.Tag is TableViewColumn tagged)
				{
					return ownedByThisTable(tagged) ? tagged : null;
				}
			}

			current = VisualTreeHelper.GetParent(current);
		}

		foreach (var hit in VisualTreeHelper.FindElementsInHostCoordinates(hostPoint, this))
		{
			if (hit is Grid cell)
			{
				if (cell.Tag is TableViewColumn tagged)
				{
					return ownedByThisTable(tagged) ? tagged : null;
				}
			}
		}

		return null;
	}
}
