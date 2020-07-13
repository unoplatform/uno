using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
