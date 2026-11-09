// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\RowMetadataProvider.h, tag winui3/main, commit 79209e041

#nullable enable

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls.Tabular.Primitives;

internal partial class RowMetadataProvider : ITableViewRowMetadataProvider
{
	// TODO Uno: Original C++: using ItemKeySelector = TableViewRowItemKeySelector;
	// C# has no member type aliases; TableViewRowItemKeySelector is used directly.

	// ~RowMetadataProvider();

	// Identity contract:
	// - Flat rows preserve ItemsSourceView/IKeyIndexMapping keys when the source supplies them.
	// - Otherwise m_itemKeySelector supplies item keys. Rows without either identity source
	//   report no stable identity; item string/display representations are never identity.
	// - Group expansion keys are "group:" + canonical stable group identity.
	// - Duplicate canonical group identities fail resolution rather than choosing an arbitrary group.
	// static TableViewRowMetadataProvider CreateForFlatRows(
	//     winrt::ItemsSourceView const& rows,
	//     ItemKeySelector const& itemKeySelector = {});
	// static TableViewRowMetadataProvider CreateForGroupedRows(
	//     winrt::ItemsSourceView const& rows,
	//     GroupedSourceAdapterPtr const& adapter = nullptr,
	//     ItemKeySelector const& itemKeySelector = {});
	// static TableViewRowMetadataProvider CreateForGroupedRows(
	//     GroupedSourceAdapterPtr const& adapter,
	//     ItemKeySelector const& itemKeySelector = {});

	// All rows are data rows; level/expandability come from the adapter descriptor, keys are "node:".
	// static TableViewRowMetadataProvider CreateForHierarchicalRows(
	//     HierarchicalSourceAdapterPtr const& adapter,
	//     ItemKeySelector const& itemKeySelector = {});

	// GroupBy over hierarchy roots. Row indices are the grouped axis (with headers), so node
	// metadata is resolved by item via TryGetNodeRowForItem.
	// static TableViewRowMetadataProvider CreateForGroupedHierarchicalRows(
	//     GroupedSourceAdapterPtr const& groupedAdapter,
	//     HierarchicalSourceAdapterPtr const& hierarchicalAdapter,
	//     ItemKeySelector const& itemKeySelector = {});

	// TableViewRowInfo GetRowInfo(int32_t index) override;
	// winrt::hstring GetIdentity(int32_t index) override;
	// bool TryGetIndexForIdentity(winrt::hstring const& identity, int32_t& index) override;
	// bool Expand(winrt::hstring const& key) override;
	// bool Collapse(winrt::hstring const& key) override;
	// bool Toggle(winrt::hstring const& key) override;
	// bool WouldChangeExpansion(winrt::hstring const& key, std::optional<bool> desired) noexcept override;

	// Bulk group commands. No-ops when the source is not grouped.
	// void ExpandAllGroups() override;
	// void CollapseAllGroups() override;
	// void ExpandAllRows() override;
	// void CollapseAllRows() override;
	// void ExpandSubtree(winrt::hstring const& key) override;

	public bool IsHierarchicalSource() =>
		m_sourceKind == SourceKind.Hierarchical || m_sourceKind == SourceKind.GroupedHierarchical;

	// bool TryGetGroupMembership(
	//     winrt::IInspectable const& rows,
	//     int32_t index,
	//     int32_t& positionInGroup,
	//     int32_t& groupSize) override;

	internal enum SourceKind
	{
		Flat,
		Grouped,
		Hierarchical,
		// Row kinds/identities from the grouped axis; level/expansion from the hierarchy adapter.
		GroupedHierarchical,
	}

	// RowMetadataProvider(
	//     SourceKind sourceKind,
	//     winrt::ItemsSourceView const& flatRows,
	//     winrt::ItemsSourceView const& groupedRows,
	//     GroupedSourceAdapterPtr const& groupedAdapter,
	//     ItemKeySelector const& itemKeySelector,
	//     winrt::ItemsSourceView const& hierarchicalRows = nullptr,
	//     HierarchicalSourceAdapterPtr const& hierarchicalAdapter = nullptr);

