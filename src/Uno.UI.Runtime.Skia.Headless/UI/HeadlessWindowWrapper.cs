#nullable enable

using System;
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
/// the <see cref="XamlRootMap"/> so the framework drives its render cycle independently. Its size can be
/// changed at runtime through the standard <c>AppWindow.Resize</c> (routed here via <see cref="Resize"/>).
/// </summary>
internal sealed class HeadlessWindowWrapper : NativeWindowWrapperBase, IXamlRootHost, IDisplayInformationExtension
{
	private readonly float _scale;
	private readonly HeadlessRenderer _renderer;
	private int _rawWidth;
	private int _rawHeight;
	private bool _closed;
	private bool _rendererDisposed;

	public HeadlessWindowWrapper(Window window, XamlRoot xamlRoot, int width, int height, HeadlessWindowOptions options)
		: base(window, xamlRoot)
	{
		_rawWidth = width;
		_rawHeight = height;
		_scale = options.Scale;

		// Scale feeds bounds via division (size / scale), so reject values a per-window configurator
		// could set that the builder's WithScale/WithDpi would have rejected.
		if (!float.IsFinite(_scale) || _scale <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(options), $"{nameof(HeadlessWindowOptions)}.{nameof(HeadlessWindowOptions.Scale)} must be a finite, strictly positive value.");
		}

		XamlRootMap.Register(xamlRoot, this);

		// Create the renderer before publishing bounds: setting bounds can synchronously route an
		// InvalidateRender back through this wrapper (as IXamlRootHost), which needs a live renderer.
		_renderer = new HeadlessRenderer(this);

		// The XamlRoot is already associated (base ctor), so bounds can be published synchronously.
		ApplySize();
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

	public override void Resize(SizeInt32 size)
	{
		if (size.Width <= 0 || size.Height <= 0 || (size.Width == _rawWidth && size.Height == _rawHeight))
		{
			return;
		}

		_rawWidth = size.Width;
		_rawHeight = size.Height;

		// Publishing the new bounds relayouts and routes an InvalidateRender back through this wrapper.
		ApplySize();
	}

	protected override void ShowCore() => _renderer.Invalidate();

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

	void IXamlRootHost.InvalidateRender() => _renderer.Invalidate();

	DisplayOrientations IDisplayInformationExtension.CurrentOrientation => DisplayOrientations.Landscape;

	uint IDisplayInformationExtension.ScreenWidthInRawPixels => (uint)_rawWidth;

	uint IDisplayInformationExtension.ScreenHeightInRawPixels => (uint)_rawHeight;

	float IDisplayInformationExtension.LogicalDpi => _scale * DisplayInformation.BaseDpi;

	double IDisplayInformationExtension.RawPixelsPerViewPixel => _scale;

	// Only the discrete ResolutionScale enum is snapped to a valid step; DPI keeps the exact scale.
	ResolutionScale IDisplayInformationExtension.ResolutionScale => (ResolutionScale)(int)(FloorScale(_scale) * 100.0);

	double? IDisplayInformationExtension.DiagonalSizeInInches => null;

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
