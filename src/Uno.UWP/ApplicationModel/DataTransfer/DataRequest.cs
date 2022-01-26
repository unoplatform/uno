using System;
using Uno.Helpers;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataRequest
	{
		internal DataRequest(Action<DataRequest> complete)
		{
			DeferralManager = new DeferralManager<DataRequestDeferral>(h => new DataRequestDeferral(h));
			DeferralManager.Completed += (s, e) => complete(this);
		}

		public DataPackage Data { get; set; } = new DataPackage();

		public DateTimeOffset Deadline { get; }

		internal DeferralManager<DataRequestDeferral> DeferralManager { get; }

		public DataRequestDeferral GetDeferral() => DeferralManager.GetDeferral();
	}
}
