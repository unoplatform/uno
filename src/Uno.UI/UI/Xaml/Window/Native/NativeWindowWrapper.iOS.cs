using System;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private Uno.UI.Controls.Window _nativeWindow;

	private RootViewController _mainController;
	private NSObject _orientationRegistration;

	internal static NativeWindowWrapper Instance => _instance.Value;

	public NativeWindowWrapper()
	{
		_nativeWindow = new Uno.UI.Controls.Window();

		_mainController = Windows.UI.Xaml.Window.ViewControllerGenerator?.Invoke() ?? new RootViewController();
		_mainController.View.BackgroundColor = UIColor.Clear;
		_mainController.NavigationBarHidden = true;

		ObserveOrientationAndSize();

#if __MACCATALYST__
		_nativeWindow.SetOwner(CoreWindow.IShouldntUseGetForCurrentThread());
#endif
	}

	public override void Activate() { } //TODO:MZ: Handle activation

	protected override void ShowCore()
	{
		_nativeWindow.RootViewController = _mainController;
		_nativeWindow.MakeKeyAndVisible();
		Visible = true;
	}

	internal Uno.UI.Controls.Window NativeWindow => _nativeWindow;

	internal RootViewController MainController => _mainController;

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosed(); // TODO: Handle closing when multiwindow #13847

	internal void RaiseNativeSizeChanged()
	{
		var newWindowSize = GetWindowSize();

		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	private void ObserveOrientationAndSize()
	{
		_orientationRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarOrientation(
				(sender, args) => RaiseNativeSizeChanged()
			);

		_orientationRegistration = UIApplication
			.Notifications
			.ObserveDidChangeStatusBarFrame(
				(sender, args) => RaiseNativeSizeChanged()
			);

		_nativeWindow.FrameChanged +=
			() => RaiseNativeSizeChanged();

		_mainController.VisibleBoundsChanged +=
			() => RaiseNativeSizeChanged();

		var statusBar = StatusBar.GetForCurrentView();
		statusBar.Showing += (o, e) => RaiseNativeSizeChanged();
		statusBar.Hiding += (o, e) => RaiseNativeSizeChanged();

		RaiseNativeSizeChanged();
	}

	internal Size GetWindowSize()
	{
		var nativeFrame = NativeWindow?.Frame ?? CGRect.Empty;

		return new Size(nativeFrame.Width, nativeFrame.Height);
	}
}
