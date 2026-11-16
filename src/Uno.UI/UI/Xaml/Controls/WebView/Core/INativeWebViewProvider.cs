using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Uno.Web.WebView2.Core;

// Public: the XAML generator emits typeof(INativeWebViewProvider) into app code for add-in [ApiExtension] registrations.
public interface INativeWebViewProvider
{
	internal INativeWebView CreateNativeWebView(ContentPresenter contentPresenter);
}
