// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\RowMetadataProvider.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Specialized;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Tabular.Primitives;

partial class RowMetadataProvider
{
	private const string c_groupExpansionPrefix = "group:";

	private static bool StartsWith(string value, string prefix)
	{
		var view = value.AsSpan();
		return view.Length >= prefix.Length && view.Slice(0, prefix.Length).SequenceEqual(prefix.AsSpan());
	}

	// TODO Uno: C++ destructor; runs when the last shared_ptr owner releases, mapped to Dispose.
	public void Dispose()
	{
		// Order matters: kill the subscription's effect first, so it is already inert no matter what
		// happens below (the handler checks this flag under a weak lock).
		if (m_alive is not null)
		{
			m_alive.Value = false;
			m_alive = null;
		}

		// Teardown runs on the owning UI thread: every strong owner of this provider is a
		// ReferenceTracker (TableViewSource, TableView) whose final_release marshals destruction to the
		// captured DispatcherQueue, so the shared_ptr that drops this object drops it on that thread.
		// Revoking the XAML subscription below is therefore safe without a thread guard.
		try
		{
			if (m_groupedRows is not null)
			{
				m_groupedRowsChangedToken.Disposable = null;
			}

			if (m_flatRows is not null)
			{
				m_flatRowsChangedToken.Disposable = null;
			}

			if (m_hierarchicalRows is not null && m_hierarchicalRowsChangedToken.Disposable is not null)
			{
				m_hierarchicalRowsChangedToken.Disposable = null;
			}
		}
		catch
		{
		}
	}

	internal static ITableViewRowMetadataProvider CreateForFlatRows(
		ItemsSourceView? rows,
		TableViewRowItemKeySelector? itemKeySelector = null)
	{
		return new RowMetadataProvider(
			SourceKind.Flat,
			rows,
			null,
			null,
			itemKeySelector);
	}

	internal static ITableViewRowMetadataProvider CreateForGroupedRows(
		ItemsSourceView? rows,
		GroupedSourceAdapter? adapter = null,
		TableViewRowItemKeySelector? itemKeySelector = null)
	{
		return new RowMetadataProvider(
			SourceKind.Grouped,
			null,
			rows,
			adapter,
			itemKeySelector);
	}

	internal static ITableViewRowMetadataProvider CreateForGroupedRows(
		GroupedSourceAdapter? adapter,
		TableViewRowItemKeySelector? itemKeySelector = null)
	{
		return CreateForGroupedRows(adapter is not null ? adapter.Entries() : null, adapter, itemKeySelector);
	}

	internal static ITableViewRowMetadataProvider CreateForHierarchicalRows(
		HierarchicalSourceAdapter? adapter,
		TableViewRowItemKeySelector? itemKeySelector = null)
	{
		return new RowMetadataProvider(
			SourceKind.Hierarchical,
			null,
			null,
			null,
			itemKeySelector,
			adapter is not null ? adapter.Entries() : null,
			adapter);
	}

	internal static ITableViewRowMetadataProvider CreateForGroupedHierarchicalRows(
		GroupedSourceAdapter? groupedAdapter,
		HierarchicalSourceAdapter? hierarchicalAdapter,
		TableViewRowItemKeySelector? itemKeySelector = null)
	{
		// Only the grouped rows are subscribed: node toggles reach us as grouped-adapter changes,
		// so subscribing to the hierarchy too would invalidate the identity index twice.
		return new RowMetadataProvider(
			SourceKind.GroupedHierarchical,
			null,
			groupedAdapter is not null ? groupedAdapter.Entries() : null,
			groupedAdapter,
			itemKeySelector,
			null,
			hierarchicalAdapter);
	}

