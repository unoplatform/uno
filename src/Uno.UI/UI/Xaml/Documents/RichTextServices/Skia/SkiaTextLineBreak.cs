// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The continuation token threaded between <see cref="TextFormatter.FormatLine"/>
/// calls. WinUI uses it to carry opaque line-break state; the Skia formatter lays
/// out the whole paragraph up front, so the token only needs to carry the index
/// of the next line to vend.
/// </summary>
internal sealed class SkiaTextLineBreak : TextLineBreak
{
	public SkiaTextLineBreak(int nextLineIndex)
	{
		NextLineIndex = nextLineIndex;
	}

	public int NextLineIndex { get; }
}
