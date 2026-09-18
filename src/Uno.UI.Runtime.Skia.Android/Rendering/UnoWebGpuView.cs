using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// WebGPU-backed rendering view for Android, mirroring <see cref="UnoVulkanView"/>: a SurfaceView
/// whose ANativeWindow drives a wgpu swapchain through the neutral graphics pipeline. Not runtime-validated on
/// Linux CI (needs an Android device/emulator with a WebGPU-capable adapter).
/// </summary>
internal sealed partial class UnoWebGpuView : SurfaceView, ISurfaceHolderCallback, IUnoRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	private global::Uno.UI.Composition.Drawing.ISwapChain? _context;
	private global::Uno.UI.Composition.Drawing.IDrawingFactory? _renderer;
	private Thread? _renderThread;
	private volatile bool _renderRequested;
	private volatile bool _surfaceReady;
	private volatile bool _disposed;
	private int _width, _height;
	private readonly ManualResetEventSlim _renderEvent = new(false);
	private IntPtr _nativeWindow; // Must stay alive while the wgpu surface references it
	private readonly ApplicationActivity _activity;

	public UnoWebGpuView(ApplicationActivity activity) : base(activity)
	{
		_activity = activity;

		ExploreByTouchHelper = new UnoExploreByTouchHelper(this);
		TextInputPlugin = new TextInputPlugin(this);
		ViewCompat.SetAccessibilityDelegate(this, ExploreByTouchHelper);
		Focusable = true;
		FocusableInTouchMode = true;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			ImportantForAutofill = ImportantForAutofill.Yes;
		}

		SetWillNotDraw(false);
		Holder!.AddCallback(this);
	}

	public void InvalidateRender()
	{
		ExploreByTouchHelper.InvalidateRoot();
		_renderRequested = true;
		_renderEvent.Set();
	}

	public void ResetRendererContext()
	{
		// The WebGPU context is recreated on the next surface creation.
	}

	#region SurfaceHolder.Callback

	public void SurfaceCreated(ISurfaceHolder holder)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("UnoWebGpuView: SurfaceCreated");
		}

		_surfaceReady = true;
		_renderThread = new Thread(RenderLoop) { Name = "UnoWebGpuRenderThread", IsBackground = true };
		_renderThread.Start(holder);
	}

	public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UnoWebGpuView: SurfaceChanged {width}x{height}");
		}

		_width = width;
		_height = height;
		InvalidateRender();
	}

	public void SurfaceDestroyed(ISurfaceHolder holder)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("UnoWebGpuView: SurfaceDestroyed");
		}

		_surfaceReady = false;

		// Android can destroy the surface after the activity's OnDestroy, by which point
		// TeardownRenderer has stopped the render thread and released everything below.
		if (_disposed)
		{
			return;
		}

		_renderEvent.Set();
		var stopped = _renderThread?.Join(TimeSpan.FromSeconds(2)) ?? true;

		// Clearing the reference also retires a thread that outlived the timeout: RenderLoop exits
		// once it is no longer the current render thread, so it cannot resume on the next surface.
		Volatile.Write(ref _renderThread, null);

		if (!stopped && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn("UnoWebGpuView: the render thread did not stop within the timeout.");
		}

		// Before the swapchain: the backend built its own device objects on it, and tearing the swapchain down
		// first leaves the driver dereferencing them. Surface re-creation negotiates a fresh backend.
		(_renderer as IDisposable)?.Dispose();
		_renderer = null;

		_context?.Dispose();
		_context = null;

		if (_nativeWindow != IntPtr.Zero)
		{
			ANativeWindow_release(_nativeWindow);
			_nativeWindow = IntPtr.Zero;
		}
	}

	#endregion

	#region Render Thread

	private void RenderLoop(object? state)
	{
		var holder = (ISurfaceHolder)state!;
		try
		{
			InitializeWebGpu(holder);
		}
		catch (Exception ex)
		{
			// Backend negotiation runs here, on the render thread, so the activity's try/catch around the view
			// constructor cannot cover it — hand the window to the canvas view rather than leave it black.
			this.Log().Error("UnoWebGpuView: WebGPU initialization failed, falling back to the canvas view", ex);
			_activity.FallbackToCanvasView();
			return;
		}

		try
		{
			while (_surfaceReady && !_disposed && ReferenceEquals(Volatile.Read(ref _renderThread), Thread.CurrentThread))
			{
				_renderEvent.Wait(TimeSpan.FromMilliseconds(100));
				_renderEvent.Reset();

				if (!_surfaceReady || _disposed || !_renderRequested)
				{
					continue;
				}

				_renderRequested = false;
				RenderFrame();
			}
		}
		catch (Exception ex)
		{
			this.Log().Error("UnoWebGpuView render thread failed", ex);
		}
	}

	private void InitializeWebGpu(ISurfaceHolder holder)
	{
		var surface = holder.Surface;
		if (surface == null || !surface.IsValid)
		{
			throw new InvalidOperationException("Android Surface is not valid");
		}

		// Keep the ANativeWindow alive for the wgpu surface's lifetime (the swapchain references it).
		_nativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, surface.Handle);
		// surface must stay alive across the interop call above, or it can be collected mid-call.
		GC.KeepAlive(surface);
		if (_nativeWindow == IntPtr.Zero)
		{
			throw new InvalidOperationException("Failed to get ANativeWindow from Surface");
		}

		var rect = holder.SurfaceFrame!;
		_width = rect.Width();
		_height = rect.Height();

		// This SurfaceView owns the ANativeWindow, so it serves the WebGpu kind by creating the swapchain context.
		// The wgpu P/Invoke resolves at runtime, so a Skia-only app that never negotiates WebGpu never loads it.
		var nativeWindow = _nativeWindow;
		global::Uno.UI.Composition.Drawing.GraphicsRegistry.ContextFactory =
			kind => System.Threading.Tasks.Task.FromResult<global::Uno.UI.Composition.Drawing.ISwapChain?>(
				kind == global::Uno.UI.Composition.Drawing.GraphicsContextKind.WebGpu
					? global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateAndroid(nativeWindow, 1f)
					: null);
		var init = global::Uno.UI.Composition.Drawing.GraphicsRegistry.Initialize();
		_context = init.Context;
		_renderer = init.Renderer;
		// Effect brushes read this while recording, so it must be set as soon as the renderer is known.
		Microsoft.UI.Composition.Compositor.GetSharedCompositor().IsSoftwareRenderer =
			init.Context.Kind == global::Uno.UI.Composition.Drawing.GraphicsContextKind.Software;
	}

	private void RenderFrame()
	{
		if (_context is not { } context)
		{
			return;
		}

		var compositionTarget = _activity.RootElement?.Visual.CompositionTarget as CompositionTarget;
		if (compositionTarget is null)
		{
			// OnNativePlatformFrameRequested is the only thing that clears the target's
			// RenderRequested flag, so dropping the frame outright would make every later
			// RequestNewFrame a no-op. Re-arm so the loop retries once the window is ready.
			_renderRequested = true;
			return;
		}

		// Contained per frame: letting it reach the loop would end the render thread for good, freezing the app on
		// its last frame while input keeps being delivered.
		try
		{
			compositionTarget.Renderer = _renderer!;
			var nativeClipPath = compositionTarget.OnNativePlatformFrameRequested(context);

			if (_activity.NativeLayerHost is { } nativeLayerHost)
			{
				nativeLayerHost.Path = nativeClipPath;
			}

			if (_activity.Wrapper.TryReleaseFirstFrameGate())
			{
				// Trigger OnPreDraw re-evaluation so the splash can dismiss once the first frame is on screen
				_activity.RelativeLayout?.Post(() =>
					_activity.RelativeLayout?.Invalidate());
			}
		}
		catch (Exception ex)
		{
			this.Log().Error("UnoWebGpuView: frame render failed", ex);
		}
	}

	#endregion

	#region Native Interop

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern void ANativeWindow_release(IntPtr window);

	#endregion

	#region Input / Accessibility (mirrored from UnoVulkanView)

	public override bool OnCheckIsTextEditor() => true;

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		if (e is null)
		{
			return base.DispatchHoverEvent(e);
		}
		return ExploreByTouchHelper.DispatchHoverEvent(e) || base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e is null)
		{
			return base.DispatchKeyEvent(e);
		}
		return ExploreByTouchHelper.DispatchKeyEvent(e) || base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
		try
		{
			ExploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
		}
		catch (Exception e)
		{
			this.Log().Error($"{nameof(UnoWebGpuView)}.{nameof(OnFocusChanged)} failed", e);
		}
	}

	public override void OnProvideAutofillVirtualStructure(ViewStructure? structure, [GeneratedEnum] AutofillFlags flags)
	{
		base.OnProvideAutofillVirtualStructure(structure, flags);
		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
		{
			return;
		}
		TextInputPlugin.OnProvideAutofillVirtualStructure(structure);
	}

	public override void Autofill(SparseArray values)
	{
		var count = values.Size();
		for (int i = 0; i < count; i++)
		{
			var virtualId = values.KeyAt(i);
			if (AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.LiveTextBoxesMap.TryGetValue(virtualId, out var textBox))
			{
				var autofillValue = (AutofillValue)values.ValueAt(i)!;
				textBox.Text = autofillValue.TextValue ?? string.Empty;
			}
		}
	}

	public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
		=> TextInputPlugin.OnCreateInputConnection(outAttrs!);

	#endregion

	public void TeardownRenderer()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_renderEvent.Set();
		var stopped = _renderThread?.Join(TimeSpan.FromSeconds(2)) ?? true;
		_renderThread = null;

		if (!stopped)
		{
			// The render thread is still inside a frame, holding the WebGPU context and the native
			// window. Releasing them here would free objects it is about to touch, so leave them to
			// the process teardown rather than corrupt the driver.
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("The WebGPU render thread did not stop within the timeout; its resources are left to the process teardown.");
			}

			return;
		}

		// The backend owns device objects built on the swapchain, so it goes first.
		(_renderer as IDisposable)?.Dispose();
		_renderer = null;
		_context?.Dispose();
		_context = null;
		if (_nativeWindow != IntPtr.Zero)
		{
			ANativeWindow_release(_nativeWindow);
			_nativeWindow = IntPtr.Zero;
		}
		_renderEvent.Dispose();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			TeardownRenderer();
		}
		base.Dispose(disposing);
	}
}
