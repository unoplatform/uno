// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Keyboard.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using static Microsoft.UI.Xaml.Controls.Tabular.TableViewAutomationHelpers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Row keyboard navigation and its focus/measurement helpers live here.

partial class TableView
{
	private static bool IsKeyDown(VirtualKey key) =>
		(InputKeyboardSource.GetKeyStateForCurrentThread(key) &
			CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

	// Row cells carry the same column Tag as header cells, so finding a tagged ancestor is not
	// enough: the walk must actually reach the header host, or a focused cell (an open editor,
	// most visibly) would resolve to a column and let arrow keys resize it.
	// Returns the focused header cell in headerCell, so a caller that needs the cell does not have
	// to scan the header band again to find what this walk already passed through.
	private static TableViewColumn? ResolveFocusedHeaderColumn(
		object? source,
		Panel? headerHost,
		out FrameworkElement? headerCell)
	{
		headerCell = null;
		if (headerHost is null)
		{
			return null;
		}

		TableViewColumn? candidate = null;
		var current = source as DependencyObject;
		while (current is not null)
		{
			if (current == headerHost)
			{
				return candidate;
			}

			if (candidate is null)
			{
				if (current is FrameworkElement element)
				{
					candidate = element.Tag as TableViewColumn;
					if (candidate is not null)
					{
						headerCell = element;
					}
				}
			}
			current = VisualTreeHelper.GetParent(current);
		}

		headerCell = null;
		return null;
	}

	// Header and row cells share visible-column coordinates, letting Tab preserve the current
	// column across bands.
	private static List<FrameworkElement> GetVisibleHeaderCells(Panel? headerHost)
	{
		List<FrameworkElement> cells = new();
		if (headerHost is null)
		{
			return cells;
		}

		var children = headerHost.Children;
		int size = children.Count;
		cells.Capacity = size;
		for (int i = 0; i < size; ++i)
		{
			if (children[i] is not FrameworkElement cell)
			{
				continue;
			}
			if (!IsVisibleColumn(cell.Tag as TableViewColumn))
			{
				continue;
			}
			cells.Add(cell);
		}
		return cells;
	}

	private static int IndexOfHeaderCell(
		List<FrameworkElement> cells, FrameworkElement? cell)
	{
		if (cell is null)
		{
			return -1;
		}
		for (int i = 0; i < cells.Count; ++i)
		{
			if (cells[i] == cell)
			{
				return i;
			}
		}
		return -1;
	}

	// Headers with no command are still keyboard targets: the header band is a single tab stop, and
	// arrow navigation must let keyboard users reach and read every visible column header.
	private static bool IsFocusableHeaderCell(FrameworkElement? cell)
	{
		var element = cell as UIElement;
		return element is not null && element.Visibility == Visibility.Visible;
	}

	private static UIElement? FindFirstFocusableDescendant(UIElement? element)
	{
		if (element is null)
		{
			return null;
		}

		int childCount = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < childCount; ++i)
		{
			if (VisualTreeHelper.GetChild(element, i) is DependencyObject child)
			{
				if (FocusManager.FindFirstFocusableElement(child) is { } target)
				{
					if (target is UIElement targetElement)
					{
						return targetElement;
					}
				}
			}
		}

		return null;
	}