	internal RowMetadataProvider(
		SourceKind sourceKind,
		ItemsSourceView? flatRows,
		ItemsSourceView? groupedRows,
		GroupedSourceAdapter? groupedAdapter,
		TableViewRowItemKeySelector? itemKeySelector,
		ItemsSourceView? hierarchicalRows = null,
		HierarchicalSourceAdapter? hierarchicalAdapter = null)
	{
		m_sourceKind = sourceKind;
		m_flatRows = flatRows;
		m_groupedRows = groupedRows;
		m_groupedAdapter = groupedAdapter;
		m_hierarchicalRows = hierarchicalRows;
		m_hierarchicalAdapter = hierarchicalAdapter;
		m_itemKeySelector = itemKeySelector;

		// Subscribe to whichever source this provider indexes so the reverse map cannot outlive the
		// rows it describes. Without this a stale identity would resolve to a moved or deleted row.
		//
		// The handler captures a weak alive-flag so a notification that races teardown becomes a no-op
		// once the destructor resets the flag -- GC / re-entrancy safety, independent of threading.
		var weakAlive = m_alive!;
		NotifyCollectionChangedEventHandler onChanged = (_, _) =>
		{
			if (weakAlive.Value)
			{
				InvalidateIdentityIndex();
				InvalidateGroupHeaderIndices();
			}
		};

		if (m_groupedRows is not null)
		{
			var groupedRowsView = m_groupedRows;
			groupedRowsView.CollectionChanged += onChanged;
			m_groupedRowsChangedToken.Disposable = Disposable.Create(() => groupedRowsView.CollectionChanged -= onChanged);
		}
		else if (m_hierarchicalRows is not null)
		{
			var hierarchicalRowsView = m_hierarchicalRows;
			hierarchicalRowsView.CollectionChanged += onChanged;
			m_hierarchicalRowsChangedToken.Disposable = Disposable.Create(() => hierarchicalRowsView.CollectionChanged -= onChanged);
		}
		else if (m_flatRows is not null)
		{
			var flatRowsView = m_flatRows;
			flatRowsView.CollectionChanged += onChanged;
			m_flatRowsChangedToken.Disposable = Disposable.Create(() => flatRowsView.CollectionChanged -= onChanged);
		}
	}

	private void InvalidateIdentityIndex()
	{
		m_identityIndexValid = false;
		m_identityToIndex.Clear();
	}

	private void EnsureIdentityIndex()
	{
		if (m_identityIndexValid)
		{
			return;
		}

		m_identityToIndex.Clear();
		m_identityIndexValid = true;

		int rowCount = 0;
		switch (m_sourceKind)
		{
			case SourceKind.Flat:
				rowCount = m_flatRows is not null ? m_flatRows.Count : 0;
				break;
			case SourceKind.Grouped:
				rowCount = m_groupedRows is not null ? m_groupedRows.Count : 0;
				break;
			case SourceKind.Hierarchical:
				rowCount = m_hierarchicalRows is not null ? m_hierarchicalRows.Count : 0;
				break;
			case SourceKind.GroupedHierarchical:
				rowCount = m_groupedRows is not null ? m_groupedRows.Count : 0;
				break;
		}

		if (rowCount <= 0)
		{
			return;
		}

		m_identityToIndex.EnsureCapacity(rowCount);
		for (int index = 0; index < rowCount; ++index)
		{
			string identity;
			try
			{
				identity = GetIdentity(index);
			}
			catch (Exception)
			{
				continue;
			}

			if (!string.IsNullOrEmpty(identity))
			{
				// First writer wins, matching the scans this replaces. Identities are validated
				// unique upstream (an ambiguous one throws rather than projecting), so the tie
				// cannot legitimately occur; resolving it consistently just keeps the replacement
				// behaviour-identical if it ever does.
				m_identityToIndex.TryAdd(identity, index);
			}
		}
	}

	public bool TryGetIndexForIdentity(string identity, out int index)
	{
		index = -1;
		if (string.IsNullOrEmpty(identity))
		{
			return false;
		}

		EnsureIdentityIndex();
		if (!m_identityToIndex.TryGetValue(identity, out var found))
		{
			return false;
		}

		index = found;
		return true;
	}

	private void InvalidateGroupHeaderIndices()
	{
		m_groupHeaderIndicesRowCount = -1;
		m_groupHeaderIndices.Clear();
	}

	private void EnsureGroupHeaderIndices(int rowCount)
	{
		if (m_groupHeaderIndicesRowCount == rowCount)
		{
			return;
		}

		m_groupHeaderIndices.Clear();
		for (int i = 0; i < rowCount; ++i)
		{
			if (GroupedEntry.TryGetGroupedEntry(m_groupedRows!.GetAt(i)) is not null)
			{
				m_groupHeaderIndices.Add(i);
			}
		}
		m_groupHeaderIndicesRowCount = rowCount;
	}

