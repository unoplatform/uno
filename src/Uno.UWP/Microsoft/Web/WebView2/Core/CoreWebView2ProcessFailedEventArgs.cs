using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2ProcessFailedEventArgs : EventArgs
{
	internal CoreWebView2ProcessFailedEventArgs(CoreWebView2ProcessFailedKind kind)
	{
		ProcessFailedKind = kind;
	}

	public CoreWebView2ProcessFailedKind ProcessFailedKind { get; }
}
