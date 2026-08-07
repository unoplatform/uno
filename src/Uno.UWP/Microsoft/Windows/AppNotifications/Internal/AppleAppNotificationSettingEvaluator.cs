#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal enum AppleAppNotificationAuthorizationStatus
{
	NotDetermined,
	Denied,
	Authorized,
	Provisional,
	Ephemeral,
}

internal static class AppleAppNotificationSettingEvaluator
{
	public static AppNotificationSetting Evaluate(AppleAppNotificationAuthorizationStatus status)
		=> status switch
		{
			AppleAppNotificationAuthorizationStatus.Authorized or
			AppleAppNotificationAuthorizationStatus.Provisional or
			AppleAppNotificationAuthorizationStatus.Ephemeral => AppNotificationSetting.Enabled,
			AppleAppNotificationAuthorizationStatus.Denied => AppNotificationSetting.DisabledForUser,
			_ => AppNotificationSetting.DisabledForApplication,
		};
}