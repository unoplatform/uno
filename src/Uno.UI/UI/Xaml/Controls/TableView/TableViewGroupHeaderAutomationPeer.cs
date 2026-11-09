// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit 79209e041

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Exposes <see cref="TableViewGroupHeader"/> types to Microsoft UI Automation.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewGroupHeaderAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IGridItemProvider
{
}
