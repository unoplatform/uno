// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

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
