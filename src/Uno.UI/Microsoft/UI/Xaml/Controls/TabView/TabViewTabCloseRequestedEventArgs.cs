// MUX Reference: TabView.idl, commit 7a46d353095d65ad1c560059d92e4b3818309b06

namespace Microsoft.UI.Xaml.Controls
{
	public sealed class TabViewTabCloseRequestedEventArgs
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
