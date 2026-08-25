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

internal interface IToastNotificationScheduleLifecycle
{
	void Reconcile();

	void OnSchedulesChanged();
}

internal interface IToastNotificationScheduleLifecycleProvider
{
	IToastNotificationScheduleLifecycle CreateScheduleLifecycle(IToastNotificationSchedulePersistence persistence);
}

internal interface INativeToastNotificationSchedulerBackend
{
	IReadOnlyCollection<string>? GetPendingScheduleIdentifiers();

	IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers();

	IReadOnlyCollection<string>? GetDeliveryReceiptIdentifiers();

	bool TryPersistDeliveryReceipt(string scheduleIdentifier);

	void ConsumeDeliveryReceipt(string scheduleIdentifier);

	void CleanupDeliveryReceipts(IReadOnlyCollection<string> retainedScheduleIdentifiers);

	bool TryPersistDeliveredHistory(ToastNotificationScheduleRecord record);

	IReadOnlyCollection<ToastNotificationScheduleRecord>? GetDeliveredHistory();

	bool TryRemoveDeliveredHistory(string scheduleIdentifier);

	bool TryCleanupDeliveredHistory(IReadOnlyCollection<string> activeScheduleIdentifiers);
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
	internal static readonly TimeSpan DeliveryRetryDelay = TimeSpan.FromMinutes(1);
	internal static readonly TimeSpan DeliveryClaimDuration = TimeSpan.FromMinutes(5);
	private const int MaximumNativeReconciliationAttempts = 16;
	private readonly object _gate = new();
	private readonly ToastNotificationScheduleStore _store;
	private readonly IToastNotificationSchedulerBackend _backend;
	private readonly IToastNotificationScheduleLifecycle? _lifecycle;
	private readonly string _deliveryOwner = Guid.NewGuid().ToString("N");
	private readonly Dictionary<string, ToastNotificationDeliveryClaim> _activeDeliveries = new(StringComparer.Ordinal);

	public ToastNotificationScheduler(
		ToastNotificationScheduleStore store,
		IToastNotificationSchedulerBackend backend,
		IToastNotificationScheduleLifecycle? lifecycle = null)
	{
		_store = store ?? throw new ArgumentNullException(nameof(store));
		_backend = backend ?? throw new ArgumentNullException(nameof(backend));
		_lifecycle = lifecycle;
	}

	public void Add(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		lock (_gate)
		{
			var operation = _store.Add(record, now);
			try
			{
				NotifySchedulesChanged();
			}
			catch (Exception exception)
			{
				throw RollbackFailedAdd(record.ScheduleIdentifier, exception);
			}

			try
			{
				ApplyNativeOperation(operation);
			}
			catch (Exception exception)
			{
				try
				{
					_store.MarkRegistrationForRetry(operation);
				}
				catch (Exception persistenceException)
				{
					throw new AggregateException(
						"Scheduled notification registration failed and its durable retry intent could not be recorded.",
						exception,
						persistenceException);
				}
				throw;
			}
		}
	}

