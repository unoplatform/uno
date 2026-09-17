#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Helpers;

internal sealed class RetainedLayer : IDisposable
{
	// The swapchain surface handed to Present is recycled and still holds an older frame. The layer
	// can carry per-pixel alpha (a system backdrop makes the tree transparent), so its pixels have to
	// replace the stale ones rather than blend over them.
	private static readonly SKPaint _presentPaint = new() { BlendMode = SKBlendMode.Src };

	private int _width;
	private int _height;

	public SKSurface? Surface { get; private set; }

	/// <summary>
	/// Returns the layer surface, (re)creating it when the size changes. A null <paramref name="context"/>
	/// creates a raster surface.
	/// </summary>
	public SKSurface EnsureSurface(GRContext? context, int width, int height, SKColor clearColor)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (Surface is null || _width != width || _height != height)
		{
			Surface?.Dispose();
			var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			Surface = (context is null ? SKSurface.Create(info) : SKSurface.Create(context, budgeted: true, info))
				?? throw new InvalidOperationException("Failed to create the damage-region retained layer surface.");
			Surface.Canvas.Clear(clearColor);
			_width = width;
			_height = height;
		}

		return Surface;
	}

	public void Present(SKSurface swapchainSurface)
	{
		if (Surface is { } layer)
		{
			layer.Draw(swapchainSurface.Canvas, 0, 0, _presentPaint);
			swapchainSurface.Canvas.Flush();
		}
	}

	public void Dispose()
	{
		Surface?.Dispose();
		Surface = null;
	}
}
