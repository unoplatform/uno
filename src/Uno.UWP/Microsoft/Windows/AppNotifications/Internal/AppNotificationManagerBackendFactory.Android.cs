#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal static partial class AppNotificationManagerBackendFactory
{
	static partial void CreatePlatform(ref IAppNotificationManagerBackend? backend)
		=> backend = new AndroidAppNotificationManagerBackend();
}
