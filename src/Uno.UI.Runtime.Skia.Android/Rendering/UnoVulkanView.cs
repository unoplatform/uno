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
using Uno.UI.Runtime.Skia.Vulkan;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Android.Platform.Vulkan;

namespace Uno.UI.Runtime.Skia.Android;

/// <summary>
/// Vulkan-backed rendering view for Android — a SurfaceView whose ANativeWindow drives a Vulkan swapchain through
/// the neutral graphics pipeline, serving the <see cref="GraphicsContextKind.Vulkan"/> kind via an
/// <see cref="AndroidVulkanGraphicsContext"/>. Not runtime-validated on Linux CI (needs an Android device/emulator
/// with a Vulkan-capable adapter).
/// </summary>
internal sealed partial class UnoVulkanView : SurfaceView, ISurfaceHolderCallback, IUnoRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	private global::Uno.UI.Composition.Drawing.ISwapChain? _context;
	private global::Uno.UI.Composition.Drawing.IDrawingFactory? _renderer;
	private Thread? _renderThread;
	private volatile bool _renderRequested;
	private volatile bool _surfaceReady;
	private volatile bool _disposed;
	private bool _firstFrameSignaled;
	private int _width, _height;
	private readonly ManualResetEventSlim _renderEvent = new(false);
	private IntPtr _nativeWindow; // Must stay alive while the Vulkan surface references it
	private readonly VulkanContext _vulkanContext = new();
	private readonly AndroidVulkanSurfaceFactory _surfaceFactory = new();

	public UnoVulkanView(Context context) : base(context)
	{
		// Create the window-independent Vulkan resources (instance, device) right away: this throws when the
		// driver is unusable, letting the caller fall back to the OpenGL ES view. The window-scoped part
		// (swapchain) is completed on the render thread once a surface exists.
		_vulkanContext.InitializeDevice(_surfaceFactory);

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
		// The swapchain is recreated on the next surface creation; the device is retained.
	}

	#region SurfaceHolder.Callback

	public void SurfaceCreated(ISurfaceHolder holder)
	{
		_surfaceReady = true;
		_renderThread = new Thread(RenderLoop) { Name = "UnoVulkanRenderThread", IsBackground = true };
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

		// Before the swapchain: the backend built its own command pools, images and pipelines on it, and
		// destroying the swapchain while those are still alive leaves the driver dereferencing them (a SIGSEGV
		// inside vkDestroySwapchainKHR). The device itself outlives the surface (see _vulkanContext.Dispose in
		// Dispose(bool)); surface re-creation negotiates a fresh backend against the same device.
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
			InitializeVulkan(holder);

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
			this.Log().Error("UnoVulkanView render thread failed", ex);
		}
	}

	private void InitializeVulkan(ISurfaceHolder holder)
	{
		var surface = holder.Surface;
		if (surface == null || !surface.IsValid)
		{
			throw new InvalidOperationException("Android Surface is not valid");
		}

		// Keep the ANativeWindow alive for the Vulkan surface's lifetime (the swapchain references it).
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

		// This SurfaceView owns the ANativeWindow, so it serves the Vulkan kind by creating the swapchain context.
		var nativeWindow = _nativeWindow;
		var width = _width;
		var height = _height;
		global::Uno.UI.Composition.Drawing.GraphicsRegistry.ContextFactory =
			kind => System.Threading.Tasks.Task.FromResult<global::Uno.UI.Composition.Drawing.ISwapChain?>(
				kind == global::Uno.UI.Composition.Drawing.GraphicsContextKind.Vulkan
					? new AndroidVulkanGraphicsContext(_vulkanContext, nativeWindow, width, height)
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

		var compositionTarget = Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement?.Visual.CompositionTarget as CompositionTarget;
		if (compositionTarget is null)
		{
			return;
		}

		compositionTarget.Renderer = _renderer!;
		var nativeClipPath = compositionTarget.OnNativePlatformFrameRequested(context);

		ApplicationActivity.NativeLayerHost!.Path = nativeClipPath;

		if (!_firstFrameSignaled)
		{
			_firstFrameSignaled = true;
			NativeWindowWrapper.Instance.NotifyFirstFrameRendered();
			// Trigger OnPreDraw re-evaluation so the splash can dismiss once the first frame is on screen
			ApplicationActivity.RelativeLayout?.Post(() =>
				ApplicationActivity.RelativeLayout?.Invalidate());
		}
	}

	#endregion

	#region Native Interop

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);

	[System.Runtime.InteropServices.DllImport("android")]
	private static extern void ANativeWindow_release(IntPtr window);

	#endregion

	#region Input / Accessibility

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
			this.Log().Error($"{nameof(UnoVulkanView)}.{nameof(OnFocusChanged)} failed", e);
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

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_disposed = true;
			_renderEvent.Set();
			_renderThread?.Join(TimeSpan.FromSeconds(2));
			_context?.Dispose();
			_context = null;
			// Releases the retained instance and device kept alive across surface re-creations.
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
