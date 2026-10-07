// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Only None and Single this release. Multiple/Extended need an anchor and a range model; members
// are appended, so these values stay stable when they arrive.
/// <summary>
/// Defines the row selection behavior of a <see cref="TableView"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewSelectionMode
{
	/// <summary>
	/// Selection is off; a display-only table.
	/// </summary>
	None = 0,

	/// <summary>
	/// At most one row is selected.
	/// </summary>
	Single = 1,
}
