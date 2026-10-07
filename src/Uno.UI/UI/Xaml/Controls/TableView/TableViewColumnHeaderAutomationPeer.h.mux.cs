// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeaderAutomationPeer.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// UIA peer for a TableView column header; reports header name, type, identity, and bounds.
// The peer takes the TableView as its owner so headers can be enumerated before the header
// templates realize. Because every header peer therefore shares the same owner, the identity
// the owner would supply is not unique, so this peer derives its own stable per-column
// RuntimeId and AutomationId from the column instead - see GetRuntimeIdCore.
partial class TableViewColumnHeaderAutomationPeer
{
	// TableViewColumnHeaderAutomationPeer(winrt::TableView const& owner, winrt::TableViewColumn const& column);

	// IAutomationPeerOverrides
	// hstring GetClassNameCore();
	// hstring GetNameCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();
	// Stable, distinct per-column identity. Both are derived from the column rather than the
	// shared TableView owner, so headers are individually addressable by assistive technology.
	// winrt::com_array<int32_t> GetRuntimeIdCore();
	// hstring GetAutomationIdCore();
	// Reports the column's current sort state, so a screen-reader user can tell a sorted header
	// from an unsorted one without relying on the chevron.
	// hstring GetHelpTextCore();
	// Invoke rather than Toggle: the sort cycle is not a two-state toggle, and the number of
	// states depends on the column's SortCycle. Only offered for a column the control will
	// actually sort - see GetPatternCore.
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface patternInterface);

	// IInvokeProvider
	// void Invoke();
	// Complements the distinct RuntimeId: AT announces "column i of n" for spatial context.
	// int32_t GetPositionInSetCore();
	// int32_t GetSizeOfSetCore();

	// Prevent each header from exposing the whole TableView subtree.
	// winrt::Windows::Foundation::Collections::IVector<winrt::AutomationPeer> GetChildrenCore();

	// Resolve bounds from this header's realized cell, not the TableView owner.
	// winrt::Windows::Foundation::Rect GetBoundingRectangleCore();
	// winrt::Windows::Foundation::Point GetClickablePointCore();

	// private:
	// winrt::FrameworkElement GetHeaderElement();
	// bool IsSortableColumn();
	// int32_t GetColumnIndex();

	private WeakReference<TableViewColumn>? m_column = null;
	// Captured at construction from the column's stable IUnknown so identity survives the
	// column being released; the peer must not resurrect the column just to report an id.
	private int[] m_columnRuntimeIdParts = { 0, 0 };
}
