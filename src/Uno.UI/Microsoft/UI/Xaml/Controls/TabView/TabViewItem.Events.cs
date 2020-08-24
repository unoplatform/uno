using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItem
	{
		public event TypedEventHandler<TabViewItem, TabViewTabCloseRequestedEventArgs> CloseRequested;
	}
}