	public void Remove(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (_store.RequestRemove(scheduleIdentifier) is not { } operation)
			{
				return;
			}
			ApplyNativeOperation(operation);
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetAll() => _store.GetAll();

	public bool UsesNativeScheduling => _backend is INativeToastNotificationSchedulerBackend;

	public bool Recover(DateTimeOffset now)
	{
		lock (_gate)
		{
			_store.Reload();
			ReconcileLifecycle();
			_store.PrepareRecoveryState(now, DeliveryRetryDelay);
			var reconciledOperations = ApplyPendingNativeOperations();

			HashSet<string>? pending = null;
			HashSet<string>? delivered = null;
			HashSet<string>? nativeDelivered = null;
			var nativeBackend = _backend as INativeToastNotificationSchedulerBackend;
			if (nativeBackend is not null)
			{
				var pendingIdentifiers = nativeBackend.GetPendingScheduleIdentifiers();
				var deliveredIdentifiers = nativeBackend.GetDeliveredScheduleIdentifiers();
				var receiptIdentifiers = nativeBackend.GetDeliveryReceiptIdentifiers();
				if (pendingIdentifiers is null || deliveredIdentifiers is null || receiptIdentifiers is null)
				{
					return false;
				}
				pending = pendingIdentifiers.ToHashSet(StringComparer.Ordinal);
				nativeDelivered = deliveredIdentifiers.ToHashSet(StringComparer.Ordinal);
				delivered = nativeDelivered
					.Concat(receiptIdentifiers)
					.ToHashSet(StringComparer.Ordinal);
			}

			var nowUtc = now.ToUniversalTime();
			foreach (var record in _store.GetAll())
			{
				if (delivered?.Contains(record.ScheduleIdentifier) == true)
				{
					if (!TryCompleteNativeDelivery(
						nativeBackend!,
						record.ScheduleIdentifier,
						now,
						persistHistory: nativeDelivered!.Contains(record.ScheduleIdentifier)))
					{
						return false;
					}
				}
				else if (pending?.Contains(record.ScheduleIdentifier) == true)
				{
					if (IsTooLateForRecovery(record, now))
					{
						Remove(record.ScheduleIdentifier);
					}
				}
				else if (nativeBackend is not null && record.DeliveryTimeUtc <= nowUtc)
				{
					if ((nowUtc - record.DeliveryTimeUtc >= DeliveryRetryDelay || IsExpired(record, now)) &&
						!TryCompleteNativeDelivery(
							nativeBackend,
							record.ScheduleIdentifier,
							now,
							persistHistory: false))
					{
						return false;
					}
				}
				else if (IsTooLateForRecovery(record, now))
				{
					Remove(record.ScheduleIdentifier);
				}
				else if (reconciledOperations.Contains(record.ScheduleIdentifier))
				{
					continue;
				}
				else if (_store.RequestSchedule(
					record.ScheduleIdentifier,
					ToastNotificationNativeOperationKind.Schedule) is { } operation)
				{
					ApplyNativeOperation(operation);
				}
			}

			var retainedIdentifiers = _store.GetAllDurableRecords()
				.Select(record => record.ScheduleIdentifier)
				.Concat(_store.GetPendingNativeOperations().Select(operation => operation.ScheduleIdentifier))
				.Distinct(StringComparer.Ordinal)
				.ToArray();
			nativeBackend?.CleanupDeliveryReceipts(retainedIdentifiers);
			if (nativeBackend is not null &&
				!nativeBackend.TryCleanupDeliveredHistory(
					nativeDelivered is null ? Array.Empty<string>() : nativeDelivered))
			{
				return false;
			}
			return true;
		}
	}

	public IReadOnlyList<ToastNotificationScheduleRecord> GetDeliveredHistory()
	{
		lock (_gate)
		{
			if (_backend is not INativeToastNotificationSchedulerBackend nativeBackend)
			{
				return Array.Empty<ToastNotificationScheduleRecord>();
			}
			return nativeBackend.GetDeliveredHistory()?
				.OrderBy(record => record.DeliveryTimeUtc)
				.ThenBy(record => record.ScheduleIdentifier, StringComparer.Ordinal)
				.ToArray()
				?? throw new InvalidOperationException("Native delivered-toast history could not be read.");
		}
	}

	public void RemoveDeliveredHistory(Func<ToastNotificationScheduleRecord, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		lock (_gate)
		{
			if (_backend is not INativeToastNotificationSchedulerBackend nativeBackend)
			{
				return;
			}
			var history = nativeBackend.GetDeliveredHistory()
				?? throw new InvalidOperationException("Native delivered-toast history could not be read.");
			foreach (var record in history.Where(predicate))
			{
				_backend.Cancel(record.ScheduleIdentifier);
				if (!nativeBackend.TryRemoveDeliveredHistory(record.ScheduleIdentifier))
				{
					throw new InvalidOperationException("Apple could not remove scheduled notification delivered history.");
				}
			}
		}
	}

	public ToastNotificationDeliveryClaim? BeginDelivery(string scheduleIdentifier)
		=> BeginDelivery(scheduleIdentifier, DateTimeOffset.UtcNow);

	internal ToastNotificationDeliveryClaim? BeginDelivery(string scheduleIdentifier, DateTimeOffset now)
	{
		lock (_gate)
		{
			if (_activeDeliveries.ContainsKey(scheduleIdentifier))
			{
				return null;
			}
			var claim = _store.TryClaimDelivery(
				scheduleIdentifier,
				_deliveryOwner,
				now,
				now + DeliveryClaimDuration);
			if (claim is not null)
			{
				_activeDeliveries.Add(scheduleIdentifier, claim);
			}
			return claim;
		}
	}

	public void CompleteDelivery(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (_activeDeliveries.TryGetValue(scheduleIdentifier, out var claim))
			{
				CompleteDelivery(claim);
			}
		}
	}

