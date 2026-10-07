// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Automation peer for the group-header band. Separate from TableViewRowAutomationPeer because
// the header is now its own container rather than a row in "group mode": there is no adaptive
// branch, no GridItem coordinates for a band that spans every column, and ExpandCollapse is
// unconditional (a non-expandable group reports LeafNode rather than dropping the pattern).
/// <summary>
/// Exposes <see cref="TableViewGroupHeader"/> types to Microsoft UI Automation.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewGroupHeaderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IGridItemProvider
{
}
