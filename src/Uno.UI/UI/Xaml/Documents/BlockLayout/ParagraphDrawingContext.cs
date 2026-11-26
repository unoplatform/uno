// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ParagraphDrawingContext.h, ParagraphDrawingContext.cpp, tag winui3/release/2.4.0, commit e8442d07a

#nullable enable

using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Generates rendering instructions for a ParagraphNode. On Uno the text drawing context
// records which line goes where, and the page painter draws the lines (see
// RecordedTextDrawingContext).
internal sealed class ParagraphDrawingContext : DrawingContext
{
	private readonly ParagraphNode m_pNode;
	private readonly RecordedTextDrawingContext m_pTextDrawingContext = new();

	public ParagraphDrawingContext(ParagraphNode pNode)
	{
		m_pNode = pNode;
	}

	// WinUI ctor took (CCoreServices, ParagraphNode); the core is unused on Uno.
	public ParagraphDrawingContext(object? pCore, ParagraphNode pNode) : this(pNode)
	{
	}

	// Gets the TextDrawingContext which is capable of rendering text lines.
	public RecordedTextDrawingContext GetTextDrawingContext() => m_pTextDrawingContext;

	public override void InvalidateRenderCache()
	{
	}

	public override void CleanupRealizations()
	{
	}
}
