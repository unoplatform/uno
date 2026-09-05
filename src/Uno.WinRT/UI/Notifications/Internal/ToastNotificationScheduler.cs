#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Windows.UI.Notifications.Internal;

internal interface IToastNotificationSchedulerBackend
{
	void Schedule(ToastNotificationScheduleRecord record);

	void Cancel(string scheduleIdentifier);
}

internal interface INativeToastNotificationSchedulerBackend
{
	IReadOnlyCollection<string>? GetPendingScheduleIdentifiers();

	IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers();
}

internal static partial class ToastNotificationSchedulerBackendFactory
{
	public static IToastNotificationSchedulerBackend? Create()
	{
		IToastNotificationSchedulerBackend? backend = null;
		CreatePlatform(ref backend);
		return backend;
	}

	static partial void CreatePlatform(ref IToastNotificationSchedulerBackend? backend);
}

internal sealed class ToastNotificationScheduler
{
	internal static readonly TimeSpan MaximumDeliveryDelay = TimeSpan.FromMinutes(5);
	private readonly object _gate = new();
	private readonly ToastNotificationScheduleStore _store;
	private readonly IToastNotificationSchedulerBackend _backend;
	private readonly HashSet<string> _activeDeliveries = new(StringComparer.Ordinal);

	public ToastNotificationScheduler(ToastNotificationScheduleStore store, IToastNotificationSchedulerBackend backend)
	{
		_store = store ?? throw new ArgumentNullException(nameof(store));
		_backend = backend ?? throw new ArgumentNullException(nameof(backend));
	}

	public void Add(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		lock (_gate)
		{
			_store.Add(record, now);
			try
			{
				_backend.Schedule(record);
			}
			catch
			{
				_store.Remove(record.ScheduleIdentifier);
				throw;
			}
		}
	}

	public void Remove(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (_store.BeginRemove(scheduleIdentifier) is null)
			{
				return;
			}
			_backend.Cancel(scheduleIdentifier);
			_store.Remove(scheduleIdentifier);
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetAll() => _store.GetAll();

	public bool UsesNativeScheduling => _backend is INativeToastNotificationSchedulerBackend;

	public bool Recover(DateTimeOffset now)
	{
		lock (_gate)
		{
			_store.ResetDeliveries(_activeDeliveries);
			foreach (var record in _store.GetPendingCancellations())
			{
				_backend.Cancel(record.ScheduleIdentifier);
				_store.Remove(record.ScheduleIdentifier);
			}
			HashSet<string>? pending = null;
			HashSet<string>? delivered = null;
			if (_backend is INativeToastNotificationSchedulerBackend nativeBackend)
			{
				var pendingIdentifiers = nativeBackend.GetPendingScheduleIdentifiers();
				var deliveredIdentifiers = nativeBackend.GetDeliveredScheduleIdentifiers();
				if (pendingIdentifiers is null || deliveredIdentifiers is null)
				{
					return false;
				}
				pending = pendingIdentifiers.ToHashSet(StringComparer.Ordinal);
				delivered = deliveredIdentifiers.ToHashSet(StringComparer.Ordinal);
			}
			foreach (var record in _store.GetAll())
			{
				if (delivered?.Contains(record.ScheduleIdentifier) == true)
				{
					_store.Remove(record.ScheduleIdentifier);
				}
				else if (IsTooLateForRecovery(record, now))
				{
					Remove(record.ScheduleIdentifier);
				}
				else if (pending?.Contains(record.ScheduleIdentifier) == true)
				{
					continue;
				}
				else
				{
					_backend.Schedule(record);
				}
			}
			return true;
		}
	}

	public ToastNotificationScheduleRecord? BeginDelivery(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (!_activeDeliveries.Add(scheduleIdentifier))
			{
				return null;
			}
			var record = _store.BeginDelivery(scheduleIdentifier);
			if (record is null)
			{
				_activeDeliveries.Remove(scheduleIdentifier);
			}
			return record;
		}
	}

	public void CompleteDelivery(string scheduleIdentifier)
	{
		lock (_gate)
		{
			_store.Remove(scheduleIdentifier);
			_activeDeliveries.Remove(scheduleIdentifier);
		}
	}

	public bool CompleteNativeDelivery(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (!_activeDeliveries.Add(scheduleIdentifier))
			{
				return false;
			}
			if (_store.BeginDelivery(scheduleIdentifier) is null)
			{
				_activeDeliveries.Remove(scheduleIdentifier);
				return false;
			}
			_store.Remove(scheduleIdentifier);
			_activeDeliveries.Remove(scheduleIdentifier);
			return true;
		}
	}

	public void RetryDelivery(string scheduleIdentifier, DateTimeOffset now)
	{
		lock (_gate)
		{
			_activeDeliveries.Remove(scheduleIdentifier);
			if (_store.ResetDelivery(scheduleIdentifier) is not { } record)
			{
				return;
			}
			if (IsExpired(record, now))
			{
				Remove(scheduleIdentifier);
			}
			else
			{
				_backend.Schedule(record);
			}
		}
	}

	public static bool IsExpired(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(record);
		return record.ExpirationTimeUtc is { } expiration && now.ToUniversalTime() > expiration;
	}

	public static bool IsTooLateForRecovery(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(record);
		var latestDelivery = record.DeliveryTimeUtc + MaximumDeliveryDelay;
		if (record.ExpirationTimeUtc is { } expiration && expiration < latestDelivery)
		{
			latestDelivery = expiration;
		}
		return now.ToUniversalTime() > latestDelivery;
	}
}
