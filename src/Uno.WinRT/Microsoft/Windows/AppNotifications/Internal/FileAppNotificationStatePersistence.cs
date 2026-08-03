#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppNotificationStatePersistenceFactory
{
	public static IAppNotificationStatePersistence Create()
	{
		var folder = ApplicationData.Current.LocalFolder.Path;
		return string.IsNullOrEmpty(folder)
			? new InMemoryAppNotificationStatePersistence()
			: new FileAppNotificationStatePersistence(Path.Combine(folder, ".uno-appnotifications-v1.bin"));
	}
}

internal sealed class FileAppNotificationStatePersistence : IAppNotificationStatePersistence
{
	private const int Magic = 0x554E4F4E;
	private const int MaxRecords = 10_000;
	private const int MaxStringBytes = 32_768;
	private const long MaxSnapshotBytes = 16 * 1024 * 1024;
	private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
	private readonly string _path;
	private readonly string _backupPath;

	public FileAppNotificationStatePersistence(string path)
	{
		_path = path ?? throw new ArgumentNullException(nameof(path));
		_backupPath = path + ".bak";
	}

	public AppNotificationStateSnapshot Load()
	{
		if (!File.Exists(_path) && !File.Exists(_backupPath))
		{
			return AppNotificationStateSnapshot.Empty;
		}
		if (!File.Exists(_path))
		{
			return ReadSnapshot(_backupPath);
		}

		try
		{
			return ReadSnapshot(_path);
		}
		catch (AppNotificationStateVersionException)
		{
			throw;
		}
		catch (Exception primaryException) when (IsCorruptStateException(primaryException))
		{
			if (!File.Exists(_backupPath))
			{
				throw new InvalidDataException("The app notification state is corrupt and no valid backup is available.", primaryException);
			}

			try
			{
				return ReadSnapshot(_backupPath);
			}
			catch (Exception backupException) when (IsCorruptStateException(backupException))
			{
				throw new InvalidDataException("The app notification state and its backup are corrupt.", new AggregateException(primaryException, backupException));
			}
		}
	}

