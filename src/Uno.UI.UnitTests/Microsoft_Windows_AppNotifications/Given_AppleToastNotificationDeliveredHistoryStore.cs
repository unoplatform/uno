#nullable enable

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppleToastNotificationDeliveredHistoryStore
{
	[TestMethod]
	public void When_Delivered_History_Is_Persisted_Identity_And_Content_RoundTrip()
	{
		var directory = CreateDirectory();
		var record = Record("tag", "group");
		try
		{
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryPersist(record, directory));
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryPersist(record, directory));

			var restored = AppleToastNotificationDeliveredHistoryStore.GetAll(directory)!.Single();
			Assert.AreEqual(record.ScheduleIdentifier, restored.ScheduleIdentifier);
			Assert.AreEqual("tag", restored.Tag);
			Assert.AreEqual("group", restored.Group);
			Assert.AreEqual(record.Payload, restored.Payload);
			Assert.AreEqual(record.DeliveryTimeUtc, restored.DeliveryTimeUtc);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	public void When_Delivered_History_Is_Removed_Or_Cleaned_Only_Active_Records_Remain()
	{
		var directory = CreateDirectory();
		var retained = Record("retained", "group");
		var removed = Record("removed", "group");
		var stale = Record("stale", "other");
		try
		{
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryPersist(retained, directory));
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryPersist(removed, directory));
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryPersist(stale, directory));
			File.WriteAllText(Path.Combine(directory, "orphan.tmp"), "incomplete");

			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryRemove(removed.ScheduleIdentifier, directory));
			Assert.IsTrue(AppleToastNotificationDeliveredHistoryStore.TryCleanup(
				new[] { retained.ScheduleIdentifier },
				directory));

			CollectionAssert.AreEqual(
				new[] { retained.ScheduleIdentifier },
				AppleToastNotificationDeliveredHistoryStore.GetAll(directory)!
					.Select(record => record.ScheduleIdentifier)
					.ToArray());
			Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp").Length);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static ToastNotificationScheduleRecord Record(string tag, string group)
		=> new(
			Guid.NewGuid().ToString("N"),
			LegacyToastNotificationPayloadAdapter.Normalize(
				"<toast><visual><binding template='ToastText02'><text id='1'>Title</text><text id='2'>Body</text></binding></visual></toast>"),
			new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero),
			"id",
			tag,
			group,
			true,
			null,
			0);

	private static string CreateDirectory()
	{
		var directory = Path.Combine(AppContext.BaseDirectory, "AppleDeliveredHistoryTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return directory;
	}
}
