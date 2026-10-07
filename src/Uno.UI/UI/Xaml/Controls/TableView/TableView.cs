// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/main, commit dc28206ea35

#nullable enable

using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls.Tabular;

//
// Template contract: re-templates must provide these named parts.
// Missing required parts disables the related feature.
//
//   PART_HeaderRow      — FrameworkElement. Collapsed when headers are hidden.
//   PART_HeaderHost     — Panel. Receives one header cell per column.
//   PART_RowsRepeater   — ItemsRepeater. Hosts row containers; do not TemplateBind ItemsSource.
//   PART_EmptyStatePresenter — ContentControl/ContentPresenter (optional). Hosts EmptyTemplate.
//
// Unsealed: ReferenceTracker requires the cppwinrt-projected derivable type surface.
//
/// <summary>
/// Represents a preview tabular control. It is display-only by default; cell editing is opt-in via <see cref="IsReadOnly"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
[ContentProperty(Name = nameof(Columns))]
public partial class TableView : Control
{
}
