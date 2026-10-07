// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Selection.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Row selection: single-item, row-scoped.
//
// SelectionModel owns the selected index and reconciles it across collection changes - the same
// component ItemsView uses. This file is the layer around it: the DP projections, the row chrome,
// the gestures and the automation events. SelectedItem and SelectedIndex are read-only to apps,
// so this control is their only writer.

partial class TableView
{
	// Restores the previous value rather than clearing, so nesting cannot unlatch an outer scope.
	private ref struct ScopedFlag : IDisposable
	{
		public ScopedFlag(ref bool flag, bool value)
		{
			m_flag = ref flag;
			m_previous = flag;
			m_flag = value;
		}

		public void Dispose() => m_flag = m_previous;

		// ScopedFlag(ScopedFlag const&) = delete;
		// ScopedFlag& operator=(ScopedFlag const&) = delete;

		private readonly ref bool m_flag;
		private readonly bool m_previous;
	}

	private static bool IsResolvableSelectionAnnouncementRow(
		TableView? owner,
		TableViewRow? row,
		AutomationPeer? peer,
		object? expectedItem)
	{
		if (owner is null || row is null || peer is null || expectedItem is null ||
			row.GetOwningTableView() != owner)
		{
			return false;
		}

		var repeater = owner.GetRowsRepeaterInternal();
		if (repeater is null)
		{
			return false;
		}

		int index = repeater.GetElementIndex(row);
		var element = index >= 0 ? repeater.TryGetElement(index) as TableViewRow : null;
		// Identity, not just index: a recycled container can still report an index while it has
		// already been re-bound to a different item, so either announcement - selected or removed
		// from selection - would name the wrong record.
		var actualItem = owner.UnwrapEditingDataItem(row.DataContext);
		return element == row && actualItem is not null && TableView.SameInspectableIdentity(actualItem, expectedItem);
	}

	internal bool CanSelectRows() => SelectionMode != TableViewSelectionMode.None;

	private bool HasRowsSource()
	{
		if (m_rowsRepeater is { } repeater)
		{
			return repeater.ItemsSourceView != null;
		}

		return false;
	}

	// ----- SelectionModel plumbing -----

