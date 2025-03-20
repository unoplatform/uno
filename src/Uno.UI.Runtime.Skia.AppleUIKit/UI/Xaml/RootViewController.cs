using System;
using System.Globalization;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using SkiaSharp;
using UIKit;
using Uno.Helpers.Theming;
using Uno.UI.Controls;
using Uno.UI.Helpers;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;


#if __IOS__
using SkiaCanvas = SkiaSharp.Views.iOS.SKMetalView;
using SkiaEventArgs = SkiaSharp.Views.iOS.SKPaintMetalSurfaceEventArgs;
#else
using SkiaSharp.Views.tvOS;
using SkiaCanvas = SkiaSharp.Views.tvOS.SKCanvasView;
using SkiaEventArgs = SkiaSharp.Views.tvOS.SKPaintSurfaceEventArgs;
#endif

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class RootViewController : UINavigationController, IRotationAwareViewController, IAppleUIKitXamlRootHost
{
	private SkiaCanvas? _skCanvasView;
	private XamlRoot? _xamlRoot;
	private UIView? _textInputLayer;
	private UIView? _nativeOverlayLayer;
	private string? _lastSvgClipPath;

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
		_textInputLayer = new UIView();
		_skCanvasView = new SkiaCanvas();
#if !__TVOS__
		_skCanvasView.Paused = false;
		_skCanvasView.EnableSetNeedsDisplay = false;
		_skCanvasView.FramebufferOnly = false;
#endif
		_skCanvasView.Frame = View!.Bounds;
		_skCanvasView.AutoresizingMask = UIViewAutoresizing.All;
		_skCanvasView.PaintSurface += OnPaintSurface;
		View!.AddSubview(_textInputLayer);
		View!.AddSubview(_skCanvasView);

		var nativeOverlayLayer = new NativeOverlayLayer();
		nativeOverlayLayer.Frame = View!.Bounds;
		nativeOverlayLayer.AutoresizingMask = UIViewAutoresizing.All;
		nativeOverlayLayer.SubviewsChanged += NativeOverlayLayer_SubviewsChanged;
		_nativeOverlayLayer = nativeOverlayLayer;

		View!.AddSubview(_nativeOverlayLayer);

		// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

#if !__TVOS__
		// Dismiss on device rotation: this reproduces the windows behavior
		UIApplication.Notifications
			.ObserveDidChangeStatusBarOrientation((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));
#endif

		// Dismiss when the app is entering background
		UIApplication.Notifications
			.ObserveWillResignActive((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));
	}

	private void NativeOverlayLayer_SubviewsChanged(object? sender, EventArgs e) => _lastSvgClipPath = null; // Ensure the clip path is invalidated for next render.

	internal event Action? VisibleBoundsChanged;

	public void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	private void OnPaintSurface(object? sender, SkiaEventArgs e)
	{
		if (_xamlRoot?.VisualTree.RootElement is { } rootElement)
		{
			var surface = e.Surface;
			surface.Canvas.Clear(SKColors.Transparent);
			surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)_xamlRoot.RasterizationScale, (float)_xamlRoot.RasterizationScale));
			if (rootElement.Visual is { } rootVisual)
			{
				int width = (int)View!.Frame.Width;
				int height = (int)View!.Frame.Height;
				var path = SkiaRenderHelper.RenderRootVisualAndReturnNegativePath(width, height, rootVisual, surface.Canvas);
				if (path is { })
				{
					var svgPath = path.ToSvgPathData();
					if (svgPath != _lastSvgClipPath)
					{
						_lastSvgClipPath = svgPath;
						ClipBySvgPath(svgPath);
					}
				}
			}
		}
	}

	private void ClipBySvgPath(string svg)
	{
		if (svg != null)
		{
			var cgPath = new CGPath();
			var length = svg.Length;

			var scale = UIScreen.MainScreen.Scale;

			var subviews = _nativeOverlayLayer!.Subviews;
			for (int i = 0; i < subviews.Length; i++)
			{
				var view = subviews[i];
				var vx = view.Frame.X;
				var vy = view.Frame.Y;

				for (int index = 0; index < length;)
				{
					nfloat x, y, x2, y2;
					char op = svg[index];
					switch (op)
					{
						case 'M':
							index++; // skip M
							x = ReadNextSvgCoord(svg, ref index, length);
							index++; // skip separator
							y = ReadNextSvgCoord(svg, ref index, length);

							x = (x / scale - vx);
							y = (y / scale - vy);
							cgPath.MoveToPoint(x, y);
							break;

						case 'Q':
							index++; // skip Z
							x = ReadNextSvgCoord(svg, ref index, length);
							index++; // skip separator
							y = ReadNextSvgCoord(svg, ref index, length);
							index++; // skip separator
							x2 = ReadNextSvgCoord(svg, ref index, length);
							index++; // skip separator
							y2 = ReadNextSvgCoord(svg, ref index, length);
							// there might not be a separator (not required before the next op)
							x = (x / scale - vx);
							y = (y / scale - vy);
							x2 = (x2 / scale - vx);
							y2 = (y2 / scale - vy);
							cgPath.AddQuadCurveToPoint(default, x, y, x2, y2);
							break;

						case 'L':
							index++; // skip L
							x = ReadNextSvgCoord(svg, ref index, length);
							index++; // skip separator
							y = ReadNextSvgCoord(svg, ref index, length);

							x = (x / scale - vx);
							y = (y / scale - vy);
							cgPath.AddLineToPoint(x, y);
							break;

						case 'Z':
							index++; // skip Z
							cgPath.CloseSubpath();
							break;

						default:
							index++; // skip unknown op
							break;
					}
				}

				var mask = view.Layer.Mask as CAShapeLayer;
				if (mask == null)
				{
					mask = new CAShapeLayer();
					view.Layer.Mask = mask;
				}

				mask.FillColor = UIColor.Blue.CGColor; // anything but clear color
				mask.Path = cgPath;
				mask.FillRule = CAShapeLayer.FillRuleEvenOdd;
			}
		}
	}

	private float ReadNextSvgCoord(string svg, ref int position, long length)
	{
		float result = float.NaN;
		if (position >= length)
		{
			return result;
		}

		if (svg[position] == ' ')
		{
			position++;
		}

		var endPos = position;
		while (endPos < svg.Length && (char.IsDigit(svg[endPos]) || svg[endPos] == '.' || svg[endPos] == '-'))
		{
			endPos++;
		}
		var reading = svg.Substring(position, endPos - position).Trim();

		var coord = float.Parse(reading, CultureInfo.InvariantCulture);

		position = endPos;
		return coord;
	}

	public void InvalidateRender() => _skCanvasView?.LayoutSubviews();

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
