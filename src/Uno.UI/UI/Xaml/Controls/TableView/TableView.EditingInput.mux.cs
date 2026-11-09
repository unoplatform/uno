// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_EditingInput.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Input gestures that drive editing.
//
// Kept separate from TableView_Editing.cpp: that file owns the edit state machine, reachable
// entirely through BeginEdit/CommitEdit/CancelEdit; this one owns the policy of which gestures map
// onto that API. The split lets a future keyboard or selection layer re-route gestures without
// reopening the state machine, and keeps the state machine testable without synthesizing input.

partial class TableView
{
	// True when `candidate` is `ancestor` or sits underneath it, including the ancestor's own
	// popups (a ComboBox editor's dropdown items).
	private static bool IsWithinElement(object? candidate, FrameworkElement? ancestor)
	{
		if (ancestor is null)
		{
			return false;
		}

		return TableViewDetails.IsWithinElementPopupAware(candidate, ancestor, ancestor.XamlRoot);
	}

	// An open flyout owned by the editor or one of its descendants: a ContextFlyout, or a text
	// control's SelectionFlyout. Its popup is not in the visual tree, so focus inside it is not
	// within the editor.
	private static FlyoutBase? FindOpenEditorFlyout(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			if (uiElement.ContextFlyout is { IsOpen: true } contextFlyout)
			{
				return contextFlyout;
			}
		}

		FlyoutBase? selectionFlyout = null;
		if (element is TextBox textBox)
		{
			selectionFlyout = textBox.SelectionFlyout;
		}
		else if (element is RichEditBox richEditBox)
		{
			selectionFlyout = richEditBox.SelectionFlyout;
		}
		if (selectionFlyout is { IsOpen: true })
		{
			return selectionFlyout;
		}

