using System;
using Uno.Helpers;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataRequest
	{
		private readonly Action<DataRequest> _complete;

		private DeferralManager<DataRequestDeferral> _deferralManager;

		internal DataRequest(Action<DataRequest> complete) => _complete = complete;

		public DataPackage Data { get; set; } = new DataPackage();

		public DateTimeOffset Deadline { get; }

		internal bool IsDeferred => _deferralManager != null;

		internal void EventRaiseCompleted() => _deferralManager?.EventRaiseCompleted();

		public DataRequestDeferral GetDeferral()
		{
			if (_deferralManager == null)
			{
				_deferralManager = new DeferralManager<DataRequestDeferral>(h => new DataRequestDeferral(h));
				_deferralManager.Completed += (s, e) => _complete(this);
			}

			return _deferralManager.GetDeferral();
		}
	}
}
