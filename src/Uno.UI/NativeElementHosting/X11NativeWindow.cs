#if UNO_REFERENCE_API
using System;
namespace Uno.UI.NativeElementHosting;

public sealed class X11NativeWindow
{
	public IntPtr WindowId { get; }

	public X11NativeWindow(IntPtr windowId)
	{
		WindowId = windowId;
	}
}
#endif
