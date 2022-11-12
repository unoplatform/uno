#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uno;

namespace Windows.Foundation;

internal class AsyncOperation<TResult> : IAsyncOperation<TResult>, IAsyncOperationInternal<TResult>
{
	public static AsyncOperation<TResult> FromTask(FuncAsync<AsyncOperation<TResult>, TResult> builder)
		=> new AsyncOperation<TResult>(builder);

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();
	private AsyncOperationCompletedHandler<TResult>? _onCompleted;
	private AsyncStatus _status;

	public AsyncOperation(FuncAsync<AsyncOperation<TResult>, TResult> taskBuilder)
	{
		Task = BuildTaskAsync(taskBuilder);
	}

	private async Task<TResult> BuildTaskAsync(FuncAsync<AsyncOperation<TResult>, TResult> taskBuilder)
	{
		Status = AsyncStatus.Started;

		try
		{
			var result = await taskBuilder(_cts.Token, this);
			Status = AsyncStatus.Completed;
			return result;
		}
		catch (OperationCanceledException)
		{
			Status = AsyncStatus.Canceled;
			throw;
		}
		catch (Exception e)
		{
			ErrorCode = e;
			Status = AsyncStatus.Error;
			throw;
		}
	}

	public AsyncOperationCompletedHandler<TResult>? Completed
	{
		get => _onCompleted;
		set
		{
			_onCompleted = value;
			if (Status != AsyncStatus.Started)
			{
				value?.Invoke(this, Status);
			}
		}
	}

	public uint Id { get; } = AsyncOperation.CreateId();

	public Task<TResult> Task { get; }

	public Exception? ErrorCode { get; private set; }

	public AsyncStatus Status
	{
		get => _status;
		set
		{
			_status = value;

			// Note: No need to check if the 'value' is a final state, the 'Sarted' state is set in ctor,
			//		 i.e. before the '_onCompleted' is being set.
			_onCompleted?.Invoke(this, value);
		}
	}

	public void Cancel()
		=> _cts.Cancel();

	public void Close()
		=> _cts.Cancel();

	public TResult GetResults()
		=> Task.Result;
}
