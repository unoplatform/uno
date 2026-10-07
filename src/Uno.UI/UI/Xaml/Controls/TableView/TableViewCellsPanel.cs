// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Cell layout host used as PART_HeaderHost and each row's PART_CellsHost. Measures cells for Auto
// size-to-content and arranges them at each column's resolved ActualWidth (see TableViewCellsPanel.cpp
// and TableView_Layout.cpp).
/// <summary>
/// Cell layout host used for the header row and for each row's cells.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewCellsPanel : Panel
{
}
