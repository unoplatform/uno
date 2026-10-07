// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\SortDirection.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Control-agnostic sort direction for the shaping primitives. Declared in layer 1 rather than
// beside any one consumer, because TableViewSource, TableViewColumn and TableViewSortingEventArgs
// all name it.
//
// The namespace is load-bearing, not cosmetic: this type must NOT be declared into
// Microsoft.UI.Xaml.Data, a namespace the Windows App SDK also owns. A managed consumer
// referencing both winmds binds its C# projection to the SDK's copy of that namespace, which
// makes a type declared there by this binary invisible:
//
//   error CS0234: The type or namespace name 'SortDirection' does not exist in the
//                 namespace 'Microsoft.UI.Xaml.Data'
//
// Declaring shaping types only in namespaces this binary exclusively owns removes the
// split-namespace collision at the root.
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
