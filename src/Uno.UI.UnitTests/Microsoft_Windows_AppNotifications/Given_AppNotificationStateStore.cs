#nullable enable

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationStateStore
{
	private static readonly DateTimeOffset Now = new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

	[TestMethod]
	public void When_Posting_Fails_Reserved_Id_Is_Not_Reused()
	{
		var store = CreateStore();
		var failed = Reserve(store, "failed");
		store.Abort(failed.Id);

		var accepted = Reserve(store, "accepted");

		Assert.AreEqual(1u, failed.Id);
		Assert.AreEqual(2u, accepted.Id);
	}

	[TestMethod]
	public void When_State_Is_Recovered_Abandoned_Postings_Remain_Recoverable_And_Next_Id_Is_Preserved()
	{
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			7,
			new[]
			{
				Record(5, "shown", AppNotificationPostingState.Shown),
				Record(6, "posting", AppNotificationPostingState.Posting),
			}));

		var store = new AppNotificationStateStore(persistence);
		var next = Reserve(store, "next");

		Assert.AreEqual(7u, next.Id);
		Assert.AreEqual(1, store.GetShown().Count);
		Assert.AreEqual(5u, store.GetShown()[0].Id);
		CollectionAssert.AreEquivalent(new[] { 6u, next.Id }, store.GetPendingPostings().Select(record => record.Id).ToArray());
	}

	[TestMethod]
	public void When_Expired_Or_Reboot_Bound_Records_Are_Selected_They_Remain_Until_Native_Removal()
	{
		var store = CreateStore();
		var expired = Reserve(store, "expired", expiration: Now.AddMinutes(-1));
		store.MarkShown(expired.Id);
		var rebooted = Reserve(store, "rebooted", expiresOnReboot: true, bootIdentifier: "boot-1");
		store.MarkShown(rebooted.Id);
		var active = Reserve(store, "active", expiration: Now.AddMinutes(5), expiresOnReboot: true, bootIdentifier: "boot-2");
		store.MarkShown(active.Id);

		var expiredRecords = store.GetExpired(Now, "boot-2");

		CollectionAssert.AreEquivalent(new[] { expired.Id, rebooted.Id }, expiredRecords.Select(record => record.Id).ToArray());
		Assert.AreEqual(3, store.GetShown().Count);
		foreach (var record in expiredRecords)
		{
			store.RemoveById(record.Id);
		}
		CollectionAssert.AreEqual(new[] { active.Id }, store.GetShown().Select(record => record.Id).ToArray());
	}

	[TestMethod]
	public void When_Tag_And_Group_Are_Duplicated_All_Matches_Are_Removed()
	{
		var store = CreateStore();
		var first = Reserve(store, "tag", "group");
		var second = Reserve(store, "tag", "group");
		var other = Reserve(store, "tag", "other");
		store.MarkShown(first.Id);
		store.MarkShown(second.Id);
		store.MarkShown(other.Id);

		var removed = store.RemoveByTagAndGroup("tag", "group");

		CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, removed.Select(record => record.Id).ToArray());
		CollectionAssert.AreEqual(new[] { other.Id }, store.GetShown().Select(record => record.Id).ToArray());
	}

	[TestMethod]
	public void When_All_Are_Removed_Next_Id_Does_Not_Reset()
	{
		var store = CreateStore();
		var first = Reserve(store, "first");
		store.MarkShown(first.Id);
		store.RemoveAll();

		var second = Reserve(store, "second");

		Assert.AreEqual(2u, second.Id);
	}

	[TestMethod]
	public void When_Progress_Sequence_Is_Stale_Native_Update_Is_Not_Requested()
	{
		var store = CreateStore();
		var record = Reserve(store, "progress", "group");
		store.MarkShown(record.Id);
		var first = Snapshot(5, 0.5);
		Assert.AreEqual(AppNotificationProgressResult.Succeeded, store.BeginProgressUpdate("progress", "group", first, out var firstUpdates));
		Assert.AreEqual(AppNotificationPostingState.Updating, store.GetPendingUpdates().Single().PostingState);
		store.MarkShown(firstUpdates.Single().Id);

		var result = store.BeginProgressUpdate("progress", "group", Snapshot(4, 0.4), out var staleUpdates);

		Assert.AreEqual(AppNotificationProgressResult.Succeeded, result);
		Assert.AreEqual(0, staleUpdates.Count);
		Assert.AreEqual(5u, store.GetShown()[0].Progress?.SequenceNumber);
	}

	[TestMethod]
	public void When_Progress_Tag_Is_Missing_Result_Is_NotFound()
	{
		var store = CreateStore();

		var result = store.BeginProgressUpdate("missing", group: null, Snapshot(1, 0.1), out var updates);

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, result);
		Assert.AreEqual(0, updates.Count);
	}

	[TestMethod]
	public void When_Progress_Update_Is_Recovered_Sequence_HighWater_Mark_Is_Preserved()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var store = new AppNotificationStateStore(persistence);
		var record = Reserve(store, "progress", "group");
		store.MarkShown(record.Id);
		store.BeginProgressUpdate("progress", "group", Snapshot(5, 0.5), out _);

		var recovered = new AppNotificationStateStore(persistence);
		var result = recovered.BeginProgressUpdate("progress", "group", Snapshot(4, 0.4), out var staleUpdates);

		Assert.AreEqual(AppNotificationProgressResult.Succeeded, result);
		Assert.AreEqual(0, staleUpdates.Count);
		Assert.AreEqual(5u, recovered.GetPendingUpdates().Single().Progress?.SequenceNumber);
	}

	[TestMethod]
	public void When_File_State_Is_Saved_It_RoundTrips_All_Fields()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			var record = Record(42, "tag", AppNotificationPostingState.Shown) with
			{
				Group = "group",
				ExpirationUtc = Now.AddHours(1),
				ExpiresOnReboot = true,
				BootIdentifier = "boot",
				Priority = AppNotificationPriority.High,
				SuppressDisplay = true,
				Progress = Snapshot(3, 0.75),
			};
			persistence.Save(new AppNotificationStateSnapshot(1, 43, new[] { record }));

			var loaded = persistence.Load();

			Assert.AreEqual(43u, loaded.NextId);
			Assert.AreEqual(record, loaded.Records.Single());
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
	public void When_File_State_Is_Corrupt_Load_Fails_Without_Overwriting_It()
	{
		var path = Path.GetTempFileName();
		try
		{
			File.WriteAllText(path, "not state");

			Assert.ThrowsExactly<InvalidDataException>(() => new FileAppNotificationStatePersistence(path).Load());

			Assert.AreEqual("not state", File.ReadAllText(path));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void When_Primary_State_Is_Corrupt_Last_Good_Backup_Is_Loaded()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(new AppNotificationStateSnapshot(1, 2, new[] { Record(1, "backup", AppNotificationPostingState.Shown) }));
			persistence.Save(new AppNotificationStateSnapshot(1, 3, new[] { Record(2, "primary", AppNotificationPostingState.Shown) }));
			File.WriteAllText(path, "corrupt primary");

			var loaded = persistence.Load();

			Assert.AreEqual(2u, loaded.NextId);
			Assert.AreEqual("backup", loaded.Records.Single().Tag);
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
	public void When_Primary_State_Is_Missing_Last_Good_Backup_Is_Loaded()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(new AppNotificationStateSnapshot(1, 2, new[] { Record(1, "backup", AppNotificationPostingState.Shown) }));
			persistence.Save(new AppNotificationStateSnapshot(1, 3, new[] { Record(2, "primary", AppNotificationPostingState.Shown) }));
			File.Delete(path);

			var loaded = persistence.Load();

			Assert.AreEqual(2u, loaded.NextId);
			Assert.AreEqual("backup", loaded.Records.Single().Tag);
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
	public void When_State_Schema_Is_Newer_Load_Fails_Without_Rewriting_It()
	{
		var path = Path.GetTempFileName();
		try
		{
			using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None)))
			{
				writer.Write(0x554E4F4E);
				writer.Write(AppNotificationStateSnapshot.CurrentSchemaVersion + 1);
				writer.Write(7u);
				writer.Write(0);
			}
			var original = File.ReadAllBytes(path);

			Assert.ThrowsExactly<AppNotificationStateVersionException>(() => new FileAppNotificationStatePersistence(path).Load());

			CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void When_State_Contains_Duplicate_Ids_Save_Is_Rejected()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.bin");
		var state = new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			2,
			new[]
			{
				Record(1, "first", AppNotificationPostingState.Shown),
				Record(1, "duplicate", AppNotificationPostingState.Shown),
			});

		Assert.ThrowsExactly<InvalidDataException>(() => new FileAppNotificationStatePersistence(path).Save(state));
		Assert.IsFalse(File.Exists(path));
	}

	[TestMethod]
	public void When_State_Contains_Invalid_Utf8_Load_Is_Rejected()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(new AppNotificationStateSnapshot(1, 2, new[] { Record(1, "tag", AppNotificationPostingState.Shown) }));
			var bytes = File.ReadAllBytes(path);
			bytes[24] = 0xFF;
			File.WriteAllBytes(path, bytes);

			Assert.ThrowsExactly<InvalidDataException>(() => persistence.Load());
		}
		finally
		{
			var folder = Path.GetDirectoryName(path)!;
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, recursive: true);
			}
		}
	}

	[TestMethod]
	public void When_State_Exceeds_Aggregate_Limit_Save_Is_Rejected()
	{
		var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "state.bin");
		var records = Enumerable.Range(1, 513)
			.Select(id => Record((uint)id, "tag", AppNotificationPostingState.Shown) with { Group = new string('a', 32_768) })
			.ToArray();

		Assert.ThrowsExactly<InvalidDataException>(() => new FileAppNotificationStatePersistence(path).Save(
			new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 514, records)));
		Assert.IsFalse(File.Exists(path));
	}

	private static AppNotificationStateStore CreateStore()
		=> new(new InMemoryAppNotificationStatePersistence());

	private static AppNotificationStateRecord Reserve(
		AppNotificationStateStore store,
		string tag,
		string group = "",
		DateTimeOffset? expiration = null,
		bool expiresOnReboot = false,
		string? bootIdentifier = null)
		=> store.Reserve(
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			tag,
			group,
			expiration ?? DateTimeOffset.FromFileTime(0),
			expiresOnReboot,
			bootIdentifier,
			AppNotificationPriority.Default,
			suppressDisplay: false,
			progress: null,
			Now);

	private static AppNotificationStateRecord Record(uint id, string tag, AppNotificationPostingState state)
		=> new(
			id,
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			tag,
			string.Empty,
			Now,
			DateTimeOffset.FromFileTime(0),
			false,
			null,
			AppNotificationPriority.Default,
			false,
			state,
			null);

	private static AppNotificationProgressSnapshot Snapshot(uint sequence, double value)
		=> new(sequence, "Title", value, $"{value:P0}", "Status");
}
