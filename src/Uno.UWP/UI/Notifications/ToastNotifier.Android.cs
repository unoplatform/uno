#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Notifications.Internal;

namespace Windows.UI.Notifications;

public partial class ToastNotifier
{
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
		=> ToastNotificationSchedulerRuntime.GetScheduler()?.GetAll()
			.Select(ToastNotificationSchedulerRuntime.FromRecord)
			.ToArray()
			?? Array.Empty<ScheduledToastNotification>();
}
