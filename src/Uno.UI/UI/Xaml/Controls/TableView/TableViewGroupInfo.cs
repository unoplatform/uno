// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System.ComponentModel;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Read-only group-header projection: the binding source of a GroupHeaderTemplate.
// Mirrors ICollectionViewGroup (Key <-> Group, ItemCount <-> GroupItems.Count).
// Updated in place and raises PropertyChanged, so recycling does not re-evaluate
// every binding in the header template.
/// <summary>
/// Read-only group-header projection: the binding source of a <see cref="TableView.GroupHeaderTemplate"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewGroupInfo : INotifyPropertyChanged
{
}
