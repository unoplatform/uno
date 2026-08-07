#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record LinuxAppNotificationNativeIdRecord(uint NotificationId, uint NativeId);

internal sealed record LinuxAppNotificationNativeStateSnapshot(
	int SchemaVersion,
	string ServerOwner,
	IReadOnlyList<LinuxAppNotificationNativeIdRecord> Records)
{
	public const int CurrentSchemaVersion = 1;

	public static LinuxAppNotificationNativeStateSnapshot Empty { get; } = new(CurrentSchemaVersion, string.Empty, Array.Empty<LinuxAppNotificationNativeIdRecord>());
}

internal interface ILinuxAppNotificationNativeStatePersistence
{
	LinuxAppNotificationNativeStateSnapshot Load();

	void Save(LinuxAppNotificationNativeStateSnapshot state);
}

internal sealed class InMemoryLinuxAppNotificationNativeStatePersistence : ILinuxAppNotificationNativeStatePersistence
{
	private LinuxAppNotificationNativeStateSnapshot _state;

	public InMemoryLinuxAppNotificationNativeStatePersistence(LinuxAppNotificationNativeStateSnapshot? state = null)
	{
		_state = Clone(state ?? LinuxAppNotificationNativeStateSnapshot.Empty);
	}

	public LinuxAppNotificationNativeStateSnapshot Load() => Clone(_state);

	public void Save(LinuxAppNotificationNativeStateSnapshot state) => _state = Clone(state);

	private static LinuxAppNotificationNativeStateSnapshot Clone(LinuxAppNotificationNativeStateSnapshot state)
		=> state with { Records = state.Records.ToArray() };
}

internal sealed class LinuxAppNotificationNativeStateStore
{
	internal const int MaximumRecords = 10_000;
	private readonly object _gate = new();
	private readonly ILinuxAppNotificationNativeStatePersistence _persistence;
	private LinuxAppNotificationNativeStateSnapshot _state;

	public LinuxAppNotificationNativeStateStore(ILinuxAppNotificationNativeStatePersistence persistence)
	{
		_persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
		var loaded = persistence.Load();
		_state = loaded.SchemaVersion == LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion && IsValid(loaded)
			? loaded with { Records = loaded.Records.ToArray() }
			: LinuxAppNotificationNativeStateSnapshot.Empty;
	}

	public bool SetServerOwner(string serverOwner)
	{
		ArgumentNullException.ThrowIfNull(serverOwner);
		lock (_gate)
		{
			if (_state.ServerOwner == serverOwner)
			{
				return false;
			}
			Commit(new LinuxAppNotificationNativeStateSnapshot(
				LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion,
				serverOwner,
				Array.Empty<LinuxAppNotificationNativeIdRecord>()));
			return true;
		}
	}

	public void Set(uint notificationId, uint nativeId)
	{
		if (notificationId == 0 || nativeId == 0)
		{
			throw new ArgumentException("Notification IDs must be non-zero.");
		}
		lock (_gate)
		{
			var records = _state.Records
				.Where(record => record.NotificationId != notificationId && record.NativeId != nativeId)
				.Append(new LinuxAppNotificationNativeIdRecord(notificationId, nativeId))
				.ToArray();
			if (records.Length > MaximumRecords)
			{
				throw new InvalidOperationException("The Linux app-notification native ID store is full.");
			}
			Commit(_state with { Records = records });
		}
	}

	public uint? GetNativeId(uint notificationId)
	{
		lock (_gate)
		{
			return _state.Records.FirstOrDefault(record => record.NotificationId == notificationId)?.NativeId;
		}
	}

	public uint? GetNotificationId(uint nativeId)
	{
		lock (_gate)
		{
			return _state.Records.FirstOrDefault(record => record.NativeId == nativeId)?.NotificationId;
		}
	}

	public IReadOnlyList<LinuxAppNotificationNativeIdRecord> GetAll()
	{
		lock (_gate)
		{
			return _state.Records.ToArray();
		}
	}

	public bool RemoveByNotificationId(uint notificationId)
		=> Remove(record => record.NotificationId == notificationId);

	public bool RemoveByNativeId(uint nativeId)
		=> Remove(record => record.NativeId == nativeId);

	public void RemoveAll()
	{
		lock (_gate)
		{
			if (_state.Records.Count > 0)
			{
				Commit(_state with { Records = Array.Empty<LinuxAppNotificationNativeIdRecord>() });
			}
		}
	}

