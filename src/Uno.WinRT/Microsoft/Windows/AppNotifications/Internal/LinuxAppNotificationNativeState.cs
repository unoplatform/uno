#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Storage;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record LinuxAppNotificationNativeIdRecord(uint NotificationId, uint NativeId);

internal sealed class LinuxAppNotificationNativeStateStore
{
	// Native IDs and activation commands are valid only while their D-Bus signal subscription is live.
	internal const int MaximumRecords = 10_000;
	private readonly object _gate = new();
	private readonly Dictionary<uint, LinuxAppNotificationCommand> _commands = new();
	private long _nextSessionId;
	private long _activeSessionId;
	private IReadOnlyList<LinuxAppNotificationNativeIdRecord> _records = Array.Empty<LinuxAppNotificationNativeIdRecord>();

	public LinuxAppNotificationNativeStateSession StartSession(string serverOwner)
	{
		ArgumentException.ThrowIfNullOrEmpty(serverOwner);
		lock (_gate)
		{
			var sessionId = ++_nextSessionId;
			_activeSessionId = sessionId;
			_records = Array.Empty<LinuxAppNotificationNativeIdRecord>();
			_commands.Clear();
			return new LinuxAppNotificationNativeStateSession(this, sessionId, serverOwner);
		}
	}

	internal bool IsActive(long sessionId)
	{
		lock (_gate)
		{
			return IsActiveCore(sessionId);
		}
	}

	internal bool TrySet(long sessionId, uint notificationId, uint nativeId, LinuxAppNotificationCommand? command)
	{
		if (notificationId == 0 || nativeId == 0)
		{
			throw new ArgumentException("Notification IDs must be non-zero.");
		}
		lock (_gate)
		{
			if (!IsActiveCore(sessionId))
			{
				return false;
			}
			var records = _records
				.Where(record => record.NotificationId != notificationId && record.NativeId != nativeId)
				.Append(new LinuxAppNotificationNativeIdRecord(notificationId, nativeId))
				.ToArray();
			if (records.Length > MaximumRecords)
			{
				throw new InvalidOperationException("The Linux app-notification native ID store is full.");
			}
			foreach (var record in _records.Where(record => record.NotificationId == notificationId || record.NativeId == nativeId))
			{
				_commands.Remove(record.NotificationId);
			}
			_records = records;
			if (command is not null)
			{
				_commands[notificationId] = command;
			}
			return true;
		}
	}

	internal uint? GetNativeId(long sessionId, uint notificationId)
	{
		lock (_gate)
		{
			return IsActiveCore(sessionId)
				? _records.FirstOrDefault(record => record.NotificationId == notificationId)?.NativeId
				: null;
		}
	}

	internal uint? GetNotificationId(long sessionId, uint nativeId)
	{
		lock (_gate)
		{
			return IsActiveCore(sessionId)
				? _records.FirstOrDefault(record => record.NativeId == nativeId)?.NotificationId
				: null;
		}
	}

	internal LinuxAppNotificationCommand? GetCommand(long sessionId, uint nativeId)
	{
		lock (_gate)
		{
			if (!IsActiveCore(sessionId) ||
				_records.FirstOrDefault(record => record.NativeId == nativeId) is not { } record)
			{
				return null;
			}
			return _commands.GetValueOrDefault(record.NotificationId);
		}
	}

	internal IReadOnlyList<LinuxAppNotificationNativeIdRecord> GetAll(long sessionId)
	{
		lock (_gate)
		{
			return IsActiveCore(sessionId)
				? _records.ToArray()
				: Array.Empty<LinuxAppNotificationNativeIdRecord>();
		}
	}

	internal bool RemoveByNotificationId(long sessionId, uint notificationId)
		=> Remove(sessionId, record => record.NotificationId == notificationId);

	internal bool RemoveByNativeId(long sessionId, uint nativeId)
		=> Remove(sessionId, record => record.NativeId == nativeId);

	internal bool RemoveAll(long sessionId)
	{
		lock (_gate)
		{
			if (!IsActiveCore(sessionId) || _records.Count == 0)
			{
				return false;
			}
			_records = Array.Empty<LinuxAppNotificationNativeIdRecord>();
			_commands.Clear();
			return true;
		}
	}

