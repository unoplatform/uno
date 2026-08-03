#nullable enable

using System;
using Microsoft.Windows.AppNotifications;

namespace Windows.UI.Notifications;

public partial class ToastNotifier
{
	private readonly AppNotificationManager _manager;

	internal ToastNotifier()
		: this(AppNotificationManager.Default)
	{
	}

	internal ToastNotifier(AppNotificationManager manager)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	public NotificationSetting Setting => _manager.Setting switch
	{
		AppNotificationSetting.Enabled => NotificationSetting.Enabled,
		AppNotificationSetting.DisabledForUser => NotificationSetting.DisabledForUser,
		AppNotificationSetting.DisabledByGroupPolicy => NotificationSetting.DisabledByGroupPolicy,
		AppNotificationSetting.DisabledByManifest => NotificationSetting.DisabledByManifest,
		_ => NotificationSetting.DisabledForApplication,
	};

	public void Show(ToastNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);

		var appNotification = LegacyToastNotificationPayloadAdapter.ToAppNotification(notification);
		_manager.ShowReplacingTagAndGroup(appNotification);
		if (appNotification.Id != 0)
		{
			notification.AppNotificationId = appNotification.Id;
		}
	}
}
