using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly Stopwatch _renderStopwatch = new Stopwatch();
	private readonly IXamlRootHost _host;
	private readonly IBrowserRenderer _renderer;
	private readonly JSObject _nativeInstance;

	private int _renderCount;
	private SKCanvas? _canvas;

	public BrowserRenderer(IXamlRootHost host, bool forceSoftwareRendering)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		if (!forceSoftwareRendering && WebGlBrowserRenderer.TryCreate(out var webGlBrowserRenderer))
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
		((BrowserRenderer)instance).RenderFrame();
	}

	private void RenderFrame()
	{
		// The RootElement may not be set yet during startup because the JavaScript
		// requestAnimationFrame can fire before the app initialization completes.
		if (_host.RootElement is not { Visual.CompositionTarget: CompositionTarget compositionTarget })
		{
			return;
		}

		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		_renderer.MakeCurrent();

		if (_renderer.NeedsForceResize())
		{
			_canvas?.Dispose();
			_canvas = null;
		}

		var currentClipPath = compositionTarget.OnNativePlatformFrameRequested(_canvas, size =>
		{
			return _canvas = _renderer.Resize((int)size.Width, (int)size.Height);
		});

		if (_canvas is not null)
		{
			_canvas.Flush();
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
		internal static partial JSObject CreateInstance([JSMarshalAs<JSType.Any>] object owner, string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);
	}
}
