#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationScheduleLifecycle
{
	private static readonly DateTimeOffset Now = new(2026, 8, 16, 10, 30, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Schedule_Is_Added_Lifecycle_Runs_Before_Platform_Scheduling()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var operations = new List<string>();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			new TestSchedulerBackend { ScheduleAction = _ => operations.Add("schedule") },
			new TestScheduleLifecycle(persistence, count => operations.Add($"state:{count}")));

		scheduler.Add(Record(), Now);

		Assert.AreEqual(2, operations.Count, string.Join(",", operations));
		Assert.AreEqual("state:1", operations[0]);
		Assert.AreEqual("schedule", operations[1]);
	}

	[TestMethod]
	public void When_Lifecycle_Enable_Fails_Durable_Schedule_Is_Rolled_Back()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var counts = new List<int>();
		var expected = new InvalidOperationException("failed");
		var lifecycleCalls = 0;
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			backend,
			new TestScheduleLifecycle(persistence, count =>
			{
				counts.Add(count);
				if (lifecycleCalls++ == 0)
				{
					throw expected;
				}
			}));

		var actual = Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Add(Record(), Now));

		Assert.AreSame(expected, actual);
		Assert.AreEqual(0, backend.ScheduleCount);
		Assert.AreEqual(0, persistence.Load().Records.Count);
		Assert.AreEqual(2, counts.Count);
		Assert.AreEqual(1, counts[0]);
		Assert.AreEqual(0, counts[1]);
	}

	[TestMethod]
	public void When_Schedule_Is_Removed_Lifecycle_Runs_After_Durable_Removal()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var operations = new List<string>();
		var backend = new TestSchedulerBackend { CancelAction = _ => operations.Add("cancel") };
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			backend,
			new TestScheduleLifecycle(persistence, count => operations.Add($"state:{count}")));
		var record = Record();
		scheduler.Add(record, Now);
		operations.Clear();

		scheduler.Remove(record.ScheduleIdentifier);

		Assert.AreEqual(2, operations.Count, string.Join(",", operations));
		Assert.AreEqual("cancel", operations[0]);
		Assert.AreEqual("state:0", operations[1]);
	}

	[TestMethod]
	public void When_Delivery_Completes_Lifecycle_Observes_Empty_Durable_State()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var counts = new List<int>();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			new TestSchedulerBackend(),
			new TestScheduleLifecycle(persistence, counts.Add));
		var record = Record();
		scheduler.Add(record, Now);
		var claim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.IsNotNull(claim);
		counts.Clear();

		scheduler.CompleteDelivery(claim);

		Assert.AreEqual(1, counts.Count);
		Assert.AreEqual(0, counts[0]);
	}

	[TestMethod]
	public void When_Pending_Cancellation_Is_Recovered_Lifecycle_Tracks_Final_State()
	{
		var record = Record() with { Status = ToastNotificationScheduleStatus.Canceling };
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var counts = new List<int>();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			new TestSchedulerBackend(),
			new TestScheduleLifecycle(persistence, counts.Add));

		scheduler.Recover(Now);

		Assert.AreEqual(2, counts.Count);
		Assert.AreEqual(1, counts[0]);
		Assert.AreEqual(0, counts[1]);
	}

	[TestMethod]
	public void When_Empty_State_Is_Recovered_Lifecycle_Is_Reconciled()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var counts = new List<int>();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			new TestSchedulerBackend(),
			new TestScheduleLifecycle(persistence, counts.Add));

		scheduler.Recover(Now);

		Assert.AreEqual(1, counts.Count);
		Assert.AreEqual(0, counts[0]);
	}

	private static ToastNotificationScheduleRecord Record()
		=> new(
			Guid.NewGuid().ToString("N"),
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			Now.AddHours(1),
			null,
			string.Empty,
			string.Empty,
			string.Empty,
			false,
			null,
			0);

	private sealed class TestScheduleLifecycle : IToastNotificationScheduleLifecycle
	{
		private readonly IToastNotificationSchedulePersistence _persistence;
		private readonly Action<int> _onChanged;

		public TestScheduleLifecycle(
			IToastNotificationSchedulePersistence persistence,
			Action<int> onChanged)
		{
			_persistence = persistence;
			_onChanged = onChanged;
		}

		public void OnSchedulesChanged() => _onChanged(_persistence.Load().Records.Count);

		public void Reconcile() => _onChanged(_persistence.Load().Records.Count);
	}

	private sealed class TestSchedulerBackend : IToastNotificationSchedulerBackend
	{
		public Action<ToastNotificationScheduleRecord>? ScheduleAction { get; init; }

		public Action<string>? CancelAction { get; init; }

		public int ScheduleCount { get; private set; }

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			ScheduleCount++;
			ScheduleAction?.Invoke(record);
		}

		public void Cancel(string scheduleIdentifier) => CancelAction?.Invoke(scheduleIdentifier);
	}
}
