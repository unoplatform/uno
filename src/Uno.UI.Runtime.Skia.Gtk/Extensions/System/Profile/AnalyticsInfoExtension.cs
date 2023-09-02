using Windows.System.Profile.Internal;

namespace Uno.UI.Runtime.Skia.Gtk.System.Profile;

internal class AnalyticsInfoExtension : IAnalyticsInfoExtension
{
	public UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
