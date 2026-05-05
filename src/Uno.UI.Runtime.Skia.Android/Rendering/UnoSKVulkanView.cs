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
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.WinUI.Runtime.Skia.Android.Platform.Vulkan;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Vulkan-backed rendering view for Android, equivalent to UnoSKCanvasView (which uses GLSurfaceView/OpenGL ES).
/// Uses SurfaceView + ISurfaceHolderCallback for Vulkan surface lifecycle.
/// </summary>
internal sealed partial class UnoSKVulkanView : SurfaceView, ISurfaceHolderCallback, IUnoSkiaRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	private readonly VulkanContext _vulkanContext = new();
	private Thread? _renderThread;
	private volatile bool _renderRequested;
	private volatile bool _surfaceReady;
	private volatile bool _disposed;
	private volatile bool _paused;
	private readonly ManualResetEventSlim _renderEvent = new(false);
	private readonly object _renderLock = new();
	private IntPtr _nativeWindow; // Must stay alive while Vulkan surfaces reference it

	public UnoSKVulkanView(Context context) : base(context)
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
		// Vulkan context will be recreated on next surface creation
	}

	void IUnoSkiaRenderView.OnPause()
	{
		// _renderRequested is preserved across pause for replay on resume.
		_paused = true;
	}

	void IUnoSkiaRenderView.OnResume()
	{
		_paused = false;
		// Recover the render-scheduling state machine from any callback dropped
		// across the pause/resume boundary, then force one frame.
		CompositionTarget.NotifyRenderingResumed();
		InvalidateRender();
	}

	#region SurfaceHolder.Callback

	public void SurfaceCreated(ISurfaceHolder holder)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("UnoSKVulkanView: SurfaceCreated");
		}

		_surfaceReady = true;

		// Start the render thread
		_renderThread = new Thread(RenderLoop)
		{
			Name = "UnoVulkanRenderThread",
			IsBackground = true
		};
		_renderThread.Start(holder);
	}

	public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"UnoSKVulkanView: SurfaceChanged {width}x{height}");
		}

		if (_vulkanContext.IsInitialized)
		{
			_vulkanContext.Resize(width, height);
		}
		// Signal a render
		InvalidateRender();
	}

	public void SurfaceDestroyed(ISurfaceHolder holder)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("UnoSKVulkanView: SurfaceDestroyed");
		}

		_surfaceReady = false;
		_renderEvent.Set(); // Wake the render thread so it can exit
		_renderThread?.Join(TimeSpan.FromSeconds(2));
		_renderThread = null;

		// Dispose Vulkan context first, then release the native window
		_vulkanContext.Dispose();

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
			// Initialize Vulkan on the render thread
			InitializeVulkan(holder);

			while (_surfaceReady && !_disposed)
			{
				// Park until a Set() wakes us. OnPause does not wake the loop —
				// it just flips _paused so a mid-render iteration skips back to Wait.
				_renderEvent.Wait();
				_renderEvent.Reset();

				if (!_surfaceReady || _disposed || _paused || !_renderRequested)
					continue;

				_renderRequested = false;
				RenderFrame();
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("UnoSKVulkanView render thread failed", ex);
			}
		}
	}

	private void InitializeVulkan(ISurfaceHolder holder)
	{
		var surface = holder.Surface;
		if (surface == null || !surface.IsValid)
			throw new InvalidOperationException("Android Surface is not valid");

		// Get the ANativeWindow pointer from the Surface.
		// We must keep this alive for the lifetime of the Vulkan context because
		// the VkSurfaceKHR created from it internally references the ANativeWindow.
		// Releasing it while Vulkan surfaces are active causes a destroyed-mutex crash.
		_nativeWindow = ANativeWindow_fromSurface(
			JNIEnv.Handle,
			surface.Handle);

		if (_nativeWindow == IntPtr.Zero)
			throw new InvalidOperationException("Failed to get ANativeWindow from Surface");

		var rect = holder.SurfaceFrame;
		var factory = new AndroidVulkanSurfaceFactory();
		_vulkanContext.Initialize(factory, _nativeWindow, rect!.Width(), rect.Height());

		var (deviceName, driverVersion) = _vulkanContext.GetDeviceInfo();
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Vulkan rendering initialized: {deviceName}, {driverVersion}");
		}
	}

	private void RenderFrame()
	{
		if (!_vulkanContext.IsInitialized)
			return;

		try
		{
			_vulkanContext.RenderFrame(skSurface =>
			{
				var compositionTarget = Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement?.Visual.CompositionTarget as CompositionTarget;
				if (compositionTarget == null)
					return;

				var nativeClipPath = compositionTarget.OnNativePlatformFrameRequested(
					skSurface.Canvas,
					size => skSurface.Canvas);

				// Update the native layer host clip path
				ApplicationActivity.NativeLayerHost!.Path = nativeClipPath;
			});
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("UnoSKVulkanView: Frame render failed", ex);
			}
		}
	}

	#endregion

	#region Native Interop

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern void ANativeWindow_release(IntPtr window);

	#endregion

	#region Input / Accessibility (mirrored from UnoSKCanvasView)

	public override bool OnCheckIsTextEditor() => true;

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		if (e is null)
			return base.DispatchHoverEvent(e);
		return ExploreByTouchHelper.DispatchHoverEvent(e) || base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e is null)
			return base.DispatchKeyEvent(e);
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
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(UnoSKVulkanView)}.{nameof(OnFocusChanged)} failed", e);
			}
		}
	}

	public override void OnProvideAutofillVirtualStructure(ViewStructure? structure, [GeneratedEnum] AutofillFlags flags)
	{
		base.OnProvideAutofillVirtualStructure(structure, flags);
		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
			return;
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
			_vulkanContext.Dispose();
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
