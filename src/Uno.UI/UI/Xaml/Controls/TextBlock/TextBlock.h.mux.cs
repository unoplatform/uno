// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextBlock.h, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	[Flags]
	private enum FastPathOptOutConditions : uint
	{
		CharacterSpacing_Not_Default = 0x01,
		TextTrimming_Is_Clip = 0x02,
		HasComplexContent = 0x04,
	}

	private enum TextMode : uint
	{
		Normal = 0,
		DWriteLayout = 1,
	}

	// TODO Uno: Both modes lay out with UnicodeText. The mode only picks the IsTextTrimmed rule.
	private TextMode _textMode = TextMode.DWriteLayout;

	private FastPathOptOutConditions _fastPathOptOutConditions;
}
