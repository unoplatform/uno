// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowInfo.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal enum TableViewRowKind
{
	Data = 0,
	GroupHeader = 1,
}

// TODO Uno: The C++ default member initializers (Kind = Data, Level = 0, IsExpandable = false,
// IsExpanded = false, ChildCount = 0) all equal default(TableViewRowInfo), so they are not restated.
internal struct TableViewRowInfo
{
	public TableViewRowKind Kind;
	public int Level;
	public bool IsExpandable;
	public bool IsExpanded;
	// Rows the node owns. Meaningful on GroupHeader rows, where it is the group's item count and
	// the single source of truth for expandability: an empty group must present a leaf, not a
	// chevron that expands into nothing. Always 0 on Data rows today; it becomes the child count
	// when hierarchical (tree) rows land.
	public int ChildCount;
}

// TODO Uno: std::function<winrt::hstring(winrt::IInspectable const&)>; an empty std::function maps to null.
internal delegate string TableViewRowItemKeySelector(object? item);

internal interface ITableViewRowMetadataProvider
{
	// TODO Uno: Original C++: virtual ~ITableViewRowMetadataProvider() = default;

	TableViewRowInfo GetRowInfo(int index);
	string GetIdentity(int index);
	// Reverse of GetIdentity. Owned here because this is the only type that knows how a row's
	// identity is derived; consumers that needed it were each scanning every row and calling
	// GetIdentity until one matched.
	bool TryGetIndexForIdentity(string identity, out int index);
	void Expand(string key);
	void Collapse(string key);
	bool Toggle(string key);

	void ExpandAllGroups();
	void CollapseAllGroups();
}

// TODO Uno: Original C++: using TableViewRowMetadataProvider = std::shared_ptr<ITableViewRowMetadataProvider>;
// C# has no cross-file type alias, so every TableViewRowMetadataProvider is written as ITableViewRowMetadataProvider?.
