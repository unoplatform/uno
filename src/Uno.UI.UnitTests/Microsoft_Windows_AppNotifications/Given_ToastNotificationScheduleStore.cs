#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		var restored = new ToastNotificationScheduleStore(persistence).Get(earlier.ScheduleIdentifier);
		Assert.IsNotNull(restored);
		Assert.AreEqual(earlier, restored with { Revision = 0 });
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

		var operation = store.RequestRemove(first.ScheduleIdentifier);

		Assert.AreEqual(ToastNotificationNativeOperationKind.Cancel, operation?.Kind);
		CollectionAssert.AreEqual(
			new[] { second.ScheduleIdentifier },
			store.GetAll().Select(record => record.ScheduleIdentifier).ToArray());
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
	public void When_File_State_Contains_Claim_And_Native_Intent_All_Fields_RoundTrip()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var persistence = new FileToastNotificationSchedulePersistence(path);
			var record = Record(Guid.NewGuid().ToString("N"), Now.AddHours(1)) with
			{
				Status = ToastNotificationScheduleStatus.Delivering,
				Revision = 4,
				DeliveryClaimOwner = Guid.NewGuid().ToString("N"),
				DeliveryClaimToken = Guid.NewGuid().ToString("N"),
				DeliveryClaimExpirationUtc = Now.AddMinutes(5),
			};
			var operation = new ToastNotificationNativeOperation(
				Guid.NewGuid().ToString("N"),
				ToastNotificationNativeOperationKind.Cancel,
				Guid.NewGuid().ToString("N"),
				Revision: 4);
			var state = new ToastNotificationScheduleSnapshot(
				ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
				new[] { record },
				4,
				new[] { operation });

			persistence.Save(state);

			var restored = persistence.Load();
			Assert.AreEqual(record, restored.Records.Single());
			Assert.AreEqual(operation, restored.NativeOperations!.Single());
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

	[TestMethod]
	public void When_Version_One_State_Is_Mutated_It_Is_Upgraded_Without_Losing_Records()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var legacy = Record("legacy", Now.AddHours(1));
			WriteVersionOneSnapshot(path, legacy);
			var persistence = new FileToastNotificationSchedulePersistence(path);
			var store = new ToastNotificationScheduleStore(persistence);

			store.Add(Record("new", Now.AddHours(2)), Now);

			var loaded = persistence.Load();
			Assert.AreEqual(ToastNotificationScheduleSnapshot.CurrentSchemaVersion, loaded.SchemaVersion);
			Assert.AreEqual(2, loaded.Records.Count);
			Assert.IsTrue(loaded.Records.Any(record => record.ScheduleIdentifier == legacy.ScheduleIdentifier));
			Assert.IsTrue(loaded.Revision > 0);
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
	public void When_Version_Two_State_Is_Mutated_It_Adds_The_Native_Operation_Journal()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var legacy = Record("version-two", Now.AddHours(1)) with { Revision = 3 };
			WriteVersionTwoSnapshot(path, legacy, snapshotRevision: 3);
			var persistence = new FileToastNotificationSchedulePersistence(path);
			var store = new ToastNotificationScheduleStore(persistence);

			store.RequestSchedule(
				legacy.ScheduleIdentifier,
				ToastNotificationNativeOperationKind.Schedule);

			var loaded = persistence.Load();
			Assert.AreEqual(ToastNotificationScheduleSnapshot.CurrentSchemaVersion, loaded.SchemaVersion);
			Assert.AreEqual(legacy.ScheduleIdentifier, loaded.Records.Single().ScheduleIdentifier);
			Assert.AreEqual(
				ToastNotificationNativeOperationKind.Schedule,
				loaded.NativeOperations!.Single().Kind);
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
	public async Task When_Processes_Add_Concurrent_Records_All_Are_Preserved()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			const int count = 16;
			var records = Enumerable.Range(0, count)
				.Select(index => Record($"concurrent-{index}", Now.AddHours(1)))
				.ToArray();
			var stores = records
				.Select(_ => new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path)))
				.ToArray();
			using var start = new ManualResetEventSlim();
			var tasks = stores.Select((store, index) => Task.Run(() =>
			{
				start.Wait();
				store.Add(records[index], Now);
			})).ToArray();

			start.Set();
			await Task.WhenAll(tasks);

			var loaded = new FileToastNotificationSchedulePersistence(path).Load();
			Assert.AreEqual(count, loaded.Records.Count);
			Assert.AreEqual(count, loaded.Records.Select(record => record.ScheduleIdentifier).Distinct(StringComparer.Ordinal).Count());
			Assert.AreEqual(count, loaded.Records.Select(record => record.Revision).Distinct().Count());
			Assert.AreEqual(count, loaded.Revision);
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
	public void When_Processes_Update_Different_Records_Per_Record_Merge_Preserves_Both()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var first = Record("merge-first", Now.AddHours(1));
			var second = Record("merge-second", Now.AddHours(2));
			var seed = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			seed.Add(first, Now);
			seed.Add(second, Now);
			var firstProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			var secondProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));

			firstProcess.RequestRemove(first.ScheduleIdentifier);
			secondProcess.TryClaimDelivery(
				second.ScheduleIdentifier,
				"second-process",
				Now,
				Now.AddMinutes(5));

			var loaded = new FileToastNotificationSchedulePersistence(path).Load();
			Assert.IsFalse(loaded.Records.Any(record => record.ScheduleIdentifier == first.ScheduleIdentifier));
			Assert.AreEqual(
				ToastNotificationNativeOperationKind.Cancel,
				loaded.NativeOperations!.Single(operation => operation.ScheduleIdentifier == first.ScheduleIdentifier).Kind);
			Assert.AreEqual(ToastNotificationScheduleStatus.Delivering, loaded.Records.Single(record => record.ScheduleIdentifier == second.ScheduleIdentifier).Status);
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
	public void When_Another_Process_Removes_Record_Stale_Claim_Does_Not_Overwrite_It()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var record = Record("conflict", Now.AddHours(1));
			var seed = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			seed.Add(record, Now);
			var firstProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			var secondProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			firstProcess.RequestRemove(record.ScheduleIdentifier);

			var claim = secondProcess.TryClaimDelivery(
				record.ScheduleIdentifier,
				"second-process",
				Now,
				Now.AddMinutes(5));
			var loaded = new FileToastNotificationSchedulePersistence(path).Load();

			Assert.IsNull(claim);
			Assert.IsFalse(loaded.Records.Any(item => item.ScheduleIdentifier == record.ScheduleIdentifier));
			Assert.AreEqual(
				ToastNotificationNativeOperationKind.Cancel,
				loaded.NativeOperations!.Single().Kind);
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
	public async Task When_Processes_Claim_The_Same_Record_Only_One_Live_Claim_Is_Persisted()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var record = Record("exclusive-claim", Now.AddHours(1));
			new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path)).Add(record, Now);
			var first = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			var second = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			using var start = new ManualResetEventSlim();
			var claimTasks = new[]
			{
				Task.Run(() =>
				{
					start.Wait();
					return first.TryClaimDelivery(record.ScheduleIdentifier, "first", Now, Now.AddMinutes(5));
				}),
				Task.Run(() =>
				{
					start.Wait();
					return second.TryClaimDelivery(record.ScheduleIdentifier, "second", Now, Now.AddMinutes(5));
				}),
			};
			start.Set();
			var claims = await Task.WhenAll(claimTasks);

			Assert.AreEqual(1, claims.Count(claim => claim is not null));
			var persisted = new FileToastNotificationSchedulePersistence(path).Load().Records.Single();
			Assert.AreEqual(ToastNotificationScheduleStatus.Delivering, persisted.Status);
			Assert.IsTrue(persisted.DeliveryClaimOwner is "first" or "second");
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
	public void When_Foreign_Claim_Expires_Another_Process_Can_Claim_A_New_Revision()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var first = new ToastNotificationScheduleStore(persistence);
		var second = new ToastNotificationScheduleStore(persistence);
		var record = Record("expired-claim", Now.AddHours(1));
		first.Add(record, Now);
		var original = first.TryClaimDelivery(
			record.ScheduleIdentifier,
			"first",
			Now,
			Now.AddMinutes(1));

		var replacement = second.TryClaimDelivery(
			record.ScheduleIdentifier,
			"second",
			Now.AddMinutes(2),
			Now.AddMinutes(7));

		Assert.IsNotNull(original);
		Assert.IsNotNull(replacement);
		Assert.AreEqual("second", replacement.Owner);
		Assert.IsTrue(replacement.Revision > original.Revision);
	}

	[TestMethod]
	public void When_Record_Revision_Changes_Stale_Native_Completion_Is_Rejected()
	{
		var persistence = new InMemoryToastNotificationSchedulePersistence();
		var first = new ToastNotificationScheduleStore(persistence);
		var second = new ToastNotificationScheduleStore(persistence);
		var record = Record("stale-operation", Now.AddHours(1));
		var operation = first.Add(record, Now);

		var claim = second.TryClaimDelivery(
			record.ScheduleIdentifier,
			"second",
			Now,
			Now.AddMinutes(5));

		Assert.IsNotNull(claim);
		Assert.IsFalse(first.TryCompleteNativeOperation(operation));
		Assert.AreEqual("second", persistence.Load().Records.Single().DeliveryClaimOwner);
	}

	[TestMethod]
	public void When_Another_Process_Adds_A_Record_Reads_Reload_The_Latest_State()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "schedules.bin");
		try
		{
			var firstProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			var secondProcess = new ToastNotificationScheduleStore(new FileToastNotificationSchedulePersistence(path));
			var record = Record("external", Now.AddHours(1));
			secondProcess.Add(record, Now);

			Assert.AreEqual(record.ScheduleIdentifier, firstProcess.GetAll().Single().ScheduleIdentifier);
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
			0);

	private static string CreateIdentifier(string value)
	{
		var bytes = new byte[16];
		var source = global::System.Text.Encoding.UTF8.GetBytes(value);
		Array.Copy(source, bytes, Math.Min(source.Length, bytes.Length));
		return new Guid(bytes).ToString("N");
	}

	private static void WriteVersionOneSnapshot(string path, ToastNotificationScheduleRecord record)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		using var stream = File.Create(path);
		using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
		writer.Write(0x554E4F53);
		writer.Write(1);
		writer.Write(1);
		WriteString(writer, record.ScheduleIdentifier);
		WriteString(writer, record.Payload);
		writer.Write(record.DeliveryTimeUtc.ToUniversalTime().Ticks);
		writer.Write(false);
		WriteString(writer, record.Id);
		WriteString(writer, record.Tag);
		WriteString(writer, record.Group);
		writer.Write(record.SuppressPopup);
		writer.Write(false);
		writer.Write(record.MaximumSnoozeCount);
		writer.Write((int)record.Status);
		writer.Write((int)record.NotificationMirroring);
	}

	private static void WriteVersionTwoSnapshot(
		string path,
		ToastNotificationScheduleRecord record,
		long snapshotRevision)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		using var stream = File.Create(path);
		using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
		writer.Write(0x554E4F53);
		writer.Write(2);
		writer.Write(snapshotRevision);
		writer.Write(1);
		WriteString(writer, record.ScheduleIdentifier);
		WriteString(writer, record.Payload);
		writer.Write(record.DeliveryTimeUtc.ToUniversalTime().Ticks);
		writer.Write(false);
		WriteString(writer, record.Id);
		WriteString(writer, record.Tag);
		WriteString(writer, record.Group);
		writer.Write(record.SuppressPopup);
		writer.Write(false);
		writer.Write(record.MaximumSnoozeCount);
		writer.Write((int)record.Status);
		writer.Write((int)record.NotificationMirroring);
		writer.Write(record.Revision);
	}

	private static void WriteString(BinaryWriter writer, string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value);
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}
}
