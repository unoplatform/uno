#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AndroidToastNotificationBootReceiverLifecycle
{
	[TestMethod]
	public void When_No_Durable_Schedules_Exist_Receiver_Is_Disabled()
	{
		var states = new List<bool>();
		var lifecycle = CreateLifecycle(Array.Empty<ToastNotificationScheduleRecord>(), states);

		lifecycle.OnSchedulesChanged();

		Assert.AreEqual(1, states.Count);
		Assert.IsFalse(states[0]);
	}

	[TestMethod]
	public void When_Compensating_Native_Operation_Exists_Receiver_Remains_Enabled()
	{
		var states = new List<bool>();
		var operation = new ToastNotificationNativeOperation(
			Guid.NewGuid().ToString("N"),
			ToastNotificationNativeOperationKind.Cancel,
			Guid.NewGuid().ToString("N"));
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(
			() => new ToastNotificationScheduleSnapshot(
				ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				Array.Empty<ToastNotificationScheduleRecord>(),
				NativeOperations: new[] { operation }),
			states.Add);

		lifecycle.OnSchedulesChanged();

		CollectionAssert.AreEqual(new[] { true }, states);
	}

	[TestMethod]
	[DataRow((int)ToastNotificationScheduleStatus.Active)]
	[DataRow((int)ToastNotificationScheduleStatus.Canceling)]
	[DataRow((int)ToastNotificationScheduleStatus.Delivering)]
	public void When_Any_Durable_Schedule_Exists_Receiver_Is_Enabled(int status)
	{
		var states = new List<bool>();
		var lifecycle = CreateLifecycle(new[] { Record("durable", (ToastNotificationScheduleStatus)status) }, states);

		lifecycle.OnSchedulesChanged();

		Assert.AreEqual(1, states.Count);
		Assert.IsTrue(states[0]);
	}

	[TestMethod]
	public void When_Last_Schedule_Is_Removed_Receiver_Is_Disabled()
	{
		IReadOnlyList<ToastNotificationScheduleRecord> records = new[] { Record("last") };
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(() => records, states.Add);

		lifecycle.OnSchedulesChanged();
		records = Array.Empty<ToastNotificationScheduleRecord>();
		lifecycle.OnSchedulesChanged();

		Assert.AreEqual(2, states.Count);
		Assert.IsTrue(states[0]);
		Assert.IsFalse(states[1]);
	}

	[TestMethod]
	public void When_Another_Schedule_Remains_Receiver_Stays_Enabled()
	{
		IReadOnlyList<ToastNotificationScheduleRecord> records = new[] { Record("removed"), Record("remaining") };
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(() => records, states.Add);

		lifecycle.OnSchedulesChanged();
		records = new[] { Record("remaining") };
		lifecycle.OnSchedulesChanged();

		Assert.AreEqual(2, states.Count);
		Assert.IsTrue(states[0]);
		Assert.IsTrue(states[1]);
	}

	[TestMethod]
	public void When_Persisted_Schedules_Change_Latest_State_Is_Used()
	{
		IReadOnlyList<ToastNotificationScheduleRecord> records = Array.Empty<ToastNotificationScheduleRecord>();
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(() => records, states.Add);

		lifecycle.OnSchedulesChanged();
		records = new[] { Record("added") };
		lifecycle.OnSchedulesChanged();

		Assert.AreEqual(2, states.Count);
		Assert.IsFalse(states[0]);
		Assert.IsTrue(states[1]);
	}

	[TestMethod]
	public void When_Schedules_Change_During_Component_Update_State_Is_Reconciled_Again()
	{
		IReadOnlyList<ToastNotificationScheduleRecord> records = new[] { Record("removed-concurrently") };
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(
			() => records,
			enabled =>
			{
				states.Add(enabled);
				records = Array.Empty<ToastNotificationScheduleRecord>();
			});

		lifecycle.OnSchedulesChanged();

		CollectionAssert.AreEqual(new[] { true, false }, states);
	}

	[TestMethod]
	public void When_Component_State_Update_Fails_Exception_Is_Propagated()
	{
		var expected = new InvalidOperationException("failed");
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(
			() => Array.Empty<ToastNotificationScheduleRecord>(),
			_ => throw expected);

		var actual = Assert.ThrowsExactly<InvalidOperationException>(
			lifecycle.OnSchedulesChanged);

		Assert.AreSame(expected, actual);
	}

	[TestMethod]
	public void When_Enabling_Component_Fails_Durable_Add_And_Component_State_Are_Rolled_Back()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(
			persistence,
			enabled =>
			{
				states.Add(enabled);
				if (states.Count == 1)
				{
					throw new InvalidOperationException("package manager failed");
				}
			});
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			backend,
			lifecycle);

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			scheduler.Add(Record("rollback"), DateTimeOffset.UtcNow));

		Assert.AreEqual(0, backend.ScheduleCount);
		Assert.AreEqual(0, persistence.Load().Records.Count);
		Assert.AreEqual(2, states.Count);
		Assert.IsTrue(states[0]);
		Assert.IsFalse(states[1]);
	}

	[TestMethod]
	public void When_Alarm_Registration_Fails_Durable_Retry_Keeps_Receiver_Enabled()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var states = new List<bool>();
		var lifecycle = new AndroidToastNotificationBootReceiverLifecycle(persistence, states.Add);
		var backend = new TestSchedulerBackend
		{
			ScheduleException = new InvalidOperationException("alarm failed"),
		};
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			backend,
			lifecycle);

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			scheduler.Add(Record("durable-retry"), DateTimeOffset.UtcNow));

		CollectionAssert.AreEqual(new[] { true }, states);
		Assert.AreEqual(1, persistence.Load().Records.Count);
		Assert.AreEqual(
			ToastNotificationNativeOperationKind.Retry,
			persistence.Load().NativeOperations!.Single().Kind);
	}

	[TestMethod]
	[DataRow(AndroidToastNotificationRecoveryActions.BootCompleted)]
	[DataRow(AndroidToastNotificationRecoveryActions.MyPackageReplaced)]
	public void When_Recovery_Broadcast_Is_Received_Schedules_Are_Reconciled(string action)
		=> Assert.IsTrue(AndroidToastNotificationRecoveryActions.ShouldRecover(action));

	[TestMethod]
	public void When_Unrelated_Broadcast_Is_Received_Schedules_Are_Not_Reconciled()
		=> Assert.IsFalse(AndroidToastNotificationRecoveryActions.ShouldRecover("android.intent.action.TIME_SET"));

	[TestMethod]
	public void When_Package_Is_Replaced_Durable_Alarm_Is_Registered_Again()
	{
		var record = Record("package-replaced");
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var states = new List<bool>();
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			backend,
			new AndroidToastNotificationBootReceiverLifecycle(persistence, states.Add));

		if (AndroidToastNotificationRecoveryActions.ShouldRecover(AndroidToastNotificationRecoveryActions.MyPackageReplaced))
		{
			scheduler.Recover(DateTimeOffset.UtcNow);
		}

		Assert.AreEqual(1, backend.ScheduleCount);
		Assert.AreEqual(1, states.Count);
		Assert.IsTrue(states[0]);
	}

	private static AndroidToastNotificationBootReceiverLifecycle CreateLifecycle(
		IReadOnlyList<ToastNotificationScheduleRecord> records,
		List<bool> states)
		=> new(
			() => records,
			states.Add);

	private static ToastNotificationScheduleRecord Record(
		string scheduleIdentifier,
		ToastNotificationScheduleStatus status = ToastNotificationScheduleStatus.Active)
		=> new(
			scheduleIdentifier,
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			DateTimeOffset.UtcNow.AddHours(1),
			null,
			string.Empty,
			string.Empty,
			string.Empty,
			false,
			null,
			0,
			status);

	private sealed class TestSchedulerBackend : IToastNotificationSchedulerBackend
	{
		public Exception? ScheduleException { get; init; }

		public int ScheduleCount { get; private set; }

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			if (ScheduleException is not null)
			{
				throw ScheduleException;
			}
			ScheduleCount++;
		}

		public void Cancel(string scheduleIdentifier)
		{
		}
	}
}
