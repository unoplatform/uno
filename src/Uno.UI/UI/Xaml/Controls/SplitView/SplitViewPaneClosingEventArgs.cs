using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class SplitViewPaneClosingEventArgs
	{
		internal SplitViewPaneClosingEventArgs()
		{
		}

		public bool Cancel { get; set; }
	}
}
