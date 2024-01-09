#nullable enable

using Windows.Foundation;
using Windows.UI.ViewManagement;

using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSApplicationViewExtension : IApplicationViewExtension
{
	public static MacOSApplicationViewExtension Instance = new();

	private MacOSApplicationViewExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IApplicationViewExtension), o => Instance);

	public void ExitFullScreenMode() => NativeUno.uno_application_exit_fullscreen();

	public bool TryEnterFullScreenMode() => NativeUno.uno_application_enter_fullscreen();

	public bool TryResizeView(Size size)
	{
		var main = NativeUno.uno_app_get_main_window();
		return NativeUno.uno_window_resize(main, size.Width, size.Height);
	}
}
