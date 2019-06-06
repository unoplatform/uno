#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public  partial class TreeViewExpandingEventArgs 
	{

		[Uno.NotImplemented]
		public TreeViewNode Node
		{
			get
			{
				throw new NotImplementedException("The member TreeViewNode TreeViewExpandingEventArgs.Node is not implemented in Uno.");
			}
		}

		[Uno.NotImplemented]
		public  object Item
		{
			get
			{
				throw new NotImplementedException("The member object TreeViewExpandingEventArgs.Item is not implemented in Uno.");
			}
		}
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewExpandingEventArgs.Node.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewExpandingEventArgs.Item.get
	}
}
