#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using Uno.Foundation.Logging;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Internal;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleToastNotificationDeliveredHistoryStore
{
	private const int Magic = 0x554E4F48;
	private const int Version = 1;
	private const int MaximumStringBytes = 32_768;
	private const long MaximumRecordBytes = 256 * 1024;
	private const string HistoryDirectoryName = ".uno-apple-toast-delivered-history-v1";
	private const string HistoryExtension = ".history";
	private static readonly Encoding StrictUtf8 = new UTF8Encoding(
		encoderShouldEmitUTF8Identifier: false,
		throwOnInvalidBytes: true);
	private static readonly object _gate = new();

	public static bool TryPersist(ToastNotificationScheduleRecord record)
	{
		if (GetHistoryDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryPersist(record, directoryPath);
	}

	internal static bool TryPersist(ToastNotificationScheduleRecord record, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(record);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		if (Normalize(record) is not { } normalized)
		{
			return false;
		}

		lock (_gate)
		{
			var path = GetHistoryPath(directoryPath, normalized.ScheduleIdentifier);
			if (TryRead(path, out var existing))
			{
				return existing == normalized;
			}
			var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				Directory.CreateDirectory(directoryPath);
				using (var stream = new FileStream(
					temporaryPath,
					FileMode.CreateNew,
					FileAccess.Write,
					FileShare.Read,
					bufferSize: 4096,
					FileOptions.WriteThrough))
				using (var writer = new BinaryWriter(stream, StrictUtf8, leaveOpen: true))
				{
					WriteRecord(writer, normalized);
					writer.Flush();
					stream.Flush(flushToDisk: true);
				}
				File.Move(temporaryPath, path);
				return true;
			}
			catch (IOException exception)
			{
				if (TryRead(path, out existing) && existing == normalized)
				{
					return true;
				}
				LogWarning("Apple delivered-toast history could not be persisted.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple delivered-toast history could not be persisted.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple delivered-toast history could not be persisted.", exception);
				return false;
			}
			finally
			{
				TryDeleteTemporaryFile(temporaryPath);
			}
		}
	}

	public static IReadOnlyList<ToastNotificationScheduleRecord>? GetAll()
	{
		if (GetHistoryDirectoryPath() is not { } directoryPath)
		{
			return null;
		}
		return GetAll(directoryPath);
	}

	internal static IReadOnlyList<ToastNotificationScheduleRecord>? GetAll(string directoryPath)
	{
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		lock (_gate)
		{
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					return Array.Empty<ToastNotificationScheduleRecord>();
				}
				var records = new List<ToastNotificationScheduleRecord>();
				foreach (var path in Directory.EnumerateFiles(directoryPath, "*" + HistoryExtension, SearchOption.TopDirectoryOnly))
				{
					if (!TryRead(path, out var record) ||
						!Path.GetFileNameWithoutExtension(path).Equals(record.ScheduleIdentifier, StringComparison.Ordinal))
					{
						throw new InvalidDataException("Apple delivered-toast history contains an invalid record.");
					}
					records.Add(record);
				}
				return records
					.OrderBy(record => record.DeliveryTimeUtc)
					.ThenBy(record => record.ScheduleIdentifier, StringComparer.Ordinal)
					.ToArray();
			}
			catch (IOException exception)
			{
				LogWarning("Apple delivered-toast history could not be read.", exception);
				return null;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple delivered-toast history could not be read.", exception);
				return null;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple delivered-toast history could not be read.", exception);
				return null;
			}
		}
	}

	public static bool TryRemove(string scheduleIdentifier)
	{
		if (GetHistoryDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryRemove(scheduleIdentifier, directoryPath);
	}

	internal static bool TryRemove(string scheduleIdentifier, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		if (!Guid.TryParseExact(scheduleIdentifier, "N", out _))
		{
			return false;
		}

		lock (_gate)
		{
			try
			{
				File.Delete(GetHistoryPath(directoryPath, scheduleIdentifier));
				return true;
			}
			catch (IOException exception)
			{
				LogWarning("Apple delivered-toast history could not be removed.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple delivered-toast history could not be removed.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple delivered-toast history could not be removed.", exception);
				return false;
			}
		}
	}

	public static bool TryCleanup(IReadOnlyCollection<string> activeScheduleIdentifiers)
	{
		if (GetHistoryDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryCleanup(activeScheduleIdentifiers, directoryPath);
	}

	internal static bool TryCleanup(IReadOnlyCollection<string> activeScheduleIdentifiers, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(activeScheduleIdentifiers);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		var active = activeScheduleIdentifiers
			.Where(identifier => Guid.TryParseExact(identifier, "N", out _))
			.ToHashSet(StringComparer.Ordinal);

		lock (_gate)
		{
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					return true;
				}
				foreach (var path in Directory.EnumerateFiles(directoryPath, "*" + HistoryExtension, SearchOption.TopDirectoryOnly))
				{
					var identifier = Path.GetFileNameWithoutExtension(path);
					if (identifier is null || !active.Contains(identifier))
					{
						File.Delete(path);
					}
				}
				foreach (var temporaryPath in Directory.EnumerateFiles(directoryPath, "*.tmp", SearchOption.TopDirectoryOnly))
				{
					File.Delete(temporaryPath);
				}
				return true;
			}
			catch (IOException exception)
			{
				LogWarning("Apple delivered-toast history could not be cleaned up.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple delivered-toast history could not be cleaned up.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple delivered-toast history could not be cleaned up.", exception);
				return false;
			}
		}
	}

	private static ToastNotificationScheduleRecord? Normalize(ToastNotificationScheduleRecord record)
	{
		if (!Guid.TryParseExact(record.ScheduleIdentifier, "N", out _) ||
			record.Id.Length > 16 ||
			record.Tag.Length > 64 ||
			record.Group.Length > 64 ||
			record.NotificationMirroring is not NotificationMirroring.Allowed and not NotificationMirroring.Disabled ||
			record.SnoozeInterval is null && record.MaximumSnoozeCount != 0 ||
			record.SnoozeInterval is { } interval &&
				(interval < TimeSpan.FromMinutes(1) || interval > TimeSpan.FromMinutes(60) || record.MaximumSnoozeCount is < 1 or > 5))
		{
			return null;
		}
		try
		{
			AppNotificationPayloadParser.Parse(record.Payload);
		}
		catch (FormatException)
		{
			return null;
		}
		catch (NotSupportedException)
		{
			return null;
		}
		catch (XmlException)
		{
			return null;
		}
		catch (ArgumentException)
		{
			return null;
		}
		return record with
		{
			DeliveryTimeUtc = record.DeliveryTimeUtc.ToUniversalTime(),
			ExpirationTimeUtc = record.ExpirationTimeUtc?.ToUniversalTime(),
			Status = ToastNotificationScheduleStatus.Active,
			Revision = 0,
			DeliveryClaimOwner = string.Empty,
			DeliveryClaimToken = string.Empty,
			DeliveryClaimExpirationUtc = DateTimeOffset.MinValue,
		};
	}

	private static bool TryRead(string path, out ToastNotificationScheduleRecord record)
	{
		record = null!;
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (stream.Length is <= 0 or > MaximumRecordBytes)
			{
				return false;
			}
			using var reader = new BinaryReader(stream, StrictUtf8, leaveOpen: true);
			if (reader.ReadInt32() != Magic || reader.ReadInt32() != Version)
			{
				return false;
			}
			var candidate = new ToastNotificationScheduleRecord(
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
				ToastNotificationScheduleStatus.Active,
				(NotificationMirroring)reader.ReadInt32());
			if (stream.Position != stream.Length || Normalize(candidate) is not { } normalized)
			{
				return false;
			}
			record = normalized;
			return true;
		}
		catch (IOException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (SecurityException)
		{
			return false;
		}
	}

	private static void WriteRecord(BinaryWriter writer, ToastNotificationScheduleRecord record)
	{
		writer.Write(Magic);
		writer.Write(Version);
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
		writer.Write((int)record.NotificationMirroring);
	}

	private static string ReadString(BinaryReader reader)
	{
		var length = reader.ReadInt32();
		if (length < 0 || length > MaximumStringBytes)
		{
			throw new InvalidDataException("Invalid Apple delivered-toast history string length.");
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
		if (bytes.Length > MaximumStringBytes)
		{
			throw new InvalidDataException("Apple delivered-toast history string is too large.");
		}
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	private static DateTimeOffset ReadDateTimeOffset(BinaryReader reader)
		=> new(reader.ReadInt64(), TimeSpan.Zero);

	private static void WriteDateTimeOffset(BinaryWriter writer, DateTimeOffset value)
		=> writer.Write(value.ToUniversalTime().Ticks);

	private static string? GetHistoryDirectoryPath()
	{
		var localFolderPath = ApplicationData.Current.LocalFolder.Path;
		return string.IsNullOrEmpty(localFolderPath)
			? null
			: Path.Combine(localFolderPath, HistoryDirectoryName);
	}

	private static string GetHistoryPath(string directoryPath, string scheduleIdentifier)
		=> Path.Combine(directoryPath, scheduleIdentifier + HistoryExtension);

	private static void TryDeleteTemporaryFile(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (SecurityException)
		{
		}
	}

	private static void LogWarning(string message, Exception exception)
	{
		if (typeof(AppleToastNotificationDeliveredHistoryStore).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AppleToastNotificationDeliveredHistoryStore).Log().LogWarning($"{message} {exception.Message}");
		}
	}
}
