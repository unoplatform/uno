// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Exposes <see cref="TableViewRow"/> types to Microsoft UI Automation.
/// </summary>
// TODO Uno: IVirtualizedItemProvider comes from the C++ ReferenceTracker base in TableViewRowAutomationPeer.h,
// not from the IDL; Realize is implemented explicitly so it adds no public member.
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewRowAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IVirtualizedItemProvider
{
}
