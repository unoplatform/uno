// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DrawingContext.h, DrawingContext.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Base interface for drawing contexts. On Uno the WinUI D2D/HWRender pipeline
// (HWRenderContent / D2DEnsureResources / D2DRenderContent) is dropped: glyphs are
// emitted by the Skia ParsedText.Draw during the visual Paint walk rather than via
// recorded render commands. What remains portable is the content transform (a
// translation offset) and, later, the foreground-highlight-info distribution used for
// high-contrast selection foreground (Stage 8).
internal abstract class DrawingContext
{
	// Transform applied to the content of the drawing context. The C++ CMILMatrix
	// is only ever used here as a translation, so it is reduced to an offset.
	protected Point m_transform;

	// Gets/Sets transform applied to the content of the drawing context.
	public Point GetTransform() => m_transform;
	public void SetTransform(Point transform) => m_transform = transform;

	public abstract void InvalidateRenderCache();

	public abstract void CleanupRealizations();

	// TODO Uno (Stage 8): ClearForegroundHighlightInfo / AppendForegroundHighlightInfo /
	// SetBackPlateConfiguration / SetControlEnabled for high-contrast selection foreground.
}
