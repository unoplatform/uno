// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\RowMetadataProvider.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
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

#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.
	// TODO Uno: Investigate potential leak: nothing revokes the CollectionChanged subscriptions. The grouped
	// adapter's Entries() view outlives every provider minted over it, so each rebuild leaves one more (inert,
	// weakly-bound) handler on it.

	// Original destructor logic (not executed):
	// RowMetadataProvider::~RowMetadataProvider()
	// {
	//     // Order matters: kill the subscription's effect first, so it is already inert no matter what
	//     // happens below (the handler checks this flag under a weak lock).
	//     m_alive.reset();
	//
	//     // Teardown runs on the owning UI thread: every strong owner of this provider is a
	//     // ReferenceTracker (TableViewSource, TableView) whose final_release marshals destruction to the
	//     // captured DispatcherQueue, so the shared_ptr that drops this object drops it on that thread.
	//     // Revoking the XAML subscription below is therefore safe without a thread guard.
	//     try
	//     {
	//         if (m_groupedRows && m_groupedRowsChangedToken)
	//         {
	//             m_groupedRows.CollectionChanged(m_groupedRowsChangedToken);
	//         }
	//
	//         if (m_flatRows && m_flatRowsChangedToken)
	//         {
	//             m_flatRows.CollectionChanged(m_flatRowsChangedToken);
	//         }
	//     }
	//     catch (...)
	//     {
	//     }
	// }
#endif

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

	internal RowMetadataProvider(
		SourceKind sourceKind,
		ItemsSourceView? flatRows,
		ItemsSourceView? groupedRows,
		GroupedSourceAdapter? groupedAdapter,
		TableViewRowItemKeySelector? itemKeySelector)
	{
		m_sourceKind = sourceKind;
		m_flatRows = flatRows;
		m_groupedRows = groupedRows;
		m_groupedAdapter = groupedAdapter;
		m_itemKeySelector = itemKeySelector;

		// Subscribe to whichever source this provider indexes so the reverse map cannot outlive the
		// rows it describes. Without this a stale identity would resolve to a moved or deleted row.
		//
		// The handler captures a weak alive-flag so a notification that races teardown becomes a no-op
		// once the destructor resets the flag -- GC / re-entrancy safety, independent of threading.
		WeakReference<StrongBox<bool>> weakAlive = new(m_alive!);
		// TODO Uno: Original C++ captures `this` raw. A C# closure over `this` would keep the provider alive for as
		// long as the rows view lives, so `this` is captured weakly; a collected provider makes the handler a no-op,
		// which is what the C++ alive-flag guarantees once the destructor runs.
		WeakReference<RowMetadataProvider> weakThis = new(this);
		NotifyCollectionChangedEventHandler onChanged = (_, _) =>
		{
			if (weakAlive.TryGetTarget(out _) && weakThis.TryGetTarget(out var self))
			{
				self.InvalidateIdentityIndex();
			}
		};

		if (m_groupedRows is not null)
		{
			var groupedRowsView = m_groupedRows;
			groupedRowsView.CollectionChanged += onChanged;
			m_groupedRowsChangedToken.Disposable = Disposable.Create(() => groupedRowsView.CollectionChanged -= onChanged);
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

	public TableViewRowInfo GetRowInfo(int index)
	{
		var kind = TableViewRowKind.Data;
		int groupLevel = 0;
		bool isExpandable = false;
		bool isExpanded = false;
		int childCount = 0;

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
		}

		return new TableViewRowInfo
		{
			Kind = kind,
			Level = groupLevel,
			IsExpandable = isExpandable,
			IsExpanded = isExpanded,
			ChildCount = childCount,
		};
	}

	public string GetIdentity(int index)
	{
		switch (m_sourceKind)
		{
			case SourceKind.Flat:
				if (m_flatRows is null)
				{
					// TODO Uno: Original C++ throws winrt::hresult_out_of_bounds (E_BOUNDS).
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
		}

		throw new ArgumentOutOfRangeException(nameof(index));
	}

	private bool SetGroupExpandedCore(object? group, bool? desired)
	{
		if (group is null || m_groupedAdapter is null)
		{
			return false;
		}

		bool isExpanded = desired ?? !m_groupedAdapter.IsGroupExpanded(group);
		m_groupedAdapter.SetGroupExpanded(group, isExpanded);
		return isExpanded;
	}

	public void Expand(string key)
	{
		SetGroupExpandedCore(ResolveGroupFromKey(key), true);
	}

	public void Collapse(string key)
	{
		SetGroupExpandedCore(ResolveGroupFromKey(key), false);
	}

	public bool Toggle(string key)
	{
		return SetGroupExpandedCore(ResolveGroupFromKey(key), null);
	}

	private object? GetGroupedRow(int index)
	{
		if (m_groupedRows is null || index < 0 || index >= m_groupedRows.Count)
		{
			// TODO Uno: Original C++ throws winrt::hresult_out_of_bounds (E_BOUNDS).
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
		if (m_groupedAdapter is null)
		{
			return;
		}

		m_groupedAdapter.ExpandAll();
	}

	public void CollapseAllGroups()
	{
		if (m_groupedAdapter is null)
		{
			return;
		}

		m_groupedAdapter.CollapseAll();
	}

	private object? GetFlatItem(int index)
	{
		if (m_flatRows is null || index < 0 || index >= m_flatRows.Count)
		{
			// TODO Uno: Original C++ throws winrt::hresult_out_of_bounds (E_BOUNDS).
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
		// TODO Uno: Original C++ compares the canonical IUnknown pointers obtained through try_as<::IUnknown>.
		return a is not null && b is not null && ReferenceEquals(a, b);
	}
}
