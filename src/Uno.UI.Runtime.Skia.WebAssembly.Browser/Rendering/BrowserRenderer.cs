using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly Stopwatch _renderStopwatch = new Stopwatch();
	private readonly IXamlRootHost _host;
	private readonly IBrowserRenderer _renderer;
	private readonly JSObject _nativeInstance;

	private int _renderCount;
	private SKCanvas? _canvas;

	public BrowserRenderer(IXamlRootHost host)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		if (WebGlBrowserRenderer.TryCreate(out var webGlBrowserRenderer))
		{
			_renderer = webGlBrowserRenderer;
		}
		else if (SoftwareBrowserRenderer.TryCreate(out var softwareBrowserRenderer))
		{
			_renderer = softwareBrowserRenderer;
		}
		else
		{
			throw new InvalidOperationException("Unable to create renderer");
		}

		_nativeInstance = NativeMethods.CreateInstance(this, WebAssemblyWindowWrapper.Instance.CanvasId);
	}

	internal void InvalidateRender() => NativeMethods.Invalidate(_nativeInstance);

	[JSExport]
	internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance)
	{
		if (instance is BrowserRenderer panel)
		{
			panel.RenderFrame();
		}
	}

	private void RenderFrame()
	{
		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		_renderer.MakeCurrent();

		var currentClipPath = ((CompositionTarget)_host.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_canvas, size =>
		{
			return _canvas = _renderer.Resize((int)size.Width, (int)size.Height);
		});

		if (_canvas is not null)
		{
			_canvas?.Flush();
			_renderer.Flush();
		}

		var (path, fillType) = !currentClipPath.IsEmpty ? (currentClipPath.ToSvgPathData(), currentClipPath.FillType is SKPathFillType.EvenOdd ? "evenodd" : "nonzero") : ("", "nonzero");
		BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHost(path, fillType);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render time: {_renderStopwatch.Elapsed}");
		}
	}


	internal static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createInstance")]
		internal static partial JSObject CreateInstance([JSMarshalAs<JSType.Any>] object owner, string canvadId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);
	}
}
