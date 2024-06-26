using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Graphics;
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

	private void DispatchDpiChanged() =>
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;

	internal void OnNativeClosed() => RaiseClosed();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void RaiseNativeSizeChanged(double width, double height)
	{
		var bounds = new Rect(default, new Size(width, height));

		Bounds = bounds;
		VisibleBounds = bounds;
	}

	protected override void ShowCore()
	{
		DispatchDpiChanged();
		WindowManagerInterop.WindowActivate();
	}

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

	public override void Move(PointInt32 position) => NativeMethods.MoveWindow(position.X, position.Y);

	public override void Resize(SizeInt32 size) => NativeMethods.ResizeWindow(size.Width, size.Height);
}
