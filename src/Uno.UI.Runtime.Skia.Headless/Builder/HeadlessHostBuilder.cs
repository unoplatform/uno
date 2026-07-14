#nullable enable

using System;
using SkiaSharp;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Headless;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

public class HeadlessHostBuilder : IPlatformHostBuilder
{
	private Func<HeadlessWindowContext, HeadlessWindowOptions>? _configurator;

	internal HeadlessHostBuilder()
	{
	}

	bool IPlatformHostBuilder.IsSupported => true;

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new HeadlessHost(appBuilder, this);

	/// <summary>
	/// Sets the default raw pixel dimensions applied to every window that isn't given specific
	/// <see cref="HeadlessWindowOptions"/> by <see cref="ConfigureWindow"/>. When a render buffer is
	/// supplied via <see cref="RenderTo"/>, these dimensions are also the expected size of that buffer.
	/// </summary>
	/// <param name="width">Width, in raw pixels.</param>
	/// <param name="height">Height, in raw pixels.</param>
	public HeadlessHostBuilder WithSize(int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(width), "The headless surface dimensions must be strictly positive.");
		}

		Width = width;
		Height = height;
		return this;
	}

	/// <summary>
	/// Sets the rasterization scale (a.k.a. <c>RawPixelsPerViewPixel</c>) applied to the logical
	/// layout bounds. Logical bounds are <c>size / scale</c>. Mutually exclusive with
	/// <see cref="WithDpi"/> — the last one called wins. Defaults to <c>1.0</c>.
	/// </summary>
	/// <param name="scale">The rasterization scale (e.g. <c>1.0</c>, <c>1.5</c>, <c>2.0</c>).</param>
	public HeadlessHostBuilder WithScale(float scale)
	{
		if (scale <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(scale), "The headless rasterization scale must be strictly positive.");
		}

		Scale = scale;
		return this;
	}

	/// <summary>
	/// Sets the display scale from a logical DPI value (<c>scale = dpi / 96</c>). Convenience over
	/// <see cref="WithScale"/> for callers that think in DPI. Mutually exclusive with
	/// <see cref="WithScale"/> — the last one called wins.
	/// </summary>
	/// <param name="logicalDpi">The logical DPI (e.g. <c>96</c>, <c>144</c>, <c>192</c>).</param>
	public HeadlessHostBuilder WithDpi(float logicalDpi)
	{
		if (logicalDpi <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(logicalDpi), "The headless DPI must be strictly positive.");
		}

		Scale = logicalDpi / DisplayInformation.BaseDpi;
		return this;
	}

	/// <summary>
	/// Sets the orientation reported by <see cref="DisplayInformation.CurrentOrientation"/>.
	/// This is a reporting value only; it does not rotate the rendered output. Defaults to
	/// <see cref="DisplayOrientations.Landscape"/>.
	/// </summary>
	public HeadlessHostBuilder WithOrientation(DisplayOrientations orientation)
	{
		Orientation = orientation;
		return this;
	}

	/// <summary>
	/// Sets the default render target used by every window that isn't given specific
	/// <see cref="HeadlessWindowOptions"/> by <see cref="ConfigureWindow"/>: each frame is rendered
	/// directly (zero-copy) into the supplied native buffer, invoking <paramref name="onFrameRendered"/>
	/// once the buffer has been filled. When not called, the render cycle still runs but nothing is
	/// rasterized. A single buffer cannot be shared by multiple windows — use
	/// <see cref="ConfigureWindow"/> to give each window its own buffer.
	/// </summary>
	/// <param name="buffer">Pointer to a caller-owned buffer, sized for the configured dimensions and <paramref name="rowBytes"/>.</param>
	/// <param name="rowBytes">The number of bytes per pixel row (stride) of <paramref name="buffer"/>.</param>
	/// <param name="colorType">The pixel format of <paramref name="buffer"/>.</param>
	/// <param name="onFrameRendered">Invoked on the render thread after each frame has been drawn into the buffer.</param>
	public HeadlessHostBuilder RenderTo(IntPtr buffer, int rowBytes, SKColorType colorType, Action onFrameRendered)
	{
		if (buffer == IntPtr.Zero)
		{
			throw new ArgumentException("The render buffer pointer must not be null.", nameof(buffer));
		}
		if (rowBytes <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(rowBytes), "The render buffer stride must be strictly positive.");
		}

		RenderBuffer = buffer;
		RenderRowBytes = rowBytes;
		RenderColorType = colorType;
		OnFrameRendered = onFrameRendered ?? throw new ArgumentNullException(nameof(onFrameRendered));
		return this;
	}

	/// <summary>
	/// Supplies per-window configuration. The <paramref name="configurator"/> is invoked for each
	/// window as it is created (see <see cref="HeadlessWindowContext.Index"/> to distinguish them) and
	/// returns that window's <see cref="HeadlessWindowOptions"/>, including an optional dedicated
	/// buffer. When set, it takes precedence over the builder-level defaults for every window.
	/// </summary>
	public HeadlessHostBuilder ConfigureWindow(Func<HeadlessWindowContext, HeadlessWindowOptions> configurator)
	{
		_configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
		return this;
	}

	/// <summary>
	/// Resolves the options for a window being created, preferring the per-window
	/// <see cref="ConfigureWindow"/> configurator and falling back to the builder-level defaults.
	/// </summary>
	internal HeadlessWindowOptions ResolveWindowOptions(int index, Window window)
	{
		if (_configurator is { } configurator)
		{
			return configurator(new HeadlessWindowContext(index, window))
				?? throw new InvalidOperationException($"The {nameof(ConfigureWindow)} configurator returned null for window #{index}.");
		}

		return new HeadlessWindowOptions(Width, Height)
		{
			Scale = Scale,
			Orientation = Orientation,
			Buffer = RenderBuffer,
			RowBytes = RenderRowBytes,
			ColorType = RenderColorType,
			OnFrameRendered = OnFrameRendered,
		};
	}

	/// <summary>
	/// True when no window can possibly have a render buffer, so the global paint walk can be skipped
	/// entirely. Only known when there is no per-window configurator and no default buffer.
	/// </summary>
	internal bool KnownBufferless => _configurator is null && !(RenderBuffer != IntPtr.Zero && OnFrameRendered is not null);

	internal int Width { get; private set; } = NativeWindowWrapperBase.InitialWidth;

	internal int Height { get; private set; } = NativeWindowWrapperBase.InitialHeight;

	internal float Scale { get; private set; } = 1f;

	internal DisplayOrientations Orientation { get; private set; } = DisplayOrientations.Landscape;

	internal IntPtr RenderBuffer { get; private set; }

	internal int RenderRowBytes { get; private set; }

	internal SKColorType RenderColorType { get; private set; }

	internal Action? OnFrameRendered { get; private set; }
}
