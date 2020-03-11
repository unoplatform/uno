using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml.Controls
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
