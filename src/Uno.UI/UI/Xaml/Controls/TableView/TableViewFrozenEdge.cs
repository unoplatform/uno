// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Column pinning edge; None scrolls naturally, Leading pins left, Trailing is reserved.
/// <summary>
/// Defines the edge a <see cref="TableViewColumn"/> is pinned to during horizontal scrolling.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewFrozenEdge
{
	/// <summary>
	/// Column is not frozen.
	/// </summary>
	None = 0,

	/// <summary>
	/// Column is pinned to the leading edge.
	/// </summary>
	Leading = 1,

	/// <summary>
	/// Reserved for future trailing-edge pinning.
	/// </summary>
	Trailing = 2,
}
