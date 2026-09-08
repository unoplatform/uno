#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed partial class WebAssemblyAppNotificationStatePersistence :
	IAppNotificationStatePersistence,
	IMergingAppNotificationStatePersistence,
	IAppNotificationIdAllocator,
	ITransactionalAppNotificationStatePersistence
{
	private const string LegacyStateKey = "uno.appnotifications.state.v1";
	private const string LegacyBackupKey = LegacyStateKey + ".backup";
	private const string LegacyFormatKey = "uno.appnotifications.atomic-format";
	private const string LegacyAtomicFormat = "2";
	private const string LegacyRecordPrefix = "uno.appnotifications.record.";
	private const string LegacyRecordBackupPrefix = "uno.appnotifications.record-backup.";
	private const string LegacyReceiptPrefix = "uno.appnotifications.receipt.";
	private const string ManifestKey = "uno.appnotifications.manifest.v3";
	private const string ManifestBackupKey = ManifestKey + ".backup";
	private const string GenerationPrefix = "uno.appnotifications.generation.";
	private const string TransactionLockName = "uno.appnotification-state-lock";
	private const int MaxRecords = 10_000;
	private const double TransactionLockTimeoutMilliseconds = 5_000;
	private const double TransactionLockLeaseMilliseconds = 30_000;
	private AppNotificationStateSnapshot _baseline = AppNotificationStateSnapshot.Empty;

	public static bool IsSupported => NativeMethods.IsSupported();

	public AppNotificationStateSnapshot Load()
		=> WithLock(owner =>
		{
			var (_, manifest, state) = LoadOrMigrate(owner);
			_baseline = Clone(state);
			CleanupUnreferencedGenerations(owner, manifest);
			return Clone(_baseline);
		});

	public void Save(AppNotificationStateSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		FileAppNotificationStatePersistence.ValidateSnapshot(state);
		WithLock(owner =>
		{
			var (rawManifest, manifest, _) = LoadOrMigrate(owner);
			_baseline = CommitSnapshot(owner, rawManifest, manifest, state);
			return true;
		});
	}

	public AppNotificationStateSnapshot MergeAndSave(AppNotificationStateSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		FileAppNotificationStatePersistence.ValidateSnapshot(state);
		return WithLock(owner =>
		{
			var (rawManifest, manifest, latest) = LoadOrMigrate(owner);
			var baselineById = _baseline.Records.ToDictionary(record => record.Id);
			var nextById = state.Records.ToDictionary(record => record.Id);
			var latestById = latest.Records.ToDictionary(record => record.Id);

			foreach (var id in baselineById.Keys.Union(nextById.Keys))
			{
				baselineById.TryGetValue(id, out var baselineRecord);
				nextById.TryGetValue(id, out var nextRecord);
				if (Equals(baselineRecord, nextRecord))
				{
					continue;
				}
				latestById.TryGetValue(id, out var latestRecord);
				if (!Equals(baselineRecord, latestRecord))
				{
					throw new InvalidOperationException($"App notification state record {id} was changed by another browser tab.");
				}
			}

			foreach (var id in baselineById.Keys.Union(nextById.Keys))
			{
				baselineById.TryGetValue(id, out var baselineRecord);
				nextById.TryGetValue(id, out var nextRecord);
				if (Equals(baselineRecord, nextRecord))
				{
					continue;
				}
				if (nextRecord is null)
				{
					latestById.Remove(id);
				}
				else
				{
					latestById[id] = nextRecord;
				}
			}

			var receipts = MergeReceipts(
				_baseline.DeliveryReceipts ?? Array.Empty<string>(),
				state.DeliveryReceipts ?? Array.Empty<string>(),
				latest.DeliveryReceipts ?? Array.Empty<string>());
			var merged = new AppNotificationStateSnapshot(
				AppNotificationStateSnapshot.CurrentSchemaVersion,
				state.NextId,
				latestById.Values.OrderBy(record => record.CreatedUtc).ToArray(),
				receipts);
			_baseline = CommitSnapshot(owner, rawManifest, manifest, merged);
			return Clone(_baseline);
		});
	}

	public uint AllocateId(IReadOnlyCollection<uint> localIds)
	{
		ArgumentNullException.ThrowIfNull(localIds);
		return WithLock(owner =>
		{
			var (_, _, state) = LoadOrMigrate(owner);
			return AllocateIdCore(state, localIds);
		});
	}

	public AppNotificationStateSnapshot ExecuteTransaction(Func<AppNotificationStateTransactionContext, AppNotificationStateSnapshot> mutation)
	{
		ArgumentNullException.ThrowIfNull(mutation);
		return WithLock(owner =>
		{
			var (rawManifest, manifest, latest) = LoadOrMigrate(owner);
			var next = mutation(new AppNotificationStateTransactionContext(
				Clone(latest),
				localIds => AllocateIdCore(latest, localIds)));
			FileAppNotificationStatePersistence.ValidateSnapshot(next);
			_baseline = CommitSnapshot(owner, rawManifest, manifest, next);
			return Clone(_baseline);
		});
	}

	private static uint AllocateIdCore(AppNotificationStateSnapshot state, IReadOnlyCollection<uint> localIds)
	{
		var used = state.Records.Select(record => record.Id).Concat(localIds).ToHashSet();
		for (var attempt = 0; attempt < 128; attempt++)
		{
			var value = NativeMethods.CreateNotificationId();
			if (value is >= 1 and <= uint.MaxValue)
			{
				var id = (uint)value;
				if (!used.Contains(id))
				{
					return id;
				}
			}
		}
		throw new InvalidOperationException("Unable to allocate a unique app notification ID.");
	}

	private static (string RawManifest, GenerationManifest Manifest, AppNotificationStateSnapshot State) LoadOrMigrate(string owner)
	{
		var rawManifest = NativeMethods.GetItem(ManifestKey);
		if (rawManifest is not null)
		{
			try
			{
				var manifest = DeserializeManifest(rawManifest);
				return (rawManifest, manifest, LoadManifestState(owner, manifest));
			}
			catch (Exception primaryException) when (IsCorruptStateException(primaryException))
			{
				var backupManifest = NativeMethods.GetItem(ManifestBackupKey);
				if (backupManifest is null)
				{
					throw new InvalidDataException("The app notification state manifest is corrupt and has no backup.", primaryException);
				}
				try
				{
					var manifest = DeserializeManifest(backupManifest);
					return (rawManifest, manifest, LoadManifestState(owner, manifest));
				}
				catch (Exception backupException) when (IsCorruptStateException(backupException))
				{
					throw new InvalidDataException(
						"The app notification state manifest and its backup are corrupt.",
						new AggregateException(primaryException, backupException));
				}
			}
		}

		var legacy = NativeMethods.GetItem(LegacyFormatKey) == LegacyAtomicFormat
			? LoadLegacyAtomic(owner)
			: LoadLegacySnapshot();
		var normalized = NormalizeLegacy(legacy);
		var generation = Guid.NewGuid().ToString("N");
		WriteGeneration(owner, generation, normalized);
		var migratedManifest = new GenerationManifest(3, generation, null, Guid.NewGuid().ToString("N"));
		var migratedRaw = SerializeManifest(migratedManifest);
		RenewTransactionLock(owner);
		NativeMethods.SetItem(ManifestBackupKey, migratedRaw);
		RenewTransactionLock(owner);
		if (!NativeMethods.CommitTransactionVersion(
			TransactionLockName,
			owner,
			ManifestKey,
			string.Empty,
			migratedRaw))
		{
			throw new InvalidOperationException("App notification state changed during browser migration.");
		}
		return (migratedRaw, migratedManifest, normalized);
	}

	private static AppNotificationStateSnapshot CommitSnapshot(
		string owner,
		string expectedRawManifest,
		GenerationManifest currentManifest,
		AppNotificationStateSnapshot state)
	{
		FileAppNotificationStatePersistence.ValidateSnapshot(state);
		CleanupUnreferencedGenerations(owner, currentManifest);
		RenewTransactionLock(owner);
		NativeMethods.SetItem(ManifestBackupKey, SerializeManifest(currentManifest));
		var generation = Guid.NewGuid().ToString("N");
		WriteGeneration(owner, generation, state);
		var nextManifest = new GenerationManifest(3, generation, currentManifest.Current, Guid.NewGuid().ToString("N"));
		var nextRawManifest = SerializeManifest(nextManifest);
		RenewTransactionLock(owner);
		if (!NativeMethods.CommitTransactionVersion(
			TransactionLockName,
			owner,
			ManifestKey,
			expectedRawManifest,
			nextRawManifest))
		{
			throw new InvalidOperationException("App notification state changed during the browser transaction.");
		}
		return Clone(state);
	}

	private static void WriteGeneration(string owner, string generation, AppNotificationStateSnapshot state)
	{
		var ids = new uint[state.Records.Count];
		for (var index = 0; index < state.Records.Count; index++)
		{
			if ((index & 127) == 0)
			{
				RenewTransactionLock(owner);
			}
			var record = state.Records[index];
			ids[index] = record.Id;
			NativeMethods.SetItem(
				GetGenerationRecordKey(generation, record.Id),
				JsonSerializer.Serialize(record, WebAssemblyAppNotificationStateSerializationContext.Default.AppNotificationStateRecord));
		}

		var generationIndex = new GenerationIndex(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			state.NextId,
			ids,
			(state.DeliveryReceipts ?? Array.Empty<string>()).ToArray());
		RenewTransactionLock(owner);
		NativeMethods.SetItem(
			GetGenerationIndexKey(generation),
			JsonSerializer.Serialize(generationIndex, WebAssemblyAppNotificationStateSerializationContext.Default.GenerationIndex));
	}

	private static AppNotificationStateSnapshot LoadManifestState(string owner, GenerationManifest manifest)
	{
		try
		{
			return LoadGeneration(owner, manifest.Current, manifest.Previous);
		}
		catch (AppNotificationStateVersionException)
		{
			throw;
		}
		catch (Exception currentException) when (IsCorruptStateException(currentException) && manifest.Previous is not null)
		{
			try
			{
				return LoadGeneration(owner, manifest.Previous, null);
			}
			catch (Exception previousException) when (IsCorruptStateException(previousException))
			{
				throw new InvalidDataException(
					"The current and previous app notification state generations are corrupt.",
					new AggregateException(currentException, previousException));
			}
		}
	}

	private static AppNotificationStateSnapshot LoadGeneration(string owner, string generation, string? backupGeneration)
	{
		var rawIndex = NativeMethods.GetItem(GetGenerationIndexKey(generation))
			?? throw new InvalidDataException("The app notification state generation index is missing.");
		var index = JsonSerializer.Deserialize(
			rawIndex,
			WebAssemblyAppNotificationStateSerializationContext.Default.GenerationIndex)
			?? throw new InvalidDataException("The app notification state generation index is empty.");
		if (index.SchemaVersion > AppNotificationStateSnapshot.CurrentSchemaVersion)
		{
			throw new AppNotificationStateVersionException(index.SchemaVersion);
		}
		if (index.SchemaVersion < 1 || index.RecordIds is null || index.DeliveryReceipts is null ||
			index.RecordIds.Length > MaxRecords || index.RecordIds.Distinct().Count() != index.RecordIds.Length)
		{
			throw new InvalidDataException("Invalid app notification state generation index.");
		}

		var records = new AppNotificationStateRecord[index.RecordIds.Length];
		for (var recordIndex = 0; recordIndex < index.RecordIds.Length; recordIndex++)
		{
			if ((recordIndex & 127) == 0)
			{
				RenewTransactionLock(owner);
			}
			var id = index.RecordIds[recordIndex];
			try
			{
				records[recordIndex] = DeserializeRecord(
					NativeMethods.GetItem(GetGenerationRecordKey(generation, id)),
					id);
			}
			catch (Exception currentException) when (IsCorruptStateException(currentException) && backupGeneration is not null)
			{
				try
				{
					records[recordIndex] = DeserializeRecord(
						NativeMethods.GetItem(GetGenerationRecordKey(backupGeneration, id)),
						id);
				}
				catch (Exception backupException) when (IsCorruptStateException(backupException))
				{
					throw new InvalidDataException(
						$"App notification state record {id} and its previous-generation backup are corrupt.",
						new AggregateException(currentException, backupException));
				}
			}
		}

		return NormalizeLegacy(new AppNotificationStateSnapshot(
			index.SchemaVersion,
			index.NextId,
			records,
			index.DeliveryReceipts));
	}

	private static AppNotificationStateSnapshot LoadLegacyAtomic(string owner)
	{
		var entries = GetEntries(LegacyRecordPrefix);
		if (entries.Length > MaxRecords)
		{
			throw new InvalidDataException("Invalid app notification record count.");
		}
		var records = new List<AppNotificationStateRecord>(entries.Length);
		for (var index = 0; index < entries.Length; index++)
		{
			if ((index & 127) == 0)
			{
				RenewTransactionLock(owner);
			}
			var entry = entries[index];
			if (!uint.TryParse(entry.Key, out var id) || id == 0)
			{
				throw new InvalidDataException("Invalid app notification record key.");
			}
			try
			{
				records.Add(DeserializeRecord(entry.Value, id));
			}
			catch (Exception currentException) when (IsCorruptStateException(currentException))
			{
				try
				{
					records.Add(DeserializeRecord(NativeMethods.GetItem(LegacyRecordBackupPrefix + id), id));
				}
				catch (Exception backupException) when (IsCorruptStateException(backupException))
				{
					throw new InvalidDataException(
						$"App notification state record {id} and its backup are corrupt.",
						new AggregateException(currentException, backupException));
				}
			}
		}
		var receipts = GetEntries(LegacyReceiptPrefix).Select(entry => entry.Value).ToArray();
		return NormalizeLegacy(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			1,
			records.OrderBy(record => record.CreatedUtc).ToArray(),
			receipts));
	}

	private static AppNotificationStateSnapshot LoadLegacySnapshot()
	{
		var primary = NativeMethods.GetItem(LegacyStateKey);
		var backup = NativeMethods.GetItem(LegacyBackupKey);
		if (primary is null && backup is null)
		{
			return AppNotificationStateSnapshot.Empty;
		}
		if (primary is null)
		{
			return DeserializeSnapshot(backup!);
		}
		try
		{
			return DeserializeSnapshot(primary);
		}
		catch (AppNotificationStateVersionException)
		{
			throw;
		}
		catch (Exception primaryException) when (IsCorruptStateException(primaryException))
		{
			if (backup is null)
			{
				throw new InvalidDataException("The app notification state is corrupt and no valid backup is available.", primaryException);
			}
			try
			{
				return DeserializeSnapshot(backup);
			}
			catch (Exception backupException) when (IsCorruptStateException(backupException))
			{
				throw new InvalidDataException(
					"The app notification state and its backup are corrupt.",
					new AggregateException(primaryException, backupException));
			}
		}
	}

	private static AppNotificationStateSnapshot NormalizeLegacy(AppNotificationStateSnapshot state)
	{
		if (state.SchemaVersion > AppNotificationStateSnapshot.CurrentSchemaVersion)
		{
			throw new AppNotificationStateVersionException(state.SchemaVersion);
		}
		if (state.SchemaVersion < 1)
		{
			throw new InvalidDataException("Invalid app notification state schema version.");
		}
		var receipts = (state.DeliveryReceipts ?? Array.Empty<string>())
			.Concat(state.SchemaVersion < 3
				? state.Records
					.Where(record => record.PostingState == AppNotificationPostingState.Shown)
					.Select(record => record.DeliveryCorrelation)
				: Array.Empty<string>())
			.Where(receipt => receipt.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.TakeLast(MaxRecords)
			.ToArray();
		var normalized = state with
		{
			SchemaVersion = AppNotificationStateSnapshot.CurrentSchemaVersion,
			NextId = state.NextId == 0 ? 1 : state.NextId,
			Records = state.Records.Select(record => record with
			{
				Revision = record.Revision <= 0 ? 1 : record.Revision,
				OperationOwner = record.OperationOwner ?? "legacy",
				OperationLeaseExpirationUtc = record.OperationLeaseExpirationUtc.ToUniversalTime(),
			}).ToArray(),
			DeliveryReceipts = receipts,
		};
		FileAppNotificationStatePersistence.ValidateSnapshot(normalized);
		return normalized;
	}

	private static IReadOnlyList<string> MergeReceipts(
		IReadOnlyCollection<string> baseline,
		IReadOnlyCollection<string> next,
		IReadOnlyCollection<string> latest)
	{
		var nextSet = next.ToHashSet(StringComparer.Ordinal);
		var removed = baseline.Where(receipt => !nextSet.Contains(receipt)).ToHashSet(StringComparer.Ordinal);
		var baselineSet = baseline.ToHashSet(StringComparer.Ordinal);
		return latest
			.Where(receipt => !removed.Contains(receipt))
			.Concat(next.Where(receipt => !baselineSet.Contains(receipt)))
			.Distinct(StringComparer.Ordinal)
			.TakeLast(MaxRecords)
			.ToArray();
	}

	private static void CleanupUnreferencedGenerations(string owner, GenerationManifest manifest)
	{
		foreach (var entry in GetEntries(GenerationPrefix))
		{
			var separator = entry.Key.IndexOf('.');
			var generation = separator < 0 ? entry.Key : entry.Key[..separator];
			if (generation != manifest.Current && generation != manifest.Previous)
			{
				RenewTransactionLock(owner);
				NativeMethods.RemoveItem(GenerationPrefix + entry.Key);
			}
		}
	}

	private static T WithLock<T>(Func<string, T> transaction)
	{
		var owner = Guid.NewGuid().ToString("N");
		if (!NativeMethods.AcquireTransactionLock(
			TransactionLockName,
			owner,
			TransactionLockTimeoutMilliseconds,
			TransactionLockLeaseMilliseconds))
		{
			throw new TimeoutException("Unable to acquire the app notification state transaction lock.");
		}
		try
		{
			return transaction(owner);
		}
		finally
		{
			NativeMethods.ReleaseTransactionLock(TransactionLockName, owner);
		}
	}

	private static void RenewTransactionLock(string owner)
		=> NativeMethods.RenewTransactionLock(TransactionLockName, owner, TransactionLockLeaseMilliseconds);

	private static AppNotificationStateRecord DeserializeRecord(string? value, uint expectedId)
	{
		if (value is null)
		{
			throw new InvalidDataException("The app notification state record is missing.");
		}
		var record = JsonSerializer.Deserialize(
			value,
			WebAssemblyAppNotificationStateSerializationContext.Default.AppNotificationStateRecord)
			?? throw new InvalidDataException("The app notification state record is empty.");
		if (record.Id != expectedId)
		{
			throw new InvalidDataException("The app notification state record ID does not match its key.");
		}
		FileAppNotificationStatePersistence.ValidateSnapshot(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			1,
			new[] { record }));
		return record;
	}

	private static AppNotificationStateSnapshot DeserializeSnapshot(string value)
	{
		var state = JsonSerializer.Deserialize(
			value,
			WebAssemblyAppNotificationStateSerializationContext.Default.AppNotificationStateSnapshot)
			?? throw new InvalidDataException("The app notification state is empty.");
		FileAppNotificationStatePersistence.ValidateSnapshot(state, allowPreviousSchemaVersions: true);
		return state;
	}

	private static GenerationManifest DeserializeManifest(string value)
	{
		var manifest = JsonSerializer.Deserialize(
			value,
			WebAssemblyAppNotificationStateSerializationContext.Default.GenerationManifest)
			?? throw new InvalidDataException("The app notification state manifest is empty.");
		if (manifest.Format != 3 ||
			!IsGenerationId(manifest.Current) ||
			(manifest.Previous is not null && !IsGenerationId(manifest.Previous)) ||
			!IsGenerationId(manifest.Version))
		{
			throw new InvalidDataException("The app notification state manifest is invalid.");
		}
		return manifest;
	}

	private static string SerializeManifest(GenerationManifest manifest)
		=> JsonSerializer.Serialize(manifest, WebAssemblyAppNotificationStateSerializationContext.Default.GenerationManifest);

	private static StorageEntry[] GetEntries(string prefix)
		=> JsonSerializer.Deserialize(
			NativeMethods.GetItems(prefix),
			WebAssemblyAppNotificationStateSerializationContext.Default.StorageEntryArray)
			?? throw new InvalidDataException("The app notification storage index is empty.");

	private static bool IsGenerationId(string value)
		=> Guid.TryParseExact(value, "N", out _);

	private static string GetGenerationIndexKey(string generation)
		=> GenerationPrefix + generation + ".index";

	private static string GetGenerationRecordKey(string generation, uint id)
		=> GenerationPrefix + generation + ".record." + id;

	private static AppNotificationStateSnapshot Clone(AppNotificationStateSnapshot state)
		=> state with
		{
			Records = state.Records.ToArray(),
			DeliveryReceipts = (state.DeliveryReceipts ?? Array.Empty<string>()).ToArray(),
		};

	private static bool IsCorruptStateException(Exception exception)
		=> exception is InvalidDataException or JsonException or ArgumentException or OverflowException;

	private sealed record GenerationManifest(int Format, string Current, string? Previous, string Version);

	private sealed record GenerationIndex(int SchemaVersion, uint NextId, uint[] RecordIds, string[] DeliveryReceipts);

	private sealed record StorageEntry(string Key, string Value);

	[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AppNotificationStateSnapshot))]
	[JsonSerializable(typeof(AppNotificationStateRecord))]
	[JsonSerializable(typeof(GenerationManifest))]
	[JsonSerializable(typeof(GenerationIndex))]
	[JsonSerializable(typeof(StorageEntry[]))]
	private partial class WebAssemblyAppNotificationStateSerializationContext : JsonSerializerContext
	{
	}

	private static partial class NativeMethods
	{
		private const string JsType = "globalThis.Windows.UI.Notifications.AppNotificationStatePersistence";

		[JSImport($"{JsType}.isSupported")]
		internal static partial bool IsSupported();

		[JSImport($"{JsType}.getItem")]
		internal static partial string? GetItem(string key);

		[JSImport($"{JsType}.getItems")]
		internal static partial string GetItems(string prefix);

		[JSImport($"{JsType}.setItem")]
		internal static partial void SetItem(string key, string value);

		[JSImport($"{JsType}.removeItem")]
		internal static partial void RemoveItem(string key);

		[JSImport($"{JsType}.createNotificationId")]
		internal static partial double CreateNotificationId();

		[JSImport($"{JsType}.acquireTransactionLock")]
		internal static partial bool AcquireTransactionLock(
			string lockName,
			string owner,
			double timeoutMilliseconds,
			double leaseMilliseconds);

		[JSImport($"{JsType}.renewTransactionLock")]
		internal static partial void RenewTransactionLock(
			string lockName,
			string owner,
			double leaseMilliseconds);

		[JSImport($"{JsType}.releaseTransactionLock")]
		internal static partial void ReleaseTransactionLock(string lockName, string owner);

		[JSImport($"{JsType}.commitTransactionVersion")]
		internal static partial bool CommitTransactionVersion(
			string lockName,
			string owner,
			string versionKey,
			string expectedVersion,
			string nextVersion);
	}
}
