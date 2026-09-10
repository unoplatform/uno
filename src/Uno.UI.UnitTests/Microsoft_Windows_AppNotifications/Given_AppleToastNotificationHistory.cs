#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleToastNotificationHistory
{
	private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

	[TestCleanup]
	public void Cleanup() => ToastNotificationSchedulerRuntime.SetSchedulerForTests(null);

	[TestMethod]
	public void When_Stopped_App_Delivery_Is_Recovered_History_RoundTrips_Once()
	{
		var first = Record("first", "group");
		var second = Record("second", "other");
		var backend = InitializeScheduler(first, second);
		var history = new ToastNotificationHistory(new AppNotificationManager(new TestAppNotificationBackend()));

		var initial = history.GetHistory();
		var repeated = history.GetHistory();

		Assert.AreEqual(2, initial.Count);
		CollectionAssert.AreEquivalent(new[] { "first", "second" }, initial.Select(toast => toast.Tag).ToArray());
		Assert.IsTrue(initial.Any(toast => toast.Group == "group" && toast.Content.GetXml().Contains("Title", StringComparison.Ordinal)));
		Assert.AreEqual(2, repeated.Count);
		Assert.AreEqual(2, backend.PersistHistoryCount);
		Assert.AreEqual(0, backend.Receipts.Count);
	}

	[TestMethod]
	public void When_Stopped_App_Delivery_Is_In_History_Remove_And_Clear_Remove_Native_Schedules()
	{
		var first = Record("first", "group");
		var second = Record("second", "group");
		var third = Record("third", "other");
		var backend = InitializeScheduler(first, second, third);
		var history = new ToastNotificationHistory(new AppNotificationManager(new TestAppNotificationBackend()));
		Assert.AreEqual(3, history.GetHistory().Count);

		history.Remove("first", "group");
		history.RemoveGroup("group");
		history.Clear();

		CollectionAssert.AreEquivalent(
			new[] { first.ScheduleIdentifier, second.ScheduleIdentifier, third.ScheduleIdentifier },
			backend.Canceled.ToArray());
		Assert.AreEqual(0, history.GetHistory().Count);
		Assert.AreEqual(0, backend.History.Count);
	}

	private static TestAppleNativeSchedulerBackend InitializeScheduler(params ToastNotificationScheduleRecord[] records)
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			records));
		var backend = new TestAppleNativeSchedulerBackend(records.Select(record => record.ScheduleIdentifier));
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(
			new ToastNotificationScheduler(new ToastNotificationScheduleStore(persistence), backend));
		return backend;
	}

	private static ToastNotificationScheduleRecord Record(string tag, string group)
		=> new(
			Guid.NewGuid().ToString("N"),
			LegacyToastNotificationPayloadAdapter.Normalize(
				"<toast><visual><binding template='ToastText02'><text id='1'>Title</text><text id='2'>Body</text></binding></visual></toast>"),
			Now.AddMinutes(-1),
			Now.AddHours(1),
			string.Empty,
			tag,
			group,
			false,
			null,
			0);

	private sealed class TestAppleNativeSchedulerBackend : IToastNotificationSchedulerBackend, INativeToastNotificationSchedulerBackend
	{
		private readonly HashSet<string> _delivered;

		public TestAppleNativeSchedulerBackend(IEnumerable<string> delivered)
		{
			_delivered = delivered.ToHashSet(StringComparer.Ordinal);
		}

		public HashSet<string> Receipts { get; } = new(StringComparer.Ordinal);

		public Dictionary<string, ToastNotificationScheduleRecord> History { get; } = new(StringComparer.Ordinal);

		public List<string> Canceled { get; } = new();

		public int PersistHistoryCount { get; private set; }

		public void Schedule(ToastNotificationScheduleRecord record)
			=> throw new AssertFailedException("Delivered records must not be rescheduled.");

		public void Cancel(string scheduleIdentifier)
		{
			Canceled.Add(scheduleIdentifier);
			_delivered.Remove(scheduleIdentifier);
		}

		public IReadOnlyCollection<string>? GetPendingScheduleIdentifiers() => Array.Empty<string>();

		public IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers() => _delivered.ToArray();

		public IReadOnlyCollection<string>? GetDeliveryReceiptIdentifiers() => Receipts;

		public bool TryPersistDeliveryReceipt(string scheduleIdentifier)
		{
			Receipts.Add(scheduleIdentifier);
			return true;
		}

		public void ConsumeDeliveryReceipt(string scheduleIdentifier) => Receipts.Remove(scheduleIdentifier);

		public void CleanupDeliveryReceipts(IReadOnlyCollection<string> retainedScheduleIdentifiers)
			=> Receipts.IntersectWith(retainedScheduleIdentifiers);

		public bool TryPersistDeliveredHistory(ToastNotificationScheduleRecord record)
		{
			PersistHistoryCount++;
			History.TryAdd(record.ScheduleIdentifier, record);
			return true;
		}

		public IReadOnlyCollection<ToastNotificationScheduleRecord>? GetDeliveredHistory() => History.Values.ToArray();

		public bool TryRemoveDeliveredHistory(string scheduleIdentifier) => History.Remove(scheduleIdentifier);

		public bool TryCleanupDeliveredHistory(IReadOnlyCollection<string> activeScheduleIdentifiers)
		{
			History.Keys
				.Where(identifier => !activeScheduleIdentifiers.Contains(identifier))
				.ToList()
				.ForEach(identifier => History.Remove(identifier));
			return true;
		}
	}

	private sealed class TestAppNotificationBackend : IAppNotificationManagerBackend
	{
		public bool IsSupported => true;

		public AppNotificationSetting Setting => AppNotificationSetting.Enabled;

		public string? BootIdentifier => null;

		public void Register()
		{
		}

		public void Register(string displayName, Uri iconUri)
		{
		}

		public void Unregister()
		{
		}

		public void UnregisterAll()
		{
		}

		public bool TryShow(AppNotificationEnvelope notification) => true;

		public bool TryUpdate(AppNotificationStateRecord notification) => true;

		public void Remove(AppNotificationStateRecord notification)
		{
		}

		public void RemoveAll()
		{
		}

		public IReadOnlyCollection<uint>? GetActiveNotificationIds() => null;
	}
}
