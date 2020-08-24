// MUX Reference: TabView.idl, commit 7a46d353095d65ad1c560059d92e4b3818309b06

using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Xaml.Controls
{
	public class TabViewTabDragCompletedEventArgs
    {
		internal TabViewTabDragCompletedEventArgs(DataPackageOperation dropResult, object item, TabViewItem tab)
		{
			DropResult = dropResult;
			Item = item;
			Tab = tab;
		}

		public DataPackageOperation DropResult { get; }

		public object Item { get; }

		public TabViewItem Tab { get; }
	}
}
