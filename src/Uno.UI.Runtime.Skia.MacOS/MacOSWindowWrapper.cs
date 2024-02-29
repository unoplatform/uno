using System.ComponentModel;

using Windows.Foundation;
using Windows.UI.Core.Preview;

using Uno.UI.Xaml.Controls;

using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase
{
	private readonly MacOSWindowNative _window;

	public MacOSWindowWrapper(MacOSWindowNative window)
	{
		_window = window;

		window.Host.SizeChanged += OnHostSizeChanged;
		window.Host.Closing += OnWindowClosing;
		window.Host.Closed += OnWindowClosed;
	}

	public override object NativeWindow => _window;

	private void OnHostSizeChanged(object? sender, Size size)
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

		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				e.Cancel = true;
				return;
			}
		}

		// All prerequisites passed, can safely close.
		e.Cancel = false;
	}

	private void OnWindowClosed(object? sender, EventArgs e) => RaiseClosed();
}
