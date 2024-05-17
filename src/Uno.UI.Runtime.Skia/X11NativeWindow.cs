namespace Uno.UI.Runtime.Skia;

public class X11NativeWindow
{
	public IntPtr WindowId { get; private set; }

	public X11NativeWindow(IntPtr windowId)
	{
		WindowId = windowId;
	}
}
