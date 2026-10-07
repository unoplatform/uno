// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Cell peer supplies per-cell grid/table structure and "{column header}, {cell value}" names.
/// <summary>
/// Exposes <see cref="TableView"/> cells to Microsoft UI Automation.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewCellAutomationPeer : FrameworkElementAutomationPeer, IGridItemProvider, ITableItemProvider, IValueProvider
{
}
