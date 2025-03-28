using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml.Controls
{
	public partial class DragItemsCompletedEventArgs
	{
		internal DragItemsCompletedEventArgs(DropCompletedEventArgs inner, IReadOnlyList<object> items)
		{
			DropResult = inner.DropResult;
			Items = items;
		}

		public DataPackageOperation DropResult { get; }
		public IReadOnlyList<object> Items { get; }
	}
}
