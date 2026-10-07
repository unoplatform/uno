// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Item-owned edit transaction (the WinRT counterpart of IEditableObject) is deliberately NOT in
// this release. It is the rollback mechanism for ROW-scoped editing, and editing is cell-scoped
// here, so shipping it now would be public surface with nothing behind it. It lands with row
// editing, when it has a job to do.

/// <summary>
/// Base class for <see cref="TableView"/> columns.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
[ContentProperty(Name = nameof(Header))]
public partial class TableViewColumn : DependencyObject
{
	// The editing hooks a derived column would override - GenerateEditingElement, PrepareCellForEdit,
	// CommitCellEdit, CancelCellEdit - are internal in this release. The built-in columns use them,
	// but a CellEditingTemplate is the supported way to customise an editor here, so publishing the
	// hooks now would fix their exact shape forever. They can be added later without breaking anyone.
}
