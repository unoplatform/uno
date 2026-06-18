// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Text.Core;

// Uno seam: WinUI's CPlainTextPosition::GetTextView() down-casts the owner UIElement to
// CRichTextBlock / CTextBlock and calls their GetTextView(). The Uno controls don't yet
// expose a typed GetTextView() (wired in Stage 6/9); this interface lets the text-position
// layer obtain the ITextView from whatever owner element implements it, without taking a
// hard dependency on the concrete control types.
internal interface ITextViewHost
{
	ITextView? GetTextView();
}
