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
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Microsoft.Web.WebView2.Core;

public partial class CoreWebView2
{
	internal INativeWebView? GetNativeWebViewFromTemplate()
	{
		var webView = (_owner as ViewGroup)?
			.GetChildren(v => v is AWebView)
			.FirstOrDefault() as AWebView;

		if (webView is null)
		{
			return null;
		}

		return new NativeWebViewWrapper(webView, this);
	}
}
