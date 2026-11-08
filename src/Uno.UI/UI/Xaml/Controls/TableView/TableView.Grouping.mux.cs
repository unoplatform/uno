// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Grouping.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Globalization.NumberFormatting;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	private static object? GetGroupHeaderKey(GroupedEntry entry)
	{
		var groupObject = entry.Group();
		if (groupObject is ICollectionViewGroup group)
		{
			return group.Group;
		}
		return groupObject;
	}
	// Every step can fail on a locale-starved or self-contained host, and this runs during
	// measure, so nothing is allowed to escape.
	// TODO Uno: TableView_Grouping.cpp's anonymous-namespace LocalizedOrFallback is identical to TableView_Sort.cpp's; both share this definition.
	private static string LocalizedOrFallback(string resourceName, string fallback)
	{
		try
		{
			if (ResourceAccessor.GetLocalizedStringResource(resourceName) is { } resolved && !string.IsNullOrEmpty(resolved))
			{
				return resolved;
			}
		}
		catch (Exception)
		{
		}

		return fallback;
	}

	// ---------------------------------------------------------------------------------------------
	// Grouped-projection rendering.
	//
	// A grouped TableViewSource projects group headers and data rows into one flat row list, so the
	// repeater realizes two container types from one source. TableViewRowTemplateSelector picks
	// between them using GetRowKindForItem; everything a group header needs that a TableViewRow would
	// otherwise have pushed during cell rebuild is pushed from PrepareGroupHeaderElement instead.
	// ---------------------------------------------------------------------------------------------

	// Single mapping from a row-source item to its kind. Deliberately item-based rather than
	// index-based: ElementFactoryGetArgs carries no index, and keeping one lookup here means a future
	// source kind adds its entry type in this function instead of adding a parallel path in the
	// factory.
	internal TableViewRowKind GetRowKindForItem(object? item)
	{
		if (item is null)
		{
			return TableViewRowKind.Data;
		}

		// Only a grouped TableViewSource produces GroupedEntry rows, and only for headers. A flat
		// source hands the app's item straight through, and an app item must never be shaped into a
		// group header just because it happens to satisfy the probe.
		if (!IsTableViewSourceGrouped())
		{
			return TableViewRowKind.Data;
		}

		return GroupedEntry.TryGetGroupedEntry(item) is not null ? TableViewRowKind.GroupHeader : TableViewRowKind.Data;
	}

	internal bool TryGetTableViewSourceRowInfo(int rowIndex, ref TableViewRowInfo rowInfo)
	{
		if (m_tableViewSourceRowMetadata is null || rowIndex < 0)
		{
			return false;
		}

		try
		{
			rowInfo = m_tableViewSourceRowMetadata.GetRowInfo(rowIndex);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	internal bool IsTableViewSourceGrouped()
	{
		if (m_activeSource is { } source)
		{
			return source.IsGrouped();
		}

		return false;
	}

	internal bool IsGroupHeaderRow(int index)
	{
		TableViewRowInfo rowInfo = default;
		return TryGetTableViewSourceRowInfo(index, ref rowInfo) && rowInfo.Kind == TableViewRowKind.GroupHeader;
	}

	internal int GetDataRowPositionInSetInternal(int rowIndex)
	{
		if (rowIndex < 0)
		{
			return 0;
		}

		if (!IsTableViewSourceGrouped())
		{
			return rowIndex + 1;
		}

		int position = 0;
		for (int index = 0; index <= rowIndex; ++index)
		{
			TableViewRowInfo rowInfo = default;
			if (!TryGetTableViewSourceRowInfo(index, ref rowInfo))
			{
				return rowIndex + 1;
			}

			if (rowInfo.Kind == TableViewRowKind.Data)
			{
				++position;
			}
		}

		return position;
	}

	internal int GetDataRowSizeOfSetInternal()
	{
		var rowCount = GetRowCountInternal();
		if (!IsTableViewSourceGrouped())
		{
			return rowCount;
		}

		int dataRowCount = 0;
		for (int index = 0; index < rowCount; ++index)
		{
			TableViewRowInfo rowInfo = default;
			if (!TryGetTableViewSourceRowInfo(index, ref rowInfo))
			{
				return rowCount;
			}

			if (rowInfo.Kind == TableViewRowKind.Data)
			{
				++dataRowCount;
			}
		}

		return dataRowCount;
	}

	// ---------------------------------------------------------------------------------------------
	// Expansion
	// ---------------------------------------------------------------------------------------------

	internal void ToggleGroupExpansion(UIElement container)
	{
		RequestGroupExpansion(container, null);
	}

	// Directional request from ExpandCollapsePattern.
	internal void SetGroupExpansion(UIElement container, bool expand)
	{
		RequestGroupExpansion(container, expand);
	}

	internal void ExpandRowSubtree(UIElement container)
	{
		RequestGroupExpansion(container, true, true /* subtree */);
	}

	internal bool TryFocusParentRow(UIElement container)
	{
		var repeater = m_rowsRepeater;
		if (repeater is null || container is null || m_tableViewSourceRowMetadata is null)
		{
			return false;
		}

		var index = repeater.GetElementIndex(container);
		TableViewRowInfo info = default;
		int parentIndex = -1;
		if (index < 0 || !TryGetTableViewSourceRowInfo(index, ref info) || string.IsNullOrEmpty(info.ParentIdentity) ||
			!m_tableViewSourceRowMetadata.TryGetIndexForIdentity(info.ParentIdentity, out parentIndex))
		{
			return false;
		}

		if (!FocusRow(parentIndex))
		{
			return false;
		}

		// Selection follows the keyboard cursor, as for Up/Down (drill is never Ctrl here).
		SelectRowIndexFromInteraction(parentIndex, false /* toggle */);
		return true;
	}

	private void RequestGroupExpansion(UIElement container, bool? desired, bool subtree = false)
	{
		// Consume the gesture state first so a request that goes nowhere can't leak it to a later one.
		FocusState gestureState = FocusState.Unfocused;
		if (container is TableViewGroupHeader header)
		{
			gestureState = header.TakeGestureFocusStateInternal();
		}
		else if (subtree)
		{
			// Only the Multiply key expands a row subtree, so restore as Keyboard even when the row was
			// focused by a click: its rebuild recycles the row, which would otherwise strand focus.
			gestureState = FocusState.Keyboard;
		}

		if (m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		// Resolve identity NOW, while this container's index is still current. The mutation below is
		// deferred, and the index is not stable across that gap: a scroll can recycle this container
		// onto a different group, or out of view entirely (GetElementIndex returns -1, silently
		// dropping the request). Identity is index-independent once captured.
		string identity = TryGetContainerIdentity(container);

		// Capture focus while the container still owns it; the deferred reshape recycles it.
		CaptureContainerFocusForRestore(container, identity, gestureState);

		// A header toggles a group; anything else is a tree row.
		var axis = container is TableViewGroupHeader
			? BulkExpansionAxis.Groups
			: BulkExpansionAxis.Rows;

		// One group or node is use; a subtree is a bulk request, like ExpandAllGroups.
		if (!subtree && !string.IsNullOrEmpty(identity))
		{
			TableViewTelemetry.MarkUsed(m_telemetrySession, m_telemetrySession.generation,
				axis == BulkExpansionAxis.Groups ? TableViewTelemetry.Feature.Grouping : TableViewTelemetry.Feature.Hierarchy);
		}

		QueueGroupExpansionByIdentity(identity, desired, axis, subtree);
	}

	private string TryGetContainerIdentity(UIElement? container)
	{
		if (m_tableViewSourceRowMetadata is null || container is null)
		{
			return "";
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return "";
		}

		var rowIndex = repeater.GetElementIndex(container);
		if (rowIndex < 0)
		{
			return "";
		}

		try
		{
			return m_tableViewSourceRowMetadata.GetIdentity(rowIndex);
		}
		catch (Exception)
		{
			// Best-effort: the grouping source or its state can change during cleanup.
			return "";
		}
	}

	private void CaptureContainerFocusForRestore(
		UIElement? container,
		string identity,
		FocusState gestureState = FocusState.Unfocused)
	{
		// Clear any prior capture: a fresh toggle supersedes an earlier one whose restore has not run.
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;
		m_pendingGroupFocusFromPointerGesture = false;

		if (string.IsNullOrEmpty(identity) || container is null)
		{
			return;
		}

		var root = XamlRoot;
		if (root is null)
		{
			return;
		}

		// Only restore when THIS header container (or a descendant) currently holds focus: that is the
		// gesture the reshape is about to strand. A UIA Expand()/Collapse() or a programmatic toggle on
		// an unfocused group must not yank focus across the table.
		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		bool focusInContainer = false;
		for (DependencyObject? node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
		{
			if (node == container)
			{
				focusInContainer = true;
				break;
			}
		}
		if (!focusInContainer)
		{
			return;
		}

		// A data row's focus is usually on an inner cell/editor, so read the focused element's state.
		var stateSource = container is TableViewRow
			? focused as Control
			: container as Control;

		if (stateSource is { } control)
		{
			// Headers restore pointer focus too (a band click focuses then collapses); data rows only
			// keyboard/programmatic, since the row restore always focuses with Keyboard state.
			bool isHeader = container is TableViewGroupHeader;
			var state = gestureState != FocusState.Unfocused ? gestureState : control.FocusState;
			bool restorable = isHeader
				? state != FocusState.Unfocused
				: (state == FocusState.Keyboard || state == FocusState.Programmatic);
			if (restorable)
			{
				m_pendingGroupFocusIdentity = identity;
				m_pendingGroupFocusState = state;
				m_pendingGroupFocusFromPointerGesture = gestureState == FocusState.Pointer;
			}
		}
	}

	private bool CanRestoreContainerFocus()
	{
		var root = XamlRoot;
		if (root is null || !root.IsHostVisible)
		{
			return false;
		}

		// ItemsRepeater moves focus off a cleared container to a realized neighbour, still inside.
		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		if (focused is null)
		{
			return true;
		}

		DependencyObject selfObject = this;
		return focused == selfObject || SharedHelpers.IsAncestor(focused, selfObject, false /* checkVisibility */);
	}

	private string CaptureFocusedContainerForRestore()
	{
		// A bulk expand/collapse supersedes any earlier capture whose restore has not run, exactly as
		// a fresh single-group toggle does.
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;
		m_pendingGroupFocusFromPointerGesture = false;

		var root = XamlRoot;
		if (root is null)
		{
			return "";
		}

		// Walk out from the focused element to its header container, if it is inside one. Data-row
		// focus is captured only for a hierarchical source, where a row's chevron is the expansion
		// target; in a flat grouped table, row focus is left alone.
		bool captureRows = m_tableViewSourceRowMetadata is not null && m_tableViewSourceRowMetadata.IsHierarchicalSource();
		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		for (DependencyObject? node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
		{
			UIElement? container = null;
			if (node is TableViewGroupHeader header)
			{
				container = header;
			}
			else if (node is TableViewRow row)
			{
				if (!captureRows)
				{
					break;
				}
				container = row;
			}

			if (container is not null)
			{
				var identity = TryGetContainerIdentity(container);
				CaptureContainerFocusForRestore(container, identity);
				return identity;
			}
		}

		return "";
	}

	// Directional request from a UIA provider or the band gesture. Identity is resolved here, while
	// the container's index is still current, because the mutation below is deferred.
	private void QueueGroupExpansionByIdentity(
		string identity,
		bool? desired,
		BulkExpansionAxis axis,
		bool subtree = false)
	{
		if (string.IsNullOrEmpty(identity))
		{
			return;
		}

		// Capture the intent, not just the target. A directionless toggle applied on a later turn is
		// not idempotent: two Expand() calls would expand then collapse. Carrying `desired` through
		// makes the deferred half an idempotent set.
		var generation = m_rowMetadataGeneration;

		// A bulk expand/collapse on this axis requested after this supersedes it (see ApplyGroupExpansionByIdentity).
		var bulkGeneration = BulkExpansionGeneration(axis);

		// Defer the structural mutation off the current callout. Running it inline re-projects rows
		// while the caller's frame is still on the stack, which faults on the pointer path and
		// surfaces to a UIA client as an exception escaping the COM boundary. Both callers are safe
		// once the mutation happens on a later turn.
		var weakThis = new WeakReference<TableView>(this);
		if (DispatcherQueue is { } queue)
		{
			// A refused enqueue means the thread is shutting down and the UI is going away regardless.
			_ = queue.TryEnqueue(() =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.ApplyGroupExpansionByIdentity(identity, desired, generation, axis, bulkGeneration, subtree);
				}
			});
			return;
		}

		// No dispatcher means this is not a UI thread (test host), so there is no in-flight callout to
		// unwind and the hazard above cannot apply.
		ApplyGroupExpansionByIdentity(identity, desired, generation, axis, bulkGeneration, subtree);
	}

	private void ApplyGroupExpansionByIdentity(
		string identity,
		bool? desired,
		ulong generation,
		BulkExpansionAxis axis,
		ulong bulkGeneration,
		bool subtree = false)
	{
		// Identities are value-based strings, not tied to a provider instance. If ItemsSource was
		// replaced while this request sat on the queue, the same string could name an unrelated group
		// in the new source -- exactly the wrong-group mutation this path exists to prevent.
		if (generation != m_rowMetadataGeneration)
		{
			return;
		}

		// A bulk expand/collapse on this axis was requested after this one. It is the later intent and
		// clears per-item exceptions, so applying this now would resurrect one; drop it.
		if (bulkGeneration != BulkExpansionGeneration(axis))
		{
			return;
		}

		if (m_tableViewSourceRowMetadata is null || string.IsNullOrEmpty(identity))
		{
			return;
		}

		// An open edit sits over a row that expansion is about to move or remove. Coalesce behind the
		// termination rather than reshaping underneath it.
		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending)
			{
				QueueCoalescedEditReshape(() =>
				{
					ApplyGroupExpansionByIdentity(identity, desired, generation, axis, bulkGeneration, subtree);
				});
			}
			return;
		}

		bool changed = false;
		// Local strong ref: a handler may re-declare the shape and replace the provider mid-call. Edit-ending handlers
		// ran app code above, so re-check the source before mutating; a stale request still falls through to drain
		// queued edit reshapes.
		var provider = m_tableViewSourceRowMetadata;
		if (provider is not null && generation == m_rowMetadataGeneration)
		{
			// While actions are tracked, every request is, so a failure writes TableView_Error. A request that changes one
			// group or tree node also runs a TiP test. A subtree is a bulk request, like ExpandAllGroups, and a request
			// that changes nothing runs none: each arrow on a group header is requested by the header and again by
			// TryHandleGroupHeaderExpandCollapseKey. The lookup runs only while actions are tracked.
			var withTest = TracksTipActions() && !subtree && provider.WouldChangeExpansion(identity, desired);
			using ActionTip tip = new(this, axis == BulkExpansionAxis.Groups ?
					TableViewTip.Operation.GroupExpansion : TableViewTip.Operation.HierarchyExpansion,
				TableViewTip.Stage.Expansion, true, withTest);
			try
			{
				if (subtree)
				{
					provider.ExpandSubtree(identity);
					changed = true;
				}
				else if (desired.HasValue)
				{
					// Idempotent set: applying the state we already have is a no-op, and reports no change.
					changed = desired.Value
						? provider.Expand(identity)
						: provider.Collapse(identity);
				}
				else
				{
					changed = provider.Toggle(identity);
				}

				if (generation != m_rowMetadataGeneration) { tip.Ignore(TableViewTip.IgnoreReason.Stale); }
				else if (changed) { tip.Complete(); }
				// No change shown yet. A tested request expected one; an untested one closes without a TiP result.
				else { tip.Ignore(TableViewTip.IgnoreReason.DeferredOrUnproven); }
			}
			catch (Exception ex)
			{
				// Best-effort: the grouping source or its state can change during cleanup. Expansion reads only the
				// projection TableView built, so a failure is TableView's unless the source was replaced or the exception
				// is tagged as the app's.
				var fault = TableViewTip.CurrentFault(ex);
				if (generation != m_rowMetadataGeneration) { tip.Ignore(TableViewTip.IgnoreReason.Stale); }
				else if (fault.appFault != TableViewTip.AppFaultKind.None) { tip.ApplicationFault(fault.appFault, fault.error); }
				else { tip.Fail(fault.error); }
			}
		}

		DrainCoalescedEditReshape();

		// Announce only once the tree has actually moved. Raising while the mutation was still queued
		// told clients to re-read a structure that had not changed yet.
		if (changed)
		{
			QueueGroupExpansionRowRefresh();
			// The toggled row keeps its index, so it is never re-prepared; restamp it here. In a tree,
			// rows below the splice may still report pre-reconcile indices, so restamp again once the
			// repeater has reconciled. Flat grouped rows carry no tree state, so one pass is enough.
			RefreshRealizedRowHierarchyState();
			if (m_tableViewSourceRowMetadata is not null && m_tableViewSourceRowMetadata.IsHierarchicalSource())
			{
				QueueRefreshRealizedRowHierarchyState();
			}
			RaiseGroupStructureChanged();
		}

		// Restore keyboard focus to the toggled header or row even when nothing structurally changed: a
		// non-expandable/no-op toggle still ran the container through the reshape path, and leaving
		// focus stranded is the very bug this guards. Matches on identity, so an unrelated queued
		// toggle does not consume this restore.
		RestoreContainerFocusIfPending(identity);

		if (changed && axis == BulkExpansionAxis.Rows && !subtree &&
			generation == m_rowMetadataGeneration && provider == m_tableViewSourceRowMetadata &&
			bulkGeneration == BulkExpansionGeneration(axis))
		{
			AnnounceRowExpansionChange(identity);
		}
	}

	private void AnnounceRowExpansionChange(string identity)
	{
		if (!AutomationPeer.ListenerExists(AutomationPeer.NotificationEvent) ||
			m_tableViewSourceRowMetadata is null || !m_tableViewSourceRowMetadata.IsHierarchicalSource())
		{
			return;
		}

		try
		{
			int index = -1;
			TableViewRowInfo info = default;
			if (!m_tableViewSourceRowMetadata.TryGetIndexForIdentity(identity, out index) ||
				!TryGetTableViewSourceRowInfo(index, ref info) ||
				info.Kind != TableViewRowKind.Data || !info.IsExpandable)
			{
				return;
			}

			if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
			{
				var announcement = ResourceAccessor.GetLocalizedStringResource(
					info.IsExpanded ? ResourceAccessor.SR_TableViewRowExpanded : ResourceAccessor.SR_TableViewRowCollapsed);
				if (!string.IsNullOrEmpty(announcement))
				{
					// Narrator does not voice the DataItem's ExpandCollapse change. Announce the
					// completed request here, not during row preparation or bulk expansion.
					peer.RaiseNotificationEvent(
						AutomationNotificationKind.ActionCompleted,
						AutomationNotificationProcessing.MostRecent,
						announcement,
						"TableViewRowExpansionChanged");
				}
			}
		}
		catch (Exception)
		{
			// Narrator feedback is best-effort; announcement failures must not affect row expansion.
		}
	}

	private void RestoreContainerFocusIfPending(string identity)
	{
		if (string.IsNullOrEmpty(m_pendingGroupFocusIdentity) || m_pendingGroupFocusIdentity != identity)
		{
			return;
		}

		var focusState = m_pendingGroupFocusState;
		bool fromPointerGesture = m_pendingGroupFocusFromPointerGesture;
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;
		m_pendingGroupFocusFromPointerGesture = false;

		// Defer to after the reshape's relayout. The reprojection triggered by the toggle recycles the
		// header container during the next layout pass; focusing now would land on a container layout
		// is about to recycle, dropping focus a second time. A one-shot LayoutUpdated fires once the
		// tree has settled.
		ArmContainerFocusRestore(identity, focusState, fromPointerGesture, s_containerFocusRestoreAttempts);
	}

	// Re-armed while the header is still unarranged after the relayout; the last attempt focuses anyway.
	private void ArmContainerFocusRestore(
		string identity, FocusState focusState, bool fromPointerGesture, int attemptsLeft)
	{
		if (m_pendingGroupFocusLayoutToken.Disposable is not null)
		{
			m_pendingGroupFocusLayoutToken.Disposable = null;
		}

		var weakThis = new WeakReference<TableView>(this);
		EventHandler<object> onLayoutUpdated = (_, _) =>
		{
			if (!weakThis.TryGetTarget(out var strongThis))
			{
				return;
			}

			if (strongThis.m_pendingGroupFocusLayoutToken.Disposable is not null)
			{
				strongThis.m_pendingGroupFocusLayoutToken.Disposable = null;
			}

			// This runs from a LAYOUT callback, where an escaping exception reaches no app handler
			// and fails the process fast instead. FocusContainerByIdentity realizes a container
			// and moves focus, both of which can throw when the projection has moved underneath a
			// deferred restore.
			try
			{
				if (!strongThis.CanRestoreContainerFocus())
				{
					return;
				}

				bool finalAttempt = attemptsLeft <= 1;
				if (!strongThis.FocusContainerByIdentity(identity, focusState, fromPointerGesture, finalAttempt))
				{
					strongThis.ArmContainerFocusRestore(identity, focusState, fromPointerGesture, attemptsLeft - 1);
				}
			}
			catch (Exception)
			{
				// Best-effort focus restore: the header or row can be gone by the time layout settles.
			}
		};
		LayoutUpdated += onLayoutUpdated;
		m_pendingGroupFocusLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= onLayoutUpdated);
	}

	// Returns false only when a non-final attempt found the group header not yet arranged.
	private bool FocusContainerByIdentity(
		string identity, FocusState focusState, bool fromPointerGesture, bool finalAttempt)
	{
		if (string.IsNullOrEmpty(identity) || m_tableViewSourceRowMetadata is null)
		{
			return true;
		}

		int index = -1;
		if (!m_tableViewSourceRowMetadata.TryGetIndexForIdentity(identity, out index) || index < 0)
		{
			return true;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return true;
		}

		// The metadata provider and repeater own separate views that can absorb reshapes at different
		// times, so a resolved index can exceed what the repeater accepts. GetOrCreateElement throws
		// for that, and this deferred layout callback would fail-fast the process.
		var sourceView = repeater.ItemsSourceView;
		if (sourceView is null || index >= sourceView.Count)
		{
			return true;
		}

		// Data row: a splice keeps focus in place; restore only if the Reset recycled the container.
		TableViewRowInfo info = default;
		if (TryGetTableViewSourceRowInfo(index, ref info) && info.Kind == TableViewRowKind.Data)
		{
			if (XamlRoot is { } root)
			{
				var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
				for (DependencyObject? node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
				{
					if (node is TableViewRow row)
					{
						if (repeater.GetElementIndex(row) == index)
						{
							return true;
						}
						break;
					}
				}
			}

			FocusRow(index);
			return true;
		}

		var element = repeater.GetOrCreateElement(index);
		if (element is null)
		{
			return true;
		}

		// Only a group header is a valid target: after a re-sort or source swap the identity's index
		// could now name a data row, and focusing that with a header's intent would be wrong.
		if (element is not TableViewGroupHeader)
		{
			return true;
		}

		if (!IsRowElementArranged(element))
		{
			// The layout can re-project the source; the header must still be this group's, at index.
			var expectedItem = GetRowsSourceItemAt(index);
			int revalidatedIndex = -1;
			bool sameHeader = ArrangeAndRevalidate(element, index, expectedItem) &&
				m_tableViewSourceRowMetadata is not null &&
				m_tableViewSourceRowMetadata.TryGetIndexForIdentity(identity, out revalidatedIndex) &&
				revalidatedIndex == index &&
				element is TableViewGroupHeader;
			if (!sameHeader)
			{
				if (!finalAttempt)
				{
					return false;
				}
				if (!fromPointerGesture && expectedItem is not null)
				{
					BeginPendingBodyFocus(index, m_currentCellColumn, false /* cellLevel */, null, expectedItem);
					m_pendingReshapeFocusState = focusState == FocusState.Unfocused ? FocusState.Keyboard : focusState;
				}
				return true;
			}
			if (!IsRowElementArranged(element) && !finalAttempt)
			{
				return false;
			}
		}

		if (element is Control control)
		{
			control.Focus(focusState);
		}

		// The body scrolls itself to the header; finish through the attempt loop if the header still
		// sits at an estimated position outside the viewport.
		if (!fromPointerGesture && !IsRowElementInViewport(element))
		{
			BringRowElementIntoView(element, null);
			BeginPendingBodyFocus(index, m_currentCellColumn, false /* cellLevel */, null);
			m_pendingReshapeFocusState = focusState == FocusState.Unfocused ? FocusState.Keyboard : focusState;
		}
		return true;
	}

	// ----- Grouping commands -----
	// Expands or collapses every group in one batch. Individual groups are expanded and collapsed
	// through the group header's ExpandCollapse pattern or by clicking the header itself; these
	// are the programmatic bulk counterparts. Both are no-ops when the source is not grouped.
	/// <summary>
	/// Expands every group in one batch. A no-op when the source is not grouped.
	/// </summary>
	public void ExpandAllGroups()
	{
		SetBulkExpansion(true, BulkExpansionAxis.Groups);
	}

	/// <summary>
	/// Collapses every group in one batch. A no-op when the source is not grouped.
	/// </summary>
	public void CollapseAllGroups()
	{
		SetBulkExpansion(false, BulkExpansionAxis.Groups);
	}

	/// <summary>
	/// Expands every expandable row in one batch. A no-op when the source is not hierarchical.
	/// </summary>
	public void ExpandAllRows()
	{
		SetBulkExpansion(true, BulkExpansionAxis.Rows);
	}

	/// <summary>
	/// Collapses every expandable row in one batch. A no-op when the source is not hierarchical.
	/// </summary>
	public void CollapseAllRows()
	{
		SetBulkExpansion(false, BulkExpansionAxis.Rows);
	}

	// Bulk counterpart of ApplyGroupExpansionByIdentity for either axis. Runs inline (no UIA
	// callout), but still coalesces behind an in-flight edit.
	private void SetBulkExpansion(bool expand, BulkExpansionAxis axis)
	{
		if (m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		// Keep documented no-ops from committing an open edit.
		bool hasAxis = (axis == BulkExpansionAxis.Rows)
			? m_tableViewSourceRowMetadata.IsHierarchicalSource()
			: IsTableViewSourceGrouped();
		if (!hasAxis)
		{
			return;
		}

		// Stamped at the request, so a per-item request made after this one - even while this one is
		// deferred behind an ending edit - is not mistaken for an older intent.
		++BulkExpansionGeneration(axis);

		ApplyBulkExpansion(expand, axis);
	}

	private void ApplyBulkExpansion(bool expand, BulkExpansionAxis axis)
	{
		if (m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		// Older deferred work runs first, so a provider it replaces does not make this request look stale.
		FlushDeferredEditReshapes();
		// That work may have removed the axis; a documented no-op must not end an open edit.
		if (m_tableViewSourceRowMetadata is null ||
			!((axis == BulkExpansionAxis.Rows) ? m_tableViewSourceRowMetadata.IsHierarchicalSource() : IsTableViewSourceGrouped()))
		{
			return;
		}
		ulong generation = m_rowMetadataGeneration;
		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending)
			{
				QueueCoalescedEditReshape(() =>
				{
					ApplyBulkExpansion(expand, axis);
				});
			}
			return;
		}

		// Both axes: row expansion under groups also recycles focused header containers.
		var focusedGroupIdentity = CaptureFocusedContainerForRestore();

		bool changed = false;
		try
		{
			// Local strong ref, as in ApplyGroupExpansionByIdentity; edit-ending handlers may have
			// replaced or cleared the source.
			var provider = m_tableViewSourceRowMetadata;
			if (provider is not null && generation == m_rowMetadataGeneration)
			{
				switch (axis)
				{
					case BulkExpansionAxis.Groups:
						if (expand)
						{
							provider.ExpandAllGroups();
						}
						else
						{
							provider.CollapseAllGroups();
						}
						break;

					case BulkExpansionAxis.Rows:
						if (expand)
						{
							provider.ExpandAllRows();
						}
						else
						{
							provider.CollapseAllRows();
						}
						break;
				}

				changed = true;
			}
		}
		catch (Exception)
		{
			// Best-effort: the grouping source or its state can change during cleanup.
		}

		DrainCoalescedEditReshape();

		if (changed)
		{
			QueueGroupExpansionRowRefresh();
			// Sync for rows that kept their index; queued for rows whose index the splice shifted.
			RefreshRealizedRowHierarchyState();
			QueueRefreshRealizedRowHierarchyState();

			// Both axes: under a grouped tree, row expansion changes group contents.
			RaiseGroupStructureChanged();
		}

		RestoreContainerFocusIfPending(focusedGroupIdentity);
	}

	// Restamps every realized row; rows that kept their index after a reshape are stale.
	private void RefreshRealizedRowHierarchyState()
	{
		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return;
		}

		ForEachRealizedRow(row =>
		{
			// A container awaiting recycle reports -1; its state is about to be re-derived on prepare.
			var index = repeater.GetElementIndex(row);
			if (index >= 0)
			{
				RefreshRowHierarchyState(row, index);
			}
		});
	}

	private void RaiseGroupStructureChanged()
	{
		QueueTerminalGridLineRefresh();

		if (!AutomationPeer.ListenerExists(AutomationEvents.StructureChanged))
		{
			return;
		}

		if (FrameworkElementAutomationPeer.FromElement(this) is TableViewAutomationPeer peer)
		{
			peer.RaiseStructureChangedForGroupExpansion();
		}
	}

	// ---------------------------------------------------------------------------------------------
	// Group-key text
	// ---------------------------------------------------------------------------------------------

	private DecimalFormatter? GetGroupKeyDecimalFormatter()
	{
		if (m_groupKeyDecimalFormatter is null)
		{
			try
			{
				m_groupKeyDecimalFormatter = TableViewDetails.CreateCurrentCultureDecimalFormatter();
				if (m_groupKeyDecimalFormatter is not null)
				{
					m_groupKeyDefaultFractionDigits = m_groupKeyDecimalFormatter.FractionDigits;
				}
			}
			catch (Exception)
			{
				m_groupKeyDecimalFormatter = null;
				m_groupKeyDefaultFractionDigits = 0;
			}
		}

		return m_groupKeyDecimalFormatter;
	}

	private string StringifyGroupKey(object? key)
	{
		if (key is null)
		{
			return LocalizedOrFallback(ResourceAccessor.SR_TableViewGroupHeaderNull, "(null)");
		}

		if (SharedHelpers.IsStringable(key))
		{
			try
			{
				return SharedHelpers.StringableToString(key);
			}
			catch (Exception)
			{
				// App-supplied ToString may throw; fall through to the remaining strategies rather
				// than letting it escape into the repeater's element-prepared callback.
			}
		}

		// TODO Uno: IPropertyValue projection. try_as<IPropertyValue>() + Type() becomes ValueConversionHelpers.TryGetPropertyType;
		// WinRT boxes only value types and strings, so any other reference type is not an IPropertyValue.
		if (ValueConversionHelpers.TryGetPropertyType(key, out var propertyType))
		{
			var formatter = GetGroupKeyDecimalFormatter();
			try
			{
				switch (propertyType)
				{
					case PropertyType.String:
						return (string)key;
					case PropertyType.Int32:
						if (formatter is not null) { formatter.FractionDigits = 0; return formatter.FormatInt((int)key); }
						break;
					case PropertyType.Int64:
						if (formatter is not null) { formatter.FractionDigits = 0; return formatter.FormatInt((long)key); }
						break;
					case PropertyType.UInt32:
						if (formatter is not null) { formatter.FractionDigits = 0; return formatter.FormatUInt((uint)key); }
						break;
					case PropertyType.Double:
						if (formatter is not null) { formatter.FractionDigits = m_groupKeyDefaultFractionDigits; return formatter.FormatDouble((double)key); }
						break;
					default:
						break;
				}
			}
			catch (Exception)
			{
			}
		}

		// A C# IGrouping<TKey,TItem>.Key projects through ICustomPropertyProvider. GetValue runs
		// consumer reflection and can throw during measure -- degrade to the fallback label.
		if (key is ICustomPropertyProvider customProperties)
		{
			try
			{
				if (customProperties.GetCustomProperty("Key") is { } keyProperty)
				{
					return StringifyGroupKey(keyProperty.GetValue(key));
				}
			}
			catch (Exception)
			{
			}
		}

		return LocalizedOrFallback(ResourceAccessor.SR_TableViewGroupHeaderFallback, "(group)");
	}

	// ---------------------------------------------------------------------------------------------
	// Group-header containers
	// ---------------------------------------------------------------------------------------------

	internal string GetGroupHeaderNameCandidate(GroupedEntry entry)
	{
		var key = GetGroupHeaderKey(entry);
		return TableViewAutomationHelpers.GroupInfoToName(new TableViewGroupInfo(
			key, entry.GroupItemCount(), 0, false, false, StringifyGroupKey(key)));
	}

	private void PrepareGroupHeaderElement(TableViewGroupHeader? header, int index)
	{
		if (header is null)
		{
			return;
		}

		TableViewRowInfo rowInfo = default;
		// Trust the index's metadata only when it actually describes a group header. A realized header
		// can be re-prepared (OnRowElementIndexChanged) at an index that has just become a DATA row,
		// where hasRowInfo is true but Kind == Data -- taking IsExpandable/IsExpanded from that would
		// push false/false onto a real group header and give it a dead chevron. Fall back to the
		// entry's own state in that window; the repeater re-prepares this position with the right
		// container type immediately after.
		bool hasRowInfo =
			TryGetTableViewSourceRowInfo(index, ref rowInfo) && rowInfo.Kind == TableViewRowKind.GroupHeader;

		var headerImpl = header;
		headerImpl.SetOwningTableViewInternal(this);

		// Subscribe ONCE per container: it is pooled and re-prepared many times, and subscribing per
		// prepare would fan one band gesture into N toggles. The guard lives here because containers
		// come from a DataTemplate and have no single construction site to hook.
		if (!headerImpl.IsToggleHooked())
		{
			var weakThis = new WeakReference<TableView>(this);
			header.ToggleRequested +=
				(sender, _) =>
				{
					if (weakThis.TryGetTarget(out var tableView))
					{
						tableView.ToggleGroupExpansion(sender);
					}
				};
			headerImpl.SetToggleHooked();
		}

		object? groupKey = null;
		int itemCount = 0;
		int level = 0;
		bool isExpanded = false;
		bool isExpandable = false;

		// Resolve from the PROJECTION at `index`: a reshape can rebind a pooled header in place, so its
		// DataContext may still be the previous projection's entry. DataContext is only the fallback.
		object? groupRow = null;
		if (hasRowInfo && m_rowsItemsSourceView is { } rowsView && index >= 0 && index < rowsView.Count)
		{
			try
			{
				groupRow = rowsView.GetAt(index);
			}
			catch (Exception)
			{
				// Best-effort: the projection can change underneath a deferred reshape.
			}
		}
		if (groupRow is null)
		{
			groupRow = header.DataContext;
			if (groupRow is TableViewGroupInfo)
			{
				// Already prepared once: DataContext is the projection set below, not the entry.
				groupRow = headerImpl.GroupEntryInternal();
			}
		}

		if (GroupedEntry.TryGetGroupedEntry(groupRow) is { } entry)
		{
			// TableViewGroupInfo.Key is contracted (TableView.idl) as the GroupBy key, not the
			// internal group object. entry->Group() is the ShapedGroup (an ICollectionViewGroup);
			// unwrap it to the key it carries so an app template binding {Binding Key} sees the key
			// value, not the projection wrapper. KeyText / display is unaffected either way.
			groupKey = GetGroupHeaderKey(entry);
			// Prefer row metadata (root count) over the entry's span count under a hierarchy; the entry is
			// the fallback while metadata describes a different row.
			itemCount = hasRowInfo ? rowInfo.ChildCount : entry.GroupItemCount();
			isExpanded = hasRowInfo ? rowInfo.IsExpanded : entry.IsExpanded();
			isExpandable = hasRowInfo ? rowInfo.IsExpandable : (entry.GroupItemCount() > 0);
			level = hasRowInfo ? Math.Max(0, rowInfo.Level) : 0;
		}
		else
		{
			// Reachable only if a non-GroupedEntry row value is ever classified as a header (it is not
			// today, since GetRowKindForItem derives the kind from exactly this probe). Guard on
			// hasRowInfo so this cannot silently render a default-initialized TableViewRowInfo --
			// IsExpandable=false would give a dead chevron.
			groupKey = groupRow;
			if (hasRowInfo)
			{
				itemCount = rowInfo.ChildCount;
				isExpanded = rowInfo.IsExpanded;
				isExpandable = rowInfo.IsExpandable;
				level = Math.Max(0, rowInfo.Level);
			}
		}

		// Forward an app-supplied template, else fall back to the control's own. ClearValue removes
		// only the local value, so the default Style's ContentTemplate setter re-applies through DP
		// precedence -- a recycled header can never keep a previous render's template.
		if (GroupHeaderTemplate is { } appTemplate)
		{
			header.ContentTemplate = appTemplate;
		}
		else
		{
			header.ClearValue(ContentControl.ContentTemplateProperty);
		}

		// Keep bindings live within a group, but never retarget an object held by an open menu to a
		// different group when its header is recycled.
		var keyText = StringifyGroupKey(groupKey);
		var existing = header.Content as TableViewGroupInfo;

		// Announce only when THIS group's reported state changed. Two things make the naive "did the
		// boolean flip" test wrong:
		//
		//  - The container is pooled and its projection survives recycling, so a header handed to a
		//    different group would otherwise report that group's state as a change on the new one --
		//    a pure scroll, announced to the user as a collapse. Comparing the key is what makes the
		//    announcement belong to a group rather than to a container.
		//  - The peer reports LeafNode when the group is not expandable, so the announced pair has to
		//    be built the same way the peer builds it, or a cached client ends up holding a value the
		//    provider will never return.
		//
		// Raised from here, not from the IsExpanded DP: a recycled container often already carries the
		// incoming value, so the DP never changes and a DP-based raise stays silent.
		bool sameGroup = existing is not null && TableViewGroupInfo.SameGroupKey(existing.Key, groupKey);
		var previousState = existing is null ? ExpandCollapseState.LeafNode
			: !existing.IsExpandable ? ExpandCollapseState.LeafNode
			: existing.IsExpanded ? ExpandCollapseState.Expanded
			: ExpandCollapseState.Collapsed;
		var newState = !isExpandable ? ExpandCollapseState.LeafNode
			: isExpanded ? ExpandCollapseState.Expanded
			: ExpandCollapseState.Collapsed;

		if (sameGroup)
		{
			existing!.UpdateInternal(groupKey, itemCount, level, keyText);
		}
		else
		{
			header.Content = new TableViewGroupInfo(
				groupKey, itemCount, level, isExpandable, isExpanded, keyText);
		}

		header.IsExpandable = isExpandable;
		header.IsExpanded = isExpanded;

		// Expose the projection as the header's DataContext, as a data row exposes its item, so
		// bindings in a group header Style and in its ContextFlyout (the flyout takes its target's
		// DataContext) see TableViewGroupInfo rather than the internal projection entry.
		headerImpl.SetGroupEntryInternal(groupRow);
		if (header.Content is { } info && !ReferenceEquals(header.DataContext, info))
		{
			header.DataContext = info;
		}

		if (sameGroup && previousState != newState)
		{
			headerImpl.RaiseExpandCollapseStateChanged(previousState, newState);
		}

		UpdateGroupHeaderWidth(header);
	}

	private void ForEachRealizedGroupHeader(Action<TableViewGroupHeader, int> fn)
	{
		var repeater = m_rowsRepeater;
		if (repeater is null || fn is null)
		{
			return;
		}

		var childCount = VisualTreeHelper.GetChildrenCount(repeater);
		for (int i = 0; i < childCount; ++i)
		{
			if (VisualTreeHelper.GetChild(repeater, i) is TableViewGroupHeader header)
			{
				var index = repeater.GetElementIndex(header);
				if (index < 0)
				{
					continue;
				}

				fn(header, index);
			}
		}
	}

	private void RefreshRealizedGroupHeadersAfterExpansion()
	{
		if (!IsTableViewSourceGrouped())
		{
			return;
		}

		ForEachRealizedGroupHeader((header, index) =>
		{
			PrepareGroupHeaderElement(header, index);
		});
	}

	private void OnGroupHeaderTemplatePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (ReferenceEquals(args.OldValue, args.NewValue))
		{
			return;
		}

		ForEachRealizedGroupHeader((header, index) =>
		{
			PrepareGroupHeaderElement(header, index);
		});
	}

	private void ClearGroupHeaderElement(TableViewGroupHeader? header)
	{
		if (header is null)
		{
			return;
		}

		// Leave Content in place for a same-group prepare. A different group gets a fresh projection,
		// so references held by an open menu retain their original identity. Drop the app-owned
		// template so a GroupHeaderTemplate change while this header sits in the pool cannot resurface.
		//
		// The owning-TableView weak ref is deliberately NOT cleared. On the element-factory path the
		// repeater leaves recycled containers parented, so an AT client that holds a provider across a
		// scroll can still call Expand()/Row()/ContainingGrid() on this element -- nulling the owner
		// turns those into silent no-ops, which is worse than the ancestor walk it replaced. A weak
		// ref costs nothing to keep and stays correct.
		header.ResetPointerStateInternal();
		header.ClearValue(ContentControl.ContentTemplateProperty);
		header.SetGroupEntryInternal(null);
	}

	private void UpdateGroupHeaderWidth(TableViewGroupHeader? header)
	{
		if (header is null)
		{
			return;
		}

		// The band sizes itself in TableViewGroupHeader::MeasureOverride, from the same visible-columns
		// sum the cells panel uses. All that is needed when columns move is to ask for a fresh measure
		// -- pushing a Width here races the layout and produces a short band on grow, because column
		// ActualWidths are not final when column resolve runs.
		header.InvalidateMeasure();
	}
}
