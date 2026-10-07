// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_EditingInput.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
	// True when `candidate` is `ancestor` or sits underneath it.
	private static bool IsWithinElement(object? candidate, FrameworkElement? ancestor)
	{
		if (ancestor is null)
		{
			return false;
		}

		var current = candidate as DependencyObject;
		while (current is not null)
		{
			if (current == ancestor)
			{
				return true;
			}
			current = VisualTreeHelper.GetParent(current);
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
				// and roll back the whole cell edit.
				if (!args.Handled && IsEditing)
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
		if (XamlRoot is { } root)
		{
			var focused = FocusManager.GetFocusedElement(root);

			if (IsWithinElement(focused, editingElement))
			{
				return;
			}

			// Focus settling on the ROW CONTAINER itself is the tail of the gesture that opened the
			// editor - the row takes pointer focus before the editor does - so re-focus the editor
			// rather than closing an edit the user just started.
			//
			// Deliberately the container only, not its subtree: a Button, ComboBox or hyperlink in
			// another cell of the same row is a genuine focus target, and stealing focus back from it
			// would make those controls unusable while an edit is open.
			if (ReferenceEquals(focused, row))
			{
				editingElement.Focus(FocusState.Programmatic);
				return;
			}
		}

		CommitEdit();
	}
}
