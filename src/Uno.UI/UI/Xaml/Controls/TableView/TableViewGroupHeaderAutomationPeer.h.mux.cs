// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewGroupHeaderAutomationPeer.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Automation peer for the group-header band.
//
// The header used to be a TableViewRow flipped into group mode, so its peer was
// TableViewRowAutomationPeer answering as either a row or a group depending on state. Now that
// the header is its own container, the peer is its own type: no adaptive branch, no GridItem
// coordinates for a band that spans every column, and the ExpandCollapse pattern is
// unconditional rather than state-gated.
partial class TableViewGroupHeaderAutomationPeer
{
	// explicit TableViewGroupHeaderAutomationPeer(winrt::TableViewGroupHeader const& owner);

	// IAutomationPeerOverrides
	// winrt::IInspectable GetPatternCore(winrt::PatternInterface const& patternInterface);
	// winrt::AutomationControlType GetAutomationControlTypeCore();
	// winrt::hstring GetClassNameCore();
	// winrt::hstring GetNameCore();

	// IExpandCollapseProvider
	// void Expand();
	// void Collapse();
	// winrt::ExpandCollapseState ExpandCollapseState();

	// Internal: announce a state change from THIS peer. UIA delivers property-changed events
	// through the peer the client is connected to, so the raise has to happen on the peer
	// instance itself -- raising from a peer obtained via CreatePeerForElement can hand back a
	// fresh instance that no client is listening to, and the event goes nowhere. Same shape as
	// NavigationViewItemAutomationPeer::RaiseExpandCollapseAutomationEvent.
	// void RaiseExpandCollapseAutomationEvent(winrt::ExpandCollapseState oldState, winrt::ExpandCollapseState newState);

	// IGridItemProvider — the band is a merged cell spanning every visible column, which is how
	// TableViewAutomationPeer::GetItem reports it. Without this the span would not be queryable
	// from the element the client actually reached.
	// int32_t Row();
	// int32_t Column();
	// int32_t RowSpan();
	// int32_t ColumnSpan();
	// winrt::IRawElementProviderSimple ContainingGrid();

	// private:
	// winrt::TableViewGroupHeader GetHeader() const;
	// winrt::TableView GetOwningTableView() const;
	// void SetExpansion(bool expand);
}
