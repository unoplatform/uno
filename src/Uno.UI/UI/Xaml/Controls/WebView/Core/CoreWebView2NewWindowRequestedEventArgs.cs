using System;

using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2NewWindowRequestedEventArgs
{
	public CoreWebView2 NewWindow { get; set; }

	public bool Handled { get; set; }

	public bool IsUserInitiated { get; }

	public string Uri { get; }

	public CoreWebView2WindowFeatures WindowFeatures { get; }

	public Deferral GetDeferral()
	{
		throw new NotImplementedException();
	}
}
