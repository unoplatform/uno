#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.DataTransfer;

namespace Windows.UI.Xaml.Controls
{

	[Uno.NotImplemented]
	public  partial class TreeViewDragItemsStartingEventArgs 
	{

		[Uno.NotImplemented]
		public  bool Cancel
		{
			get
			{
				throw new NotImplementedException("The member bool TreeViewDragItemsStartingEventArgs.Cancel is not implemented in Uno.");
			}
			set
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs", "bool TreeViewDragItemsStartingEventArgs.Cancel");
			}
		}

		[Uno.NotImplemented]
		public DataPackage Data
		{
			get
			{
				throw new NotImplementedException("The member DataPackage TreeViewDragItemsStartingEventArgs.Data is not implemented in Uno.");
			}
		}
		[Uno.NotImplemented]
		public IList<object> Items
		{
			get
			{
				throw new NotImplementedException("The member IList<object> TreeViewDragItemsStartingEventArgs.Items is not implemented in Uno.");
			}
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Cancel.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Cancel.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Data.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewDragItemsStartingEventArgs.Items.get
	}
}
