// MUX Reference: TabView.idl, commit 7a46d353095d65ad1c560059d92e4b3818309b06

using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Xaml.Controls
{
	public class TabViewTabDragStartingEventArgs
    {
		internal TabViewTabDragStartingEventArgs(DataPackage data, object item, TabViewItem tab)
		{
			Data = data;
			Item = item;
			Tab = tab;
		}

		public DataPackage Data { get; }

		public object Item { get; }

		public TabViewItem Tab { get; }

		public bool Cancel { get; set; }
	}
}
