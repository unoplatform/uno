using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class DynamicOverflowItemsChangingEventArgs
	{
		public global::Windows.UI.Xaml.Controls.CommandBarDynamicOverflowAction Action
		{
			get;
			internal set;
		}
	}
}
