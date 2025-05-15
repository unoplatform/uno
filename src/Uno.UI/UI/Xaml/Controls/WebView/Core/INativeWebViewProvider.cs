using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public interface INativeWebViewProvider
{
	internal INativeWebView CreateNativeWebView(ContentPresenter contentPresenter);
}
