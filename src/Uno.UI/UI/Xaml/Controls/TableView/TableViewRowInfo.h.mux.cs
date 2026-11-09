// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowInfo.h, tag winui3/main, commit 79209e041

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal enum TableViewRowKind
{
	Data = 0,
	GroupHeader = 1,
}

// The C++ default member initializers all equal default(TableViewRowInfo).
internal struct TableViewRowInfo
{
	public TableViewRowKind Kind;
	public int Level;
	public bool IsExpandable;
	public bool IsExpanded;
	// GroupHeader: group item count (0 presents a leaf). Hierarchical data row: child count.
	// Otherwise 0.
	public int ChildCount;

	// Hierarchical data rows only. PositionInSet is 1-based within the sibling set (a root's group
	// bucket under grouping); ParentIdentity is empty for roots.
	// Uno specific: a property so default(TableViewRowInfo) still reads an empty hstring rather than null.
	public string ParentIdentity
	{
		readonly get => m_parentIdentity ?? "";
		set => m_parentIdentity = value;
	}
	private string? m_parentIdentity;
	public uint PositionInSet;
	public uint SizeOfSet;
}

// TODO Uno: std::function<winrt::hstring(winrt::IInspectable const&)>; an empty std::function maps to null.
internal delegate string TableViewRowItemKeySelector(object? item);

internal interface ITableViewRowMetadataProvider : IDisposable
{
	// TODO Uno: Original C++: virtual ~ITableViewRowMetadataProvider() = default; mapped to IDisposable so
	// owners can run the destructor through the interface.

	TableViewRowInfo GetRowInfo(int index);
	string GetIdentity(int index);
	// Reverse of GetIdentity. Owned here because this is the only type that knows how a row's
	// identity is derived; consumers that needed it were each scanning every row and calling
	// GetIdentity until one matched.
	bool TryGetIndexForIdentity(string identity, out int index);
	// All three return true when the state changed (collapse included), not the resulting state.
	bool Expand(string key);
	bool Collapse(string key);
	bool Toggle(string key);
	// Whether Expand (desired true), Collapse (false) or Toggle (empty) would change the state; true when unsure.
	bool WouldChangeExpansion(string key, bool? desired);

	// One verb per axis: expanding group headers must not materialize the whole tree.
	void ExpandAllGroups();
	void CollapseAllGroups();
	void ExpandAllRows();
	void CollapseAllRows();

	// True for tree projections. Leaf roots must still reserve chevron width, which row info alone
	// cannot distinguish from grouped data rows.
	bool IsHierarchicalSource() => false;

	// Expands a tree node and all descendants. No-op for group keys or non-hierarchical sources.
	void ExpandSubtree(string key) { }

	// 1-based position among data rows between adjacent header bands, and that count. Returns false
	// if `rows` isn't the provider's projection, or for flat sources, headers, or out of range.
	bool TryGetGroupMembership(
		object? rows,
		int index,
		out int positionInGroup,
		out int groupSize)
	{
		positionInGroup = 0;
		groupSize = 0;
		return false;
	}
}

// TODO Uno: Original C++: using TableViewRowMetadataProvider = std::shared_ptr<ITableViewRowMetadataProvider>;
// C# has no cross-file type alias, so every TableViewRowMetadataProvider is written as ITableViewRowMetadataProvider?.
