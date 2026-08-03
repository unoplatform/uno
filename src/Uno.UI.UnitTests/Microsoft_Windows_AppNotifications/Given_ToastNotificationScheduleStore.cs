#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_ToastNotificationScheduleStore
{
	private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Record_Is_Added_It_RoundTrips_And_Is_Ordered_By_Delivery()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var store = new ToastNotificationScheduleStore(persistence);
		var later = Record("later", Now.AddHours(2));
		var earlier = Record("earlier", Now.AddHours(1));

		store.Add(later, Now);
		store.Add(earlier, Now);

		CollectionAssert.AreEqual(new[] { earlier.ScheduleIdentifier, later.ScheduleIdentifier }, store.GetAll().Select(record => record.ScheduleIdentifier).ToArray());
		Assert.AreEqual(earlier, new ToastNotificationScheduleStore(persistence).Get(earlier.ScheduleIdentifier));
	}

	[TestMethod]
	public void When_Delivery_Is_Not_In_The_Future_Add_Is_Rejected_Without_State_Change()
	{
		var store = new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence());

		var exception = Assert.ThrowsExactly<COMException>(() => store.Add(Record("past", Now), Now));

		Assert.AreEqual(unchecked((int)0x80070718), exception.HResult);
		Assert.AreEqual(0, store.GetAll().Count);
	}

	[TestMethod]
	public void When_Schedule_Identifier_Is_Duplicated_Add_Is_Rejected()
	{
		var store = new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence());
		store.Add(Record("duplicate", Now.AddHours(1)), Now);

		Assert.ThrowsExactly<InvalidOperationException>(() => store.Add(Record("duplicate", Now.AddHours(2)), Now));
	}

	[TestMethod]
	public void When_Quota_Is_Reached_Add_Uses_Windows_Quota_HResult()
	{
		var records = Enumerable.Range(0, ToastNotificationScheduleStore.MaximumScheduledNotifications)
			.Select(index => Record(index.ToString(global::System.Globalization.CultureInfo.InvariantCulture), Now.AddHours(1)))
			.ToArray();
		var store = new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence(
			new ToastNotificationScheduleSnapshot(ToastNotificationScheduleSnapshot.CurrentSchemaVersion, records)));

		var exception = Assert.ThrowsExactly<COMException>(() => store.Add(Record("overflow", Now.AddHours(1)), Now));

		Assert.AreEqual(unchecked((int)0x80070718), exception.HResult);
	}

	[TestMethod]
	public void When_Record_Is_Removed_Only_Matching_Schedule_Is_Deleted()
	{
		var store = new ToastNotificationScheduleStore(new InMemoryToastNotificationSchedulePersistence());
		var first = Record("first", Now.AddHours(1));
		var second = Record("second", Now.AddHours(2));
		store.Add(first, Now);
		store.Add(second, Now);

		var removed = store.Remove(first.ScheduleIdentifier);

		Assert.AreEqual(first, removed);
		CollectionAssert.AreEqual(new[] { second }, store.GetAll().ToArray());
	}

	[TestMethod]
	public void When_File_State_Is_Saved_All_Schedule_Fields_RoundTrip()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var persistence = new FileToastNotificationSchedulePersistence(path);
			var record = Record(Guid.NewGuid().ToString("N"), Now.AddHours(1)) with
			{
				ExpirationTimeUtc = Now.AddHours(2),
				Id = "id",
				Tag = "tag",
				Group = "group",
				SuppressPopup = true,
				SnoozeInterval = TimeSpan.FromMinutes(5),
				MaximumSnoozeCount = 3,
				Status = ToastNotificationScheduleStatus.Canceling,
			};

			persistence.Save(new ToastNotificationScheduleSnapshot(ToastNotificationScheduleSnapshot.CurrentSchemaVersion, new[] { record }));

			Assert.AreEqual(record, persistence.Load().Records.Single());
		}
		finally
		{
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}

	[TestMethod]
	public void When_File_State_Is_Corrupt_It_Is_Quarantined_And_Empty_State_Is_Returned()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			Directory.CreateDirectory(folder);
			File.WriteAllText(path, "not schedule state");

			var loaded = new FileToastNotificationSchedulePersistence(path).Load();

			Assert.AreEqual(0, loaded.Records.Count);
			Assert.IsFalse(File.Exists(path));
			Assert.AreEqual(1, Directory.GetFiles(folder, "schedules.bin.corrupt.*").Length);
		}
		finally
		{
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}

	private static ToastNotificationScheduleRecord Record(string identifier, DateTimeOffset deliveryTime)
		=> new(
			Guid.TryParseExact(identifier, "N", out _) ? identifier : CreateIdentifier(identifier),
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			deliveryTime,
			null,
			string.Empty,
			string.Empty,
			string.Empty,
			false,
			null,
			1);

	private static string CreateIdentifier(string value)
	{
		var bytes = new byte[16];
		var source = global::System.Text.Encoding.UTF8.GetBytes(value);
		Array.Copy(source, bytes, Math.Min(source.Length, bytes.Length));
		return new Guid(bytes).ToString("N");
	}
}
