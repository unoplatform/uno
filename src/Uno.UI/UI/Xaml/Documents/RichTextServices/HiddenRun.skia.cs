// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference HiddenRun.h, HiddenRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// HiddenRun represents hidden content that is not visible and does not affect
/// line layout.
/// </summary>
internal sealed class HiddenRun : TextRun
{
	// Initializes a new instance of the HiddenRun class.
	public HiddenRun(uint characterIndex)
		: base(1, characterIndex, TextRunType.Hidden)
	{
	}
}
