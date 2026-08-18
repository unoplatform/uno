#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A backend-specific, GPU-resident form of an image (a wgpu texture, an <c>SKImage</c>, a GL texture), created
/// once from a neutral <see cref="IImage"/>'s pixels by the <see cref="IDrawingFactory"/>. Opaque and cast back by
/// the producing backend when drawn. Its lifetime is framework-owned and released deterministically via
/// <see cref="IDisposable.Dispose"/> — no GC-driven texture caching.
/// </summary>
public interface ITexture : IDisposable
{
	int PixelWidth { get; }

	int PixelHeight { get; }
}
