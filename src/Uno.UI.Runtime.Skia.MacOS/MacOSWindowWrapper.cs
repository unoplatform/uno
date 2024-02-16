using Windows.Foundation;

using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase
{
	private readonly MacOSWindowNative _window;

	public MacOSWindowWrapper(MacOSWindowNative window)
	{
		_window = window;

		// FIXME: we hit this too late, we already have received the first resize :(
		window.Host.SizeChanged += OnHostSizeChanged;
	}

	public override object NativeWindow => _window;

	public MacOSWindowNative Native => _window;

	private void OnHostSizeChanged(object? sender, Windows.Foundation.Size size)
	{
		Bounds = new Rect(default, size);
		VisibleBounds = Bounds;
	}
}
