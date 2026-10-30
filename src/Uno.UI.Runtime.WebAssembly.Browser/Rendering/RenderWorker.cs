using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime;

/// <summary>
/// Manages the dedicated render worker thread for WASM-MT.
/// The render worker owns the OffscreenCanvas and the WebGL context: the deputy thread records frames
/// and signals presents, and the worker drives them through the shared swapchain loop.
/// </summary>
internal static partial class RenderWorker
{
	// Prevent premature GC of the JSWebWorker.
	private static Task? _workerTask;

	private static readonly SemaphoreSlim _frameAvailable = new(0, 1);

	private static IXamlRootHost? _host;

	private const string ModuleName = "uno-render-worker";

	internal static void SignalFrameAvailable()
	{
		// Release the semaphore if it's not already signaled.
		if (_frameAvailable.CurrentCount == 0)
		{
			_frameAvailable.Release();
		}
	}

	internal static Task<ISwapChain> StartAsync(IXamlRootHost host, string canvasId)
	{
		_host = host;

		var contextReady = new TaskCompletionSource<ISwapChain>(TaskCreationOptions.RunContinuationsAsynchronously);

		_workerTask = StartWorker(canvasId, WebAssemblyThreading.WindowObject, contextReady);

		return contextReady.Task;
	}

	/// <summary>
	/// Starts the render worker via JSWebWorker.RunAsync()
	/// We use unsafe accessors since the type is not in the reference assemblies, only in the mt runtime ones.
	/// </summary>
	private static Task StartWorker(string canvasId, JSObject window, TaskCompletionSource<ISwapChain> contextReady)
		=> JSWebWorkerRunAsync(null, () => RenderWorkerMain(canvasId, window, contextReady));

	private static async Task RenderWorkerMain(string canvasId, JSObject windowObject, TaskCompletionSource<ISwapChain> contextReady)
	{
		WasmRenderWorkerGraphicsContext context;

		try
		{
			// Install OffscreenCanvas message handler and GL setup code on this worker's JS context.
			await JSHost.ImportAsync(ModuleName, $"../{Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_APP_BASE")}/render-worker.js");

			// Get render worker's pthread ID.
			var workerPThreadId = NativeMethods.PThreadSelf();

			// Transfer OffscreenCanvas from main browser thread to render worker.
			// windowObject forces a dispatch to the main browser thread.
			NativeMethods.TransferCanvasToWorker(canvasId, workerPThreadId, windowObject);

			// Wait for GL context to be ready.
			var glInfo = await NativeMethods.GLReady();

			context = new WasmRenderWorkerGraphicsContext(
				glInfo.GetPropertyAsInt32("glContextHandle"),
				(uint)glInfo.GetPropertyAsInt32("fboId"),
				glInfo.GetPropertyAsInt32("stencil"),
				glInfo.GetPropertyAsInt32("samples"));
		}
		catch (Exception e)
		{
			contextReady.SetException(e);
			return;
		}

		contextReady.SetResult(context);

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

			try
			{
				var (path, fillType) = BrowserRenderer.GetSvgClipPath(compositionTarget.OnNativePlatformFrameRequested(context));

				BrowserNativeElementHostingExtension.SetSvgClipPathForNativeElementHostAsync(path, fillType);
			}
			catch (Exception e)
			{
				typeof(RenderWorker).LogError()?.Error("Rendering a frame failed.", e);
			}
		}
	}

	[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RunAsync")]
	private static extern Task JSWebWorkerRunAsync(
		[UnsafeAccessorType("System.Runtime.InteropServices.JavaScript.JSWebWorker, System.Runtime.InteropServices.JavaScript")] object? instance,
		Func<Task> body);

	internal static partial class NativeMethods
	{
		[LibraryImport("*", EntryPoint = "pthread_self")]
		internal static partial int PThreadSelf();

		[JSImport($"globalThis.Uno.UI.Runtime.{nameof(RenderWorker)}.transferAndSetupGL")]
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
