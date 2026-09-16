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
/// WebGPU-backed render view for AppleUIKit (iOS/tvOS): a <c>UIView</c> whose backing
/// <see cref="CAMetalLayer"/> drives a wgpu swapchain, drawn through the neutral
/// <c>CompositionTarget.OnNativePlatformFrameRequested</c> seam. Opt in with UNO_WEBGPU; needs an Apple device build
/// with the wgpu-native iOS static lib linked via wgpu-native.targets.
/// </summary>
internal sealed partial class UnoWebGpuMetalView : UIView, IAppleUIKitRenderView
{
	private RootViewController? _owner;
	private readonly CADisplayLink _link;
	private readonly nint _fps;
	private readonly float _scale;
	private Thread? _renderThread;

	[Export("layerClass")]
	public static Class LayerClass() => new Class(typeof(CAMetalLayer));

	private CAMetalLayer MetalLayer => (CAMetalLayer)Layer;

	public UnoWebGpuMetalView()
		: base(CGRect.Empty)
	{
		// Match the screen's pixel density so the wgpu drawable is full-resolution (drawableSize is in pixels).
		ContentScaleFactor = UIScreen.MainScreen.Scale;
		MetalLayer.ContentsScale = UIScreen.MainScreen.Scale;

		// UIKit APIs are UI-thread-checked: create the link and read the display rate/scale here, not on the render thread.
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
			currentThread.Name = "UnoWebGpuMetalViewRenderThread";

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
			Name = "UnoWebGpuMetalViewRenderThread"
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

			_owner?.OnFrameRequested();
		}
		catch (Exception ex)
		{
			this.Log().Error($"{nameof(UnoWebGpuMetalView)} render failed", ex);
		}
	}

	/// <summary>
	/// Creates the WebGpu swapchain context over this view's <c>CAMetalLayer</c>.
	/// </summary>
	internal global::Uno.UI.Composition.Drawing.ISwapChain CreateGraphicsContext()
		=> global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateMetal((IntPtr)MetalLayer.Handle, _scale);

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
			// The negotiated context is owned/disposed by RootViewController.
		}
		base.Dispose(disposing);
	}
}
