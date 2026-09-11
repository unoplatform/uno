#nullable enable

namespace Windows.UI.Notifications.Internal;

internal static partial class ToastNotificationSchedulerBackendFactory
{
	static partial void CreatePlatform(ref IToastNotificationSchedulerBackend? backend)
		=> backend = new AppleToastNotificationSchedulerBackend();
}