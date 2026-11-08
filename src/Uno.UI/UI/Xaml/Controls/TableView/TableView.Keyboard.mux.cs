// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Keyboard.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
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

	private static Hyperlink? FindFirstHyperlinkDescendant(DependencyObject element, int depth)
	{
		int childCount = depth > 0 ? VisualTreeHelper.GetChildrenCount(element) : 0;
		for (int i = 0; i < childCount; ++i)
		{
			var child = VisualTreeHelper.GetChild(element, i);
			if (child is UIElement childElement && childElement.Visibility == Visibility.Visible)
			{
				if (FindHyperlink(childElement) is { } link)
				{
					return link;
				}
				if (FindFirstHyperlinkDescendant(child, depth - 1) is { } descendantLink)
				{
					return descendantLink;
				}
			}
		}
		return null;
	}

	private static DependencyObject? GetFocusedVisual(XamlRoot root)
	{
		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		if (focused is TextElement textElement)
		{
			return textElement.ContentStart?.VisualParent;
		}
		return focused;
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

	// Alt+Left/Right resizes the focused header using WPF DataGrid's binding; Shift is large-step.
	// Alt is the only resize modifier (dev-spec Keyboard); the gripper keeps pointer and keyboard on one clamp path.
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
	// The table is one tab stop on the header band (the body when no header is actionable); arrows
	// move between the bands, and both share visible-column coordinates.

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
			if (IsHeaderCellActionableInternal(cell))
			{
				// Drops a stamp made before the header's template content loaded.
				ClearHeaderTabStopStampInternal(cell);
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
		int after = FindActionableHeaderIndex(cells, preferred, 1);
		return after >= 0 ? after : FindActionableHeaderIndex(cells, preferred - 1, -1);
	}

	private int FindActionableHeaderIndex(
		List<FrameworkElement> cells, int visibleIndex, int step)
	{
		for (int i = visibleIndex; i >= 0 && i < cells.Count; i += step)
		{
			if (IsHeaderCellActionableInternal(cells[i]))
			{
				return i;
			}
		}
		return -1;
	}

	// Sortable (static configuration), resizable, has a context flyout or a tooltip, or hosts
	// focusable content. A style-set IsTabStop="False" still wins.
	internal bool IsHeaderCellActionableInternal(FrameworkElement? header)
	{
		if (header is null || header.Visibility != Visibility.Visible || !ShouldShowColumnHeaders())
		{
			return false;
		}

		var column = header.Tag as TableViewColumn;
		var control = header as Control;
		// Our own IsTabStop stamp is not the app's opt-out.
		if (!IsVisibleColumn(column) || (control is not null && (!control.IsEnabled ||
			(!control.IsTabStop && !HasHeaderTabStopStampInternal(header)))))
		{
			return false;
		}

		// Resizable only while the gripper is available: a pinned column cannot be resized.
		if (IsColumnUserSortableInternal(column!) || (IsColumnResizeAllowed(column) && !IsColumnResizePinned(column)) ||
			column!.HeaderContextFlyout is not null || ContextFlyout is not null || header.ContextFlyout is not null ||
			TableViewDetails.HasEffectiveToolTip(header))
		{
			return true;
		}


		// Only the content region: the sort glyph and resize gripper are chrome, not content.
		DependencyObject content = header;
		if (header is TableViewColumnHeader columnHeader)
		{
			if (columnHeader.GetContentPresenterInternal() is { } presenter)
			{
				content = presenter;
			}
		}
		int budget = 64;
		return HasActionableContent(content, 8, ref budget);
	}


	private FrameworkElement? FindOwnHeaderCell(DependencyObject? element)
	{
		var host = m_headerHost;
		var cell = element as FrameworkElement;
		if (host is null || cell is null || VisualTreeHelper.GetParent(cell) != host)
		{
			return null;
		}

		var column = cell.Tag as TableViewColumn;
		return column is not null && column.GetOwningTableView() == this ? cell : null;
	}

	// The table is one tab stop. Tab and Shift+Tab entry from outside land on the header row, else the
	// body. Non-actionable headers are not tab stops (RefreshHeaderTabStopsInternal).
	private void OnTableGettingFocus(
		object? sender,
		GettingFocusEventArgs args)
	{
		var newFocus = args.NewFocusedElement;
		if (newFocus is null || args.Cancel)
		{
			return;
		}

		if (args.FocusState != FocusState.Keyboard)
		{
			m_cellIntentColumn = -1;
		}

		var direction = args.Direction;
		bool isTab =
			direction == FocusNavigationDirection.Next ||
			direction == FocusNavigationDirection.Previous;
		if (!isTab && direction != FocusNavigationDirection.None && IsInUnrealizedBodyContainer(newFocus))
		{
			args.TryCancel();
			return;
		}
		// Popup-aware so an editor's dropdown counts as inside; a focused Hyperlink stands in as its text
		// host. Focus that was in this table and has left the live tree is XAML's rescue after a reset or
		// editor teardown.
		DependencyObject selfObject = this;
		var oldFocus = args.OldFocusedElement;
		if (oldFocus is TextElement textElement)
		{
			oldFocus = textElement.ContentStart?.VisualParent;
		}
		var oldElement = oldFocus as FrameworkElement;
		bool oldRemoved = oldElement is not null && (oldElement.XamlRoot is null || !oldElement.IsLoaded) &&
			oldFocus == m_lastFocusedWithin.Get();
		bool cameFromWithin = oldFocus is not null &&
			(oldRemoved || TableViewDetails.IsWithinElementPopupAware(oldFocus, selfObject, XamlRoot));

		// A nested TableView resolves its own moves; entry from outside still lands on this table's target.
		for (DependencyObject? node = newFocus; cameFromWithin && node is not null && node != selfObject; node = VisualTreeHelper.GetParent(node))
		{
			if (node is TableView)
			{
				return;
			}
		}

		if (!cameFromWithin)
		{
			RefreshHeaderTabStopsInternal();
		}

		if (oldRemoved && isTab)
		{
			if (!TrySetBodyEntryTarget(args))
			{
				QueueFocusTableEntry(newFocus, true /* bodyOnly */);
			}
			return;
		}

		// Tab entry, or app/window focus aimed at a header that is not (or no longer) a target.
		var header = FindOwnHeaderCell(newFocus);
		bool headerInactive = header is not null && !IsHeaderCellActionableInternal(header);
		bool isEntry = !cameFromWithin &&
			(isTab || (direction == FocusNavigationDirection.None && args.FocusState != FocusState.Pointer && headerInactive));
		if (isEntry && !HasRedirectedFocusEntry(args.CorrelationId) && !TryRedirectTableEntry(args))
		{
			// Window Tab-wrap and island entry cannot be redirected; focus moves on afterward.
			QueueFocusTableEntry(newFocus, false /* bodyOnly */);
		}
		else if (!isEntry && headerInactive && !args.TryCancel())
		{
			QueueFocusTableEntry(newFocus, false /* bodyOnly */);
		}
	}

	// Corrects an entry XAML would not let us redirect, unless focus has moved on since.
	private void QueueFocusTableEntry(DependencyObject expected, bool bodyOnly)
	{
		if (DispatcherQueue is { } queue)
		{
			WeakReference<TableView> weakThis = new(this);
			WeakReference<DependencyObject> weakExpected = new(expected);
			queue.TryEnqueue(() =>
			{
				var strongThis = weakThis.Get();
				var root = strongThis?.XamlRoot;
				if (root is null)
				{
					return;
				}
				try
				{
					var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
					if (focused is not null && focused != weakExpected.Get())
					{
						return;
					}
					var cells = GetVisibleHeaderCells(strongThis!.m_headerHost);
					int index = bodyOnly || !strongThis.ShouldShowColumnHeaders() ? -1 : strongThis.ResolveHeaderEntryIndex(cells);
					if (index < 0 || strongThis.FocusVisibleHeaderFrom(index, 0) < 0)
					{
						strongThis.FocusBodyEntryTarget(false /* select */);
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Moving focus to the table entry failed.");
				}
			});
		}
	}

	// Hidden headers hand focus to the body: XAML's rescue off the header is redirected, and a queued move
	// covers XAML not moving focus at all.
	private void MoveFocusOffCollapsingHeaders()
	{
		var root = XamlRoot;
		var queue = DispatcherQueue;
		if (root is null || queue is null)
		{
			return;
		}

		// Focus may be on content inside the header (a Button, CheckBox or inline Hyperlink in its template).
		if (ResolveFocusedHeaderColumn(GetFocusedVisual(root), m_headerHost, out var headerCell) is null || headerCell is null)
		{
			return;
		}

		DependencyObject header = headerCell;
		var focused = FocusManager.GetFocusedElement(root) as UIElement;
		var focusState = focused is not null ? focused.FocusState : FocusState.Programmatic;
		Func<DependencyObject?, DependencyObject?, bool> isWithinHeader = static (element, originHeader) =>
		{
			if (element is TextElement textElement)
			{
				element = textElement.ContentStart?.VisualParent;
			}
			return element is not null && originHeader is not null &&
				(element == originHeader || SharedHelpers.IsAncestor(element, originHeader, false /* checkVisibility */));
		};
		WeakReference<TableView> weakThis = new(this);
		WeakReference<DependencyObject> weakHeader = new(header);
		EventHandler<GettingFocusEventArgs> gettingFocusHandler = (_, args) =>
		{
			// One-shot: only the first move off the hidden header is XAML's rescue.
			if (weakThis.Get() is { } strongThis)
			{
				var originHeader = weakHeader.Get();
				strongThis.m_headerCollapseFocusRevoker.Disposable = null;
				try
				{
					var oldFocus = args.OldFocusedElement;
					if ((oldFocus is null || isWithinHeader(oldFocus, originHeader)) && !strongThis.IsElementInBody(args.NewFocusedElement))
					{
						strongThis.TrySetBodyEntryTarget(args);
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Redirecting focus off hidden headers failed.");
				}
			}
		};
		FocusManager.GettingFocus += gettingFocusHandler;
		m_headerCollapseFocusRevoker.Disposable = Disposable.Create(() => FocusManager.GettingFocus -= gettingFocusHandler);
		queue.TryEnqueue(() =>
		{
			var strongThis = weakThis.Get();
			if (strongThis is null)
			{
				return;
			}
			strongThis.m_headerCollapseFocusRevoker.Disposable = null;
			var root = strongThis.XamlRoot;
			if (root is null)
			{
				return;
			}
			try
			{
				var now = FocusManager.GetFocusedElement(root) as DependencyObject;
				if (!strongThis.ShouldShowColumnHeaders() && (now is null || isWithinHeader(now, weakHeader.Get())))
				{
					strongThis.FocusBodyEntryTarget(false /* select */, focusState);
				}
			}
			catch (Exception)
			{
				TVDiag.LogRetailF("[TableView] Moving focus off hidden headers failed.");
			}
		});
	}
	// Our stamp is a local false; any other local value or binding there now is the app's.
	internal bool HasHeaderTabStopStampInternal(FrameworkElement? header)
	{
		if (header is not TableViewColumnHeader headerImpl || !headerImpl.OwnsTabStopStampInternal())
		{
			return false;
		}

		if (header.ReadLocalValue(UIElement.IsTabStopProperty) is bool local && !local)
		{
			return true;
		}
		headerImpl.SetOwnsTabStopStampInternal(false);
		return false;
	}

	internal void ClearHeaderTabStopStampInternal(FrameworkElement? header)
	{
		if (HasHeaderTabStopStampInternal(header))
		{
			header!.ClearValue(UIElement.IsTabStopProperty);
			((TableViewColumnHeader)header).SetOwnsTabStopStampInternal(false);
		}
	}

	// Inactive headers are taken out of the tab order like inactive cells. Only our own stamp is cleared,
	// so a style-set or app-local IsTabStop is kept.
	internal void RefreshHeaderTabStopsInternal()
	{
		DependencyObject? focusedInactive = null;
		foreach (var cell in GetVisibleHeaderCells(m_headerHost))
		{
			if (cell is not TableViewColumnHeader header)
			{
				continue;
			}
			bool stamped = HasHeaderTabStopStampInternal(cell);
			bool actionable = IsHeaderCellActionableInternal(cell);
			if (actionable && stamped)
			{
				ClearHeaderTabStopStampInternal(cell);
			}
			else if (!actionable && !stamped && header.IsTabStop &&
				header.ReadLocalValue(UIElement.IsTabStopProperty) == DependencyProperty.UnsetValue)
			{
				header.IsTabStop = false;
				header.SetOwnsTabStopStampInternal(true);
			}
			if (!actionable && header.FocusState != FocusState.Unfocused)
			{
				focusedInactive = header;
			}
		}

		// A focused header that stopped being actionable hands focus to the nearest actionable one, else the body.
		if (focusedInactive is not null && DispatcherQueue is { } queue)
		{
			WeakReference<TableView> weakThis = new(this);
			WeakReference<DependencyObject> weakHeader = new(focusedInactive);
			queue.TryEnqueue(() =>
			{
				var strongThis = weakThis.Get();
				var root = strongThis?.XamlRoot;
				var header = weakHeader.Get();
				try
				{
					if (root is null || header is null || FocusManager.GetFocusedElement(root) != header)
					{
						return;
					}
					var cells = GetVisibleHeaderCells(strongThis!.m_headerHost);
					int index = IndexOfHeaderCell(cells, (FrameworkElement)header);
					if (index < 0 || (strongThis.FocusVisibleHeaderFrom(index, 1) < 0 && strongThis.FocusVisibleHeaderFrom(index, -1) < 0))
					{
						strongThis.FocusBodyEntryTarget(false /* select */);
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Moving focus off an inactive header failed.");
				}
			});
		}
	}
	private bool TryRedirectTableEntry(GettingFocusEventArgs args)
	{
		if (ShouldShowColumnHeaders())
		{
			var cells = GetVisibleHeaderCells(m_headerHost);
			if (ResolveHeaderEntryIndex(cells) is var index && index >= 0)
			{
				DependencyObject target = cells[index];
				if (target == args.NewFocusedElement || args.TrySetNewFocusedElement(target))
				{
					NoteFocusEntryRedirect(args.CorrelationId);
					return true;
				}
				return false;
			}
		}

		return TrySetBodyEntryTarget(args);
	}

	private bool TrySetBodyEntryTarget(GettingFocusEventArgs args)
	{
		DependencyObject selfObject = this;
		Func<FocusEntryTarget, bool> trySet = entry =>
		{
			if (entry.Element is null)
			{
				return false;
			}

			// Only one level of a row is focusable at a time; arm the target's level first. A cell-level target
			// also takes ownership of the armed row so later row-level cleanup disarms the right row.
			var rowImpl = entry.Row;
			bool wasCellLevel = rowImpl is not null && rowImpl.IsCellLevelInternal();
			var previousOwner = m_cellLevelRow.Get();
			bool previousCursorActive = m_cellCursorActive;
			if (rowImpl is not null)
			{
				if (entry.CellLevel)
				{
					ArmCellLevelRowInternal(entry.Row);
				}
				else
				{
					rowImpl.SetCellLevelInternal(false);
				}
			}

			DependencyObject target = entry.Element;
			if (target == args.NewFocusedElement ||
				(IsEntryRedirectTargetFocusable(entry.Element, selfObject) && args.TrySetNewFocusedElement(target)))
			{
				NoteFocusEntryRedirect(args.CorrelationId);
				if (!entry.CellLevel)
				{
					SetCellCursorActiveInternal(false);
				}
				return true;
			}

			if (rowImpl is not null)
			{
				rowImpl.SetCellLevelInternal(wasCellLevel);
				if (entry.CellLevel)
				{
					if (previousOwner is not null && previousOwner != entry.Row)
					{
						previousOwner.SetCellLevelInternal(true);
					}
					m_cellLevelRow = previousOwner is not null ? new WeakReference<TableViewRow>(previousOwner) : null;
					m_cellCursorActive = previousCursorActive;
				}
			}
			return false;
		};

		bool previous = args.Direction == FocusNavigationDirection.Previous;
		if (trySet(ResolveBodyEntryTarget(previous)))
		{
			return true;
		}

		FocusEntryTarget edge = new();
		edge.Element = FindViewportEdgeElementInternal(previous, true /* includeGroupHeaders */, true /* allowNoViewport */);
		edge.Row = edge.Element as TableViewRow;
		return trySet(edge);
	}

	// The empty-state content when it is shown; else the selected row in view if it was selected after
	// the remembered row was recorded (by the app or a right-click); else the remembered row (or its
	// actionable remembered cell) in view; else the selected row in view; else the first (last) row or
	// group header in view.
	private FocusEntryTarget ResolveBodyEntryTarget(bool last)
	{
		FocusEntryTarget result = new();
		if (m_emptyStatePresenter is { } emptyState && emptyState.Visibility == Visibility.Visible)
		{
			result.Element = last ? FocusManager.FindLastFocusableElement(emptyState)
				: FocusManager.FindFirstFocusableElement(emptyState);
			return result;
		}

		Func<TableViewRow?, bool> usable = row =>
		{
			return row is not null && row.IsEnabled && IsRowElementInViewport(row);
		};

		TableViewRow? remembered = null;
		if ((m_currentCellRow >= 0 ? m_currentItem : null) is { } rememberedItem)
		{
			remembered =
				TryGetItemAtRowIndex(m_currentCellRow, out var itemAtRow) && SameInspectableIdentity(itemAtRow, rememberedItem)
				? GetRealizedRowAt(m_currentCellRow)
				: FindRealizedRowForItem(rememberedItem);
		}

		var selected = GetRealizedRowAt(SelectedIndexInternal());
		bool selectionIsNewer = m_selectionSequence != m_rememberedSelectionSequence;
		result.Row = selectionIsNewer && usable(selected) ? selected : usable(remembered) ? remembered : usable(selected) ? selected : null;
		result.Remembered = result.Row is not null && result.Row == remembered;

		if (result.Row is not null)
		{
			var rowImpl = result.Row;
			int cellCount = rowImpl.GetVisibleCellCountInternal();
			var cell = m_cellCursorActive && cellCount > 0
				? rowImpl.GetVisibleCellInternal(Math.Clamp(m_currentCellColumn, 0, cellCount - 1))
				: null;
			result.CellLevel = cell is not null && rowImpl.IsCellActionableInternal(cell);
			result.Element = result.CellLevel ? cell : result.Row;
			return result;
		}

		// A group header first (last) in view is an entry target like a row.
		result.Element = FindViewportEdgeElementInternal(last, true /* includeGroupHeaders */, true /* allowNoViewport */);
		result.Row = result.Element as TableViewRow;
		return result;
	}

	// Down from the header row (selection follows focus unless Ctrl is held), or a deferred entry.
	private bool FocusBodyEntryTarget(bool select, FocusState focusState = FocusState.Keyboard)
	{
		// A newer selection scrolled out of view is brought back rather than replaced.
		int selectedIndex = SelectedIndexInternal();
		if (select && selectedIndex >= 0 && m_selectionSequence != m_rememberedSelectionSequence)
		{
			if (GetRealizedRowAt(selectedIndex) is var selected && (selected is null || !IsRowElementInViewport(selected)))
			{
				var root = XamlRoot;
				var origin = root is not null ? FocusManager.GetFocusedElement(root) as DependencyObject : null;
				bool moved = FocusRow(selectedIndex);
				if (moved && origin is not null && m_pendingReshapeFocusRow >= 0)
				{
					m_pendingBodyFocusOrigin = new WeakReference<DependencyObject>(origin);
				}
				return moved;
			}
		}

		var entry = ResolveBodyEntryTarget(false);
		if (entry.Element is null)
		{
			return false;
		}

		if (entry.Row is null)
		{
			if (entry.Element is Hyperlink link)
			{
				return link.Focus(focusState);
			}
			var element = entry.Element as UIElement;
			return element is not null && (element is TableViewGroupHeader
				? FocusRowContainerInternal(element, focusState)
				: element.Focus(focusState));
		}

		// An edge row standing in at cell level takes the actionable cell nearest the column.
		bool edgeAtCell = m_cellCursorActive && !entry.Remembered && entry.Row != GetRealizedRowAt(SelectedIndexInternal());
		if (!(entry.CellLevel || edgeAtCell ? FocusRealizedRowCell(entry.Row, m_currentCellColumn, focusState) : FocusRowContainerInternal(entry.Row, focusState)))
		{
			return false;
		}

		// An edge row stands in for a selection scrolled out of view; it does not take the selection.
		if (m_rowsRepeater is { } repeater && select && (entry.Remembered || SelectedIndexInternal() < 0))
		{
			SelectRowIndexFromKeyboardFocus(repeater.GetElementIndex(entry.Row));
		}
		return true;
	}
	// Editable means F2 opens an editor: a text column needs a Binding to generate one.
	internal bool IsColumnCellEditableInternal(TableViewColumn? column)
	{
		return column is not null && !IsReadOnly && !column.IsReadOnly &&
			(column.CellEditingTemplate is not null || (column is TableViewTextColumn textColumn && textColumn.Binding is not null));
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
		bool isHome = key == VirtualKey.Home;
		bool isEnd = key == VirtualKey.End;
		if (!isLeft && !isRight && !isHome && !isEnd)
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

		// Home/End reach the first/last actionable header and the scroll edge, never row navigation.
		if (isHome || isEnd)
		{
			int target = FindActionableHeaderIndex(cells, isHome ? 0 : count - 1, isHome ? 1 : -1);
			if (target >= 0 && target != liveIndex)
			{
				FocusVisibleHeaderFrom(target, 0);
			}
			ScrollBodyHorizontallyInternal(isEnd, true /* toEdge */, target >= 0 ? cells[target] : null);
			args.Handled = true;
			return true;
		}

		// RTL mirrors the cell move: Right means "towards the row end" in reading order.
		bool isRtl = FlowDirection == FlowDirection.RightToLeft;
		bool forward = isRight != isRtl;
		int step = forward ? 1 : -1;
		int targetIndex = Math.Clamp(currentIndex + step, 0, count - 1);

		// Past the last (first) actionable header the row scrolls on, so the remaining columns stay reachable.
		if ((liveIndex == targetIndex || FocusVisibleHeaderFrom(targetIndex, targetIndex == currentIndex ? 0 : step) < 0) &&
			liveIndex == currentIndex)
		{
			ScrollBodyHorizontallyInternal(forward);
		}

		// Consume bounds/refusals so directional focus navigation cannot walk out of the table.
		args.Handled = true;
		return true;
	}

	// Up stays in the header band; Down leaves it for the body. Either way the key is consumed, so
	// focus navigation cannot escape the band sideways or land mid-table.
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

		// The bands share one column cursor, so the body is entered in the header's column.
		if (key == VirtualKey.Down)
		{
			SetColumnCursorInternal(headerIndex);
			FocusBodyEntryTarget(!IsKeyDown(VirtualKey.Control));
		}

		args.Handled = true;
		return true;
	}

	private bool TryMoveFocusToHeaderBandFromBody(int visibleColumnIndex)
	{
		var host = m_headerHost;
		if (host is null)
		{
			return false;
		}

		var cells = GetVisibleHeaderCells(host);
		if (cells.Count == 0)
		{
			return false;
		}

		// Publish first so ResolveHeaderEntryIndex aims at it; negative (row level) keeps the old cursor.
		SetColumnCursorInternal(visibleColumnIndex);

		int target = ResolveHeaderEntryIndex(cells);
		if (target < 0)
		{
			return false;
		}

		return FocusVisibleHeaderFrom(target, 0) >= 0;
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
		m_navKeyHandledEarly = false;
		var pressed = args.Key;
		bool isModifier = pressed == VirtualKey.Shift || pressed == VirtualKey.Control || pressed == VirtualKey.Menu ||
			pressed == VirtualKey.LeftWindows || pressed == VirtualKey.RightWindows ||
			pressed == VirtualKey.LeftShift || pressed == VirtualKey.RightShift ||
			pressed == VirtualKey.LeftControl || pressed == VirtualKey.RightControl ||
			pressed == VirtualKey.LeftMenu || pressed == VirtualKey.RightMenu;
		if (!isModifier && pressed != VirtualKey.Up && pressed != VirtualKey.Down)
		{
			m_cellIntentColumn = -1;
		}
		if (!isModifier && pressed != VirtualKey.Left && pressed != VirtualKey.Right)
		{
			m_drillInOffset = -1.0;
		}
		if (!isModifier)
		{
			m_drillRestoreOffset = -1.0;
		}

		// A keyboard move still scrolling to its row is the newest intent, not a stale restore: a
		// repeated key pages on from that row, and any other key leaves the scroll to finish.
		int pendingKeyboardRow = -1;
		if (IsKeyboardBodyFocusPending())
		{
			pendingKeyboardRow = m_pendingReshapeFocusRow;
			if (m_pendingBodyFocusItem is not null && !IsPendingBodyFocusItemAt(pendingKeyboardRow))
			{
				pendingKeyboardRow = FindEntryIndexForDataItem(m_pendingBodyFocusItem);
			}
		}

		switch (args.Key)
		{
			case VirtualKey.Shift:
			case VirtualKey.Control:
			case VirtualKey.Menu:
			case VirtualKey.LeftWindows:
			case VirtualKey.RightWindows:
				break;
			default:
				if (!IsKeyboardBodyFocusPending())
				{
					CancelReshapeFocusRestoreForUserInput();
				}
				break;
		}

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

		// Live focus can still be on the row a pending keyboard move is leaving; anchor at its target.
		switch (args.Key)
		{
			case VirtualKey.Up:
			case VirtualKey.Down:
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
				if (pendingKeyboardRow >= 0 && !IsBodyFocusAtRow(pendingKeyboardRow))
				{
					m_navAnchorRow = pendingKeyboardRow;
					m_navAnchorCellRow = m_pendingReshapeFocusCellLevel ? pendingKeyboardRow : -1;
					m_navAnchorCellColumn = m_pendingReshapeFocusCellLevel ? m_pendingReshapeFocusColumn : -1;
					m_navAnchorRowContainer = -1;
					m_navAnchorGroupHeader = -1;
				}
				break;
			default:
				break;
		}

		// PART_BodyScroller would otherwise page on its own before our handledEventsToo KeyDown runs.
		if ((args.Key == VirtualKey.PageUp ||
				args.Key == VirtualKey.PageDown) &&
			!args.Handled &&
			!IsFocusInsideEditingElement() &&
			IsFocusExactlyOnBodyItem())
		{
			args.Handled = true;
		}
	}

	// Shared by the Preview and bubbling navigation handlers so both agree on who owns a paging key.
	private bool IsFocusInsideEditingElement()
	{
		if (!IsEditing)
		{
			return false;
		}

		var editingElement = CurrentEditingElement();
		var root = XamlRoot;
		if (editingElement is null || root is null)
		{
			return false;
		}

		// Popup-aware: an open ComboBox editor's dropdown items are not visual descendants of the editor.
		return TableViewDetails.IsWithinElementPopupAware(FocusManager.GetFocusedElement(root), editingElement, root);
	}

	private void OnKeyDownForNavigation(
		object? sender,
		KeyRoutedEventArgs args)
	{
		// While editing, let the editor own keys; otherwise PageUp/PageDown can recycle the edited row
		// mid-edit. WPF DataGrid does the same.
		if (IsFocusInsideEditingElement())
		{
			return;
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
		// hosted controls.
		if (args.Handled && !IsFocusExactlyOnBodyItem())
		{
			return;
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
			!IsKeyDown(VirtualKey.Menu);
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
			double? initialAlign = null;
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
					initialRow = GetPageTargetRow(0, true /* pageDown */, rowCount);
					initialAlign = 1.0;
					break;
				default:
					break;
			}
			if (initialRow >= 0 && FocusRowContainer(initialRow, initialAlign))
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
		// Absolute/page keys are consumed at boundaries; cell-level Up/Down may escape the table.
		bool absoluteRowNav = false;
		double? alignRatio = null;

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
				newRow = GetPageTargetRow(currentRow, false /* pageDown */, rowCount);
				alignRatio = 0.0;
				absoluteRowNav = true;
				break;
			case VirtualKey.PageDown:
				newRow = GetPageTargetRow(currentRow, true /* pageDown */, rowCount);
				alignRatio = 1.0;
				absoluteRowNav = true;
				break;
			default:
				consumeKey = false;
				break;
		}

		if (consumeKey)
		{
			// A cell-level Up/Down that landed on a neighbor cell or on the row keeps its column.
			bool hasIntent = m_cellIntentColumn >= 0 && m_cellIntentRow == currentRow &&
				(m_navAnchorCellRow == m_navAnchorRow || m_navAnchorRowContainer == m_navAnchorRow);
			bool hasCellAnchor = hasIntent ||
				(m_navAnchorRow >= 0 &&
				m_navAnchorCellRow == m_navAnchorRow &&
				m_navAnchorCellColumn >= 0);
			int anchorColumn = hasIntent ? m_cellIntentColumn : hasCellAnchor ? m_navAnchorCellColumn : m_currentCellColumn;
			m_cellIntentColumn = -1;
			if (newRow != currentRow)
			{
				// Up/Down preserve the starting level per ARIA treegrid: row-to-row or same-column
				// cell-to-cell.
				bool moved = hasCellAnchor
					? FocusCell(newRow, anchorColumn, alignRatio)
					: FocusRowContainer(newRow, alignRatio);
				// Row-level Up/Down steps over rows that refuse focus (disabled), as ListView does.
				if (!moved && !hasCellAnchor && !absoluteRowNav)
				{
					int step = newRow > currentRow ? 1 : -1;
					for (int next = newRow + step; next >= 0 && next < rowCount; next += step)
					{
						if (FocusRowContainer(next, alignRatio))
						{
							newRow = next;
							moved = true;
							break;
						}
					}
				}
				if (moved)
				{
					if (hasCellAnchor && (key == VirtualKey.Up || key == VirtualKey.Down))
					{
						m_cellIntentColumn = anchorColumn;
						m_cellIntentRow = newRow;
						SetColumnCursorInternal(anchorColumn);
					}
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
			else if (key == VirtualKey.Up &&
				currentRow == 0 &&
				TryMoveFocusToHeaderBandFromBody(hasCellAnchor ? anchorColumn : -1))
			{
				args.Handled = true;
			}

			// Unhandled, the body scroller would focus a control inside this row's cells.
			if (!args.Handled && m_navKeyHandledEarly && !hasCellAnchor &&
				(key == VirtualKey.Up || key == VirtualKey.Down))
			{
				args.Handled = true;
			}
		}
	}

	// Rows forward Left/Right (and row-level Up/Down) before PART_BodyScroller acts on them; the
	// handledEventsToo KeyDown then skips the key, since hierarchy Left/Right is not idempotent.
	internal void HandleBodyKeyFromRowInternal(KeyRoutedEventArgs args)
	{
		m_navKeyHandledEarly = true;
		OnKeyDownForNavigation(null, args);
	}

	internal bool FocusRow(int index)
	{
		// Follow the current cursor level: row-level callers land on rows; drilled-in callers stay on cells.
		return m_cellCursorActive
			? FocusCell(index, m_currentCellColumn)
			: FocusRowContainer(index);
	}

	// ----- Re-shape focus restore (dev-spec Keyboard, "Re-shape while focused") -----
	//
	// A sort or filter keeps body focus at the same projected row index, whichever record now occupies
	// it. This deliberately differs from Tab re-entry (ResolveBodyEntryTarget), which follows the item.
	// Restore only if focus was in the body or dropped to null; focus elsewhere is never taken back.
	private void CaptureBodyFocusForReshape()
	{
		if (m_reshapeFocusValid)
		{
			// Both entry points fire inside one re-shape; the first capture wins.
			return;
		}

		m_reshapeFocusRow = -1;
		m_reshapeFocusColumn = m_currentCellColumn;
		m_reshapeFocusCellLevel = false;

		var root = XamlRoot;
		if (root is null)
		{
			return;
		}

		var focusedElement = FocusManager.GetFocusedElement(root);
		var focused = focusedElement as DependencyObject;

		FocusState focusState = FocusState.Keyboard;

		if (TryResolveLiveBodyFocusPosition(out int row, out int column, out bool cellLevel))
		{
			focusState = GetRestoreFocusStateFor(focusedElement);
		}
		else
		{
			// Focus orphaned in a recycled body container counts as dropped and uses the remembered
			// position.
			if (focused is not null && !IsElementInBody(focused))
			{
				return;
			}

			if (!m_lastBodyFocusValid || m_lastBodyFocusRow < 0)
			{
				return;
			}

			row = m_lastBodyFocusRow;
			column = m_lastBodyFocusColumn;
			cellLevel = m_lastBodyFocusCellLevel;
			focusState = m_lastBodyFocusState;
		}

		m_reshapeFocusRow = row;
		m_reshapeFocusColumn = column;
		m_reshapeFocusCellLevel = cellLevel;
		m_reshapeFocusState = focusState;
		m_reshapeFocusValid = true;

		QueueReshapeFocusCaptureDisarm();
	}

	private bool IsElementInBody(DependencyObject? element)
	{
		if (element is null)
		{
			return false;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return false;
		}

		DependencyObject repeaterObject = repeater;
		return element == repeaterObject ||
			SharedHelpers.IsAncestor(element, repeaterObject, false /* checkVisibility */);
	}

	// Null focus is deliberately false: that is what a re-shape leaves behind, not a user move.
	private bool IsFocusOnNonBodyElement()
	{
		var root = XamlRoot;
		if (root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		return focused is not null && !IsElementInBody(focused);
	}

	private void ForgetBodyFocusPosition()
	{
		m_lastBodyFocusValid = false;
		m_lastBodyFocusRow = -1;
		m_lastBodyFocusState = FocusState.Keyboard;
	}

	// Pointer/app-placed focus is replayed as Programmatic so a restore never paints keyboard visuals.
	private static FocusState GetRestoreFocusStateFor(object? focusedElement)
	{
		if (focusedElement is Control control)
		{
			switch (control.FocusState)
			{
				case FocusState.Pointer:
				case FocusState.Programmatic:
					return FocusState.Programmatic;
				default:
					break;
			}
		}

		return FocusState.Keyboard;
	}

	private bool TryResolveLiveBodyFocusPosition(out int row, out int column, out bool cellLevel)
	{
		row = -1;
		column = m_currentCellColumn;
		cellLevel = false;

		var repeater = m_rowsRepeater;
		var root = XamlRoot;
		if (repeater is null || root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		if (focused is null)
		{
			return false;
		}

		DependencyObject repeaterObject = repeater;
		if (focused != repeaterObject &&
			!SharedHelpers.IsAncestor(focused, repeaterObject, false /* checkVisibility */))
		{
			return false;
		}

		// Non-exact: focus may be on hosted content inside the cell, which is still cell level.
		bool onCell = TryGetFocusedCell(out var cellRow, out var cellColumn, false /* requireExactCell */);

		// GetFocusedRowIndex also recognizes group headers, which occupy projected positions too.
		int resolvedRow = onCell ? cellRow : GetFocusedRowIndex();
		if (resolvedRow < 0)
		{
			return false;
		}

		row = resolvedRow;
		column = (onCell && cellColumn >= 0) ? cellColumn : m_currentCellColumn;
		cellLevel = onCell;
		return true;
	}

	// Tracked continuously because ItemsRepeater can recycle the focused row before any re-shape
	// notification reaches us.
	private void TrackBodyFocusPosition()
	{
		if (m_suppressBodyFocusTracking)
		{
			// Inside a re-shape: ItemsRepeater's focus rescue (MoveFocusFromClearedIndex) must not
			// overwrite the user's position.
			return;
		}

		if (TryResolveLiveBodyFocusPosition(out var row, out var column, out var cellLevel))
		{
			m_lastBodyFocusValid = true;
			m_lastBodyFocusRow = row;
			m_lastBodyFocusColumn = column;
			m_lastBodyFocusCellLevel = cellLevel;
			if (XamlRoot is { } root)
			{
				m_lastBodyFocusState = GetRestoreFocusStateFor(FocusManager.GetFocusedElement(root));
			}
			return;
		}

		if (IsFocusOnNonBodyElement())
		{
			ForgetBodyFocusPosition();
		}
	}

	private void OnTableViewGotFocusForBodyTracking(
		object? sender,
		RoutedEventArgs args)
	{
		if (args.OriginalSource is DependencyObject source)
		{
			m_lastFocusedWithin = new WeakReference<DependencyObject>(source);
		}
		TrackBodyFocusPosition();
	}

	// The remembered body position survives only focus being dropped to null; focus landing on any
	// element outside the body discards it, however it got there.
	private void OnTableViewLosingFocusForBodyTracking(
		object? sender,
		LosingFocusEventArgs args)
	{
		if (m_suppressBodyFocusTracking)
		{
			// The re-shape's own focus rescue; see TrackBodyFocusPosition.
			return;
		}

		var newFocus = args.NewFocusedElement;

		if (newFocus is null)
		{
			return;
		}

		if (IsElementInBody(newFocus))
		{
			// GotFocus records the new position.
			return;
		}

		ForgetBodyFocusPosition();
	}

	private void SuppressBodyFocusTrackingForThisTurn()
	{
		if (m_suppressBodyFocusTracking)
		{
			return;
		}

		m_suppressBodyFocusTracking = true;

		var queue = DispatcherQueue;
		if (queue is null)
		{
			m_suppressBodyFocusTracking = false;
			return;
		}

		WeakReference<TableView> weakThis = new(this);
		if (!queue.TryEnqueue(() =>
			{
				if (weakThis.Get() is { } strongThis)
				{
					strongThis.m_suppressBodyFocusTracking = false;
				}
			}))
		{
			m_suppressBodyFocusTracking = false;
		}
	}

	// An app can raise Reset on its own collection, so a capture not claimed by a restore in the same
	// turn is dropped rather than left for an unrelated later re-shape to replay.
	private void QueueReshapeFocusCaptureDisarm()
	{
		if (m_reshapeFocusDisarmQueued)
		{
			return;
		}

		var queue = DispatcherQueue;
		if (queue is null)
		{
			return;
		}

		m_reshapeFocusDisarmQueued = true;
		WeakReference<TableView> weakThis = new(this);
		if (!queue.TryEnqueue(() =>
			{
				if (weakThis.Get() is { } strongThis)
				{
					strongThis.m_reshapeFocusDisarmQueued = false;
					strongThis.m_reshapeFocusValid = false;
					strongThis.m_reshapeFocusRow = -1;
				}
			}))
		{
			m_reshapeFocusDisarmQueued = false;
		}
	}

	private void RestoreBodyFocusAfterReshape()
	{
		if (!m_reshapeFocusValid)
		{
			return;
		}

		// Consume synchronously so the queued disarm cannot cancel a claimed restore.
		int row = m_reshapeFocusRow;
		m_pendingReshapeFocusColumn = m_reshapeFocusColumn;
		m_pendingReshapeFocusCellLevel = m_reshapeFocusCellLevel;
		m_pendingReshapeFocusState = m_reshapeFocusState;

		m_reshapeFocusValid = false;
		m_reshapeFocusRow = -1;

		if (row < 0)
		{
			return;
		}

		m_pendingReshapeFocusRow = row;
		m_pendingReshapeFocusAlign = null;
		m_pendingBodyFocusItem = null;
		m_pendingBodyFocusFromKeyboard = false;
		m_pendingBodyFocusOrigin = null;

		m_reshapeFocusArmedThisTurn = true;
		if (DispatcherQueue is { } queue)
		{
			WeakReference<TableView> weakThis = new(this);
			if (!queue.TryEnqueue(() =>
				{
					if (weakThis.Get() is { } strongThis)
					{
						strongThis.m_reshapeFocusArmedThisTurn = false;
					}
				}))
			{
				m_reshapeFocusArmedThisTurn = false;
			}
		}
		else
		{
			m_reshapeFocusArmedThisTurn = false;
		}
		if (m_pendingReshapeFocusCellLevel)
		{
			SetColumnCursorInternal(m_pendingReshapeFocusColumn);
		}

		// Budgeted retry: ItemsRepeater is still unwinding its Reset, so nothing is realized at the
		// index yet, and deferred re-shape work can drop focus again after a single attempt.
		m_reshapeFocusAttemptsLeft = s_reshapeFocusRestoreAttempts;
		ArmReshapeFocusAttempt();
	}

	// Attempts run per layout pass AND per dispatcher turn: layout changes whether the target is
	// arranged, but only a turn outside layout may force realization (ItemsRepeater refuses
	// GetOrCreateElement during layout). Both spend the same budget.
	private void ArmReshapeFocusAttempt()
	{
		if (m_reshapeFocusLayoutToken.Disposable is not null)
		{
			m_reshapeFocusLayoutToken.Disposable = null;
		}

		if (m_pendingReshapeFocusRow < 0)
		{
			return;
		}

		WeakReference<TableView> weakThis = new(this);
		EventHandler<object> handler = (_, _) =>
		{
			if (weakThis.Get() is { } strongThis)
			{
				strongThis.OnReshapeFocusAttempt();
			}
		};
		LayoutUpdated += handler;
		m_reshapeFocusLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= handler);

		// A body scroll in flight lands through layout; spinning dispatcher turns would only burn the
		// attempt budget before it applies.
		if (m_reshapeFocusTurnQueued || m_reshapeFocusWaitForLayout)
		{
			return;
		}

		if (DispatcherQueue is { } queue)
		{
			m_reshapeFocusTurnQueued = true;
			if (!queue.TryEnqueue(() =>
				{
					if (weakThis.Get() is { } strongThis)
					{
						strongThis.m_reshapeFocusTurnQueued = false;
						try
						{
							strongThis.OnReshapeFocusAttempt();
						}
						catch (Exception)
						{
							// Best-effort; never fail-fast the dispatcher.
						}
					}
				}))
			{
				m_reshapeFocusTurnQueued = false;
			}
		}
	}

	private void CancelReshapeFocusRestore()
	{
		m_pendingReshapeFocusRow = -1;
		m_pendingBodyFocusItem = null;
		m_pendingBodyFocusFromKeyboard = false;
		m_pendingBodyFocusOrigin = null;
		m_requestedBodyVerticalOffset = null;
		m_reshapeFocusWaitForLayout = false;
		m_reshapeFocusAttemptsLeft = 0;
		if (m_reshapeFocusLayoutToken.Disposable is not null)
		{
			m_reshapeFocusLayoutToken.Disposable = null;
		}
	}

	// A restore re-aims at its row until it confirms, which is right against the repeater's own focus
	// rescue but wrong against the user: a key or pointer press during that window is a newer intent,
	// even when it lands on another body row (which the non-body check in OnReshapeFocusAttempt misses).
	// Input in the turn that armed the restore is exempt: that input is what triggered the re-shape
	// (a page PreviewKeyDown tunnels ahead of ours; a press-mode control inside the table bubbles first).
	private void CancelReshapeFocusRestoreForUserInput()
	{
		if (m_pendingReshapeFocusRow >= 0 && !m_reshapeFocusArmedThisTurn)
		{
			CancelReshapeFocusRestore();
		}
	}

	private void OnReshapeFocusAttempt()
	{
		if (m_reshapeFocusLayoutToken.Disposable is not null)
		{
			m_reshapeFocusLayoutToken.Disposable = null;
		}

		if (m_pendingReshapeFocusRow < 0)
		{
			return;
		}

		if (m_reshapeFocusAttemptsLeft <= 0)
		{
			TVDiag.DbgLogF(
				"TableView: re-shape focus restore gave up at row %d.", m_pendingReshapeFocusRow);
			CancelReshapeFocusRestore();
			return;
		}
		--m_reshapeFocusAttemptsLeft;

		// Checked before every aim: focus the user or app moved outside the body is never pulled back.
		var origin = m_pendingBodyFocusOrigin.Get();
		var root = origin is not null ? XamlRoot : null;
		bool onOrigin = root is not null && (FocusManager.GetFocusedElement(root) as DependencyObject) == origin;
		if (!onOrigin)
		{
			// One-way: once focus has left the origin, returning to it is a new user move.
			m_pendingBodyFocusOrigin = null;
		}
		if (!onOrigin && IsFocusOnNonBodyElement())
		{
			CancelReshapeFocusRestore();
			return;
		}

		bool landed = false;
		try
		{
			// Layout callbacks fail-fast on escaping exceptions.
			landed = TryLandReshapeFocus();
		}
		catch (Exception)
		{
			// Best-effort: the projection can move under a settling restore.
		}

		if (landed)
		{
			CancelReshapeFocusRestore();
			return;
		}

		ArmReshapeFocusAttempt();
	}

	// Success (focus at the row and the row in view) is confirmed on the next attempt, not inferred
	// from Focus(), so a landing that later re-shape work wipes or scrolls away is re-aimed.
	private bool TryLandReshapeFocus()
	{
		int rowCount = GetItemsSourceCount();
		if (rowCount <= 0)
		{
			return true;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return true;
		}

		if (m_pendingBodyFocusItem is not null)
		{
			// Add/Remove/Move above the target shift indices without a Reset; follow the item, and
			// give up once it leaves the projection.
			if (!IsPendingBodyFocusItemAt(m_pendingReshapeFocusRow))
			{
				int resolved = FindEntryIndexForDataItem(m_pendingBodyFocusItem);
				if (resolved < 0)
				{
					return true;
				}
				m_pendingReshapeFocusRow = resolved;
			}
		}

		// A filter can shorten the projection past the captured position; clamp to stay in the body.
		int target = Math.Clamp(m_pendingReshapeFocusRow, 0, rowCount - 1);

		// TryGetElement first: GetOrCreateElement can recycle the container the repeater's rescue just
		// focused, and it throws while a layout pass is in flight.
		var element = repeater.TryGetElement(target);
		m_reshapeFocusWaitForLayout = false;
		if (element is null && m_pendingBodyFocusFromKeyboard)
		{
			// A keyboard move scrolls until the repeater realizes its row; see FocusRowElementInternal.
			if (IsBodyScrollInFlight() || ScrollBodyToIndex(target, m_pendingReshapeFocusAlign))
			{
				m_reshapeFocusWaitForLayout = true;
				return false;
			}
		}

		if (element is null)
		{
			try
			{
				element = repeater.GetOrCreateElement(target);
			}
			catch (Exception)
			{
				return false;
			}

			if (element is null)
			{
				return false;
			}
		}

		if (!IsRowElementArranged(element))
		{
			// Realized but not arranged yet; Focus() would be refused.
			m_reshapeFocusWaitForLayout = m_pendingBodyFocusFromKeyboard;
			return false;
		}

		// Compare containers, not indices: a container focused at an estimated position can still report
		// the target index after the repeater realizes another container for it in the viewport.
		bool focused = IsBodyFocusInContainer(element);
		if (focused && IsRowElementInViewport(element))
		{
			RevealFocusedBodyElement(element);
			return true;
		}

		if (!focused)
		{
			if (m_pendingReshapeFocusCellLevel)
			{
				FocusRealizedRowCell(element, m_pendingReshapeFocusColumn, m_pendingReshapeFocusState);
			}
			else
			{
				FocusRowContainerInternal(element, m_pendingReshapeFocusState);
			}
		}

		if (!IsRowElementInViewport(element))
		{
			bool scrolling = IsBodyScrollInFlight() || ScrollBodyToRowElement(element, m_pendingReshapeFocusAlign);
			m_reshapeFocusWaitForLayout = scrolling || m_pendingBodyFocusFromKeyboard;
		}
		return false;
	}

	private void BeginPendingBodyFocus(
		int row, int column, bool cellLevel, double? alignRatio, object? item = null)
	{
		m_pendingReshapeFocusRow = row;
		m_pendingReshapeFocusColumn = column;
		m_pendingReshapeFocusCellLevel = cellLevel;
		m_pendingReshapeFocusState = FocusState.Keyboard;
		m_pendingReshapeFocusAlign = alignRatio;
		// A caller that ran layout passes the item it captured before; the row may now hold another.
		m_pendingBodyFocusItem = item ?? GetRowsSourceItemAt(row);
		m_pendingBodyFocusFromKeyboard = true;
		m_pendingBodyFocusOrigin = null;
		m_reshapeFocusAttemptsLeft = s_keyboardFocusAttempts;
		ArmReshapeFocusAttempt();
	}

	private bool IsPendingBodyFocusItemAt(int row)
	{
		if (m_rowsItemsSourceView is null || row < 0 || row >= m_rowsItemsSourceView.Count)
		{
			return false;
		}

		var target = m_pendingBodyFocusItem;
		var candidate = m_rowsItemsSourceView.GetAt(row);
		return target is not null && candidate is not null && ReferenceEquals(target, candidate);
	}

	private object? GetRowsSourceItemAt(int index)
	{
		if (m_rowsItemsSourceView is null || index < 0 || index >= m_rowsItemsSourceView.Count)
		{
			return null;
		}
		return m_rowsItemsSourceView.GetAt(index);
	}

	// A synchronous layout runs app code (SizeChanged, bindings) that can edit the source or recycle
	// the container; false means the element no longer shows expectedItem at index.
	private bool ArrangeAndRevalidate(
		UIElement? element, int index, object? expectedItem)
	{
		var repeater = m_rowsRepeater;
		var view = m_rowsItemsSourceView;
		if (element is null || repeater is null || index < 0)
		{
			return false;
		}

		try
		{
			element.UpdateLayout();
		}
		catch (Exception)
		{
			return false;
		}

		if (m_rowsRepeater != repeater || m_rowsItemsSourceView != view ||
			repeater.GetElementIndex(element) != index)
		{
			return false;
		}

		var current = GetRowsSourceItemAt(index);
		return ReferenceEquals(expectedItem, current);
	}

	private static bool IsRowElementArranged(UIElement? element)
	{
		var frameworkElement = element as FrameworkElement;
		if (frameworkElement is null ||
			!frameworkElement.IsLoaded ||
			!(frameworkElement.ActualHeight > 0.0) ||
			VisualTreeHelper.GetParent(frameworkElement) is null)
		{
			return false;
		}

		// ItemsRepeater parks cleared containers at (-10000, -10000); one it re-realizes keeps that
		// offset until its next arrange, and focusing it there strands focus off-screen.
		var offset = frameworkElement.ActualOffset;
		return !(offset.X <= -9999.0f && offset.Y <= -9999.0f);
	}

	private bool IsRowElementInViewport(UIElement? element)
	{
		var frameworkElement = element as FrameworkElement;
		var scroller = m_bodyScroller;
		if (frameworkElement is null)
		{
			return false;
		}
		if (scroller is null)
		{
			// No body scroller: let focus's own bring-into-view reach the app's scroller.
			return true;
		}

		double viewportHeight = scroller.ViewportHeight;
		if (!(viewportHeight > 0.0))
		{
			return false;
		}

		double top = 0.0;
		try
		{
			top = frameworkElement.TransformToVisual(scroller).TransformPoint(new Point(0, 0)).Y;
		}
		catch (Exception)
		{
			return false;
		}

		// Same slack as FindViewportEdgeElementInternal; a row taller than the viewport counts once top-aligned
		// or once it covers the whole viewport (a bottom-aligned PageDown target leaves its top negative).
		const double slack = 0.5;
		double bottom = top + frameworkElement.ActualHeight;
		return (top >= -slack && (bottom <= viewportHeight + slack || top <= slack)) ||
			(top <= slack && bottom >= viewportHeight - slack);
	}

	private bool BringRowElementIntoView(UIElement? element, double? alignRatio)
	{
		var frameworkElement = element as FrameworkElement;
		if (frameworkElement is null)
		{
			return false;
		}

		if (!IsRowElementArranged(element))
		{
			var repeater = m_rowsRepeater;
			int index = repeater is not null ? repeater.GetElementIndex(element) : -1;
			if (index < 0 ||
				!ArrangeAndRevalidate(element, index, GetRowsSourceItemAt(index)) ||
				!IsRowElementArranged(element))
			{
				return false;
			}
		}

		if (IsRowElementInViewport(element))
		{
			return true;
		}

		// Scroll PART_BodyScroller directly. A far target sits at the repeater's estimated position,
		// and a bubbling StartBringIntoView hands whatever the body cannot reach yet to the page's
		// ScrollViewer, scrolling the table out of view. The attempt loop re-aims as estimates settle;
		// RevealFocusedBodyElement bubbles once the row is really in view.
		ScrollBodyToRowElement(element, alignRatio);
		return true;
	}

	private bool ScrollBodyToRowElement(UIElement? element, double? alignRatio)
	{
		var frameworkElement = element as FrameworkElement;
		var scroller = m_bodyScroller;
		var repeater = m_rowsRepeater;
		if (frameworkElement is null || scroller is null || repeater is null)
		{
			return false;
		}

		double viewportHeight = scroller.ViewportHeight;
		if (!(viewportHeight > 0.0))
		{
			return false;
		}

		double offset = scroller.VerticalOffset;
		double scrollable = Math.Max(0.0, scroller.ScrollableHeight);
		int index = repeater.GetElementIndex(frameworkElement);
		int count = GetItemsSourceCount();

		double target = offset;
		if (index == 0)
		{
			// The extent edges are exact even while the rows between them are still estimated.
			target = 0.0;
		}
		else if (index > 0 && index == count - 1)
		{
			target = scrollable;
		}
		else
		{
			double top = 0.0;
			try
			{
				top = frameworkElement.TransformToVisual(scroller).TransformPoint(new Point(0, 0)).Y;
			}
			catch (Exception)
			{
				return false;
			}

			double height = frameworkElement.ActualHeight;
			double delta = 0.0;
			if (height > viewportHeight)
			{
				delta = top;
			}
			else if (alignRatio.HasValue)
			{
				delta = top - alignRatio.Value * (viewportHeight - height);
			}
			else if (top < 0.0)
			{
				delta = top;
			}
			else if (top + height > viewportHeight)
			{
				delta = top + height - viewportHeight;
			}
			target = offset + delta;
		}

		target = Math.Clamp(target, 0.0, scrollable);
		if (Math.Abs(target - offset) < 0.5)
		{
			return false;
		}

		RequestBodyVerticalOffset(target);
		return true;
	}

	private void RequestBodyVerticalOffset(double offset)
	{
		if (m_bodyScroller is { } scroller)
		{
			m_requestedBodyVerticalOffset = offset;
			scroller.ChangeView(null, offset, null, true /* disableAnimation */);
		}
	}

	// ChangeView applies on a later tick (cleared by OnBodyScrollerViewChanged); until then positions
	// still describe the old offset.
	private bool IsBodyScrollInFlight()
	{
		var scroller = m_bodyScroller;
		if (scroller is null || !m_requestedBodyVerticalOffset.HasValue)
		{
			return false;
		}

		if (Math.Abs(scroller.VerticalOffset - m_requestedBodyVerticalOffset.Value) < 0.5)
		{
			m_requestedBodyVerticalOffset = null;
			return false;
		}
		return true;
	}

	// For a row the repeater has not realized: estimate from the realized container nearest to it, so
	// the repeater realizes the row where the viewport lands. Re-run until the row itself is realized,
	// at which point ScrollBodyToRowElement is exact.
	private bool ScrollBodyToIndex(int index, double? alignRatio)
	{
		var repeater = m_rowsRepeater;
		var scroller = m_bodyScroller;
		if (repeater is null || scroller is null)
		{
			return false;
		}

		if (repeater.TryGetElement(index) is { } element && IsRowElementArranged(element))
		{
			return ScrollBodyToRowElement(element, alignRatio);
		}

		double viewportHeight = scroller.ViewportHeight;
		if (!(viewportHeight > 0.0))
		{
			return false;
		}

		double offset = scroller.VerticalOffset;
		double scrollable = Math.Max(0.0, scroller.ScrollableHeight);
		int count = GetItemsSourceCount();

		double target = offset;
		if (index <= 0)
		{
			target = 0.0;
		}
		else if (index >= count - 1)
		{
			target = scrollable;
		}
		else
		{
			int nearestIndex = -1;
			double nearestTop = 0.0;
			double nearestHeight = 0.0;
			double totalHeight = 0.0;
			int measured = 0;
			int childCount = VisualTreeHelper.GetChildrenCount(repeater);
			for (int i = 0; i < childCount; ++i)
			{
				if (VisualTreeHelper.GetChild(repeater, i) is not FrameworkElement child)
				{
					continue;
				}

				int childIndex = repeater.GetElementIndex(child);
				double height = child.ActualHeight;
				if (childIndex < 0 || !(height > 0.0))
				{
					continue;
				}

				double childTop = 0.0;
				try
				{
					childTop = child.TransformToVisual(scroller).TransformPoint(new Point(0, 0)).Y;
				}
				catch (Exception)
				{
					continue;
				}

				totalHeight += height;
				++measured;
				if (nearestIndex < 0 || Math.Abs(childIndex - index) < Math.Abs(nearestIndex - index))
				{
					nearestIndex = childIndex;
					nearestTop = childTop;
					nearestHeight = height;
				}
			}

			if (measured == 0)
			{
				return false;
			}

			double averageHeight = totalHeight / measured;
			double top = index > nearestIndex
				? nearestTop + nearestHeight + (index - nearestIndex - 1) * averageHeight
				: nearestTop - (nearestIndex - index) * averageHeight;
			double delta = 0.0;
			if (alignRatio.HasValue && averageHeight <= viewportHeight)
			{
				delta = top - alignRatio.Value * (viewportHeight - averageHeight);
			}
			else if (top < 0.0 || averageHeight > viewportHeight)
			{
				delta = top;
			}
			else if (top + averageHeight > viewportHeight)
			{
				delta = top + averageHeight - viewportHeight;
			}
			target = offset + delta;
		}

		target = Math.Clamp(target, 0.0, scrollable);
		if (Math.Abs(target - offset) < 0.5)
		{
			return false;
		}

		RequestBodyVerticalOffset(target);
		return true;
	}

	// Once the body shows the row, reveal it in any outer scroller (ListView does the same on focus).
	// The request starts at the TableView, not the row: routed through PART_BodyScroller and the
	// repeater it would re-anchor the virtualized body.
	private void RevealFocusedBodyElement(UIElement? element)
	{
		var root = XamlRoot;
		var scroller = m_bodyScroller;
		if (element is null || root is null || scroller is null)
		{
			return;
		}

		var target = element as FrameworkElement;
		if (FocusManager.GetFocusedElement(root) is FrameworkElement focused &&
			SharedHelpers.IsAncestor(focused, element, false /* checkVisibility */))
		{
			target = focused;
		}
		if (target is null)
		{
			return;
		}

		try
		{
			// A focus move suppressed while off-viewport skipped the horizontal reveal of its cell.
			var inBody = target.TransformToVisual(scroller).TransformPoint(new Point(0, 0));
			double width = target.ActualWidth;
			double viewportWidth = scroller.ViewportWidth;
			if (target != element && viewportWidth > 0.0 && width <= viewportWidth)
			{
				double delta = 0.0;
				if (inBody.X < 0.0)
				{
					delta = inBody.X;
				}
				else if (inBody.X + width > viewportWidth)
				{
					delta = inBody.X + width - viewportWidth;
				}
				if (Math.Abs(delta) >= 0.5)
				{
					scroller.ChangeView(scroller.HorizontalOffset + delta, null, null, true /* disableAnimation */);
				}
			}

			var origin = target.TransformToVisual(this).TransformPoint(new Point(0, 0));
			double tableHeight = ActualHeight;
			double top = Math.Clamp(origin.Y, 0.0, tableHeight);
			double bottom = Math.Clamp(origin.Y + target.ActualHeight, 0.0, tableHeight);
			if (!(bottom > top))
			{
				return;
			}

			double tableWidth = ActualWidth;
			double left = Math.Clamp(origin.X, 0.0, tableWidth);
			double right = Math.Clamp(origin.X + target.ActualWidth, 0.0, tableWidth);
			BringIntoViewOptions options = new();
			options.AnimationDesired = false;
			options.TargetRect = new Rect(
				left, top,
				Math.Max(0.0, right - left), bottom - top);
			StartBringIntoView(options);
		}
		catch (Exception)
		{
		}
	}

	// GetOrCreateElement makes its element the repeater's layout anchor, and only the repeater's own
	// bring-into-view handling releases it (no public API cancels it). Raise a request for the repeater
	// through one of its children, and swallow it once the repeater has seen it, so nothing scrolls.
	internal void ReleaseRowsRepeaterAnchor(UIElement? anchor)
	{
		var repeater = m_rowsRepeater;
		if (repeater is null || VisualTreeHelper.GetChildrenCount(repeater) == 0)
		{
			return;
		}

		var target = anchor;
		if (target is null || VisualTreeHelper.GetParent(target) != repeater)
		{
			target = VisualTreeHelper.GetChild(repeater, 0) as UIElement;
		}
		if (target is null)
		{
			return;
		}

		m_anchorReleaseTargets.RemoveAll(static pending => pending.Get() is null);
		m_anchorReleaseTargets.Add(new WeakReference<UIElement>(target));
		BringIntoViewOptions options = new();
		options.AnimationDesired = false;
		options.TargetRect = new Rect();
		target.StartBringIntoView(options);
	}

	// XAML pads a keyboard-focus bring-into-view by 20 DIPs (or the focus visual margin), so focusing a
	// row that is already fully in view would still nudge the body, e.g. a first PageDown would scroll.
	// Drop only the vertical padding; the horizontal request still reveals a focused cell.
	private void OnBodyBringIntoViewRequested(BringIntoViewRequestedEventArgs args)
	{
		// Routed asynchronously, so matched by its target and empty rect.
		if (args.TargetRect.Width == 0.0 && args.TargetRect.Height == 0.0)
		{
			var releaseTarget = args.TargetElement;
			int it = m_anchorReleaseTargets.FindIndex(pending => pending.Get() == releaseTarget);
			if (it >= 0)
			{
				m_anchorReleaseTargets.RemoveAt(it);
				args.Handled = true;
				return;
			}
		}

		var repeater = m_rowsRepeater;
		var target = args.TargetElement as FrameworkElement;
		if (repeater is null || target is null)
		{
			return;
		}

		// Our own reveal after a drill-in: a scroll it causes is recorded, not taken as the user's.
		using var noteDrillReveal = Disposable.Create(() =>
		{
			var scroller = m_bodyScroller;
			if (m_drillInOffset < 0.0 || args.Handled || scroller is null)
			{
				return;
			}
			try
			{
				// Only a reveal the scroller can still perform scrolls (padding at an edge does not).
				var bounds = target.TransformToVisual(scroller).TransformBounds(args.TargetRect);
				double offset = scroller.HorizontalOffset;
				m_drillRevealPending = m_drillRevealPending ||
					(bounds.X < -0.5 && offset > 0.5) ||
					(bounds.X + bounds.Width > scroller.ViewportWidth + 0.5 && offset < scroller.ScrollableWidth - 0.5);
			}
			catch (Exception)
			{
				m_drillRevealPending = true;
			}
		});

		UIElement? bodyItem = null;
		for (DependencyObject? node = target; node is not null; node = VisualTreeHelper.GetParent(node))
		{
			if (node is TableViewRow || node is TableViewGroupHeader)
			{
				if (node is UIElement element &&
					VisualTreeHelper.GetParent(element) == repeater)
				{
					bodyItem = element;
					break;
				}
			}
		}

		if (bodyItem is not null && m_drillRestoreOffset >= 0.0 && target == bodyItem && bodyItem == m_drillRestoreRow.Get())
		{
			double offset = m_drillRestoreOffset;
			m_drillRestoreOffset = -1.0;
			args.Handled = true;
			if (m_bodyScroller is { } scroller)
			{
				scroller.ChangeView(offset, null, null, true /* disableAnimation */);
			}
			return;
		}

		if (bodyItem is not null)
		{
			// A group toggle's rescued focus may sit on a cached row or header; revealing it would jump the body.
			if (m_pendingGroupFocusLayoutToken.Disposable is not null &&
				IsBodyFocusInContainer(bodyItem))
			{
				args.Handled = true;
				return;
			}

			// Focus raises its padded request asynchronously. When keys move focus faster than that,
			// the request arrives for a container focus has already left, whose position may now be
			// a recycled or estimated one; honoring it scrolled the body back to the top.
			var requested = args.TargetRect;
			bool focusPadded = requested.Y < 0.0 || requested.Y + requested.Height > target.ActualHeight;
			if (focusPadded && !IsBodyFocusInContainer(bodyItem))
			{
				args.Handled = true;
				return;
			}

			// While the table drives the body to a row it has not reached yet, the request focus raises
			// would bubble the unreachable part of an estimated position to outer scrollers.
			if (IsKeyboardBodyFocusPending() && !IsRowElementInViewport(bodyItem))
			{
				args.Handled = true;
				return;
			}
		}

		if (!double.IsNaN(args.VerticalAlignmentRatio) || args.VerticalOffset != 0.0)
		{
			return;
		}

		var root = XamlRoot;
		if (root is null || FocusManager.GetFocusedElement(root) != target)
		{
			return;
		}

		// Only the containers and cells the table focuses itself; hosted content keeps XAML's behavior.
		UIElement? container = null;
		if (target is TableViewRow || target is TableViewGroupHeader)
		{
			container = target;
		}
		else if (VisualTreeHelper.GetParent(target) is TableViewCellsPanel)
		{
			for (var node = VisualTreeHelper.GetParent(target); node is not null; node = VisualTreeHelper.GetParent(node))
			{
				if (node is TableViewRow row)
				{
					container = row;
					break;
				}
			}
		}
		if (container is null || VisualTreeHelper.GetParent(container) != repeater || !IsRowElementInViewport(container))
		{
			return;
		}

		var rect = args.TargetRect;
		double height = target.ActualHeight;
		if (!(rect.Height > 0.0) || !(height > 0.0))
		{
			return;
		}

		double top = Math.Max(rect.Y, 0.0);
		double bottom = Math.Min(rect.Y + rect.Height, height);
		if (bottom > top && (top != rect.Y || bottom != rect.Y + rect.Height))
		{
			rect.Y = top;
			rect.Height = bottom - top;
			args.TargetRect = rect;
		}
	}

	private bool IsInUnrealizedBodyContainer(DependencyObject? element)
	{
		var repeater = m_rowsRepeater;
		DependencyObject? repeaterObject = repeater;
		DependencyObject? node = element;
		if (element is TextElement textElement)
		{
			node = textElement.ContentStart?.VisualParent;
		}
		while (repeater is not null && node is not null)
		{
			var parent = VisualTreeHelper.GetParent(node);
			if (parent == repeaterObject)
			{
				return repeater.GetElementIndex((UIElement)node) < 0;
			}
			node = parent;
		}
		return false;
	}

	private bool IsBodyFocusInContainer(UIElement? container)
	{
		var repeater = m_rowsRepeater;
		var root = XamlRoot;
		if (container is null || repeater is null || root is null)
		{
			return false;
		}

		for (var node = FocusManager.GetFocusedElement(root) as DependencyObject;
			node is not null;
			node = VisualTreeHelper.GetParent(node))
		{
			if (node == container)
			{
				return true;
			}
			if (node == repeater)
			{
				return false;
			}
		}
		return false;
	}

	private bool IsBodyFocusAtRow(int rowIndex)
	{
		if (rowIndex < 0)
		{
			return false;
		}

		if (TryGetFocusedCell(out var cellRow, out _, false /* requireExactCell */))
		{
			return cellRow == rowIndex;
		}

		return GetFocusedRowIndex() == rowIndex;
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

	// A refused target can be substituted by one that lands back on a row and re-enters the redirect.
	internal static bool IsEntryRedirectTargetFocusable(DependencyObject? targetObject, DependencyObject owner)
	{
		// A Hyperlink is checked through its host text element.
		var link = targetObject as Hyperlink;
		var target = link is not null ? link.ContentStart?.VisualParent as UIElement : targetObject as UIElement;
		if (target is null || target.XamlRoot is null || target.Visibility != Visibility.Visible || !(link is not null ? link.IsTabStop : target.IsTabStop))
		{
			return false;
		}

		if (!SharedHelpers.IsAncestor(target, owner, true /* checkVisibility */))
		{
			return false;
		}

		// Cells are Grids, not Controls, so a disabled ancestor row does not show in the target's own state.
		DependencyObject? current = target;
		while (current is not null)
		{
			if (current is Control control && !control.IsEnabled)
			{
				return false;
			}
			if (current == owner)
			{
				break;
			}
			current = VisualTreeHelper.GetParent(current);
		}

		return true;
	}

	internal UIElement? FindViewportEdgeElementInternal(bool last, bool includeGroupHeaders, bool allowNoViewport = false)
	{
		var repeater = m_rowsRepeater;
		var scroller = m_bodyScroller;
		if (repeater is null || (scroller is null && !allowNoViewport))
		{
			return null;
		}

		double viewportHeight = scroller is not null ? scroller.ViewportHeight : 0.0;
		if (scroller is not null && !(viewportHeight > 0.0))
		{
			return null;
		}

		UIElement? fullyVisible = null;
		int fullyVisibleIndex = -1;
		UIElement? partiallyVisible = null;
		int partiallyVisibleIndex = -1;
		Func<int, int, bool> better = (candidate, current) =>
		{
			return current < 0 || (last ? candidate > current : candidate < current);
		};

		int childCount = VisualTreeHelper.GetChildrenCount(repeater);
		for (int i = 0; i < childCount; ++i)
		{
			var element = VisualTreeHelper.GetChild(repeater, i) as Control;
			bool isCandidate = element is not null &&
				(element is TableViewRow || (includeGroupHeaders && element is TableViewGroupHeader));
			if (!isCandidate || element!.Visibility != Visibility.Visible || !element.IsEnabled)
			{
				continue;
			}

			// Recycled containers stay parented with index -1.
			int index = repeater.GetElementIndex(element);
			double height = element.ActualHeight;
			if (index < 0 || !(height > 0.0))
			{
				continue;
			}

			if (scroller is null)
			{
				if (better(index, fullyVisibleIndex))
				{
					fullyVisible = element;
					fullyVisibleIndex = index;
				}
				continue;
			}

			double top = 0.0;
			try
			{
				top = element.TransformToVisual(scroller).TransformPoint(new Point(0, 0)).Y;
			}
			catch (Exception)
			{
				continue;
			}

			// Half a pixel of slack absorbs layout rounding at the viewport edges.
			const double slack = 0.5;
			double bottom = top + height;
			if (bottom <= slack || top >= viewportHeight - slack)
			{
				continue;
			}

			if (top >= -slack && bottom <= viewportHeight + slack)
			{
				if (better(index, fullyVisibleIndex))
				{
					fullyVisible = element;
					fullyVisibleIndex = index;
				}
			}
			else if (better(index, partiallyVisibleIndex))
			{
				partiallyVisible = element;
				partiallyVisibleIndex = index;
			}
		}

		return fullyVisible is not null ? fullyVisible : partiallyVisible;
	}

	private int GetPageTargetRow(int currentRow, bool pageDown, int rowCount)
	{
		// Flattened indices: a group header at the viewport edge is a page target like a row, so a
		// viewport holding one data row between headers still pages by the viewport.
		int first = -1;
		int last = -1;
		if (m_rowsRepeater is { } repeater)
		{
			if (FindViewportEdgeElementInternal(false /* last */, true /* includeGroupHeaders */) is { } firstElement)
			{
				first = repeater.GetElementIndex(firstElement);
			}
			if (FindViewportEdgeElementInternal(true /* last */, true /* includeGroupHeaders */) is { } lastElement)
			{
				last = repeater.GetElementIndex(lastElement);
			}
		}

		int step = (first >= 0 && last >= 0)
			? Math.Max(1, last - first)
			: Math.Max(1, GetEstimatedRowsPerPage() - 1);
		if (pageDown)
		{
			return (last >= 0 && currentRow < last) ? last : Math.Clamp(currentRow + step, 0, rowCount - 1);
		}
		return (first >= 0 && currentRow > first) ? first : Math.Clamp(currentRow - step, 0, rowCount - 1);
	}

	// Excludes nested TableViews and hosted content: hosted TextBox/ComboBox controls keep the keys they claim.
	private bool IsFocusExactlyOnBodyItem()
	{
		var root = XamlRoot;
		if (root is null)
		{
			return false;
		}

		var focused = FocusManager.GetFocusedElement(root) as UIElement;
		if (focused is null)
		{
			return false;
		}

		if (focused is TableViewRow || focused is TableViewGroupHeader)
		{
			var repeater = m_rowsRepeater;
			return repeater is not null && repeater.GetElementIndex(focused) >= 0;
		}

		return TryGetFocusedCell(out _, out _, true /* requireExactCell */);
	}

	internal void ArmCellLevelRowInternal(TableViewRow? row)
	{
		// Release any other drilled-in row first so only one row ever has its cells armed.
		if (row is null)
		{
			return;
		}

		if (m_cellLevelRow.Get() is { } previous && previous != row)
		{
			previous.SetCellLevelInternal(false);
		}
		if (!row.IsCellLevelInternal())
		{
			row.SetCellLevelInternal(true);
		}
		m_cellLevelRow = new WeakReference<TableViewRow>(row);
		m_cellCursorActive = true;
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

	internal void OnRowCellFocusChanged(TableViewRow? row, DependencyObject? focusedElement)
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
		if (rowIndex != m_cellIntentRow)
		{
			m_cellIntentColumn = -1;
		}

		// GotFocus is async and live focus may have moved on (e.g. into a context menu), so use the element that got it.
		var focusedCell = focusedElement is not null
			? rowImpl.FindOwnCellInternal(focusedElement, false /* requireExact */)
			: null;

		if (focusedCell is null)
		{
			var root = XamlRoot;
			DependencyObject rowObject = row;
			bool focusStillOnRow = root is not null && (FocusManager.GetFocusedElement(root) as DependencyObject) == rowObject;

			// Focus went to the ROW container itself - the body's entry level. Record the row.
			if ((focusedElement == rowObject || focusStillOnRow) && rowIndex >= 0)
			{
				m_currentCellRow = rowIndex;
				m_rememberedSelectionSequence = m_selectionSequence;
				SetCurrentItem(TryGetItemAtRowIndex(rowIndex, out var item) ? item : null);
			}

			// Even if focus has moved on: this row focus is newer than any cell focus already handled.
			SetCellCursorActiveInternal(false);
			return;
		}

		int columnIndex = rowImpl.GetVisibleCellIndexInternal(focusedCell);
		if (m_cellIntentColumn >= 0 && rowIndex == m_cellIntentRow)
		{
			SetColumnCursorInternal(m_cellIntentColumn);
		}
		else if (columnIndex >= 0)
		{
			// A cell actually took focus, so the user is in a column. Establishes the shared cursor for
			// the header band too - Shift+Tab back up should land on the column being worked in.
			SetColumnCursorInternal(columnIndex);
		}

		if (rowIndex >= 0)
		{
			m_currentCellRow = rowIndex;
			m_rememberedSelectionSequence = m_selectionSequence;
		}

		ArmCellLevelRowInternal(row);

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

		var focused = GetFocusedVisual(root);
		if (focused is null)
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

	internal bool FocusCell(int rowIndex, int visibleColumnIndex, double? alignRatio = null)
	{
		if (visibleColumnIndex >= 0)
		{
			// An explicit column names the column the caller wants the cursor on, so it establishes it.
			SetColumnCursorInternal(visibleColumnIndex);
		}

		return FocusRowElementInternal(rowIndex, m_currentCellColumn, true /* cellLevel */, alignRatio);
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

	internal bool FocusRowContainer(int rowIndex, double? alignRatio = null)
	{
		// Row level keeps the remembered column untouched for the next drill-in.
		return FocusRowElementInternal(rowIndex, m_currentCellColumn, false /* cellLevel */, alignRatio);
	}

	// Shared realization path; a row that cannot be arranged yet finishes in the re-shape attempt loop.
	private bool FocusRowElementInternal(
		int rowIndex, int targetColumn, bool cellLevel, double? alignRatio)
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

		// This request is newer intent than any keyboard or re-shape focus still settling.
		CancelReshapeFocusRestore();

		// A row the repeater has not realized is scrolled to first and focused once realized.
		// GetOrCreateElement would make it the repeater's anchor at an estimated position, and repeated
		// far jumps (key-repeat PageDown) left focus on such a container after the repeater re-indexed
		// it off-screen.
		var element = repeater.TryGetElement(rowIndex);
		if (element is null)
		{
			if (ScrollBodyToIndex(rowIndex, alignRatio))
			{
				BeginPendingBodyFocus(rowIndex, targetColumn, cellLevel, alignRatio);
				return true;
			}
			element = repeater.GetOrCreateElement(rowIndex);
		}
		if (element is not Control)
		{
			return false;
		}

		if (!IsRowElementArranged(element))
		{
			var expectedItem = GetRowsSourceItemAt(rowIndex);
			if (!ArrangeAndRevalidate(element, rowIndex, expectedItem))
			{
				BeginPendingBodyFocus(rowIndex, targetColumn, cellLevel, alignRatio, expectedItem);
				return true;
			}
		}

		if (IsRowElementArranged(element))
		{
			bool focused = cellLevel
				? FocusRealizedRowCell(element, targetColumn)
				: FocusRowContainerInternal(element);
			if (!focused)
			{
				return false;
			}

			if (IsRowElementInViewport(element))
			{
				return true;
			}
			BringRowElementIntoView(element, alignRatio);
		}

		BeginPendingBodyFocus(rowIndex, targetColumn, cellLevel, alignRatio);
		return true;
	}

	// Focuses an already-realized container at row level; group headers have no cell level.
	private bool FocusRowContainerInternal(UIElement element, FocusState focusState = FocusState.Keyboard)
	{
		var row = element as TableViewRow;
		if (row is not null)
		{
			// Make the row focusable before clearing the focused cell from tab order.
			row.EnableRowFocusInternal();
		}

		if (element is not Control control || !control.Focus(focusState))
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
	internal bool FocusRealizedRowCell(
		UIElement element,
		int visibleColumnIndex,
		FocusState focusState = FocusState.Keyboard)
	{
		if (element is TableViewRow row)
		{
			var rowImpl = row;
			int cellCount = rowImpl.GetVisibleCellCountInternal();
			if (cellCount > 0)
			{
				// The nearest actionable cell to the requested column; ties go forward.
				int requested = Math.Clamp(visibleColumnIndex, 0, cellCount - 1);
				int after = rowImpl.FindActionableCellInternal(requested, 1);
				int before = rowImpl.FindActionableCellInternal(requested - 1, -1);
				int column = (after >= 0 && (before < 0 || after - requested <= requested - before)) ? after : before;
				if (column >= 0 && rowImpl.FocusVisibleCellInternal(column, focusState))
				{
					return true;
				}
			}

			// The row has no focusable cell (no columns, or every column collapsed). Row level is the
			// only level left, and it is a legitimate resting place in the two-level model.
			return FocusRowContainerInternal(element, focusState);
		}

		// A group header: never a cell-level target.
		return FocusRowContainerInternal(element, focusState);
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

		header.SetGestureFocusStateInternal(FocusState.Keyboard);
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
		var link = target is not null ? null : FindFirstHyperlinkDescendant(cell, 8);
		if (target is not null ? !target.Focus(FocusState.Keyboard) : !(link is not null && link.Focus(FocusState.Keyboard)))
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
			GetFocusedVisual(root) as UIElement :
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
			GetFocusedVisual(root) as UIElement :
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
			var row = GetRealizedRowAt(m_navAnchorRowContainer);
			if (row is null)
			{
				// Consume Left so directional focus navigation cannot walk out of the table.
				if (!drillIn)
				{
					args.Handled = true;
					return true;
				}
				return false;
			}

			// Treegrid level: forward expands a collapsed node else drills in; backward collapses an
			// expanded node else moves to the parent. Shift chords keep the plain drill.
			if (row.Level > 0 && !IsKeyDown(VirtualKey.Shift))
			{
				if (drillIn && row.IsExpandable && !row.IsExpanded)
				{
					SetGroupExpansion(row, true);
					args.Handled = true;
					return true;
				}

				if (!drillIn)
				{
					if (row.IsExpandable && row.IsExpanded)
					{
						SetGroupExpansion(row, false);
					}
					else if (!TryFocusParentRow(row))
					{
						ScrollBodyHorizontallyInternal(false);
					}

					// Consumed even at a root, for the same reason as the flat row below.
					args.Handled = true;
					return true;
				}
			}

			if (!drillIn)
			{
				// Nothing further out at row level. Consumed anyway: an unconsumed Left here reaches
				// directional focus navigation, which would walk focus sideways out of the table.
				ScrollBodyHorizontallyInternal(false);
				args.Handled = true;
				return true;
			}

			var rowImpl = row;
			int cellCount = rowImpl.GetVisibleCellCountInternal();
			if (cellCount <= 0)
			{
				// A row with no visible columns has no cell level to drill into.
				args.Handled = true;
				return true;
			}

			// Enter at the first actionable cell, not the remembered column, so Right and Left stay
			// inverses: Left on the first actionable cell pops back to the row. Bringing it into view
			// undoes any horizontal scroll. Without one, Right scrolls on.
			if (rowImpl.FindActionableCellInternal(0, 1) is var first && first >= 0)
			{
				// Remembered so Left can undo the bring-into-view in one step.
				var drillScroller = m_bodyScroller;
				m_drillInOffset = drillScroller is not null ? drillScroller.HorizontalOffset : -1.0;
				m_drillSettledOffset = m_drillInOffset;
				m_drillRevealPending = false;
				m_drillInRow = m_navAnchorRowContainer;
				rowImpl.FocusVisibleCellInternal(first, FocusState.Keyboard);
			}
			else
			{
				ScrollBodyHorizontallyInternal(true);
			}
			args.Handled = true;
			return true;
		}

		// Focus was on a cell: only the first actionable cell pops out; other cells leave the key to
		// cell navigation.
		var anchorRow = GetRealizedRowAt(m_navAnchorCellRow);
		if (drillIn || anchorRow is null || m_navAnchorCellColumn < 0 ||
			anchorRow.FindActionableCellInternal(m_navAnchorCellColumn - 1, -1) >= 0)
		{
			return false;
		}

		// Right and Left stay inverses across a horizontal scroll: the drill-in's bring-into-view is undone
		// in one step, else the body steps back while the cell stays in view.
		var scroller = m_bodyScroller;
		// A scroll since our own reveals settled was the user's (or the app's): step back through it instead.
		bool scrolledSinceDrill = scroller is not null && !m_drillRevealPending &&
			Math.Abs(scroller.HorizontalOffset - m_drillSettledOffset) > 8.0;
		double restoreOffset = m_drillInRow == m_navAnchorCellRow && !scrolledSinceDrill ? m_drillInOffset : -1.0;
		if (restoreOffset < 0.0 &&
			ScrollBodyHorizontallyInternal(false, false, anchorRow.GetVisibleCellInternal(m_navAnchorCellColumn)))
		{
			args.Handled = true;
			return true;
		}
		m_drillInOffset = -1.0;
		if (scroller is not null && restoreOffset >= 0.0)
		{
			m_drillRestoreRow = new WeakReference<UIElement>(anchorRow);
			m_drillRestoreOffset = restoreOffset;
		}
		if (!FocusRowContainer(m_navAnchorCellRow))
		{
			m_drillRestoreOffset = -1.0;
			// The row refused focus; let the cell cursor have the key rather than swallowing it.
			return false;
		}

		// Applied again in place of the row's focus bring-into-view (OnBodyBringIntoViewRequested).
		if (scroller is not null && restoreOffset >= 0.0)
		{
			scroller.ChangeView(restoreOffset, null, null, true /* disableAnimation */);
		}
		args.Handled = true;
		return true;
	}

	// Reading-order forward or back by one step (or to the edge), clamped to the scrollable range. Not
	// animated, so auto-repeat steps from the offset the previous key reached.
	private bool ScrollBodyHorizontallyInternal(bool forward, bool toEdge = false, UIElement? keepInView = null)
	{
		var scroller = m_bodyScroller;
		if (scroller is null || (forward ? scroller.HorizontalOffset >= scroller.ScrollableWidth - 0.5 : scroller.HorizontalOffset <= 0.5))
		{
			return false;
		}

		// A few pixels left by a focus bring-into-view count as the start: settle there, but report no
		// scroll so Left still returns to the row.
		const double slack = 8.0;
		if (!forward && !toEdge && scroller.HorizontalOffset <= slack)
		{
			scroller.ChangeView(0.0, null, null, true /* disableAnimation */);
			return false;
		}

		const double step = 48.0;
		double offset = scroller.HorizontalOffset;
		double target = toEdge ? (forward ? scroller.ScrollableWidth : 0.0)
			: Math.Clamp(offset + (forward ? step : -step), 0.0, scroller.ScrollableWidth);

		// A focused non-frozen element stays in view: an edge scroll stops short, and a step is refused.
		var element = keepInView as FrameworkElement;
		var column = element is not null ? element.Tag as TableViewColumn : null;
		if (element is not null && !(column is not null && column.FrozenEdge != TableViewFrozenEdge.None))
		{
			var bounds = element.TransformToVisual(scroller).TransformBounds(new Rect(0, 0, element.ActualWidth, 0));
			double left = offset + bounds.X;
			// The leading frozen band covers the start of the viewport.
			double inView = forward ? Math.Min(target, Math.Max(0.0, left - ComputeLeadingFrozenWidth()))
				: Math.Max(target, left + bounds.Width - scroller.ViewportWidth);
			if (!toEdge && inView != target)
			{
				return false;
			}
			target = inView;
		}
		m_drillInOffset = -1.0;
		return scroller.ChangeView(target, null, null, true /* disableAnimation */);
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

		// Only actionable cells take focus, so moves skip inactive ones and stop at the last actionable.
		var rowImpl = row;
		bool isRtl = FlowDirection == FlowDirection.RightToLeft;
		int step = (isRight != isRtl) ? 1 : -1;
		int targetColumn =
			isHome ? rowImpl.FindActionableCellInternal(0, 1) :
			isEnd ? rowImpl.FindActionableCellInternal(cellCount - 1, -1) :
			rowImpl.FindActionableCellInternal(currentColumn + step, step);
		if (targetColumn < 0)
		{
			targetColumn = currentColumn;
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
			// Home/End reach the edge, and past the last (first) actionable cell the body scrolls on.
			if (isHome || isEnd || atBound)
			{
				ScrollBodyHorizontallyInternal(isHome ? false : isEnd ? true : step > 0, isHome || isEnd,
					isHome || isEnd ? rowImpl.GetVisibleCellInternal(targetColumn) : null);
			}
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
