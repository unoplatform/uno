// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowAutomationPeer.h, tag winui3/main, commit 79209e041

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewRowAutomationPeer
{
	// TableViewRowAutomationPeer(winrt::TableViewRow const& owner);

	// IAutomationPeerOverrides
	// hstring GetClassNameCore();
	// winrt::AutomationControlType GetAutomationControlTypeCore();
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);

	// IAutomationPeerOverrides. Routes through TableView so focus and stale checks apply to a row
	// style or template ContextFlyout too; falls back to the framework when TableView shows nothing.
	// void ShowContextMenuCore();

	// A TableViewRow is a Control with no content of its own, so the base peer computes no name
	// and AT announces a bare "data item". Compose the visible cell texts instead.
	// hstring GetNameCore();

	// Rows are virtualized: UIA only ever sees the realized window, so the control has to supply
	// "row i of n" on the same flat basis as IGridProvider. A tree row reports its position
	// within its sibling set instead.
	// int32_t GetPositionInSetCore();
	// int32_t GetSizeOfSetCore();

	// Expose direct cell wrappers only to avoid deep, costly UIA subtree walks.
	// winrt::IVector<winrt::AutomationPeer> GetChildrenCore();

	// ISelectionItemProvider — the row reports state the owning TableView holds; it never keeps
	// its own copy, because rows are recycled.
	// bool IsSelected();
	// winrt::IRawElementProviderSimple SelectionContainer();
	// void AddToSelection();
	// void RemoveFromSelection();
	// void Select();

	// IVirtualizedItemProvider — available only after the realized row has been recycled out.
	// void Realize();
	// Creates the row at rowIndex and brings it into view (UIA Realize only, never a read). Throws
	// ElementNotAvailable when it cannot, or when the row is not the expected one.
	// static void RealizeRowAt(
	//     winrt::TableView const& tableView,
	//     int32_t rowIndex,
	//     std::function<bool(winrt::TableViewRow const&)> const& isExpectedRow);

	// Single source of cell-peer identity for GetChildrenCore and Grid.GetItem; UIA compares
	// providers by identity.
	// winrt::AutomationPeer GetOrCreateCellPeer(winrt::FrameworkElement const& cell);
	// bool CanReuseForRowItem(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// void TrackCurrentRowItem(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// void DropCellPeerCache();

	// IExpandCollapseProvider — hierarchical rows only. A leaf reports LeafNode rather than the
	// pattern being withdrawn, matching TableViewGroupHeaderAutomationPeer.
	// winrt::ExpandCollapseState ExpandCollapseState();
	// void Expand();
	// void Collapse();

	// IAutomationPeerOverrides3. Level is the row's 1-based tree depth.
	// int32_t GetLevelCore();

	// Raised by the owning row on expansion change so clients don't read stale state.
	// void RaiseExpandCollapseAutomationEvent(winrt::ExpandCollapseState oldState, winrt::ExpandCollapseState newState);

	// Raised by the owning row on every hierarchy restamp. Clients cache Level/PositionInSet/
	// SizeOfSet, so a reparent or sibling insert must notify; compares against the last values
	// this peer reported and raises only for the ones that moved.
	// void RaiseHierarchyPositionChanged();

	// private:
	// int32_t ComputeLevel();
	// int32_t ComputePositionInSet();
	// int32_t ComputeSizeOfSet();

	// Last values handed to a client through the Get*Core overrides; -1 until first read.
	private int m_lastReportedLevel = -1;
	private int m_lastReportedPositionInSet = -1;
	private int m_lastReportedSizeOfSet = -1;

	// The owning TableView, or null once the row has been recycled out of the tree.
	// winrt::TableView GetOwningTableView();
	// This row's index in the owner's ItemsSource index space, or -1 when unrealized.
	// int32_t GetRowIndex();
	// bool IsVirtualized();
	// void RealizeCore();
	// winrt::IInspectable GetTrackedItem() const;
	// int32_t GetTrackedItemIndex(winrt::TableView const& tableView);
	// int32_t GetItemOccurrenceAtIndex(winrt::TableView const& tableView, winrt::IInspectable const& item, int32_t targetIndex);
	// bool IsTrackedRow(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// void TrackRowItem(winrt::TableViewRow const& row, winrt::TableView const& tableView);
	// Joins the visible cells' display text in visual order.
	// static std::wstring ComposeCellTexts(
	//     TableViewRow* rowImpl,
	//     winrt::Panel const& cellsHost,
	//     bool allowPeerCreation);

	// Dead cell entries stay cached until the next prune during child enumeration or peer lookup.
	private readonly struct CellPeerCacheEntry
	{
		// TODO Uno: the C++ owner (ITrackerHandleManager) only feeds tracker_ref; a plain strong reference is enough here.
		public CellPeerCacheEntry(
			FrameworkElement cellElement,
			AutomationPeer cellPeer)
		{
			cell = new(cellElement);
			peer = cellPeer;
		}

		public readonly WeakReference<FrameworkElement>? cell;
		public readonly AutomationPeer? peer;
	}

	private List<CellPeerCacheEntry> m_cellPeerCache = new();
	private WeakReference<TableView>? m_lastOwningTable;
	private TableViewTrackedItemIdentity m_item = new();
	private int m_lastKnownRowIndex = -1;
	private int m_trackedItemOccurrence = -1;
	// winrt::TableViewRow GetRow() const;
	// bool IsHierarchicalRow() const;
	// Passed to the owner unresolved so it stays idempotent.
	// void SetExpansion(bool expand);
	// Sibling-set position/size. False when not a realized tree row (UIA gets 0, "unknown").
	// bool TryGetSiblingPosition(int32_t& positionInSet, int32_t& sizeOfSet);
}
