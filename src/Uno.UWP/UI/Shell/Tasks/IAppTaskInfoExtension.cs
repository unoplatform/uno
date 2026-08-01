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
	private bool _isSynchronizing;
	private bool _isAvailable;

	public abstract bool IsSupported();

	public void SetAvailability(bool isAvailable)
	{
		lock (_synchronizationGate)
		{
			if (isAvailable && !_isAvailable)
			{
				_lastRevision = -1;
			}

			_isAvailable = isAvailable;
		}
	}

	public void Synchronize(long revision, Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks)
	{
		lock (_synchronizationGate)
		{
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

		_ = ProcessQueueAsync();
	}

	protected abstract Task OnSynchronizeAsync(Windows.UI.Shell.Tasks.AppTaskInfoSnapshot[] tasks);

	protected void InvalidateSynchronization()
	{
		lock (_synchronizationGate)
		{
			_lastRevision = -1;
		}
	}

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
			catch (Exception error)
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
