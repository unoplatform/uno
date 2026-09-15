#nullable enable

using Android.OS;

namespace Windows.UI.Notifications.Internal;

internal static partial class ToastNotificationSchedulerBackendFactory
{
	static partial void CreatePlatform(ref IToastNotificationSchedulerBackend? backend)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
		{
			backend = new AndroidToastNotificationSchedulerBackend();
		}
	}
}
