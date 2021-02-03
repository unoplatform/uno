// MUX Reference: TabView.idl, commit 8aaf7f8

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TabViewItem
	{
		public event TypedEventHandler<TabViewItem, TabViewTabCloseRequestedEventArgs> CloseRequested;
	}
}
