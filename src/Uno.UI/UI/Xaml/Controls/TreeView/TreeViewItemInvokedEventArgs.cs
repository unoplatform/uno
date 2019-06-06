#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	[Uno.NotImplemented]
	public partial class TreeViewItemInvokedEventArgs 
	{
		[Uno.NotImplemented]
		public bool Handled
		{
			get
			{
				throw new NotImplementedException("The member bool TreeViewItemInvokedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs", "bool TreeViewItemInvokedEventArgs.Handled");
			}
		}

		[Uno.NotImplemented]
		public object InvokedItem
		{
			get
			{
				throw new NotImplementedException("The member object TreeViewItemInvokedEventArgs.InvokedItem is not implemented in Uno.");
			}
		}

		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs.InvokedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs.Handled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemInvokedEventArgs.Handled.get
	}
}
