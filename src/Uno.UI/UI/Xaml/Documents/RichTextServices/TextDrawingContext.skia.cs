// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TextDrawingContext.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Describes text visual content using draw commands. It is actually storing a
/// set of rendering instructions that will later be used by the graphics system.
/// </summary>
internal abstract class TextDrawingContext
{
	// Generates rendering data for run of glyphs.
	//
	// The WinUI signature took an FssGlyphRun produced by the DWrite shaping
	// engine. On Uno the glyphs are already shaped by the Skia engine, so the
	// run is passed as the shaped GlyphInfo list plus the resolved FontDetails.
	public abstract void DrawGlyphRun(
		Point position,
		double runWidth,
		IReadOnlyList<GlyphInfo> glyphs,
		FontDetails font,
		WeakReference<DependencyObject>? brushSource,
		Rect? clipRect);

	// TODO Uno: The DWrite-typed DrawGlyphs overload (cluster map, shaping
	// properties, IFssFontFace) is not ported; the Skia engine shapes glyphs
	// before they reach the drawing context.

	// Generates rendering data for a line. (underline / strikethrough)
	public abstract void DrawLine(
		Point position,
		double width,
		double thickness,
		byte bidirectionalLevel,
		WeakReference<DependencyObject>? brushSource,
		Rect? clipRect);

	// Clears all rendering data.
	public abstract void Clear();

	// Determines whether rendering data has been already populated.
	public abstract bool HasRenderingData { get; }

	// Sets line parameters.
	public abstract void SetLineInfo(
		double viewportWidth,
		bool invertHorizontalAxis,
		double yOffset,
		double verticalAdvance);

	// Sets whether color fonts should be enabled for text drawing operations.
	public abstract void SetIsColorFontEnabled(bool isColorFontEnabled);

	// Sets the palette index to use for color glyphs if color fonts are enabled.
	public abstract void SetColorFontPaletteIndex(uint colorFontPaletteIndex);
}
