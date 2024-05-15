using System.Globalization;
using Uno.Foundation.Extensibility;
using Uno.UI.Notifications;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSBadgeUpdaterExtension : IBadgeUpdaterExtension
{
	private static readonly MacOSBadgeUpdaterExtension _instance = new();

	private MacOSBadgeUpdaterExtension()
	{
	}

	public static void Register() => ApiExtensibility.Register(typeof(IBadgeUpdaterExtension), _ => _instance);

	public void SetBadge(int? value)
	{
		var s = string.Empty;
		if (value is not null)
		{
			s = value.Value.ToString(CultureInfo.CurrentUICulture.NumberFormat);
		}
		NativeUno.uno_application_set_badge(s);
	}
}