	public void Save(AppNotificationStateSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
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
				writer.Write(state.SchemaVersion);
				writer.Write(state.NextId);
				writer.Write(state.Records.Count);
				foreach (var record in state.Records)
				{
					WriteRecord(writer, record);
				}
				var deliveryReceipts = state.DeliveryReceipts ?? Array.Empty<string>();
				writer.Write(deliveryReceipts.Count);
				foreach (var receipt in deliveryReceipts)
				{
					WriteString(writer, receipt);
				}
				writer.Flush();
				if (stream.Length > MaxSnapshotBytes)
				{
					throw new InvalidDataException("App notification state exceeds the maximum snapshot size.");
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

	private static AppNotificationStateSnapshot ReadSnapshot(string path)
	{
		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream.Length > MaxSnapshotBytes)
		{
			throw new InvalidDataException("App notification state exceeds the maximum snapshot size.");
		}
		using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
		if (reader.ReadInt32() != Magic)
		{
			throw new InvalidDataException("Invalid app notification state header.");
		}
		var schemaVersion = reader.ReadInt32();
		if (schemaVersion is < 1 or > AppNotificationStateSnapshot.CurrentSchemaVersion)
		{
			throw new AppNotificationStateVersionException(schemaVersion);
		}
		var nextId = reader.ReadUInt32();
		if (nextId == 0)
		{
			throw new InvalidDataException("Invalid app notification next ID.");
		}
		var count = reader.ReadInt32();
		if (count < 0 || count > MaxRecords)
		{
			throw new InvalidDataException("Invalid app notification record count.");
		}

		var records = new List<AppNotificationStateRecord>(count);
		var ids = new HashSet<uint>();
		for (var index = 0; index < count; index++)
		{
			var record = ReadRecord(reader, schemaVersion);
			ValidateRecord(record, ids);
			records.Add(record);
		}
		var deliveryReceipts = new List<string>();
		if (schemaVersion >= 3)
		{
			var receiptCount = reader.ReadInt32();
			if (receiptCount < 0 || receiptCount > MaxRecords)
			{
				throw new InvalidDataException("Invalid app notification delivery receipt count.");
			}
			var receiptSet = new HashSet<string>(StringComparer.Ordinal);
			for (var index = 0; index < receiptCount; index++)
			{
				var receipt = ReadString(reader);
				if (receipt.Length == 0 || !receiptSet.Add(receipt))
				{
					throw new InvalidDataException("Invalid app notification delivery receipt.");
				}
				deliveryReceipts.Add(receipt);
			}
		}
		if (stream.Position != stream.Length)
		{
			throw new InvalidDataException("App notification state contains trailing data.");
		}
		return new AppNotificationStateSnapshot(schemaVersion, nextId, records, deliveryReceipts);
	}

	private static AppNotificationStateRecord ReadRecord(BinaryReader reader, int schemaVersion)
	{
		var id = reader.ReadUInt32();
		var payload = ReadString(reader);
		var tag = ReadString(reader);
		var group = ReadString(reader);
		var createdUtc = ReadDateTimeOffset(reader);
		var expirationUtc = ReadDateTimeOffset(reader);
		var expiresOnReboot = reader.ReadBoolean();
		var bootIdentifier = ReadNullableString(reader);
		var priority = (AppNotificationPriority)reader.ReadInt32();
		var suppressDisplay = reader.ReadBoolean();
		var postingState = (AppNotificationPostingState)reader.ReadInt32();
		AppNotificationProgressSnapshot? progress = null;
		if (reader.ReadBoolean())
		{
			progress = new AppNotificationProgressSnapshot(
				reader.ReadUInt32(),
				ReadString(reader),
				reader.ReadDouble(),
				ReadString(reader),
				ReadString(reader));
		}
		var deliveryCorrelation = schemaVersion >= 2 ? ReadString(reader) : string.Empty;
		return new AppNotificationStateRecord(
			id,
			payload,
			tag,
			group,
			createdUtc,
			expirationUtc,
			expiresOnReboot,
			bootIdentifier,
			priority,
			suppressDisplay,
			postingState,
			progress,
			deliveryCorrelation);
	}

	private static void WriteRecord(BinaryWriter writer, AppNotificationStateRecord record)
	{
		writer.Write(record.Id);
		WriteString(writer, record.Payload);
		WriteString(writer, record.Tag);
		WriteString(writer, record.Group);
		WriteDateTimeOffset(writer, record.CreatedUtc);
		WriteDateTimeOffset(writer, record.ExpirationUtc);
		writer.Write(record.ExpiresOnReboot);
		WriteNullableString(writer, record.BootIdentifier);
		writer.Write((int)record.Priority);
		writer.Write(record.SuppressDisplay);
		writer.Write((int)record.PostingState);
		writer.Write(record.Progress is not null);
		if (record.Progress is { } progress)
		{
			writer.Write(progress.SequenceNumber);
			WriteString(writer, progress.Title);
			writer.Write(progress.Value);
			WriteString(writer, progress.ValueStringOverride);
			WriteString(writer, progress.Status);
		}
		WriteString(writer, record.DeliveryCorrelation);
	}

	private static DateTimeOffset ReadDateTimeOffset(BinaryReader reader)
		=> new(reader.ReadInt64(), TimeSpan.Zero);

	private static void WriteDateTimeOffset(BinaryWriter writer, DateTimeOffset value)
		=> writer.Write(value.ToUniversalTime().Ticks);

	private static string? ReadNullableString(BinaryReader reader)
		=> reader.ReadBoolean() ? ReadString(reader) : null;

	private static void WriteNullableString(BinaryWriter writer, string? value)
	{
		writer.Write(value is not null);
		if (value is not null)
		{
			WriteString(writer, value);
		}
	}

	private static string ReadString(BinaryReader reader)
	{
		var length = reader.ReadInt32();
		if (length < 0 || length > MaxStringBytes)
		{
			throw new InvalidDataException("Invalid app notification state string length.");
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
			throw new InvalidDataException("App notification state string is too large.");
		}
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	private static void ValidateSnapshot(AppNotificationStateSnapshot state)
	{
		if (state.SchemaVersion != AppNotificationStateSnapshot.CurrentSchemaVersion)
		{
			throw new AppNotificationStateVersionException(state.SchemaVersion);
		}
		if (state.NextId == 0 || state.Records.Count > MaxRecords)
		{
			throw new InvalidDataException("Invalid app notification state metadata.");
		}

		var ids = new HashSet<uint>();
		long encodedBytes = sizeof(int) * 3 + sizeof(uint);
		foreach (var record in state.Records)
		{
			ValidateRecord(record, ids);
			encodedBytes += GetEncodedByteCount(record.Payload);
			encodedBytes += GetEncodedByteCount(record.Tag);
			encodedBytes += GetEncodedByteCount(record.Group);
			encodedBytes += GetEncodedByteCount(record.DeliveryCorrelation);
			encodedBytes += record.BootIdentifier is null ? 0 : GetEncodedByteCount(record.BootIdentifier);
			if (record.Progress is { } progress)
			{
				encodedBytes += GetEncodedByteCount(progress.Title);
				encodedBytes += GetEncodedByteCount(progress.ValueStringOverride);
				encodedBytes += GetEncodedByteCount(progress.Status);
			}
			if (encodedBytes > MaxSnapshotBytes)
			{
				throw new InvalidDataException("App notification state exceeds the maximum snapshot size.");
			}
		}
		var deliveryReceipts = state.DeliveryReceipts ?? Array.Empty<string>();
		if (deliveryReceipts.Count > MaxRecords ||
			deliveryReceipts.Any(receipt => receipt.Length == 0) ||
			deliveryReceipts.Distinct(StringComparer.Ordinal).Count() != deliveryReceipts.Count)
		{
			throw new InvalidDataException("Invalid app notification delivery receipts.");
		}
		foreach (var receipt in deliveryReceipts)
		{
			encodedBytes += GetEncodedByteCount(receipt);
		}
		if (encodedBytes > MaxSnapshotBytes)
		{
			throw new InvalidDataException("App notification state exceeds the maximum snapshot size.");
		}
	}

	private static void ValidateRecord(AppNotificationStateRecord record, ISet<uint> ids)
	{
		if (record.Id == 0 || !ids.Add(record.Id))
		{
			throw new InvalidDataException("App notification state contains an invalid or duplicate ID.");
		}
		if (record.Priority is not AppNotificationPriority.Default and not AppNotificationPriority.High ||
			record.PostingState is not AppNotificationPostingState.Posting and not AppNotificationPostingState.Shown and not AppNotificationPostingState.Updating)
		{
			throw new InvalidDataException("App notification state contains an unknown enum value.");
		}
		if (record.Progress is { SequenceNumber: 0 })
		{
			throw new InvalidDataException("App notification state contains an invalid progress sequence.");
		}
		try
		{
			AppNotificationPayloadParser.Parse(record.Payload);
		}
		catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
		{
			throw new InvalidDataException("App notification state contains an invalid payload.", exception);
		}
	}

	private static int GetEncodedByteCount(string value)
	{
		var count = StrictUtf8.GetByteCount(value);
		if (count > MaxStringBytes)
		{
			throw new InvalidDataException("App notification state string is too large.");
		}
		return sizeof(int) + count;
	}

	private static bool IsCorruptStateException(Exception exception)
		=> exception is InvalidDataException or EndOfStreamException or DecoderFallbackException or ArgumentException or OverflowException;
}

internal sealed class AppNotificationStateVersionException : NotSupportedException
{
	public AppNotificationStateVersionException(int version)
		: base($"App notification state schema version {version} is not supported.")
	{
	}
}
