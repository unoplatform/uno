#nullable enable

using System.Linq;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		if (VisualTreeHelper.GetChild((DependencyObject)_owner, 0) is not ContentPresenter { Name: "WebViewTemplateRoot" } contentPresenter)
		{
			return null;
		}

		var nativeWebView = new NativeWebView(this);
		contentPresenter.Content = nativeWebView;
		return nativeWebView;
	}
}
