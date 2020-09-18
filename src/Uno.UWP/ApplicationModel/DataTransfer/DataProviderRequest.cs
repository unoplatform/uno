#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataProviderRequest
	{
		private readonly TaskCompletionSource<object> _asyncData = new TaskCompletionSource<object>();
		private object? _data;
		private DataProviderDeferral? _deferral;

		internal DataProviderRequest(string formatId, DateTimeOffset deadline)
		{
			FormatId = formatId;
			Deadline = deadline;
		}

		public string FormatId { get; }

		public DateTimeOffset Deadline { get; }

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
	}
}
