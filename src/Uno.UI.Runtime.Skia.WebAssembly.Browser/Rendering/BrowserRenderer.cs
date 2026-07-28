using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly Stopwatch _renderStopwatch = new Stopwatch();
	private readonly IXamlRootHost _host;
	private readonly IBrowserRenderer? _renderer;
	private JSObject? _nativeInstance;

	private int _renderCount;
	private SKCanvas? _canvas;
	private bool _pendingInvalidate;

	public BrowserRenderer(IXamlRootHost host, bool forceSoftwareRendering)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		if (WebAssemblyThreading.IsThreadingEnabled)
		{
			// TODO: Software rendering (post-MVP)
			if (forceSoftwareRendering)
			{
				throw new NotSupportedException("Software renderer is not supported on MT.");
			}

			// TODO: Refactor to use IBrowserRenderer (post-MVP)
			// In MT mode, the render worker owns the WebGL context and all GL resources.
			// The render worker is started separately by the host.
			// BrowserRenderer only handles the InvalidateRender.
			_renderer = null;
		}
		else if (!forceSoftwareRendering && WebGlBrowserRenderer.TryCreate(out var webGlBrowserRenderer))
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
	}

	internal void InvalidateRender()
	{
		if (_pendingInvalidate)
		{
			return;
		}

		_pendingInvalidate = true;
		_nativeInstance ??= NativeMethods.CreateInstance(this, WebAssemblyWindowWrapper.Instance.CanvasId);
		NativeMethods.Invalidate(_nativeInstance);
	}

	[JSExport]
	internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance)
	{
		((BrowserRenderer)instance).RenderFrame();
	}

	[JSExport]
	internal static Task RenderFrameAsync([JSMarshalAs<JSType.Any>] object instance)
	{
		((BrowserRenderer)instance).RenderFrameMT();
		return Task.CompletedTask;
	}

	private void RenderFrame()
	{
		// The RootElement may not be set yet during startup because the JavaScript
		// requestAnimationFrame can fire before app initialization completes. When that
		// happens, re-arm another frame instead of dropping the pending request, otherwise
		// the render pump stalls and the splash screen is never removed (#23586).
		if (_host.RootElement is not { Visual.CompositionTarget: CompositionTarget compositionTarget })
		{
			if (_pendingInvalidate && _nativeInstance is not null)
			{
				NativeMethods.Invalidate(_nativeInstance);
			}

			return;
		}

		_pendingInvalidate = false;

		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		_renderer!.MakeCurrent();

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

	private void RenderFrameMT()
	{
		_pendingInvalidate = false;

		if (_host.RootElement is not { Visual.CompositionTarget: CompositionTarget compositionTarget })
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("Render");
		}

		RenderWorker.SignalFrameAvailable();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render worker was signaled.");
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
