using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
namespace Uno.UI.Xaml.Controls;

public partial class NativeWindowWrapper : INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	private Size _previousWindowSize = new Size(-1, -1);

	internal static NativeWindowWrapper Instance => _instance.Value; // TODO: Temporary until proper multi-window support is added.

	public event EventHandler<Size> SizeChanged;
	public event EventHandler<CoreWindowActivationState> ActivationChanged;
	public event EventHandler<bool> VisibilityChanged;
	public event EventHandler Closed;
	public event EventHandler Shown;

	public bool Visible => throw new NotImplementedException();

	public void Activate()
	{
		// TODO: Bring window to the foreground?
	}

	internal void OnNativeClosed() => Closed?.Invoke(this, EventArgs.Empty);

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationChanged?.Invoke(this, state);

	internal void OnNativeVisibilityChanged(bool visible) => VisibilityChanged?.Invoke(this, visible);

	internal void RaiseNativeSizeChanged(double width, double height)
	{
		var windowSize = new Size(width, height);

		ApplicationView.GetForCurrentView()?.SetVisibleBounds(new Rect(Point.Zero, windowSize));

		if (_previousWindowSize != windowSize)
		{
			_previousWindowSize = windowSize;

			SizeChanged?.Invoke(this, windowSize);
		}
	}

	public void Show()
	{
		WindowManagerInterop.WindowActivate();
		OnNativeActivated(CoreWindowActivationState.CodeActivated);
		Shown?.Invoke(this, EventArgs.Empty);
	}
}
