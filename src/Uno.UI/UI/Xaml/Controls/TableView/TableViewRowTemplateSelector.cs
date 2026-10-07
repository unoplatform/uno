// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Picks the row container template from TableViewRowKind. Set as PART_RowsRepeater.ItemTemplate,
// so ItemsRepeater supplies the per-template recycle pools. Projected only because
// ItemTemplateWrapper lives in another DLL; not app-facing, since selection needs an owning
// TableView that only TableView can set. unsealed + ctor is what MIDL requires for a member-less
// runtimeclass.
internal partial class TableViewRowTemplateSelector : DataTemplateSelector
{
}
