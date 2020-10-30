using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBar
	{
		public event TypedEventHandler<InfoBar, object> CloseButtonClick;

		public event TypedEventHandler<InfoBar, InfoBarClosingEventArgs> Closing;

		public event TypedEventHandler<InfoBar, InfoBarClosedEventArgs> Closed;
	}
}
