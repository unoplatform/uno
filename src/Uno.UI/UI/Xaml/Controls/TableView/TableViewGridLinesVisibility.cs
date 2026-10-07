// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Mirrors WPF DataGridGridLinesVisibility names and values for persisted casts.
/// <summary>
/// Defines which gridlines a <see cref="TableView"/> displays.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public enum TableViewGridLinesVisibility
{
	/// <summary>
	/// Show horizontal and vertical gridlines.
	/// </summary>
	All = 0,

	/// <summary>
	/// Show horizontal gridlines.
	/// </summary>
	Horizontal = 1,

	/// <summary>
	/// Show no gridlines.
	/// </summary>
	None = 2,

	/// <summary>
	/// Show vertical gridlines.
	/// </summary>
	Vertical = 3,
}
