// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Keyboard.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Row keyboard navigation and its focus/measurement helpers live here.

partial class TableView
{
	// TODO Uno: anonymous-namespace helper of TableView_Keyboard.cpp.
	private static bool IsKeyDown(VirtualKey key) =>
		(InputKeyboardSource.GetKeyStateForCurrentThread(key) &
			CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

	// Row cells carry the same column Tag as header cells, so finding a tagged ancestor is not
	// enough: the walk must actually reach the header host, or a focused cell (an open editor,
	// most visibly) would resolve to a column and let arrow keys resize it.
	// Returns the focused header cell in headerCell, so a caller that needs the cell does not have
	// to scan the header band again to find what this walk already passed through.
	// TODO Uno: anonymous-namespace helper of TableView_Keyboard.cpp.
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

	// The focused header is the element AT is on, so attribute the announcement to its peer.
	// TODO Uno: anonymous-namespace helper of TableView_Keyboard.cpp.
	private static void AnnounceColumnWidthOn(object? announcer, TableViewColumn? column)
	{
		var element = announcer as UIElement;
		if (element is null || column is null)
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
			// TODO Uno: C++ matches column.Header().try_as<winrt::IStringable>(). A WinRT boxed string can
			// answer that QI, but a .NET string does not implement IStringable, so it is matched explicitly.
			if (column.Header is string headerString)
			{
				headerName = headerString;
			}
			else if (column.Header is IStringable stringable)
			{
				headerName = stringable.ToString();
			}
		}

		// Whole pixels: sub-pixel precision is noise in an announcement.
		var width = ((int)Math.Round(column.ActualWidth, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

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
			// The host app may not merge the control's PRI; a missing string must not break resize.
		}
	}

	// Both input paths end in DragCompleted, so the announcement lives there rather than in the key
	// handler: a pointer resize was otherwise completely silent to assistive technology.
	private void AnnounceColumnWidth(object? announcer, TableViewColumn? column)
	{
		AnnounceColumnWidthOn(announcer, column);
	}

	// Left/Right resizes the column whose header has focus; Shift takes the large step, and Ctrl is
	// accepted as an alias. Tab moves between headers, so the arrows are free to resize. Driving the
	// gripper's own Begin/Try/End keeps pointer and keyboard on one clamping path.
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

		// Alt is reserved: Alt alone opens the window menu.
		if (IsKeyDown(VirtualKey.Menu))
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

		// Consume even at a bound, so the key does not fall through to focus navigation.
		args.Handled = true;
		return true;
	}

	private void OnPreviewKeyDownForNavigation(
		object sender,
		KeyRoutedEventArgs args)
	{
		// Runs on the tunneling pass, before the framework's built-in focus navigation moves focus for
		// this key. Record the focused row now so OnKeyDownForNavigation (bubbling, possibly after the
		// built-in move) advances from the pre-move row. -1 when focus isn't on one of our rows.
		switch (args.Key)
		{
			case VirtualKey.Up:
			case VirtualKey.Down:
			case VirtualKey.Home:
			case VirtualKey.End:
			case VirtualKey.PageUp:
			case VirtualKey.PageDown:
				m_navAnchorRow = GetFocusedRowIndex();
				break;
			default:
				break;
		}
	}

