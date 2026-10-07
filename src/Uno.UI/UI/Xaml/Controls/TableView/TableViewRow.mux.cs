// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRow.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI;
using Microsoft.UI.Input;
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

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRow
{
	private const string s_CellsHostPartName = "PART_CellsHost";

	private static readonly Thickness s_verticalThickness = new(0, 0, 1, 0);
	private static readonly Thickness s_zeroThickness = new(0, 0, 0, 0);

	private static SolidColorBrush? s_transparent;

	// Shared transparent fill for cell wrappers. Cached because rows and cells are rebuilt on every
	// scroll, and a fresh brush per cell is pure allocation for a value that never varies.
	private static Brush TransparentBrush()
	{
		// TODO Uno: C++ uses a function-local static (`static const winrt::SolidColorBrush s_transparent{ winrt::Colors::Transparent() };`).
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
		// TODO Uno: The rasterization scale is read before the metrics (C++ reads it after) because the
		// metric substitute below needs it.
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

		// TODO Uno: There is no cross-platform SM_CXDOUBLECLK / SM_CYDOUBLECLK. Uno's GestureRecognizer
		// uses TapMaxXDelta / TapMaxYDelta as a DIP half-width for its own multi-tap detection, so they are
		// expressed here as the equivalent full physical-pixel rectangle, which makes the formula below
		// yield exactly the recognizer's tolerance.
		// const auto cx = static_cast<double>(::GetSystemMetrics(SM_CXDOUBLECLK));
		// const auto cy = static_cast<double>(::GetSystemMetrics(SM_CYDOUBLECLK));
		var cx = 2.0 * GestureRecognizer.TapMaxXDelta * scale;
		var cy = 2.0 * GestureRecognizer.TapMaxYDelta * scale;
		double physical = Math.Max(Math.Max(cx, cy), 4.0) / 2.0;

		return physical / scale;
	}

	// PointerPoint.Timestamp is in microseconds; the system double-click time is in milliseconds.
	// Not cached, so a change made in the mouse control panel takes effect immediately.
	private static ulong GetDoubleClickIntervalMicroseconds()
	{
		// TODO Uno: ::GetDoubleClickTime() is Win32-only; UISettings.DoubleClickTime is the WinRT equivalent.
		// return static_cast<uint64_t>(::GetDoubleClickTime()) * 1000ull;
		return (ulong)new UISettings().DoubleClickTime * 1000UL;
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

		m_cellsHost = GetTemplateChild(s_CellsHostPartName) as Panel;

		// Let the panel recognise this row's editing cell so it can keep it out of the Auto-width pass.
		if (m_cellsHost is TableViewCellsPanel cellsPanel)
		{
			cellsPanel.SetOwningRowInternal(this);
		}

		RebuildCells();

		UpdateVisualState(false /* useTransitions */);
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

	private void DetachColumnsSubscription()
	{
		// Clearing the revoker detaches from the previously observed Columns vector.
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

			UpdateVisualState(false);
		}

		// Observe the new owner's Columns vector (no-op on recycle-out when owner is null).
		AttachColumnsSubscription(owner);

		RebuildCells();
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
			host.Children.Clear();
		}

		// Rebuild cells against the new Columns vector.
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

		// Move keyboard focus to the row so the next keyboard interaction targets it.
		Focus(FocusState.Pointer);

		// Selection state lives on the control; the row is just where the press lands. Left unhandled
		// so the begin-edit handler for this same press still runs. Commits on RELEASE for every
		// pointer type (ListViewBaseItem parity) - committing on press would select the row a pan
		// started on, or one the user drags away from and cancels.
		m_selectOnPointerRelease = true;
		// Remember WHICH pointer armed it: with two contacts on the same row, the second one's release
		// or cancel must not commit (or discard) the first one's pending selection.
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
			host.Children.Clear();
		}

		// Regenerate realized cells after runtime cell-content changes.
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

	private void RebuildCells()
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

				host.Children.Clear();
				return;
			}

			var dataContext = DataContext;
			object? dataItem = dataContext;
			var children = host.Children;

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

					var cellWrapper = children[childIndex] as Border;
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

					var cellWrapper = (Border)children[childIndex];
					// Do NOT re-push data here. Cells inherit the row's DataContext (ItemsRepeater updates it
					// on recycle) and bind to it reactively (TextColumn Text, TemplateColumn Content), so a
					// recycled row's *data* updates without setting DataContext/Content on a live, in-tree cell
					// during the ItemsRepeater measure pass -- that data mutation (the value always changes on
					// recycle and is layout-affecting) is what re-entered framework layout and tripped a
					// re-entrancy assertion (0xc0000420) on scroll. Only per-column / per-density visuals are
					// refreshed below; on a pure scroll-recycle these are equal-valued no-ops (columns and
					// density unchanged), so they do not re-invalidate layout. Keep it that way -- if any of
					// these is ever made to vary per data item, restore an off-tree update to avoid re-entry.
					cellWrapper.Visibility = column.Visibility;
					cellWrapper.MinHeight = rowMinHeight;

					if (cellWrapper.Child is FrameworkElement cellElement)
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

			host.Children.Clear();

			foreach (var column in columns)
			{
				// Skip entries this TableView rejected so a half-owned column cannot realize cells here.
				if (!isOwnedColumn(column))
				{
					continue;
				}

				// Cell wrapper root.
				Border cellWrapper = new();
				cellWrapper.Tag = column;
				cellWrapper.Visibility = column.Visibility;
				cellWrapper.MinHeight = rowMinHeight;

				// A Border with a null Background does not hit-test, so without this only the generated
				// content itself (a TextBlock, which is as wide as its text) would respond to a press. A
				// click anywhere in the cell's padding resolved no column at all: no current cell, and
				// double-click-to-edit silently did nothing on most of the cell's area. Transparent keeps
				// the cell invisible while making the whole cell rectangle pressable.
				cellWrapper.Background = TransparentBrush();
				// No Width binding: TableViewCellsPanel arranges cells at the column's ActualWidth; an explicit
				// Width would defeat the panel's unconstrained Auto measured-width measurement.

				// No local DataContext: the cell inherits the row's DataContext once appended, so recycled
				// rows update reactively via inheritance instead of a live per-recycle push. This is a
				// load-bearing invariant: nothing on the cell path (wrapper Border, PART_CellsHost, or the
				// built-in cell elements) may set a local DataContext, or it would shadow inheritance and the
				// cell would show stale data after recycle. Custom columns (overridable GenerateElementCore)
				// must likewise bind reactively to the inherited DataContext rather than baking in the initial
				// dataItem, since recycled rows are no longer restamped.
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
			if (children[i] is Border cellWrapper)
			{
				TableViewDetails.ClearOwnedToolTip(cellWrapper);
			}
		}
	}

	// Installs a generated display element as a cell's content, including the ContentPresenter wiring a
	// template column needs. Shared by the cell rebuild and by the post-commit refresh, because
	// GenerateElement alone is NOT a complete cell - forgetting the second half leaves a template
	// column's Content unbound and the cell blank.
	private void AttachCellContent(Border? cellWrapper, FrameworkElement? cellElement)
	{
		if (cellWrapper is null || cellElement is null)
		{
			return;
		}

		cellWrapper.Child = cellElement;

		// A ContentPresenter cell (built-in TemplateColumn) needs its Content wired to the row item.
		// Bind Content to the WRAPPER Border's inherited DataContext -- which tracks the item across
		// recycle -- rather than the presenter's own DataContext: ContentPresenter pins its DataContext
		// to its Content, so a self-referential binding would freeze after the first item and show stale
		// content on recycled rows. This binding persists across recycles (the restamp fast-path reuses
		// the cell), so no Content is pushed during the measure pass.
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

		var host = m_cellsHost;
		if (host is null)
		{
			return;
		}

		bool wantVertical = WantsVerticalLines(visibility);
		Brush? gridLineBrush = null;
		if (wantVertical)
		{
			gridLineBrush = owner.GetGridLineBrush();
		}

		var children = host.Children;
		int childCount = children.Count;
		for (int i = 0; i < childCount; ++i)
		{
			if (children[i] is Border cellWrapper)
			{
				if (wantVertical)
				{
					cellWrapper.BorderThickness = s_verticalThickness;
					cellWrapper.BorderBrush = gridLineBrush;
				}
				else
				{
					cellWrapper.ClearValue(Border.BorderThicknessProperty);
					cellWrapper.ClearValue(Border.BorderBrushProperty);
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

		Border? cellWrapper = null;
		foreach (var child in host.Children)
		{
			if (child is Border border)
			{
				if (ReferenceEquals(border.Tag as TableViewColumn, column))
				{
					cellWrapper = border;
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
			// The column declined the edit (no Binding, no editing template, or a base column).
			return false;
		}

		// No local DataContext on the editing element, for the same reason the display cell sets none:
		// it inherits from the wrapper, which tracks the item across row recycle.
		m_editingDisplayElement = cellWrapper.Child;
		cellWrapper.Child = editingElement;

		m_editingColumn = column;
		m_editingCellWrapper = cellWrapper;
		// An editor owns its cell; a tooltip over a live text box is noise.
		TableViewDetails.ClearOwnedToolTip(cellWrapper);
		m_editingElement = editingElement;

		// The column decides how its editor is primed - focus, caret, selection are editor-specific,
		// and the row has no business knowing that a TextBox wants SelectAll. The control calls it
		// (TableView::BeginEdit) so it can keep the returned pre-edit value for cancel.

		return true;
	}

	internal void EndCellEdit(TableViewEditAction action)
	{
		var cellWrapper = m_editingCellWrapper;
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
				Focus(FocusState.Programmatic);
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
			cellWrapper.Child = displayElement;
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The bound value did not change, so only an explicit re-apply restores what the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);
	}

	internal void AbandonCellEdit()
	{
		// Restores the display child, but deliberately does NOT touch focus. Callers run inside a layout
		// pass, where moving focus re-enters the framework and trips the re-entrancy guard. Replacing
		// the child does not - and it must happen, or the row keeps showing a TextBox after the edit
		// closed, and over a different item once recycled.
		var cellWrapper = m_editingCellWrapper;
		if (cellWrapper is not null)
		{
			cellWrapper.Child = m_editingDisplayElement;
		}

		m_editingColumn = null;
		m_editingCellWrapper = null;
		m_editingElement = null;
		m_editingDisplayElement = null;

		// The cell is a display cell again; restore the tooltip the edit retracted.
		TableViewDetails.RefreshOwnedToolTip(cellWrapper);
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
	private void ResetPressState()
	{
		m_lastPressTimestamp = 0;
		m_lastPressPosition = default;
		m_lastPressColumn = null;
		m_lastPressItem = null;
	}

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
	// Border, whose Tag carries the owning column (set in RebuildCells). Once the row itself has focus
	// a press can arrive with the row as OriginalSource and no tagged Border on the chain, so fall back
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
			if (current is Border border)
			{
				if (border.Tag is TableViewColumn tagged)
				{
					return ownedByThisTable(tagged) ? tagged : null;
				}
			}

			current = VisualTreeHelper.GetParent(current);
		}

		// TODO Uno: Correct results with frozen (clipped, translated) columns depend on
		// FindElementsInHostCoordinates being clip-aware, front-to-back and duplicate-free (plan I13).
		foreach (var hit in VisualTreeHelper.FindElementsInHostCoordinates(hostPoint, this))
		{
			if (hit is Border border)
			{
				if (border.Tag is TableViewColumn tagged)
				{
					return ownedByThisTable(tagged) ? tagged : null;
				}
			}
		}

		return null;
	}
}
