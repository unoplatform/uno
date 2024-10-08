using System.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
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

	private void OnHostSizeChanged(Size size)
	{
		Bounds = new Rect(default, size);
		VisibleBounds = Bounds;
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			e.Cancel = true;
		}

		// All prerequisites passed, can safely close.
		e.Cancel = false;
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
