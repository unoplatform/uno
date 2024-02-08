#nullable enable

using Windows.System.Profile.Internal;

using Uno.Foundation.Extensibility;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSAnalyticsInfoExtension : IAnalyticsInfoExtension
{
	public static MacOSAnalyticsInfoExtension Instance = new();

	private MacOSAnalyticsInfoExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IAnalyticsInfoExtension), o => Instance);

	public UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
