// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarItem.h, tag winui3/release/1.5.2, commit b91b3ce6f25c587a9e18c4e122f348f51331f18b

#nullable enable

namespace Windows.UI.Xaml.Controls;

partial class SelectorBarItem
{
	private ContentPresenter? _iconVisual;
	private TextBlock? _textVisual;

	private const string s_iconVisualPartName = "PART_IconVisual";
	private const string s_textVisualPartName = "PART_TextVisual";
}
