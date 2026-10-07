// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewColumnHeaderAutomationPeer.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// UIA peer for a TableView column header; reports header name, type, identity, and bounds.
// Cached/public peers are owned by the TableView so headers can be enumerated before the
// templates realize; realized header cells use the same per-column identity.
partial class TableViewColumnHeaderAutomationPeer
{
	// TableViewColumnHeaderAutomationPeer(winrt::TableView const& owner, winrt::TableViewColumn const& column);
	// TableViewColumnHeaderAutomationPeer(winrt::FrameworkElement const& header,
	//     winrt::TableView const& table, winrt::TableViewColumn const& column);

	// IAutomationPeerOverrides
	// hstring GetClassNameCore();
	// hstring GetNameCore();
	// bool IsEnabledCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();
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

	// Prevent TableView-owned header peers from exposing the whole TableView subtree.
	// winrt::Windows::Foundation::Collections::IVector<winrt::AutomationPeer> GetChildrenCore();
	// winrt::Windows::Foundation::Rect GetBoundingRectangleCore();
	// winrt::Windows::Foundation::Point GetClickablePointCore();
	// bool IsOffscreenCore();

	// bool IsTableViewOwned();

	// private:
	// winrt::FrameworkElement GetHeaderElement();
	// bool IsSortableColumn();
	// int32_t GetColumnIndex();

	private readonly WeakReference<TableViewColumn>? m_column;
	private readonly WeakReference<TableView>? m_table;
	private readonly uint m_columnAutomationIdentity;
}
