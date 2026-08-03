#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationScheduler
{
	private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Platform_Schedule_Fails_Durable_Reservation_Is_Rolled_Back()
	{
		var backend = new TestSchedulerBackend { ScheduleException = new InvalidOperationException("failed") };
		var scheduler = CreateScheduler(backend);

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Add(Record("failed", Now.AddHours(1)), Now));

		Assert.AreEqual(0, scheduler.GetAll().Count);
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
		scheduler.Recover(Now);
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
	public void When_Alarm_Is_Consumed_Duplicate_Delivery_Is_Idempotent()
	{
		var scheduler = CreateScheduler(new TestSchedulerBackend());
		var record = Record("once", Now.AddHours(1));
		scheduler.Add(record, Now);

		var delivering = scheduler.BeginDelivery(record.ScheduleIdentifier);

		Assert.AreEqual(record.ScheduleIdentifier, delivering?.ScheduleIdentifier);
		Assert.AreEqual(ToastNotificationScheduleStatus.Delivering, delivering?.Status);
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
		scheduler.CompleteDelivery(record.ScheduleIdentifier);
		Assert.IsNull(scheduler.BeginDelivery(record.ScheduleIdentifier));
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
		Assert.AreEqual(ToastNotificationScheduleStatus.Delivering, scheduler.BeginDelivery(delivering.ScheduleIdentifier)?.Status);
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

		scheduler.RetryDelivery(record.ScheduleIdentifier, Now);

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
}
