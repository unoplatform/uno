using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TeachingTip
    {
		public event TypedEventHandler<TeachingTip, object> ActionButtonClick;

		public event TypedEventHandler<TeachingTip, object> CloseButtonClick;

		public event TypedEventHandler<TeachingTip, TeachingTipClosedEventArgs> Closed;

		public event TypedEventHandler<TeachingTip, TeachingTipClosingEventArgs> Closing;
	}
}
