using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Win32;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32NativeWebViewProvider(CoreWebView2 owner) : INativeWebViewProvider
{
	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var backend = Environment.GetEnvironmentVariable("UNO_WEBVIEW2_BACKEND")?.Trim().ToLowerInvariant();
		if (!string.IsNullOrEmpty(backend) && backend != "webview2aot")
		{
			// webview2aot is the only backend left since the Microsoft.Web.WebView2 backend
			// was removed in Uno Platform 7.0; any other value is ignored, not fatal.
			typeof(Win32Host).LogWarn()?.Warn($"`UNO_WEBVIEW2_BACKEND={backend}` is not supported. The Microsoft.Web.WebView2 backend was removed in Uno Platform 7.0; `webview2aot` is the only backend and is used regardless.");
		}

		return new Win32NativeAotWebView(owner, contentPresenter);
	}
}
