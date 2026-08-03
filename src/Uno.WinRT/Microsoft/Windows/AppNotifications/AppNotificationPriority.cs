using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public enum AppNotificationPriority
{
	Default = 0,
	High = 1,
}
