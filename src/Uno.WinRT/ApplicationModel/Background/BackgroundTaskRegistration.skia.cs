#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskRegistration
{
	private readonly BackgroundTaskRegistrationRecord? _record;
	private readonly object _eventGate = new();
	private BackgroundTaskEventWatcher? _eventWatcher;
	private BackgroundTaskCompletedEventHandler? _completed;
	private BackgroundTaskProgressEventHandler? _progress;

	internal BackgroundTaskRegistration(BackgroundTaskRegistrationRecord record)
	{
		_record = record;
		Name = record.Name;
		Trigger = record.Trigger;
	}

	private BackgroundTaskRegistrationRecord Record =>
		_record ?? throw new InvalidOperationException(
			"This background task registration is not backed by a persisted task.");

	public static IReadOnlyDictionary<string, BackgroundTaskRegistrationGroup> AllTaskGroups
	{
		get
		{
			var groups = BackgroundTaskRegistrationStore
				.GetAll()
				.Where(record => record.GroupId is not null)
				.GroupBy(record => record.GroupId!, StringComparer.Ordinal)
				.ToDictionary(
					group => group.Key,
					group => new BackgroundTaskRegistrationGroup(
						group.Key,
						group.First().GroupName ?? group.Key),
					StringComparer.Ordinal);
			return new ReadOnlyDictionary<string, BackgroundTaskRegistrationGroup>(groups);
		}
	}

	public static IReadOnlyDictionary<Guid, IBackgroundTaskRegistration> AllTasks
	{
		get
		{
			var registrations = BackgroundTaskRegistrationStore
				.GetAll()
				.Where(record => record.GroupId is null)
				.ToDictionary(
					record => record.TaskId,
					record => (IBackgroundTaskRegistration)new BackgroundTaskRegistration(record));
			return new ReadOnlyDictionary<Guid, IBackgroundTaskRegistration>(registrations);
		}
	}

	public BackgroundTaskRegistrationGroup? TaskGroup =>
		Record.GroupId is { } groupId
			? new BackgroundTaskRegistrationGroup(
				groupId,
				Record.GroupName ?? groupId)
			: null;

	BackgroundTaskRegistrationGroup IBackgroundTaskRegistration3.TaskGroup =>
		TaskGroup!;

	public Guid TaskId => Record.TaskId;

	public static BackgroundTaskRegistrationGroup? GetTaskGroup(string groupId)
	{
		ArgumentNullException.ThrowIfNull(groupId);
		return AllTaskGroups.TryGetValue(groupId, out var group) ? group : null;
	}

	public void Unregister(bool cancelTask)
		=> BackgroundTaskRegistrationStore.Unregister(TaskId, cancelTask);

	public event BackgroundTaskCompletedEventHandler Completed
	{
		add
		{
			ArgumentNullException.ThrowIfNull(value);
			lock (_eventGate)
			{
				_completed += value;
				EnsureEventWatcher();
			}
		}
		remove
		{
			lock (_eventGate)
			{
				_completed -= value;
				DisposeEventWatcherIfUnused();
			}
		}
	}

	public event BackgroundTaskProgressEventHandler Progress
	{
		add
		{
			ArgumentNullException.ThrowIfNull(value);
			lock (_eventGate)
			{
				_progress += value;
				EnsureEventWatcher();
			}
		}
		remove
		{
			lock (_eventGate)
			{
				_progress -= value;
				DisposeEventWatcherIfUnused();
			}
		}
	}

	private void EnsureEventWatcher()
	{
		_eventWatcher ??= new BackgroundTaskEventWatcher(TaskId, OnTaskEvent);
	}

	private void DisposeEventWatcherIfUnused()
	{
		if (_completed is null && _progress is null)
		{
			_eventWatcher?.Dispose();
			_eventWatcher = null;
		}
	}

	private void OnTaskEvent(BackgroundTaskEvent taskEvent)
	{
		if (taskEvent.Kind == BackgroundTaskEventKind.Progress)
		{
			_progress?.Invoke(
				this,
				new BackgroundTaskProgressEventArgs(
					taskEvent.InstanceId,
					taskEvent.Progress));
		}
		else
		{
			_completed?.Invoke(
				this,
				new BackgroundTaskCompletedEventArgs(
					taskEvent.InstanceId,
					taskEvent.ErrorMessage));
		}
	}

	private sealed class BackgroundTaskEventWatcher : IDisposable
	{
		private static readonly TimeSpan CatchUpInterval = TimeSpan.FromSeconds(2);

		private readonly Guid _taskId;
		private readonly long _since;
		private readonly Action<BackgroundTaskEvent> _handler;
		private readonly ConcurrentDictionary<string, byte> _processed =
			new(StringComparer.Ordinal);
		private readonly ConcurrentQueue<string> _processedOrder = new();
		private readonly FileSystemWatcher _watcher;
		private readonly Timer _catchUp;

		internal BackgroundTaskEventWatcher(
			Guid taskId,
			Action<BackgroundTaskEvent> handler)
		{
			_taskId = taskId;
			_since = DateTime.UtcNow.Ticks;
			_handler = handler;
			Directory.CreateDirectory(BackgroundTaskRegistrationStore.EventsDirectory);
			_watcher = new FileSystemWatcher(
				BackgroundTaskRegistrationStore.EventsDirectory,
				$"{taskId:N}-*.event")
			{
				NotifyFilter = NotifyFilters.FileName
			};
			_watcher.Created += OnCreated;
			_watcher.Renamed += OnRenamed;
			_watcher.Error += OnError;
			_watcher.EnableRaisingEvents = true;

			// File-system notifications are best-effort and can be dropped, which would lose a
			// completion for good. Poll as a backstop for as long as the subscription is alive.
			_catchUp = new Timer(_ => QueuePending(), null, CatchUpInterval, CatchUpInterval);
			QueuePending();
		}

		public void Dispose()
		{
			_catchUp.Dispose();
			_watcher.Created -= OnCreated;
			_watcher.Renamed -= OnRenamed;
			_watcher.Error -= OnError;
			_watcher.Dispose();
		}

		private void OnCreated(object sender, FileSystemEventArgs args)
			=> Queue(args.FullPath);

		private void OnRenamed(object sender, RenamedEventArgs args)
			=> Queue(args.FullPath);

		private void OnError(object sender, ErrorEventArgs args)
			=> QueuePending();

		private void QueuePending()
		{
			try
			{
				foreach (var path in Directory.EnumerateFiles(
					BackgroundTaskRegistrationStore.EventsDirectory,
					$"{_taskId:N}-*.event"))
				{
					if (GetEventTicks(path) >= _since)
					{
						Queue(path);
					}
				}
			}
			catch (Exception error) when (
				error is IOException or UnauthorizedAccessException)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Background task events could not be replayed: {error}");
				}
			}
		}

		private static long GetEventTicks(string path)
		{
			var name = Path.GetFileNameWithoutExtension(path);
			var start = name.IndexOf('-') + 1;
			var end = start > 0 ? name.IndexOf('-', start) : -1;
			return end > start
				&& long.TryParse(
					name.AsSpan(start, end - start),
					NumberStyles.None,
					CultureInfo.InvariantCulture,
					out var ticks)
				? ticks
				: 0;
		}

		private void Queue(string path)
		{
			if (_processed.TryAdd(path, 0))
			{
				_processedOrder.Enqueue(path);
				while (_processedOrder.Count > 256 &&
					_processedOrder.TryDequeue(out var stalePath))
				{
					_processed.TryRemove(stalePath, out _);
				}

				_ = Task.Run(() => Process(path));
			}
		}

		private void Process(string path)
		{
			try
			{
				var taskEvent = BackgroundTaskRegistrationStore.ReadEvent(path);
				if (taskEvent.TaskId == _taskId)
				{
					_handler(taskEvent);
				}
			}
			catch (Exception error) when (
				error is IOException or UnauthorizedAccessException)
			{
				// The writer, a backup agent or a virus scanner can still hold a freshly created
				// event file. Forget it so the catch-up scan retries instead of dropping it.
				_processed.TryRemove(path, out _);
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug(
						$"Background task event '{path}' is not readable yet: {error}");
				}
			}
			catch (InvalidDataException error)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug(
						$"Background task event '{path}' could not be processed: {error}");
				}
			}
		}
	}
}