	internal bool EndSession(long sessionId)
	{
		lock (_gate)
		{
			if (!IsActiveCore(sessionId))
			{
				return false;
			}
			_activeSessionId = 0;
			_records = Array.Empty<LinuxAppNotificationNativeIdRecord>();
			_commands.Clear();
			return true;
		}
	}

	private bool Remove(long sessionId, Func<LinuxAppNotificationNativeIdRecord, bool> predicate)
	{
		lock (_gate)
		{
			if (!IsActiveCore(sessionId))
			{
				return false;
			}
			var records = _records.Where(record => !predicate(record)).ToArray();
			if (records.Length == _records.Count)
			{
				return false;
			}
			foreach (var record in _records.Where(predicate))
			{
				_commands.Remove(record.NotificationId);
			}
			_records = records;
			return true;
		}
	}

	private bool IsActiveCore(long sessionId)
		=> sessionId != 0 && sessionId == _activeSessionId;
}

internal sealed class LinuxAppNotificationNativeStateSession : IDisposable
{
	private readonly LinuxAppNotificationNativeStateStore _store;
	private readonly long _sessionId;
	private int _isDisposed;

	internal LinuxAppNotificationNativeStateSession(LinuxAppNotificationNativeStateStore store, long sessionId, string serverOwner)
	{
		_store = store;
		_sessionId = sessionId;
		ServerOwner = serverOwner;
	}

	public string ServerOwner { get; }

	public bool IsActive => Volatile.Read(ref _isDisposed) == 0 && _store.IsActive(_sessionId);

	public bool TrySet(uint notificationId, uint nativeId, LinuxAppNotificationCommand? command = null)
		=> Volatile.Read(ref _isDisposed) == 0 && _store.TrySet(_sessionId, notificationId, nativeId, command);

	public uint? GetNativeId(uint notificationId)
		=> Volatile.Read(ref _isDisposed) == 0 ? _store.GetNativeId(_sessionId, notificationId) : null;

	public uint? GetNotificationId(uint nativeId)
		=> Volatile.Read(ref _isDisposed) == 0 ? _store.GetNotificationId(_sessionId, nativeId) : null;

	public LinuxAppNotificationCommand? GetCommand(uint nativeId)
		=> Volatile.Read(ref _isDisposed) == 0 ? _store.GetCommand(_sessionId, nativeId) : null;

	public IReadOnlyList<LinuxAppNotificationNativeIdRecord> GetAll()
		=> Volatile.Read(ref _isDisposed) == 0
			? _store.GetAll(_sessionId)
			: Array.Empty<LinuxAppNotificationNativeIdRecord>();

	public bool RemoveByNotificationId(uint notificationId)
		=> Volatile.Read(ref _isDisposed) == 0 && _store.RemoveByNotificationId(_sessionId, notificationId);

	public bool RemoveByNativeId(uint nativeId)
		=> Volatile.Read(ref _isDisposed) == 0 && _store.RemoveByNativeId(_sessionId, nativeId);

	public bool RemoveAll()
		=> Volatile.Read(ref _isDisposed) == 0 && _store.RemoveAll(_sessionId);

	internal bool End()
		=> Interlocked.Exchange(ref _isDisposed, 1) == 0 && _store.EndSession(_sessionId);

	public void Dispose() => End();
}

internal static class LinuxAppNotificationNativeStateStoreFactory
{
	private const string LegacyStateFileName = ".uno-linux-appnotifications-v1.bin";

	public static LinuxAppNotificationNativeStateStore Create()
	{
		ClearLegacyPersistedState();
		return new LinuxAppNotificationNativeStateStore();
	}

	private static void ClearLegacyPersistedState()
	{
		var folder = ApplicationData.Current.LocalFolder.Path;
		if (string.IsNullOrEmpty(folder))
		{
			return;
		}

		// The freedesktop protocol cannot verify native IDs or deliver activation across process lifetimes.
		try
		{
			File.Delete(Path.Combine(folder, LegacyStateFileName));
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			// The legacy file is never read again, so cleanup is best effort.
		}
	}
}