	private bool Remove(Func<LinuxAppNotificationNativeIdRecord, bool> predicate)
	{
		lock (_gate)
		{
			var records = _state.Records.Where(record => !predicate(record)).ToArray();
			if (records.Length == _state.Records.Count)
			{
				return false;
			}
			Commit(_state with { Records = records });
			return true;
		}
	}

	private void Commit(LinuxAppNotificationNativeStateSnapshot state)
	{
		_persistence.Save(state);
		_state = state;
	}

	private static bool IsValid(LinuxAppNotificationNativeStateSnapshot state)
		=> state.Records.Count <= MaximumRecords &&
			state.Records.All(record => record.NotificationId != 0 && record.NativeId != 0) &&
			state.Records.Select(record => record.NotificationId).Distinct().Count() == state.Records.Count &&
			state.Records.Select(record => record.NativeId).Distinct().Count() == state.Records.Count;
}

internal static class LinuxAppNotificationNativeStatePersistenceFactory
{
	public static ILinuxAppNotificationNativeStatePersistence Create()
	{
		var folder = ApplicationData.Current.LocalFolder.Path;
		return string.IsNullOrEmpty(folder)
			? new InMemoryLinuxAppNotificationNativeStatePersistence()
			: new FileLinuxAppNotificationNativeStatePersistence(Path.Combine(folder, ".uno-linux-appnotifications-v1.bin"));
	}
}

internal sealed class FileLinuxAppNotificationNativeStatePersistence : ILinuxAppNotificationNativeStatePersistence
{
	private const int Magic = 0x554E4F4C;
	private const int MaximumOwnerBytes = 1024;
	private readonly string _path;

	public FileLinuxAppNotificationNativeStatePersistence(string path)
	{
		_path = path ?? throw new ArgumentNullException(nameof(path));
	}

	public LinuxAppNotificationNativeStateSnapshot Load()
	{
		if (!File.Exists(_path))
		{
			return LinuxAppNotificationNativeStateSnapshot.Empty;
		}
		try
		{
			using var stream = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
			if (reader.ReadInt32() != Magic || reader.ReadInt32() != LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion)
			{
				throw new InvalidDataException("Invalid Linux app-notification native state header.");
			}
			var ownerLength = reader.ReadInt32();
			if (ownerLength < 0 || ownerLength > MaximumOwnerBytes)
			{
				throw new InvalidDataException("Invalid Linux app-notification server owner.");
			}
			var ownerBytes = reader.ReadBytes(ownerLength);
			if (ownerBytes.Length != ownerLength)
			{
				throw new EndOfStreamException();
			}
			var count = reader.ReadInt32();
			if (count < 0 || count > LinuxAppNotificationNativeStateStore.MaximumRecords)
			{
				throw new InvalidDataException("Invalid Linux app-notification native state count.");
			}
			var records = new LinuxAppNotificationNativeIdRecord[count];
			for (var index = 0; index < count; index++)
			{
				records[index] = new LinuxAppNotificationNativeIdRecord(reader.ReadUInt32(), reader.ReadUInt32());
			}
			if (stream.Position != stream.Length)
			{
				throw new InvalidDataException("Linux app-notification native state contains trailing data.");
			}
			return new LinuxAppNotificationNativeStateSnapshot(
				LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion,
				Encoding.UTF8.GetString(ownerBytes),
				records);
		}
		catch (Exception exception) when (exception is IOException or InvalidDataException or EndOfStreamException or DecoderFallbackException)
		{
			File.Move(_path, _path + ".corrupt." + Guid.NewGuid().ToString("N"));
			return LinuxAppNotificationNativeStateSnapshot.Empty;
		}
	}

	public void Save(LinuxAppNotificationNativeStateSnapshot state)
	{
		ArgumentNullException.ThrowIfNull(state);
		var ownerBytes = Encoding.UTF8.GetBytes(state.ServerOwner);
		if (state.SchemaVersion != LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion ||
			state.Records.Count > LinuxAppNotificationNativeStateStore.MaximumRecords ||
			ownerBytes.Length > MaximumOwnerBytes)
		{
			throw new InvalidDataException("Invalid Linux app-notification native state.");
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
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(Magic);
				writer.Write(state.SchemaVersion);
				writer.Write(ownerBytes.Length);
				writer.Write(ownerBytes);
				writer.Write(state.Records.Count);
				foreach (var record in state.Records)
				{
					writer.Write(record.NotificationId);
					writer.Write(record.NativeId);
				}
				writer.Flush();
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
}