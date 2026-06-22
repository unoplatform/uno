#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Helpers;

internal sealed class RetainedLayer : IDisposable
{
	private int _width;
	private int _height;

	public SKSurface? Surface { get; private set; }

	public SKSurface EnsureSurface(GRContext context, int width, int height, SKColor clearColor)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (Surface is null || _width != width || _height != height)
		{
			Surface?.Dispose();
			var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			Surface = SKSurface.Create(context, budgeted: true, info)
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
			layer.Draw(swapchainSurface.Canvas, 0, 0, null);
			swapchainSurface.Canvas.Flush();
		}
	}

	public void Dispose()
	{
		Surface?.Dispose();
		Surface = null;
	}
}
