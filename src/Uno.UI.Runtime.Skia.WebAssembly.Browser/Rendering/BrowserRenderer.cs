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
	private JSObject? _nativeInstance;

	// Created asynchronously (the WebGPU device import runs in JS and must not block the JS thread), so it stays
	// null until ready while frames re-arm meanwhile.
	private ISwapChain? _context;
	private IDrawingFactory? _renderer;
	private bool _initFailed;

	private int _renderCount;
	private bool _pendingInvalidate;

	public BrowserRenderer(IXamlRootHost host, bool forceSoftwareRendering)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		// Context creation is async (the WebGPU device import runs in JS), so kick it off and render once ready.
		_ = InitAsync(forceSoftwareRendering);
	}

	private async Task InitAsync(bool forceSoftwareRendering)
	{
		try
		{
			// Only GPU-API context creation is browser-specific; the app-registered backend owns the kind order.
			GraphicsRegistry.ContextFactory = kind => CreateContextAsync(kind, forceSoftwareRendering);

			var init = await GraphicsRegistry.InitializeAsync();
			_context = init.Context;
			_renderer = init.Renderer;
			Microsoft.UI.Composition.Compositor.GetSharedCompositor().IsSoftwareRenderer = init.Context.Kind == GraphicsContextKind.Software;

			// Force a fresh record+present: a frame recorded before the async switch completes is skipped.
			(_host.RootElement as Microsoft.UI.Xaml.UIElement)?.InvalidateArrange();
			InvalidateRender();
		}
		catch (Exception e)
		{
			// Terminal: stop re-arming the frame pump, since nothing will ever set _context now.
			_initFailed = true;
			this.Log().Error($"Browser graphics init failed: {e.Message}.");
		}
	}

	/// <summary>
	/// Creates the browser context for the requested kind: WebGL, the 2D-canvas software context, or the WebGpu
	/// canvas context. WebGL is declined when the host is configured for software.
	/// </summary>
	private async Task<ISwapChain?> CreateContextAsync(GraphicsContextKind kind, bool forceSoftwareRendering)
	{
		switch (kind)
		{
			case GraphicsContextKind.WebGL when !forceSoftwareRendering:
				return WebGlBrowserRenderer.TryCreate(out var gl) ? new WasmGLGraphicsContext(gl) : null;
			case GraphicsContextKind.Software:
				return SoftwareBrowserRenderer.TryCreate(out var sw) ? new WasmSoftwareGraphicsContext(sw) : null;
			case GraphicsContextKind.WebGpu:
				// WebGpuContext is a lightweight renderer-agnostic assembly whose emdawnwebgpu link is always
				// present for WASM via WebGpu.Init's targets, so no reflection or opt-in-link is needed.
				return await global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateWasmAsync(WebAssemblyWindowWrapper.Instance.CanvasId);
			default:
				return null;
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

		if (_context is null)
		{
			// Async context init not finished yet — re-arm so we render as soon as it's ready (unless it failed).
			if (!_initFailed && _nativeInstance is not null) { NativeMethods.Invalidate(_nativeInstance); }
			return;
		}

		// The context owns the surface/present; the backend (whichever won negotiation) wraps the acquired target.
		// The renderer is per-window (bound to this window's context), installed on its CompositionTarget each frame.
		compositionTarget.Renderer = _renderer!;
		var currentClipPath = compositionTarget.OnNativePlatformFrameRequested(_context);
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
