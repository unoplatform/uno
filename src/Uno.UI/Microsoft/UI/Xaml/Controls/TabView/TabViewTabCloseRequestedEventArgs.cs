// MUX Reference: TabView.idl, commit 8aaf7f8

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class TabViewTabCloseRequestedEventArgs
    {
		internal TabViewTabCloseRequestedEventArgs(object item, TabViewItem tab)
		{
			Item = item;
			Tab = tab;
		}

		public object Item { get; }

		public TabViewItem Tab { get; }
    }
}
