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
				"denied" => AppNotificationSetting.DisabledForApplication,
				_ => AppNotificationSetting.DisabledForApplication,
			};
}

internal static class WebAssemblyAppNotificationCapabilities
{
	public static bool SupportsProgressUpdates => false;

	/// <summary>
	/// Action buttons are only rendered by <c>ServiceWorkerRegistration.showNotification()</c>;
	/// a document-scoped <c>new Notification(...)</c> ignores the <c>actions</c> option.
	/// </summary>
	public static bool SupportsActions(bool useServiceWorker) => useServiceWorker;
}