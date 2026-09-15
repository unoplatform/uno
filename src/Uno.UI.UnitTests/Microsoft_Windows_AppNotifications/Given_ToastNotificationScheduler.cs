#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationScheduler
{
	private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Platform_Schedule_Fails_Durable_Retry_Intent_Is_Retained()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var backend = new TestSchedulerBackend { ScheduleException = new InvalidOperationException("failed") };
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Add(Record("failed", Now.AddHours(1)), Now));

		Assert.AreEqual(1, scheduler.GetAll().Count);
		Assert.AreEqual(
			ToastNotificationNativeOperationKind.Retry,
			persistence.Load().NativeOperations!.Single().Kind);
	}

	[TestMethod]
	public void When_Cancel_Fails_Durable_Record_Remains_For_Retry()
	{
		var backend = new TestSchedulerBackend();
		var scheduler = CreateScheduler(backend);
		var record = Record("retry", Now.AddHours(1));
		scheduler.Add(record, Now);
		backend.CancelException = new InvalidOperationException("failed");

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Remove(record.ScheduleIdentifier));

		Assert.AreEqual(0, scheduler.GetAll().Count);
		backend.CancelException = null;
		Assert.IsTrue(scheduler.Recover(Now));
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.Canceled.ToArray());
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Canceling_Record_Is_Recovered_It_Is_Canceled_And_Not_Rescheduled()
	{
		var canceling = Record("canceling", Now.AddHours(1)) with { Status = ToastNotificationScheduleStatus.Canceling };
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { canceling }));
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		CollectionAssert.AreEqual(new[] { canceling.ScheduleIdentifier }, backend.Canceled.ToArray());
		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.IsNull(scheduler.BeginDelivery(canceling.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Scheduler_Recovers_Missed_And_Future_Records_Are_Registered()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { Record("past", Now.AddMinutes(-1)), Record("future", Now.AddMinutes(1)) }));
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		CollectionAssert.AreEqual(new[] { "past", "future" }, backend.Scheduled.ToArray());
	}

	[TestMethod]
	public void When_Native_Scheduler_Recovers_Pending_Request_Is_Not_Duplicated()
	{
		var record = Record("pending", Now.AddMinutes(1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = new[] { record.ScheduleIdentifier },
			Delivered = Array.Empty<string>(),
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(1, scheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Native_Scheduler_Recovers_Delivered_Request_Journal_Is_Completed()
	{
		var record = Record("delivered", Now.AddMinutes(-1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = new[] { record.ScheduleIdentifier },
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		Assert.IsTrue(scheduler.Recover(Now));
		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.PersistedReceipts.Count);
		Assert.AreEqual(0, backend.Canceled.Count);
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.PersistedHistory.Keys.ToArray());
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.ConsumedReceipts.ToArray());
	}

	[TestMethod]
	public void When_Native_Delivery_Receipt_Cannot_Be_Persisted_Journal_Is_Retained()
	{
		var record = Record("receipt-failure", Now.AddMinutes(-1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = new[] { record.ScheduleIdentifier },
			PersistReceiptResult = false,
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		var recovered = scheduler.Recover(Now);

		Assert.IsFalse(recovered);
		Assert.AreEqual(1, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.Scheduled.Count);
	}

	[TestMethod]
	public void When_Due_Native_Request_Is_Transiently_Missing_Recovery_Waits_Without_Replaying()
	{
		var record = Record("delivery-transition", Now.AddSeconds(-10));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(1, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(0, backend.PersistedReceipts.Count);

		Assert.IsTrue(scheduler.Recover(Now.Add(ToastNotificationScheduler.DeliveryRetryDelay)));

		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.Scheduled.Count);
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.ConsumedReceipts.ToArray());
	}

	[TestMethod]
	public void When_Due_Native_Request_Disappeared_Recovery_Records_Receipt_Without_Replaying()
	{
		var record = Record("dismissed", Now.AddMinutes(-1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(0, backend.PersistedReceipts.Count);
		Assert.AreEqual(0, backend.PersistedHistory.Count);
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.ConsumedReceipts.ToArray());
	}

	[TestMethod]
	public void When_Durable_Native_Receipt_And_Stale_Journal_Are_Recovered_Delivery_Is_Idempotent()
	{
		var record = Record("receipt-recovery", Now.AddMinutes(-1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		backend.PersistedReceipts.Add(record.ScheduleIdentifier);
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		Assert.IsTrue(scheduler.Recover(Now));
		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(0, backend.PersistedReceipts.Count);
		Assert.AreEqual(0, backend.PersistedHistory.Count);
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.ConsumedReceipts.ToArray());
	}

	[TestMethod]
	public void When_Native_Scheduler_Starts_Stale_Receipts_Are_Cleaned_Up()
	{
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		backend.PersistedReceipts.Add("stale");
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend);

		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(0, backend.PersistedReceipts.Count);
		Assert.AreEqual(1, backend.CleanupCount);
	}

	[TestMethod]
	public void When_Native_Scheduler_Starts_Stale_Delivered_History_Is_Cleaned_Up()
	{
		var stale = Record("stale-history", Now.AddMinutes(-1));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		backend.PersistedHistory[stale.ScheduleIdentifier] = stale;
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend);

		Assert.IsTrue(scheduler.Recover(Now));

		Assert.AreEqual(0, backend.PersistedHistory.Count);
		Assert.AreEqual(1, backend.HistoryCleanupCount);
	}

	[TestMethod]
	public void When_Native_State_Is_Unavailable_Recovery_Is_Deferred()
	{
		var record = Record("unknown", Now.AddMinutes(1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		var recovered = scheduler.Recover(Now);

		Assert.IsFalse(recovered);
		Assert.AreEqual(0, backend.Scheduled.Count);
		Assert.AreEqual(1, scheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Native_Pending_Request_Is_Too_Late_It_Is_Canceled()
	{
		var record = Record("expired-pending", Now.AddMinutes(-6));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestNativeSchedulerBackend
		{
			Pending = new[] { record.ScheduleIdentifier },
			Delivered = Array.Empty<string>(),
		};
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.Canceled.ToArray());
		Assert.AreEqual(0, scheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Alarm_Is_Consumed_Duplicate_Delivery_Is_Idempotent()
	{
		var scheduler = CreateScheduler(new TestSchedulerBackend());
		var record = Record("once", Now.AddHours(1));
		scheduler.Add(record, Now);

		var delivering = scheduler.BeginDelivery(record.ScheduleIdentifier);

		Assert.AreEqual(record.ScheduleIdentifier, delivering?.Record.ScheduleIdentifier);
		Assert.AreEqual(ToastNotificationScheduleStatus.Delivering, delivering?.Record.Status);
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
		scheduler.CompleteDelivery(delivering!);
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Native_Platform_Delivers_Record_Is_Completed_Without_Second_Show()
	{
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
		};
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend);
		var record = Record("native", Now.AddHours(1));
		scheduler.Add(record, Now);

		Assert.IsTrue(scheduler.CompleteNativeDelivery(record.ScheduleIdentifier));
		Assert.IsFalse(scheduler.CompleteNativeDelivery(record.ScheduleIdentifier));
		Assert.AreEqual(0, scheduler.GetAll().Count);
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
		Assert.AreEqual(0, backend.PersistedReceipts.Count);
		Assert.AreEqual(0, backend.Canceled.Count);
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.PersistedHistory.Keys.ToArray());
		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.ConsumedReceipts.ToArray());
	}

	[TestMethod]
	public void When_Native_Delivered_History_Cannot_Be_Persisted_Journal_Is_Retained()
	{
		var backend = new TestNativeSchedulerBackend
		{
			Pending = Array.Empty<string>(),
			Delivered = Array.Empty<string>(),
			PersistHistoryResult = false,
		};
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend);
		var record = Record("history-failure", Now.AddHours(1));
		scheduler.Add(record, Now);

		Assert.IsFalse(scheduler.CompleteNativeDelivery(record.ScheduleIdentifier));

		Assert.AreEqual(1, scheduler.GetAll().Count);
		Assert.AreEqual(1, backend.PersistedReceipts.Count);
		Assert.AreEqual(0, backend.PersistedHistory.Count);
	}

	[TestMethod]
	public void When_Delivery_Was_Interrupted_Recovery_Resets_And_Registers_It()
	{
		var delivering = Record("delivering", Now.AddMinutes(1)) with { Status = ToastNotificationScheduleStatus.Delivering };
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { delivering }));
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		CollectionAssert.AreEqual(new[] { delivering.ScheduleIdentifier }, backend.Scheduled.ToArray());
		Assert.AreEqual(
			ToastNotificationScheduleStatus.Delivering,
			scheduler.BeginDelivery(delivering.ScheduleIdentifier)?.Record.Status);
	}

	[TestMethod]
	public void When_Missed_Alarm_Exceeds_Delivery_Window_It_Is_Dropped()
	{
		var withinWindow = Record("within", Now.AddMinutes(-4).AddSeconds(-59));
		var tooLate = Record("late", Now.AddMinutes(-5).AddSeconds(-1));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { withinWindow, tooLate }));
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		scheduler.Recover(Now);

		CollectionAssert.AreEqual(new[] { withinWindow.ScheduleIdentifier }, backend.Scheduled.ToArray());
		Assert.IsNull(scheduler.BeginDelivery(tooLate.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Direct_Delivery_Retry_Is_Six_Minutes_Late_It_Is_Reregistered()
	{
		var record = Record("late-retry", Now.AddMinutes(-6));
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record with { Status = ToastNotificationScheduleStatus.Delivering } }));
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend);

		var claim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.IsNotNull(claim);
		scheduler.RetryDelivery(claim, Now);

		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.Scheduled.ToArray());
	}

	private static ToastNotificationScheduler CreateScheduler(TestSchedulerBackend backend)
		=> new(new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()), backend);

	private static ToastNotificationScheduleRecord Record(string identifier, DateTimeOffset deliveryTime)
		=> new(
			identifier,
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			deliveryTime,
			null,
			string.Empty,
			string.Empty,
			string.Empty,
			false,
			null,
			1);

	private sealed class TestSchedulerBackend : IToastNotificationSchedulerBackend
	{
		public Exception? ScheduleException { get; set; }

		public Exception? CancelException { get; set; }

		public List<string> Scheduled { get; } = new();

		public List<string> Canceled { get; } = new();

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			if (ScheduleException is not null)
			{
				throw ScheduleException;
			}
			Scheduled.Add(record.ScheduleIdentifier);
		}

		public void Cancel(string scheduleIdentifier)
		{
			if (CancelException is not null)
			{
				throw CancelException;
			}
			Canceled.Add(scheduleIdentifier);
		}
	}

	private sealed class TestNativeSchedulerBackend : IToastNotificationSchedulerBackend, INativeToastNotificationSchedulerBackend
	{
		public IReadOnlyCollection<string>? Pending { get; init; }

		public IReadOnlyCollection<string>? Delivered { get; init; }

		public bool PersistReceiptResult { get; init; } = true;

		public bool PersistHistoryResult { get; init; } = true;

		public HashSet<string> PersistedReceipts { get; } = new(StringComparer.Ordinal);

		public Dictionary<string, ToastNotificationScheduleRecord> PersistedHistory { get; } = new(StringComparer.Ordinal);

		public List<string> ConsumedReceipts { get; } = new();

		public int CleanupCount { get; private set; }

		public int HistoryCleanupCount { get; private set; }

		public List<string> Scheduled { get; } = new();

		public List<string> Canceled { get; } = new();

		public void Schedule(ToastNotificationScheduleRecord record) => Scheduled.Add(record.ScheduleIdentifier);

		public void Cancel(string scheduleIdentifier) => Canceled.Add(scheduleIdentifier);

		public IReadOnlyCollection<string>? GetPendingScheduleIdentifiers() => Pending;

		public IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers() => Delivered;

		public IReadOnlyCollection<string>? GetDeliveryReceiptIdentifiers() => PersistedReceipts;

		public bool TryPersistDeliveryReceipt(string scheduleIdentifier)
		{
			if (!PersistReceiptResult)
			{
				return false;
			}
			PersistedReceipts.Add(scheduleIdentifier);
			return true;
		}

		public void ConsumeDeliveryReceipt(string scheduleIdentifier)
		{
			PersistedReceipts.Remove(scheduleIdentifier);
			ConsumedReceipts.Add(scheduleIdentifier);
		}

		public void CleanupDeliveryReceipts(IReadOnlyCollection<string> retainedScheduleIdentifiers)
		{
			CleanupCount++;
			PersistedReceipts.IntersectWith(retainedScheduleIdentifiers);
		}

		public bool TryPersistDeliveredHistory(ToastNotificationScheduleRecord record)
		{
			if (!PersistHistoryResult)
			{
				return false;
			}
			PersistedHistory[record.ScheduleIdentifier] = record;
			return true;
		}

		public IReadOnlyCollection<ToastNotificationScheduleRecord>? GetDeliveredHistory()
			=> PersistedHistory.Values.ToArray();

		public bool TryRemoveDeliveredHistory(string scheduleIdentifier)
			=> PersistedHistory.Remove(scheduleIdentifier);

		public bool TryCleanupDeliveredHistory(IReadOnlyCollection<string> activeScheduleIdentifiers)
		{
			HistoryCleanupCount++;
			PersistedHistory.Keys
				.Where(identifier => !activeScheduleIdentifiers.Contains(identifier))
				.ToList()
				.ForEach(identifier => PersistedHistory.Remove(identifier));
			return true;
		}
	}
}
