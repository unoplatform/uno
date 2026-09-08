#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal static partial class AppNotificationManagerBackendFactory
{
	public static IAppNotificationManagerBackend? Create()
	{
		IAppNotificationManagerBackend? backend = null;
		CreatePlatform(ref backend);
		return backend;
	}

	static partial void CreatePlatform(ref IAppNotificationManagerBackend? backend);
}
