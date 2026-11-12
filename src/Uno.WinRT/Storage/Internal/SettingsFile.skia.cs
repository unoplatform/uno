#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.Storage.Internal;

/// <summary>
/// A settings file that several processes of the same app can share: every change is applied to what is on disk at
/// that moment rather than to this process's copy, so concurrent instances don't undo each other's writes.
/// </summary>
/// <remarks>
/// Writers hold an exclusive lock on a sibling ".lock" file and readers a shared one (flock on Unix, sharing modes on
/// Windows). The locks are advisory: they only exclude other instances going through this class, and they do nothing
/// when an app turns off .NET file locking (System.IO.DisableFileLocking) or on a file system that ignores flock.
/// </remarks>
internal sealed class SettingsFile
{
	private static readonly TimeSpan _readLockTimeout = TimeSpan.FromSeconds(1);

	// File systems record modification times as coarsely as 2 s (FAT; HFS+ is 1 s), so two writes that close together
	// can leave identical stamps. A stamp read within this window of the file's last write is not trusted.
	private static readonly TimeSpan _stampGranularity = TimeSpan.FromSeconds(2);

	private static readonly TimeSpan _staleTemporaryFileAge = TimeSpan.FromMinutes(10);
	private const int Attempts = 5;

	private readonly object _gate = new();
	private readonly TimeSpan _writeLockTimeout;
	private readonly string _path;
	private readonly string _lockPath;

	// What is on disk as of the last read or write, and the changes this process could not save yet. Values is the
	// first with the second applied, so a change that could not be written stays visible here until it is.
	private Dictionary<string, string> _disk = new(StringComparer.Ordinal);
	private readonly List<Change> _pending = new();
	private Dictionary<string, string> _values = new(StringComparer.Ordinal);

	private Stamp? _diskStamp;
	private DateTime _diskReadAt;
	private bool _temporaryFilesSwept;

	private readonly record struct Stamp(DateTime LastWrite, long Length);

	/// <summary>A key set (Value not null), a key removed (Value null), or every key removed (Key null).</summary>
	private readonly record struct Change(string? Key, string? Value);

	internal SettingsFile(string path, TimeSpan? writeLockTimeout = null)
	{
		_path = path;
		_writeLockTimeout = writeLockTimeout ?? TimeSpan.FromSeconds(5);
		_lockPath = path + ".lock";

		lock (_gate)
		{
			Refresh();
		}
	}

	/// <summary>
	/// The current settings, including the changes other processes made since the last access.
	/// </summary>
	internal IReadOnlyDictionary<string, string> Values
	{
		get
		{
			lock (_gate)
			{
				Refresh();
				return _values;
			}
		}
	}

	internal void Set(string key, string value)
		=> Update(new Change(key, value));

	internal bool Remove(string key)
		=> Update(new Change(key, null));

	internal void Clear()
		=> Update(new Change(null, null));

	/// <summary>
	/// Applies <paramref name="change"/> to the file's current content under an exclusive lock, then replaces the file.
	/// </summary>
	/// <returns>Whether the change made a difference to the settings.</returns>
	private bool Update(Change change)
	{
		// Waiting for another process happens outside the gate, so this process's readers aren't held up meanwhile.
		using var fileLock = AcquireLock(exclusive: true, _writeLockTimeout);

		lock (_gate)
		{
			var canWrite = false;

			if (fileLock is not null && TryReadForUpdate(out var disk, out var stamp))
			{
				canWrite = true;
				_disk = disk;
				_diskStamp = stamp;
				_diskReadAt = DateTime.UtcNow;
			}

			var updated = WithPending(new Dictionary<string, string>(_disk, StringComparer.Ordinal));
			var changed = Apply(change, updated);
			_pending.Add(change);

			if (canWrite)
			{
				if (!changed && _pending.Count == 1)
				{
					// Nothing to save: don't rewrite the file and make every other instance read it again.
					_pending.Clear();
				}
				else if (TryWrite(updated))
				{
					_pending.Clear();
					_disk = updated;
					_diskStamp = GetStamp();
					_diskReadAt = DateTime.UtcNow;
				}

				SweepTemporaryFiles();
			}

			_values = WithPending(_disk);
			return changed;
		}
	}

