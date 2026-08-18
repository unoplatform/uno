#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A persistent GPU <see cref="SKSurface"/> the frame renders into so the previous frame's pixels survive across
/// presents — enabling damage-region partial repaint on backends whose swapchain surface does not preserve contents
/// (GL <c>SwapBuffers</c>, a Metal <c>CAMetalLayer</c>'s pooled drawable). <see cref="Present"/> blits the whole
/// layer onto the swapchain each frame, so only the damaged region is re-rendered while the swapchain shows it all.
/// </summary>
internal sealed class RetainedLayer : IDisposable
{
	private int _width;
	private int _height;
	private SKColorType _colorType;

	public SKSurface? Surface { get; private set; }

	/// <summary>Returns the persistent layer surface, (re)creating it on first use or a size/format change. The new
	/// surface is cleared once; thereafter the caller redraws only the damaged region into it.</summary>
	public SKSurface EnsureSurface(GRContext context, int width, int height, SKColorType colorType)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (Surface is null || _width != width || _height != height || _colorType != colorType)
		{
			Surface?.Dispose();
			var info = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);
			Surface = SKSurface.Create(context, budgeted: true, info)
				?? throw new InvalidOperationException("Failed to create the damage-region retained layer surface.");
			Surface.Canvas.Clear(SKColors.Transparent);
			_width = width;
			_height = height;
			_colorType = colorType;
		}

		return Surface;
	}

	/// <summary>Blits the whole retained layer onto <paramref name="swapchainSurface"/> (same GRContext, GPU-to-GPU).</summary>
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
