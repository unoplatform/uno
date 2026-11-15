using System;
using System.Threading;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// WebGPU-backed render view for AppleUIKit (iOS/tvOS): a <c>UIView</c> hosting a <see cref="CAMetalLayer"/> that
/// drives a wgpu swapchain, drawn through the neutral
/// <c>CompositionTarget.OnNativePlatformFrameRequested</c> seam. Opt in by registering the WebGPU backend on the host builder; needs an Apple device build
/// with the wgpu-native iOS static lib linked via wgpu-native.targets.
/// </summary>
internal sealed partial class UnoWebGpuMetalView : UIView, IAppleUIKitRenderView
{
	private RootViewController? _owner;
	private readonly CADisplayLink _link;
	private readonly nint _fps;
	private readonly float _scale;
	private Thread? _renderThread;
	// A sublayer rather than the view's backing layer: wgpu configures it (drawable size, pixel format, timeouts) on
	// the render thread, and UIKit only allows a view's own layer to be modified on the main thread.
	private readonly CAMetalLayer _metalLayer = new();
	// The drawable size in pixels, as last laid out on the main thread; the render thread only reads it.
	private int _pixelWidth, _pixelHeight;

	// VoiceOver, Switch Control and XCTest read the accessibility elements through these; the first read builds them.
	[Export("accessibilityElements")]
	public NSObject[]? GetAccessibilityElements() => _owner?.GetAccessibilityElements();

	[Export("automationElements")]
	public NSObject[]? GetAutomationElements() => _owner?.GetAccessibilityElements();

	[Export("accessibilityElementCount")]
	public nint GetAccessibilityElementCount() => _owner?.GetAccessibilityElementCount() ?? 0;

	[Export("accessibilityElementAtIndex:")]
	public NSObject? GetAccessibilityElementAt(nint index) => _owner?.GetAccessibilityElementAt(index);

	[Export("indexOfAccessibilityElement:")]
	public nint GetIndexOfAccessibilityElement(NSObject? element)
		=> _owner?.GetIndexOfAccessibilityElement(element) ?? NSRange.NotFound;

	[Export("accessibilityHitTest:withEvent:")]
	public NSObject? AccessibilityHitTest(CGPoint point, UIEvent? uievent)
		=> _owner?.AccessibilityHitTest(point);

	public UnoWebGpuMetalView()
		: base(CGRect.Empty)
	{
		// Match the screen's pixel density so the wgpu drawable is full-resolution (drawableSize is in pixels).
		ContentScaleFactor = UIScreen.MainScreen.Scale;
		_metalLayer.ContentsScale = UIScreen.MainScreen.Scale;
		Layer.AddSublayer(_metalLayer);

		// UIKit APIs are UI-thread-checked: create the link and read the display rate/scale here, not on the render thread.
		_link = CADisplayLink.Create(OnDisplayLink);
		_fps = UIScreen.MainScreen.MaximumFramesPerSecond;
		_scale = (float)UIScreen.MainScreen.Scale;

		StartRenderThread();
	}

	internal void SetOwner(RootViewController owner) => _owner = owner;

	void IAppleUIKitRenderView.SetOwner(RootViewController owner) => SetOwner(owner);

	public void StopRender()
	{
		_stopped = true;

		if (_link is { } link)
		{
			link.Paused = true;
		}
	}

	public void QueueRender()
	{
		if (!_stopped && _link is { } link)
		{
			link.Paused = false;
		}
	}

	private volatile bool _stopped;

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
			if (Volatile.Read(ref _pixelWidth) < 1 || Volatile.Read(ref _pixelHeight) < 1)
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
		=> global::Uno.UI.Composition.WebGpu.WebGpuContext.CreateMetal((IntPtr)_metalLayer.Handle, _scale);

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		// Keep the layer over the whole view, without the implicit animation a sublayer's frame change gets. The
		// drawable size is wgpu's: a size change reconfigures the surface on the next frame.
		CATransaction.Begin();
		CATransaction.DisableActions = true;
		_metalLayer.Frame = Bounds;
		CATransaction.Commit();

		var scale = ContentScaleFactor;
		Volatile.Write(ref _pixelWidth, (int)(Bounds.Width * scale));
		Volatile.Write(ref _pixelHeight, (int)(Bounds.Height * scale));
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
