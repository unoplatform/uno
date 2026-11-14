#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Uno.Web.WebView2.Core;

// Public: the XAML generator emits typeof(INativeWebViewProvider) into app code for add-in [ApiExtension] registrations.
public interface INativeWebViewProvider
{
	// A provider can decline creation when the current host is unsupported.
	internal INativeWebView? CreateNativeWebView(ContentPresenter contentPresenter);
}
