using System;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// EXPERIMENTAL WebGPU-backed render view for AppleUIKit (iOS/tvOS), mirroring the Android <c>UnoSKWebGpuView</c>:
/// a <c>UIView</c> whose backing <see cref="CAMetalLayer"/> drives a wgpu swapchain through the shared
/// wgpu swapchain over its CAMetalLayer. Present is wgpuSurfacePresent, and the scene is drawn through the
/// neutral <c>CompositionTarget.OnNativePlatformFrameRequested</c> seam via the app-registered WebGPU pipeline.
/// Opt in with UNO_WEBGPU. Not toolchain-validated on Linux CI — needs an Apple device build (and the wgpu-native
/// iOS static lib linked via wgpu-native.targets, so <c>DllImport("webgpu")</c> resolves to the main program).
/// </summary>
internal sealed partial class UnoSKWebGpuMetalView : UIView, IAppleUIKitRenderView
{
	private RootViewController? _owner;
	private global::Uno.UI.Composition.Drawing.IGraphicsContext? _context;
	private readonly CADisplayLink _link;
	private readonly nint _fps;
	private readonly float _scale;
	private Thread? _renderThread;
	private bool _rendererInstalled;

	[Export("layerClass")]
	public static Class LayerClass() => new Class(typeof(CAMetalLayer));

	private CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

	public UnoSKWebGpuMetalView()
		: base(CGRect.Empty)
	{
		// Match the screen's pixel density so the wgpu drawable is full-resolution (CAMetalLayer.drawableSize is in
		// pixels; the view's Bounds are in points).
		ContentScaleFactor = UIScreen.MainScreen.Scale;
		MetalLayer.ContentsScale = UIScreen.MainScreen.Scale;

		// UIKit APIs are UI-thread-checked: create the link and read the display rate/scale here, not on the
		// render thread (EnsureContext runs there).
		_link = CADisplayLink.Create(OnDisplayLink);
		_fps = UIScreen.MainScreen.MaximumFramesPerSecond;
		_scale = (float)UIScreen.MainScreen.Scale;

		StartRenderThread();
	}

	internal void SetOwner(RootViewController owner) => _owner = owner;

	void IAppleUIKitRenderView.SetOwner(RootViewController owner) => SetOwner(owner);

	public void QueueRender()
	{
		if (_link is { } link)
		{
			link.Paused = false;
		}
	}

	private void StartRenderThread()
	{
		_renderThread = new Thread(() =>
		{
			var currentThread = NSThread.Current;
			currentThread.QualityOfService = NSQualityOfService.UserInteractive;
			currentThread.Name = "UnoSKWebGpuMetalViewRenderThread";

			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				_link.PreferredFrameRateRange = new CAFrameRateRange { Minimum = 30, Preferred = _fps, Maximum = _fps };
			}
			else
			{
#pragma warning disable CA1422 // Validate platform compatibility
				_link.PreferredFramesPerSecond = _fps;
#pragma warning restore CA1422
			}

			_link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
			NSRunLoop.Current.Run(); // blocks; the display link drives OnDisplayLink
		})
		{
			IsBackground = true,
			Name = "UnoSKWebGpuMetalViewRenderThread"
		};
		_renderThread.Start();
	}

	private void OnDisplayLink()
	{
		// Coalesce: render once per requested invalidation, then pause until the next QueueRender.
		if (_link is { } link)
		{
			link.Paused = true;
		}

		try
		{
			var size = MetalLayer.DrawableSize;
			if (size.Width < 1 || size.Height < 1)
			{
				return;
			}

			EnsureContext();
			_owner?.OnWebGpuFrameRequested(_context!);
		}
		catch (Exception ex)
		{
			this.Log().Error($"{nameof(UnoSKWebGpuMetalView)} render failed", ex);
		}
	}

	private void EnsureContext()
	{
		if (_context is not null)
		{
			return;
		}

		// The host references no WebGPU type — it hands the neutral Metal-layer window (+ DPI scale) to the
		// pluggable pipeline; the app-registered WebGPU provider builds the surface + device and mints the
		// (factory, renderer) pair.
		var layerHandle = (IntPtr)MetalLayer.Handle;
		var nativeWindow = new AppleMetalGraphicsNativeWindow(layerHandle, _scale);
		var init = global::Uno.UI.Composition.Drawing.GraphicsRegistry.Initialize(
			nativeWindow, new[] { global::Uno.UI.Composition.Drawing.GraphicsContextKind.WebGpu });
		_context = init.Context;

		if (!_rendererInstalled)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Renderer = init.Renderer;
			_rendererInstalled = true;
			this.Log().Info("Neutral graphics pipeline active: WebGpu context via the neutral pipeline (AppleUIKit).");
		}
	}

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		// Keep the drawable sized in pixels; a size change reconfigures the wgpu surface on the next frame.
		var scale = ContentScaleFactor;
		MetalLayer.DrawableSize = new CGSize(Bounds.Width * scale, Bounds.Height * scale);
		QueueRender();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_link.Invalidate();
			_context?.Dispose();
			_context = null;
		}
		base.Dispose(disposing);
	}
}
