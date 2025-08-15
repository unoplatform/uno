﻿using SkiaSharp;
using MUX = Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Windows.Graphics.Display;
using System.Runtime.InteropServices.JavaScript;
using Uno.UI.Hosting;
using System.Diagnostics;
using Uno.UI.Helpers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserRenderer
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly IXamlRootHost _host;
	private int _renderCount;
	private DisplayInformation? _displayInformation;
	private bool _isWindowInitialized;

	private const int ResourceCacheBytes = 256 * 1024 * 1024; // 256 MB
	private const SKColorType colorType = SKColorType.Rgba8888;
	private const GRSurfaceOrigin surfaceOrigin = GRSurfaceOrigin.BottomLeft;

	private readonly JSObject _nativeSwapChainPanel;
	private readonly Stopwatch _renderStopwatch = new Stopwatch();

	private GRGlInterface? _glInterface;
	private GRContext? _context;
	private JsInfo _jsInfo;
	private GRGlFramebufferInfo _glInfo;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private SKCanvas? _canvas;

	private SKSizeI? _lastSize;

	public BrowserRenderer(IXamlRootHost host)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing Renderer");
		}

		_host = host;

		_nativeSwapChainPanel = NativeMethods.CreateInstance(this);

		CompositionTarget.RenderingActiveChanged += CompositionTarget_RenderingActiveChanged;
	}

	private void CompositionTarget_RenderingActiveChanged()
	{
		NativeMethods.SetContinousRender(_nativeSwapChainPanel, CompositionTarget.IsRenderingActive);
	}

	private void Initialize()
	{
		_jsInfo = NativeMethods.CreateContext(this, _nativeSwapChainPanel, WebAssemblyWindowWrapper.Instance?.CanvasId ?? "invalid");
	}

	internal void InvalidateRender() => NativeMethods.Invalidate(_nativeSwapChainPanel);

	private void RenderFrame()
	{
		using var _ = _fpsHelper.BeginFrame();

		if (_host.RootElement is { } rootElement && (rootElement.IsArrangeDirtyOrArrangeDirtyPath || rootElement.IsMeasureDirtyOrMeasureDirtyPath))
		{
			InvalidateRender();
			return;
		}

		if (!_jsInfo.IsValid)
		{
			Initialize();
		}

		_renderStopwatch.Restart();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render {_renderCount++}");
		}

		// create the SkiaSharp context
		if (_context == null)
		{
			_glInterface = GRGlInterface.Create();
			_context = GRContext.CreateGl(_glInterface);

			// bump the default resource cache limit
			_context.SetResourceCacheLimit(ResourceCacheBytes);
		}

		_displayInformation ??= DisplayInformation.GetForCurrentView();

		var scale = _displayInformation.RawPixelsPerViewPixel;

		// get the new surface size
		var newCanvasSize = new SKSizeI((int)(Microsoft.UI.Xaml.Window.CurrentSafe!.Bounds.Width), (int)(Microsoft.UI.Xaml.Window.CurrentSafe!.Bounds.Height));

		// manage the drawing surface
		if (_surface == null || _canvas == null || _renderTarget == null || _lastSize != newCanvasSize || !_renderTarget.IsValid)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Render size {newCanvasSize.Width}x{newCanvasSize.Height}, Window size: {Microsoft.UI.Xaml.Window.Current!.Bounds}, Scale: {scale}");
			}

			// create or update the dimensions
			_lastSize = newCanvasSize;

			_glInfo = new GRGlFramebufferInfo(_jsInfo.FboId, colorType.ToGlSizedFormat());

			// destroy the old surface
			_surface?.Dispose();
			_surface = null;
			_canvas = null;

			// re-create the render target
			_renderTarget?.Dispose();
			_renderTarget = new GRBackendRenderTarget((int)(newCanvasSize.Width * scale), (int)(newCanvasSize.Height * scale), _jsInfo.Samples, _jsInfo.Stencil, _glInfo);

			// create the surface
			_surface = SKSurface.Create(_context, _renderTarget, surfaceOrigin, colorType);
			_canvas = _surface.Canvas;

			if (!_isWindowInitialized)
			{
				_isWindowInitialized = true;
				// Microsoft.UI.Xaml.Window.Current.OnNativeWindowCreated();
			}
		}

		using (new SKAutoCanvasRestore(_canvas, true))
		{
			_surface.Canvas.Clear(SKColors.Transparent);
			_surface.Canvas.Scale((float)scale);
			if (_host.RootElement?.Visual is { } rootVisual)
			{
				var negativePath = SkiaRenderHelper.RenderRootVisualAndReturnNegativePath(_renderTarget.Width, _renderTarget.Height, rootVisual, _canvas);
				_fpsHelper.DrawFps(_canvas);
				// Unlike other skia platforms, on skia/wasm we need to undo the scaling  adjustment that happens inside
				// RenderRootVisualAndReturnNegativePath since the numbers we get from native are already scaled, so we
				// don't need to do our own scaling in RenderRootVisualAndReturnNegativePath.
				negativePath.Transform(SKMatrix.CreateScale((float)(1 / scale), (float)(1 / scale)), negativePath);
				BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHost(!negativePath.IsEmpty ? negativePath.ToSvgPathData() : "");
				_host.RootElement?.XamlRoot?.InvokeFramePainted();
			}
		}

		// update the control
		_canvas?.Flush();
		_context.Flush();
		_host.RootElement?.XamlRoot?.InvokeFrameRendered();

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Render time: {_renderStopwatch.Elapsed}");
		}

		if (CompositionTarget.IsRenderingActive)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Dispatch next rendering");
			}

			// Force a loop of the dispatcher, so that the Rendering Event
			// even can be raised. It is done synchronously because we're already
			// on a timed callback and it won't requeue immediately like a standard
			// dispatcher dispatch would.
			NativeDispatcher.Main.SynchronousDispatchRendering();
		}
	}

	[JSExport]
	internal static void RenderFrame([JSMarshalAs<JSType.Any>] object instance)
	{
		if (instance is BrowserRenderer panel)
		{
			panel.RenderFrame();
		}
	}

	internal struct JsInfo
	{
		public bool IsValid { get; set; }

		public int ContextId { get; set; }

		public uint FboId { get; set; }

		public int Stencil { get; set; }

		public int Samples { get; set; }

		public int Depth { get; set; }
	}

	internal static partial class NativeMethods
	{
		public static JSObject CreateInstance(BrowserRenderer owner)
		{
			return CreateInstanceInternal(owner);
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createInstance")]
		internal static partial JSObject CreateInstanceInternal([JSMarshalAs<JSType.Any>] object owner);

		internal static JsInfo CreateContext(BrowserRenderer owner, JSObject nativeSwapChainPanel, string canvasId)
		{
			var jsInfo = new JsInfo();
			var jsObject = CreateContextInternal(nativeSwapChainPanel, canvasId);

			jsInfo.IsValid = true;
			jsInfo.ContextId = jsObject.GetPropertyAsInt32("contextId");
			jsInfo.FboId = (uint)jsObject.GetPropertyAsInt32("fboId");
			jsInfo.Stencil = jsObject.GetPropertyAsInt32("stencil");
			jsInfo.Samples = jsObject.GetPropertyAsInt32("samples");
			jsInfo.Depth = jsObject.GetPropertyAsInt32("depth");
			return jsInfo;
		}

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.createContextStatic")]
		internal static partial JSObject CreateContextInternal(JSObject nativeSwapChainPanel, string canvasId);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.invalidate")]
		internal static partial void Invalidate(JSObject nativeSwapChainPanel);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserRenderer)}.setContinousRender")]
		internal static partial void SetContinousRender(JSObject nativeSwapChainPanel, bool enabled);
	}
}
