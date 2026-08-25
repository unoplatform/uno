#nullable enable

using System;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background;

internal sealed class BackgroundTaskInstance : IBackgroundTaskInstance
{
	private readonly object _gate = new();
	private readonly TaskCompletionSource _completion =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly TaskCompletionSource _cancellationRequested =
		new(TaskCreationOptions.RunContinuationsAsynchronously);
	private int _deferralCount;
	private bool _runReturned;
	private uint _progress;

	internal BackgroundTaskInstance(
		BackgroundTaskRegistration registration,
		object triggerDetails)
	{
		Task = registration;
		TriggerDetails = triggerDetails;
		InstanceId = Guid.NewGuid();
	}

	public Guid InstanceId { get; }

	public uint Progress
	{
		get => _progress;
		set
		{
			_progress = value;
			BackgroundTaskRegistrationStore.WriteEvent(
				new BackgroundTaskEvent(
					BackgroundTaskEventKind.Progress,
					Task.TaskId,
					InstanceId,
					value,
					ErrorMessage: null));
		}
	}

	public uint SuspendedCount => 0;

	public BackgroundTaskRegistration Task { get; }

	public object TriggerDetails { get; }

	internal global::System.Threading.Tasks.Task Completion => _completion.Task;

	internal global::System.Threading.Tasks.Task CancellationRequested =>
		_cancellationRequested.Task;

	public event BackgroundTaskCanceledEventHandler? Canceled;

	public BackgroundTaskDeferral GetDeferral()
	{
		lock (_gate)
		{
			if (_completion.Task.IsCompleted)
			{
				throw new InvalidOperationException(
					"The background task has already completed.");
			}

			_deferralCount++;
			return new BackgroundTaskDeferral(CompleteDeferral);
		}
	}

	internal void MarkRunReturned()
	{
		lock (_gate)
		{
			_runReturned = true;
			CompleteIfReady();
		}
	}

	internal void Cancel(BackgroundTaskCancellationReason reason)
	{
		if (_cancellationRequested.TrySetResult())
		{
			Canceled?.Invoke(this, reason);
		}
	}

	private void CompleteDeferral()
	{
		lock (_gate)
		{
			if (_deferralCount == 0)
			{
				return;
			}

			_deferralCount--;
			CompleteIfReady();
		}
	}

	private void CompleteIfReady()
	{
		if (_runReturned && _deferralCount == 0)
		{
			_completion.TrySetResult();
		}
	}
}
