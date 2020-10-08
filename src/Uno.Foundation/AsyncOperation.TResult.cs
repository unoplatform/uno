#nullable enable

using Uno.Diagnostics.Eventing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Uno;

namespace Windows.Foundation
{
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
				_onCompleted?.Invoke(this, AsyncStatus.Error);
			}
		}

		public void Cancel()
			=> _cts.Cancel();

		public void Close()
			=> _cts.Cancel();

		public TResult GetResults()
			=> Task.Result;
	}
}
