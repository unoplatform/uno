using System.Threading;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous action without result value.
/// </summary>
internal class AsyncAction : IAsyncAction, IAsyncActionInternal
{
	private readonly Task _task;
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private AsyncActionCompletedHandler _onCompleted;
	private AsyncStatus _status;

	public static AsyncAction FromTask(Func<CancellationToken, Task> taskBuilder) => new AsyncAction(taskBuilder);

	private AsyncAction(Func<CancellationToken, Task> taskBuilder)
	{
		_task = BuildTaskAsync(taskBuilder);
	}

	public Task Task => _task;

	public uint Id => 0;

	public Exception ErrorCode { get; private set; }

	public AsyncActionCompletedHandler Completed
	{
		get => _onCompleted;
		set
		{
			_onCompleted = value;
			if (Status != AsyncStatus.Started) { value?.Invoke(this, Status); }
		}
	}

	public AsyncStatus Status
	{
		get => _status;
		set
		{
			_status = value;
			_onCompleted?.Invoke(this, AsyncStatus.Error);
		}
	}

	public void Cancel() => _cancellationTokenSource.Cancel();

	public void Close() => _cancellationTokenSource.Cancel();

	public void GetResults() => _task.Wait();

	private async Task BuildTaskAsync(Func<CancellationToken, Task> taskBuilder)
	{
		Status = AsyncStatus.Started;

		try
		{
			await taskBuilder(_cancellationTokenSource.Token);
			Status = AsyncStatus.Completed;
		}
		catch (Exception e)
		{
			ErrorCode = e;
			Status = AsyncStatus.Error;
			throw;
		}
	}
}
