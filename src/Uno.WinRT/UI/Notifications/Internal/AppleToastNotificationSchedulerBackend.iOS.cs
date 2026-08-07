#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Windows.AppNotifications.Internal;
using UserNotifications;

namespace Windows.UI.Notifications.Internal;

internal sealed class AppleToastNotificationSchedulerBackend : IToastNotificationSchedulerBackend, INativeToastNotificationSchedulerBackend
{
	public void Schedule(ToastNotificationScheduleRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		var command = AppleAppNotificationTranslator.TranslateScheduled(
			record.ScheduleIdentifier,
			record.Payload,
			record.Tag,
			record.Group,
			record.SuppressPopup);
		var delay = Math.Max(1, (record.DeliveryTimeUtc - DateTimeOffset.UtcNow).TotalSeconds);
		var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(delay, repeats: false);
		if (!AppleAppNotificationRuntime.TryPost(command, trigger))
		{
			throw new InvalidOperationException("Apple rejected the scheduled toast notification.");
		}
	}

	public void Cancel(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		AppleAppNotificationRuntime.Remove(AppleAppNotificationTranslator.ScheduledRequestIdentifierPrefix + scheduleIdentifier);
	}

	public IReadOnlyCollection<string>? GetPendingScheduleIdentifiers()
		=> AppleAppNotificationRuntime.GetPendingScheduleIdentifiers();

	public IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers()
		=> AppleAppNotificationRuntime.GetDeliveredScheduleIdentifiers();
}