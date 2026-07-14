#nullable enable

using System;
using SkiaSharp;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Per-window configuration returned by <see cref="HeadlessHostBuilder.ConfigureWindow"/>.
/// Each headless window can have its own size, scale, orientation and (optionally) its own
/// render target buffer.
/// </summary>
public sealed class HeadlessWindowOptions
{
	/// <summary>
	/// Creates options for a window of the given raw pixel dimensions. When <see cref="Buffer"/> is
	/// set, these dimensions are also the expected size of that buffer.
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
	/// Pointer to a caller-owned buffer, sized for <see cref="Width"/>/<see cref="Height"/> and
	/// <see cref="RowBytes"/>. When set (together with <see cref="OnFrameRendered"/>), each frame is
	/// rendered zero-copy into it; otherwise the paint walk is skipped and nothing is rasterized.
	/// </summary>
	public IntPtr Buffer { get; init; }

	/// <summary>The number of bytes per pixel row (stride) of <see cref="Buffer"/>.</summary>
	public int RowBytes { get; init; }

	/// <summary>The pixel format of <see cref="Buffer"/>.</summary>
	public SKColorType ColorType { get; init; }

	/// <summary>Invoked on the render thread after each frame has been drawn into <see cref="Buffer"/>.</summary>
	public Action? OnFrameRendered { get; init; }

	internal bool HasBuffer => Buffer != IntPtr.Zero && OnFrameRendered is not null;

	/// <summary>
	/// Validates the buffer configuration up-front so misconfiguration surfaces as a clear error at
	/// window creation rather than as a caught exception on the render thread.
	/// </summary>
	internal void Validate()
	{
		if (Buffer != IntPtr.Zero)
		{
			if (RowBytes <= 0)
			{
				throw new InvalidOperationException($"{nameof(RowBytes)} must be strictly positive when a {nameof(Buffer)} is provided.");
			}
			if (OnFrameRendered is null)
			{
				throw new InvalidOperationException($"{nameof(OnFrameRendered)} must be set when a {nameof(Buffer)} is provided.");
			}
		}
	}
}