	private void OnKeyDownForNavigation(
		object sender,
		KeyRoutedEventArgs args)
	{
		// An open editor owns its keys. Row navigation would scroll the edited row out of the
		// realization window, recycling it mid-edit, and a single-line TextBox does not mark
		// PageUp/PageDown handled - so without this guard the DEFAULT editor is enough to trigger it.
		// WPF's DataGrid suppresses navigation the same way while a cell is being edited.
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

		// Column resize from a focused header: after the editing guard, so an open editor keeps its
		// arrow keys, and before row navigation, since the header band is not part of it.
		if (TryHandleHeaderColumnResizeKey(args))
		{
			return;
		}

		// Registered with handledEventsToo so navigation can still run after the ancestor
		// PART_BodyScroller marks nav keys Handled for scrolling. But handledEventsToo also
		// surfaces keys a focused *descendant* consumed (e.g. an editor/ComboBox inside a
		// TableViewTemplateColumn cell). Distinguish the two: only act on an already-handled
		// key when focus is on one of OUR TableViewRow containers (the scroller-handled case).
		// Requiring the focused row to belong to m_rowsRepeater also prevents a nested
		// TableView's inner-row key from double-navigating this outer table.
		if (args.Handled)
		{
			bool focusOnOurRow = false;
			if (XamlRoot is { } root)
			{
				// A group header is as much "one of our containers" as a data row: both are elements of
				// m_rowsRepeater and both are valid arrow-navigation anchors. Recognizing only rows here
				// ate keys pressed while a header had focus (the scroller marks nav keys Handled before
				// this bubbling handler runs, so the guard returned early and no navigation happened).
				if (FocusManager.GetFocusedElement(root) is UIElement focused)
				{
					if (focused is TableViewRow || focused is TableViewGroupHeader)
					{
						if (m_rowsRepeater is { } repeater)
						{
							focusOnOurRow = repeater.GetElementIndex(focused) >= 0;
						}
					}
				}
			}
			if (!focusOnOurRow)
			{
				return;
			}
		}

		var key = args.Key;

		// GridCoordinateHelper keeps row-navigation clamp semantics in one place.
		int rowCount = GetItemsSourceCount();
		if (rowCount <= 0)
		{
			return;
		}

		// Space selects the focused row without moving it. Only when the ROW ITSELF has focus: a Space
		// pressed inside a cell's interactive content (a CheckBox in a template column) belongs to that
		// control, and swallowing it here would break it.
		if (key == VirtualKey.Space && CanSelectRows())
		{
			if (XamlRoot is { } root)
			{
				if (FocusManager.GetFocusedElement(root) is TableViewRow focusedRow)
				{
					if (m_rowsRepeater is { } repeater)
					{
						var focusedIndex = repeater.GetElementIndex(focusedRow);
						if (focusedIndex >= 0)
						{
							SelectRowIndexFromInteraction(focusedIndex);
							args.Handled = true;
						}
					}
				}
			}

			return;
		}

		// Anchor on the row focus was on BEFORE this key (captured in PreviewKeyDown). The framework's
		// built-in navigation may have already advanced focus one row and marked the key Handled;
		// navigating from the post-move row would skip a row.
		int currentRow = m_navAnchorRow;

		// Ctrl+Arrow moves the focus cursor WITHOUT selecting, matching ListViewBase. Without it a
		// keyboard-only user cannot review other rows and come back, and every row they pass through
		// raises SelectionChanged plus UIA selection events - a selection storm for a screen reader.
		bool isControlDown = IsKeyDown(VirtualKey.Control);

		// Focus was not on one of our rows (the user clicked a header, tabbed away and back, or closed
		// a dialog). With selection on, resume relative-navigation from the SELECTED row rather than
		// restarting at row 0 - otherwise Down after clicking away yanks the selection to the top of
		// the table. Absolute keys (Home/End/Page*) keep their own entry points below.
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
			if (initialRow >= 0 && FocusRow(initialRow))
			{
				// Single selection follows the keyboard cursor, matching ListView and WPF's DataGrid.
				if (!isControlDown)
				{
					SelectRowIndexFromInteraction(initialRow);
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
			if (newRow != currentRow)
			{
				// The framework's built-in navigation may have already advanced focus to newRow; if so
				// don't move again (that doubling is the alternate-row skip) — just consume the key.
				if (GetFocusedRowIndex() == newRow || FocusRow(newRow))
				{
					if (!isControlDown)
					{
						SelectRowIndexFromInteraction(newRow);
					}
					args.Handled = true;
				}
			}
			else if (absoluteRowNav)
			{
				// Consume boundary no-ops so PART_BodyScroller does not scroll.
				args.Handled = true;
			}
		}
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

	internal bool FocusRow(int index)
	{
		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return false;
		}

		var rowCount = GetItemsSourceCount();
		if (index < 0 || index >= rowCount)
		{
			return false;
		}

		// A new focus request supersedes any earlier pending deferred (off-screen) focus, so an
		// older callback can't later yank focus back to a stale index over this newer one.
		if (m_pendingFocusLayoutToken.Disposable is not null)
		{
			// Original C++: LayoutUpdated(m_pendingFocusLayoutToken); m_pendingFocusLayoutToken = {};
			m_pendingFocusLayoutToken.Disposable = null;
		}

		// Materialize virtualized rows before focusing and scrolling them into view.
		var element = repeater.GetOrCreateElement(index);
		if (element is null)
		{
			return false;
		}

		var frameworkElement = element as FrameworkElement;
		if (frameworkElement is not null)
		{
			// Scroll through PART_BodyScroller without animation when focus moves.
			frameworkElement.StartBringIntoView();
		}

		if (element is Control control)
		{
			if (frameworkElement is not null &&
				(!frameworkElement.IsLoaded ||
					frameworkElement.ActualHeight <= 0.0 ||
					VisualTreeHelper.GetParent(frameworkElement) is null))
			{
				// Keep only one pending deferred focus callback (any prior one was cleared above).
				WeakReference<TableView> weakThis = new(this);
				EventHandler<object> handler = (_, _) =>
				{
					if (weakThis.TryGetTarget(out var strongThis))
					{
						if (strongThis.m_pendingFocusLayoutToken.Disposable is not null)
						{
							// Original C++: strongThis->LayoutUpdated(strongThis->m_pendingFocusLayoutToken); strongThis->m_pendingFocusLayoutToken = {};
							strongThis.m_pendingFocusLayoutToken.Disposable = null;
						}

						if (index < 0 || index >= strongThis.GetItemsSourceCount())
						{
							return;
						}

						// TODO Uno: Locals renamed (C++ reuses repeater/element/control); C# forbids shadowing them here.
						if (strongThis.m_rowsRepeater is { } deferredRepeater)
						{
							if (deferredRepeater.GetOrCreateElement(index) is { } deferredElement)
							{
								if (deferredElement is Control deferredControl)
								{
									deferredControl.Focus(FocusState.Keyboard);
								}
							}
						}
					}
				};
				LayoutUpdated += handler;
				m_pendingFocusLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= handler);
				return true;
			}

			return control.Focus(FocusState.Keyboard);
		}
		return false;
	}
}