	public void CompleteDelivery(ToastNotificationDeliveryClaim claim)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			try
			{
				if (_store.CompleteDelivery(claim) is { } operation)
				{
					ApplyNativeOperation(operation);
				}
			}
			finally
			{
				RemoveLocalClaim(claim);
			}
		}
	}

	public bool CompleteNativeDelivery(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (_backend is not INativeToastNotificationSchedulerBackend nativeBackend)
			{
				return false;
			}
			return TryCompleteNativeDelivery(
				nativeBackend,
				scheduleIdentifier,
				DateTimeOffset.UtcNow,
				persistHistory: true);
		}
	}

	public void RetryDelivery(ToastNotificationDeliveryClaim claim, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			try
			{
				var retryTime = GetRetryTime(claim.Record, now);
				if (_store.RetryDelivery(claim, retryTime) is not { } operation)
				{
					return;
				}
				ApplyNativeOperation(operation);
			}
			finally
			{
				RemoveLocalClaim(claim);
			}
		}
	}

	public void RetryDelivery(string scheduleIdentifier, DateTimeOffset now)
	{
		lock (_gate)
		{
			if (_activeDeliveries.TryGetValue(scheduleIdentifier, out var claim))
			{
				RetryDelivery(claim, now);
			}
		}
	}

	public void ReleaseDeliveryClaim(ToastNotificationDeliveryClaim claim, DateTimeOffset now)
	{
		ArgumentNullException.ThrowIfNull(claim);
		lock (_gate)
		{
			try
			{
				var retryTime = GetRetryTime(claim.Record, now);
				if (_store.RetryDelivery(claim, retryTime) is { } operation)
				{
					ApplyNativeOperation(operation);
				}
			}
			finally
			{
				RemoveLocalClaim(claim);
			}
		}
	}

	public void ReleaseDeliveryClaim(ToastNotificationDeliveryClaim claim)
		=> ReleaseDeliveryClaim(claim, DateTimeOffset.UtcNow);

	public void ReleaseDeliveryClaim(string scheduleIdentifier)
	{
		lock (_gate)
		{
			if (_activeDeliveries.TryGetValue(scheduleIdentifier, out var claim))
			{
				ReleaseDeliveryClaim(claim, DateTimeOffset.UtcNow);
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

	private bool TryCompleteNativeDelivery(
		INativeToastNotificationSchedulerBackend backend,
		string scheduleIdentifier,
		DateTimeOffset now,
		bool persistHistory)
	{
		if (BeginDelivery(scheduleIdentifier, now) is not { } claim)
		{
			return false;
		}
		var completed = false;
		try
		{
			if (!backend.TryPersistDeliveryReceipt(scheduleIdentifier))
			{
				return false;
			}
			if (persistHistory && !backend.TryPersistDeliveredHistory(claim.Record))
			{
				return false;
			}
			if (!_store.CompleteNativeDelivery(claim))
			{
				return false;
			}
			NotifySchedulesChanged();
			backend.ConsumeDeliveryReceipt(scheduleIdentifier);
			completed = true;
			return true;
		}
		finally
		{
			if (!completed)
			{
				_store.ReleaseDeliveryClaim(claim);
			}
			RemoveLocalClaim(claim);
		}
	}

	private HashSet<string> ApplyPendingNativeOperations()
	{
		var reconciled = new HashSet<string>(StringComparer.Ordinal);
		foreach (var operation in _store.GetPendingNativeOperations())
		{
			ApplyNativeOperation(operation);
			reconciled.Add(operation.ScheduleIdentifier);
		}
		return reconciled;
	}

	private void ApplyNativeOperation(ToastNotificationNativeOperation operation)
	{
		for (var attempt = 0; attempt < MaximumNativeReconciliationAttempts; attempt++)
		{
			if (operation.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry)
			{
				var record = _store.Get(operation.ScheduleIdentifier);
				if (record?.Revision == operation.RecordRevision)
				{
					_backend.Schedule(record);
				}
			}
			else
			{
				_backend.Cancel(operation.ScheduleIdentifier);
			}

			if (_store.TryCompleteNativeOperation(operation))
			{
				if (operation.Kind == ToastNotificationNativeOperationKind.Cancel)
				{
					NotifySchedulesChanged();
				}
				return;
			}
			if (_store.PrepareCompensation(operation) is not { } compensation)
			{
				return;
			}
			operation = compensation;
		}
		throw new InvalidOperationException("Scheduled notification native state kept changing during reconciliation.");
	}

	private Exception RollbackFailedAdd(string scheduleIdentifier, Exception exception)
	{
		var failures = new List<Exception> { exception };
		var operationCompleted = false;
		try
		{
			if (_store.RequestRemove(scheduleIdentifier) is { } cancel)
			{
				ApplyNativeOperation(cancel);
				operationCompleted = true;
			}
		}
		catch (Exception rollbackException)
		{
			failures.Add(rollbackException);
		}
		if (!operationCompleted)
		{
			try
			{
				NotifySchedulesChanged();
			}
			catch (Exception rollbackException)
			{
				failures.Add(rollbackException);
			}
		}
		return failures.Count == 1
			? exception
			: new AggregateException(
				"Scheduled notification registration failed and its durable lifecycle state could not be rolled back.",
				failures);
	}

	private static DateTimeOffset GetRetryTime(ToastNotificationScheduleRecord record, DateTimeOffset now)
	{
		var retryTime = now.ToUniversalTime() + DeliveryRetryDelay;
		if (record.ExpirationTimeUtc is { } expiration && retryTime > expiration)
		{
			retryTime = expiration;
		}
		return retryTime;
	}

	private void RemoveLocalClaim(ToastNotificationDeliveryClaim claim)
	{
		if (_activeDeliveries.TryGetValue(claim.Record.ScheduleIdentifier, out var active) &&
			active.Token == claim.Token)
		{
			_activeDeliveries.Remove(claim.Record.ScheduleIdentifier);
		}
	}

	private void NotifySchedulesChanged() => _lifecycle?.OnSchedulesChanged();

	private void ReconcileLifecycle() => _lifecycle?.Reconcile();
}
