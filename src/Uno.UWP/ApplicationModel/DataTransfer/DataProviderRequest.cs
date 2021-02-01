#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataProviderRequest
	{
		private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30); // Arbitrary value that should be validated against UWP

		private readonly TaskCompletionSource<object> _asyncData = new TaskCompletionSource<object>();
		private readonly CancellationTokenSource _ct;
		private readonly IDisposable _autoAbort;
		private object? _data;
		private DataProviderDeferral? _deferral;

		internal DataProviderRequest(string formatId)
		{
			FormatId = formatId;
			Deadline = DateTimeOffset.Now + _timeout; // We create the Deadline the ct, so we ensure that it will be before the actual timeout

			_ct = new CancellationTokenSource(_timeout);
			_autoAbort = _ct.Token.Register(Abort);
		}

		public string FormatId { get; }

		public DateTimeOffset Deadline { get; }

		internal CancellationToken CancellationToken => _ct.Token;

		public DataProviderDeferral GetDeferral()
			=> _deferral ??= new DataProviderDeferral(Complete);

		public void SetData(object value)
		{
			_data = value;
			if (_deferral is null)
			{
				Complete();
			}
		}

		internal Task<object> GetData()
			=> _asyncData.Task;

		internal void Abort()
			=> _asyncData.TrySetException(new TimeoutException("Data provider didn't replied within the allocated time frame"));

		private void Complete()
		{
			if (_data is null)
			{
				_asyncData.TrySetCanceled();
			}
			else
			{
				_asyncData.TrySetResult(_data);
			}
		}

		internal void Dispose() // We do not implement IDisposable as it would defer with the UWP contract, and we don't want the app to dispose this request.
		{
			_autoAbort.Dispose();
			_ct.Dispose();
		}
	}
}
