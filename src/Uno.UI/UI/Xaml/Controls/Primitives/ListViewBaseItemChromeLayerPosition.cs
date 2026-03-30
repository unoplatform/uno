// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemChrome.h, tag winui3/release/1.4.2

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// CListViewBaseItemChrome knows how to render all layers of the chrome, including
/// the layers present in the Grid/ListViewItem and in the secondary chrome.
/// So we can let the chrome know which layer to render, we have this enum
/// (in order of lowest layer to highest layer).
/// </summary>
internal enum ListViewBaseItemChromeLayerPosition
{
	Base_Pre,
	PrimaryChrome_Pre,
	SecondaryChrome_Pre,
	SecondaryChrome_Post,
	PrimaryChrome_Post,
	Base_Post,
}
