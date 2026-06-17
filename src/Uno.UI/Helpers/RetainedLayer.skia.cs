#nullable enable

using System;
using SkiaSharp;

namespace Uno.UI.Helpers;

/// <summary>
/// A persistent offscreen GPU surface used by dirty-rectangles rendering on swapchain renderers (GL, Metal,
/// Vulkan, WebGL). Their window/drawable surface is not preserved between presents (the swapchain rotates
/// buffers), so the previous frame's pixels can't be retained there to clip the next present against.
/// Instead the composition renders onto this retained layer — which <em>does</em> preserve the previous
/// frame — and the whole layer is blitted to the (non-retaining) swapchain surface each frame. This avoids
/// per-driver buffer-age handling and keeps the dirty-rectangles logic identical across renderers.
/// </summary>
internal sealed class RetainedLayer : IDisposable
{
	private int _width;
	private int _height;

	/// <summary>The retained surface, or null until <see cref="EnsureSurface"/> is first called.</summary>
	public SKSurface? Surface { get; private set; }

	/// <summary>
	/// Returns the retained layer surface for the given GPU context and size, (re)creating and clearing it
	/// when the size changes. Render the frame onto the returned surface's canvas.
	/// </summary>
	public SKSurface EnsureSurface(GRContext context, int width, int height, SKColor clearColor)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);

		if (Surface is null || _width != width || _height != height)
		{
			Surface?.Dispose();
			var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
			Surface = SKSurface.Create(context, budgeted: true, info)
				?? throw new InvalidOperationException("Failed to create the dirty-rectangles retained layer surface.");
			Surface.Canvas.Clear(clearColor);
			_width = width;
			_height = height;
		}

		return Surface;
	}

	/// <summary>Blits the whole retained layer onto the (non-retaining) swapchain surface, then flushes it.</summary>
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
