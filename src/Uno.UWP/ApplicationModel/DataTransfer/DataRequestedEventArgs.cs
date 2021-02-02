#nullable enable

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataRequestedEventArgs
	{
		internal DataRequestedEventArgs(Action<DataRequest> deferralComplete)
		{
			Request = new DataRequest(deferralComplete);
		}

		public DataRequest Request { get; }
	}
}
