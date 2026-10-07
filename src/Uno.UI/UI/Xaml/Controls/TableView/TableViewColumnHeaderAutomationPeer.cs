// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Header peer gives plain Grid header visuals distinct automation metadata.
/// <summary>
/// Exposes <see cref="TableView"/> column headers to Microsoft UI Automation.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewColumnHeaderAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
}
