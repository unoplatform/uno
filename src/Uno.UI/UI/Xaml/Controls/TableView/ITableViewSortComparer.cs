// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Orders two row items for a column's custom sort.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public interface ITableViewSortComparer
{
	/// <summary>
	/// Compares two row items.
	/// </summary>
	/// <returns>Less than zero, zero, or greater than zero, like a classic three-way comparison.</returns>
	int Compare(object? left, object? right);
}
