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
		AppleAppNotificationRuntime.RemoveScheduled(scheduleIdentifier);
	}

	public IReadOnlyCollection<string>? GetPendingScheduleIdentifiers()
		=> AppleAppNotificationRuntime.GetPendingScheduleIdentifiers();

	public IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers()
		=> AppleAppNotificationRuntime.GetDeliveredScheduleIdentifiers();

	public IReadOnlyCollection<string>? GetDeliveryReceiptIdentifiers()
		=> AppleToastNotificationDeliveryReceiptStore.GetIdentifiers();

	public bool TryPersistDeliveryReceipt(string scheduleIdentifier)
		=> AppleToastNotificationDeliveryReceiptStore.TryPersist(scheduleIdentifier);

	public void ConsumeDeliveryReceipt(string scheduleIdentifier)
		=> AppleToastNotificationDeliveryReceiptStore.TryConsume(scheduleIdentifier);

	public void CleanupDeliveryReceipts(IReadOnlyCollection<string> retainedScheduleIdentifiers)
		=> AppleToastNotificationDeliveryReceiptStore.TryCleanup(retainedScheduleIdentifiers);

	public bool TryPersistDeliveredHistory(ToastNotificationScheduleRecord record)
		=> AppleToastNotificationDeliveredHistoryStore.TryPersist(record);

	public IReadOnlyCollection<ToastNotificationScheduleRecord>? GetDeliveredHistory()
		=> AppleToastNotificationDeliveredHistoryStore.GetAll();

	public bool TryRemoveDeliveredHistory(string scheduleIdentifier)
		=> AppleToastNotificationDeliveredHistoryStore.TryRemove(scheduleIdentifier);

	public bool TryCleanupDeliveredHistory(IReadOnlyCollection<string> activeScheduleIdentifiers)
		=> AppleToastNotificationDeliveredHistoryStore.TryCleanup(activeScheduleIdentifiers);
}