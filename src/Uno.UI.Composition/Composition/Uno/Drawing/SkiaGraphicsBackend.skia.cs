#nullable enable

using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A <see cref="IGraphicsContext"/> for the Skia backend. Owns a software <see cref="SKSurface"/> as the
/// offscreen color target (a GPU/windowed provider would instead wrap the host's GRContext + swapchain and
/// own the dirty-rect blit). <see cref="Snapshot"/> exposes the rendered pixels as a neutral <see cref="IImage"/>.
/// </summary>
public sealed class SkiaGraphicsContext : IGraphicsContext
{
	private SKSurface? _surface;

	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public bool IsLost => false;

	public IRenderTarget CreateRenderTarget(int width, int height)
	{
		_surface?.Dispose();
		_surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
		return new SkiaRenderTarget(_surface.Canvas);
	}

	/// <summary>Snapshots the current render-target contents as a neutral image (the readback RTB / validation needs).</summary>
	public IImage Snapshot()
		=> _surface is { } s ? new SkiaImage(s.Snapshot()) : throw new InvalidOperationException("No render target created.");

	public void Dispose() => _surface?.Dispose();
}

/// <summary>The provider for the software Skia context.</summary>
public sealed class SkiaContextProvider : IGraphicsContextProvider
{
	public GraphicsContextKind Kind => GraphicsContextKind.Software;

	public IGraphicsContext? TryCreate(INativeWindow window, in GraphicsRequirements requirements)
		=> new SkiaGraphicsContext();
}

/// <summary>The registerable Skia backend pair (the built-in default choice; registered like any other backend).</summary>
public sealed class SkiaGraphicsBackend : IGraphicsBackend
{
	private static readonly GraphicsContextKind[] _preferred = { GraphicsContextKind.Software };

	public IReadOnlyList<GraphicsContextKind> PreferredContexts => _preferred;

	public GraphicsRequirements Requirements => new() { MinStencilBits = 8, PreferredColor = GraphicsColorFormat.Bgra8888 };

	public IDrawingBackend Drawing { get; } = new SkiaDrawingBackend();

	public IRenderBackend CreateRenderBackend(IGraphicsContext context) => new SkiaRenderBackend();
}
