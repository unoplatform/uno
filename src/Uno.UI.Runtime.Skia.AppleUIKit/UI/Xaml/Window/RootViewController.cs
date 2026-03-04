using System;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using SkiaSharp;
using UIKit;
using Uno.Helpers.Theming;
using Uno.UI.Helpers;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Uno.UI.Dispatching;
using System.Threading;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using SkiaCanvas = Uno.UI.Runtime.Skia.AppleUIKit.UnoSKMetalView;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class RootViewController : UINavigationController, IAppleUIKitXamlRootHost
{
	private SkiaCanvas? _skCanvasView;
	private XamlRoot? _xamlRoot;
	private UIView? _textInputLayer;
	private UIView? _topViewLayer;
	private UIView? _nativeOverlayLayer;

	/// <summary>
	/// Canvas reference used by <see cref="_getCanvasDelegate"/> to avoid
	/// per-frame closure allocations in <see cref="OnRenderFrameRequested"/>.
	/// Set on the render thread before the delegate is invoked.
	/// </summary>
	private SKCanvas? _currentCanvas;

	/// <summary>
	/// Cached delegate that returns <see cref="_currentCanvas"/>. Created once
	/// to eliminate the per-frame lambda closure + delegate allocation.
	/// </summary>
	private Func<Size, SKCanvas>? _getCanvasDelegate;

	/// <summary>
	/// Set when native overlay subviews change to force a clip path update
	/// on the next frame regardless of the change detection heuristic.
	/// </summary>
	private bool _clipPathDirty;

	/// <summary>
	/// Point count of the last native clipping path applied, used for
	/// change detection. -1 means no clipping has been applied yet.
	/// </summary>
	private int _lastClipPathPointCount = -1;

	/// <summary>
	/// Bounds of the last native clipping path applied, used together
	/// with <see cref="_lastClipPathPointCount"/> for change detection.
	/// </summary>
	private SKRect _lastClipPathBounds;

	public RootViewController()
	{
		Initialize();
	}

	public RootViewController(UIViewController rootViewController)
		: base(rootViewController)
	{
		Initialize();
	}

	public RootViewController(NSObjectFlag t)
		: base(t)
	{
		Initialize();
	}

	public RootViewController(NSCoder coder)
		: base(coder)
	{
		Initialize();
	}

	public RootViewController(NativeHandle handle)
		: base(handle)
	{
		Initialize();
	}

	public void Initialize()
	{
		var view = View!;

		_textInputLayer = new UIView();
		view.AddSubview(_textInputLayer);

		_skCanvasView = new SkiaCanvas();
		_skCanvasView.SetOwner(this);
		_skCanvasView.Frame = view.Bounds;
		_skCanvasView.AutoresizingMask = UIViewAutoresizing.All;
		view.AddSubview(_skCanvasView);

		_topViewLayer = new TopViewLayer();
		_topViewLayer.Frame = view.Bounds;
		_topViewLayer.AutoresizingMask = UIViewAutoresizing.All;
		var nativeOverlayLayer = new NativeOverlayLayer();
		nativeOverlayLayer.Frame = view.Bounds;
		nativeOverlayLayer.AutoresizingMask = UIViewAutoresizing.All;
		nativeOverlayLayer.SubviewsChanged += NativeOverlayLayer_SubviewsChanged;
		_nativeOverlayLayer = nativeOverlayLayer;
		_topViewLayer.AddSubview(_nativeOverlayLayer);
		view.AddSubview(_topViewLayer);

		// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

#if !__TVOS__
		// Dismiss on device rotation: this reproduces the windows behavior
		UIApplication.Notifications
			.ObserveDidChangeStatusBarOrientation(DismissPopups);
#endif

		// Dismiss when the app is entering background
		UIApplication.Notifications
			.ObserveWillResignActive(DismissPopups);
	}

	private void DismissPopups(object? sender, object? args)
	{
		if (_xamlRoot is not null)
		{
			VisualTreeHelper.CloseLightDismissPopups(_xamlRoot);
		}
	}

	private void NativeOverlayLayer_SubviewsChanged(object? sender, EventArgs e) => _clipPathDirty = true; // Ensure the clip path is invalidated for next render.

	internal event Action? VisibleBoundsChanged;

	public void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	internal void OnRenderFrameRequested(SKCanvas canvas)
	{
		// Cache the delegate to avoid a closure + delegate allocation on every
		// frame. The delegate reads _currentCanvas which is set just below.
		_currentCanvas = canvas;
		_getCanvasDelegate ??= _ => _currentCanvas!;

		var clipPath = (RootElement?.Visual.CompositionTarget as CompositionTarget)?.OnNativePlatformFrameRequested(canvas, _getCanvasDelegate);

		if (clipPath is not null)
		{
			UpdateNativeClipping(clipPath);
		}
	}

	private void UpdateNativeClipping(SKPath path)
	{
		var pointCount = path.IsEmpty ? 0 : path.PointCount;
		var bounds = path.IsEmpty ? SKRect.Empty : path.Bounds;

		// Quick change detection using point count + bounds.
		// The _clipPathDirty flag is set when native overlay subviews change,
		// forcing a re-conversion regardless of the heuristic.
		if (!_clipPathDirty && pointCount == _lastClipPathPointCount && bounds == _lastClipPathBounds)
		{
			return;
		}

		_clipPathDirty = false;
		_lastClipPathPointCount = pointCount;
		_lastClipPathBounds = bounds;

		if (path.IsEmpty)
		{
			NativeDispatcher.Main.Enqueue(ClearNativeClipping);
		}
		else
		{
			var cgPath = ConvertSKPathToCGPath(path);
			NativeDispatcher.Main.Enqueue(() => ApplyNativeClipping(cgPath));
		}
	}

	/// <summary>
	/// Converts an <see cref="SKPath"/> directly to a <see cref="CGPath"/> by
	/// iterating verbs and points, avoiding the intermediate SVG string
	/// serialization/parsing and its associated allocations.
	/// </summary>
	private CGPath ConvertSKPathToCGPath(SKPath skPath)
	{
		var scale = (nfloat)UIScreen.MainScreen.Scale;
		var view = _nativeOverlayLayer!;
		var vx = view.Frame.X;
		var vy = view.Frame.Y;

		var cgPath = new CGPath();
		var iter = skPath.CreateIterator(forceClose: false);

		Span<SKPoint> points = stackalloc SKPoint[4];
		SKPathVerb verb;

		while ((verb = iter.Next(points)) != SKPathVerb.Done)
		{
			switch (verb)
			{
				case SKPathVerb.Move:
					cgPath.MoveToPoint(
						points[0].X / scale - vx,
						points[0].Y / scale - vy);
					break;

				case SKPathVerb.Line:
					cgPath.AddLineToPoint(
						points[1].X / scale - vx,
						points[1].Y / scale - vy);
					break;

				case SKPathVerb.Quad:
					cgPath.AddQuadCurveToPoint(
						points[1].X / scale - vx,
						points[1].Y / scale - vy,
						points[2].X / scale - vx,
						points[2].Y / scale - vy);
					break;

				case SKPathVerb.Cubic:
					cgPath.AddCurveToPoint(
						points[1].X / scale - vx,
						points[1].Y / scale - vy,
						points[2].X / scale - vx,
						points[2].Y / scale - vy,
						points[3].X / scale - vx,
						points[3].Y / scale - vy);
					break;

				case SKPathVerb.Conic:
					// Approximate conic sections with quadratic curves.
					// CGPath has no native conic support.
					var conicWeight = iter.ConicWeight();
					var conicPoints = new SKPoint[5];
					var quadCount = SKPath.ConvertConicToQuads(
						points[0], points[1], points[2],
						conicWeight, conicPoints, 1);
					for (int q = 0; q < quadCount; q++)
					{
						var qi = q * 2;
						cgPath.AddQuadCurveToPoint(
							conicPoints[qi + 1].X / scale - vx,
							conicPoints[qi + 1].Y / scale - vy,
							conicPoints[qi + 2].X / scale - vx,
							conicPoints[qi + 2].Y / scale - vy);
					}
					break;

				case SKPathVerb.Close:
					cgPath.CloseSubpath();
					break;
			}
		}

		return cgPath;
	}

	private void ClearNativeClipping()
	{
		if (_nativeOverlayLayer is { } view)
		{
			// If the path is empty, we need to clear the mask of the native overlay layer
			// to avoid showing the previous clip.
			if (view.Layer.Mask is CAShapeLayer mask)
			{
				mask.Path = null;
				mask.FillColor = UIColor.Clear.CGColor;
			}
		}
	}

	private void ApplyNativeClipping(CGPath cgPath)
	{
		if (_nativeOverlayLayer is { } view)
		{
			if (view.Layer.Mask is not CAShapeLayer mask)
			{
				mask = new CAShapeLayer();
				view.Layer.Mask = mask;
			}

			mask.FillColor = UIColor.Blue.CGColor; // anything but clear color
			mask.Path = cgPath;
			mask.FillRule = CAShapeLayer.FillRuleEvenOdd;
		}
	}

	public void InvalidateRender()
	{
		_skCanvasView?.QueueRender();
	}

	public UIElement? RootElement => _xamlRoot?.VisualTree.RootElement;

	public UIView TextInputLayer => _textInputLayer!;

	// This will handle when the status bar is showed / hidden by the system on iPhones
	public override void ViewSafeAreaInsetsDidChange()
	{
		base.ViewSafeAreaInsetsDidChange();
		VisibleBoundsChanged?.Invoke();
	}

#if !__TVOS__
	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}
#endif

	public override void MotionEnded(UIEventSubtype motion, UIEvent? evt)
	{
#if !__TVOS__
		if (motion == UIEventSubtype.MotionShake)
		{
			Accelerometer.HandleShake();
		}
#endif
		base.MotionEnded(motion, evt);
	}

	public bool CanAutorotate { get; set; } = true;

	public UIView? NativeOverlayLayer => _nativeOverlayLayer;

#pragma warning disable CA1422 // Validate platform compatibility
#if !__TVOS__
	public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();
#endif

	public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
	{
		base.TraitCollectionDidChange(previousTraitCollection);
		SystemThemeHelper.RefreshSystemTheme();
	}
#pragma warning restore CA1422 // Validate platform compatibility
}
