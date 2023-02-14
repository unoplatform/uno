using Uno.UI.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class WebViewNavigationCompletedEventArgs
	{
		public bool IsSuccess { get; internal set; }
		public Uri Uri { get; internal set; }
		public WebErrorStatus WebErrorStatus { get; internal set; }
	}
}
