#nullable enable

using System.Threading;
using Uno;

namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous operation with result value.
/// </summary>
/// <typeparam name="TResult"></typeparam>
internal class AsyncOperation<TResult> : IAsyncOperation<TResult>, IAsyncOperationInternal<TResult>
{
	private readonly CancellationTokenSource _cts = new CancellationTokenSource();
	private AsyncOperationCompletedHandler<TResult>? _onCompleted;
	private AsyncStatus _status;

	public AsyncOperation(FuncAsync<AsyncOperation<TResult>, TResult> taskBuilder)
	{
		Task = BuildTaskAsync(taskBuilder);
	}

	public uint Id { get; } = AsyncOperation.CreateId();

	public Task<TResult> Task { get; }

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

	public Exception? ErrorCode { get; private set; }

	public AsyncStatus Status
	{
		get => _status;
		set
		{
			_status = value;

			// Note: No need to check if the 'value' is a final state, the 'Started' state is set in ctor,
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
}
