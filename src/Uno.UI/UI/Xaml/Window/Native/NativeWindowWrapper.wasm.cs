using System;
using System.Runtime.InteropServices.JavaScript;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using static __Uno.UI.Xaml.Controls.NativeWindowWrapper;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private readonly DisplayInformation _displayInformation;

	internal static NativeWindowWrapper Instance => _instance.Value;

	public NativeWindowWrapper()
	{
		_displayInformation = DisplayInformation.GetForCurrentViewSafe() ?? throw new InvalidOperationException("DisplayInformation must be available when the window is initialized");
		_displayInformation.DpiChanged += (s, e) => DispatchDpiChanged();
	}

	public override object NativeWindow => null;

	public override void Activate()
	{
	}

	public override void Close()
	{
	}

	private void DispatchDpiChanged()
	{
		this.Log().LogError("Setting RasterizationScale to {0}", _displayInformation.RawPixelsPerViewPixel);
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
	}

	internal void OnNativeClosed() => RaiseClosed();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void RaiseNativeSizeChanged(double width, double height)
	{
		var bounds = new Rect(default, new Size(width, height));

		Bounds = bounds;
		VisibleBounds = bounds;
	}

	protected override void ShowCore() => WindowManagerInterop.WindowActivate();

	private bool SetFullScreenMode(bool turnOn) => NativeMethods.SetFullScreenMode(turnOn);

	public override string Title
	{
		get => NativeMethods.GetWindowTitle();
		set => NativeMethods.SetWindowTitle(value);
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		SetFullScreenMode(true);
		return Disposable.Create(() => SetFullScreenMode(false));
	}
}
