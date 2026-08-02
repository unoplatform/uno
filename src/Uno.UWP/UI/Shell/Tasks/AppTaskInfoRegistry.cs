#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Uno.Foundation.Logging;
using Uno.UI.Shell.Tasks;

namespace Windows.UI.Shell.Tasks;

internal static class AppTaskInfoRegistry
{
	private static readonly object Gate = new();
	private static Dictionary<string, AppTaskInfoSnapshot>? _tasks;
	private static IAppTaskInfoStore? _store;
	private static IAppTaskInfoExtension? _extension;
	private static IAppTaskInfoExtension? _testExtension;
	private static string? _storeValue;
	private static long _revision;

	internal static bool IsSupported()
	{
		var extension = TryGetSupportedExtension();
		if (extension is null)
		{
			return false;
		}

		AppTaskInfoSnapshot[] snapshots;
		long revision;
		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				EnsureLoadedLocked(store, forceReload: true);
				snapshots = GetSnapshotsLocked();
				revision = _revision;
			}
		}

		extension.Synchronize(revision, snapshots);
		return true;
	}

	internal static AppTaskInfo[] FindAll()
	{
		var extension = TryGetSupportedExtension();
		if (extension is null)
		{
			return Array.Empty<AppTaskInfo>();
		}

		AppTaskInfoSnapshot[] snapshots;
		long revision;
		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				EnsureLoadedLocked(store, forceReload: true);
				snapshots = GetSnapshotsLocked();
				revision = _revision;
			}
		}

		extension.Synchronize(revision, snapshots);
		return snapshots.Select(static snapshot => new AppTaskInfo(snapshot)).ToArray();
	}

	internal static AppTaskInfo Create(
		string title,
		string subtitle,
		Uri deepLink,
		Uri iconUri,
		AppTaskContentSnapshot content)
	{
		var extension = TryGetSupportedExtension()
			?? throw new PlatformNotSupportedException("App tasks are not supported on the current platform.");

		var snapshot = new AppTaskInfoSnapshot(
			Guid.NewGuid().ToString("D"),
			title,
			subtitle,
			deepLink,
			iconUri,
			AppTaskState.Running,
			DateTimeOffset.UtcNow,
			null,
			HiddenByUser: false,
			content);

		AppTaskInfoSnapshot[] snapshots;
		long revision;
		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				EnsureLoadedLocked(store, forceReload: true);
				_tasks!.Add(snapshot.Id, snapshot);
				try
				{
					(revision, snapshots) = PersistLocked(store);
				}
				catch
				{
					_tasks.Remove(snapshot.Id);
					throw;
				}
			}
		}

		extension.Synchronize(revision, snapshots);
		return new(snapshot);
	}

	internal static AppTaskInfoSnapshot? TryGet(string id)
	{
		lock (Gate)
		{
			if (_tasks is not null)
			{
				return _tasks.TryGetValue(id, out var loadedSnapshot) ? loadedSnapshot : null;
			}
		}

		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				if (_tasks is null)
				{
					EnsureLoadedLocked(store, forceReload: true);
				}

				return _tasks!.TryGetValue(id, out var snapshot) ? snapshot : null;
			}
		}
	}

	internal static AppTaskInfoSnapshot? Update(
		string id,
		Func<AppTaskInfoSnapshot, AppTaskInfoSnapshot> update)
	{
		ArgumentNullException.ThrowIfNull(update);

		AppTaskInfoSnapshot updated;
		AppTaskInfoSnapshot[] snapshots;
		long revision;
		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				EnsureLoadedLocked(store, forceReload: true);
				if (!_tasks!.TryGetValue(id, out var current))
				{
					return null;
				}

				updated = update(current);
				_tasks[id] = updated;
				try
				{
					(revision, snapshots) = PersistLocked(store);
				}
				catch
				{
					_tasks[id] = current;
					throw;
				}
			}
		}

		SynchronizeIfSupported(revision, snapshots);
		return updated;
	}

	internal static AppTaskInfoSnapshot? Remove(string id)
	{
		AppTaskInfoSnapshot removed;
		AppTaskInfoSnapshot[] snapshots;
		long revision;
		var store = GetStore();
		using (store.AcquireLock())
		{
			lock (Gate)
			{
				EnsureLoadedLocked(store, forceReload: true);
				if (!_tasks!.Remove(id, out removed!))
				{
					return null;
				}

				try
				{
					(revision, snapshots) = PersistLocked(store);
				}
				catch
				{
					_tasks.Add(id, removed);
					throw;
				}
			}
		}

		SynchronizeIfSupported(revision, snapshots);
		return removed;
	}

	internal static void SetHiddenByUser(string id, bool hiddenByUser)
	{
		_ = Update(id, snapshot => snapshot with { HiddenByUser = hiddenByUser });
	}

	internal static void ConfigureForTests(IAppTaskInfoStore store, IAppTaskInfoExtension extension)
	{
		ArgumentNullException.ThrowIfNull(store);
		ArgumentNullException.ThrowIfNull(extension);

		lock (Gate)
		{
			_store = store;
			_testExtension = extension;
			_extension = null;
			_tasks = null;
			_storeValue = null;
			_revision = 0;
		}
	}

	internal static void ResetAfterTests()
	{
		lock (Gate)
		{
			_store = null;
			_testExtension = null;
			_extension = null;
			_tasks = null;
			_storeValue = null;
			_revision = 0;
		}
	}

	private static IAppTaskInfoExtension? TryGetSupportedExtension()
	{
		IAppTaskInfoExtension? extension;
		lock (Gate)
		{
			extension = _testExtension;
			if (extension is null)
			{
				_extension ??= AppTaskInfoPlatform.CreateExtension();
				extension = _extension;
			}
		}

		if (extension is null)
		{
			return null;
		}

		var isSupported = extension.IsSupported();
		extension.SetAvailability(isSupported);
		return isSupported ? extension : null;
	}

	private static void EnsureLoadedLocked(IAppTaskInfoStore store, bool forceReload)
	{
		if (_tasks is not null && !forceReload)
		{
			return;
		}

		var value = store.Read();
		if (_tasks is not null && value == _storeValue)
		{
			return;
		}

		Dictionary<string, AppTaskInfoSnapshot> loadedTasks;
		try
		{
			var snapshots = value is null
				? Array.Empty<AppTaskInfoSnapshot>()
				: AppTaskInfoSerializer.Deserialize(value);
			loadedTasks = new(StringComparer.Ordinal);
			foreach (var snapshot in snapshots)
			{
				if (!loadedTasks.TryAdd(snapshot.Id, snapshot))
				{
					throw new InvalidDataException($"The persisted app task ID '{snapshot.Id}' is duplicated.");
				}
			}
		}
		catch (Exception error) when (error is JsonException or InvalidDataException or FormatException)
		{
			if (typeof(AppTaskInfoRegistry).Log().IsEnabled(LogLevel.Error))
			{
				typeof(AppTaskInfoRegistry).Log().Error(
					"The persisted app task registry is invalid and will be quarantined.",
					error);
			}

			store.Quarantine();
			value = null;
			loadedTasks = new(StringComparer.Ordinal);
		}

		_tasks = loadedTasks;
		_storeValue = value;
		_revision++;
	}

	private static (long Revision, AppTaskInfoSnapshot[] Snapshots) PersistLocked(IAppTaskInfoStore store)
	{
		var snapshots = GetSnapshotsLocked();
		var value = AppTaskInfoSerializer.Serialize(snapshots);
		store.Write(value);
		_storeValue = value;
		return (++_revision, snapshots);
	}

	private static IAppTaskInfoStore GetStore()
	{
		lock (Gate)
		{
			return _store ??= new FileAppTaskInfoStore();
		}
	}

	private static AppTaskInfoSnapshot[] GetSnapshotsLocked() =>
		_tasks!.Values
			.OrderBy(static task => task.StartTime)
			.ThenBy(static task => task.Id, StringComparer.Ordinal)
			.ToArray();

	private static void SynchronizeIfSupported(long revision, AppTaskInfoSnapshot[] snapshots) =>
		TryGetSupportedExtension()?.Synchronize(revision, snapshots);
}
