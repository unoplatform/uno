using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Manages the dedicated render worker thread for WASM-MT.
/// The render worker owns the OffscreenCanvas, WebGL context, and all SkiaSharp GL resources.
/// It receives SKPicture frames from the deputy thread and replays them onto the GL surface.
/// </summary>
internal static partial class RenderWorker
{
	// Prevent premature GC of the JSWebWorkerInstance (.NET 9 workaround).
	private static Task? _workerTask;

	private static readonly SemaphoreSlim _frameAvailable = new(0, 1);

	private static IXamlRootHost? _host;

	private static GRContext? _grContext;
	private static GRBackendRenderTarget? _backendRenderTarget;
	private static SKSurface? _surface;
	private static SKCanvas? _canvas;

	private const string ModuleName = "uno-render-worker";

	internal static void SignalFrameAvailable()
	{
		// Release the semaphore if it's not already signaled.
		if (_frameAvailable.CurrentCount == 0)
		{
			_frameAvailable.Release();
		}
	}

	internal static void Start(WebAssemblyBrowserHost host, string canvasId)
	{
		_host = host;

		_workerTask = StartWorker(canvasId, WebAssemblyThreading.WindowObject);
	}

	/// <summary>
	/// Starts the render worker via JSWebWorker.RunAsync()
	/// We use reflection since the type is not in the reference assemblies, only in the mt runtime ones.
	/// </summary>
	private static Task StartWorker(string canvasId, JSObject window)
	{
		var jsWebWorkerType =
			Type.GetType("System.Runtime.InteropServices.JavaScript.JSWebWorker, System.Runtime.InteropServices.JavaScript") ??
				throw new InvalidOperationException("JSWebWorker type not found.");

		var runAsyncMethod =
			jsWebWorkerType.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Static, [typeof(Func<Task>)]) ??
				throw new InvalidOperationException("JSWebWorker.RunAsync(Func<Task>) method not found.");

		var renderWorkerMain = () => RenderWorkerMain(canvasId, window);

		return (Task)runAsyncMethod.Invoke(null, [renderWorkerMain])!;
	}

	private static async Task RenderWorkerMain(string canvasId, JSObject windowObject)
	{
		// Install OffscreenCanvas message handler and and GL setup code on this worker's JS context.
		await JSHost.ImportAsync("uno-render-worker", $"../{Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_APP_BASE")}/render-worker.js");

		// Get render worker's pthread ID.
		var workerPThreadId = NativeMethods.PThreadSelf();

		// Transfer OffscreenCanvas from main browser thread to render worker.
		// windowObject forces a dispatch to the main browser thread.
		NativeMethods.TransferCanvasToWorker(canvasId, workerPThreadId, windowObject);

		// Wait for GL context to be ready.
		var glInfo = await NativeMethods.GLReady();

		var glContextHandle = glInfo.GetPropertyAsInt32("glContextHandle");
		var fboId = (uint)glInfo.GetPropertyAsInt32("fboId");
		var stencil = glInfo.GetPropertyAsInt32("stencil");
		var samples = glInfo.GetPropertyAsInt32("samples");

		// Make GL context current and create SkiaSharp resources.
		NativeMethods.GLMakeCurrent(glContextHandle);

		_grContext = GRContext.CreateGl(GRGlInterface.Create());
		_grContext.SetResourceCacheLimit(256 * 1024 * 1024 /* 256 MB */);

		// === Render loop ===
		// IMPORTANT: Must use async wait (SemaphoreSlim.WaitAsync).
		// On a JSWebWorker thread, blocking calls freezes the event loop,
		// preventing rendered frames from ever being presented.
		while (true)
		{
			await _frameAvailable.WaitAsync();

			if (_host?.RootElement is not { Visual.CompositionTarget: CompositionTarget compositionTarget })
			{
				continue;
			}

			NativeMethods.GLMakeCurrent(glContextHandle);

			compositionTarget.OnNativePlatformFrameRequested(_canvas, size =>
			{
				var width = (int)size.Width;
				var height = (int)size.Height;

				NativeMethods.ResizeCanvas(width, height);

				_surface?.Dispose();
				_surface = null;

				_backendRenderTarget?.Dispose();

				var glFramebufferInfo = new GRGlFramebufferInfo(fboId, /* RGBA8 */ 0x8058u);

				_backendRenderTarget = new GRBackendRenderTarget(width, height, samples, stencil, glFramebufferInfo);

				_surface = SKSurface.Create(_grContext, _backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);

				return _canvas = _surface!.Canvas;
			});

			if (_canvas is not null)
			{
				_grContext.Flush(submit: true);
			}
		}
	}

	internal static partial class NativeMethods
	{
		[LibraryImport("*", EntryPoint = "pthread_self")]
		internal static partial int PThreadSelf();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.RenderWorker.transferAndSetupGL")]
		internal static partial void TransferCanvasToWorker(string canvasId, int targetPthreadId, JSObject window);

		[JSImport("glReady", ModuleName)]
		[return: JSMarshalAs<JSType.Promise<JSType.Object>>]
		internal static partial Task<JSObject> GLReady();

		[JSImport("glMakeCurrent", ModuleName)]
		internal static partial void GLMakeCurrent(int contextHandle);

		[JSImport("resizeCanvas", ModuleName)]
		internal static partial void ResizeCanvas(int width, int height);
	}
}
