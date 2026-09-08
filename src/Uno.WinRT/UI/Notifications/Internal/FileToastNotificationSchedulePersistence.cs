#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Windows.Storage;

namespace Windows.UI.Notifications.Internal;

internal static class ToastNotificationSchedulePersistenceFactory
{
	public static IToastNotificationSchedulePersistence Create()
	{
		var folder = ApplicationData.Current.LocalFolder.Path;
		return string.IsNullOrEmpty(folder)
			? new InMemoryToastNotificationSchedulePersistence()
			: new FileToastNotificationSchedulePersistence(Path.Combine(folder, ".uno-toast-schedules-v1.bin"));
	}
}

internal sealed class FileToastNotificationSchedulePersistence : IToastNotificationSchedulePersistence, IMergingToastNotificationSchedulePersistence
{
	private const int Magic = 0x554E4F53;
	private const int MaxStringBytes = 32_768;
	private const long MaxSnapshotBytes = 16 * 1024 * 1024;
	private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
	private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
	private readonly string _path;
	private readonly string _backupPath;
	private readonly string _lockPath;

	public FileToastNotificationSchedulePersistence(string path)
	{
		_path = path ?? throw new ArgumentNullException(nameof(path));
		_backupPath = path + ".bak";
		_lockPath = path + ".lock";
	}

	public ToastNotificationScheduleSnapshot Load()
	{
		using var stateLock = AcquireLock();
		return Clone(Normalize(LoadCore()));
	}

	public void Save(ToastNotificationScheduleSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		ValidateSnapshot(state);
		using var stateLock = AcquireLock();
		SaveCore(state);
	}

	public ToastNotificationScheduleSnapshot MergeAndSave(
		ToastNotificationScheduleSnapshot baseline,
		ToastNotificationScheduleSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(baseline);
		ArgumentNullException.ThrowIfNull(state);
		baseline = Normalize(baseline);
		state = Normalize(state);
		ValidateSnapshot(baseline);
		ValidateSnapshot(state);

		using var stateLock = AcquireLock();
		var latest = Normalize(LoadCore());
		var merged = ToastNotificationScheduleSnapshotMerger.Merge(baseline, state, latest);
		ValidateSnapshot(merged);
		SaveCore(merged);
		return Clone(merged);
	}

	private ToastNotificationScheduleSnapshot LoadCore()
	{
		if (!File.Exists(_path) && !File.Exists(_backupPath))
		{
			return ToastNotificationScheduleSnapshot.Empty;
		}
		if (!File.Exists(_path))
		{
			return ReadSnapshot(_backupPath);
		}

		try
		{
			return ReadSnapshot(_path);
		}
		catch (Exception primaryException) when (IsCorruptStateException(primaryException))
		{
			if (File.Exists(_backupPath))
			{
				try
				{
					var backup = ReadSnapshot(_backupPath);
					Quarantine(_path);
					return backup;
				}
				catch (Exception backupException) when (IsCorruptStateException(backupException))
				{
					Quarantine(_path);
					Quarantine(_backupPath);
					return ToastNotificationScheduleSnapshot.Empty;
				}
			}

			Quarantine(_path);
			return ToastNotificationScheduleSnapshot.Empty;
		}
	}

