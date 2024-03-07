#nullable enable

using System.Linq;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		var webView = ((UIElement)_owner)
			.GetChildren()
			.OfType<NativeWebView>()
			.FirstOrDefault();

		if (webView is null)
		{
			return null;
		}

		return webView;
	}
}
