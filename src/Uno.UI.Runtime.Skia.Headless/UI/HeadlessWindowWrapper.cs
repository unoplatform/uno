#nullable enable

using System;
using System.Globalization;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;
using Uno.UI.Runtime.Skia.Headless;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.Headless.UI;

/// <summary>
/// A single headless (offscreen) window. Each window owns its own renderer, acts as its own
/// <see cref="IXamlRootHost"/> and <see cref="IDisplayInformationExtension"/>, and is registered in
/// the <see cref="XamlRootMap"/> so the framework drives its render cycle independently.
/// </summary>
internal sealed class HeadlessWindowWrapper : NativeWindowWrapperBase, IXamlRootHost, IDisplayInformationExtension
{
	private readonly int _rawWidth;
	private readonly int _rawHeight;
	private readonly float _scale;
	private readonly DisplayOrientations _orientation;
	private readonly HeadlessRenderer _renderer;
	private readonly HeadlessWindow _headlessWindow;
	private readonly bool _rendersOnDemand;
	private bool _closed;
	private bool _rendererDisposed;

	public HeadlessWindowWrapper(Window window, XamlRoot xamlRoot, HeadlessWindowOptions options)
		: base(window, xamlRoot)
	{
		_rawWidth = options.Width;
		_rawHeight = options.Height;
		_scale = ResolveScale(options.Scale);
		_orientation = options.Orientation;
		_rendersOnDemand = options.RendersOnDemand;

		XamlRootMap.Register(xamlRoot, this);

		// Create the renderer before publishing bounds: setting bounds can synchronously route an
		// InvalidateRender back through this wrapper (as IXamlRootHost), which needs a live renderer.
		_renderer = new HeadlessRenderer(this, _rawWidth, _rawHeight);
		_headlessWindow = new HeadlessWindow(_renderer, _rawWidth, _rawHeight);

		// The XamlRoot is already associated (base ctor), so bounds can be published synchronously.
		ApplySize();

		// Hand the caller its window handle once; it may subscribe to NewFrameReady and/or render.
		options.OnWindowCreated?.Invoke(_headlessWindow);
	}

	public override object? NativeWindow => null;

	internal int RawWidth => _rawWidth;

	internal int RawHeight => _rawHeight;

	internal float Scale => _scale;

	private void ApplySize()
	{
		RasterizationScale = _scale;
		var bounds = new Rect(0, 0, _rawWidth / _scale, _rawHeight / _scale);
		SetBoundsAndVisibleBounds(bounds, bounds);
		var rawSize = new SizeInt32(_rawWidth, _rawHeight);
		SetSizes(rawSize, rawSize);
	}

	protected override void ShowCore() => RequestFrame();

	// Always tick the render cycle so the lifecycle/animations/RenderTargetBitmap keep advancing, and —
	// when a handle was handed out — additionally signal the app so it can pull a frame on its own
	// schedule (layered on top of the keep-alive cycle, not replacing it).
	private void RequestFrame()
	{
		_renderer.Invalidate();

		if (_rendersOnDemand)
		{
			_headlessWindow.RaiseNewFrameReady();
		}
	}

	protected override void CloseCore()
	{
		if (_closed)
		{
			return;
		}
		_closed = true;

		DisposeRenderer();

		if (XamlRoot is { } xamlRoot)
		{
			XamlRootMap.Unregister(xamlRoot);
		}
	}

	/// <summary>
	/// Stops and disposes this window's renderer (joining its render thread). Called on window close
	/// and on host shutdown, so render threads never outlive the buffers they draw into.
	/// </summary>
	internal void DisposeRenderer()
	{
		if (_rendererDisposed)
		{
			return;
		}
		_rendererDisposed = true;

		_renderer.Dispose();
	}

	UIElement? IXamlRootHost.RootElement => Window?.RootElement;

	void IXamlRootHost.InvalidateRender() => RequestFrame();

	DisplayOrientations IDisplayInformationExtension.CurrentOrientation => _orientation;

	uint IDisplayInformationExtension.ScreenWidthInRawPixels => (uint)_rawWidth;

	uint IDisplayInformationExtension.ScreenHeightInRawPixels => (uint)_rawHeight;

	float IDisplayInformationExtension.LogicalDpi => _scale * DisplayInformation.BaseDpi;

	double IDisplayInformationExtension.RawPixelsPerViewPixel => _scale;

	// Only the discrete ResolutionScale enum is snapped to a valid step; DPI keeps the exact scale.
	ResolutionScale IDisplayInformationExtension.ResolutionScale => (ResolutionScale)(int)(FloorScale(_scale) * 100.0);

	double? IDisplayInformationExtension.DiagonalSizeInInches => null;

	/// <summary>
	/// The configured scale can be overridden globally via the UNO_DISPLAY_SCALE_OVERRIDE environment
	/// variable, keeping a single source of truth shared by the window bounds and DisplayInformation.
	/// </summary>
	private static float ResolveScale(float configuredScale)
		=> float.TryParse(
			Environment.GetEnvironmentVariable("UNO_DISPLAY_SCALE_OVERRIDE"),
			NumberStyles.Any,
			CultureInfo.InvariantCulture,
			out var envScale) && envScale > 0
			? envScale
			: configuredScale;

	private static float FloorScale(float rawScale)
		=> rawScale switch
		{
			>= 5.00f => 5.00f,
			>= 4.50f => 4.50f,
			>= 4.00f => 4.00f,
			>= 3.50f => 3.50f,
			>= 3.00f => 3.00f,
			>= 2.50f => 2.50f,
			>= 2.25f => 2.25f,
			>= 2.00f => 2.00f,
			>= 1.80f => 1.80f,
			>= 1.75f => 1.75f,
			>= 1.60f => 1.60f,
			>= 1.50f => 1.50f,
			>= 1.40f => 1.40f,
			>= 1.25f => 1.25f,
			>= 1.20f => 1.20f,
			_ => 1.00f,
		};
}
