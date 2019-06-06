#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public  partial class TreeViewDragItemsCompletedEventArgs 
	{
		[Uno.NotImplemented]
		public  DataPackageOperation DropResult
		{
			get
			{
				throw new NotImplementedException("The member DataPackageOperation TreeViewDragItemsCompletedEventArgs.DropResult is not implemented in Uno.");
			}
		}

		[Uno.NotImplemented]
		public  IReadOnlyList<object> Items
		{
			get
			{
				throw new NotImplementedException("The member IReadOnlyList<object> TreeViewDragItemsCompletedEventArgs.Items is not implemented in Uno.");
			}
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs.DropResult.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsCompletedEventArgs.Items.get
	}
}
