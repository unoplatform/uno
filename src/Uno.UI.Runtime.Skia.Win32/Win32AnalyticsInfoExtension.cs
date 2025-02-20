using Windows.System.Profile.Internal;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32AnalyticsInfoExtension : IAnalyticsInfoExtension
{
	public static Win32AnalyticsInfoExtension Instance { get; } = new();

	private Win32AnalyticsInfoExtension()
	{
	}

	public UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
