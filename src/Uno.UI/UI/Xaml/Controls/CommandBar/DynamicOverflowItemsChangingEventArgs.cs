using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class DynamicOverflowItemsChangingEventArgs
	{
		public global::Microsoft.UI.Xaml.Controls.CommandBarDynamicOverflowAction Action
		{
			get;
			internal set;
		}
	}
}
