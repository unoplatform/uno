// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit 79209e041

#nullable enable

using System.ComponentModel;

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Read-only group-header projection: the binding source of a <see cref="TableView.GroupHeaderTemplate"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public sealed partial class TableViewGroupInfo : INotifyPropertyChanged
{
	// Object Key { get; };
	// Int32 ItemCount { get; };
	// Int32 Level { get; };
	// Boolean IsExpandable { get; };
	// Boolean IsExpanded { get; };

	// String KeyText { get; };
	// String ItemCountText { get; };
}
