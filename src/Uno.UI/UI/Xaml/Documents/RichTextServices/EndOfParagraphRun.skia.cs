// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EndOfParagraphRun.h, EndOfParagraphRun.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// EndOfParagraphRun represents paragraph terminating content.
/// </summary>
internal sealed class EndOfParagraphRun : TextRun
{
	// Initializes a new instance of the EndOfParagraphRun class, allowing
	// length to be specified.
	//
	// TODO: EOP run should only allow length 1. Some SL Controls
	// (TextBlock and TextBox) specify synthetic EOP character not in the
	// backing store. If EOP run with length 1 is used, the backing store end
	// will be passed, which causes failures higher up the stack. The controls
	// should be written to not introduce synthetic EOP.
	public EndOfParagraphRun(uint length, uint characterIndex)
		: base(length, characterIndex, TextRunType.EndOfParagraph)
	{
		MUX_ASSERT(length <= 1);
	}
}
