using Uno.Diagnostics.Eventing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Windows.Foundation
{
	internal class AsyncOperation<TResult> : IAsyncOperation<TResult>
	{
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private AsyncOperationCompletedHandler<TResult> _onCompleted;
		private Task<TResult> _task;
		private AsyncStatus _status;
		private uint _id = 0;

		public AsyncOperation(Func<CancellationToken, Task<TResult>> taskBuilder)
		{
			_task = BuildTaskAsync(taskBuilder);
		}

		private async Task<TResult> BuildTaskAsync(Func<CancellationToken, Task<TResult>> taskBuilder)
		{
			Status = AsyncStatus.Started;

			try
			{
				var result = await taskBuilder(_cts.Token);
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

		public AsyncOperationCompletedHandler<TResult> Completed
		{
			get { return _onCompleted; }
			set
			{
				_onCompleted = value;
				if (Status != AsyncStatus.Started) { value?.Invoke(this, Status); }
			}
		}

		public Exception ErrorCode { get; private set; }

		public uint Id => _id;

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

		public TResult GetResults()
		{
			return _task.Result;
		}

		public Task<TResult> Task => _task;
	}
}
