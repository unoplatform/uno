using System;
using CoreGraphics;
using Foundation;
using UIKit;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Controls;

internal class NativeWindowWrapper : INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private Uno.UI.Controls.Window _nativeWindow;

	private RootViewController _mainController;
	private NSObject _orientationRegistration;
	private Size _previousWindowSize = new Size(-1, -1);

	internal static NativeWindowWrapper Instance => _instance.Value;

	public NativeWindowWrapper()
	{
		_nativeWindow = new Uno.UI.Controls.Window();

		_mainController = Windows.UI.Xaml.Window.ViewControllerGenerator?.Invoke() ?? new RootViewController();
		_mainController.View.BackgroundColor = UIColor.Clear;
		_mainController.NavigationBarHidden = true;

		ObserveOrientationAndSize();

#if __MACCATALYST__
		_nativeWindow.SetOwner(IShouldntUseCoreWindow);
#endif
	}

	public bool Visible { get; private set; }

	public event EventHandler<Size> SizeChanged;
	public event EventHandler<CoreWindowActivationState> ActivationChanged;
	public event EventHandler<bool> VisibilityChanged;
	public event EventHandler Closed;
	public event EventHandler Shown;

	public void Activate() { } //TODO:MZ: Handle activation

	public void Show()
	{
		_nativeWindow.RootViewController = _mainController;
		_nativeWindow.MakeKeyAndVisible();
		Visible = true;
		Shown?.Invoke(this, EventArgs.Empty);
	}

	internal Uno.UI.Controls.Window NativeWindow => _nativeWindow;

	internal RootViewController MainController => _mainController;

	internal void OnNativeVisibilityChanged(bool visible)
	{
		Visible = visible;
		VisibilityChanged?.Invoke(this, visible);
	}

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationChanged?.Invoke(this, state);

	internal void OnNativeClosed() => Closed?.Invoke(this, EventArgs.Empty); //TODO:MZ: Handle closing

	internal void RaiseNativeSizeChanged()
	{
		var newWindowSize = GetWindowSize();

		ApplicationView.GetForCurrentView()?.SetVisibleBounds(_nativeWindow, newWindowSize);

		if (_previousWindowSize != newWindowSize)
		{
			_previousWindowSize = newWindowSize;

			SizeChanged?.Invoke(this, newWindowSize);
		}
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
#if __IOS__
		var nativeFrame = NativeWindow?.Frame ?? CGRect.Empty;

		return new Size(nativeFrame.Width, nativeFrame.Height);
#else //TODO:MZ: Move to macOS file
		var applicationFrameSize = NSScreen.MainScreen.VisibleFrame;
		return new CGSize(applicationFrameSize.Width, applicationFrameSize.Height);
#endif
	}
}
