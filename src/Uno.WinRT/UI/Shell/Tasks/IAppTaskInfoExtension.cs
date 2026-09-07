#nullable enable
#pragma warning disable CS8305

using System;
using System.Threading.Tasks;
using Uno.Foundation.Logging;

namespace Uno.UI.Shell.Tasks;

internal interface IAppTaskInfoExtension
{
	bool IsSupported();

	void SetAvailability(bool isAvailable);

	void Synchronize(long revision, Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks);
}

internal abstract class AppTaskInfoExtensionBase : IAppTaskInfoExtension
{
	private readonly object _synchronizationGate = new();
	private long _lastRevision = -1;
	private long _activeRevision = -1;
	private long _queuedRevision = -1;
	private Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[]? _queuedTasks;
	private long _latestRevision = -1;
	private Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[]? _latestTasks;
	private bool _isSynchronizing;
	private bool _isAvailable;

	public abstract bool IsSupported();

	public void SetAvailability(bool isAvailable)
	{
		bool replay;
		lock (_synchronizationGate)
		{
			replay = isAvailable && !_isAvailable;
			_isAvailable = isAvailable;
		}

		if (replay)
		{
			InvalidateSynchronization();
		}
	}

	public void Synchronize(long revision, Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks)
	{
		lock (_synchronizationGate)
		{
			if (revision < _latestRevision)
			{
				return;
			}

			_latestRevision = revision;
			_latestTasks = tasks;
			if (revision <= _lastRevision || revision <= _activeRevision || revision <= _queuedRevision)
			{
				return;
			}

			_queuedRevision = revision;
			_queuedTasks = tasks;
			if (_isSynchronizing)
			{
				return;
			}

			_isSynchronizing = true;
		}

		ProcessQueueSafelyAsync();
	}

	protected abstract Task OnSynchronizeAsync(Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks);

	protected void InvalidateSynchronization()
	{
		bool start = false;
		lock (_synchronizationGate)
		{
			_lastRevision = -1;
			if (_latestTasks is not null)
			{
				_queuedRevision = _latestRevision;
				_queuedTasks = _latestTasks;
				if (!_isSynchronizing)
				{
					_isSynchronizing = true;
					start = true;
				}
			}
		}

		if (start)
		{
			ProcessQueueSafelyAsync();
		}
	}

	// This void entry point surfaces unrecoverable failures through the caller's context rather than an unobserved Task.
	private async void ProcessQueueSafelyAsync()
	{
		try
		{
			await ProcessQueueAsync();
		}
		catch (Exception error) when (IsRecoverable(error))
		{
			lock (_synchronizationGate)
			{
				_activeRevision = -1;
				_queuedRevision = -1;
				_queuedTasks = null;
				_isSynchronizing = false;
			}

			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error(
					$"Unexpected failure in app task presenter queue '{GetType().FullName}'.",
					error);
			}
		}
	}

	// Presenter failures must never tear down the app, but process-level failures still have to surface.
	protected static bool IsRecoverable(Exception error) =>
		error is not (OutOfMemoryException
			or StackOverflowException
			or AccessViolationException
			or BadImageFormatException
			or InvalidProgramException
			or AppDomainUnloadedException
			or CannotUnloadAppDomainException);

	private async Task ProcessQueueAsync()
	{
		while (true)
		{
			long revision;
			Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks;
			lock (_synchronizationGate)
			{
				revision = _queuedRevision;
				tasks = _queuedTasks!;
				_queuedRevision = -1;
				_queuedTasks = null;
				_activeRevision = revision;
			}

			try
			{
				await OnSynchronizeAsync(tasks);
				lock (_synchronizationGate)
				{
					_lastRevision = Math.Max(_lastRevision, revision);
				}
			}
			catch (Exception error) when (IsRecoverable(error))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(
						$"Failed to synchronize app task presenter '{GetType().FullName}' at revision {revision}.",
						error);
				}
			}

			lock (_synchronizationGate)
			{
				_activeRevision = -1;
				if (_queuedTasks is null)
				{
					_isSynchronizing = false;
					return;
				}
			}
		}
	}
}
