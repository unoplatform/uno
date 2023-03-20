using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	private readonly IWebView _owner;

	internal CoreWebView2(IWebView owner)
	{
		_owner = owner;
	}
}
