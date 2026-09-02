using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[ContractVersion(typeof(AppNotificationsContract), 1 * 0x10000u)]
public enum AppNotificationSetting
{
	Enabled = 0,
	DisabledForApplication = 1,
	DisabledForUser = 2,
	DisabledByGroupPolicy = 3,
	DisabledByManifest = 4,
	[ContractVersion(typeof(AppNotificationsContract), 2 * 0x10000u)]
	Unsupported = 5,
}
