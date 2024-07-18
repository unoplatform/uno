using System;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using SkiaSharp;
using SkiaSharp.Views.iOS;
using UIKit;
using Uno.Helpers.Theming;
using Uno.UI.Controls;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class RootViewController : UINavigationController, IRotationAwareViewController, IAppleUIKitXamlRootHost
{
	private SKCanvasView? _skCanvasView;
	private XamlRoot? _xamlRoot;

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
		_skCanvasView = new SKCanvasView();
		_skCanvasView.BackgroundColor = UIColor.Red;
		_skCanvasView.Frame = View!.Bounds;
		_skCanvasView.AutoresizingMask = UIViewAutoresizing.All;
		_skCanvasView.PaintSurface += OnPaintSurface;
		View!.AddSubview(_skCanvasView);

		// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

		// Dismiss on device rotation: this reproduces the windows behavior
		UIApplication.Notifications
			.ObserveDidChangeStatusBarOrientation((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));

		// Dismiss when the app is entering background
		UIApplication.Notifications
			.ObserveWillResignActive((sender, args) =>
				VisualTreeHelper.CloseLightDismissPopups(WinUICoreServices.Instance.ContentRootCoordinator!.CoreWindowContentRoot!.XamlRoot));

		// iOS 17+ only
		if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
		{
			((IUITraitChangeObservable)this).RegisterForTraitChanges<UITraitUserInterfaceStyle>((env, traits) => SystemThemeHelper.RefreshSystemTheme());
		}
	}

	internal event Action? VisibleBoundsChanged;

	public void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	public SKColor BackgroundColor { get; set; } = SKColors.White;

	private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
	{
		if (_xamlRoot?.VisualTree.RootElement is { } rootElement)
		{
			var surface = e.Surface;
			surface.Canvas.Clear(BackgroundColor);
			surface.Canvas.SetMatrix(SKMatrix.CreateScale((float)_xamlRoot.RasterizationScale, (float)_xamlRoot.RasterizationScale));
			if (rootElement.Visual is { } rootVisual)
			{
				rootVisual.Compositor.RenderRootVisual(surface, rootVisual, null);
			}
		}
	}

	public void InvalidateRender() => _skCanvasView?.LayoutSubviews();

	public UIElement? RootElement => _xamlRoot?.VisualTree.RootElement;


	// This will handle when the status bar is showed / hidden by the system on iPhones
	public override void ViewSafeAreaInsetsDidChange()
	{
		base.ViewSafeAreaInsetsDidChange();
		VisibleBoundsChanged?.Invoke();
	}

	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations()
	{
		return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
	}

	public override void MotionEnded(UIEventSubtype motion, UIEvent? evt)
	{
		if (motion == UIEventSubtype.MotionShake)
		{
			Accelerometer.HandleShake();
		}
		base.MotionEnded(motion, evt);
	}

	public bool CanAutorotate { get; set; } = true;

#pragma warning disable CA1422 // Validate platform compatibility
	public override bool ShouldAutorotate() => CanAutorotate && base.ShouldAutorotate();

	public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
	{
		base.TraitCollectionDidChange(previousTraitCollection);
		SystemThemeHelper.RefreshSystemTheme();
	}
#pragma warning restore CA1422 // Validate platform compatibility
}
