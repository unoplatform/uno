using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	public partial class TreeViewItemInvokedEventArgs
	{
		internal TreeViewItemInvokedEventArgs(object invokedItem) => InvokedItem = invokedItem;

		public bool Handled { get; set; }

		public object InvokedItem { get; }
	}
}
