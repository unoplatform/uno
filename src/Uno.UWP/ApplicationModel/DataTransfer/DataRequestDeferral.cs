using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataRequestDeferral
	{
		private readonly DeferralCompletedHandler _handler;

		internal DataRequestDeferral(DeferralCompletedHandler handler) => _handler = handler;

		public void Complete() => _handler?.Invoke();
	}
}