	public bool TryGetGroupMembership(
		object? rows,
		int index,
		out int positionInGroup,
		out int groupSize)
	{
		positionInGroup = 0;
		groupSize = 0;

		// Only answer for our own projection, so the index can't resolve against a stale one.
		if (m_sourceKind != SourceKind.Grouped || m_groupedRows is null || rows is null || !SameObject(rows, m_groupedRows))
		{
			return false;
		}

		var rowCount = m_groupedRows.Count;
		if (index < 0 || index >= rowCount)
		{
			return false;
		}

		EnsureGroupHeaderIndices(rowCount);

		// First band after the row; the one before it (if any) opens the row's group.
		// Uno specific: List<int>.BinarySearch has no upper_bound, so derive it from the complement.
		var found = m_groupHeaderIndices.BinarySearch(index);
		var next = found >= 0 ? found + 1 : ~found;
		int previousHeader = next == 0 ? -1 : m_groupHeaderIndices[next - 1];
		if (previousHeader == index)
		{
			// A header band is not a member of any group's data set.
			return false;
		}
		int nextHeader = next == m_groupHeaderIndices.Count ? rowCount : m_groupHeaderIndices[next];

		positionInGroup = index - previousHeader;
		groupSize = nextHeader - previousHeader - 1;
		return true;
	}

	public TableViewRowInfo GetRowInfo(int index)
	{
		var kind = TableViewRowKind.Data;
		int groupLevel = 0;
		bool isExpandable = false;
		bool isExpanded = false;
		int childCount = 0;
		// Set only for hierarchical data rows.
		HierarchicalSourceAdapter.NodeRow? node = null;

		switch (m_sourceKind)
		{
			case SourceKind.Flat:
				_ = GetFlatItem(index);
				break;

			case SourceKind.Grouped:
				{
					var entry = TryGetGroupHeaderEntry(index);
					if (entry is not null)
					{
						kind = TableViewRowKind.GroupHeader;
						childCount = entry.GroupItemCount();
						// An empty group has nothing to expand into, so it presents as a leaf. Callers that
						// previously derived this from GroupItemCount() themselves now read it from here.
						isExpandable = childCount > 0;
						isExpanded = entry.IsExpanded();
					}
					else
					{
						groupLevel = 1;
					}
					break;
				}

			case SourceKind.Hierarchical:
				{
					if (m_hierarchicalAdapter is not null)
					{
						node = m_hierarchicalAdapter.TryGetNodeRow(index);
					}
					break;
				}

			case SourceKind.GroupedHierarchical:
				{
					var entry = TryGetGroupHeaderEntry(index);
					if (entry is not null)
					{
						// Count roots, not rows, so the header count does not change as nodes expand. Falls back to
						// the row count if the projection did not publish one.
						kind = TableViewRowKind.GroupHeader;
						childCount = entry.GroupItemCount();
						if (entry.Group() is ShapingHelpers.IGroupChildCount counted)
						{
							int rootCount = counted.GroupChildCount();
							if (rootCount >= 0)
							{
								childCount = rootCount;
							}
						}
						// Expandability follows rows, so an empty group stays a leaf.
						isExpandable = entry.GroupItemCount() > 0;
						isExpanded = entry.IsExpanded();
						break;
					}

					// Index addresses the grouped axis (with headers); the item is the only handle shared with
					// the hierarchy adapter.
					groupLevel = 1;
					if (m_hierarchicalAdapter is not null)
					{
						node = m_hierarchicalAdapter.TryGetNodeRowForItem(GetGroupedRow(index));
					}
					break;
				}
		}

		TableViewRowInfo info = new()
		{
			Kind = kind,
			Level = groupLevel,
			IsExpandable = isExpandable,
			IsExpanded = isExpanded,
			ChildCount = childCount,
		};
		if (node is not null)
		{
			// Depth is 0-based; Level (and UIA AutomationProperties.Level) is 1-based.
			info.Level = node.Depth + 1;
			info.IsExpandable = node.HasChildren;
			info.IsExpanded = node.IsExpanded;
			info.ChildCount = node.ChildCount;
			info.ParentIdentity = node.ParentKey;
			info.PositionInSet = (uint)node.SiblingIndex;
			info.SizeOfSet = (uint)node.SiblingCount;
		}
		return info;
	}

	public string GetIdentity(int index)
	{
		switch (m_sourceKind)
		{
			case SourceKind.Flat:
				if (m_flatRows is null)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				if (m_flatRows.HasKeyIndexMapping)
				{
					return m_flatRows.KeyFromIndex(index);
				}
				return GetItemKey(GetFlatItem(index));

			case SourceKind.Grouped:
				{
					var entry = TryGetGroupHeaderEntry(index);
					if (entry is not null)
					{
						return GetGroupExpansionKey(entry.Group());
					}
					return GetItemKey(GetGroupedRow(index));
				}

			case SourceKind.Hierarchical:
				{
					// Node key, matching what expand/collapse takes; survives item re-creation.
					if (m_hierarchicalAdapter is not null)
					{
						if (m_hierarchicalAdapter.TryGetNodeRow(index) is { } node)
						{
							return node.NodeKey;
						}
					}
					throw new ArgumentOutOfRangeException(nameof(index));
				}

			case SourceKind.GroupedHierarchical:
				{
					var entry = TryGetGroupHeaderEntry(index);
					if (entry is not null)
					{
						return GetGroupExpansionKey(entry.Group());
					}

					// Node key so identity matches Toggle's addressing. Falls back to the item key mid-reshape.
					var item = GetGroupedRow(index);
					if (m_hierarchicalAdapter is not null)
					{
						if (m_hierarchicalAdapter.TryGetNodeRowForItem(item) is { } node)
						{
							return node.NodeKey;
						}
					}
					return GetItemKey(item);
				}
		}

		throw new ArgumentOutOfRangeException(nameof(index));
	}

