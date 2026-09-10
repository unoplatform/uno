#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
	public void When_Operation_Lease_Is_Live_Another_Owner_Cannot_Claim_It()
	{
		var pending = Record(1, "tag", AppNotificationPostingState.Posting) with
		{
			Revision = 7,
			OperationOwner = "owner-a",
			OperationLeaseExpirationUtc = Now.AddMinutes(1),
		};
		var store = new AppNotificationStateStore(new InMemoryAppNotificationStatePersistence(
			new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { pending })));

		var liveClaim = store.TryClaimExpiredOperation(pending, "owner-b", Now.AddMinutes(2), Now, out _);
		var expiredClaim = store.TryClaimExpiredOperation(pending, "owner-b", Now.AddMinutes(3), Now.AddMinutes(2), out var claimed);

		Assert.IsFalse(liveClaim);
		Assert.IsTrue(expiredClaim);
		Assert.AreEqual("owner-b", claimed?.OperationOwner);
		Assert.AreEqual(8L, claimed?.Revision);
	}

	[TestMethod]
	public void When_Foreign_Posting_Lease_Is_Live_Show_Reservation_Does_Not_Mutate_It()
	{
		var pending = Record(1, "tag", AppNotificationPostingState.Posting) with
		{
			Group = "group",
			Revision = 7,
			OperationOwner = "owner-a",
			OperationLeaseExpirationUtc = Now.AddMinutes(1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(
			new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { pending }));
		var store = new AppNotificationStateStore(persistence);

		var reservation = store.PrepareShow(
			pending.Payload,
			pending.Tag,
			pending.Group,
			pending.ExpirationUtc,
			pending.ExpiresOnReboot,
			pending.BootIdentifier,
			pending.Priority,
			pending.SuppressDisplay,
			pending.Progress,
			Now,
			"owner-b",
			Now.AddMinutes(1),
			replaceTagAndGroup: true);

		Assert.AreEqual(AppNotificationShowReservationKind.Busy, reservation.Kind);
		Assert.AreEqual(pending, persistence.Load().Records.Single());
	}

	[TestMethod]
	public void When_Foreign_Update_Lease_Is_Live_Progress_Update_Does_Not_Mutate_It()
	{
		var pending = Record(1, "tag", AppNotificationPostingState.Updating) with
		{
			Group = "group",
			Progress = Snapshot(1, 0.1),
			Revision = 7,
			OperationOwner = "owner-a",
			OperationLeaseExpirationUtc = Now.AddMinutes(1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(
			new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { pending }));
		var store = new AppNotificationStateStore(persistence);

		var result = store.BeginProgressUpdate(
			"tag",
			"group",
			Snapshot(2, 0.2),
			"owner-b",
			Now.AddMinutes(1),
			Now,
			out var updates);

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, result);
		Assert.AreEqual(0, updates.Count);
		Assert.AreEqual(pending, persistence.Load().Records.Single());
	}

	[TestMethod]
	public void When_Foreign_Posting_Lease_Is_Live_Progress_Update_Does_Not_Mutate_Other_Matches()
	{
		var shown = Record(1, "tag", AppNotificationPostingState.Shown) with { Group = "group" };
		var posting = Record(2, "tag", AppNotificationPostingState.Posting) with
		{
			Group = "group",
			Revision = 7,
			OperationOwner = "owner-a",
			OperationLeaseExpirationUtc = Now.AddMinutes(1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(
			new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 3, new[] { shown, posting }));
		var store = new AppNotificationStateStore(persistence);

		var result = store.BeginProgressUpdate(
			"tag",
			"group",
			Snapshot(2, 0.2),
			"owner-b",
			Now.AddMinutes(1),
			Now,
			out var updates);

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, result);
		Assert.AreEqual(0, updates.Count);
		CollectionAssert.AreEqual(new[] { shown, posting }, persistence.Load().Records.ToArray());
	}

	[TestMethod]
	public void When_Awaited_Record_Revision_Is_Stale_It_Cannot_Remove_A_Replacement()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var store = new AppNotificationStateStore(persistence);
		var original = PrepareShow(store, "tag", "group", string.Empty, "owner-a").Record!;
		store.TryMarkShown(original);
		var captured = new AppNotificationStateStore(persistence).GetShown().Single();
		var replacement = PrepareShow(store, "tag", "group", string.Empty, "owner-b").Record!;

		var removed = store.TryRemove(captured);

		Assert.IsFalse(removed);
		Assert.AreEqual(replacement.Revision, new AppNotificationStateStore(persistence).GetPendingUpdates().Single().Revision);
	}

	[TestMethod]
	public async Task When_Delivery_Correlation_Is_Reserved_Concurrently_Only_One_Record_Is_Created()
	{
		const string correlation = "0123456789abcdef0123456789abcdef";
		var persistence = new InMemoryAppNotificationStatePersistence();
		var first = new AppNotificationStateStore(persistence);
		var second = new AppNotificationStateStore(persistence);

		var reservations = await Task.WhenAll(
			Task.Run(() => PrepareShow(first, "first", string.Empty, correlation, "owner-a")),
			Task.Run(() => PrepareShow(second, "second", string.Empty, correlation, "owner-b")));

		CollectionAssert.AreEquivalent(
			new[] { AppNotificationShowReservationKind.New, AppNotificationShowReservationKind.Busy },
			reservations.Select(reservation => reservation.Kind).ToArray());
		Assert.AreEqual(1, persistence.Load().Records.Count);
	}

	[TestMethod]
	public void When_Tag_And_Group_Are_Replaced_Reservation_Is_Atomic_And_Reuses_The_Id()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var first = new AppNotificationStateStore(persistence);
		var second = new AppNotificationStateStore(persistence);
		var original = PrepareShow(first, "tag", "group", string.Empty, "owner-a").Record!;
		first.TryMarkShown(original);

		var replacement = PrepareShow(second, "tag", "group", string.Empty, "owner-b");

		Assert.AreEqual(AppNotificationShowReservationKind.Replacement, replacement.Kind);
		Assert.AreEqual(original.Id, replacement.Record?.Id);
		Assert.AreEqual(1, persistence.Load().Records.Count);
	}

	[TestMethod]
	public void When_Duplicate_Tag_And_Group_Records_Are_Replaced_They_Are_Atomically_Reserved_For_Removal()
	{
		var first = Record(1, "tag", AppNotificationPostingState.Shown) with { Group = "group" };
		var second = Record(2, "tag", AppNotificationPostingState.Shown) with { Group = "group" };
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			3,
			new[] { first, second }));
		var store = new AppNotificationStateStore(persistence);

		var reservation = PrepareShow(store, "tag", "group", string.Empty, "owner");

		Assert.AreEqual(AppNotificationShowReservationKind.Replacement, reservation.Kind);
		Assert.AreEqual(1, reservation.DuplicateRecords.Count);
		Assert.AreEqual(AppNotificationPostingState.Removing, reservation.DuplicateRecords[0].Removal.PostingState);
		CollectionAssert.AreEquivalent(
			new[] { AppNotificationPostingState.Updating, AppNotificationPostingState.Removing },
			persistence.Load().Records.Select(record => record.PostingState).ToArray());
		Assert.IsTrue(store.TryResolveFailedShow(reservation.Record!, reservation.PreviousRecord, reservation.DuplicateRecords));
		Assert.IsTrue(persistence.Load().Records.All(record => record.PostingState == AppNotificationPostingState.Shown));
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
				Revision = 9,
				OperationOwner = "owner",
				OperationLeaseExpirationUtc = Now.AddMinutes(1),
			};
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 43, new[] { record }));

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
	public void When_Persisted_State_Has_No_Records_It_Is_Rejected_As_Corrupt()
	{
		var state = new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 1, null!);

		Assert.ThrowsExactly<InvalidDataException>(() => FileAppNotificationStatePersistence.ValidateSnapshot(state));
	}

	[TestMethod]
	public void When_Primary_State_Is_Corrupt_Last_Good_Backup_Is_Loaded()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { Record(1, "backup", AppNotificationPostingState.Shown) }));
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 3, new[] { Record(2, "primary", AppNotificationPostingState.Shown) }));
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
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { Record(1, "backup", AppNotificationPostingState.Shown) }));
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 3, new[] { Record(2, "primary", AppNotificationPostingState.Shown) }));
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
	public void When_SchemaV1_State_Is_Loaded_Delivery_Correlation_Defaults_Empty()
	{
		var path = Path.GetTempFileName();
		try
		{
			var record = Record(1, "tag", AppNotificationPostingState.Shown);
			using (var writer = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None)))
			{
				writer.Write(0x554E4F4E);
				writer.Write(1);
				writer.Write(2u);
				writer.Write(1);
				writer.Write(record.Id);
				WriteV1String(writer, record.Payload);
				WriteV1String(writer, record.Tag);
				WriteV1String(writer, record.Group);
				writer.Write(record.CreatedUtc.UtcTicks);
				writer.Write(record.ExpirationUtc.UtcTicks);
				writer.Write(record.ExpiresOnReboot);
				writer.Write(false);
				writer.Write((int)record.Priority);
				writer.Write(record.SuppressDisplay);
				writer.Write((int)record.PostingState);
				writer.Write(false);
			}

			var loaded = new FileAppNotificationStatePersistence(path).Load();

			Assert.AreEqual(string.Empty, loaded.Records.Single().DeliveryCorrelation);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void When_SchemaV2_Shown_Correlation_Is_Migrated_Delivery_Receipt_Is_Preserved()
	{
		const string correlation = "0123456789abcdef0123456789abcdef";
		var correlated = Record(1, "tag", AppNotificationPostingState.Shown) with { DeliveryCorrelation = correlation };
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(2, 2, new[] { correlated }));

		var store = new AppNotificationStateStore(persistence);

		Assert.IsTrue(store.HasDeliveryReceipt(correlation));
		var next = Reserve(store, "next");
		Assert.AreEqual(AppNotificationStateSnapshot.CurrentSchemaVersion, persistence.Load().SchemaVersion);
		Assert.AreEqual(2u, next.Id);
	}

	[TestMethod]
	public void When_Delivery_Receipts_Are_At_Capacity_Expired_Receipts_Are_Removed_Before_New_Ones_Are_Added()
	{
		var changes = AppNotificationDeliveryReceiptRetention.CreatePlan(
			new[] { "oldest", "middle", "newest" },
			new[] { "middle", "newest", "new" },
			maximumCount: 3);

		CollectionAssert.AreEqual(new[] { "oldest" }, changes.Removed.ToArray());
		CollectionAssert.AreEqual(new[] { "new" }, changes.Added.ToArray());
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
			persistence.Save(new AppNotificationStateSnapshot(AppNotificationStateSnapshot.CurrentSchemaVersion, 2, new[] { Record(1, "tag", AppNotificationPostingState.Shown) }));
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

	private static AppNotificationShowReservation PrepareShow(
		AppNotificationStateStore store,
		string tag,
		string group,
		string deliveryCorrelation,
		string owner)
		=> store.PrepareShow(
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			tag,
			group,
			DateTimeOffset.FromFileTime(0),
			false,
			null,
			AppNotificationPriority.Default,
			false,
			null,
			Now,
			owner,
			Now.AddMinutes(1),
			replaceTagAndGroup: true,
			deliveryCorrelation: deliveryCorrelation);

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

	private static void WriteV1String(BinaryWriter writer, string value)
	{
		var bytes = global::System.Text.Encoding.UTF8.GetBytes(value);
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}
}
