#nullable enable

using System;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Per-window configuration returned by <see cref="HeadlessHostBuilder.ConfigureWindow"/>.
/// Each headless window can have its own size, scale, orientation and (optionally) a per-frame
/// callback that receives the rendered pixels.
/// </summary>
public sealed class HeadlessWindowOptions
{
	/// <summary>
	/// Creates options for a window of the given raw pixel dimensions. These are also the dimensions
	/// of the frames rendered via <see cref="HeadlessWindow.RenderIntoAsync"/>.
	/// </summary>
	/// <param name="width">Width, in raw pixels.</param>
	/// <param name="height">Height, in raw pixels.</param>
	public HeadlessWindowOptions(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), "The headless surface dimensions must be strictly positive.");
		}

		Width = width;
		Height = height;
	}

	/// <summary>Width of the surface, in raw pixels.</summary>
	public int Width { get; }

	/// <summary>Height of the surface, in raw pixels.</summary>
	public int Height { get; }

	/// <summary>
	/// The rasterization scale (a.k.a. <c>RawPixelsPerViewPixel</c>). Logical bounds are
	/// <c>size / scale</c>. Defaults to <c>1.0</c>.
	/// </summary>
	public float Scale { get; init; } = 1f;

	/// <summary>
	/// The orientation reported by <see cref="DisplayInformation.CurrentOrientation"/>. This is a
	/// reporting value only; it does not rotate the rendered output. Defaults to
	/// <see cref="DisplayOrientations.Landscape"/>.
	/// </summary>
	public DisplayOrientations Orientation { get; init; } = DisplayOrientations.Landscape;

	/// <summary>
	/// Invoked once when the window is created, handing over its <see cref="HeadlessWindow"/> handle.
	/// Use it to subscribe to <see cref="HeadlessWindow.NewFrameReady"/> and/or call
	/// <see cref="HeadlessWindow.RenderIntoAsync"/>. When left unset, the paint walk is skipped and
	/// nothing is rasterized (the render cycle still runs).
	/// </summary>
	public Action<HeadlessWindow>? OnWindowCreated { get; init; }

	internal bool RendersOnDemand => OnWindowCreated is not null;
}
