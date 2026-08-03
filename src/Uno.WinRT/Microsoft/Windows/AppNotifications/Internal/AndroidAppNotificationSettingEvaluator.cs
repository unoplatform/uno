#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AndroidAppNotificationSettingEvaluator
{
	public static bool IsSupported(int apiLevel) => apiLevel >= 23;

	public static AppNotificationSetting Evaluate(bool requiresRuntimePermission, bool declaredInManifest, bool permissionGranted, bool notificationsEnabled)
	{
		if (requiresRuntimePermission && !declaredInManifest)
		{
			return AppNotificationSetting.DisabledByManifest;
		}
		if (requiresRuntimePermission && !permissionGranted)
		{
			return AppNotificationSetting.DisabledForApplication;
		}
		return notificationsEnabled
			? AppNotificationSetting.Enabled
			: AppNotificationSetting.DisabledForApplication;
	}
}
