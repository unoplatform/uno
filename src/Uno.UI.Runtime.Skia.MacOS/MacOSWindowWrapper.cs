using System.ComponentModel;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core.Preview;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase
{
	private readonly MacOSWindowNative _window;

	public MacOSWindowWrapper(MacOSWindowNative nativeWindow, Window window, XamlRoot xamlRoot, Size initialSize) : base(window, xamlRoot)
	{
		_window = nativeWindow;

		nativeWindow.Host.Closing += OnWindowClosing;
		nativeWindow.Host.RasterizationScaleChanged += Host_RasterizationScaleChanged;
		nativeWindow.Host.SizeChanged += (_, s) => OnHostSizeChanged(s);
		OnHostSizeChanged(initialSize);
		nativeWindow.Host.PositionChanged += (_, s) => OnHostPositionChanged(s.X, s.Y);
		// the initial event occurred before the managed side was ready to handle it
		NativeUno.uno_window_get_position(nativeWindow.Handle, out var x, out var y);
		OnHostPositionChanged(x, y);

		RasterizationScale = (float)_window.Host.RasterizationScale;
	}

	private void Host_RasterizationScaleChanged(object? sender, EventArgs args)
	{
		RasterizationScale = (float)_window.Host.RasterizationScale;
	}

	public override object NativeWindow => _window;

	public override string Title
	{
		get => NativeUno.uno_window_get_title(_window.Handle);
		set => NativeUno.uno_window_set_title(_window.Handle, value);
	}

	internal protected override void Activate()
	{
		NativeUno.uno_window_activate(_window.Handle);
	}

	protected override void CloseCore()
	{
		NativeUno.uno_window_close(_window.Handle);
	}

	public override void Move(PointInt32 position)
	{
		// user input in physical pixels transformed into logical pixels
		var x = position.X / RasterizationScale;
		var y = position.Y / RasterizationScale;
		NativeUno.uno_window_move(_window.Handle, x, y);
	}

	public override void Resize(SizeInt32 size)
	{
		// user input in physical pixels transformed into logical pixels
		var w = size.Width / RasterizationScale;
		var h = size.Height / RasterizationScale;
		NativeUno.uno_window_resize(_window.Handle, w, h);
	}

	private void OnHostPositionChanged(double x, double y)
	{
		// in physical pixels
		var sx = (int)(x * RasterizationScale);
		var sy = (int)(y * RasterizationScale);
		Position = new PointInt32(sx, sy);
	}

	private void OnHostSizeChanged(Size size)
	{
		// in logical pixels
		Bounds = new Rect(default, size);
		VisibleBounds = Bounds;
		// in physical pixels
		int w = (int)(size.Width * RasterizationScale);
		int h = (int)(size.Height * RasterizationScale);
		Size = new SizeInt32(w, h);
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		e.Cancel = closingArgs.Cancel;
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		NativeUno.uno_window_enter_full_screen(_window.Handle);
		return Disposable.Create(() => NativeUno.uno_window_exit_full_screen(_window.Handle));
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new MacOSNativeOverlappedPresenter(_window));
		return Disposable.Create(() => presenter.SetNative(null));
	}
}
