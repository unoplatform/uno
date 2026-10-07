// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView.idl, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// A templated ContentControl rather than a code-built visual tree: the chrome and
// expander live in one control-owned ControlTemplate, and GroupHeaderTemplate fills
// only the content region, so it cannot drop the expander or the themed band.
// Replacing Style/Template outright still hands the app the whole visual -- the usual
// re-template contract, which is why the parts are documented here.
//
// The whole band is the toggle target (matching ListView / TreeView) rather than a
// nested Button, so there is one interactive element and one automation story.
//
// Parts (all optional; omitting one degrades rather than fails):
//   PART_ExpanderGutter  Border    reserves the chevron column so content stays aligned
//   PART_ExpanderIcon    FontIcon  glyph driven by ExpansionStates, not by code
//   content region       inherited from ContentControl
//
// States: CommonStates Normal|PointerOver|Pressed|Disabled
//         ExpansionStates Expanded|Collapsed
//         ExpandabilityStates Expandable|NotExpandable
/// <summary>
/// Represents the expandable header band of a group in a <see cref="TableView"/>.
/// </summary>
[global::Windows.Foundation.Metadata.Experimental]
public partial class TableViewGroupHeader : ContentControl
{
	// Content is the TableViewGroupInfo projection; ContentTemplate is the header content
	// template. The default Style seeds it, and TableView.GroupHeaderTemplate overrides it as a
	// local value that ClearValue reverts back to the Style setter.
}