		int count = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < count; ++i)
		{
			if (FindOpenEditorFlyout(VisualTreeHelper.GetChild(element, i)) is { } flyout)
			{
				return flyout;
			}
		}

		return null;
	}

	// The open popup that is or holds `element`. Flyouts that are not reachable from the editor's
	// properties (a DatePicker's picker, a Button.Flyout, an attached flyout) surface only here.
	// While a flyout opens, focus can rest on its Popup before reaching the content.
	private static Popup? FindOpenPopupContaining(object? element, XamlRoot? root)
	{
		if (element is null || root is null)
		{
			return null;
		}

		var elementAsPopup = element as Popup;
		foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root))
		{
			if (popup == elementAsPopup)
			{
				return popup;
			}
			if (popup.Child is FrameworkElement child && IsWithinElement(element, child))
			{
				return popup;
			}
		}

		return null;
	}

	// An open Button.Flyout, SplitButton.Flyout or attached flyout under the editor (a
	// CalendarDatePicker's calendar is attached in its template).
	private static bool HasOpenOwnedFlyout(DependencyObject element)
	{
		static bool isOpen(FlyoutBase? flyout) => flyout is { IsOpen: true };

		if (element is FrameworkElement fe && isOpen(FlyoutBase.GetAttachedFlyout(fe)))
		{
			return true;
		}
		if (element is Button button && isOpen(button.Flyout))
		{
			return true;
		}
		if (element is SplitButton splitButton && isOpen(splitButton.Flyout))
		{
			return true;
		}

		int count = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < count; ++i)
		{
			if (HasOpenOwnedFlyout(VisualTreeHelper.GetChild(element, i)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsElementOfType<T>(DependencyObject element)
	{
		if (element is T)
		{
			return true;
		}

		int count = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < count; ++i)
		{
			if (ContainsElementOfType<T>(VisualTreeHelper.GetChild(element, i)))
			{
				return true;
			}
		}
		return false;
	}

	// True when an open popup outside the editor's tree was opened by the editor. A popup has no
	// public link to its owner, so only the editor's own flyouts count; a ContentDialog, a
	// TeachingTip or another table's menu does not.
	private static bool IsEditorOwnedPopup(Popup popup, FrameworkElement editor)
	{
		// Only one top-level flyout is open per root, so an open flyout under the editor is this one.
		if (HasOpenOwnedFlyout(editor))
		{
			return true;
		}

		// A DatePicker/TimePicker shows its picker flyout from code, out of reach of the walk above.
		var child = popup.Child;
		if (child is DatePickerFlyoutPresenter)
		{
			return ContainsElementOfType<DatePicker>(editor);
		}
		if (child is TimePickerFlyoutPresenter)
		{
			return ContainsElementOfType<TimePicker>(editor);
		}
		return false;
	}

	// F2 / Enter / Escape. Registered WITH handledEventsToo, because a single-line TextBox reports Enter
	// as handled and the commit must still run. Each case therefore owns its own Handled policy
	// explicitly: F2 and Escape defer to an editor that genuinely consumed the key, Enter does not.
	private void OnKeyDownForEditing(
		object sender,
		KeyRoutedEventArgs args)
	{
		switch (args.Key)
		{
			case VirtualKey.F2:
				// Keyboard equivalent of double-click, on the cell the user navigated to. Only when the
				// key is still unhandled: F2 belongs to whatever focused control claimed it first.
				if (!args.Handled && !IsEditing && BeginEdit())
				{
					args.Handled = true;
				}
				break;

			case VirtualKey.Enter:
				if (IsEditing)
				{
					// Acted on even if the editor marked it handled: a single-line TextBox reports Enter as
					// handled, which would leave the editor with no way to close from the keyboard.
					// Handled regardless of the result - a veto or pending deferral still owns the key, and
					// letting it bubble would scroll the table under an open editor.
					CommitEdit();
					args.Handled = true;
				}
				break;

			case VirtualKey.Escape:
				// Like F2, this defers to an editor that genuinely consumed the key - a ComboBox in a
				// CellEditingTemplate swallows Escape to close its popup, and that must not also cancel
				// and roll back the whole cell edit. The same goes for the editor's own menu: Escape
				// light-dismisses it and focus is back in the editor by the time the key routes there.
				if (!args.Handled && IsEditing && !m_editorFlyoutClosing)
				{
					CancelEdit();
					args.Handled = true;
				}
				break;

			default:
				break;
		}
	}

	// Click-away / tab-away commit. Without it the editor stays open when focus leaves, and a second
	// edit elsewhere leaves two editors on screen. Committing matches the platform grid convention.
	//
	// The decision is deliberately NOT made here. Opening an editor produces a burst of focus traffic,
	// so a synchronous commit on the first LosingFocus closes the edit in the gesture that opened it.
	// The check is posted and re-evaluated once focus has settled.
	private void OnLosingFocusForEditing(
		object sender,
		LosingFocusEventArgs args)
	{
		if (!IsEditing)
		{
			return;
		}

		var row = m_currentEditRow;
		if (row is null)
		{
			return;
		}

		var editingElement = row.GetEditingElement();
		if (editingElement is null)
		{
			return;
		}

		// Focus must actually be leaving the editor, and not merely moving within it (a ComboBox
		// opening its popup, or a template column containing several controls).
		if (!IsWithinElement(args.OldFocusedElement, editingElement) ||
			IsWithinElement(args.NewFocusedElement, editingElement))
		{
			return;
		}

		if (m_focusLossCommitQueued)
		{
			return;
		}

		QueueFocusLossCommit();
	}

	private void QueueFocusLossCommit()
	{
		m_focusLossCommitQueued = true;

		var weakThis = new WeakReference<TableView>(this);
		var queued = false;
		if (DispatcherQueue is { } dispatcher)
		{
			queued = dispatcher.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.CompleteFocusLossCommit();
				}
			});
		}

		if (!queued)
		{
			// Nothing will clear the flag for us. Leaving it set would silently disable focus-loss
			// commit for the rest of the control's life.
			m_focusLossCommitQueued = false;
		}
	}

	private void CompleteFocusLossCommit()
	{
		m_focusLossCommitQueued = false;
		m_editorFlyoutClosedRevoker.Disposable = null;
		m_editorFlyoutClosingRevoker.Disposable = null;
		m_editorPopupClosedRevoker.Disposable = null;

		if (!IsEditing)
		{
			return;
		}

		var row = m_currentEditRow;
		if (row is null)
		{
			return;
		}

		var editingElement = row.GetEditingElement();
		if (editingElement is null)
		{
			return;
		}

		// Focus may have come back to the editor while this was queued - which is exactly what the
		// open-an-editor gesture itself does. Only a genuine move away closes the edit.
		var root = XamlRoot;
		object? focused = null;
		if (root is not null)
		{
			focused = FocusManager.GetFocusedElement(root);

			if (IsWithinElement(focused, editingElement))
			{
				return;
			}

			// Refocus the editor only when focus lands on the row/cell host from the opening gesture;
			// hosted controls in other cells remain valid focus targets.
			var editingCell = row.GetEditingCellWrapper();
			if (ReferenceEquals(focused, row) ||
				(editingCell is not null && ReferenceEquals(focused, editingCell)))
			{
				editingElement.Focus(FocusState.Programmatic);
				return;
			}
		}

		// Focus went into the editor's own menu (a TextBox's copy/paste flyout): the user is still
		// working in the editor. Decide again once the menu closes - it hands focus back.
		if (FindOpenEditorFlyout(editingElement) is { } flyout)
		{
			// Escape light-dismisses the menu before the key is routed, so the editor then receives
			// that same Escape. Mark the close so the key handler leaves the edit alone.
			var weakClosing = new WeakReference<TableView>(this);
			TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> closingHandler = (_, _) =>
			{
				if (weakClosing.TryGetTarget(out var strongThis))
				{
					strongThis.MarkEditorFlyoutClosing();
				}
			};
			flyout.Closing += closingHandler;
			m_editorFlyoutClosingRevoker.Disposable = Disposable.Create(() => flyout.Closing -= closingHandler);

			var weakClosed = new WeakReference<TableView>(this);
			EventHandler<object> closedHandler = (_, _) =>
			{
				if (weakClosed.TryGetTarget(out var strongThis))
				{
					strongThis.m_editorFlyoutClosingRevoker.Disposable = null;
					strongThis.m_editorFlyoutClosedRevoker.Disposable = null;
					if (!strongThis.m_focusLossCommitQueued)
					{
						strongThis.QueueFocusLossCommit();
					}
				}
			};
			flyout.Closed += closedHandler;
			m_editorFlyoutClosedRevoker.Disposable = Disposable.Create(() => flyout.Closed -= closedHandler);
			return;
		}

		// Focus went into a popup the editor opened - a picker or a button flyout inside it. Decide again
		// once it closes. Focus in any other popup is a genuine move away and commits now.
		if (FindOpenPopupContaining(focused, root) is { } popup && IsEditorOwnedPopup(popup, editingElement))
		{
			var weakPopup = new WeakReference<TableView>(this);
			EventHandler<object> popupClosedHandler = (_, _) =>
			{
				if (weakPopup.TryGetTarget(out var strongThis))
				{
					strongThis.m_editorPopupClosedRevoker.Disposable = null;
					strongThis.MarkEditorFlyoutClosing();
					if (!strongThis.m_focusLossCommitQueued)
					{
						strongThis.QueueFocusLossCommit();
					}
				}
			};
			popup.Closed += popupClosedHandler;
			m_editorPopupClosedRevoker.Disposable = Disposable.Create(() => popup.Closed -= popupClosedHandler);
			return;
		}

		CommitEdit();
	}

	private void MarkEditorFlyoutClosing()
	{
		// Lives only until the current input has been dispatched.
		m_editorFlyoutClosing = true;
		var weakThis = new WeakReference<TableView>(this);
		var dispatcher = DispatcherQueue;
		if (dispatcher is null || !dispatcher.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_editorFlyoutClosing = false;
				}
			}))
		{
			m_editorFlyoutClosing = false;
		}
	}
}