	/// <summary>Reloads the file if another process changed it since it was last read.</summary>
	private void Refresh()
	{
		var stamp = GetStamp();

		if (stamp == _diskStamp && (stamp is null || _diskReadAt - stamp.Value.LastWrite >= _stampGranularity))
		{
			return;
		}

		using var fileLock = AcquireLock(exclusive: false, _readLockTimeout);

		if (fileLock is null)
		{
			// A writer is busy; keep the current copy and look again on the next access.
			return;
		}

		stamp = GetStamp();

		try
		{
			_disk = ReadOrEmpty();
			_diskStamp = stamp;
			_diskReadAt = DateTime.UtcNow;
			_values = WithPending(_disk);
		}
		catch (Exception e)
		{
			// Keep the last good copy. A damaged file is not read again until it changes; the next write repairs it.
			// Any other failure is retried on the next access.
			if (e is EndOfStreamException or FormatException)
			{
				_diskStamp = stamp;
				_diskReadAt = DateTime.UtcNow;
			}

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"Failed to read settings from {_path}: {e.Message}");
			}
		}
	}

	private bool TryReadForUpdate(out Dictionary<string, string> values, out Stamp? stamp)
	{
		for (var attempt = 1; ; attempt++)
		{
			stamp = GetStamp();

			try
			{
				values = ReadOrEmpty();
				return true;
			}
			catch (Exception e) when (e is EndOfStreamException or FormatException && attempt < 3)
			{
				// An instance running an older version of this code writes the file in place, without the lock,
				// so a short read can be its write in progress rather than damage.
				Thread.Sleep(50 * attempt);
			}
			catch (Exception e) when (e is EndOfStreamException or FormatException)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"The settings file {_path} is damaged; it was moved aside and rebuilt from this process's last good copy", e);
				}

				try
				{
					File.Move(_path, _path + ".corrupt", overwrite: true);
				}
				catch (Exception moveError)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"Failed to move the damaged settings file {_path} aside", moveError);
					}

					values = null!;
					return false;
				}

				values = new Dictionary<string, string>(_disk, StringComparer.Ordinal);
				return true;
			}
			catch (Exception e) when (attempt < Attempts)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to read settings from {_path} (attempt {attempt}), retrying: {e.Message}");
				}

				Thread.Sleep(20 * attempt);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to read settings from {_path}; the change is kept in memory and saved on a later write", e);
				}

				values = null!;
				return false;
			}
		}
	}

	private Dictionary<string, string> ReadOrEmpty()
	{
		FileStream stream;

		try
		{
			stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}
		catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
		{
			return new(StringComparer.Ordinal);
		}

		using (stream)
		using (var reader = new BinaryReader(stream))
		{
			var count = reader.ReadInt32();

			// Each entry takes at least two bytes, so a larger count can only come from damage.
			if (count < 0 || count > (stream.Length - sizeof(int)) / 2)
			{
				throw new FormatException($"Invalid settings count {count}");
			}

			var values = new Dictionary<string, string>(count, StringComparer.Ordinal);

			for (var i = 0; i < count; i++)
			{
				var key = reader.ReadString();
				values[key] = reader.ReadString();
			}

			return values;
		}
	}

	private bool TryWrite(Dictionary<string, string> values)
	{
		for (var attempt = 1; ; attempt++)
		{
			try
			{
				Write(values);
				return true;
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException && attempt < Attempts)
			{
				// On Windows another program holding the file open (an indexer, antivirus, a sync client) makes the
				// replace fail for a moment.
				Thread.Sleep(20 * attempt);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to write settings to {_path}; the change is kept in memory and saved on a later write", e);
				}

				return false;
			}
		}
	}

	private void Write(Dictionary<string, string> values)
	{
		// A crash or a full disk part-way through leaves the previous file in place rather than a truncated one.
		var temporaryPath = $"{_path}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";

		try
		{
			using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(values.Count);

				foreach (var pair in values)
				{
					writer.Write(pair.Key);
					writer.Write(pair.Value);
				}

				writer.Flush();
				stream.Flush(flushToDisk: true);
			}

			File.Move(temporaryPath, _path, overwrite: true);
		}
		finally
		{
			File.Delete(temporaryPath);
		}
	}

	/// <summary>Removes temporary files that a process which crashed mid-write left behind.</summary>
	private void SweepTemporaryFiles()
	{
		if (_temporaryFilesSwept)
		{
			return;
		}

		_temporaryFilesSwept = true;

		try
		{
			foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(_path)!, Path.GetFileName(_path) + ".*.tmp"))
			{
				if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > _staleTemporaryFileAge)
				{
					File.Delete(file);
				}
			}
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Failed to remove stale temporary settings files: {e.Message}");
			}
		}
	}

	private FileStream? AcquireLock(bool exclusive, TimeSpan timeout)
	{
		var elapsed = Stopwatch.StartNew();

		while (true)
		{
			try
			{
				if (exclusive)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
					return new FileStream(_lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				}

				return new FileStream(_lockPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
			}
			catch (IOException e) when (IsHeldByAnotherProcess(e) && elapsed.Elapsed < timeout)
			{
				Thread.Sleep(10);
			}
			catch (Exception e) when (!exclusive && e is DirectoryNotFoundException)
			{
				// Nothing has been written yet, so there is nothing to read either.
				return null;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Failed to lock the settings file {_path}: {e.Message}");
				}

				return null;
			}
		}
	}

	private static bool IsHeldByAnotherProcess(IOException e)
		=> OperatingSystem.IsWindows()
			// ERROR_SHARING_VIOLATION, ERROR_LOCK_VIOLATION
			? e.HResult is unchecked((int)0x80070020) or unchecked((int)0x80070021)
			// EWOULDBLOCK from flock: 11 on Linux, 35 on macOS and the BSDs
			: e.HResult is 11 or 35;

	private Dictionary<string, string> WithPending(Dictionary<string, string> disk)
	{
		if (_pending.Count == 0)
		{
			return disk;
		}

		var values = new Dictionary<string, string>(disk, StringComparer.Ordinal);

		foreach (var change in _pending)
		{
			Apply(change, values);
		}

		return values;
	}

	private static bool Apply(Change change, Dictionary<string, string> values)
	{
		if (change.Key is null)
		{
			var hadValues = values.Count > 0;
			values.Clear();
			return hadValues;
		}

		if (change.Value is null)
		{
			return values.Remove(change.Key);
		}

		var changed = !values.TryGetValue(change.Key, out var previous) || previous != change.Value;
		values[change.Key] = change.Value;
		return changed;
	}

	private Stamp? GetStamp()
	{
		try
		{
			var info = new FileInfo(_path);
			return info.Exists ? new Stamp(info.LastWriteTimeUtc, info.Length) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
