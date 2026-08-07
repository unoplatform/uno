#nullable enable

using System;
using Microsoft.Windows.AppNotifications;

namespace Windows.UI.Notifications.Internal;

internal static class ToastNotificationSchedulerRuntime
{
	private static readonly object _gate = new();
	private static ToastNotificationScheduler? _scheduler;
	private static bool _wasRecovered;

	public static ToastNotificationScheduler? GetScheduler() => GetScheduler(recover: true);

	private static ToastNotificationScheduler? GetScheduler(bool recover)
	{
		lock (_gate)
		{
			if (_scheduler is null)
			{
				if (ToastNotificationSchedulerBackendFactory.Create() is not { } backend)
				{
					return null;
				}
				_scheduler = new ToastNotificationScheduler(
					new ToastNotificationScheduleStore(ToastNotificationSchedulePersistenceFactory.Create()),
					backend);
			}
			if (recover && !_wasRecovered)
			{
				_wasRecovered = _scheduler.Recover(DateTimeOffset.UtcNow);
			}
			return _scheduler;
		}
	}

	public static ToastNotificationScheduler? GetSchedulerForEnumeration()
	{
		lock (_gate)
		{
			var scheduler = GetScheduler(recover: false);
			if (scheduler is not null && (!_wasRecovered || scheduler.UsesNativeScheduling))
			{
				_wasRecovered = scheduler.Recover(DateTimeOffset.UtcNow);
			}
			return scheduler;
		}
	}

	public static void Recover()
	{
		var scheduler = GetScheduler(recover: false);
		var recovered = scheduler?.Recover(DateTimeOffset.UtcNow) == true;
		lock (_gate)
		{
			_wasRecovered = recovered;
		}
	}

	public static void Deliver(string scheduleIdentifier)
		=> Deliver(scheduleIdentifier, AppNotificationManager.Default);

	public static bool CompleteNativeDelivery(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		var scheduler = GetScheduler(recover: false);
		var completed = scheduler?.CompleteNativeDelivery(scheduleIdentifier) == true;
		EnsureRecovered(scheduler);
		return completed;
	}

	internal static void Deliver(string scheduleIdentifier, AppNotificationManager manager)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentNullException.ThrowIfNull(manager);
		var scheduler = GetScheduler(recover: false);
		var record = scheduler?.BeginDelivery(scheduleIdentifier);
		EnsureRecovered(scheduler);
		if (record is null)
		{
			return;
		}
		if (ToastNotificationScheduler.IsExpired(record, DateTimeOffset.UtcNow))
		{
			scheduler!.CompleteDelivery(scheduleIdentifier);
			return;
		}

		try
		{
			var notification = new AppNotification(record.Payload)
			{
				Tag = record.Tag,
				Group = record.Group,
				Expiration = record.ExpirationTimeUtc ?? DateTimeOffset.FromFileTime(0),
				SuppressDisplay = record.SuppressPopup,
			};
			manager.ShowScheduled(notification, record.ScheduleIdentifier);
			scheduler!.CompleteDelivery(scheduleIdentifier);
		}
		catch
		{
			scheduler!.RetryDelivery(scheduleIdentifier, DateTimeOffset.UtcNow);
			throw;
		}
	}

	internal static ToastNotificationScheduleRecord ToRecord(ScheduledToastNotification notification)
	{
		ArgumentNullException.ThrowIfNull(notification);
		return new ToastNotificationScheduleRecord(
			notification.ScheduleIdentifier,
			LegacyToastNotificationPayloadAdapter.Normalize(notification.Content.GetXml()),
			notification.DeliveryTime.ToUniversalTime(),
			notification.ExpirationTime?.ToUniversalTime(),
			notification.Id,
			notification.Tag,
			notification.Group,
			notification.SuppressPopup,
			notification.SnoozeInterval,
			notification.MaximumSnoozeCount,
			NotificationMirroring: notification.SchedulingNotificationMirroring);
	}

	internal static ScheduledToastNotification FromRecord(ToastNotificationScheduleRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		var content = new Windows.Data.Xml.Dom.XmlDocument();
		content.LoadXml(LegacyToastNotificationPayloadAdapter.Restore(record.Payload));
		var notification = record.SnoozeInterval is { } interval
			? new ScheduledToastNotification(content, record.DeliveryTimeUtc.ToLocalTime(), interval, record.MaximumSnoozeCount)
			: new ScheduledToastNotification(content, record.DeliveryTimeUtc.ToLocalTime());
		notification.ScheduleIdentifier = record.ScheduleIdentifier;
		notification.ExpirationTime = record.ExpirationTimeUtc?.ToLocalTime();
		notification.Id = record.Id;
		if (record.Tag.Length > 0)
		{
			notification.Tag = record.Tag;
		}
		notification.Group = record.Group;
		notification.SuppressPopup = record.SuppressPopup;
		notification.SchedulingNotificationMirroring = record.NotificationMirroring;
		return notification;
	}

	internal static void SetSchedulerForTests(ToastNotificationScheduler? scheduler)
	{
		lock (_gate)
		{
			_scheduler = scheduler;
			_wasRecovered = false;
		}
	}

	internal static void InitializeForTests(IToastNotificationSchedulePersistence persistence, IToastNotificationSchedulerBackend backend, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(persistence);
		ArgumentNullException.ThrowIfNull(backend);
		lock (_gate)
		{
			_scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);
			_scheduler.Recover(now);
			_wasRecovered = true;
		}
	}

	private static void EnsureRecovered(ToastNotificationScheduler? scheduler)
	{
		if (scheduler is null)
		{
			return;
		}
		lock (_gate)
		{
			if (!_wasRecovered)
			{
				_wasRecovered = scheduler.Recover(DateTimeOffset.UtcNow);
			}
		}
	}
}
