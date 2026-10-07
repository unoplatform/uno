// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Orders two row items. Return <0, 0 or >0 like a classic three-way comparison. The comparer must
// be a pure function of its inputs: re-entering the control from it is an app bug.
// An interface rather than a delegate so a comparer can be declared as a XAML resource and assigned
// in markup.
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
