// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

//
// Template contract (MIDL3 does not surface [TemplatePart], so this block is it).
// All parts are REQUIRED: the Selected* states target them by name, and an unresolved Storyboard
// target fails when the state is applied, not when the template is parsed.
//
//   PART_RootBorder              — Border. Row chrome; Background driven by CommonStates.
//   PART_CellsHost               — Panel. One cell wrapper per column, in Columns order.
//   PART_CellForegroundPresenter — ContentPresenter hosting PART_CellsHost. Exists because a
//                                  Border has no Foreground for the Selected* states to animate.
//   PART_SelectionIndicator      — Shape (default: Rectangle). Selected* states animate Opacity
//                                  0 -> 1; SelectedDisabled also animates Fill. Custom templates
//                                  using other indicator types must adapt the state targets.
//
//   CommonStates: Normal, PointerOver, Pressed, Disabled,
//                 Selected, SelectedPointerOver, SelectedPressed, SelectedDisabled
//
// Selected states share this one group so nothing depends on GoToState call order.
// Multiple/Extended selection will add a separate MultiSelectStates group (and a checkbox part)
// rather than extending this one, so it stays additive for existing templates.
//
/// <summary>
/// Represents a generated row in a <see cref="TableView"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewRow : Control
{
}