	// The focused header is the element AT is on, so attribute the announcement to its peer.
	private static void AnnounceColumnWidthOn(object? announcer, TableViewColumn? column)
	{
		var element = announcer as UIElement;
		if (element is null || column is null)
		{
			return;
		}

		if (!AutomationPeer.ListenerExists(AutomationPeer.NotificationEvent))
		{
			return;
		}

		var peer = FrameworkElementAutomationPeer.FromElement(element);
		if (peer is null)
		{
			return;
		}

		string headerName = peer.GetName();
		if (string.IsNullOrEmpty(headerName))
		{
			if (SharedHelpers.IsStringable(column.Header))
			{
				headerName = SharedHelpers.StringableToString(column.Header);
			}
		}

		// Whole pixels: sub-pixel precision is noise in an announcement.
		var width = TableViewDetails.FormatIntegerForCurrentCulture((int)Math.Round(column.ActualWidth, MidpointRounding.AwayFromZero));

		try
		{
			var format = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TableViewColumnWidthChanged);
			if (string.IsNullOrEmpty(format))
			{
				return;
			}

			peer.RaiseNotificationEvent(
				AutomationNotificationKind.Other,
				AutomationNotificationProcessing.MostRecent,
				StringUtil.FormatString(format, headerName, width),
				"TableViewColumnWidthChangedActivityId");
		}
		catch (Exception)
		{
			// Best-effort UIA announcement; missing PRI/UIA must not cancel resize.
		}
	}

	// Both input paths end in DragCompleted, so the announcement lives there rather than in the key
	// handler: a pointer resize was otherwise completely silent to assistive technology.
	internal void AnnounceColumnWidth(object? announcer, TableViewColumn? column)
	{
		// FromElement never creates a peer. During a keyboard resize the header cell is focused so one
		// exists, but a pointer drag needs no focus, so fall back to the table's own peer.
		if (announcer is UIElement element &&
			FrameworkElementAutomationPeer.FromElement(element) is not null)
		{
			AnnounceColumnWidthOn(announcer, column);
		}
		else
		{
			AnnounceColumnWidthOn(this, column);
		}
	}

	// Alt+Left/Right resizes the focused header using WPF DataGrid's binding; Shift is large-step,
	// Ctrl is accepted as an alias, and the gripper keeps pointer and keyboard on one clamp path.
	private bool TryHandleHeaderColumnResizeKey(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return false;
		}

		// Escape aborts a pointer drag in flight; the host reverts to the width it captured at
		// DragStarted. Checked before the arrow keys because it is valid regardless of focus.
		if (args.Key == VirtualKey.Escape)
		{
			if (m_activeColumnResizeDrag is not null)
			{
				CancelColumnResizeDrag();
				args.Handled = true;
				return true;
			}
			return false;
		}

		var key = args.Key;
		if (key != VirtualKey.Left &&
			key != VirtualKey.Right)
		{
			return false;
		}

		// Alt is the resize modifier, not a disqualifier. Alt+Arrow is an ordinary accelerator chord:
		// the window menu opens on SC_KEYMENU, which the OS only synthesizes when Alt is pressed and
		// released with NO other key in the chord, so an arrow in the chord already suppresses it.
		// Marking the key handled below additionally stops the chord reaching the access-key manager.
		if (!IsKeyDown(VirtualKey.Menu))
		{
			return false;
		}

		if (!CanUserResizeColumns)
		{
			return false;
		}

		var column = ResolveFocusedHeaderColumn(args.OriginalSource, m_headerHost, out var headerCell);
		if (column is null || !column.CanResize)
		{
			return false;
		}

		var gripper = FindResizeGripperInCell(headerCell);
		if (gripper is null)
		{
			return false;
		}

		// The gripper owns direction, the RTL mirror, the step size and the Shift multiplier: one
		// implementation for both key paths.
		if (!gripper.TryKeyboardStep(key))
		{
			return false;
		}

		// The gripper carries no UIA value, so the resize is otherwise silent. The announcement is
		// raised from DragCompleted, which both input paths reach.

		args.Handled = true;
		return true;
	}

	// ----- Header-band keyboard navigation -----
	//
	// One tab stop per band: Tab crosses bands, arrows stay within the starting band, and both bands
	// share visible-column coordinates.

	private int GetFocusedVisibleHeaderIndex()
	{
		var host = m_headerHost;
		var root = XamlRoot;
		if (host is null || root is null)
		{
			return -1;
		}

		var focused = FocusManager.GetFocusedElement(root);
		if (focused is null)
		{
			return -1;
		}

		if (ResolveFocusedHeaderColumn(focused, host, out var headerCell) is null || headerCell is null)
		{
			return -1;
		}

		return IndexOfHeaderCell(GetVisibleHeaderCells(host), headerCell);
	}

	// step=0 tries the exact header; +/-1 walks to the next focusable visible header.
	private int FocusVisibleHeaderFrom(int visibleIndex, int step)
	{
		var host = m_headerHost;
		if (host is null)
		{
			return -1;
		}

		var cells = GetVisibleHeaderCells(host);
		int count = cells.Count;
		if (count <= 0 || visibleIndex < 0 || visibleIndex >= count)
		{
			return -1;
		}

		for (int i = visibleIndex; i >= 0 && i < count; i += step)
		{
			var cell = cells[i];
			if (IsFocusableHeaderCell(cell))
			{
				if (cell is UIElement element &&
					element.Focus(FocusState.Keyboard))
				{
					return i;
				}
			}
			if (step == 0)
			{
				break;
			}
		}

		return -1;
	}

	// Before any real column cursor exists, enter the first focusable header; afterward, honor the
	// shared body/header column cursor. m_columnCursorEstablished distinguishes column 0 from "none".
	private int ResolveHeaderEntryIndex(List<FrameworkElement> cells)
	{
		int count = cells.Count;
		if (count <= 0)
		{
			return -1;
		}

		int preferred = m_columnCursorEstablished
			? Math.Clamp(m_currentCellColumn, 0, count - 1)
			: 0;
		for (int i = preferred; i < count; ++i)
		{
			if (IsFocusableHeaderCell(cells[i]))
			{
				return i;
			}
		}
		for (int i = preferred - 1; i >= 0; --i)
		{
			if (IsFocusableHeaderCell(cells[i]))
			{
				return i;
			}
		}
		return -1;
	}

	// Redirect Tab entry from the band's first header to the remembered column; never redirect focus
	// leaving the band.
	private void OnHeaderHostGettingFocus(
		UIElement sender,
		GettingFocusEventArgs args)
	{
		var host = m_headerHost;
		if (host is null)
		{
			return;
		}

		// Redirect Tab entry only; programmatic focus already names its exact header.
		var direction = args.Direction;
		if (direction != FocusNavigationDirection.Next &&
			direction != FocusNavigationDirection.Previous)
		{
			return;
		}

		DependencyObject hostObject = host;
		var oldFocus = args.OldFocusedElement;
		if (oldFocus is not null &&
			(oldFocus == hostObject || SharedHelpers.IsAncestor(oldFocus, hostObject, false /* checkVisibility */)))
		{
			// Tabbing OUT of the band. Leave it alone.
			return;
		}

		var cells = GetVisibleHeaderCells(host);
		int target = ResolveHeaderEntryIndex(cells);
		if (target < 0)
		{
			// No focusable header: XAML's own target stands.
			return;
		}

		DependencyObject? element = cells[target];
		if (element is null || element == args.NewFocusedElement)
		{
			return;
		}

		// Refused during some focus operations; failing just leaves focus on the band's first header,
		// which is still inside the band.
		args.TrySetNewFocusedElement(element);
	}

	// Records the focused header column into the shared cursor for body entry.
	private void OnHeaderHostGotFocus(
		object sender,
		RoutedEventArgs args)
	{
		if (GetFocusedVisibleHeaderIndex() is var index && index >= 0)
		{
			// From here on, band entry follows the shared cursor rather than restarting at column 0.
			SetColumnCursorInternal(index);
		}
	}

	// Bare Left/Right re-asserts from the pre-key header anchor; live focus may already have moved, so
	// stepping again would skip a column.
	private bool TryHandleHeaderNavigationKey(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return false;
		}

		// Alt is the resize chord and Ctrl is not a header gesture; neither navigates.
		if (IsKeyDown(VirtualKey.Menu) || IsKeyDown(VirtualKey.Control))
		{
			return false;
		}

		var key = args.Key;
		bool isLeft = key == VirtualKey.Left;
		bool isRight = key == VirtualKey.Right;
		if (!isLeft && !isRight)
		{
			return false;
		}

		var host = m_headerHost;
		if (host is null)
		{
			return false;
		}

		var cells = GetVisibleHeaderCells(host);
		int count = cells.Count;
		if (count <= 0)
		{
			return false;
		}

		int liveIndex = GetFocusedVisibleHeaderIndex();

		// Prefer the pre-key header anchor; fall back to live focus if the snapshot is stale.
		int currentIndex = m_navAnchorHeaderColumn;
		if (currentIndex < 0 || currentIndex >= count)
		{
			currentIndex = liveIndex;
		}
		if (currentIndex < 0)
		{
			// Focus is not on the header band; this is not a header gesture.
			return false;
		}

		// RTL mirrors the cell move: Right means "towards the row end" in reading order.
		bool isRtl = FlowDirection == FlowDirection.RightToLeft;
		bool forward = isRight != isRtl;
		int step = forward ? 1 : -1;
		int targetIndex = Math.Clamp(currentIndex + step, 0, count - 1);

		if (liveIndex != targetIndex)
		{
			FocusVisibleHeaderFrom(targetIndex, targetIndex == currentIndex ? 0 : step);
		}

		// Consume bounds/refusals so directional focus navigation cannot walk out of the table.
		args.Handled = true;
		return true;
	}

	// Clamp Up/Down inside the header band; otherwise focus navigation can escape the band or row
	// navigation can enter row 0.
	private bool TryHandleHeaderVerticalKey(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return false;
		}

		if (IsKeyDown(VirtualKey.Menu))
		{
			return false;
		}

		var key = args.Key;
		if (key != VirtualKey.Down &&
			key != VirtualKey.Up)
		{
			return false;
		}

		int headerIndex = m_navAnchorHeaderColumn;
		if (headerIndex < 0)
		{
			headerIndex = GetFocusedVisibleHeaderIndex();
		}
		if (headerIndex < 0)
		{
			// Focus is not on the header band; this is not a header gesture.
			return false;
		}

		args.Handled = true;
		return true;
	}

	private TableViewColumn? ResolveHeaderSortKeyTarget(KeyRoutedEventArgs args)
	{
		if (IsKeyDown(VirtualKey.Menu) ||
			IsKeyDown(VirtualKey.Control) ||
			IsKeyDown(VirtualKey.Shift))
		{
			return null;
		}

		if (!CanUserSortColumns)
		{
			return null;
		}

		var column = ResolveFocusedHeaderColumn(args.OriginalSource, m_headerHost, out var headerCell);
		if (column is null || !column.CanSort)
		{
			return null;
		}

		// Require the header chrome itself; an unhandled Enter from a TextBox in a header template must
		// not toggle sort.
		if (args.OriginalSource as DependencyObject != headerCell)
		{
			return null;
		}

		return column;
	}

	// Enter activates on KeyDown; Space follows XAML activation semantics: arm on KeyDown, fire on
	// unhandled KeyUp.
	private bool TryHandleHeaderSortKey(KeyRoutedEventArgs args)
	{
		if (args.Handled)
		{
			return false;
		}

		var key = args.Key;
		bool isEnter = key == VirtualKey.Enter;
		bool isSpace = key == VirtualKey.Space;
		if (!isEnter && !isSpace)
		{
			return false;
		}

		var column = ResolveHeaderSortKeyTarget(args);
		if (column is null)
		{
			return false;
		}

		// TODO Uno: the Skia hosts never set CorePhysicalKeyStatus.WasKeyDown on KeyDown, so auto-repeat
		// is indistinguishable from a fresh press here (a held Enter re-toggles the sort on every repeat).
		if (args.KeyStatus.WasKeyDown)
		{
			if (isSpace && m_headerSortSpaceArmedColumn.Get() == column)
			{
				args.Handled = true;
				return true;
			}
			return false;
		}

		if (isSpace)
		{
			m_headerSortSpaceArmedColumn = new WeakReference<TableViewColumn>(column);
			args.Handled = true;
			return true;
		}

		if (!ToggleSortDirection(column))
		{
			return false;
		}

		args.Handled = true;
		return true;
	}

	private bool TryHandleHeaderSortKeyUp(KeyRoutedEventArgs args)
	{
		var armed = m_headerSortSpaceArmedColumn.Get();
		if (armed is null)
		{
			return false;
		}

		if (args.Key != VirtualKey.Space)
		{
			return false;
		}

		if (args.Handled)
		{
			return false;
		}

		m_headerSortSpaceArmedColumn = null;

		if (ResolveHeaderSortKeyTarget(args) != armed)
		{
			return false;
		}

		if (!ToggleSortDirection(armed))
		{
			return false;
		}

		args.Handled = true;
		return true;
	}

	private void OnKeyUpForHeaderSort(
		object sender,
		KeyRoutedEventArgs args)
	{
		TryHandleHeaderSortKeyUp(args);
	}

	private void OnPreviewKeyDownForNavigation(
		object sender,
		KeyRoutedEventArgs args)
	{
		// Snapshot before XAML focus navigation; Left/Right only need the cell anchor.
		switch (args.Key)
		{
			case VirtualKey.Up:
			case VirtualKey.Down:
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
				m_navAnchorRow = GetFocusedRowIndex();
				goto case VirtualKey.Left; // Original C++: [[fallthrough]];
			case VirtualKey.Left:
			case VirtualKey.Right:
				m_navAnchorCellRow = -1;
				m_navAnchorCellColumn = -1;
				TryGetFocusedCell(out m_navAnchorCellRow, out m_navAnchorCellColumn, true /* requireExactCell */);
				// Record the pre-key body level: cell, row container, or group header.
				m_navAnchorRowContainer = m_navAnchorCellColumn >= 0 ? -1 : GetFocusedRowContainerIndex();
				// Group headers own Left/Right for collapse/expand, not cell drill-in.
				m_navAnchorGroupHeader =
					(m_navAnchorCellColumn >= 0 || m_navAnchorRowContainer >= 0) ? -1 : GetFocusedGroupHeaderIndex();
				// Header navigation needs the same pre-key anchor.
				m_navAnchorHeaderColumn = GetFocusedVisibleHeaderIndex();
				break;
			default:
				m_navAnchorHeaderColumn = -1;
				m_navAnchorRowContainer = -1;
				m_navAnchorGroupHeader = -1;
				break;
		}
	}

	private void OnKeyDownForNavigation(
		object sender,
		KeyRoutedEventArgs args)
	{
		// While editing, let the editor own keys; otherwise PageUp/PageDown can recycle the edited row
		// mid-edit. WPF DataGrid does the same.
		if (IsEditing)
		{
			if (CurrentEditingElement() is { } editingElement)
			{
				if (XamlRoot is { } root)
				{
					var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
					while (focused is not null)
					{
						if (focused == editingElement)
						{
							return;
						}
						focused = VisualTreeHelper.GetParent(focused);
					}
				}
			}
		}

		// After the editing guard so an open editor keeps its arrow keys; Alt+Arrow cannot shadow the
		// bare navigation arrows below.
		if (TryHandleHeaderColumnResizeKey(args))
		{
			return;
		}

		// Header arrows run before sort and row navigation so focus stays inside the header band.
		if (TryHandleHeaderNavigationKey(args))
		{
			return;
		}

		if (TryHandleHeaderVerticalKey(args))
		{
			return;
		}

		if (TryHandleHeaderSortKey(args))
		{
			return;
		}

		// handledEventsToo recovers keys swallowed by PART_BodyScroller, but not keys consumed by
		// hosted controls. Accept exact focus on our row/group/cell only, excluding nested TableViews.
		if (args.Handled)
		{
			bool focusOnOurRow = false;
			if (XamlRoot is { } root)
			{
				// Group headers and exact cells are valid anchors. Do not walk from descendants: hosted
				// TextBox/ComboBox controls keep the handled keys they claim.
				if (FocusManager.GetFocusedElement(root) is UIElement focused)
				{
					if (focused is TableViewRow || focused is TableViewGroupHeader)
					{
						if (m_rowsRepeater is { } repeater)
						{
							focusOnOurRow = repeater.GetElementIndex(focused) >= 0;
						}
					}
					else
					{
						focusOnOurRow = TryGetFocusedCell(out _, out _, true /* requireExactCell */);
					}
				}
			}
			if (!focusOnOurRow)
			{
				return;
			}
		}

		var key = args.Key;

		// Escape runs after the handled guard: hosted controls such as ComboBox keep their first
		// Escape, and the unhandled second Escape restores grid navigation. Header resize ran above.
		if (TryHandleCellInteractionEscapeKey(args))
		{
			return;
		}

		if (TryHandleCellInteractionNavigationKey(args))
		{
			return;
		}

		// Claim group-header expand/collapse before row drill-in, then cell cursor movement; each owns
		// a different level of the treegrid.
		if (TryHandleGroupHeaderExpandCollapseKey(args))
		{
			return;
		}

		if (TryHandleRowLevelDrillKey(args))
		{
			return;
		}

		if (TryHandleCellInteractionEnterKey(args))
		{
			return;
		}

		if (TryHandleCellNavigationKey(args))
		{
			return;
		}

		int rowCount = GetItemsSourceCount();
		if (rowCount <= 0)
		{
			return;
		}

		bool isRowSelectionSpace =
			key == VirtualKey.Space &&
			!IsKeyDown(VirtualKey.Menu) &&
			!IsKeyDown(VirtualKey.Control);
		if (isRowSelectionSpace && CanSelectRows())
		{
			if (XamlRoot is { } root)
			{
				if (m_rowsRepeater is { } repeater)
				{
					int focusedIndex = -1;
					if (FocusManager.GetFocusedElement(root) is TableViewRow focusedRow)
					{
						focusedIndex = repeater.GetElementIndex(focusedRow);
					}
					else
					{
						if (!TryGetFocusedCell(out focusedIndex, out _, true /* requireExactCell */))
						{
							focusedIndex = -1;
						}
					}

					if (focusedIndex >= 0)
					{
						SelectRowIndexFromInteraction(focusedIndex);
						args.Handled = true;
					}
				}
			}

			return;
		}

		// Use the pre-key row anchor; live focus may already have advanced.
		int currentRow = m_navAnchorRow;

		// Ctrl+Arrow moves focus without selection/UIA churn, matching ListViewBase.
		bool isControlDown = IsKeyDown(VirtualKey.Control);

		// If relative navigation re-enters without a row anchor, resume from selection instead of
		// yanking focus to row 0. Absolute keys keep their own entry points below.
		if (currentRow < 0 &&
			(key == VirtualKey.Up ||
				key == VirtualKey.Down))
		{
			if (SelectedIndexInternal() is var selectedRow &&
				selectedRow >= 0 && selectedRow < rowCount)
			{
				currentRow = selectedRow;
			}
		}
		if (currentRow < 0)
		{
			// First navigation key enters at the WPF DataGrid/ListView-equivalent row.
			int initialRow = -1;
			switch (key)
			{
				case VirtualKey.Up:
				case VirtualKey.Down:
				case VirtualKey.Home:
				case VirtualKey.PageUp:
					initialRow = 0;
					break;
				case VirtualKey.End:
					initialRow = rowCount - 1;
					break;
				case VirtualKey.PageDown:
					initialRow = Math.Clamp(GetEstimatedRowsPerPage() - 1, 0, rowCount - 1);
					break;
				default:
					break;
			}
			if (initialRow >= 0 && FocusRowContainer(initialRow))
			{
				// Single selection follows the keyboard cursor, matching ListView and WPF's DataGrid.
				if (!isControlDown)
				{
					SelectRowIndexFromKeyboardFocus(initialRow);
				}
				args.Handled = true;
			}
			return;
		}

		int newRow = currentRow;
		bool consumeKey = true;
		// Absolute/page keys are consumed at boundaries; Up/Down may escape the table.
		bool absoluteRowNav = false;

		switch (key)
		{
			case VirtualKey.Up:
				{
					// Use the 1-column logical grid to reuse clamping behavior.
					GridCoordinateHelper helper = new(rowCount, 1);
					if (helper.TryGetNextFocusableCell(currentRow, 0,
							FocusNavigationDirection.Up, false, out var nextR, out var nextC))
					{
						newRow = nextR;
					}
					break;
				}
			case VirtualKey.Down:
				{
					GridCoordinateHelper helper = new(rowCount, 1);
					if (helper.TryGetNextFocusableCell(currentRow, 0,
							FocusNavigationDirection.Down, false, out var nextR, out var nextC))
					{
						newRow = nextR;
					}
					break;
				}
			case VirtualKey.Home:
				newRow = 0;
				absoluteRowNav = true;
				break;
			case VirtualKey.End:
				newRow = rowCount - 1;
				absoluteRowNav = true;
				break;
			case VirtualKey.PageUp:
				{
					int step = GetEstimatedRowsPerPage();
					newRow = Math.Max(0, currentRow - step);
					absoluteRowNav = true;
					break;
				}
			case VirtualKey.PageDown:
				{
					int step = GetEstimatedRowsPerPage();
					newRow = Math.Min(rowCount - 1, currentRow + step);
					absoluteRowNav = true;
					break;
				}
			default:
				consumeKey = false;
				break;
		}

		if (consumeKey)
		{
			bool hasCellAnchor =
				m_navAnchorRow >= 0 &&
				m_navAnchorCellRow == m_navAnchorRow &&
				m_navAnchorCellColumn >= 0;
			int anchorColumn = hasCellAnchor ? m_navAnchorCellColumn : m_currentCellColumn;
			if (newRow != currentRow)
			{
				// Up/Down preserve the starting level per ARIA treegrid: row-to-row or same-column
				// cell-to-cell.
				bool moved = hasCellAnchor
					? FocusCell(newRow, anchorColumn)
					: FocusRowContainer(newRow);
				if (moved)
				{
					if (!isControlDown)
					{
						SelectRowIndexFromKeyboardFocus(newRow);
					}
					args.Handled = true;
				}
			}
			else if (absoluteRowNav)
			{
				args.Handled = true;
			}
		}
	}

	internal bool FocusRow(int index)
	{
		// Follow the current cursor level: row-level callers land on rows; drilled-in callers stay on cells.
		return m_cellCursorActive
			? FocusCell(index, m_currentCellColumn)
			: FocusRowContainer(index);
	}

	// ----- Cell-level keyboard focus -----

	private TableViewRow? GetRealizedRowAt(int rowIndex)
	{
		if (rowIndex < 0)
		{
			return null;
		}
		if (m_rowsRepeater is { } repeater)
		{
			return repeater.TryGetElement(rowIndex) as TableViewRow;
		}
		return null;
	}

	internal TableViewRow? ResolveFocusEntryRow(
		TableViewRow? row, DependencyObject? oldFocusedElement)
	{
		if (row is null)
		{
			return null;
		}

		DependencyObject selfObject = this;
		bool focusCameFromWithin =
			oldFocusedElement == selfObject ||
			SharedHelpers.IsAncestor(oldFocusedElement, selfObject, false /* checkVisibility */);

		if (m_currentCellRow < 0 || focusCameFromWithin)
		{
			return row;
		}

		// m_currentCellRow is a bare index, and nothing renumbers it when the source reshapes
		// (sort, filter, group expand/collapse, insert, remove), so index 2 can name a different
		// record by the time focus comes back. Follow the remembered ITEM instead: m_currentItem is
		// written by the same OnRowCellFocusChanged funnel that writes m_currentCellRow, and it is
		// cleared with SetCurrentCell(nullptr, nullptr) when the source is replaced.
		var rememberedItem = m_currentItem;
		object? itemAtRememberedRow = null;
		bool stillTheSameRecord =
			rememberedItem is not null &&
			TryGetItemAtRowIndex(m_currentCellRow, out itemAtRememberedRow) &&
			SameInspectableIdentity(itemAtRememberedRow, rememberedItem);

		// A reshape that only moved the item is recoverable: FindRealizedRowForItem searches
		// realized rows only, so this never forces a realization or a surprise scroll. When the
		// item is gone or off-screen, focus stays on the row the framework aimed at.
		if ((stillTheSameRecord
				? GetRealizedRowAt(m_currentCellRow)
				: FindRealizedRowForItem(rememberedItem)) is { } remembered)
		{
			return remembered;
		}

		return row;
	}

	// Popping out of cell level must restore the drilled row to row level, or that row disappears from
	// Tab navigation.
	internal void SetCellCursorActiveInternal(bool active)
	{
		if (!active)
		{
			if (m_cellLevelRow.Get() is { } drilled)
			{
				drilled.SetCellLevelInternal(false);
			}
			m_cellLevelRow = null;
		}

		m_cellCursorActive = active;
	}

	internal void OnRowCellFocusChanged(TableViewRow? row)
	{
		if (row is null)
		{
			return;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return;
		}

		var rowImpl = row;
		int rowIndex = repeater.GetElementIndex(row);

		UIElement? focusedCell = null;
		if (XamlRoot is { } root)
		{
			if (FocusManager.GetFocusedElement(root) is DependencyObject focused)
			{
				focusedCell = rowImpl.FindOwnCellInternal(focused, false /* requireExact */);
			}
		}

		if (focusedCell is null)
		{
			// Focus is on the ROW container itself - the body's entry level. Record the row so a later
			// re-entry comes back here, and make sure the two-level cursor agrees that no cell is
			// current, including releasing whichever row was previously drilled in.
			if (rowIndex >= 0)
			{
				m_currentCellRow = rowIndex;
				SetCurrentItem(TryGetItemAtRowIndex(rowIndex, out var item) ? item : null);
			}
			SetCellCursorActiveInternal(false);
			return;
		}

		int columnIndex = rowImpl.GetVisibleCellIndexInternal(focusedCell);
		if (columnIndex >= 0)
		{
			// A cell actually took focus, so the user is in a column. Establishes the shared cursor for
			// the header band too - Shift+Tab back up should land on the column being worked in.
			SetColumnCursorInternal(columnIndex);
		}

		if (rowIndex >= 0)
		{
			m_currentCellRow = rowIndex;
		}

		// A cell holds focus, so the cursor is at cell level on THIS row. Release any other row that
		// was still drilled in before claiming this one, so only one row ever has its cells armed.
		if (m_cellLevelRow.Get() is { } previous && previous != row)
		{
			previous.SetCellLevelInternal(false);
		}
		rowImpl.SetCellLevelInternal(true);
		m_cellLevelRow = new WeakReference<TableViewRow>(row);
		m_cellCursorActive = true;

		if (!IsEditing)
		{
			if (rowImpl.GetCellOwningColumn(focusedCell) is { } column)
			{
				if (rowIndex >= 0 && TryGetItemAtRowIndex(rowIndex, out var item) && item is not null)
				{
					SetCurrentCell(item, column);
				}
			}
		}
	}

	internal bool TryGetFocusedCell(out int rowIndex, out int columnIndex, bool requireExactCell)
	{
		rowIndex = -1;
		columnIndex = -1;

		var repeater = m_rowsRepeater;
		var root = XamlRoot;
		if (repeater is null || root is null)
		{
			return false;
		}

		if (FocusManager.GetFocusedElement(root) is not DependencyObject focused)
		{
			return false;
		}

		DependencyObject? node = focused;
		while (node is not null)
		{
			if (node is TableViewRow row)
			{
				int index = repeater.GetElementIndex(row);
				if (index >= 0)
				{
					var cell = row.FindOwnCellInternal(focused, requireExactCell);
					if (cell is null)
					{
						return false;
					}
					int column = row.GetVisibleCellIndexInternal(cell);
					if (column < 0)
					{
						return false;
					}
					rowIndex = index;
					columnIndex = column;
					return true;
				}
			}
			node = VisualTreeHelper.GetParent(node);
		}

		return false;
	}

	internal bool FocusCell(int rowIndex, int visibleColumnIndex)
	{
		if (visibleColumnIndex >= 0)
		{
			// An explicit column names the column the caller wants the cursor on, so it establishes it.
			SetColumnCursorInternal(visibleColumnIndex);
		}

		return FocusRowElementInternal(rowIndex, m_currentCellColumn, true /* cellLevel */);
	}

	// Single writer for the shared column cursor; setting it also marks that a real column was chosen.
	private void SetColumnCursorInternal(int visibleColumnIndex)
	{
		if (visibleColumnIndex < 0)
		{
			return;
		}

		m_currentCellColumn = visibleColumnIndex;
		m_columnCursorEstablished = true;
	}

	// Reset to "no column chosen" so the next band entry starts at the first focusable header.
	private void ResetColumnCursorInternal()
	{
		m_currentCellColumn = 0;
		m_columnCursorEstablished = false;
	}

	internal bool FocusRowContainer(int rowIndex)
	{
		// Row level keeps the remembered column untouched for the next drill-in.
		return FocusRowElementInternal(rowIndex, m_currentCellColumn, false /* cellLevel */);
	}

	// Shared realization path; if layout is required, finish on LayoutUpdated at the same requested level.
	private bool FocusRowElementInternal(int rowIndex, int targetColumn, bool cellLevel)
	{
		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return false;
		}

		var rowCount = GetItemsSourceCount();
		if (rowIndex < 0 || rowIndex >= rowCount)
		{
			return false;
		}

		if (m_pendingFocusLayoutToken.Disposable is not null)
		{
			// Original C++: LayoutUpdated(m_pendingFocusLayoutToken); m_pendingFocusLayoutToken = {};
			m_pendingFocusLayoutToken.Disposable = null;
		}

		var element = repeater.GetOrCreateElement(rowIndex);
		if (element is null)
		{
			return false;
		}

		var frameworkElement = element as FrameworkElement;
		if (frameworkElement is not null)
		{
			frameworkElement.StartBringIntoView();
		}

		if (element is Control)
		{
			if (frameworkElement is not null &&
				(!frameworkElement.IsLoaded ||
					frameworkElement.ActualHeight <= 0.0 ||
					VisualTreeHelper.GetParent(frameworkElement) is null))
			{
				WeakReference<TableView> weakThis = new(this);
				EventHandler<object> handler = (_, _) =>
				{
					if (!weakThis.TryGetTarget(out var strongThis))
					{
						return;
					}

					if (strongThis.m_pendingFocusLayoutToken.Disposable is not null)
					{
						// Original C++: strongThis->LayoutUpdated(strongThis->m_pendingFocusLayoutToken); strongThis->m_pendingFocusLayoutToken = {};
						strongThis.m_pendingFocusLayoutToken.Disposable = null;
					}

					// Layout callbacks fail-fast on escaping exceptions, so contain realization/focus work.
					try
					{
						if (rowIndex < 0 || rowIndex >= strongThis.GetItemsSourceCount())
						{
							return;
						}

						if (strongThis.m_rowsRepeater is { } repeater)
						{
							if (repeater.GetOrCreateElement(rowIndex) is { } element)
							{
								if (cellLevel)
								{
									strongThis.FocusRealizedRowCell(element, targetColumn);
								}
								else
								{
									strongThis.FocusRowContainerInternal(element);
								}
							}
						}
					}
					catch (Exception)
					{
						// Best-effort deferred focus: the row can be recycled or the source
						// reshaped between the request and this callback.
					}
				};
				LayoutUpdated += handler;
				m_pendingFocusLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= handler);
				return true;
			}

			return cellLevel
				? FocusRealizedRowCell(element, targetColumn)
				: FocusRowContainerInternal(element);
		}
		return false;
	}

	// Focuses an already-realized container at row level; group headers have no cell level.
	private bool FocusRowContainerInternal(UIElement element)
	{
		var row = element as TableViewRow;
		if (row is not null)
		{
			// Make the row focusable before clearing the focused cell from tab order.
			row.EnableRowFocusInternal();
		}

		if (element is not Control control || !control.Focus(FocusState.Keyboard))
		{
			return false;
		}

		// Focus has landed on the row; now cells can leave the tab order.
		if (row is not null)
		{
			row.SetCellLevelInternal(false);
		}
		SetCellCursorActiveInternal(false);
		return true;
	}

	// Focuses a visible cell in an already-realized row; group headers stay at row level.
	internal bool FocusRealizedRowCell(UIElement element, int visibleColumnIndex)
	{
		if (element is TableViewRow row)
		{
			var rowImpl = row;
			int cellCount = rowImpl.GetVisibleCellCountInternal();
			if (cellCount > 0)
			{
				int column = Math.Clamp(visibleColumnIndex, 0, cellCount - 1);
				if (rowImpl.FocusVisibleCellInternal(column, FocusState.Keyboard))
				{
					return true;
				}
			}

			// The row has no focusable cell (no columns, or every column collapsed). Row level is the
			// only level left, and it is a legitimate resting place in the two-level model.
			return FocusRowContainerInternal(element);
		}

		// A group header: never a cell-level target.
		return FocusRowContainerInternal(element);
	}

	// Group headers use treegrid Left/Right expand/collapse, not row drill-in. This handledEventsToo
	// backstop is safe because directional expansion is idempotent.
	private bool TryHandleGroupHeaderExpandCollapseKey(KeyRoutedEventArgs args)
	{
		if (IsKeyDown(VirtualKey.Menu) || IsKeyDown(VirtualKey.Control))
		{
			return false;
		}

		var key = args.Key;
		bool isLeft = key == VirtualKey.Left;
		bool isRight = key == VirtualKey.Right;
		if (!isLeft && !isRight)
		{
			return false;
		}

		// Use the pre-key group-header anchor; live focus may already have moved.
		if (m_navAnchorGroupHeader < 0)
		{
			return false;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return false;
		}

		if (repeater.TryGetElement(m_navAnchorGroupHeader) is not TableViewGroupHeader header)
		{
			return false;
		}

		// Consume even when nothing changes so focus navigation cannot walk sideways out of the body.
		args.Handled = true;

		if (!header.IsExpandable)
		{
			return true;
		}

		// RTL mirrors the arrows the same way every other band does: "Right" is towards the row end in
		// reading order, and expanding is the reading-order-forward direction.
		bool isRtl = FlowDirection == FlowDirection.RightToLeft;
		bool expand = isRight != isRtl;

		// Avoid queuing a reshape/focus-restore round trip for repeated no-op arrows.
		if (header.IsExpanded == expand)
		{
			return true;
		}

		SetGroupExpansion(header, expand);
		return true;
	}

	// Exact focused group-header index; hosted focusable content keeps the arrow keys it claims.
	private int GetFocusedGroupHeaderIndex()
	{
		var repeater = m_rowsRepeater;
		var root = XamlRoot;
		if (repeater is null || root is null)
		{
			return -1;
		}

		if (FocusManager.GetFocusedElement(root) is not UIElement focused)
		{
			return -1;
		}

		if (focused is not TableViewGroupHeader header)
		{
			return -1;
		}

		// Also rejects a nested TableView's header, which is not an element of our repeater.
		return repeater.GetElementIndex(header);
	}

	private bool TryHandleCellInteractionEnterKey(KeyRoutedEventArgs args)
	{
		if (args.Handled ||
			args.Key != VirtualKey.Enter ||
			IsEditing ||
			IsKeyDown(VirtualKey.Menu) ||
			IsKeyDown(VirtualKey.Control) ||
			IsKeyDown(VirtualKey.Shift))
		{
			return false;
		}

		if (!TryGetFocusedCell(out var rowIndex, out var columnIndex, true /* requireExactCell */))
		{
			return false;
		}

		var row = GetRealizedRowAt(rowIndex);
		if (row is null)
		{
			return false;
		}

		// Enter reaches focusable display content, not the editor: F2 owns editing, while template
		// columns need Enter to reach controls such as ComboBox/CheckBox inside the single tab stop.
		var cell = row.GetVisibleCellInternal(columnIndex);
		if (cell is not FrameworkElement cellElement)
		{
			return false;
		}

		var target = FindFirstFocusableDescendant(cell);
		if (target is null || !target.Focus(FocusState.Keyboard))
		{
			return false;
		}

		SetColumnCursorInternal(columnIndex);
		SetCellCursorActiveInternal(true);
		m_cellInteractionActive = true;
		m_cellInteractionCell = new WeakReference<FrameworkElement>(cellElement);
		args.Handled = true;
		return true;
	}

	private bool TryHandleCellInteractionNavigationKey(KeyRoutedEventArgs args)
	{
		if (args.Handled ||
			!m_cellInteractionActive ||
			IsEditing ||
			IsKeyDown(VirtualKey.Menu) ||
			IsKeyDown(VirtualKey.Control))
		{
			return false;
		}

		switch (args.Key)
		{
			case VirtualKey.Left:
			case VirtualKey.Right:
			case VirtualKey.Up:
			case VirtualKey.Down:
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
				break;
			default:
				return false;
		}

		var enteredCell = m_cellInteractionCell.Get();
		var root = XamlRoot;
		var focused = root is not null ?
			FocusManager.GetFocusedElement(root) as UIElement :
			null;
		if (enteredCell is null ||
			focused is null ||
			(focused != enteredCell &&
				!SharedHelpers.IsAncestor(focused, enteredCell, false /* checkVisibility */)))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		if (!TryGetFocusedCell(out _, out _, false /* requireExactCell */))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		if (TryGetFocusedCell(out _, out _, true /* requireExactCell */))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		// The hosted control did not claim this key - args.Handled() is still false - so without this
		// XAML's directional navigation takes it and moves focus to whatever is geometrically nearest.
		// That walks OUT of the cell and lands unpredictably: from a CheckBox in column 7, Right
		// skipped the ComboBox in column 8 entirely and landed on the date editor in column 9, because
		// the search is by position, not by column. Interaction mode means the cell's content owns the
		// arrows; Escape is the way back to grid navigation, exactly as the ARIA grid pattern requires.
		args.Handled = true;
		return true;
	}

	private bool TryHandleCellInteractionEscapeKey(KeyRoutedEventArgs args)
	{
		if (args.Handled ||
			args.Key != VirtualKey.Escape ||
			!m_cellInteractionActive ||
			IsEditing)
		{
			return false;
		}

		var enteredCell = m_cellInteractionCell.Get();
		var root = XamlRoot;
		var focused = root is not null ?
			FocusManager.GetFocusedElement(root) as UIElement :
			null;
		if (enteredCell is null ||
			focused is null ||
			(focused != enteredCell &&
				!SharedHelpers.IsAncestor(focused, enteredCell, false /* checkVisibility */)))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		if (!TryGetFocusedCell(out var rowIndex, out var columnIndex, false /* requireExactCell */))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		if (TryGetFocusedCell(out _, out _, true /* requireExactCell */))
		{
			m_cellInteractionActive = false;
			m_cellInteractionCell = null;
			return false;
		}

		SetColumnCursorInternal(columnIndex);
		if (!FocusCell(rowIndex, columnIndex))
		{
			return false;
		}

		if (!TryGetFocusedCell(out var focusedRow, out var focusedColumn, true /* requireExactCell */) ||
			focusedRow != rowIndex ||
			focusedColumn != columnIndex)
		{
			return false;
		}

		SetCellCursorActiveInternal(true);
		m_cellInteractionActive = false;
		m_cellInteractionCell = null;
		args.Handled = true;
		return true;
	}

	// In the ARIA treegrid model, Right drills from row to first cell and Left from first cell pops
	// back to row; group headers own expand/collapse above.
	private bool TryHandleRowLevelDrillKey(KeyRoutedEventArgs args)
	{
		if (IsKeyDown(VirtualKey.Menu) || IsKeyDown(VirtualKey.Control))
		{
			return false;
		}

		var key = args.Key;
		bool isLeft = key == VirtualKey.Left;
		bool isRight = key == VirtualKey.Right;
		if (!isLeft && !isRight)
		{
			return false;
		}

		// RTL mirrors the arrows exactly as the cell and header moves do: "Right" means "towards the
		// row end" in reading order, so drilling in is the reading-order-forward key.
		bool isRtl = FlowDirection == FlowDirection.RightToLeft;
		bool drillIn = isRight != isRtl;

		// Use the pre-key row anchor; live focus may already have moved out of the row.

		// Focus was on a row container.
		if (m_navAnchorRowContainer >= 0)
		{
			if (!drillIn)
			{
				// Nothing further out at row level. Consumed anyway: an unconsumed Left here reaches
				// directional focus navigation, which would walk focus sideways out of the table.
				args.Handled = true;
				return true;
			}

			var row = GetRealizedRowAt(m_navAnchorRowContainer);
			if (row is null)
			{
				return false;
			}

			var rowImpl = row;
			int cellCount = rowImpl.GetVisibleCellCountInternal();
			if (cellCount <= 0)
			{
				// A row with no visible columns has no cell level to drill into.
				args.Handled = true;
				return true;
			}

			// Enter at the FIRST cell, not the remembered column. Right and Left must be inverses: Left
			// on the first cell pops back to the row, so Right on the row has to land where that Left
			// came from. Drilling into a remembered column instead made the pair asymmetric - Right
			// from the row reached column 4, but Left from column 4 went to column 3, never back to the
			// row - and put focus mid-row when the user pressed the key at the row's leading edge. The
			// shared column cursor still serves TAB entry, which is where resuming a column belongs.
			rowImpl.FocusVisibleCellInternal(0, FocusState.Keyboard);
			args.Handled = true;
			return true;
		}

		// Focus was on a cell: only the first cell pops out; other cells leave the key to cell navigation.
		if (drillIn || m_navAnchorCellRow < 0 || m_navAnchorCellColumn != 0)
		{
			return false;
		}

		if (!FocusRowContainer(m_navAnchorCellRow))
		{
			// The row refused focus; let the cell cursor have the key rather than swallowing it.
			return false;
		}

		args.Handled = true;
		return true;
	}

	// Exact focused row-container index; group headers and cells own different navigation levels.
	private int GetFocusedRowContainerIndex()
	{
		var repeater = m_rowsRepeater;
		var root = XamlRoot;
		if (repeater is null || root is null)
		{
			return -1;
		}

		if (FocusManager.GetFocusedElement(root) is not UIElement focused)
		{
			return -1;
		}

		if (focused is not TableViewRow row)
		{
			return -1;
		}

		// Also rejects a nested TableView's row, which is not an element of our repeater.
		return repeater.GetElementIndex(row);
	}

	// Cell-level navigation follows WPF/ListView: Left/Right/Home/End do not wrap, Ctrl+Home/End jump
	// grid-wide, and selection stays row-level. Re-assert from the pre-key anchor because XAML focus
	// navigation may already have moved live focus.
	private bool TryHandleCellNavigationKey(KeyRoutedEventArgs args)
	{
		if (IsKeyDown(VirtualKey.Menu))
		{
			return false;
		}

		if (!TryMoveCellCursorFromAnchor(
				args.Key, IsKeyDown(VirtualKey.Control),
				m_navAnchorCellRow, m_navAnchorCellColumn))
		{
			return false;
		}

		args.Handled = true;
		return true;
	}

	internal bool TryMoveCellCursorFromAnchor(
		VirtualKey key, bool isControlDown,
		int anchorRow, int anchorColumn)
	{
		bool isLeft = key == VirtualKey.Left;
		bool isRight = key == VirtualKey.Right;
		bool isHome = key == VirtualKey.Home;
		bool isEnd = key == VirtualKey.End;
		if (!isLeft && !isRight && !isHome && !isEnd)
		{
			return false;
		}

		if (isControlDown && (isLeft || isRight))
		{
			return false;
		}

		bool onLiveCell = TryGetFocusedCell(out var liveRow, out var liveColumn, true /* requireExactCell */);

		int currentRow = anchorRow;
		int currentColumn = anchorColumn;
		bool onCell = currentRow >= 0 && currentColumn >= 0;

		if (onCell && onLiveCell && liveRow != currentRow)
		{
			currentRow = liveRow;
			currentColumn = liveColumn;
		}

		int rowCount = GetItemsSourceCount();
		if (rowCount <= 0)
		{
			return false;
		}

		if (!onCell)
		{
			// Ctrl+Home/End may enter the grid; relative keys need an existing cell cursor.
			if (!isControlDown || (!isHome && !isEnd))
			{
				return false;
			}

			int targetRow = isHome ? 0 : rowCount - 1;
			// Consumed even when focus refuses: an unconsumed Home/End reaches the row-level handler,
			// which re-reads it as a jump to the first/last ROW.
			FocusCell(targetRow, isHome ? 0 : c_lastColumnSentinel);
			return true;
		}

		var row = GetRealizedRowAt(currentRow);
		if (row is null)
		{
			return false;
		}

		int cellCount = row.GetVisibleCellCountInternal();
		if (cellCount <= 0)
		{
			return false;
		}

		if (isControlDown && (isHome || isEnd))
		{
			int targetRow = isHome ? 0 : rowCount - 1;
			// Ctrl moves the cursor without dragging selection/UIA events through every row.
			FocusCell(targetRow, isHome ? 0 : c_lastColumnSentinel);
			return true;
		}

		int targetColumn = currentColumn;
		if (isHome)
		{
			targetColumn = 0;
		}
		else if (isEnd)
		{
			targetColumn = cellCount - 1;
		}
		else
		{
			// RTL mirrors the arrows: Right means "towards the row end" in reading order.
			bool isRtl = FlowDirection == FlowDirection.RightToLeft;
			bool forward = isRight != isRtl;
			targetColumn = Math.Clamp(currentColumn + (forward ? 1 : -1), 0, cellCount - 1);
		}

		bool atBound = targetColumn == currentColumn;
		bool alreadyOnTarget = onLiveCell && liveRow == currentRow && liveColumn == targetColumn;

		bool moved = alreadyOnTarget;
		if (!moved)
		{
			row.FocusVisibleCellInternal(
				targetColumn, FocusState.Keyboard);

			// FocusVisibleCellInternal can fall back to the row, so read focus back; non-exact keeps
			// hosted cell content as a hit.
			moved =
				TryGetFocusedCell(out var movedRow, out var movedColumn, false /* requireExactCell */) &&
				movedRow == currentRow && movedColumn == targetColumn;
		}

		if (moved)
		{
			SetColumnCursorInternal(targetColumn);
			return true;
		}

		// If focus refuses the target, keep the cursor on real focus. Still consume bounds and Home/End
		// so focus navigation or row navigation cannot reinterpret them.
		return atBound || isHome || isEnd;
	}

	private int GetFocusedRowIndex()
	{
		if (m_rowsRepeater is { } repeater)
		{
			if (XamlRoot is { } root)
			{
				if (FocusManager.GetFocusedElement(root) is DependencyObject focused)
				{
					// Walk up in case focus is on a row descendant.
					DependencyObject? node = focused;
					while (node is not null)
					{
						// Both data rows and group headers are elements of m_rowsRepeater, so either is a
						// valid focus anchor for arrow navigation. A header MUST be recognized here: if
						// it reports -1, relative navigation falls back to row 0 / the selected row and
						// fights the framework's built-in focus move (skipped rows, focus bouncing back
						// to the previous header, and selection landing on the wrong row).
						if (node is TableViewRow || node is TableViewGroupHeader)
						{
							if (node is UIElement element)
							{
								var idx = repeater.GetElementIndex(element);
								if (idx >= 0)
								{
									return idx;
								}
							}
							// Nested TableViews can contribute inner rows; keep walking for ours.
						}
						node = VisualTreeHelper.GetParent(node);
					}
				}
			}
		}
		return -1;
	}

	private int GetEstimatedRowsPerPage()
	{
		if (m_bodyScroller is { } sv)
		{
			var vh = sv.ViewportHeight;

			// Sample realized visual children; item-index sampling fails after virtualization.
			double rowH = GetDensityRowMinHeight(); // Density is the best fallback before realized rows can be sampled.
			if (m_rowsRepeater is { } repeater)
			{
				// A grouped projection realizes group headers alongside data rows, and a header is
				// typically taller than a row. Paging moves the focused *data* row, so measure a data
				// row; only fall back to any realized element when no row has been realized yet.
				double anyChildH = 0.0;
				int childCount = VisualTreeHelper.GetChildrenCount(repeater);
				for (int i = 0; i < childCount; i++)
				{
					var child = VisualTreeHelper.GetChild(repeater, i);
					var el = child as FrameworkElement;
					if (el is null)
					{
						continue;
					}

					var h = el.ActualHeight;
					if (h <= 0)
					{
						continue;
					}

					if (child is TableViewRow)
					{
						anyChildH = h;
						break;
					}

					if (anyChildH <= 0)
					{
						anyChildH = h;
					}
				}

				if (anyChildH > 0)
				{
					rowH = anyChildH;
				}
			}

			if (vh > 0 && rowH > 0)
			{
				var pageRows = (int)(vh / rowH);
				return Math.Max(1, pageRows);
			}
		}
		return 10; // sensible fallback when template isn't realized yet.
	}
}
