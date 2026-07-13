#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Helpers;

/// <summary>The default <see cref="IRenderBackend"/>: the established Skia two-phase (SKPicture) lifecycle.</summary>
internal sealed class SkiaRenderBackend : IRenderBackend
{
	public (IRenderData frame, SKPath nativeElementClipPath, List<Visual> nativeVisualsInZOrder) Record(
		ContainerVisual rootVisual,
		float width,
		float height,
		bool invertNativeElementClipPath)
		=> SkiaRenderHelper.RecordFrameAndReturnPath(width, height, rootVisual, invertNativeElementClipPath);

	public void Present(IRenderData frame, SKCanvas canvas, Action<SKCanvas>? postPresent)
		=> SkiaRenderHelper.RenderFrame(canvas, frame, SKColors.Transparent, postPresent);
}