	private bool SetGroupExpandedCore(object? group, bool? desired)
	{
		if (group is null || m_groupedAdapter is null)
		{
			return false;
		}

		// Strong ref: a handler of the resulting change may replace this provider mid-call.
		var adapter = m_groupedAdapter;
		bool before = adapter.IsGroupExpanded(group);
		adapter.SetGroupExpanded(group, desired ?? !before);
		return adapter.IsGroupExpanded(group) != before;
	}

	public bool Expand(string key)
	{
		if (IsNodeExpansionKey(key))
		{
			return SetNodeExpandedCore(key, true);
		}
		return SetGroupExpandedCore(ResolveGroupFromKey(key), true);
	}

	public bool Collapse(string key)
	{
		if (IsNodeExpansionKey(key))
		{
			return SetNodeExpandedCore(key, false);
		}
		return SetGroupExpandedCore(ResolveGroupFromKey(key), false);
	}

	public bool Toggle(string key)
	{
		if (IsNodeExpansionKey(key))
		{
			return SetNodeExpandedCore(key, null);
		}
		return SetGroupExpandedCore(ResolveGroupFromKey(key), null);
	}

	// The guards of SetGroupExpandedCore and HierarchicalSourceAdapter::SetNodeExpanded: a group that resolves, or a
	// presented node with children, in another state than the one requested.
	public bool WouldChangeExpansion(string key, bool? desired)
	{
		try
		{
			if (IsNodeExpansionKey(key))
			{
				var row = m_hierarchicalAdapter!.TryGetIndexForNodeKey(key, out var nodeIndex)
					? m_hierarchicalAdapter.TryGetNodeRow(nodeIndex) : null;
				return row is not null && row.HasChildren && (desired is null || row.IsExpanded != desired.Value);
			}

			var group = ResolveGroupFromKey(key);
			return group is not null && m_groupedAdapter is not null && (desired is null || m_groupedAdapter.IsGroupExpanded(group) != desired.Value);
		}
		catch
		{
			return true;
		}
	}

	// Under GroupedHierarchical both "group:" and "node:" keys arrive here; route on the key
	// (disjoint prefixes), not SourceKind.
	private bool IsNodeExpansionKey(string key)
	{
		if (m_hierarchicalAdapter is null || string.IsNullOrEmpty(key))
		{
			return false;
		}

		// Positive prefix test, so a stale or foreign identity cannot become node intent.
		return HierarchicalSourceAdapter.IsNodeKey(key);
	}

	private bool SetNodeExpandedCore(string nodeKey, bool? desired)
	{
		if (m_hierarchicalAdapter is null || string.IsNullOrEmpty(nodeKey))
		{
			return false;
		}

		var adapter = m_hierarchicalAdapter;
		bool before = adapter.IsNodeExpanded(nodeKey);
		adapter.SetNodeExpanded(nodeKey, desired ?? !before);

		// Reports any change (collapse included), not the resulting state.
		return adapter.IsNodeExpanded(nodeKey) != before;
	}

	private object? GetGroupedRow(int index)
	{
		if (m_groupedRows is null || index < 0 || index >= m_groupedRows.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return m_groupedRows.GetAt(index);
	}

	private GroupedEntry? TryGetGroupHeaderEntry(int index)
	{
		// A grouped row is either a header, for which the projection mints a GroupedEntry, or an app
		// item exposed as-is. So this is a genuine test, not an assertion: null means "data row".
		// The IGroupedEntryTag probe is what makes it safe against an arbitrary app object, since a
		// bare try_as<GroupedEntry> succeeds on every WinRT object and then dereferences garbage.
		return GroupedEntry.TryGetGroupedEntry(GetGroupedRow(index));
	}

