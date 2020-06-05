using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosingDeferral
	{
		private readonly DeferralCompletedHandler _handler;

		/// <summary>
		/// This constructor does not exist in UWP, can be removed.
		/// </summary>
		public ContentDialogClosingDeferral() { }

		internal ContentDialogClosingDeferral(DeferralCompletedHandler handler)
		{
			_handler = handler;
		}

		public void Complete() => _handler?.Invoke();
	}
}
