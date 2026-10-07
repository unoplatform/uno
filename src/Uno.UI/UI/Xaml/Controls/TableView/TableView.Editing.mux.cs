// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Editing.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
// TODO Uno: Microsoft.UI.Xaml.Data.INotifyDataErrorInfo is projected to System.ComponentModel.INotifyDataErrorInfo in C#.
using INotifyDataErrorInfo = System.ComponentModel.INotifyDataErrorInfo;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	// The data field a column edits. Derived from the column's Binding rather than authored
	// separately: a column already knows what it is bound to, and the public "which field is this
	// column about?" concept is the base column's SortMemberPath - editing must not grow a second
	// one. Empty means "no single source property", and the control falls back to object-level
	// validation and to scraping the editor's bindings for a cancel snapshot.
	private static string ResolveEditingPropertyPath(TableViewColumn column)
	{
		if (column is TableViewTextColumn textColumn)
		{
			try
			{
				return textColumn.GetEditingPropertyPath();
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		return string.Empty;
	}

	private static bool IsColumnInColumns(TableView? owner, TableViewColumn? column)
	{
		if (owner is null || column is null)
		{
			return false;
		}

		if (owner.Columns is { } columns)
		{
			foreach (var candidate in columns)
			{
				if (candidate == column)
				{
					return true;
				}
			}
		}

		return false;
	}

	// First column a keyboard user could edit. Used only as the fallback when no current column
	// has ever been established (see TryResolveFocusedCell).
	private static TableViewColumn? FirstEditableColumn(TableView? owner)
	{
		if (owner is null)
		{
			return null;
		}

		if (owner.Columns is { } columns)
		{
			foreach (var candidate in columns)
			{
				if (candidate is not null &&
					!candidate.IsReadOnly &&
					candidate.Visibility == Visibility.Visible)
				{
					return candidate;
				}
			}
		}

		return null;
	}

#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private static object? GetBindingSnapshotSource(
		Binding? binding,
		object? fallbackDataItem)
	{
		if (binding is not null)
		{
			if (binding.Source is { } source)
			{
				return source;
			}
		}
		return fallbackDataItem;
	}

	private static string GetBindingPath(Binding? binding)
	{
		if (binding is not null)
		{
			if (binding.Path is { } path)
			{
				return path.Path;
			}
		}
		return string.Empty;
	}
#pragma warning restore IDE0051

	// TODO Uno: winrt::to_hresult() reads the in-flight exception; C# has no ambient current
	// exception, so each catch site passes it in.
	private static void LogConsumerHandlerThrow(string eventName, Exception exception)
	{
		// Retail-visible on purpose. A consumer handler that throws leaves the edit continuing
		// as though the handler had agreed, which is indistinguishable at runtime from a handler
		// that simply did nothing. Logging only under DBG meant a shipping app had no way to
		// diagnose it.
		var hr = unchecked((uint)exception.HResult);
		TVDiag.LogRetailF(
			"[TableView] Consumer %ls handler threw (HRESULT 0x%08X); edit continues as un-cancelled.",
			eventName,
			hr);
	}

	// A cancel that cannot restore the pre-edit value is a silent data-integrity failure from the
	// user's point of view: they pressed Esc and their edit stayed. Report it.
#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private static void LogRevertUnavailable(string reason)
	{
		TVDiag.LogRetailF(
			"[TableView] Cancel cannot revert this edit (%ls). Implement ITableViewEditableItem " +
			"on the data item to make rollback reliable.",
			reason);
	}
#pragma warning restore IDE0051

	// ----- Editing lifecycle (opt-in via IsReadOnly=false) -----

	// COM identity, not ABI-pointer equality: IInspectable's operator== compares raw abi pointers, so
	// the same object reached through a different interface - boxed values, projected interfaces, a
	// future wrapping layer - would not match. A member rather than a file-local helper so every
	// comparison of data items in the control uses the same one.
	internal static bool SameInspectableIdentity(object? lhs, object? rhs)
	{
		if (ReferenceEquals(lhs, rhs))
		{
			return true;
		}
		if (lhs is null || rhs is null)
		{
			return false;
		}

		try
		{
			// TODO Uno: COM identity (QueryInterface for IUnknown) is reference identity in .NET.
			// Original C++:
			// return lhs.as<winrt::Windows::Foundation::IUnknown>() ==
			//     rhs.as<winrt::Windows::Foundation::IUnknown>();
			return ReferenceEquals(lhs, rhs);
		}
		catch (Exception)
		{
			return false;
		}
	}

	// Single choke point for turning a row's container-level item into the object an edit writes to.
	// Identity today; it exists as a seam for a layer that wraps items (grouping), where the unwrap and
	// the nullptr "not editable" answer belong here rather than at every edit entry point.
	internal object? UnwrapEditingDataItem(object? item) => item;

	internal object? CurrentItem()
	{
		// The current cell tracks focus at all times, and deliberately does NOT switch to reporting the
		// edit target while an edit is open: a property whose meaning depends on hidden state would
		// stop being valid the moment the edit closes. BeginEdit moves it onto the edited cell anyway.
		return m_currentItem;
	}

	internal TableViewColumn? CurrentColumn()
	{
		var column = m_currentColumn;
		if (IsColumnInColumns(this, column))
		{
			return column;
		}
		return null;
	}

	private void SetCurrentItem(object? item) => m_currentItem = item;

	private void UpdateCurrentColumn(TableViewColumn? column)
	{
		if (IsColumnInColumns(this, column))
		{
			m_currentColumn = column;
		}
		else
		{
			m_currentColumn = null;
		}
	}

	internal void SetCurrentCell(object? item, TableViewColumn? column)
	{
		SetCurrentItem(item);
		UpdateCurrentColumn(column);
	}

	private bool RaiseBeginningEdit(object? item, TableViewColumn? column)
	{
		if (BeginningEdit is null)
		{
			return true;
		}
		var args = new TableViewBeginningEditEventArgs(item, column);
		try
		{
			BeginningEdit?.Invoke(this, args);
		}
		catch (Exception ex)
		{
			LogConsumerHandlerThrow("BeginningEdit", ex);
		}
		return !args.Cancel;
	}

	private EditEndingResult RaiseEditEnding(
		EditingUnit unit,
		TableViewEditAction action,
		bool honorCancel)
	{
		var item = m_currentEditItem;
		var column = m_currentEditColumn;

		// Raised synchronously. There is no deferral in this release, so a handler decides before it
		// returns and the veto is simply read afterwards - no sink, no completion marshalling, and no
		// way for an unclosed deferral to wedge the control at IsEditing == true.
		if (CellEditEnding is not null)
		{
			var cellArgs = new TableViewCellEditEndingEventArgs(item, column, action);
			var self = this;
			PostEditNotification(() =>
			{
				try
				{
					self.CellEditEnding?.Invoke(self, cellArgs);
				}
				catch (Exception ex)
				{
					LogConsumerHandlerThrow("CellEditEnding", ex);
				}
			});

			// honorCancel == false is a forced close - the caller is tearing the edit down now and a
			// handler must not be able to keep it open. That is also the only case in which the raise
			// above was deferred onto the dispatcher, so Cancel would not have been written yet anyway.
			if (honorCancel && cellArgs.Cancel)
			{
				return EditEndingResult.Vetoed;
			}
		}

		return EditEndingResult.Completed;
	}

	private bool TryResolveFocusedCell(out object? item, out TableViewColumn? column)
	{
		item = null;
		column = null;

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return false;
		}

		TableViewRow? focusedRow = null;
		TableViewColumn? focusedColumn = null;

		if (XamlRoot is { } root)
		{
			if (FocusManager.GetFocusedElement(root) is DependencyObject focused)
			{
				DependencyObject? node = focused;
				while (node is not null)
				{
					if (focusedColumn is null)
					{
						if (node is FrameworkElement fe)
						{
							focusedColumn = fe.Tag as TableViewColumn;
						}
					}

					if (focusedRow is null)
					{
						focusedRow = node as TableViewRow;
					}

					if (focusedRow is not null && focusedColumn is not null)
					{
						break;
					}

					node = VisualTreeHelper.GetParent(node);
				}
			}
		}

		if (focusedRow is null)
		{
			return false;
		}

		var rowIndex = repeater.GetElementIndex(focusedRow);
		if (!TryGetItemAtRowIndex(rowIndex, out item))
		{
			return false;
		}

		column = focusedColumn is not null ? focusedColumn : m_currentColumn;

		// Keyboard-only fallback. Row navigation focuses the row, not a column-tagged cell, so a user
		// who has never used the pointer has no current column and F2 would silently do nothing.
		// Defaulting to the first visible editable column makes the keyboard path reachable.
		if (column is null)
		{
			column = FirstEditableColumn(this);
		}

		if (item is null || column is null || !IsColumnInColumns(this, column))
		{
			item = null;
			column = null;
			return false;
		}

		return true;
	}

	private bool TryResolveCurrentCell(out object? item, out TableViewColumn? column)
	{
		// Focus wins over the cached current cell when they disagree. Row navigation moves focus but not
		// the current cell (there is no cell-level keyboard navigation yet), so without this, clicking
		// row 1 then arrowing to row 40 and pressing F2 would edit row 1. Preferring the focused row
		// while keeping the cached column gives the user the cell they are looking at.
		if (TryResolveFocusedCell(out var focusedItem, out var focusedColumn))
		{
			item = focusedItem;
			column = focusedColumn;
			SetCurrentCell(item, column);
			return true;
		}

		item = m_currentItem;
		column = m_currentColumn;

		if (item is not null && column is not null && IsColumnInColumns(this, column))
		{
			return true;
		}

		item = null;
		column = null;
		return false;
	}

	private bool TryResolveCurrentCellForEdit(out object? item, out TableViewColumn? column)
	{
		if (!TryResolveCurrentCell(out item, out column))
		{
			return false;
		}
		// Editing additionally requires the resolved column to be editable; a read-only
		// column is a valid current cell for navigation but cannot begin an edit.
		if (column!.IsReadOnly)
		{
			item = null;
			column = null;
			return false;
		}
		return true;
	}

	private bool TryGetItemAtRowIndex(int rowIndex, out object? item)
	{
		item = null;
		if (rowIndex < 0)
		{
			return false;
		}

		if (m_rowsRepeater is { } repeater)
		{
			if (repeater.TryGetElement(rowIndex) is TableViewRow row)
			{
				if (UnwrapEditingDataItem(row.DataContext) is { } dataItem)
				{
					item = dataItem;
					return true;
				}
			}

			if (repeater.ItemsSourceView is { } view)
			{
				if (rowIndex < view.Count)
				{
					if (UnwrapEditingDataItem(view.GetAt(rowIndex)) is { } dataItem)
					{
						item = dataItem;
						return true;
					}
				}
			}
		}

		return false;
	}

	private TableViewRow? FindRealizedRowForItem(object? item)
	{
		if (item is null)
		{
			return null;
		}

		if (m_rowsRepeater is { } repeater)
		{
			var childrenCount = VisualTreeHelper.GetChildrenCount(repeater);
			for (var childIndex = 0; childIndex < childrenCount; ++childIndex)
			{
				if (VisualTreeHelper.GetChild(repeater, childIndex) is TableViewRow row)
				{
					// ItemsRepeater keeps recycled containers parented in its pool, and a pooled row
					// still carries the DataContext of the item it last showed. Without this filter a
					// scrolled-away ghost can win the identity match below over the genuinely realized
					// row - so the visible row never gets its state, intermittently and only after
					// scrolling. GetElementIndex is >= 0 only for containers the repeater currently
					// considers realized.
					if (repeater.GetElementIndex(row) < 0)
					{
						continue;
					}

					// COM identity, not ABI-pointer equality: IInspectable's operator== compares the
					// raw abi pointers, so the same object reached through a different interface
					// (boxed values, GroupedEntry unwrapping, projected interfaces) would not match
					// and BeginEdit would fail with no diagnostic.
					if (UnwrapEditingDataItem(row.DataContext) is { } dataItem &&
						SameInspectableIdentity(dataItem, item))
					{
						return row;
					}
				}
			}
		}

		return null;
	}

	private bool TryBeginEditVisual(object? item, TableViewColumn column)
	{
		if (FindRealizedRowForItem(item) is { } row)
		{
			if (row.BeginCellEdit(column, item))
			{
				m_currentEditRow = row;
				return true;
			}
		}

		return false;
	}

	// The editor currently in the tree, or null once the edit has been torn down. Surfaced on the
	// EditEnding args so a handler can read the pending value before it is written to the source.
	internal FrameworkElement? CurrentEditingElement()
	{
		if (m_currentEditRow is { } row)
		{
			return row.GetEditingElement();
		}
		return null;
	}

	private void EndEditVisual(TableViewEditAction action)
	{
		if (m_currentEditRow is { } row)
		{
			if (m_suppressEditVisualRestore)
			{
				// Running inside a layout pass: release the row's edit state without touching the
				// tree. The row's cells are being restamped or rebuilt, so the display visual does
				// not need restoring - and doing it here would re-enter layout.
				row.AbandonCellEdit();
			}
			else
			{
				row.EndCellEdit(action);
			}
		}
		m_currentEditRow = null;
	}

	internal bool TerminateEditWithoutVisualRestore(bool insideLayoutPass = false)
	{
		if (m_editState == EditState.None)
		{
			return true;
		}

		m_suppressEditVisualRestore = true;
		m_insideLayoutPass = m_insideLayoutPass || insideLayoutPass;
		var clearLayoutFlag = insideLayoutPass;
		try
		{
			return TerminateEditForReset(true /* force */);
		}
		finally
		{
			m_suppressEditVisualRestore = false;
			if (clearLayoutFlag)
			{
				m_insideLayoutPass = false;
			}
		}
	}

	// App code must not run inside ItemsRepeater's measure. A CellEditEnding / EditEnded handler is
	// ordinary app code, and the obvious things it does - touch the tree, invalidate layout, show a
	// dialog - re-enter the pass we are standing in and trip the XAML re-entrancy fail-fast.
	//
	// Deferring them is legal precisely here: a teardown from a layout pass is always FORCED, so
	// honorCancel is false, no handler can veto it, and RaiseEditEnding never waits on a deferral in
	// that case. The edit is over either way - the notification is purely informational, so delivering
	// it one tick later changes nothing an app can observe except that it no longer crashes.
	//
	// The item write is deliberately NOT deferred: it stays synchronous so a forced close cannot lose
	// the user's value if the control goes away before the dispatcher runs.
	internal void PostEditNotification(Action? notify)
	{
		if (notify is null)
		{
			return;
		}

		if (!m_insideLayoutPass)
		{
			notify();
			return;
		}

		var weakThis = new WeakReference<TableView>(this);
		var queue = DispatcherQueue;
		if (queue is not null && queue.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out _))
				{
					notify();
				}
			}))
		{
			return;
		}

		// No dispatcher, or it is shutting down. Losing the notification silently would be worse than
		// delivering it inline: an app that tracks edit state would be wedged with no way to recover.
		notify();
	}

	private bool HasBlockingValidationErrors(
		object? item,
		TableViewColumn? column)
	{
		if (item is null)
		{
			return false;
		}

		var errorInfo = item as INotifyDataErrorInfo;
		if (errorInfo is null)
		{
			return false;
		}

		// Scope to the property this column actually writes. Object-level HasErrors lets an
		// unrelated, pre-existing error on a different property block this cell permanently -
		// the user could never leave the edit.
		var editingPath = column is not null ? ResolveEditingPropertyPath(column) : string.Empty;

		if (!string.IsNullOrEmpty(editingPath))
		{
			// GetErrors takes a property name, not a path; use the leaf segment.
			var lastDot = editingPath.LastIndexOf('.');
			var propertyName = (lastDot == -1)
				? editingPath
				: editingPath.Substring(lastDot + 1);
			try
			{
				if (errorInfo.GetErrors(propertyName) is { } errors)
				{
					foreach (var error in errors)
					{
						_ = error;
						return true;
					}
				}
				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}

		try
		{
			return errorInfo.HasErrors;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private bool FinishEditTeardown(EditingUnit unit, TableViewEditAction action, bool honorCancel)
	{
		var item = m_currentEditItem;
		var column = m_currentEditColumn;

		var editingElement = CurrentEditingElement();

		if (action == TableViewEditAction.Commit)
		{
			// A forced close already transferred the value (see TerminateEditForReset), so writing
			// again here would invoke the app's setters twice for one commit.
			var alreadyTransferred = !honorCancel;
			var wrote = alreadyTransferred;

			if (!wrote && column is not null && editingElement is not null)
			{
				try
				{
					wrote = column.CommitCellEdit(editingElement);
					m_editSourceWritten = m_editSourceWritten || wrote;
				}
				catch (Exception ex)
				{
					LogConsumerHandlerThrow("CommitCellEdit", ex);
					wrote = false;
				}
			}

			if (!wrote)
			{
				// Nothing reached the item - a setter threw, or no writable binding could be resolved.
				// Reporting success here is silent data loss, so the edit stays open instead.
				//
				// Logged because "the editor will not close" is otherwise indistinguishable from a
				// hang. The common cause is an editor the base cannot see: GetBindingExpression only
				// finds classic {Binding}, so a CellEditingTemplate using {x:Bind}, or any editor
				// outside the built-in property list, must override CommitCellEditCore.
				TVDiag.LogRetailF(
					"[TableView] Commit wrote nothing, so the edit stays open. If this column uses a " +
					"CellEditingTemplate, override CommitCellEditCore - compiled {x:Bind} bindings and " +
					"custom editors are not discoverable by the base implementation.");
				return false;
			}

			// Validate AFTER the write - the pending value lives in the editor until the column
			// commits, so validating before it would only ever re-check the pre-edit value.
			if (honorCancel && HasBlockingValidationErrors(item, column))
			{
				if (column is not null && editingElement is not null)
				{
					// Undo the write. CancelCellEdit restores the EDITOR, and because the editing
					// binding is Explicit that alone never reaches the item - so commit the restored
					// value to push the pre-edit value back to the source. Without this second write
					// the item keeps the value the control just rejected, and the user's typed text is
					// replaced in the editor as well: invalid data on the item, nothing on screen.
					column.CancelCellEdit(editingElement, m_editUneditedValue);

					if (!column.CommitCellEdit(editingElement))
					{
						TVDiag.LogRetailF(
							"[TableView] A rejected value could not be rolled back; the item still holds it.");
					}
					m_editSourceWritten = false;
				}
				return false;
			}
		}
		else
		{
			// Cancel. The column reverts just this editor, leaving cells already committed in
			// the row alone.
			if (column is not null && editingElement is not null)
			{
				// Only touch the editor when the source actually needs repairing.
				//
				// A plain cancel never reached the item - the editing binding is Explicit - and the
				// editor is torn out of the tree a few lines below, so restoring its text achieves
				// nothing visible. It is also actively harmful: mutating an in-tree editor's Text
				// changes the cell's desired width, which re-enters column width resolution during
				// teardown and fail-fasts with 0xc0000420 in an Auto-width column.
				//
				// The one case that DOES need it: a commit already wrote to the item and validation
				// then rejected the value, leaving the edit open. There the editor has to be restored
				// and re-committed so the item stops holding a value the control rejected.
				if (m_editSourceWritten)
				{
					try
					{
						column.CancelCellEdit(editingElement, m_editUneditedValue);
						column.CommitCellEdit(editingElement);
					}
					catch (Exception ex)
					{
						LogConsumerHandlerThrow("CancelCellEdit", ex);
					}

					m_editSourceWritten = false;
				}
			}
		}

		EndEditVisual(action);
		m_editUneditedValue = null;

		m_currentEditItem = null;
		m_currentEditColumn = null;
		return true;
	}

	private bool CompleteEditEnd(EditingUnit unit, TableViewEditAction action, bool honorCancel, bool vetoed)
	{
		if (m_editState != EditState.Ending)
		{
			return false;
		}

		if (vetoed)
		{
			// Ending -> Editing: a handler kept the edit open.
			m_editState = EditState.Editing;
			ClearCoalescedEditReshape();
			return false;
		}

		var generation = m_editGeneration;

		var closed = FinishEditTeardown(unit, action, honorCancel);

		// A rollback write inside FinishEditTeardown raises PropertyChanged, which can re-enter layout
		// and drive a forced teardown (row rebuild / recycle) underneath this frame. That teardown has
		// already closed the edit and torn the editor out of the tree, so resurrecting the state here
		// would leave the control reporting IsEditing with no editor.
		if (m_editGeneration != generation)
		{
			return false;
		}

		m_editState = closed ? EditState.None : EditState.Editing;

		if (closed)
		{
			DrainCoalescedEditReshape();
		}
		else
		{
			ClearCoalescedEditReshape();
		}

		return closed;
	}

	private bool EndCurrentEdit(EditingUnit unit, TableViewEditAction action, bool honorCancel)
	{
		// Only an open, interactive edit can be closed: Beginning and Ending are both rejected here, so
		// a consumer callback cannot re-enter teardown.
		//
		// The exception is a forced close (honorCancel == false) while a previous close waits on a
		// deferral. That edit is already logically over and cannot be vetoed, so the caller must be able
		// to finish it now - otherwise an app that never closes its deferral wedges the control at
		// IsEditing == true and a recycled row keeps a live editor over a different item.
		//
		// The ending events are NOT raised again for it: the app has already seen CellEditEnding for
		// this edit, and a second one - potentially reporting a different action - would look like two
		// separate edits closing. The pending deferral is abandoned via the generation bump and the
		// teardown runs directly.
		if (m_editState == EditState.Ending && !honorCancel)
		{
			// Any late deferral completion must not run teardown a second time. The state is already
			// Ending, which is what CompleteEditEnd requires, so run the teardown directly.
			++m_editGeneration;

			var closed = CompleteEditEnd(unit, action, honorCancel, false /* vetoed */);
			if (closed)
			{
				DrainCoalescedEditReshape();
			}
			return closed;
		}

		if (m_editState != EditState.Editing)
		{
			return false;
		}

		m_editState = EditState.Ending;

		var result = RaiseEditEnding(unit, action, honorCancel);

		return CompleteEditEnd(unit, action, honorCancel, result == EditEndingResult.Vetoed);
	}

	internal bool TerminateEditForReset(bool force)
	{
		if (m_editState == EditState.None)
		{
			return true;
		}

		// Forced teardown (source-driven reset / ItemsSource replacement / unload) cannot be
		// vetoed: commit the pending value if it is valid, otherwise report Cancel and restore
		// the edit snapshot. Control-initiated (non-forced) reshapes stay cancelable.
		var action = TableViewEditAction.Commit;
		if (force)
		{
			// Transfer the pending editor value to the source BEFORE validating. The editing
			// bindings use UpdateSourceTrigger::Explicit, so until this runs the typed value is
			// still sitting in the editor and validation would only ever re-check the pre-edit
			// value - and because a forced close passes honorCancel=false, the post-write
			// validation in FinishEditTeardown is skipped. Without this order a forced teardown
			// silently commits a value the control itself considers invalid.
			//
			// FinishEditTeardown must not repeat this write; it detects the forced case via
			// honorCancel == false.
			var wrote = false;
			if (m_currentEditColumn is { } column)
			{
				if (CurrentEditingElement() is { } editingElement)
				{
					try
					{
						wrote = column.CommitCellEdit(editingElement);
					}
					catch (Exception ex)
					{
						LogConsumerHandlerThrow("CommitCellEdit", ex);
					}
				}
			}

			if (!wrote || HasBlockingValidationErrors(m_currentEditItem, m_currentEditColumn))
			{
				// Cancel: FinishEditTeardown's non-commit path reverts the editor, undoing the
				// transfer above.
				action = TableViewEditAction.Cancel;
			}
		}

		return EndCurrentEdit(
			EditingUnit.Row,
			action,
			!force);
	}

	internal bool TryTerminateEditForControlInitiatedReshape()
	{
		if (m_editState == EditState.None)
		{
			return true;
		}

		// Already inside a transition (a consumer callback, or a deferral in flight): the reshape
		// cannot proceed now. The caller queues it and it is replayed once the edit closes.
		if (m_editState != EditState.Editing)
		{
			return false;
		}

		var terminated = TerminateEditForReset(false /* force */);
		if (!terminated)
		{
			ClearCoalescedEditReshape();
			return false;
		}

		// Reentrant reshape requests raised by edit-ending handlers are queued and drained by the
		// outer operation after it applies. This preserves the original outer intent (A) and then
		// replays reentrant intents (B, C...) in arrival order.
		return terminated;
	}

	internal void QueueCoalescedEditReshape(Action? operation)
	{
		if (operation is not null)
		{
			m_pendingEditReshapes.Enqueue(operation);
		}
	}

	internal void ClearCoalescedEditReshape() => m_pendingEditReshapes.Clear();

	internal void DrainCoalescedEditReshape()
	{
		// Never replay inside a layout pass. The queued operations are app-supplied callables that
		// reshape the source; running one from ItemsRepeater's element-clearing/measure callback
		// re-enters the repeater mid-pass. m_suppressEditVisualRestore is exactly the "we are inside
		// layout" signal, so it gates this too.
		if (m_suppressEditVisualRestore)
		{
			return;
		}

		if (m_isApplyingCoalescedEditReshape || m_pendingEditReshapes.Count == 0)
		{
			return;
		}

		m_isApplyingCoalescedEditReshape = true;
		try
		{
			// Operations are opaque callables, so editing has no compile-time knowledge of sorting,
			// grouping or row expansion; the caller that deferred the work supplies the intent.
			while (m_pendingEditReshapes.Count != 0)
			{
				var operation = m_pendingEditReshapes.Dequeue();
				if (operation is not null)
				{
					operation();
				}
			}
		}
		finally
		{
			m_isApplyingCoalescedEditReshape = false;
		}
	}

	internal bool BeginEdit()
	{
		if (!TryResolveCurrentCellForEdit(out var item, out var column))
		{
			return false;
		}

		return BeginEdit(item, column);
	}

	internal bool BeginEdit(object? item, TableViewColumn? column)
	{
		if (IsReadOnly || item is null || column is null || column.IsReadOnly || !IsColumnInColumns(this, column))
		{
			return false;
		}

		// Reject reentrancy from inside a consumer callback, and while a deferred close is in flight.
		if (m_editState == EditState.Beginning || m_editState == EditState.Ending)
		{
			return false;
		}

		if (m_editState == EditState.Editing)
		{
			// Moving commits the open edit. WPF parity: leaving the *row* is what ends the row-level
			// transaction, so moving to a different item commits with unit Row - which is what raises
			// RowEditEnding and calls ITableViewEditableItem.EndEdit at row scope. Moving between cells
			// of the same row stays a Cell-unit commit.
			//
			// A veto, a validation failure or a deferral all leave the existing edit open, so the move
			// must not proceed.
			var movingToAnotherItem =
				!SameInspectableIdentity(m_currentEditItem, item);

			var unit = movingToAnotherItem
				? EditingUnit.Row
				: EditingUnit.Cell;

			if (!CommitEditInternal(unit))
			{
				return false;
			}
		}

		m_currentEditItem = item;
		m_currentEditColumn = column;
		m_editState = EditState.Beginning;
		m_abandonPendingBeginEdit = false;
		m_editSourceWritten = false;

		// Everything from here to the promotion below runs app code - a BeginningEdit handler, the
		// editing binding's getter, ITableViewEditableItem.BeginEdit - any of which can mutate the
		// collection and have ItemsRepeater recycle the row underneath us. A forced teardown cannot
		// close an edit that is still Beginning, so OnRowElementClearing raises this flag instead and
		// the checks below unwind rather than promoting to Editing over a recycled row.
		var allowed = RaiseBeginningEdit(item, column);
		if (!allowed || m_abandonPendingBeginEdit || !TryBeginEditVisual(item, column))
		{
			// Beginning -> None. Do not leave stale trackers behind, and reset the state before
			// draining so a queued reshape can itself start an edit.
			//
			// The item transaction is deliberately NOT touched: a cell commit keeps the row
			// transaction open, so anything held here belongs to a previous cell in the same row and
			// is still legitimately live. Clearing it would leave the item with an unpaired BeginEdit.
			m_currentEditItem = null;
			m_currentEditColumn = null;
			m_editUneditedValue = null;
			m_editState = EditState.None;
			m_abandonPendingBeginEdit = false;
			DrainCoalescedEditReshape();
			return false;
		}

		// The column primes its own editor and hands back whatever it needs to revert. Done here rather
		// than in the row so the returned value has an owner for the lifetime of the edit.
		if (m_currentEditRow is { } row)
		{
			if (row.GetEditingElement() is { } editingElement)
			{
				try
				{
					m_editUneditedValue = column.PrepareCellForEdit(editingElement, null);
				}
				catch (Exception ex)
				{
					LogConsumerHandlerThrow("PrepareCellForEdit", ex);
				}
			}
		}

		// Last chance to unwind: PrepareCellForEdit calls app code, so
		// the row can have been recycled between the check above and here. Promoting to Editing over a
		// recycled row is what lets a later commit write into a different item.
		if (m_abandonPendingBeginEdit || m_currentEditRow is null)
		{
			m_abandonPendingBeginEdit = false;
			m_currentEditItem = null;
			m_currentEditColumn = null;
			m_editUneditedValue = null;
			m_editState = EditState.None;
			DrainCoalescedEditReshape();
			return false;
		}

		SetCurrentCell(item, column);
		m_editState = EditState.Editing;
		DrainCoalescedEditReshape();
		return true;
	}

	/// <summary>
	/// Closes the open cell edit, writing the value back.
	/// </summary>
	/// <returns>
	/// false if there is no open edit, a handler vetoed the close, validation rejected the value, or
	/// the column could not write the value; otherwise true.
	/// </returns>
	public bool CommitEdit() => CommitEditInternal(EditingUnit.Cell);

	internal bool CommitEditInternal(EditingUnit unit) => EndCurrentEdit(unit, TableViewEditAction.Commit, true /* honorCancel */);

	/// <summary>
	/// Closes the open cell edit, discarding the editor so the display cell re-reads the unchanged source.
	/// </summary>
	/// <returns>
	/// false if there is no open edit or a handler vetoed the close; otherwise true.
	/// </returns>
	public bool CancelEdit() => CancelEditInternal(EditingUnit.Cell);

	internal bool CancelEditInternal(EditingUnit unit)
	{
		// Normal cancel is cancelable per spec (only FORCED teardown is non-cancelable):
		// a CellEditEnding handler may veto the cancel and keep the edit open.
		return EndCurrentEdit(unit, TableViewEditAction.Cancel, true /* honorCancel */);
	}
}
