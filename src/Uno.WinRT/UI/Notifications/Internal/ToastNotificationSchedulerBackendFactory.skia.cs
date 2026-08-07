#nullable enable

using Uno.Foundation.Extensibility;

namespace Windows.UI.Notifications.Internal;

internal static partial class ToastNotificationSchedulerBackendFactory
{
	static partial void CreatePlatform(ref IToastNotificationSchedulerBackend? backend)
	{
		if (ApiExtensibility.CreateInstance<IToastNotificationSchedulerBackend>(typeof(ToastNotifier), out var extension))
		{
			backend = extension;
		}
	}
}