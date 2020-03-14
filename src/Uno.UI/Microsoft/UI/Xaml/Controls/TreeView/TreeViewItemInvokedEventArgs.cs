namespace Microsoft.UI.Xaml.Controls
{
	public partial class TreeViewItemInvokedEventArgs
	{
		internal TreeViewItemInvokedEventArgs(object invokedItem)
		{
			InvokedItem = invokedItem;
		}

		public bool Handled { get; set; }

		public object InvokedItem { get; }
	}
}
