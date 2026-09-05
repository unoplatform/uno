#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

internal sealed class FileToastNotificationSchedulePersistence : IToastNotificationSchedulePersistence
{
	private const int Magic = 0x554E4F53;
	private const int MaxStringBytes = 32_768;
	private const long MaxSnapshotBytes = 16 * 1024 * 1024;
	private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
	private readonly string _path;

	public FileToastNotificationSchedulePersistence(string path)
	{
		_path = path ?? throw new ArgumentNullException(nameof(path));
	}

	public ToastNotificationScheduleSnapshot Load()
	{
		if (!File.Exists(_path))
		{
			return ToastNotificationScheduleSnapshot.Empty;
		}

		try
		{
			return ReadSnapshot();
		}
		catch (Exception exception) when (IsCorruptStateException(exception))
		{
			var quarantinePath = _path + ".corrupt." + Guid.NewGuid().ToString("N");
			File.Move(_path, quarantinePath);
			return ToastNotificationScheduleSnapshot.Empty;
		}
	}

	private ToastNotificationScheduleSnapshot ReadSnapshot()
	{
		using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream.Length > MaxSnapshotBytes)
		{
			throw new InvalidDataException("Toast notification schedule state exceeds the maximum size.");
		}
		using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
		if (reader.ReadInt32() != Magic || reader.ReadInt32() != ToastNotificationScheduleSnapshot.CurrentSchemaVersion)
		{
			throw new InvalidDataException("Invalid toast notification schedule state header.");
		}
		var count = reader.ReadInt32();
		if (count < 0 || count > ToastNotificationScheduleStore.MaximumScheduledNotifications)
		{
			throw new InvalidDataException("Invalid scheduled notification count.");
		}

		var records = new List<ToastNotificationScheduleRecord>(count);
		var identifiers = new HashSet<string>(StringComparer.Ordinal);
		for (var index = 0; index < count; index++)
		{
			var record = ReadRecord(reader);
			if (!Guid.TryParseExact(record.ScheduleIdentifier, "N", out _) ||
				!identifiers.Add(record.ScheduleIdentifier) ||
				record.Status is not ToastNotificationScheduleStatus.Active and not ToastNotificationScheduleStatus.Canceling and not ToastNotificationScheduleStatus.Delivering ||
				record.Id.Length > 16 ||
				record.Tag.Length > 64 ||
				record.Group.Length > 64)
			{
				throw new InvalidDataException("Invalid scheduled notification record.");
			}
			ValidateRecord(record);
			records.Add(record);
		}
		if (stream.Position != stream.Length)
		{
			throw new InvalidDataException("Toast notification schedule state contains trailing data.");
		}
		return new ToastNotificationScheduleSnapshot(ToastNotificationScheduleSnapshot.CurrentSchemaVersion, records);
	}

	public void Save(ToastNotificationScheduleSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		if (state.SchemaVersion != ToastNotificationScheduleSnapshot.CurrentSchemaVersion ||
			state.Records.Count > ToastNotificationScheduleStore.MaximumScheduledNotifications)
		{
			throw new InvalidDataException("Invalid toast notification schedule state metadata.");
		}

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
				writer.Write(state.Records.Count);
				foreach (var record in state.Records)
				{
					if (!Guid.TryParseExact(record.ScheduleIdentifier, "N", out _) ||
						record.Status is not ToastNotificationScheduleStatus.Active and not ToastNotificationScheduleStatus.Canceling and not ToastNotificationScheduleStatus.Delivering ||
						record.Id.Length > 16 ||
						record.Tag.Length > 64 ||
						record.Group.Length > 64)
					{
						throw new InvalidDataException("Invalid scheduled notification record.");
					}
					ValidateRecord(record);
					WriteRecord(writer, record);
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

	private static ToastNotificationScheduleRecord ReadRecord(BinaryReader reader)
		=> new(
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

	private static void ValidateRecord(ToastNotificationScheduleRecord record)
	{
		if (record.NotificationMirroring is not NotificationMirroring.Allowed and not NotificationMirroring.Disabled ||
			record.SnoozeInterval is null && record.MaximumSnoozeCount != 0 ||
			record.SnoozeInterval is { } interval &&
				(interval < TimeSpan.FromMinutes(1) || interval > TimeSpan.FromMinutes(60) || record.MaximumSnoozeCount is < 1 or > 5))
		{
			throw new InvalidDataException("Invalid scheduled notification values.");
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

	private static bool IsCorruptStateException(Exception exception)
		=> exception is EndOfStreamException or InvalidDataException or DecoderFallbackException or FormatException or NotSupportedException or XmlException or ArgumentException or OverflowException;
}
