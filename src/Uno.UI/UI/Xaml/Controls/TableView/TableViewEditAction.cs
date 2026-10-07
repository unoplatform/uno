// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// How an edit closed.
/// <summary>
/// Defines how a cell edit is being closed.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewEditAction
{
	/// <summary>
	/// The edited value is being written to the source.
	/// </summary>
	Commit = 0,

	/// <summary>
	/// The edit is being cancelled and the editor is discarded.
	/// </summary>
	Cancel = 1,
}
