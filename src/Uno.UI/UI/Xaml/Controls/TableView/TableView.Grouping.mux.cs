// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Grouping.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

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

	private void RequestGroupExpansion(UIElement container, bool? desired)
	{
		if (m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		// Resolve identity NOW, while this container's index is still current. The mutation below is
		// deferred, and the index is not stable across that gap: a scroll can recycle this container
		// onto a different group, or out of view entirely (GetElementIndex returns -1, silently
		// dropping the request). Identity is index-independent once captured.
		string identity = TryGetContainerIdentity(container);

		// Capture keyboard focus on this header (if any) so it can be restored after the deferred
		// reshape recycles the container. Done here, while the container still owns focus.
		CaptureGroupHeaderFocusForRestore(container, identity);

		QueueGroupExpansionByIdentity(identity, desired);
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

	private void CaptureGroupHeaderFocusForRestore(UIElement? container, string identity)
	{
		// Clear any prior capture: a fresh toggle supersedes an earlier one whose restore has not run.
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;

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

		if (container is Control control)
		{
			// Pointer focus draws no focus visual, so there is nothing to restore for a band click.
			// Keyboard (and programmatic, e.g. a test driving Focus) carry a visual worth preserving.
			var state = control.FocusState;
			if (state == FocusState.Keyboard || state == FocusState.Programmatic)
			{
				m_pendingGroupFocusIdentity = identity;
				m_pendingGroupFocusState = state;
			}
		}
	}

	private string CaptureFocusedGroupHeaderForRestore()
	{
		// A bulk expand/collapse supersedes any earlier capture whose restore has not run, exactly as
		// a fresh single-group toggle does.
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;

		var root = XamlRoot;
		if (root is null)
		{
			return "";
		}

		// Walk out from the focused element to its header container, if it is inside one. Focus on a
		// data row is deliberately not captured.
		var focused = FocusManager.GetFocusedElement(root) as DependencyObject;
		for (DependencyObject? node = focused; node is not null; node = VisualTreeHelper.GetParent(node))
		{
			if (node is TableViewGroupHeader header)
			{
				var identity = TryGetContainerIdentity(header);
				CaptureGroupHeaderFocusForRestore(header, identity);
				return identity;
			}
		}

		return "";
	}

	// Directional request from a UIA provider or the band gesture. Identity is resolved here, while
	// the container's index is still current, because the mutation below is deferred.
	private void QueueGroupExpansionByIdentity(string identity, bool? desired)
	{
		if (string.IsNullOrEmpty(identity))
		{
			return;
		}

		// Capture the intent, not just the target. A directionless toggle applied on a later turn is
		// not idempotent: two Expand() calls would expand then collapse. Carrying `desired` through
		// makes the deferred half an idempotent set.
		var generation = m_rowMetadataGeneration;

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
					strongThis.ApplyGroupExpansionByIdentity(identity, desired, generation);
				}
			});
			return;
		}

		// No dispatcher means this is not a UI thread (test host), so there is no in-flight callout to
		// unwind and the hazard above cannot apply.
		ApplyGroupExpansionByIdentity(identity, desired, generation);
	}

	private void ApplyGroupExpansionByIdentity(string identity, bool? desired, ulong generation)
	{
		// Identities are value-based strings, not tied to a provider instance. If ItemsSource was
		// replaced while this request sat on the queue, the same string could name an unrelated group
		// in the new source -- exactly the wrong-group mutation this path exists to prevent.
		if (generation != m_rowMetadataGeneration)
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
					ApplyGroupExpansionByIdentity(identity, desired, generation);
				});
			}
			return;
		}

		bool changed = false;
		try
		{
			if (desired.HasValue)
			{
				// Idempotent set: applying the state we already have is a no-op in the provider.
				if (desired.Value)
				{
					m_tableViewSourceRowMetadata.Expand(identity);
				}
				else
				{
					m_tableViewSourceRowMetadata.Collapse(identity);
				}
				changed = true;
			}
			else
			{
				changed = m_tableViewSourceRowMetadata.Toggle(identity);
			}
		}
		catch (Exception)
		{
			// Best-effort: the grouping source or its state can change during cleanup.
		}

		DrainCoalescedEditReshape();

		// Announce only once the tree has actually moved. Raising while the mutation was still queued
		// told clients to re-read a structure that had not changed yet.
		if (changed)
		{
			RaiseGroupStructureChanged();
		}

		// Restore keyboard focus to the toggled header even when nothing structurally changed: a
		// non-expandable/no-op toggle still ran the container through the reshape path, and leaving
		// focus stranded is the very bug this guards. Matches on identity, so an unrelated queued
		// toggle does not consume this restore.
		RestoreGroupHeaderFocusIfPending(identity);
	}

	private void RestoreGroupHeaderFocusIfPending(string identity)
	{
		if (string.IsNullOrEmpty(m_pendingGroupFocusIdentity) || m_pendingGroupFocusIdentity != identity)
		{
			return;
		}

		var focusState = m_pendingGroupFocusState;
		m_pendingGroupFocusIdentity = "";
		m_pendingGroupFocusState = FocusState.Unfocused;

		// Defer to after the reshape's relayout. The reprojection triggered by the toggle recycles the
		// header container during the next layout pass; focusing now would land on a container layout
		// is about to recycle, dropping focus a second time. A one-shot LayoutUpdated fires once the
		// tree has settled, mirroring FocusRow's deferred-focus pattern.
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

			strongThis.FocusGroupHeaderByIdentity(identity, focusState);
		};
		LayoutUpdated += onLayoutUpdated;
		m_pendingGroupFocusLayoutToken.Disposable = Disposable.Create(() => LayoutUpdated -= onLayoutUpdated);
	}

	private void FocusGroupHeaderByIdentity(string identity, FocusState focusState)
	{
		if (string.IsNullOrEmpty(identity) || m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		int index = -1;
		if (!m_tableViewSourceRowMetadata.TryGetIndexForIdentity(identity, out index) || index < 0)
		{
			return;
		}

		var repeater = m_rowsRepeater;
		if (repeater is null)
		{
			return;
		}

		var element = repeater.GetOrCreateElement(index);
		if (element is null)
		{
			return;
		}

		// Only a group header is a valid target: after a re-sort or source swap the identity's index
		// could now name a data row, and focusing that with a header's intent would be wrong.
		if (element is not TableViewGroupHeader)
		{
			return;
		}

		if (element is FrameworkElement frameworkElement)
		{
			frameworkElement.StartBringIntoView();
		}

		if (element is Control control)
		{
			control.Focus(focusState);
		}
	}

	// ----- Grouping commands -----
	// Expands or collapses every group in one batch. Individual groups are expanded and collapsed
	// through the group header's ExpandCollapse pattern or by clicking the header itself; these
	// are the programmatic bulk counterparts. Both are no-ops when the source is not grouped.
	public void ExpandAllGroups()
	{
		SetAllGroupsExpansion(true);
	}

	public void CollapseAllGroups()
	{
		SetAllGroupsExpansion(false);
	}

	// Bulk counterpart of ApplyGroupExpansionByIdentity. No UIA callout to unwind here - the caller is
	// the app - so this runs inline, but it still has to coalesce behind an in-flight edit for the same
	// reason: the edit sits over a row that the reshape is about to move.
	private void SetAllGroupsExpansion(bool expand)
	{
		if (m_tableViewSourceRowMetadata is null)
		{
			return;
		}

		if (!TryTerminateEditForControlInitiatedReshape())
		{
			if (m_editState == EditState.Ending)
			{
				QueueCoalescedEditReshape(() =>
				{
					SetAllGroupsExpansion(expand);
				});
			}
			return;
		}

		var focusedGroupIdentity = CaptureFocusedGroupHeaderForRestore();

		bool changed = false;
		try
		{
			if (expand)
			{
				m_tableViewSourceRowMetadata.ExpandAllGroups();
			}
			else
			{
				m_tableViewSourceRowMetadata.CollapseAllGroups();
			}
			changed = true;
		}
		catch (Exception)
		{
			// Best-effort: the grouping source or its state can change during cleanup.
		}

		DrainCoalescedEditReshape();

		if (changed)
		{
			RaiseGroupStructureChanged();
		}

		RestoreGroupHeaderFocusIfPending(focusedGroupIdentity);
	}

	private void RaiseGroupStructureChanged()
	{
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

		if (GroupedEntry.TryGetGroupedEntry(header.DataContext) is { } entry)
		{
			// TableViewGroupInfo.Key is contracted (TableView.idl) as the GroupBy key, not the
			// internal group object. entry->Group() is the ShapedGroup (an ICollectionViewGroup);
			// unwrap it to the key it carries so an app template binding {Binding Key} sees the key
			// value, not the projection wrapper. KeyText / display is unaffected either way.
			var groupObject = entry.Group();
			if (groupObject is ICollectionViewGroup collectionViewGroup)
			{
				groupKey = collectionViewGroup.Group;
			}
			else
			{
				groupKey = groupObject;
			}
			itemCount = entry.GroupItemCount();
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
			groupKey = header.DataContext;
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

		// Update the projection in place. Re-pointing Content at a fresh instance every prepare would
		// re-evaluate every binding in the content template and drop any binding the app made against
		// the previous instance.
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

		if (existing is not null)
		{
			existing.UpdateInternal(groupKey, itemCount, level, keyText);
		}
		else
		{
			header.Content = new TableViewGroupInfo(
				groupKey, itemCount, level, isExpandable, isExpanded, keyText);
		}

		header.IsExpandable = isExpandable;
		header.IsExpanded = isExpanded;

		if (sameGroup && previousState != newState)
		{
			headerImpl.RaiseExpandCollapseStateChanged(previousState, newState);
		}

		UpdateGroupHeaderWidth(header);
	}

	private void ClearGroupHeaderElement(TableViewGroupHeader? header)
	{
		if (header is null)
		{
			return;
		}

		// Leave Content in place: the pooled header is handed back for the next group and
		// UpdateInternal re-points the same projection, which is the whole reason bindings survive
		// recycling. Only the app-owned template is dropped, so a GroupHeaderTemplate change while
		// this header sits in the pool cannot resurface.
		//
		// The owning-TableView weak ref is deliberately NOT cleared. On the element-factory path the
		// repeater leaves recycled containers parented, so an AT client that holds a provider across a
		// scroll can still call Expand()/Row()/ContainingGrid() on this element -- nulling the owner
		// turns those into silent no-ops, which is worse than the ancestor walk it replaced. A weak
		// ref costs nothing to keep and stays correct.
		header.ClearValue(ContentControl.ContentTemplateProperty);
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
