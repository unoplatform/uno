using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

internal interface INativeWebViewProvider
{
	INativeWebView CreateNativeWebView(ContentPresenter contentPresenter);
}
