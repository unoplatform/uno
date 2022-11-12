using System;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Foundation;

internal class AsyncAction : IAsyncAction, IAsyncActionInternal
{
	private CancellationTokenSource _cts = new CancellationTokenSource();
	private AsyncActionCompletedHandler _onCompleted;
	private Task _task;
	private AsyncStatus _status;

	public static AsyncAction FromTask(Func<CancellationToken, Task> taskBuilder) => new AsyncAction(taskBuilder);

	private AsyncAction(Func<CancellationToken, Task> taskBuilder)
	{
		_task = BuildTaskAsync(taskBuilder);
	}

	private async Task BuildTaskAsync(Func<CancellationToken, Task> taskBuilder)
	{
		Status = AsyncStatus.Started;

		try
		{
			await taskBuilder(_cts.Token);
			Status = AsyncStatus.Completed;
		}
		catch (Exception e)
		{
			ErrorCode = e;
			Status = AsyncStatus.Error;
			throw;
		}
	}

	public AsyncActionCompletedHandler Completed
	{
		get { return _onCompleted; }
		set
		{
			_onCompleted = value;
			if (Status != AsyncStatus.Started) { value?.Invoke(this, Status); }
		}
	}

	public Exception ErrorCode { get; private set; }

	public uint Id => 0;

	public AsyncStatus Status
	{
		get { return _status; }
		set
		{
			_status = value;
			_onCompleted?.Invoke(this, AsyncStatus.Error);
		}
	}

	public void Cancel()
	{
		_cts.Cancel();
	}

	public void Close()
	{
		_cts.Cancel();
	}

	public void GetResults()
	{
		_task.Wait();
	}

	public Task Task => _task;
}
