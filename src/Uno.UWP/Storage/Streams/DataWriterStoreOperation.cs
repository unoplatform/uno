using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	public partial class DataWriterStoreOperation : IAsyncOperation<uint>, IAsyncInfo
	{
		private readonly CancellationTokenSource _cts = new CancellationTokenSource();
		private AsyncOperationCompletedHandler<uint> _onCompleted;
		private AsyncStatus _status;
		public DataWriterStoreOperation()
		{
			Status = AsyncStatus.Started;
		}

		public AsyncOperationCompletedHandler<uint> Completed
		{
			get { return _onCompleted; }
			set
			{
				_onCompleted = value;
				if (Status != AsyncStatus.Started) { value?.Invoke(this, Status); }
			}
		}

		public Exception ErrorCode { get; set; }

		public uint Id { get; set; }

		public AsyncStatus Status
		{
			get => _status;
			set => _status = value;
		}

		public void Cancel() => _cts.Cancel();

		public void Close()
		{
			_cts.Cancel();
			_cts.Dispose();
		}
		public uint Result { get; set; }
		public uint GetResults()
		{
			return Status == AsyncStatus.Completed ? Result : 0;
		}
	}
}