	private void EnsureSelectionModel()
	{
		if (m_selectionModel is not null)
		{
			return;
		}

		var selectionModel = new SelectionModel();
		m_selectionModel = selectionModel;
		selectionModel.SingleSelect = true;

		var weakThis = new WeakReference<TableView>(this);
		TypedEventHandler<SelectionModel, SelectionModelSelectionChangedEventArgs> handler =
			(SelectionModel sender, SelectionModelSelectionChangedEventArgs args) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnSelectionModelSelectionChanged(sender, args);
				}
			};
		selectionModel.SelectionChanged += handler;
		m_selectionModelChangedRevoker.Disposable = Disposable.Create(() => selectionModel.SelectionChanged -= handler);
	}

	private void UpdateSelectionModelSource()
	{
		if (m_selectionModel is null)
		{
			return;
		}

		object? rowsSource = null;
		if (m_rowsRepeater is { } repeater)
		{
			// The repeater's view, not the raw source: SelectionNode reuses an ItemsSourceView it is
			// handed, so the model and the repeater observe one shared view in one subscription order
			// instead of racing two independent views over the same collection.
			rowsSource = repeater.ItemsSourceView;
		}

		// Source has no identity short-circuit: assigning the same source again clears the selection.
		if (!ReferenceEquals(m_selectionModel.Source, rowsSource))
		{
			m_selectionModel.Source = rowsSource;
		}
	}

	internal int SelectedIndexInternal()
	{
		if (m_selectionModel is not null)
		{
			// Flat source, so the path is one deep when there is a selection.
			if (m_selectionModel.SelectedIndex is { } path && path.GetSize() > 0)
			{
				return path.GetAt(0);
			}
		}

		return -1;
	}

	internal object? SelectedItemInternal() => SelectedItemForIndex(SelectedIndexInternal());

	private object? SelectedItemForIndex(int index)
	{
		// SelectionModel::SelectedItem indexes its source without a bounds check, and the model can be
		// momentarily ahead of or behind the collection while a change is being dispatched.
		if (m_selectionModel is null || index < 0 || index >= GetItemsSourceCount())
		{
			return null;
		}

		return m_selectionModel.SelectedItem;
	}

	// ----- Deferred reload request -----
	//
	// Only one thing defers now: unload drains the repeater's source, and re-sourcing on load hands
	// SelectionModel a new view, which clears it. The selected item is held across that round trip.
	// ItemsView needs no equivalent because it never drains its repeater on unload.

	private bool ShouldDeferSelectionRequest() => !CanSelectRows() || !HasRowsSource();

	private void ClearPendingSelection() => m_pendingSelectedItem = null;

	private bool DrainPendingSelection()
	{
		if (ShouldDeferSelectionRequest())
		{
			return false;
		}

		if (m_pendingSelectedItem is { } pendingItem)
		{
			ClearPendingSelection();
			ApplySelection(IndexOfItem(pendingItem));
			return true;
		}

		return false;
	}

	private int IndexOfItem(object? item)
	{
		if (item is null)
		{
			return -1;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return -1;
		}

		var view = repeater.ItemsSourceView;
		if (view is null)
		{
			return -1;
		}

		// SelectionModel indexes but does not look items up, so resolving a held item on reload still
		// needs this. Linear scan: it runs at most once per reload.
		var target = UnwrapEditingDataItem(item);
		int count = view.Count;
		for (int index = 0; index < count; ++index)
		{
			// Identity, not ABI-pointer equality: the same object reached through a different
			// interface (boxed values, projected interfaces) must still match.
			if (UnwrapEditingDataItem(view.GetAt(index)) is { } candidate &&
				SameInspectableIdentity(candidate, target))
			{
				return index;
			}
		}

		return -1;
	}

	// ----- The single writer -----

	private void ApplySelection(int index)
	{
		if (!CanSelectRows())
		{
			index = -1;
		}

		if (index >= 0 && index >= GetItemsSourceCount())
		{
			index = -1;
		}

		if (index < 0 && m_selectionModel is null)
		{
			// Nothing selected and no model yet - nothing to clear, but publish so the projections
			// start out agreeing with the model.
			m_stickySelectedItem = null;
			PushSelectionProperties();
			return;
		}

		EnsureSelectionModel();

		if (index < 0)
		{
			m_selectionModel!.ClearSelection();
		}
		else
		{
			m_selectionModel!.Select(index);
		}

		// Capture the intentional selection by object identity. This is the single selection writer, so
		// every user gesture / programmatic set / restore passes through here and nowhere else moves the
		// sticky anchor - which is exactly why a Reset-driven model clear (which does NOT call this)
		// leaves it intact for the identity restore.
		m_stickySelectedItem = index >= 0 ? SelectedItemForIndex(index) : null;

		// The model raises SelectionChanged only when the selection actually moved; publish here too so
		// a rejected write still leaves the DPs agreeing with the model.
		PushSelectionProperties();
	}

	private void OnSelectionModelSelectionChanged(
		SelectionModel sender,
		SelectionModelSelectionChangedEventArgs args)
	{
		int newIndex = SelectedIndexInternal();
		var newItem = SelectedItemInternal();
		var deselectedItem = UnwrapEditingDataItem(SelectedItem);
		var selectedItem = UnwrapEditingDataItem(newItem);

		var deselectedRow = FindRealizedRowForIndex(m_lastPublishedIndex);
		var selectedRow = FindRealizedRowForIndex(newIndex);
		m_lastPublishedIndex = newIndex;

		// Arm the guard BEFORE any DP write. Both the row IsSelected pushes below and
		// PushSelectionProperties notify synchronously, so an observer can select something else from
		// inside either one. When that happens the nested pass has already published and raised for the
		// newer selection; finishing this one would overwrite it and raise a bogus delta.
		uint version = ++m_selectionVersion;

		// Restamp before notifying, so a handler that walks the rows sees settled chrome.
		if (deselectedRow is not null)
		{
			deselectedRow.SetIsSelectedInternal(false);
		}

		if (selectedRow is not null)
		{
			selectedRow.SetIsSelectedInternal(true);
		}

		if (m_selectionVersion != version)
		{
			return;
		}

		// While a reload is being restored, SelectionModel::Source clears before the stashed item is
		// re-selected. Chrome is already settled above; suppressing the rest keeps that round trip from
		// publishing a transient null and raising a clear-then-reselect pair for a selection that never
		// actually changed. ResolveSelectionAfterSourceChange publishes once when it finishes.
		if (m_isRestoringSelection)
		{
			return;
		}

		PushSelectionProperties();
		if (m_selectionVersion != version)
		{
			return;
		}

		RaiseSelectionAutomationEvents(deselectedRow, deselectedItem, selectedRow, selectedItem);
		RaiseSelectionChanged(newItem);
	}

	private void PushSelectionProperties()
	{
		// The properties are read-only to apps, so this is the only writer and there is no echo to
		// suppress and nothing to re-assert against an observer writing back.
		// Read the index once and derive the item from it: each SelectedIndexInternal() call builds an
		// IndexPath, and SelectedItemInternal() would read it again.
		int index = SelectedIndexInternal();
		var item = SelectedItemForIndex(index);

		if (SelectedIndex != index)
		{
			SelectedIndex = index;
		}

		if (!SameInspectableIdentity(SelectedItem, item))
		{
			SelectedItem = item;
		}
	}

	private TableViewRow? FindRealizedRowForIndex(int index)
	{
		if (index < 0)
		{
			return null;
		}

		if (m_rowsRepeater is { } repeater)
		{
			// TryGetElement only returns containers the repeater currently considers realized, so this
			// cannot hand back a pooled ghost.
			return repeater.TryGetElement(index) as TableViewRow;
		}

		return null;
	}

	private void RaiseSelectionChanged(object? addedItem)
	{
		// `removed` is the last item actually REPORTED, so a superseded transition's removal still
		// surfaces exactly once rather than being lost with the suppressed event.
		//
		// Single-selection only: the delta is derived from one cached item. Under Multiple this
		// short-circuit would SILENTLY suppress a real change whose first item happened not to move -
		// selecting a second row while the first stays selected. Multiple must diff the selected sets
		// instead, the way Selector does; SelectionModelSelectionChangedEventArgs carries no delta.
		var removedItem = m_lastRaisedSelectedItem;
		if (SameInspectableIdentity(removedItem, addedItem))
		{
			return;
		}

		m_lastRaisedSelectedItem = addedItem;

		List<object> added = new();
		if (addedItem is not null)
		{
			added.Add(addedItem);
		}

		List<object> removed = new();
		if (removedItem is not null)
		{
			removed.Add(removedItem);
		}

		// The platform args type, not a bespoke one, so a handler can be shared with ListView.
		// Constructor order is (removedItems, addedItems).
		var args = new SelectionChangedEventArgs(
			removed,
			added);

		SelectionChanged?.Invoke(this, args);
	}

	private void RaiseSelectionAutomationEvents(
		TableViewRow? deselectedRow,
		object? deselectedItem,
		TableViewRow? selectedRow,
		object? selectedItem)
	{
		// Container-level first: it is the only signal available when the selected row is unrealized
		// and there is no row peer to raise a per-element event on.
		if (AutomationPeer.ListenerExists(AutomationEvents.SelectionPatternOnInvalidated))
		{
			if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
			{
				peer.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
			}
		}

		// FromElement returns an existing peer or null - it never forces one into existence.
		if (selectedRow is not null &&
			AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected))
		{
			if (FrameworkElementAutomationPeer.FromElement(selectedRow) is { } peer)
			{
				if (IsResolvableSelectionAnnouncementRow(this, selectedRow, peer, selectedItem))
				{
					peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
				}
			}
		}

		if (deselectedRow is not null &&
			AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
		{
			if (FrameworkElementAutomationPeer.FromElement(deselectedRow) is { } peer)
			{
				if (IsResolvableSelectionAnnouncementRow(this, deselectedRow, peer, deselectedItem))
				{
					peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
				}
			}
		}

		// Narrator keys off the IsSelected property change, not only the pattern events above;
		// TreeViewItem raises both for the same reason.
		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			if (selectedRow is not null)
			{
				if (FrameworkElementAutomationPeer.FromElement(selectedRow) is { } peer)
				{
					if (IsResolvableSelectionAnnouncementRow(this, selectedRow, peer, selectedItem))
					{
						peer.RaisePropertyChangedEvent(
							SelectionItemPatternIdentifiers.IsSelectedProperty,
							BoolBoxes.False,
							BoolBoxes.True);
					}
				}
			}

			if (deselectedRow is not null)
			{
				if (FrameworkElementAutomationPeer.FromElement(deselectedRow) is { } peer)
				{
					if (IsResolvableSelectionAnnouncementRow(this, deselectedRow, peer, deselectedItem))
					{
						peer.RaisePropertyChangedEvent(
							SelectionItemPatternIdentifiers.IsSelectedProperty,
							BoolBoxes.True,
							BoolBoxes.False);
					}
				}
			}
		}
	}

	// ----- Property-changed callbacks -----

	private void OnSelectionModePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (!CanSelectRows())
		{
			// Turning selection off is an explicit app action: clear, and drop anything held for a
			// reload so it cannot resurrect after the app asked for nothing to be selected.
			ClearPendingSelection();
			ApplySelection(-1);
		}
	}


	// ----- Source changes -----

	private void UpdateSelectionCollectionChangedSubscription()
	{
		ItemsSourceView? view = null;
		if (m_rowsRepeater is { } repeater)
		{
			view = repeater.ItemsSourceView;
		}

		if (m_selectionCollectionChangedRevoker.Disposable is not null && SameInspectableIdentity(view, m_selectionCollectionChangedView))
		{
			return;
		}

		// auto_revoke drops the prior source's subscription.
		m_selectionCollectionChangedRevoker.Disposable = null;
		m_selectionCollectionChangedView = null;

		if (view is not null)
		{
			view.CollectionChanged += OnSelectionItemsSourceCollectionChanged;
			m_selectionCollectionChangedRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= OnSelectionItemsSourceCollectionChanged);
			m_selectionCollectionChangedView = view;
		}
	}

	private void UpdateSelectionResetDetectorSubscription()
	{
		ItemsSourceView? view = null;
		if (m_rowsRepeater is { } repeater)
		{
			view = repeater.ItemsSourceView;
		}

		if (m_selectionResetDetectorRevoker.Disposable is not null && SameInspectableIdentity(view, m_selectionResetDetectorView))
		{
			return;
		}

		// auto_revoke drops the prior source's subscription.
		m_selectionResetDetectorRevoker.Disposable = null;
		m_selectionResetDetectorView = null;

		if (view is not null)
		{
			view.CollectionChanged += OnSelectionSourceReset;
			m_selectionResetDetectorRevoker.Disposable = Disposable.Create(() => view.CollectionChanged -= OnSelectionSourceReset);
			m_selectionResetDetectorView = view;
		}
	}

	private void OnSelectionSourceReset(
		object? sender,
		NotifyCollectionChangedEventArgs args)
	{
		// Only a Reset drops the model's indices wholesale (an Add/Remove shifts the selected index and
		// SelectionModel reconciles it in place, so selection survives without help). This must run
		// FIRST, before the model reconciles the same Reset - else the app sees a spurious
		// SelectionChanged clearing the selection, immediately followed by another restoring it.
		if (args.Action != NotifyCollectionChangedAction.Reset)
		{
			return;
		}

		// A reload restore already owns the stash/suppress protocol; don't stack a second one.
		if (m_isRestoringSelection || m_resetSelectionRestorePending)
		{
			return;
		}

		var sticky = m_stickySelectedItem;
		if (sticky is null)
		{
			return;
		}

		// Stash by identity and arm the restore. Setting m_isRestoringSelection now suppresses the
		// transient clear the model is about to publish, so the restore reads as one atomic event (or
		// as silence when the same row is re-selected) rather than a clear-then-reselect pair.
		m_pendingSelectedItem = sticky;
		m_isRestoringSelection = true;
		m_resetSelectionRestorePending = true;
	}

	private void OnSelectionItemsSourceCollectionChanged(
		object? sender,
		object args)
	{
		if (m_resetSelectionRestorePending)
		{
			// The model has now reconciled the Reset (this handler is subscribed after it), so the view
			// is settled and the identity lookup is valid. Restore the stashed item: DrainPendingSelection
			// re-selects it when it survived the reshape, or clears when the filter removed it. Mirrors
			// ResolveSelectionAfterSourceChange - drain under the restoring flag so the internal write is
			// suppressed, then publish exactly once.
			m_resetSelectionRestorePending = false;
			DrainPendingSelection();
			m_isRestoringSelection = false;

			m_lastPublishedIndex = SelectedIndexInternal();
			RestampAllRealizedRowSelection(m_lastPublishedIndex);
			PushSelectionProperties();
			RaiseSelectionChanged(SelectedItemInternal());
			return;
		}

		// SelectionModel has already reconciled the index - and deliberately does not raise when an
		// insert merely shifts it. All that is left is the chrome: ItemsRepeater re-indexes its
		// realized containers around now, so re-derive every one of them rather than trusting the
		// index any single row was last stamped against.
		m_lastPublishedIndex = SelectedIndexInternal();
		RestampAllRealizedRowSelection(m_lastPublishedIndex);
		PushSelectionProperties();
	}

