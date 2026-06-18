// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParagraphDrawingContext.h, ParagraphDrawingContext.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Generates rendering instructions for a ParagraphNode. On Uno the WinUI
// D2DTextDrawingContext recorder is dropped: the paragraph's glyphs are drawn by
// the Skia ParsedText.Draw during the visual Paint walk. This is a thin transform
// holder (see DrawingContext) for now.
internal sealed class ParagraphDrawingContext : DrawingContext
{
	private readonly ParagraphNode m_pNode;

	public ParagraphDrawingContext(ParagraphNode pNode)
	{
		m_pNode = pNode;
	}

	// WinUI ctor took (CCoreServices, ParagraphNode); the core is unused on Uno.
	public ParagraphDrawingContext(object? pCore, ParagraphNode pNode) : this(pNode)
	{
	}

	// Gets the TextDrawingContext which is capable of rendering text lines.
	// TODO Uno (Stage 6/8): the WinUI D2DTextDrawingContext line recorder is not ported;
	// rendering goes through ParsedText.Draw. Returns null until per-line recording is needed.
	public TextDrawingContext? GetTextDrawingContext() => null;

	public override void InvalidateRenderCache()
	{
	}

	public override void CleanupRealizations()
	{
	}
}
