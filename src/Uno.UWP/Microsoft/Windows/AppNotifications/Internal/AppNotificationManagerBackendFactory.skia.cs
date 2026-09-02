#nullable enable

using Uno.Foundation.Extensibility;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static partial class AppNotificationManagerBackendFactory
{
	static partial void CreatePlatform(ref IAppNotificationManagerBackend? backend)
	{
		if (ApiExtensibility.CreateInstance<IAppNotificationManagerBackend>(typeof(AppNotificationManager), out var extension))
		{
			backend = extension;
		}
	}
}
