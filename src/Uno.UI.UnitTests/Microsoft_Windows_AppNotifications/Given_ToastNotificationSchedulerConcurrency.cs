#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationSchedulerConcurrency
{
	private static readonly DateTimeOffset Now = new(2026, 8, 20, 14, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Registration_Fails_Durable_Retry_Intent_Is_Recovered()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var backend = new StatefulSchedulerBackend
		{
			ScheduleException = new InvalidOperationException("registration failed"),
		};
		var scheduler = CreateScheduler(persistence, backend);
		var record = Record("registration-retry");

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Add(record, Now));

		var failed = persistence.Load();
		Assert.AreEqual(1, failed.Records.Count);
		Assert.AreEqual(
			ToastNotificationNativeOperationKind.Retry,
			failed.NativeOperations!.Single().Kind);

		backend.ScheduleException = null;
		Assert.IsTrue(CreateScheduler(persistence, backend).Recover(Now));

		var recovered = persistence.Load();
		Assert.AreEqual(1, recovered.Records.Count);
		Assert.AreEqual(0, recovered.NativeOperations!.Count);
		Assert.IsTrue(backend.IsScheduled(record.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Schedule_Succeeds_But_Completion_Persistence_Fails_Retry_Intent_Remains()
	{
		var persistence = new FailOnSaveSchedulePersistence { FailOnSaveCall = 2 };
		var backend = new StatefulSchedulerBackend();
		var scheduler = CreateScheduler(persistence, backend);
		var record = Record("schedule-completion-failure");

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Add(record, Now));

		var failed = persistence.Load();
		Assert.IsTrue(backend.IsScheduled(record.ScheduleIdentifier));
		Assert.AreEqual(
			ToastNotificationNativeOperationKind.Retry,
			failed.NativeOperations!.Single().Kind);

		Assert.IsTrue(CreateScheduler(persistence, backend).Recover(Now));
		Assert.AreEqual(0, persistence.Load().NativeOperations!.Count);
		Assert.IsTrue(backend.IsScheduled(record.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Cancel_Succeeds_But_Completion_Persistence_Fails_Cancel_Intent_Remains()
	{
		var persistence = new FailOnSaveSchedulePersistence { FailOnSaveCall = 4 };
		var backend = new StatefulSchedulerBackend();
		var scheduler = CreateScheduler(persistence, backend);
		var record = Record("cancel-completion-failure");
		scheduler.Add(record, Now);

		Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Remove(record.ScheduleIdentifier));

		var failed = persistence.Load();
		Assert.IsFalse(backend.IsScheduled(record.ScheduleIdentifier));
		Assert.AreEqual(
			ToastNotificationNativeOperationKind.Cancel,
			failed.NativeOperations!.Single().Kind);

		Assert.IsTrue(CreateScheduler(persistence, backend).Recover(Now));
		Assert.AreEqual(0, persistence.Load().NativeOperations!.Count);
		Assert.IsFalse(backend.IsScheduled(record.ScheduleIdentifier));
	}

	[TestMethod]
	public async Task When_Remove_Races_Blocked_Add_Stale_Schedule_Is_Compensated()
	{
		var folder = CreateDirectory();
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var backend = new StatefulSchedulerBackend();
			var first = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var second = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var record = Record("add-remove-race");
			backend.BlockNextSchedule();

			var add = Task.Run(() => first.Add(record, Now));
			Assert.IsTrue(backend.WaitForBlockedSchedule());
			second.Remove(record.ScheduleIdentifier);
			backend.ReleaseBlockedSchedule();
			await add;

			var state = new FileToastNotificationSchedulePersistence(path).Load();
			Assert.AreEqual(0, state.Records.Count);
			Assert.AreEqual(0, state.NativeOperations!.Count);
			Assert.IsFalse(backend.IsScheduled(record.ScheduleIdentifier));
			Assert.IsTrue(backend.CancelCount >= 2);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[TestMethod]
	public async Task When_Remove_Races_Blocked_Retry_Stale_Alarm_Is_Compensated()
	{
		var folder = CreateDirectory();
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var backend = new StatefulSchedulerBackend();
			var first = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var second = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var record = Record("retry-remove-race");
			first.Add(record, Now);
			var claim = first.BeginDelivery(record.ScheduleIdentifier, Now);
			Assert.IsNotNull(claim);
			backend.BlockNextSchedule();

			var retry = Task.Run(() => first.RetryDelivery(claim, Now));
			Assert.IsTrue(backend.WaitForBlockedSchedule());
			second.Remove(record.ScheduleIdentifier);
			backend.ReleaseBlockedSchedule();
			await retry;

			var state = new FileToastNotificationSchedulePersistence(path).Load();
			Assert.AreEqual(0, state.Records.Count);
			Assert.AreEqual(0, state.NativeOperations!.Count);
			Assert.IsFalse(backend.IsScheduled(record.ScheduleIdentifier));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[TestMethod]
	public async Task When_Readd_Races_Blocked_Cancel_Newer_Schedule_Is_Restored()
	{
		var folder = CreateDirectory();
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var backend = new StatefulSchedulerBackend();
			var first = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var second = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var record = Record("cancel-readd-race");
			first.Add(record, Now);
			backend.BlockNextCancel();

			var remove = Task.Run(() => first.Remove(record.ScheduleIdentifier));
			Assert.IsTrue(backend.WaitForBlockedCancel());
			second.Add(record with { DeliveryTimeUtc = Now.AddHours(2) }, Now);
			backend.ReleaseBlockedCancel();
			await remove;

			var state = new FileToastNotificationSchedulePersistence(path).Load();
			Assert.AreEqual(1, state.Records.Count);
			Assert.AreEqual(Now.AddHours(2), state.Records.Single().DeliveryTimeUtc);
			Assert.AreEqual(0, state.NativeOperations!.Count);
			Assert.IsTrue(backend.IsScheduled(record.ScheduleIdentifier));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[TestMethod]
	public void When_Foreign_Delivery_Claim_Is_Live_It_Cannot_Be_Stolen()
	{
		var folder = CreateDirectory();
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var backend = new StatefulSchedulerBackend();
			var first = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var second = CreateScheduler(new FileToastNotificationSchedulePersistence(path), backend);
			var record = Record("foreign-claim");
			first.Add(record, Now);

			var firstClaim = first.BeginDelivery(record.ScheduleIdentifier, Now);
			var blockedClaim = second.BeginDelivery(record.ScheduleIdentifier, Now.AddMinutes(1));
			var replacementClaim = second.BeginDelivery(record.ScheduleIdentifier, Now.AddMinutes(6));

			Assert.IsNotNull(firstClaim);
			Assert.IsNull(blockedClaim);
			Assert.IsNotNull(replacementClaim);
			Assert.AreNotEqual(firstClaim.Token, replacementClaim.Token);
			Assert.IsTrue(replacementClaim.Revision > firstClaim.Revision);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	private static ToastNotificationScheduler CreateScheduler(
		IToastNotificationSchedulePersistence persistence,
		IToastNotificationSchedulerBackend backend)
		=> new(new ToastNotificationScheduleStore(persistence), backend);

	private static ToastNotificationScheduleRecord Record(string value)
		=> new(
			CreateIdentifier(value),
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			Now.AddHours(1),
			null,
			string.Empty,
			string.Empty,
			string.Empty,
			false,
			null,
			0);

	private static string CreateIdentifier(string value)
	{
		var bytes = new byte[16];
		var source = global::System.Text.Encoding.UTF8.GetBytes(value);
		Array.Copy(source, bytes, Math.Min(source.Length, bytes.Length));
		return new Guid(bytes).ToString("N");
	}

	private static string CreateDirectory()
	{
		var directory = Path.Combine(
			AppContext.BaseDirectory,
			nameof(Given_ToastNotificationSchedulerConcurrency),
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return directory;
	}

	private sealed class StatefulSchedulerBackend : IToastNotificationSchedulerBackend
	{
		private readonly object _gate = new();
		private readonly HashSet<string> _scheduled = new(StringComparer.Ordinal);
		private readonly ManualResetEventSlim _scheduleEntered = new();
		private readonly ManualResetEventSlim _continueSchedule = new();
		private readonly ManualResetEventSlim _cancelEntered = new();
		private readonly ManualResetEventSlim _continueCancel = new();
		private int _blockNextSchedule;
		private int _blockNextCancel;

		public Exception? ScheduleException { get; set; }

		public int CancelCount { get; private set; }

		public void BlockNextSchedule()
		{
			_scheduleEntered.Reset();
			_continueSchedule.Reset();
			Interlocked.Exchange(ref _blockNextSchedule, 1);
		}

		public bool WaitForBlockedSchedule() => _scheduleEntered.Wait(TimeSpan.FromSeconds(10));

		public void ReleaseBlockedSchedule() => _continueSchedule.Set();

		public void BlockNextCancel()
		{
			_cancelEntered.Reset();
			_continueCancel.Reset();
			Interlocked.Exchange(ref _blockNextCancel, 1);
		}

		public bool WaitForBlockedCancel() => _cancelEntered.Wait(TimeSpan.FromSeconds(10));

		public void ReleaseBlockedCancel() => _continueCancel.Set();

		public bool IsScheduled(string scheduleIdentifier)
		{
			lock (_gate)
			{
				return _scheduled.Contains(scheduleIdentifier);
			}
		}

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			if (ScheduleException is not null)
			{
				throw ScheduleException;
			}
			if (Interlocked.Exchange(ref _blockNextSchedule, 0) == 1)
			{
				_scheduleEntered.Set();
				if (!_continueSchedule.Wait(TimeSpan.FromSeconds(10)))
				{
					throw new TimeoutException("Blocked schedule was not released.");
				}
			}
			lock (_gate)
			{
				_scheduled.Add(record.ScheduleIdentifier);
			}
		}

		public void Cancel(string scheduleIdentifier)
		{
			if (Interlocked.Exchange(ref _blockNextCancel, 0) == 1)
			{
				_cancelEntered.Set();
				if (!_continueCancel.Wait(TimeSpan.FromSeconds(10)))
				{
					throw new TimeoutException("Blocked cancellation was not released.");
				}
			}
			lock (_gate)
			{
				CancelCount++;
				_scheduled.Remove(scheduleIdentifier);
			}
		}
	}

	private sealed class FailOnSaveSchedulePersistence : IToastNotificationSchedulePersistence
	{
		private readonly InMemoryToastNotificationSchedulePersistence _inner = new();
		private int _saveCount;

		public int FailOnSaveCall { get; set; }

		public ToastNotificationScheduleSnapshot Load() => _inner.Load();

		public void Save(ToastNotificationScheduleSnapshot state)
		{
			_saveCount++;
			if (_saveCount == FailOnSaveCall)
			{
				throw new InvalidOperationException("persistence failed");
			}
			_inner.Save(state);
		}
	}
}
