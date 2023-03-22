#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.Web;
using Windows.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	private NativeWebViewWrapper _nativeWebViewWrapper;

	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		var webView = (_owner as ViewGroup)?
			.GetChildren(v => v is Android.Webkit.WebView)
			.FirstOrDefault() as Android.Webkit.WebView;

		// TODO:MZ: This uses "Android.Webkit.WebView" directly instead of
		// NativeWebView, so it is then wrapped in NativeWebViewWrapper.
		// Should we allow "custom" webview here, or just scratch this
		// and use our own NativeWebView instead?

		if (webView is null)
		{
			return null;
		}

		_nativeWebViewWrapper = new NativeWebViewWrapper(webView, this);

		return _nativeWebViewWrapper;
	}
}
