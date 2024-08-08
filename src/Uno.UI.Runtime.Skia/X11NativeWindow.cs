namespace Uno.UI.Runtime.Skia;

public sealed class X11NativeWindow
{
	public IntPtr WindowId { get; }

	public X11NativeWindow(IntPtr windowId)
	{
		WindowId = windowId;
	}
}
