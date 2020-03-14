using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewDragItemsStartingEventArgs
    {
		private readonly DragItemsStartingEventArgs _dragItemsStartingEventArgs;

		public TreeViewDragItemsStartingEventArgs(DragItemsStartingEventArgs args)
		{
			_dragItemsStartingEventArgs = args;
		}

		public bool Cancel { get; set; }

		public DataPackage Data => _dragItemsStartingEventArgs.Data;

		public IList<object> Items => _dragItemsStartingEventArgs.Items;
    }
}
