using System;
using System.Threading.Tasks;
using Uno.UI.Composition.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly Stopwatch _renderStopwatch = new Stopwatch();
	private readonly IXamlRootHost _host;
	private readonly IBrowserRenderer? _renderer;
	private JSObject? _nativeInstance;

	// On-canvas WebGPU path (opt-in via UNO_WEBGPU): the device is created asynchronously, so _webgpuContext is
	// null until ready and frames re-arm meanwhile. Uses its own canvas surface (no Skia SKCanvas / WebGL context).
	private readonly bool _webgpuRequested;
	private IGraphicsContext? _webgpuContext;

	private int _renderCount;
	private IRenderTarget? _renderTarget;
	private bool _pendingInvalidate;

	public BrowserRenderer(IXamlRootHost host, bool forceSoftwareRendering)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		var webgpu = Environment.GetEnvironmentVariable("UNO_WEBGPU");
		if (webgpu is "1" or "true" or "neutral" or "swapchain")
		{
			// WebGPU needs its own canvas context (can't coexist with a WebGL context on the same canvas), so
			// don't create the WebGl/Software renderer; kick off async device init and render once it's ready.
			_webgpuRequested = true;
			_ = InitWebGpuAsync();
			return;
		}

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
	}

	private async Task InitWebGpuAsync()
	{
		try
		{
			// The host references no WebGPU type: it hands the neutral browser window (canvas id) to the pluggable
			// pipeline and the app-registered WebGPU context factory does the async JS device import + canvas surface
			// (device bring-up runs in JS — the in-WASM event pump hangs from a managed call stack). InitializeAsync
			// mints the context + renderer; the render loop drives the neutral IGraphicsContext.
			var window = new WasmGraphicsNativeWindow(WebAssemblyWindowWrapper.Instance.CanvasId);
			var init = await GraphicsRegistry.InitializeAsync(window, new[] { GraphicsContextKind.WebGpu });
			_webgpuContext = init.Context;
			CompositionTarget.Renderer = init.Renderer;
			this.Log().Info("Neutral graphics pipeline active: WebGpu context via the neutral pipeline (browser).");
			// Force a fresh record+present under the new renderer (the last frame was recorded by SkiaRenderer
			// before the async switch and is skipped by the present session).
			(_host.RootElement as Microsoft.UI.Xaml.UIElement)?.InvalidateArrange();
			InvalidateRender();   // device is ready — request a frame now
		}
		catch (Exception e)
		{
			this.Log().Error($"WebGPU browser init failed: {e.Message}. No fallback (canvas context already claimed).");
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

		if (_webgpuRequested)
		{
			if (_webgpuContext is null)
			{
				// Async device init not finished yet — re-arm so we render as soon as it's ready.
				if (_nativeInstance is not null) { NativeMethods.Invalidate(_nativeInstance); }
				return;
			}

			// Render into the offscreen resolve target, then Present() blits it into the canvas backbuffer.
			var webgpuClip = compositionTarget.OnNativePlatformFrameRequested(
				null,
				size => _webgpuContext.AcquireRenderTarget((int)size.Width, (int)size.Height));
			_webgpuContext.Present();
			ApplyNativeElementClip(webgpuClip);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render time (WebGPU): {_renderStopwatch.Elapsed}");
			}
			return;
		}

		_renderer!.MakeCurrent();

		if (_renderer.NeedsForceResize())
		{
			_renderTarget?.Dispose();
			_renderTarget = null;
		}

		var currentClipPath = compositionTarget.OnNativePlatformFrameRequested(_renderTarget, size =>
		{
			_renderTarget?.Dispose();
			return _renderTarget = _renderer.Resize((int)size.Width, (int)size.Height);
		});

		if (_renderTarget is not null)
		{
			// The Skia backend flushed its surface; present the buffer (GL flush / software blit).
			_renderer.Flush();
		}

		ApplyNativeElementClip(currentClipPath);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render time: {_renderStopwatch.Elapsed}");
		}
	}

	private static void ApplyNativeElementClip(IGeometry currentClipPath)
	{
		string path, fillType;
		if (!currentClipPath.IsEmpty)
		{
			path = currentClipPath.ToSvgPathData();
			fillType = currentClipPath.FillRule == GeometryFillRule.EvenOdd ? "evenodd" : "nonzero";
		}
		else
		{
			path = "";
			fillType = "nonzero";
		}
		BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHost(path, fillType);
	}


	internal static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createInstance")]
		internal static partial JSObject CreateInstance([JSMarshalAs<JSType.Any>] object owner, string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);
	}
}
