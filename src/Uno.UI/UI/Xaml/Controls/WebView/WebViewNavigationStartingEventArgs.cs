using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class WebViewNavigationStartingEventArgs
	{
		public bool Cancel { get; set; }
		public Uri Uri { get; internal set; }
	}
}