	// private:
	// Single implementation behind all six expand/collapse/toggle entry points. They differ only
	// in how the caller names the group (row key vs. the app's GroupBy key), so resolution stays
	// in the wrappers and the state change lives here exactly once. `desired` empty means toggle.
	// Returns true when the expansion state changed.
	// bool SetGroupExpandedCore(winrt::IInspectable const& group, std::optional<bool> desired);

	// Hierarchy equivalent; the node key is the adapter's addressing scheme.
	// bool SetNodeExpandedCore(winrt::hstring const& nodeKey, std::optional<bool> desired);

	// Composed projection routes both key spaces through Expand/Collapse/Toggle.
	// bool IsNodeExpansionKey(winrt::hstring const& key) const;

	// winrt::IInspectable GetGroupedRow(int32_t index) const;
	// winrt::com_ptr<GroupedEntry> TryGetGroupHeaderEntry(int32_t index) const;
	// winrt::IInspectable GetFlatItem(int32_t index) const;

	// winrt::hstring GetItemKey(winrt::IInspectable const& item) const;
	// winrt::hstring GetGroupKey(winrt::IInspectable const& group) const;
	// winrt::hstring GetGroupExpansionKey(winrt::IInspectable const& group) const;
	// winrt::IInspectable ResolveGroupFromKey(winrt::hstring const& key) const;
	// winrt::IInspectable ResolveGroupFromGroupKey(winrt::hstring const& groupKey) const;

	// static bool IsGroupExpansionKey(winrt::hstring const& key);
	// static winrt::hstring AppendPrefix(std::wstring_view prefix, winrt::hstring const& key);
	// static winrt::hstring GetCanonicalGroupKey(winrt::IInspectable const& key);
	// static bool SameObject(winrt::IInspectable const& a, winrt::IInspectable const& b);

	private SourceKind m_sourceKind = SourceKind.Flat;
	private ItemsSourceView? m_flatRows = null;
	private ItemsSourceView? m_groupedRows = null;
	private GroupedSourceAdapter? m_groupedAdapter;
	private ItemsSourceView? m_hierarchicalRows = null;
	private HierarchicalSourceAdapter? m_hierarchicalAdapter;
	private TableViewRowItemKeySelector? m_itemKeySelector;

	// Lazily built identity -> row index over the rows this provider wraps. Rebuilt wholesale
	// rather than maintained incrementally: the sources it indexes signal change but not enough
	// of it to patch a map (a grouped expand/collapse splices a range, a re-sort permutes every
	// row), and a map that is wrong is worse than one that is rebuilt.
	// void InvalidateIdentityIndex();
	// void EnsureIdentityIndex();
	private readonly Dictionary<string, int> m_identityToIndex = new();
	private bool m_identityIndexValid = false;

	// Sorted header-band indexes; invalidated on CollectionChanged and keyed to row count. Built by
	// a plain scan: mid-expand a header's count can lead its inserted rows, so striding skips groups.
	// void InvalidateGroupHeaderIndices();
	// void EnsureGroupHeaderIndices(int32_t rowCount);
	private readonly List<int> m_groupHeaderIndices = new();
	private int m_groupHeaderIndicesRowCount = -1;

	// Subscriptions to XAML's ItemsSourceView are held as raw tokens, not auto-revokers.
	//
	// Teardown is guaranteed on the owning UI thread: every strong owner of this provider is a
	// ReferenceTracker (TableViewSource, TableView) whose final_release marshals destruction to the
	// captured DispatcherQueue. So the destructor revokes the subscription directly, with no thread
	// guard. (Before TableViewSource became a ReferenceTracker a GC could destroy this on the
	// finalizer thread, which is why a guard used to be needed.)
	//
	// The weak alive-flag is still held: the destructor resets it first, so a notification that
	// races teardown becomes a no-op under the weak lock -- GC / re-entrancy safety, not threading.
	private readonly SerialDisposable m_groupedRowsChangedToken = new();
	private readonly SerialDisposable m_flatRowsChangedToken = new();
	private readonly SerialDisposable m_hierarchicalRowsChangedToken = new();
	// TODO Uno: std::shared_ptr<bool> mapped to StrongBox<bool>; handlers capture the box and test Value
	// in place of weak_ptr::lock(), and Dispose clears Value and nulls the field in place of m_alive.reset().
	private StrongBox<bool>? m_alive = new(true);
}
