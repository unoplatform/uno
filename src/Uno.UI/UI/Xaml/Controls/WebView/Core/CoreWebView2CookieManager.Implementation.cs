#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// CoreWebView2CookieManager implementation
/// </summary>
public partial class CoreWebView2CookieManager
{
	private INativeWebView? _nativeWebView;

	internal CoreWebView2CookieManager(INativeWebView nativeWebView)
	{
		_nativeWebView = nativeWebView ?? throw new ArgumentNullException(nameof(nativeWebView));
	}
}