	public void ExpandAllGroups()
	{
		// Groups only, even when composed: driving the hierarchy too would expand every tree node.
		// See ExpandAllRows.
		if (m_groupedAdapter is { } adapter)
		{
			adapter.ExpandAll();
		}
	}

	public void CollapseAllGroups()
	{
		if (m_groupedAdapter is { } adapter)
		{
			adapter.CollapseAll();
		}
	}

	public void ExpandAllRows()
	{
		// Moves the hierarchy baseline; no app code runs. Does not open group headers when composed.
		if (m_hierarchicalAdapter is { } adapter)
		{
			adapter.ExpandAll();
		}
	}

	public void ExpandSubtree(string key)
	{
		if (IsNodeExpansionKey(key))
		{
			var adapter = m_hierarchicalAdapter!;
			adapter.ExpandSubtree(key);
		}
	}

	public void CollapseAllRows()
	{
		if (m_hierarchicalAdapter is { } adapter)
		{
			adapter.CollapseAll();
		}
	}

	private object? GetFlatItem(int index)
	{
		if (m_flatRows is null || index < 0 || index >= m_flatRows.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return m_flatRows.GetAt(index);
	}

	private string GetItemKey(object? item)
	{
		if (item is null)
		{
			return "";
		}

		if (m_itemKeySelector is not null)
		{
			var key = m_itemKeySelector(item);
			if (!string.IsNullOrEmpty(key))
			{
				return key;
			}
		}

		return "";
	}

	private string GetGroupKey(object? group)
	{
		// Read through the declared ICollectionViewGroup contract, using the same derivation the
		// grouped adapter uses. The two MUST agree: an expansion key produced here is handed back to
		// the adapter's identity lookup, and a mismatch would silently degrade every expand/collapse
		// to the O(rows) scan below.
		var identity = ShapingHelpers.GetGroupKeyIdentity(group);
		if (!string.IsNullOrEmpty(identity))
		{
			return identity;
		}

		return GetCanonicalGroupKey(ShapingHelpers.GetGroupKeyObject(group));
	}

	private string GetGroupExpansionKey(object? group)
	{
		var groupKey = GetGroupKey(group);
		return string.IsNullOrEmpty(groupKey) ? "" : AppendPrefix(c_groupExpansionPrefix, groupKey);
	}

	private object? ResolveGroupFromKey(string key)
	{
		if (!IsGroupExpansionKey(key) || m_groupedRows is null)
		{
			return null;
		}

		return ResolveGroupFromGroupKey(key.Substring(c_groupExpansionPrefix.Length));
	}

	private object? ResolveGroupFromGroupKey(string groupKey)
	{
		if (string.IsNullOrEmpty(groupKey) || m_groupedRows is null)
		{
			return null;
		}

		if (m_groupedAdapter is not null)
		{
			if (m_groupedAdapter.ResolveLiveGroupByIdentity(groupKey) is { } group)
			{
				return group;
			}
		}

		object? resolved = null;
		// Only header rows are GroupedEntry; data rows are the app items themselves and fail the probe.
		for (int i = 0; i < m_groupedRows.Count; ++i)
		{
			if (GroupedEntry.TryGetGroupedEntry(m_groupedRows.GetAt(i)) is { } entry)
			{
				var group = entry.Group();
				if (group is not null && GetGroupKey(group) == groupKey)
				{
					if (resolved is null)
					{
						resolved = group;
					}
					else if (!SameObject(resolved, group))
					{
						return null;
					}
				}
			}
		}

		return resolved;
	}

	private static bool IsGroupExpansionKey(string key)
	{
		return StartsWith(key, c_groupExpansionPrefix);
	}

	private static string AppendPrefix(string prefix, string key)
	{
		return prefix + key;
	}

	private static string GetCanonicalGroupKey(object? key)
	{
		// Delegate to the shaping layer's canonicalizer rather than formatting here. A group key
		// string minted on this side is compared against one minted by the shaping engine, so the
		// two must be produced by the same code: a second implementation drifts silently. It did --
		// this used to format floats in decimal while the engine formats their exact bit pattern,
		// so no float-keyed group could ever match by string and every lookup fell through to the
		// slower object comparison.
		//
		// rejectEmptyString keeps the existing contract that an empty key string means "no usable
		// canonical key", which the caller reads as "fall back to comparing the key objects".
		if (!ShapingHelpers.ValueKey.TryGetStablePropertyKey(key, out var canonicalKey, true))
		{
			return "";
		}

		return canonicalKey;
	}

	private static bool SameObject(object? a, object? b)
	{
		return a is not null && b is not null && ReferenceEquals(a, b);
	}
}
