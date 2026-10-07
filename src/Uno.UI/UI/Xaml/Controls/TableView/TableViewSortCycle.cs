// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Direction sequence a column's header walks on repeated clicks. The first pair member is the
// direction an unsorted column opens in: Ascending* suits names and categories, Descending* suits
// metrics where the interesting rows are the largest (CPU, memory, size). The *None variants add a
// trailing unsorted step, so a third click returns the column to source order instead of going
// straight back to the opening direction.
/// <summary>
/// Defines the direction sequence a column header walks on repeated clicks.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewSortCycle
{
	/// <summary>
	/// Ascending, then descending.
	/// </summary>
	AscendingDescending = 0,

	/// <summary>
	/// Ascending, then descending, then unsorted.
	/// </summary>
	AscendingDescendingNone = 1,

	/// <summary>
	/// Descending, then ascending.
	/// </summary>
	DescendingAscending = 2,

	/// <summary>
	/// Descending, then ascending, then unsorted.
	/// </summary>
	DescendingAscendingNone = 3,
}
