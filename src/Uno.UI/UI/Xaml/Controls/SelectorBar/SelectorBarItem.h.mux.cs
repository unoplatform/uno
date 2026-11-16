// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\SelectorBar\SelectorBarItem.h

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class SelectorBarItem
{
	private ContentPresenter? _iconVisual;
	private TextBlock? _textVisual;

	private const string s_iconVisualPartName = "PART_IconVisual";
	private const string s_textVisualPartName = "PART_TextVisual";
}
