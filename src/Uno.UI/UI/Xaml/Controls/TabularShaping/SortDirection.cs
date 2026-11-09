// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\SortDirection.idl, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Do not move to Microsoft.UI.Xaml.Data: C# consumers then fail with CS0234. See docs/design-notes/TabularControls/TableView-dev-spec.md.
/// <summary>
/// Defines the direction of a sort.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum SortDirection
{
	/// <summary>
	/// Not sorted.
	/// </summary>
	None = 0,

	/// <summary>
	/// Sorted in ascending order.
	/// </summary>
	Ascending = 1,

	/// <summary>
	/// Sorted in descending order.
	/// </summary>
	Descending = 2,
}
