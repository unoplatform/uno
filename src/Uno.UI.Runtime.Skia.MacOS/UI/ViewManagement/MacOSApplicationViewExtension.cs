using Windows.Foundation;
using Windows.UI.ViewManagement;

using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSApplicationViewExtension : IApplicationViewExtension
{
	private static readonly MacOSApplicationViewExtension _instance = new();

	private MacOSApplicationViewExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IApplicationViewExtension), _ => _instance);

	public bool TryResizeView(Size size)
	{
		var main = NativeUno.uno_app_get_main_window();
		return NativeUno.uno_window_resize(main, size.Width, size.Height);
	}
}
