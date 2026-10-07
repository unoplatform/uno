// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Density preset for row height and built-in cell/header padding resources.
/// <summary>
/// Defines the row and cell spacing preset of a <see cref="TableView"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewDensity
{
	/// <summary>
	/// Compact spacing.
	/// </summary>
	Compact = 0,

	/// <summary>
	/// Standard spacing.
	/// </summary>
	Standard = 1,

	/// <summary>
	/// Comfortable spacing.
	/// </summary>
	Comfortable = 2,
}
