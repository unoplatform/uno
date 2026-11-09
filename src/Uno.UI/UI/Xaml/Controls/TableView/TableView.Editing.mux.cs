// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Editing.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
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

	// Retail-visible like a consumer handler's throw, but an editing hook is TableView's own column code.
	private static void LogColumnHookThrow(string hookName, Exception exception)
	{
		var hr = unchecked((uint)exception.HResult);
		TVDiag.LogRetailF("[TableView] Column %ls threw (HRESULT 0x%08X).", hookName, hr);
	}

	// A cancel that cannot restore the pre-edit value is a silent data-integrity failure from the
	// user's point of view: they pressed Esc and their edit stayed. Report it.
#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private static void LogRevertUnavailable(string reason)
	{
		TVDiag.LogRetailF(
			"[TableView] Cancel cannot revert this edit (%ls). Buffer the editing binding with " +
			"UpdateSourceTrigger=Explicit so a cancelled value never reaches the data item.",
			reason);
	}
#pragma warning restore IDE0051

	// Value-typed properties come back as a fresh box on every read, so compare those by value.
	// Returns nullopt when the values can't be compared (enums, structs, managed-only value types
	// such as decimal, or other non-identical objects), since a fresh box can't be told apart from
	// a newer write.
	private static bool? SameEditValue(object? left, object? right)
	{
		if (SameInspectableIdentity(left, right))
		{
			return true;
		}
		if (left is null || right is null)
		{
			return false;
		}

		// TODO Uno: IPropertyValue projection, see ValueConversionHelpers.TryGetPropertyType.
		if (!ValueConversionHelpers.TryGetPropertyType(left, out var leftType) ||
			!ValueConversionHelpers.TryGetPropertyType(right, out var rightType))
		{
			return null;
		}
		if (leftType != rightType)
		{
			return false;
		}

		switch (leftType)
		{
			case PropertyType.Boolean: return (bool)left == (bool)right;
			case PropertyType.Char16: return (char)left == (char)right;
			case PropertyType.UInt8: return (byte)left == (byte)right;
			case PropertyType.Int16: return (short)left == (short)right;
			case PropertyType.UInt16: return (ushort)left == (ushort)right;
			case PropertyType.Int32: return (int)left == (int)right;
			case PropertyType.UInt32: return (uint)left == (uint)right;
			case PropertyType.Int64: return (long)left == (long)right;
			case PropertyType.UInt64: return (ulong)left == (ulong)right;
			case PropertyType.Single: return (float)left == (float)right;
			case PropertyType.Double: return (double)left == (double)right;
			case PropertyType.String: return (string)left == (string)right;
			case PropertyType.Guid: return (Guid)left == (Guid)right;
			case PropertyType.DateTime: return (DateTimeOffset)left == (DateTimeOffset)right;
			case PropertyType.TimeSpan: return (TimeSpan)left == (TimeSpan)right;
			default: return null;
		}
	}

	// Records whether the edited item raised PropertyChanged for the edited property between a
	// posted rollback being queued and running. The handler holds this weakly, so the posted work
	// owns it and dropping that work revokes the subscription.
	// TODO Uno: C++ revokes in the destructor; C# has no deterministic destruction, so a dropped
	// posted rollback leaves the (weak) handler subscribed until the item goes away.
	private sealed class RollbackSupersededWatch
	{
		public global::System.ComponentModel.INotifyPropertyChanged? Source;
		public global::System.ComponentModel.PropertyChangedEventHandler? Handler;
		public volatile bool Superseded;

		public void Revoke()
		{
			if (Source is { } source)
			{
				Source = null;
				try { source.PropertyChanged -= Handler; }
				catch (Exception) { }
			}
		}
	}

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

	private bool RaiseBeginningEdit(object? item, TableViewColumn? column, ActionTip tip)
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
			tip.RecoveredAppFault(TableViewTip.AppFaultKind.Callback, ex.HResult);
			LogConsumerHandlerThrow("BeginningEdit", ex);
		}
		return !args.Cancel;
	}

	private EditEndingResult RaiseEditEnding(
		EditingUnit unit,
		TableViewEditAction action,
		bool honorCancel,
		ActionTip? tip)
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
			var lifetime = m_tipLifetimeGeneration;
			var operation = action == TableViewEditAction.Commit ?
				TableViewTip.Operation.CommitEdit : TableViewTip.Operation.CancelEdit;
			var postedTip = m_insideLayoutPass ? null : tip;
			PostEditNotification(() =>
			{
				try
				{
					self.CellEditEnding?.Invoke(self, cellArgs);
				}
				catch (Exception ex)
				{
					if (lifetime == self.m_tipLifetimeGeneration)
					{
						if (postedTip is not null) { postedTip.RecoveredAppFault(TableViewTip.AppFaultKind.Callback, ex.HResult); }
						else
						{
							self.NoteRecoveredAppFault(operation, TableViewTip.Stage.Editing,
								TableViewTip.AppFaultKind.Callback, ex.HResult);
						}
					}
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

		// With focus on the row rather than a cell there is no current column, and F2 would silently
		// do nothing.
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
		// Focus wins over the cached current cell when they disagree, so clicking row 1, arrowing to
		// row 40 and pressing F2 edits the focused cell rather than row 1.
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

	private void EndEditVisual(TableViewEditAction action, ActionTip? tip)
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
				StrongBox<int?> displayFailure = new();
				StrongBox<bool> applicationFault = new();
				row.EndCellEdit(action, tip is not null ? displayFailure : null, applicationFault);
				if (displayFailure.Value is { } failure && tip is not null)
				{
					// The edit already landed and the row fell back to its parked element.
					if (applicationFault.Value) { tip.RecoveredAppFault(TableViewTip.AppFaultKind.Factory, failure); }
					else { tip.Fail(failure); }
				}
			}
		}
		m_currentEditRow = null;
		m_editorFlyoutClosedRevoker.Disposable = null;
		m_editorFlyoutClosingRevoker.Disposable = null;
		m_editorPopupClosedRevoker.Disposable = null;
	}

	internal bool TerminateEditWithoutVisualRestore(bool insideLayoutPass = false, bool sourceReplaced = false)
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
			return TerminateEditForReset(true /* force */, sourceReplaced);
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
		TableViewColumn? column,
		ActionTip? tip = null)
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
		// A validation read that throws is the app's; the edit treats it as no error and carries on.
		void recoverValidationFault(int error)
		{
			if (tip is not null) { tip.RecoveredAppFault(TableViewTip.AppFaultKind.Validation, error); }
			else
			{
				NoteRecoveredAppFault(TableViewTip.Operation.CommitEdit, TableViewTip.Stage.Editing,
					TableViewTip.AppFaultKind.Validation, error);
			}
		}

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
			catch (Exception ex)
			{
				recoverValidationFault(ex.HResult);
				return false;
			}
		}

		try
		{
			return errorInfo.HasErrors;
		}
		catch (Exception ex)
		{
			recoverValidationFault(ex.HResult);
			return false;
		}
	}

	private bool FinishEditTeardown(EditingUnit unit, TableViewEditAction action, bool honorCancel, ActionTip? tip)
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
			var ownWriteFailed = false;

			if (!wrote && column is not null && editingElement is not null)
			{
				try
				{
					wrote = column.CommitCellEdit(editingElement);
					m_editSourceWritten = m_editSourceWritten || wrote;
				}
				catch (Exception ex)
				{
					// The editing hooks are TableView's code for every column; a value the app rejected returns false
					// instead of throwing.
					var error = ex.HResult;
					ownWriteFailed = true;
					if (tip is not null) { tip.Fail(error); }
					LogColumnHookThrow("CommitCellEdit", ex);
					wrote = false;
				}
			}

			if (!wrote)
			{
				if (tip is not null)
				{
					if (column is null || editingElement is null) { tip.Ignore(TableViewTip.IgnoreReason.Stale); }
					else if (!ownWriteFailed) { tip.ApplicationFault(TableViewTip.AppFaultKind.Write); }
				}
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
			if (honorCancel && HasBlockingValidationErrors(item, column, tip))
			{
				if (tip is not null) { tip.Ignore(TableViewTip.IgnoreReason.ValidationRejected); }
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
						// The rejection stands either way; the app's failed write is reported with it.
						if (tip is not null) { tip.RecoveredAppFault(TableViewTip.AppFaultKind.Write); }
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
				// then rejected the value, leaving the edit open, or a forced teardown transferred a
				// value that failed validation. There the editor has to be restored
				// and re-committed so the item stops holding a value the control rejected.
				if (m_editSourceWritten)
				{
					if (m_insideLayoutPass)
					{
						// Restoring the editor here is the layout re-entrancy above, so post it. By then
						// the editor is detached and has no inherited DataContext, so pin it to our item.
						//
						// Snapshot the rejected value now: if the item no longer holds it when the
						// rollback runs, something newer (another edit, an app or live write) owns the
						// property and restoring the pre-edit value would overwrite it.
						var weakThis = new WeakReference<TableView>(this);
						var uneditedValue = m_editUneditedValue;
						var editingPath = ResolveEditingPropertyPath(column);
						var resolver = new SortMemberPathResolver(editingPath);
						var rejectedValue = resolver.Resolve(item);
						// The snapshot cannot tell a fresh box from a newer write for enums, structs or
						// an empty path, so also watch the item: any change to the edited property
						// before the rollback runs supersedes it. Revoked when the posted work runs or
						// is dropped.
						var watch = new RollbackSupersededWatch();
						if (item is global::System.ComponentModel.INotifyPropertyChanged observable)
						{
							var anyProperty = editingPath.Length == 0 || editingPath.IndexOfAny(['.', '[', '(']) != -1;
							var weakWatch = new WeakReference<RollbackSupersededWatch>(watch);
							try
							{
								global::System.ComponentModel.PropertyChangedEventHandler handler = (_, args) =>
								{
									if (weakWatch.TryGetTarget(out var strongWatch))
									{
										var changed = args.PropertyName;
										if (anyProperty || string.IsNullOrEmpty(changed) || changed == editingPath)
										{
											strongWatch.Superseded = true;
										}
									}
								};
								observable.PropertyChanged += handler;
								watch.Handler = handler;
								watch.Source = observable;
							}
							catch (Exception)
							{
							}
						}
						var rollbackColumn = column;
						var rollbackEditor = editingElement;
						var queue = DispatcherQueue;
						if (queue is null || !queue.TryEnqueue(() =>
							{
								// Before the rollback write, which raises PropertyChanged itself.
								watch.Revoke();

								// Column, editor and item are held strongly, so the rollback still runs
								// after the table is gone; only the newer-edit check needs the table.
								var newerEditOpen = false;
								if (weakThis.TryGetTarget(out var strongThis))
								{
									newerEditOpen =
										strongThis.m_editState != EditState.None &&
										strongThis.m_currentEditColumn == rollbackColumn &&
										SameInspectableIdentity(strongThis.m_currentEditItem, item);
								}
								if (newerEditOpen || watch.Superseded ||
									(editingPath.Length != 0 && SameEditValue(resolver.Resolve(item), rejectedValue) == false))
								{
									TVDiag.LogRetailF("[TableView] Skipped a posted edit rollback: the value changed after the rejected commit.");
									return;
								}

								var restored = false;
								try
								{
									rollbackEditor.DataContext = item;
									rollbackColumn.CancelCellEdit(rollbackEditor, uneditedValue);
									restored = rollbackColumn.CommitCellEdit(rollbackEditor);
								}
								catch (Exception ex)
								{
									LogColumnHookThrow("CancelCellEdit", ex);
								}
								try { rollbackEditor.ClearValue(FrameworkElement.DataContextProperty); }
								catch (Exception) { }

								if (!restored)
								{
									TVDiag.LogRetailF("[TableView] A rejected value could not be rolled back; the item still holds it.");
								}
							}))
						{
							TVDiag.LogRetailF("[TableView] A rejected value could not be rolled back; the item still holds it.");
						}
					}
					else
					{
						// The cancel closes the edit whatever the rollback does. A write that did not land is the app's.
						try
						{
							column.CancelCellEdit(editingElement, m_editUneditedValue);
							if (!column.CommitCellEdit(editingElement))
							{
								if (tip is not null) { tip.RecoveredAppFault(TableViewTip.AppFaultKind.Write); }
								else
								{
									NoteRecoveredAppFault(TableViewTip.Operation.CancelEdit, TableViewTip.Stage.Editing,
										TableViewTip.AppFaultKind.Write, null);
								}
							}
						}
						catch (Exception ex)
						{
							NoteColumnFault(tip, TableViewTip.Operation.CancelEdit, ex.HResult);
							LogColumnHookThrow("CancelCellEdit", ex);
						}
					}

					m_editSourceWritten = false;
				}
			}
		}

		EndEditVisual(action, tip);
		m_editUneditedValue = null;

		m_currentEditItem = null;
		m_currentEditColumn = null;
		return true;
	}

	private bool CompleteEditEnd(EditingUnit unit, TableViewEditAction action, bool honorCancel, bool vetoed, ActionTip? tip = null)
	{
		if (m_editState != EditState.Ending)
		{
			if (tip is not null) { tip.Ignore(TableViewTip.IgnoreReason.Stale); }
			return false;
		}

		if (vetoed)
		{
			if (tip is not null) { tip.Ignore(TableViewTip.IgnoreReason.Vetoed); }
			// Ending -> Editing: a handler kept the edit open.
			m_editState = EditState.Editing;
			ClearCoalescedEditReshape();
			return false;
		}

		var generation = m_editGeneration;

		var closed = FinishEditTeardown(unit, action, honorCancel, tip);

		// A rollback write inside FinishEditTeardown raises PropertyChanged, which can re-enter layout
		// and drive a forced teardown (row rebuild / recycle) underneath this frame. That teardown has
		// already closed the edit and torn the editor out of the tree, so resurrecting the state here
		// would leave the control reporting IsEditing with no editor.
		if (m_editGeneration != generation)
		{
			if (tip is not null) { tip.Ignore(TableViewTip.IgnoreReason.Superseded); }
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

		using ActionTip tip = new(this,
			action == TableViewEditAction.Commit ? TableViewTip.Operation.CommitEdit : TableViewTip.Operation.CancelEdit,
			TableViewTip.Stage.Editing, honorCancel);
		try
		{
			var generation = m_editGeneration;
			m_editState = EditState.Ending;

			var result = RaiseEditEnding(unit, action, honorCancel, honorCancel ? tip : null);

			var closed = CompleteEditEnd(unit, action, honorCancel, result == EditEndingResult.Vetoed, honorCancel ? tip : null);
			if (closed && m_editGeneration == generation) { tip.Complete(); }
			else if (m_editGeneration != generation) { tip.Ignore(TableViewTip.IgnoreReason.Superseded); }
			return closed;
		}
		catch (Exception ex)
		{
			tip.Escaping(TableViewTip.CurrentFault(ex));
			throw;
		}
	}

	internal bool TerminateEditForReset(bool force, bool sourceReplaced = false)
	{
		if (m_editState == EditState.None)
		{
			return true;
		}

		// Forced teardown (source-driven reset / ItemsSource replacement / unload) cannot be
		// vetoed: commit the pending value if it is valid, otherwise report Cancel and restore
		// the edit snapshot. An item that has left the source (removed, filtered out, source
		// replaced) is reported as Cancel with nothing written, as WPF DataGrid does, so a deleted
		// record is never written back. Control-initiated (non-forced) reshapes stay cancelable.
		var action = TableViewEditAction.Commit;
		if (force)
		{
			// Asked of the active source, not the repeater: a projection swap is reported before the
			// repeater re-binds, and an item under a collapsed group or node is still present.
			var source = m_activeSource;
			var itemLeftSource = sourceReplaced || source is null ||
				!source.ContainsShapedItem(UnwrapEditingDataItem(m_currentEditItem));

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
			if (m_currentEditColumn is { } column && !itemLeftSource)
			{
				if (CurrentEditingElement() is { } editingElement)
				{
					// The forced close carries on whatever the write does; a write that did not land is the app's.
					try
					{
						wrote = column.CommitCellEdit(editingElement);
						if (!wrote)
						{
							NoteRecoveredAppFault(TableViewTip.Operation.CommitEdit, TableViewTip.Stage.Editing,
								TableViewTip.AppFaultKind.Write, null);
						}
						m_editSourceWritten = m_editSourceWritten || wrote;
					}
					catch (Exception ex)
					{
						NoteColumnFault(null, TableViewTip.Operation.CommitEdit, ex.HResult);
						LogColumnHookThrow("CommitCellEdit", ex);
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
			// Work deferred past an earlier forced close is older than this request.
			FlushDeferredEditReshapes();
			// A handler run by that work may have opened an edit; end it like any other.
			if (m_editState == EditState.None)
			{
				return true;
			}
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

	internal void QueueReshapeAfterEditCloses(Action? operation)
	{
		if (operation is not null)
		{
			m_reshapesAfterEditCloses.Enqueue(operation);
		}
	}

	internal void ClearCoalescedEditReshape() => m_pendingEditReshapes.Clear();

	private void QueueDeferredEditReshapeDrain()
	{
		if (IsEditing || m_editReshapeDrainQueued ||
			(m_reshapesAfterEditCloses.Count == 0 && m_pendingEditReshapes.Count == 0))
		{
			return;
		}
		if (DispatcherQueue is { } queue)
		{
			var weakThis = new WeakReference<TableView>(this);
			m_editReshapeDrainQueued = queue.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.m_editReshapeDrainQueued = false;
					// While another table owns the source the queue waits for the next edit close.
					if (!strongThis.IsEditing && !strongThis.IsActiveSourceOwnedByOther())
					{
						try
						{
							strongThis.DrainCoalescedEditReshape();
						}
						catch (Exception)
						{
							TVDiag.LogRetailF("[TableView] A reshape deferred past a forced edit close threw while draining.");
						}
					}
				}
			});
		}
	}

	internal void FlushDeferredEditReshapes()
	{
		// Each pass pops at least one operation, so this terminates even if every one throws.
		while (!m_suppressEditVisualRestore && !m_isApplyingCoalescedEditReshape && !IsActiveSourceOwnedByOther() && !IsEditing &&
			(m_pendingEditReshapes.Count != 0 || m_reshapesAfterEditCloses.Count != 0))
		{
			try
			{
				DrainCoalescedEditReshape();
			}
			catch (Exception)
			{
				// The rest is re-posted; the newer request must not fail for an older one.
				TVDiag.LogRetailF("[TableView] A deferred edit reshape threw while flushing ahead of a newer one.");
			}
		}
	}

	internal void DrainCoalescedEditReshape()
	{
		// Every edit close lands here. Release a held live reshape unconditionally: it is posted, so
		// safe even inside a layout pass.
		if (!IsEditing)
		{
			if (m_activeSource is { } source)
			{
				source.ResumeHeldLiveShaping();
			}
		}

		// Never replay inside a layout pass. The queued operations are app-supplied callables that
		// reshape the source; running one from ItemsRepeater's element-clearing/measure callback
		// re-enters the repeater mid-pass. m_suppressEditVisualRestore is exactly the "we are inside
		// layout" signal, so it gates this too.
		if (m_suppressEditVisualRestore)
		{
			// No later close is coming to replay them.
			QueueDeferredEditReshapeDrain();
			return;
		}

		if (!IsEditing && m_reshapesAfterEditCloses.Count != 0)
		{
			// Uno specific: Queue has no insert at the front, so the pending operations are re-queued behind these.
			var pending = m_pendingEditReshapes.ToArray();
			m_pendingEditReshapes.Clear();
			foreach (var deferred in m_reshapesAfterEditCloses)
			{
				m_pendingEditReshapes.Enqueue(deferred);
			}
			foreach (var queued in pending)
			{
				m_pendingEditReshapes.Enqueue(queued);
			}
			m_reshapesAfterEditCloses.Clear();
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
					try
					{
						operation();
					}
					catch (Exception)
					{
						// Already popped, so re-posting the rest terminates.
						QueueDeferredEditReshapeDrain();
						throw;
					}
				}
			}
		}
		finally
		{
			m_isApplyingCoalescedEditReshape = false;
		}
	}

	private void ResetPendingBeginEdit()
	{
		m_currentEditItem = null;
		m_currentEditColumn = null;
		m_editUneditedValue = null;
		m_editState = EditState.None;
		m_abandonPendingBeginEdit = false;
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
			// transaction, so moving to a different item commits with unit Row. Moving between cells
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

		using ActionTip tip = new(this, TableViewTip.Operation.BeginEdit, TableViewTip.Stage.Editing);
		try
		{
			var generation = m_editGeneration;
			m_currentEditItem = item;
			m_currentEditColumn = column;
			m_currentEditRow = null;
			m_editState = EditState.Beginning;
			m_abandonPendingBeginEdit = false;
			m_editSourceWritten = false;

			// Only a throw reaches here still Beginning; without this unwind IsEditing would stay true and
			// every later edit would be rejected as reentrant.
			using var unwindOnThrow = Disposable.Create(() =>
			{
				if (m_editState != EditState.Beginning)
				{
					return;
				}
				try
				{
					// Not a layout pass, so unlike AbandonCellEdit this may hand focus back to the cell.
					if (m_currentEditRow is { } failedRow)
					{
						failedRow.EndCellEdit(TableViewEditAction.Cancel);
					}
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] Abandoning a failed BeginEdit could not restore the display cell.");
				}
				m_currentEditRow = null;
				ResetPendingBeginEdit();
				try
				{
					DrainCoalescedEditReshape();
				}
				catch (Exception)
				{
					TVDiag.LogRetailF("[TableView] A reshape queued during a failed BeginEdit threw while draining.");
				}
			});

			// Everything from here to the promotion below runs app code - a BeginningEdit handler, the
			// editing binding's getter, PrepareCellForEdit - any of which can mutate the
			// collection and have ItemsRepeater recycle the row underneath us. A forced teardown cannot
			// close an edit that is still Beginning, so OnRowElementClearing raises this flag instead and
			// the checks below unwind rather than promoting to Editing over a recycled row.
			var allowed = RaiseBeginningEdit(item, column, tip);
			if (!allowed || m_abandonPendingBeginEdit || !TryBeginEditVisual(item, column))
			{
				tip.Ignore(!allowed ? TableViewTip.IgnoreReason.Vetoed :
					m_abandonPendingBeginEdit ? TableViewTip.IgnoreReason.Stale : TableViewTip.IgnoreReason.Cancelled);
				// Beginning -> None. Do not leave stale trackers behind, and reset the state before
				// draining so a queued reshape can itself start an edit.
				//
				// The item transaction is deliberately NOT touched: a cell commit keeps the row
				// transaction open, so anything held here belongs to a previous cell in the same row and
				// is still legitimately live. Clearing it would leave the item with an unpaired BeginEdit.
				ResetPendingBeginEdit();
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
						// The hook is TableView's code; the app code it ran (a CellEditingTemplate's content) tags its throw.
						var fault = TableViewTip.CurrentFault(ex);
						NoteColumnFault(tip, TableViewTip.Operation.BeginEdit, fault.error,
							fault.appFault != TableViewTip.AppFaultKind.None ? fault.appFault : null);
						LogColumnHookThrow("PrepareCellForEdit", ex);
					}
				}
			}

			// Last chance to unwind: PrepareCellForEdit calls app code, so
			// the row can have been recycled between the check above and here. Promoting to Editing over a
			// recycled row is what lets a later commit write into a different item.
			if (m_abandonPendingBeginEdit || m_currentEditRow is null)
			{
				tip.Ignore(TableViewTip.IgnoreReason.Stale);
				ResetPendingBeginEdit();
				DrainCoalescedEditReshape();
				return false;
			}

			SetCurrentCell(item, column);
			m_editState = EditState.Editing;
			DrainCoalescedEditReshape();
			if (m_editGeneration == generation && m_editState == EditState.Editing &&
				m_currentEditColumn == column && SameInspectableIdentity(m_currentEditItem, item))
			{
				tip.Complete();
			}
			else
			{
				tip.Ignore(TableViewTip.IgnoreReason.Superseded);
			}
			return true;
		}
		catch (Exception ex)
		{
			tip.Escaping(TableViewTip.CurrentFault(ex));
			throw;
		}
	}

	// Editing starts from the user: double-click, or F2 on the current cell. There is no
	// programmatic BeginEdit in this release, and therefore no public current-cell surface for one
	// to target. Both arrive together when there is a scenario - a context menu or a view-model
	// command - to justify them.

	// Closes the open CELL edit. False when it did not close: a veto or failed validation leaves the
	// edit open and IsEditing true.
	//
	// There is no row-scoped commit/cancel API and no RowEditEnding in this release; both arrive
	// with row editing.
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
