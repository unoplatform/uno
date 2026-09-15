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
		var backend = Environment.GetEnvironmentVariable("UNO_WEBVIEW2_BACKEND")?.ToLowerInvariant();
		switch (backend?.Trim())
		{
			case "webview2aot":
				return CreateWin32NativeAotWebView(contentPresenter);
			case "":
			case null:
				break;
			default:
				typeof(Win32Host).LogError()?.Error($"Unsupported `UNO_WEBVIEW2_BACKEND` value `{backend}`! {SupportedUnoWebview2BackendValues}");
				break;
		}
		return CreateDefaultWebView(contentPresenter);
	}

	private const string SupportedUnoWebview2BackendValues = "Supported values: `webview2aot`.";

	private INativeWebView CreateWin32NativeAotWebView(ContentPresenter contentPresenter)
		=> new Win32NativeAotWebView(owner, contentPresenter);

	private INativeWebView CreateDefaultWebView(ContentPresenter contentPresenter)
		=> CreateWin32NativeAotWebView(contentPresenter);
}
