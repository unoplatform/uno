using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Wrapper for a version-dependent native iOS WebView
/// </summary>
internal partial interface INativeWebView
{
	bool CanGoBack { get; }
	bool CanGoForward { get; }

	void LoadRequest(NSUrlRequest request);	
	void LoadHtmlString(string s, NSUrl baseUrl);
	void SetScrollingEnabled(bool isScrollingEnabled);

	void RegisterNavigationEvents(WebView xamlWebView);
	Task<string> EvaluateJavascriptAsync(CancellationToken ct, string javascript);
}
