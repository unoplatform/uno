#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class WebAssemblyAppNotificationSettingEvaluator
{
	public static bool IsSupported(bool isSecureContext, bool hasNotificationsApi)
		=> isSecureContext && hasNotificationsApi;

	public static AppNotificationSetting Evaluate(bool isSupported, string? permission)
		=> !isSupported
			? AppNotificationSetting.Unsupported
			: permission switch
			{
				"granted" => AppNotificationSetting.Enabled,
				"denied" => AppNotificationSetting.DisabledForUser,
				_ => AppNotificationSetting.DisabledForApplication,
			};
}