#pragma warning disable IDE0051 // Unused upstream as well, kept for 1:1 parity
	private void RestampAllRealizedRowSelection() => RestampAllRealizedRowSelection(SelectedIndexInternal());
#pragma warning restore IDE0051

	private void RestampAllRealizedRowSelection(int selectedIndex)
	{
		// The index is passed in because SelectedIndexInternal() builds an IndexPath per call, and this
		// runs once per realized row on every collection notification.
		ForEachRealizedRow((TableViewRow row) =>
			{
				RefreshRowSelectionState(row, selectedIndex);
			});
	}

	private void StashSelectionForReload()
	{
		if (m_pendingSelectedItem is not null)
		{
			return;
		}

		if (SelectedItemInternal() is { } item)
		{
			m_pendingSelectedItem = item;
		}
	}

	private void ResolveSelectionAfterSourceChange()
	{
		// Create the model before anything else subscribes. UpdateSelectionModelSource is a no-op while
		// the model is null, so leaving it lazy here would let this control register on the shared view
		// first and restamp rows from a not-yet-reconciled index. ItemsView constructs its model inline
		// for the same reason.
		EnsureSelectionModel();

		// A source/shape swap (grouped<->flat, an ItemsSource replacement) reassigns the model's Source,
		// which clears it. If nothing was explicitly stashed, seed the restore from the sticky anchor so
		// a selection that survives the swap by identity is preserved here too - the same guarantee the
		// in-place Reset path gives. A stale item (new dataset) simply resolves to -1 and clears.
		if (m_pendingSelectedItem is null)
		{
			if (m_stickySelectedItem is { } sticky)
			{
				m_pendingSelectedItem = sticky;
			}
		}

		bool hasStashedSelection = m_pendingSelectedItem is not null;

		{
			// Setting Source clears, and the drain then re-selects. Publishing between the two would
			// emit a transient null and a clear-then-reselect event pair for a selection that did not
			// actually change, so the whole restore is published once, at the end.
			using var restoring = new ScopedFlag(ref m_isRestoringSelection, hasStashedSelection);

			// Detector first, then model, then the restamp handler: the detector must observe a later
			// in-place Reset ahead of the model so it can capture the selection before the model drops
			// it, and the restamp handler must observe after the model so it restores against a
			// reconciled view.
			UpdateSelectionResetDetectorSubscription();
			// Model second: it must be subscribed to the shared view ahead of the restamp handler, so
			// that by the time OnSelectionItemsSourceCollectionChanged runs the index is reconciled.
			UpdateSelectionModelSource();
			UpdateSelectionCollectionChangedSubscription();

			// A selection held across a reload outranks the live one: the live value was resolved
			// against the source being replaced, whereas the held item is what was selected before.
			DrainPendingSelection();
		}

		m_lastPublishedIndex = SelectedIndexInternal();
		PushSelectionProperties();
		RestampAllRealizedRowSelection(m_lastPublishedIndex);

		// Only raises when the item actually differs from the last one reported, so a round trip that
		// restores the same selection stays silent.
		RaiseSelectionChanged(SelectedItemInternal());
	}

	// ----- Row plumbing -----

	internal void RefreshRowSelectionState(TableViewRow? row) => RefreshRowSelectionState(row, SelectedIndexInternal());

	internal void RefreshRowSelectionState(TableViewRow? row, int selectedIndex)
	{
		if (row is null)
		{
			return;
		}

		bool isSelected = false;
		if (selectedIndex >= 0)
		{
			if (m_rowsRepeater is { } repeater)
			{
				// By INDEX, not item identity: a collection may hold the same object twice, and an
				// identity match would light up every row showing it.
				isSelected = repeater.GetElementIndex(row) == selectedIndex;
			}
		}

		row.SetIsSelectedInternal(isSelected);
	}

	internal void OnRowPointerSelect(TableViewRow? row)
	{
		if (row is null)
		{
			return;
		}

		if (m_rowsRepeater is { } repeater)
		{
			var index = repeater.GetElementIndex(row);
			if (index >= 0)
			{
				// SelectRowIndexFromInteraction gates on SelectionMode.
				SelectRowIndexFromInteraction(index);
			}
		}
	}

	internal void SelectRowIndexFromInteraction(int index)
	{
		// Ctrl toggles, matching SingleSelector::OnInteractedAction and ListViewBase. Without it there
		// is no pointer or keyboard gesture that can clear a selection once one is made - the app would
		// have to call DeselectAll. Read live rather than off the args, as ItemsView's interaction
		// layer does, so the row's pointer handlers do not have to carry modifier state.
		bool isControlDown =
			(InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) &
				CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

		SelectRowIndexFromInteraction(index, isControlDown);
	}

	internal void SelectRowIndexFromInteraction(int index, bool toggle)
	{
		if (!CanSelectRows())
		{
			// A user gesture while selection is off is a no-op.
			return;
		}

		// Group headers share the flat projection's index space with data rows but are not selectable.
		// Keyboard navigation legitimately lands focus on a header (to expand/collapse it); when it
		// does, leave the existing data selection untouched rather than moving it to - or clearing it
		// against - a header index. Selection-follows-focus resumes on the next data row.
		if (index >= 0 && IsGroupHeaderRow(index))
		{
			return;
		}

		// An explicit gesture settles the question - drop anything held for a reload.
		ClearPendingSelection();

		if (toggle && IsSelected(index))
		{
			ApplySelection(-1);
			return;
		}

		ApplySelection(index);
	}

	internal void SelectRowIndexFromKeyboardFocus(int index)
	{
		SelectRowIndexFromInteraction(index, false /* toggle */);
	}

	// ----- Public API -----
	// Named to match ItemsView. No identity-based overloads: an index is the only addressing mode.

	/// <summary>
	/// Selects the item at <paramref name="index"/>. A negative index clears the selection.
	/// </summary>
	/// <param name="index">The index of the item to select.</param>
	public void Select(int index)
	{
		if (index < 0)
		{
			// Explicit "select nothing".
			DeselectAll();
			return;
		}

		// Reject rather than coerce. ApplySelection turns an unresolvable index into "clear", which is
		// right for a coercion path but wrong here: Select(999) must not wipe an existing selection.
		// ItemsView::Select is a straight pass-through to SelectionModel and never clears either.
		if (!CanSelectRows() || index >= GetItemsSourceCount())
		{
			return;
		}

		// A group header shares the flat projection's index space with data rows but is not selectable,
		// so it is rejected here. Without this the internal GroupedEntry would reach the app.
		if (IsGroupHeaderRow(index))
		{
			return;
		}

		ApplySelection(index);
	}

	/// <summary>
	/// Clears the selection only when <paramref name="index"/> is the selected index, so a stale call
	/// cannot clobber a newer selection.
	/// </summary>
	/// <param name="index">The index of the item to deselect.</param>
	public void Deselect(int index)
	{
		// Only clears when `index` IS the selection, so a stale call cannot clobber a newer one.
		if (IsSelected(index))
		{
			ApplySelection(-1);
		}
	}

	/// <summary>
	/// Returns whether <paramref name="index"/> is the selected index.
	/// </summary>
	/// <param name="index">The index of the item to check.</param>
	/// <returns>true if the item at <paramref name="index"/> is selected; otherwise, false.</returns>
	public bool IsSelected(int index)
	{
		// Ask the model rather than comparing against the single selected index, so this stays correct
		// when Multiple lands. The guard is required: SelectionModel::IsSelected asserts on index < 0.
		if (index < 0 || m_selectionModel is null || index >= GetItemsSourceCount())
		{
			return false;
		}

		var selected = m_selectionModel.IsSelected(index);
		return selected.HasValue && selected.Value;
	}

	/// <summary>
	/// Clears the selection. Works regardless of <see cref="SelectionMode"/>, so turning selection off
	/// can always be made to stick.
	/// </summary>
	public void DeselectAll()
	{
		ClearPendingSelection();
		ApplySelection(-1);
	}
}
