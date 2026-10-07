// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowAutomationPeer.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRowAutomationPeer
{
	// TableViewRowAutomationPeer(winrt::TableViewRow const& owner);

	// IAutomationPeerOverrides
	// hstring GetClassNameCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);

	// Expose direct cell wrappers only to avoid deep, costly UIA subtree walks.
	// winrt::IVector<winrt::AutomationPeer> GetChildrenCore();

	// ISelectionItemProvider — the row reports state the owning TableView holds; it never keeps
	// its own copy, because rows are recycled.
	// bool IsSelected();
	// winrt::IRawElementProviderSimple SelectionContainer();
	// void AddToSelection();
	// void RemoveFromSelection();
	// void Select();

	// private:
	// The owning TableView, or null once the row has been recycled out of the tree.
	// winrt::TableView GetOwningTableView();
	// This row's index in the owner's ItemsSource index space, or -1 when unrealized.
	// int32_t GetRowIndex();
}
