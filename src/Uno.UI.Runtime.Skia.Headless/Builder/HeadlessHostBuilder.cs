#nullable enable

using System;
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
	/// Sets the default window-created callback used by every window that isn't given specific
	/// <see cref="HeadlessWindowOptions"/> by <see cref="ConfigureWindow"/>. It hands over the window's
	/// <see cref="HeadlessWindow"/> handle once; use it to subscribe to
	/// <see cref="HeadlessWindow.NewFrameReady"/> and/or call <see cref="HeadlessWindow.RenderIntoAsync"/>.
	/// When not called, the render cycle still runs but nothing is rasterized.
	/// </summary>
	public HeadlessHostBuilder OnWindowCreated(Action<HeadlessWindow> handler)
	{
		WindowCreated = handler ?? throw new ArgumentNullException(nameof(handler));
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
			OnWindowCreated = WindowCreated,
		};
	}

	/// <summary>
	/// True when no window can render (no handle is ever handed out), so the global paint walk can be
	/// skipped entirely. Only known when there is no per-window configurator and no default callback.
	/// </summary>
	internal bool NoWindowCallbacks => _configurator is null && WindowCreated is null;

	internal int Width { get; private set; } = NativeWindowWrapperBase.InitialWidth;

	internal int Height { get; private set; } = NativeWindowWrapperBase.InitialHeight;

	internal float Scale { get; private set; } = 1f;

	internal DisplayOrientations Orientation { get; private set; } = DisplayOrientations.Landscape;

	internal Action<HeadlessWindow>? WindowCreated { get; private set; }
}
