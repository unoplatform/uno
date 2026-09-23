#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Windows.AppNotifications;
using Windows.UI.Notifications.Internal;

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

	public void AddToSchedule(ScheduledToastNotification scheduledToast)
	{
		ArgumentNullException.ThrowIfNull(scheduledToast);
		var scheduler = ToastNotificationSchedulerRuntime.GetScheduler()
			?? throw new NotSupportedException("Scheduled toast notifications are not supported on this platform.");
		scheduler.Add(ToastNotificationSchedulerRuntime.ToRecord(scheduledToast), DateTimeOffset.UtcNow);
	}

	public void RemoveFromSchedule(ScheduledToastNotification scheduledToast)
	{
		ArgumentNullException.ThrowIfNull(scheduledToast);
		ToastNotificationSchedulerRuntime.GetScheduler()?.Remove(scheduledToast.ScheduleIdentifier);
	}

	public IReadOnlyList<ScheduledToastNotification> GetScheduledToastNotifications()
		=> ToastNotificationSchedulerRuntime.GetSchedulerForEnumeration()?.GetAll()
			.Select(ToastNotificationSchedulerRuntime.FromRecord)
			.ToArray()
			?? Array.Empty<ScheduledToastNotification>();
}
