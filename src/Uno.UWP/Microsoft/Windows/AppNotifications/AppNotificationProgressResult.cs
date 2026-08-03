using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public enum AppNotificationProgressResult
{
	Succeeded = 0,
	AppNotificationNotFound = 1,
	[ContractVersion(typeof(AppNotificationsContract), 2 * 0x10000u)]
	Unsupported = 2,
}
