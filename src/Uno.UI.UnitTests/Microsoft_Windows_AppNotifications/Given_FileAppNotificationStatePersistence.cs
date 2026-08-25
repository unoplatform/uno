#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_FileAppNotificationStatePersistence
{
	private const string ValidPayload = "<toast><visual><binding template='ToastGeneric'/></visual></toast>";

	[TestMethod]
	public void When_Two_Processes_Add_Different_Records_Both_Are_Preserved()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var firstStore = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var secondStore = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));

			var first = Reserve(firstStore, "first");
			var second = Reserve(secondStore, "second");

			Assert.AreNotEqual(first.Id, second.Id);
			var loaded = new FileAppNotificationStatePersistence(path).Load();
			CollectionAssert.AreEquivalent(new[] { "first", "second" }, loaded.Records.Select(record => record.Tag).ToArray());
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
	public void When_Two_Processes_Update_The_Same_Record_Transactions_Are_Serialized()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var seed = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var record = Reserve(seed, "progress");
			seed.MarkShown(record.Id);
			var firstStore = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var secondStore = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			firstStore.BeginProgressUpdate("progress", null, Progress(1), out _);

			var result = secondStore.BeginProgressUpdate("progress", null, Progress(2), out _);

			Assert.AreEqual(AppNotificationProgressResult.Succeeded, result);
			Assert.AreEqual(2u, new FileAppNotificationStatePersistence(path).Load().Records.Single().Progress?.SequenceNumber);
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
	public void When_Primary_Payload_Xml_Is_Corrupt_The_Backup_Is_Loaded()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(Snapshot("backup"));
			persistence.Save(Snapshot("primary"));
			var bytes = File.ReadAllBytes(path);
			var validPayload = Encoding.UTF8.GetBytes(ValidPayload);
			var invalidPayload = Encoding.UTF8.GetBytes(ValidPayload[..^1] + "<");
			var payloadIndex = FindSequence(bytes, validPayload);
			Assert.IsTrue(payloadIndex >= 0);
			invalidPayload.CopyTo(bytes, payloadIndex);
			File.WriteAllBytes(path, bytes);

			var loaded = persistence.Load();

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
	public async Task When_Two_Processes_Replace_The_Same_Tag_And_Group_Live_Lease_Serializes_Them()
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var seed = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var original = PrepareShow(seed, "tag", "group", string.Empty, "seed").Record!;
			seed.TryMarkShown(original);
			var first = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var second = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			using var barrier = new Barrier(2);

			var replacements = await Task.WhenAll(
				Task.Run(() =>
				{
					barrier.SignalAndWait();
					return PrepareShow(first, "tag", "group", string.Empty, "first");
				}),
				Task.Run(() =>
				{
					barrier.SignalAndWait();
					return PrepareShow(second, "tag", "group", string.Empty, "second");
				}));

			CollectionAssert.AreEquivalent(
				new[] { AppNotificationShowReservationKind.Replacement, AppNotificationShowReservationKind.Busy },
				replacements.Select(result => result.Kind).ToArray());
			var winner = replacements.Single(result => result.Kind == AppNotificationShowReservationKind.Replacement).Record!;
			Assert.AreEqual(original.Id, winner.Id);
			var loaded = new FileAppNotificationStatePersistence(path).Load();
			Assert.AreEqual(1, loaded.Records.Count);
			Assert.AreEqual(winner, loaded.Records.Single());
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
	public async Task When_Two_Processes_Reserve_One_Delivery_Correlation_Only_One_Succeeds()
	{
		const string correlation = "0123456789abcdef0123456789abcdef";
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var first = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			var second = new AppNotificationStateStore(new FileAppNotificationStatePersistence(path));
			using var barrier = new Barrier(2);

			var reservations = await Task.WhenAll(
				Task.Run(() =>
				{
					barrier.SignalAndWait();
					return PrepareShow(first, "first", string.Empty, correlation, "first");
				}),
				Task.Run(() =>
				{
					barrier.SignalAndWait();
					return PrepareShow(second, "second", string.Empty, correlation, "second");
				}));

			CollectionAssert.AreEquivalent(
				new[] { AppNotificationShowReservationKind.New, AppNotificationShowReservationKind.Busy },
				reservations.Select(result => result.Kind).ToArray());
			Assert.AreEqual(1, new FileAppNotificationStatePersistence(path).Load().Records.Count);
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
	[DataRow(0)]
	[DataRow(-1)]
	public void When_Primary_Schema_Is_Low_The_Backup_Is_Loaded(int schemaVersion)
	{
		var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
		var path = Path.Combine(folder, "state.bin");
		try
		{
			var persistence = new FileAppNotificationStatePersistence(path);
			persistence.Save(Snapshot("backup"));
			persistence.Save(Snapshot("primary"));
			var bytes = File.ReadAllBytes(path);
			BitConverter.GetBytes(schemaVersion).CopyTo(bytes, sizeof(int));
			File.WriteAllBytes(path, bytes);

			var loaded = persistence.Load();

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

	private static AppNotificationStateSnapshot Snapshot(string tag)
		=> new(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			2,
			new[]
			{
				new AppNotificationStateRecord(
					1,
					ValidPayload,
					tag,
					string.Empty,
					DateTimeOffset.UtcNow,
					DateTimeOffset.FromFileTime(0),
					false,
					null,
					AppNotificationPriority.Default,
					false,
					AppNotificationPostingState.Shown,
					null),
			});

	private static AppNotificationStateRecord Reserve(AppNotificationStateStore store, string tag)
		=> store.Reserve(
			ValidPayload,
			tag,
			string.Empty,
			DateTimeOffset.FromFileTime(0),
			false,
			null,
			AppNotificationPriority.Default,
			false,
			null,
			DateTimeOffset.UtcNow);

	private static AppNotificationShowReservation PrepareShow(
		AppNotificationStateStore store,
		string tag,
		string group,
		string deliveryCorrelation,
		string owner)
	{
		var now = DateTimeOffset.UtcNow;
		return store.PrepareShow(
			ValidPayload,
			tag,
			group,
			DateTimeOffset.FromFileTime(0),
			false,
			null,
			AppNotificationPriority.Default,
			false,
			null,
			now,
			owner,
			now.AddMinutes(1),
			replaceTagAndGroup: true,
			deliveryCorrelation: deliveryCorrelation);
	}

	private static AppNotificationProgressSnapshot Progress(uint sequenceNumber)
		=> new(sequenceNumber, string.Empty, 0.5, string.Empty, string.Empty);

	private static int FindSequence(byte[] source, byte[] value)
	{
		for (var index = 0; index <= source.Length - value.Length; index++)
		{
			if (source.AsSpan(index, value.Length).SequenceEqual(value))
			{
				return index;
			}
		}
		return -1;
	}
}