	private void SaveCore(ToastNotificationScheduleSnapshot state)
	{
		ValidateSnapshot(state);
		var directory = Path.GetDirectoryName(_path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var temporaryPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			using (var stream = File.Open(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (var writer = new BinaryWriter(stream, StrictUtf8, leaveOpen: true))
			{
				writer.Write(Magic);
				writer.Write(ToastNotificationScheduleSnapshot.CurrentSchemaVersion);
				writer.Write(state.Revision);
				writer.Write(state.Records.Count);
				foreach (var record in state.Records)
				{
					WriteRecord(writer, record);
				}
				var operations = GetOperations(state);
				writer.Write(operations.Count);
				foreach (var operation in operations)
				{
					WriteOperation(writer, operation);
				}
				writer.Flush();
				if (stream.Length > MaxSnapshotBytes)
				{
					throw new InvalidDataException("Toast notification schedule state exceeds the maximum size.");
				}
				stream.Flush(flushToDisk: true);
			}

			if (File.Exists(_path))
			{
				try
				{
					ReadSnapshot(_path);
					File.Copy(_path, _backupPath, overwrite: true);
				}
				catch (Exception exception) when (IsCorruptStateException(exception))
				{
					// Keep the last known-good backup when replacing a corrupt primary snapshot.
				}
				File.Replace(temporaryPath, _path, null, ignoreMetadataErrors: true);
			}
			else
			{
				File.Move(temporaryPath, _path);
			}
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	private FileStream AcquireLock()
	{
		var directory = Path.GetDirectoryName(_lockPath);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}
		var timeout = Stopwatch.StartNew();
		while (true)
		{
			try
			{
				return File.Open(_lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
			}
			catch (IOException) when (timeout.Elapsed < LockTimeout)
			{
				Thread.Sleep(25);
			}
		}
	}

	private static ToastNotificationScheduleSnapshot ReadSnapshot(string path)
	{
		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream.Length > MaxSnapshotBytes)
		{
			throw new InvalidDataException("Toast notification schedule state exceeds the maximum size.");
		}
		using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
		if (reader.ReadInt32() != Magic)
		{
			throw new InvalidDataException("Invalid toast notification schedule state header.");
		}
		var schemaVersion = reader.ReadInt32();
		if (schemaVersion is < 1 or > ToastNotificationScheduleSnapshot.CurrentSchemaVersion)
		{
			throw new InvalidDataException("Unsupported toast notification schedule state version.");
		}
		var revision = schemaVersion >= 2 ? reader.ReadInt64() : 0;
		var count = reader.ReadInt32();
		if (revision < 0 || count < 0 || count > ToastNotificationScheduleStore.MaximumScheduledNotifications)
		{
			throw new InvalidDataException("Invalid toast notification schedule state metadata.");
		}

		var records = new List<ToastNotificationScheduleRecord>(count);
		var identifiers = new HashSet<string>(StringComparer.Ordinal);
		for (var index = 0; index < count; index++)
		{
			var record = ReadRecord(reader, schemaVersion);
			if (!identifiers.Add(record.ScheduleIdentifier))
			{
				throw new InvalidDataException("Duplicate scheduled notification identifier.");
			}
			ValidateRecord(record);
			if (record.Revision > revision)
			{
				throw new InvalidDataException("Scheduled notification revision exceeds the snapshot revision.");
			}
			records.Add(record);
		}
		var operations = new List<ToastNotificationNativeOperation>();
		if (schemaVersion >= 3)
		{
			var operationCount = reader.ReadInt32();
			if (operationCount < 0 || operationCount > ToastNotificationScheduleStore.MaximumScheduledNotifications)
			{
				throw new InvalidDataException("Invalid scheduled notification native operation count.");
			}
			var operationIdentifiers = new HashSet<string>(StringComparer.Ordinal);
			for (var index = 0; index < operationCount; index++)
			{
				var operation = ReadOperation(reader);
				if (!operationIdentifiers.Add(operation.ScheduleIdentifier))
				{
					throw new InvalidDataException("Duplicate scheduled notification native operation identifier.");
				}
				ValidateOperation(operation, records);
				if (operation.Revision > revision)
				{
					throw new InvalidDataException("Scheduled notification native operation revision exceeds the snapshot revision.");
				}
				operations.Add(operation);
			}
		}
		if (stream.Position != stream.Length)
		{
			throw new InvalidDataException("Toast notification schedule state contains trailing data.");
		}
		return new ToastNotificationScheduleSnapshot(schemaVersion, records, revision, operations);
	}

	private static ToastNotificationScheduleRecord ReadRecord(BinaryReader reader, int schemaVersion)
	{
		var record = new ToastNotificationScheduleRecord(
			ReadString(reader),
			ReadString(reader),
			ReadDateTimeOffset(reader),
			reader.ReadBoolean() ? ReadDateTimeOffset(reader) : null,
			ReadString(reader),
			ReadString(reader),
			ReadString(reader),
			reader.ReadBoolean(),
			reader.ReadBoolean() ? TimeSpan.FromTicks(reader.ReadInt64()) : null,
			reader.ReadUInt32(),
			(ToastNotificationScheduleStatus)reader.ReadInt32(),
			(NotificationMirroring)reader.ReadInt32());
		if (schemaVersion >= 2)
		{
			record = record with { Revision = reader.ReadInt64() };
		}
		if (schemaVersion >= 3)
		{
			record = record with
			{
				DeliveryClaimOwner = ReadString(reader),
				DeliveryClaimToken = ReadString(reader),
				DeliveryClaimExpirationUtc = ReadDateTimeOffset(reader),
			};
		}
		return record;
	}

	private static void WriteRecord(BinaryWriter writer, ToastNotificationScheduleRecord record)
	{
		WriteString(writer, record.ScheduleIdentifier);
		WriteString(writer, record.Payload);
		WriteDateTimeOffset(writer, record.DeliveryTimeUtc);
		writer.Write(record.ExpirationTimeUtc is not null);
		if (record.ExpirationTimeUtc is { } expiration)
		{
			WriteDateTimeOffset(writer, expiration);
		}
		WriteString(writer, record.Id);
		WriteString(writer, record.Tag);
		WriteString(writer, record.Group);
		writer.Write(record.SuppressPopup);
		writer.Write(record.SnoozeInterval is not null);
		if (record.SnoozeInterval is { } interval)
		{
			writer.Write(interval.Ticks);
		}
		writer.Write(record.MaximumSnoozeCount);
		writer.Write((int)record.Status);
		writer.Write((int)record.NotificationMirroring);
		writer.Write(record.Revision);
		WriteString(writer, record.DeliveryClaimOwner);
		WriteString(writer, record.DeliveryClaimToken);
		WriteDateTimeOffset(writer, record.DeliveryClaimExpirationUtc);
	}

	private static ToastNotificationNativeOperation ReadOperation(BinaryReader reader)
		=> new(
			ReadString(reader),
			(ToastNotificationNativeOperationKind)reader.ReadInt32(),
			ReadString(reader),
			reader.ReadInt64(),
			reader.ReadInt64());

	private static void WriteOperation(BinaryWriter writer, ToastNotificationNativeOperation operation)
	{
		WriteString(writer, operation.ScheduleIdentifier);
		writer.Write((int)operation.Kind);
		WriteString(writer, operation.OperationIdentifier);
		writer.Write(operation.RecordRevision);
		writer.Write(operation.Revision);
	}

	private static DateTimeOffset ReadDateTimeOffset(BinaryReader reader)
		=> new(reader.ReadInt64(), TimeSpan.Zero);

	private static void WriteDateTimeOffset(BinaryWriter writer, DateTimeOffset value)
		=> writer.Write(value.ToUniversalTime().Ticks);

	private static string ReadString(BinaryReader reader)
	{
		var length = reader.ReadInt32();
		if (length < 0 || length > MaxStringBytes)
		{
			throw new InvalidDataException("Invalid scheduled notification string length.");
		}
		var bytes = reader.ReadBytes(length);
		if (bytes.Length != length)
		{
			throw new EndOfStreamException();
		}
		return StrictUtf8.GetString(bytes);
	}

	private static void WriteString(BinaryWriter writer, string value)
	{
		var bytes = StrictUtf8.GetBytes(value);
		if (bytes.Length > MaxStringBytes)
		{
			throw new InvalidDataException("Scheduled notification string is too large.");
		}
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	private static void ValidateSnapshot(ToastNotificationScheduleSnapshot state)
	{
		if (state.SchemaVersion != ToastNotificationScheduleSnapshot.CurrentSchemaVersion ||
			state.Revision < 0 ||
			state.Records.Count > ToastNotificationScheduleStore.MaximumScheduledNotifications ||
			GetOperations(state).Count > ToastNotificationScheduleStore.MaximumScheduledNotifications)
		{
			throw new InvalidDataException("Invalid toast notification schedule state metadata.");
		}

		var identifiers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var record in state.Records)
		{
			if (!identifiers.Add(record.ScheduleIdentifier))
			{
				throw new InvalidDataException("Duplicate scheduled notification identifier.");
			}
			ValidateRecord(record);
			if (record.Revision > state.Revision)
			{
				throw new InvalidDataException("Scheduled notification revision exceeds the snapshot revision.");
			}
		}
		var operationIdentifiers = new HashSet<string>(StringComparer.Ordinal);
		foreach (var operation in GetOperations(state))
		{
			if (!operationIdentifiers.Add(operation.ScheduleIdentifier))
			{
				throw new InvalidDataException("Duplicate scheduled notification native operation identifier.");
			}
			ValidateOperation(operation, state.Records);
			if (operation.Revision > state.Revision)
			{
				throw new InvalidDataException("Scheduled notification native operation revision exceeds the snapshot revision.");
			}
		}
	}

	private static void ValidateRecord(ToastNotificationScheduleRecord record)
	{
		if (!Guid.TryParseExact(record.ScheduleIdentifier, "N", out _) ||
			record.Revision < 0 ||
			record.Status is not ToastNotificationScheduleStatus.Active and not ToastNotificationScheduleStatus.Canceling and not ToastNotificationScheduleStatus.Delivering ||
			record.Id.Length > 16 ||
			record.Tag.Length > 64 ||
			record.Group.Length > 64 ||
			record.NotificationMirroring is not NotificationMirroring.Allowed and not NotificationMirroring.Disabled ||
			record.DeliveryClaimOwner.Length > MaxStringBytes ||
			record.DeliveryClaimToken.Length > MaxStringBytes ||
			record.Status != ToastNotificationScheduleStatus.Delivering &&
				(record.DeliveryClaimOwner.Length > 0 ||
					record.DeliveryClaimToken.Length > 0 ||
					record.DeliveryClaimExpirationUtc != DateTimeOffset.MinValue) ||
			record.Status == ToastNotificationScheduleStatus.Delivering &&
				(record.DeliveryClaimOwner.Length == 0) != (record.DeliveryClaimToken.Length == 0) ||
			record.Status == ToastNotificationScheduleStatus.Delivering &&
				record.DeliveryClaimOwner.Length == 0 &&
				record.DeliveryClaimExpirationUtc != DateTimeOffset.MinValue ||
			record.DeliveryClaimOwner.Length > 0 &&
				record.DeliveryClaimExpirationUtc == DateTimeOffset.MinValue ||
			record.SnoozeInterval is null && record.MaximumSnoozeCount != 0 ||
			record.SnoozeInterval is { } interval &&
				(interval < TimeSpan.FromMinutes(1) || interval > TimeSpan.FromMinutes(60) || record.MaximumSnoozeCount is < 1 or > 5))
		{
			throw new InvalidDataException("Invalid scheduled notification record.");
		}
		try
		{
			Microsoft.Windows.AppNotifications.Internal.AppNotificationPayloadParser.Parse(record.Payload);
		}
		catch (Exception exception) when (exception is FormatException or NotSupportedException or XmlException or ArgumentException)
		{
			throw new InvalidDataException("Invalid scheduled notification payload.", exception);
		}
	}

	private static void ValidateOperation(
		ToastNotificationNativeOperation operation,
		IReadOnlyCollection<ToastNotificationScheduleRecord> records)
	{
		var record = records.FirstOrDefault(candidate => candidate.ScheduleIdentifier == operation.ScheduleIdentifier);
		if (!Guid.TryParseExact(operation.ScheduleIdentifier, "N", out _) ||
			!Guid.TryParseExact(operation.OperationIdentifier, "N", out _) ||
			operation.Kind is not ToastNotificationNativeOperationKind.Schedule and
				not ToastNotificationNativeOperationKind.Cancel and
				not ToastNotificationNativeOperationKind.Retry ||
			operation.RecordRevision < 0 ||
			operation.Revision < 0 ||
			operation.Kind is ToastNotificationNativeOperationKind.Schedule or ToastNotificationNativeOperationKind.Retry &&
				(record is null ||
					record.Status != ToastNotificationScheduleStatus.Active ||
					record.Revision != operation.RecordRevision))
		{
			throw new InvalidDataException("Invalid scheduled notification native operation.");
		}
	}

	private static ToastNotificationScheduleSnapshot Normalize(ToastNotificationScheduleSnapshot state)
	{
		if (state.SchemaVersion is < 1 or > ToastNotificationScheduleSnapshot.CurrentSchemaVersion)
		{
			throw new InvalidDataException("Unsupported toast notification schedule state version.");
		}
		return state with
		{
			SchemaVersion = ToastNotificationScheduleSnapshot.CurrentSchemaVersion,
			Records = state.Records
				.Select(record => record with { Revision = Math.Max(0, record.Revision) })
				.ToArray(),
			Revision = Math.Max(0, state.Revision),
			NativeOperations = GetOperations(state)
				.Select(operation => operation with
				{
					RecordRevision = Math.Max(0, operation.RecordRevision),
					Revision = Math.Max(0, operation.Revision),
				})
				.ToArray(),
		};
	}

	private static ToastNotificationScheduleSnapshot Clone(ToastNotificationScheduleSnapshot state)
		=> ToastNotificationScheduleSnapshotMerger.Clone(state);

	private static IReadOnlyList<ToastNotificationNativeOperation> GetOperations(ToastNotificationScheduleSnapshot state)
		=> ToastNotificationScheduleSnapshotMerger.GetOperations(state);

	private static void Quarantine(string path)
	{
		if (File.Exists(path))
		{
			File.Move(path, path + ".corrupt." + Guid.NewGuid().ToString("N"));
		}
	}

	private static bool IsCorruptStateException(Exception exception)
		=> exception is EndOfStreamException or InvalidDataException or DecoderFallbackException or FormatException or NotSupportedException or XmlException or ArgumentException or OverflowException;
}
