using System;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2NewWindowRequestedEventArgs
{
	internal CoreWebView2NewWindowRequestedEventArgs(string uri, Uri referrer) =>
		(Uri, Referrer) = (uri, referrer);

	public string Uri { get; }

	internal Uri Referrer { get; }

	public bool Handled { get; set; }
}
