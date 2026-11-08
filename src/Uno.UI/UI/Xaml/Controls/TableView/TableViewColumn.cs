// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit 79209e041

#nullable enable

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls.Tabular;

/// <summary>
/// Base class for <see cref="TableView"/> columns.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
[ContentProperty(Name = nameof(Header))]
public partial class TableViewColumn : DependencyObject
{
}
