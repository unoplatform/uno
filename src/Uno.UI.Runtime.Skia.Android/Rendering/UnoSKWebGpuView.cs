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

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// WebGPU-backed rendering view for Android, mirroring <see cref="UnoSKVulkanView"/>: a SurfaceView
/// whose ANativeWindow drives a wgpu swapchain through the neutral graphics pipeline. Not runtime-validated on
/// Linux CI (needs an Android device/emulator with a WebGPU-capable adapter).
/// </summary>
internal sealed partial class UnoSKWebGpuView : SurfaceView, ISurfaceHolderCallback, IUnoSkiaRenderView
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

	public UnoSKWebGpuView(Context context) : base(context)
	{
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
		_surfaceReady = true;
		_renderThread = new Thread(RenderLoop) { Name = "UnoWebGpuRenderThread", IsBackground = true };
		_renderThread.Start(holder);
	}

	public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
	{
		_width = width;
		_height = height;
		InvalidateRender();
	}

	public void SurfaceDestroyed(ISurfaceHolder holder)
	{
		_surfaceReady = false;
		_renderEvent.Set();
		_renderThread?.Join(TimeSpan.FromSeconds(2));
		_renderThread = null;

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

			while (_surfaceReady && !_disposed)
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
			this.Log().Error("UnoSKWebGpuView render thread failed", ex);
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
	}

	private void RenderFrame()
	{
		if (_context is not { } context)
		{
			return;
		}

		var compositionTarget = Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement?.Visual.CompositionTarget as CompositionTarget;
		if (compositionTarget is null)
		{
			return;
		}

		compositionTarget.Renderer = _renderer!;
		var nativeClipPath = compositionTarget.OnNativePlatformFrameRequested(context);

		ApplicationActivity.NativeLayerHost!.Path = nativeClipPath;
	}

	#endregion

	#region Native Interop

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern void ANativeWindow_release(IntPtr window);

	#endregion

	#region Input / Accessibility (mirrored from UnoSKVulkanView)

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
			this.Log().Error($"{nameof(UnoSKWebGpuView)}.{nameof(OnFocusChanged)} failed", e);
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
				textBox.Text = autofillValue.TextValue;
			}
		}
	}

	public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
		=> TextInputPlugin.OnCreateInputConnection(outAttrs!);

	#endregion

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disposed = true;
			_renderEvent.Set();
			_renderThread?.Join(TimeSpan.FromSeconds(2));
			_context?.Dispose();
			_context = null;
			if (_nativeWindow != IntPtr.Zero)
			{
				ANativeWindow_release(_nativeWindow);
				_nativeWindow = IntPtr.Zero;
			}
			_renderEvent.Dispose();
		}
		base.Dispose(disposing);
	}
}
