using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentDialogButtonClickDeferral
	{
		private readonly DeferralCompletedHandler _handler;

		internal ContentDialogButtonClickDeferral(DeferralCompletedHandler handler) => _handler = handler;

		public void Complete() => _handler?.Invoke();
	}
}
