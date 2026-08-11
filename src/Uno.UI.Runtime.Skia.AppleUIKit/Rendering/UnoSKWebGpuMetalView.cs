using System;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Composition.WebGpu;
using Uno.WebGpu.Native;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// EXPERIMENTAL WebGPU-backed render view for AppleUIKit (iOS/tvOS), mirroring the Android <c>UnoSKWebGpuView</c>:
/// a <c>UIView</c> whose backing <see cref="CAMetalLayer"/> drives a wgpu swapchain through the shared
/// <see cref="WebGpuSwapChainContext"/> (CreateMetalSurface). Present is wgpuSurfacePresent, and the scene is drawn
/// through the neutral <c>CompositionTarget.OnNativePlatformFrameRequested</c> seam + <see cref="WebGpuRenderer"/>.
/// Opt in with UNO_WEBGPU. Not toolchain-validated on Linux CI — needs an Apple device build (and the wgpu-native
/// iOS static lib linked via wgpu-native.targets, so <c>DllImport("webgpu")</c> resolves to the main program).
/// </summary>
internal sealed class UnoSKWebGpuMetalView : UIView, IAppleUIKitRenderView
{
	private RootViewController? _owner;
	private WebGpuSwapChainContext? _context;
	private CADisplayLink? _link;
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

			_link = CADisplayLink.Create(OnDisplayLink);
			var fps = UIScreen.MainScreen.MaximumFramesPerSecond;
			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				_link.PreferredFrameRateRange = new CAFrameRateRange { Minimum = 30, Preferred = fps, Maximum = fps };
			}
			else
			{
#pragma warning disable CA1422 // Validate platform compatibility
				_link.PreferredFramesPerSecond = fps;
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

		var layerHandle = (IntPtr)MetalLayer.Handle;
		_context = new WebGpuSwapChainContext(
			WGPUTextureFormat.BGRA8Unorm,
			inst => WebGpuSwapChainContext.CreateMetalSurface(inst, layerHandle));

		if (!_rendererInstalled)
		{
			Microsoft.UI.Xaml.Media.CompositionTarget.Renderer = new WebGpuRenderer(_context.Device);
			_rendererInstalled = true;
			this.Log().Info("Neutral graphics pipeline active: WebGpu context via WebGpuRenderer (AppleUIKit).");
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
			_link?.Invalidate();
			_link = null;
			_context?.Dispose();
			_context = null;
		}
		base.Dispose(disposing);
	}
}
