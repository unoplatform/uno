// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewAutomationPeer.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewAutomationPeer
{
	// TableViewAutomationPeer(winrt::TableView const& owner);

	// IAutomationPeerOverrides
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);
	// hstring GetClassNameCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();

	// IGridProvider / ITableProvider expose visible logical columns.
	// Headers remain semantic cell names even when the header strip is hidden.
	// int32_t RowCount();
	// int32_t ColumnCount();
	// winrt::IRawElementProviderSimple GetItem(int32_t row, int32_t column);

	// ITableProvider — TableView has column headers but no row-header concept.
	// winrt::RowOrColumnMajor RowOrColumnMajor();
	// winrt::com_array<winrt::IRawElementProviderSimple> GetRowHeaders();
	// winrt::com_array<winrt::IRawElementProviderSimple> GetColumnHeaders();

	// ISelectionProvider — row-scoped, single selection.
	// winrt::com_array<winrt::IRawElementProviderSimple> GetSelection();
	// bool CanSelectMultiple();
	// bool IsSelectionRequired();

	// IItemContainerProvider — lets AT clients enumerate beyond realized rows.
	// winrt::IRawElementProviderSimple FindItemByProperty(
	//     winrt::IRawElementProviderSimple const& startAfter,
	//     winrt::AutomationProperty const& property,
	//     winrt::IInspectable const& value);

	// Internal — raised by TableView when a programmatic shaping verb rewrote the projection.
	// A reshape has no input event behind it, so this is the only signal an AT client gets that
	// the rows it cached are stale.
	// void RaiseStructureChangedForSortChange();
	// void RaiseStructureChangedForVirtualizationReset();
	// Expanding or collapsing a group adds or removes whole runs of rows, so every cached child
	// of the TableView peer may be stale.
	// void RaiseStructureChangedForGroupExpansion();

	// private:
	// One peer per column, kept alive for as long as the column stays in Columns(). Each call
	// to GetColumnHeaders must hand back the same provider for a given column: minting a fresh
	// peer per call yields unstable provider identity and leaves the returned providers with no
	// owner keeping them alive.
	private struct ColumnHeaderPeerCacheEntry
	{
		public WeakReference<TableViewColumn>? column;
		public AutomationPeer? peer;
	}

	// winrt::AutomationPeer GetOrCreateColumnHeaderPeer(
	//     winrt::TableView const& tableView,
	//     winrt::TableViewColumn const& column);

	// void RaiseStructureChanged(winrt::AutomationStructureChangeType const& structureChangeType);
	// com_ptr<TableView> GetImpl();

	private List<ColumnHeaderPeerCacheEntry> m_columnHeaderPeerCache = new();
}
