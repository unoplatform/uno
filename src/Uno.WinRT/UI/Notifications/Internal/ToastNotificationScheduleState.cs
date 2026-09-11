#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Windows.UI.Notifications.Internal;

internal enum ToastNotificationScheduleStatus
{
	Active,
	Canceling,
	Delivering,
}

internal sealed record ToastNotificationScheduleRecord(
	string ScheduleIdentifier,
	string Payload,
	DateTimeOffset DeliveryTimeUtc,
	DateTimeOffset? ExpirationTimeUtc,
	string Id,
	string Tag,
	string Group,
	bool SuppressPopup,
	TimeSpan? SnoozeInterval,
	uint MaximumSnoozeCount,
	ToastNotificationScheduleStatus Status = ToastNotificationScheduleStatus.Active,
	NotificationMirroring NotificationMirroring = NotificationMirroring.Allowed);

internal sealed record ToastNotificationScheduleSnapshot(
	int SchemaVersion,
	IReadOnlyList<ToastNotificationScheduleRecord> Records)
{
	public const int CurrentSchemaVersion = 1;

	public static ToastNotificationScheduleSnapshot Empty { get; } = new(CurrentSchemaVersion, Array.Empty<ToastNotificationScheduleRecord>());
}

internal interface IToastNotificationSchedulePersistence
{
	ToastNotificationScheduleSnapshot Load();

	void Save(ToastNotificationScheduleSnapshot state);
}

internal sealed class InMemoryToastNotificationSchedulePersistence : IToastNotificationSchedulePersistence
{
	private ToastNotificationScheduleSnapshot _state;

	public InMemoryToastNotificationSchedulePersistence(ToastNotificationScheduleSnapshot? state = null)
	{
		_state = Clone(state ?? ToastNotificationScheduleSnapshot.Empty);
	}

	public ToastNotificationScheduleSnapshot Load() => Clone(_state);

	public void Save(ToastNotificationScheduleSnapshot state) => _state = Clone(state);

	private static ToastNotificationScheduleSnapshot Clone(ToastNotificationScheduleSnapshot state)
		=> state with { Records = state.Records.ToArray() };
}

internal sealed class ToastNotificationScheduleStore
{
	internal const int MaximumScheduledNotifications = 4096;
	private readonly object _gate = new();
	private readonly IToastNotificationSchedulePersistence _persistence;
	private ToastNotificationScheduleSnapshot _state;

	public ToastNotificationScheduleStore(IToastNotificationSchedulePersistence persistence)
	{
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		var loaded = persistence.Load();
		_state = loaded.SchemaVersion == ToastNotificationScheduleSnapshot.CurrentSchemaVersion
			? loaded with { Records = loaded.Records.ToArray() }
			: ToastNotificationScheduleSnapshot.Empty;
	}

	public void Add(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(record);
		if (record.DeliveryTimeUtc <= now.ToUniversalTime())
		{
			throw new COMException("The scheduled notification delivery time must be in the future.", unchecked((int)0x80070718));
		}

		lock (_gate)
		{
			if (_state.Records.Count >= MaximumScheduledNotifications)
			{
				throw new COMException("The maximum number of scheduled notifications has been reached.", unchecked((int)0x80070718));
			}
			if (_state.Records.Any(item => item.ScheduleIdentifier == record.ScheduleIdentifier))
			{
				throw new InvalidOperationException("The scheduled notification is already registered.");
			}

			Commit(_state with { Records = _state.Records.Append(record).ToArray() });
		}
	}

	public ToastNotificationScheduleRecord? Get(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			return _state.Records.FirstOrDefault(record => record.ScheduleIdentifier == scheduleIdentifier);
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetAll()
	{
		lock (_gate)
		{
			return _state.Records
				.Where(record => record.Status == ToastNotificationScheduleStatus.Active)
				.OrderBy(record => record.DeliveryTimeUtc)
				.ToArray();
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetPendingCancellations()
	{
		lock (_gate)
		{
			return _state.Records
				.Where(record => record.Status == ToastNotificationScheduleStatus.Canceling)
				.ToArray();
		}
	}

	public ToastNotificationScheduleRecord? BeginRemove(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			var record = _state.Records.FirstOrDefault(item => item.ScheduleIdentifier == scheduleIdentifier);
			if (record is null)
			{
				return null;
			}
			var canceling = record with { Status = ToastNotificationScheduleStatus.Canceling };
			Commit(_state with
			{
				Records = _state.Records.Select(item => item.ScheduleIdentifier == scheduleIdentifier ? canceling : item).ToArray(),
			});
			return canceling;
		}
	}

	public ToastNotificationScheduleRecord? BeginDelivery(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			var record = _state.Records.FirstOrDefault(item =>
				item.ScheduleIdentifier == scheduleIdentifier &&
				item.Status is ToastNotificationScheduleStatus.Active or ToastNotificationScheduleStatus.Delivering);
			if (record is null)
			{
				return null;
			}
			var delivering = record with { Status = ToastNotificationScheduleStatus.Delivering };
			Commit(_state with
			{
				Records = _state.Records.Select(item => item.ScheduleIdentifier == scheduleIdentifier ? delivering : item).ToArray(),
			});
			return delivering;
		}
	}

	public void ResetDeliveries(IReadOnlySet<string> activeDeliveries)
	{
		ArgumentNullException.ThrowIfNull(activeDeliveries);
		lock (_gate)
		{
			if (_state.Records.Any(record =>
				record.Status == ToastNotificationScheduleStatus.Delivering &&
				!activeDeliveries.Contains(record.ScheduleIdentifier)))
			{
				Commit(_state with
				{
					Records = _state.Records
						.Select(record =>
							record.Status == ToastNotificationScheduleStatus.Delivering &&
							!activeDeliveries.Contains(record.ScheduleIdentifier)
							? record with { Status = ToastNotificationScheduleStatus.Active }
							: record)
						.ToArray(),
				});
			}
		}
	}

	public ToastNotificationScheduleRecord? ResetDelivery(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			ToastNotificationScheduleRecord? active = null;
			var records = _state.Records.Select(record =>
			{
				if (record.ScheduleIdentifier != scheduleIdentifier || record.Status != ToastNotificationScheduleStatus.Delivering)
				{
					return record;
				}
				active = record with { Status = ToastNotificationScheduleStatus.Active };
				return active;
			}).ToArray();
			if (active is not null)
			{
				Commit(_state with { Records = records });
			}
			return active;
		}
	}

	public ToastNotificationScheduleRecord? Remove(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		lock (_gate)
		{
			var removed = _state.Records.FirstOrDefault(record => record.ScheduleIdentifier == scheduleIdentifier);
			if (removed is not null)
			{
				Commit(_state with { Records = _state.Records.Where(record => record.ScheduleIdentifier != scheduleIdentifier).ToArray() });
			}
			return removed;
		}
	}

	private void Commit(ToastNotificationScheduleSnapshot next)
	{
		_persistence.Save(next);
		_state = next;
	}
}
