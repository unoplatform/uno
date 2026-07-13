#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Owns the frame lifecycle: records the visual tree into an opaque <see cref="IRenderData"/> frame and
/// presents a recorded frame. The default implementation runs the established Skia two-phase
/// (record on the UI thread, present on the render thread); a different backend structures this however
/// it wants. This is the seam that will grow to own invalidation and vsync (replacing CompositionTarget).
/// </summary>
/// <remarks>
/// The native-element clip path and the present canvas are still SkiaSharp types at this boundary — they
/// are tied to native-view occlusion and the host's swapchain surface, whose neutralization (and the
/// per-platform host contract) is the remaining milestone-2 work.
/// </remarks>
internal interface IRenderBackend
{
	/// <summary>Records <paramref name="rootVisual"/> into an opaque frame, returning the native-element clip path and z-order.</summary>
	(IRenderData frame, SKPath nativeElementClipPath, List<Visual> nativeVisualsInZOrder) Record(
		ContainerVisual rootVisual,
		float width,
		float height,
		bool invertNativeElementClipPath);

	/// <summary>Presents a previously recorded <paramref name="frame"/> onto <paramref name="canvas"/>.</summary>
	void Present(IRenderData frame, SKCanvas canvas, Action<SKCanvas>? postPresent);
}
