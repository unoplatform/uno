#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationDeliveryReliability
{
	private static readonly DateTimeOffset Now = new(2026, 8, 20, 15, 0, 0, TimeSpan.Zero);

	[TestCleanup]
	public void Cleanup() => ToastNotificationSchedulerRuntime.SetSchedulerForTests(null);

	[TestMethod]
	public void When_Begin_Delivery_Persistence_Fails_Claim_Is_Released()
	{
		var persistence = new FailOnceToastNotificationSchedulePersistence();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(persistence),
			new TestSchedulerBackend());
		var record = Record();
		scheduler.Add(record, Now);
		persistence.FailNextSave = true;

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.BeginDelivery(record.ScheduleIdentifier));

		var claim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.AreEqual(record.ScheduleIdentifier, claim?.Record.ScheduleIdentifier);
		scheduler.ReleaseDeliveryClaim(claim!, Now);
	}

	[TestMethod]
	public void When_Recovery_Fails_After_Delivery_Claim_Record_Is_Retried_And_Unwedged()
	{
		var backend = new TestSchedulerBackend();
		var lifecycle = new FailOnceReconcileLifecycle();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend,
			lifecycle);
		var record = Record();
		scheduler.Add(record, Now);
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var expected = new InvalidOperationException("recovery failed");
		lifecycle.Exception = expected;

		var actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
			ToastNotificationSchedulerRuntime.Deliver(
				record.ScheduleIdentifier,
				new AppNotificationManager((IAppNotificationManagerBackend?)null)));

		Assert.AreSame(expected, actual);
		Assert.AreEqual(2, backend.Scheduled.Count);
		var claim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.AreEqual(record.ScheduleIdentifier, claim?.Record.ScheduleIdentifier);
		scheduler.ReleaseDeliveryClaim(claim!, Now);
	}

	[TestMethod]
	public void When_Retry_Scheduling_Fails_Record_Remains_Claimable()
	{
		var backend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			backend);
		var record = Record();
		scheduler.Add(record, Now);
		var claim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.IsNotNull(claim);
		backend.ScheduleException = new InvalidOperationException("retry failed");

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.RetryDelivery(claim, Now));

		backend.ScheduleException = null;
		var retryClaim = scheduler.BeginDelivery(record.ScheduleIdentifier, Now);
		Assert.AreEqual(record.ScheduleIdentifier, retryClaim?.Record.ScheduleIdentifier);
		scheduler.ReleaseDeliveryClaim(retryClaim!, Now);
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

	private sealed class FailOnceToastNotificationSchedulePersistence : IToastNotificationSchedulePersistence
	{
		private readonly InMemoryToastNotificationSchedulePersistence _inner = new();

		public bool FailNextSave { get; set; }

		public ToastNotificationScheduleSnapshot Load() => _inner.Load();

		public void Save(ToastNotificationScheduleSnapshot state)
		{
			if (FailNextSave)
			{
				FailNextSave = false;
				throw new InvalidOperationException("persistence failed");
			}
			_inner.Save(state);
		}
	}

	private sealed class FailOnceReconcileLifecycle : IToastNotificationScheduleLifecycle
	{
		public Exception? Exception { get; set; }

		public void OnSchedulesChanged()
		{
		}

		public void Reconcile()
		{
			if (Exception is { } exception)
			{
				Exception = null;
				throw exception;
			}
		}
	}

	private sealed class TestSchedulerBackend : IToastNotificationSchedulerBackend
	{
		public Exception? ScheduleException { get; set; }

		public List<ToastNotificationScheduleRecord> Scheduled { get; } = new();

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			if (ScheduleException is not null)
			{
				throw ScheduleException;
			}
			Scheduled.Add(record);
		}

		public void Cancel(string scheduleIdentifier)
		{
		}
	}
}
