#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ScheduledToastNotification
{
	[TestCleanup]
	public void Cleanup() => ToastNotificationSchedulerRuntime.SetSchedulerForTests(null);

	[TestMethod]
	public void When_Scheduled_Toast_Is_Constructed_Properties_Are_Validated()
	{
		var content = CreateContent();
		Assert.ThrowsExactly<ArgumentException>(() => new ScheduledToastNotification(null!, DateTimeOffset.UtcNow));
		Assert.ThrowsExactly<ArgumentException>(() => new ScheduledToastNotification(content, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(59), 1));
		Assert.ThrowsExactly<ArgumentException>(() => new ScheduledToastNotification(content, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), 0));

		var scheduled = new ScheduledToastNotification(content, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(5), 3);
		scheduled.Id = "identifier";
		scheduled.Tag = "tag";
		scheduled.Group = "group";
		Assert.AreEqual(TimeSpan.FromMinutes(5), scheduled.SnoozeInterval);
		Assert.AreEqual(3u, scheduled.MaximumSnoozeCount);
		Assert.ThrowsExactly<ArgumentException>(() => scheduled.Id = new string('i', 17));
		scheduled.Tag = string.Empty;
		Assert.AreEqual(string.Empty, scheduled.Tag);
		Assert.ThrowsExactly<ArgumentException>(() => scheduled.Group = new string('g', 65));
	}

	[TestMethod]
	public void When_Scheduled_Toast_Is_Converted_Record_RoundTrips_Legacy_Content()
	{
		var delivery = DateTimeOffset.UtcNow.AddHours(1);
		var scheduled = new ScheduledToastNotification(CreateContent(), delivery)
		{
			Id = "id",
			Tag = "tag",
			Group = "group",
			ExpirationTime = delivery.AddHours(1),
			SuppressPopup = true,
		};

		var record = ToastNotificationSchedulerRuntime.ToRecord(scheduled);
		var restored = ToastNotificationSchedulerRuntime.FromRecord(record);

		Assert.AreEqual(scheduled.ScheduleIdentifier, restored.ScheduleIdentifier);
		Assert.AreEqual("id", restored.Id);
		Assert.AreEqual("tag", restored.Tag);
		Assert.AreEqual("group", restored.Group);
		Assert.IsTrue(restored.SuppressPopup);
		StringAssert.Contains(restored.Content.GetXml(), "ToastText02");
	}

	[TestMethod]
	public void When_Scheduled_Mirroring_Is_Converted_Internal_Value_RoundTrips()
	{
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddHours(1));
		scheduled.SchedulingNotificationMirroring = NotificationMirroring.Disabled;

		var restored = ToastNotificationSchedulerRuntime.FromRecord(ToastNotificationSchedulerRuntime.ToRecord(scheduled));

		Assert.AreEqual(NotificationMirroring.Disabled, restored.SchedulingNotificationMirroring);
	}

	[TestMethod]
	public void When_Alarm_Delivers_Record_It_Is_Consumed_And_Posted_Once()
	{
		var schedulerBackend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			schedulerBackend);
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddHours(1)) { Tag = "tag", Group = "group" };
		scheduler.Add(ToastNotificationSchedulerRuntime.ToRecord(scheduled), DateTimeOffset.UtcNow);
		var appBackend = new TestAppNotificationBackend();
		var manager = new AppNotificationManager(appBackend, new InMemoryAppNotificationStatePersistence());

		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, manager);
		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, manager);

		Assert.AreEqual(1, appBackend.Shown.Count);
		Assert.AreEqual(0, scheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Cold_Alarm_Delivers_Fired_Record_Is_Not_Reregistered_During_Recovery()
	{
		var schedulerBackend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			schedulerBackend);
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddMinutes(1));
		scheduler.Add(ToastNotificationSchedulerRuntime.ToRecord(scheduled), DateTimeOffset.UtcNow);
		schedulerBackend.Scheduled.Clear();
		var appBackend = new TestAppNotificationBackend();

		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, new AppNotificationManager(appBackend));

		Assert.AreEqual(0, schedulerBackend.Scheduled.Count);
		Assert.AreEqual(1, appBackend.Shown.Count);
	}

	[TestMethod]
	public void When_Delivery_Retries_After_Success_Correlation_Prevents_Duplicate_Native_Post()
	{
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			new TestSchedulerBackend());
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddMinutes(1));
		var record = ToastNotificationSchedulerRuntime.ToRecord(scheduled);
		scheduler.Add(record, DateTimeOffset.UtcNow);
		var appPersistence = new InMemoryAppNotificationStatePersistence();
		var appBackend = new TestAppNotificationBackend();
		var manager = new AppNotificationManager(appBackend, appPersistence);

		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, manager);
		var retryScheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
				ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				new[] { record with { Status = ToastNotificationScheduleStatus.Delivering } }))),
			new TestSchedulerBackend());
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(retryScheduler);
		retryScheduler.Recover(DateTimeOffset.UtcNow);
		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, new AppNotificationManager(appBackend, appPersistence));

		Assert.AreEqual(1, appBackend.Shown.Count);
		Assert.AreEqual(0, retryScheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Alarm_Record_Is_Expired_It_Is_Consumed_Without_Posting()
	{
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			new TestSchedulerBackend());
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddHours(1))
		{
			Tag = "tag",
			ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		scheduler.Add(ToastNotificationSchedulerRuntime.ToRecord(scheduled), DateTimeOffset.UtcNow);
		var appBackend = new TestAppNotificationBackend();

		ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, new AppNotificationManager(appBackend));

		Assert.AreEqual(0, appBackend.Shown.Count);
		Assert.AreEqual(0, scheduler.GetAll().Count);
	}

	[TestMethod]
	public void When_Alarm_Post_Throws_Delivery_Is_Reregistered_For_Retry()
	{
		var schedulerBackend = new TestSchedulerBackend();
		var scheduler = new ToastNotificationScheduler(
			new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence()),
			schedulerBackend);
		ToastNotificationSchedulerRuntime.SetSchedulerForTests(scheduler);
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddMinutes(1));
		scheduler.Add(ToastNotificationSchedulerRuntime.ToRecord(scheduled), DateTimeOffset.UtcNow);
		var appBackend = new TestAppNotificationBackend { ShowException = new InvalidOperationException("failed") };

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			ToastNotificationSchedulerRuntime.Deliver(scheduled.ScheduleIdentifier, new AppNotificationManager(appBackend)));

		Assert.AreEqual(2, schedulerBackend.Scheduled.Count);
		Assert.AreEqual(scheduled.ScheduleIdentifier, scheduler.BeginDelivery(scheduled.ScheduleIdentifier)?.ScheduleIdentifier);
	}

	[TestMethod]
	public void When_Runtime_Initializes_Persisted_Records_Are_Reconciled()
	{
		var scheduled = new ScheduledToastNotification(CreateContent(), DateTimeOffset.UtcNow.AddHours(1));
		var record = ToastNotificationSchedulerRuntime.ToRecord(scheduled);
		var persistence = new InMemoryToastNotificationSchedulePersistence(new ToastNotificationScheduleSnapshot(
			ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			new[] { record }));
		var backend = new TestSchedulerBackend();

		ToastNotificationSchedulerRuntime.InitializeForTests(persistence, backend, DateTimeOffset.UtcNow);

		CollectionAssert.AreEqual(new[] { record.ScheduleIdentifier }, backend.Scheduled.ToArray());
	}

	[TestMethod]
	public void When_Scheduling_Surface_Is_Inspected_NonAndroid_Host_Remains_Unsupported()
	{
		var schedule = typeof(ToastNotifier).GetMethod(nameof(ToastNotifier.AddToSchedule))!;
		var getScheduled = typeof(ToastNotifier).GetMethod(nameof(ToastNotifier.GetScheduledToastNotifications))!;

		Assert.IsTrue(HasNotImplementedAttribute(schedule));
		Assert.IsTrue(HasNotImplementedAttribute(getScheduled));
	}

	private static XmlDocument CreateContent()
	{
		var content = new XmlDocument();
		content.LoadXml("<toast><visual><binding template='ToastText02'><text id='1'>Title</text><text id='2'>Body</text></binding></visual></toast>");
		return content;
	}

	private static bool HasNotImplementedAttribute(MemberInfo member)
		=> member.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "Uno.NotImplementedAttribute");

	private sealed class TestSchedulerBackend : IToastNotificationSchedulerBackend
	{
		public List<string> Scheduled { get; } = new();

		public void Schedule(ToastNotificationScheduleRecord record)
		{
			Scheduled.Add(record.ScheduleIdentifier);
		}

		public void Cancel(string scheduleIdentifier)
		{
		}
	}

	private sealed class TestAppNotificationBackend : IAppNotificationManagerBackend
	{
		public bool IsSupported => true;

		public AppNotificationSetting Setting => AppNotificationSetting.Enabled;

		public string? BootIdentifier => "boot";

		public List<AppNotificationEnvelope> Shown { get; } = new();

		public Exception? ShowException { get; set; }

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

		public bool TryShow(AppNotificationEnvelope notification)
		{
			if (ShowException is not null)
			{
				throw ShowException;
			}
			Shown.Add(notification);
			return true;
		}

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
