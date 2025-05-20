using Windows.System.Profile.Internal;

using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSAnalyticsInfoExtension : IAnalyticsInfoExtension
{
	private static readonly MacOSAnalyticsInfoExtension _instance = new();

	private MacOSAnalyticsInfoExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), _ => _instance);

	public UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
