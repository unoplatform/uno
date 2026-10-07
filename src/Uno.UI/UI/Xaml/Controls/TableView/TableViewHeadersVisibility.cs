// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Column-header visibility (None/Column). Mirrors the Column slot of WPF
// DataGridHeadersVisibility; row headers are out of scope, so Row/All are not defined.
/// <summary>
/// Defines which headers a <see cref="TableView"/> displays.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
[Flags]
public enum TableViewHeadersVisibility : uint
{
	/// <summary>
	/// No headers.
	/// </summary>
	None = 0,

	/// <summary>
	/// Column-header strip.
	/// </summary>
	Column = 1,
}
