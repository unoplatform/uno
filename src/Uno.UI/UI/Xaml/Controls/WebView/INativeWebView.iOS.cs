using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Wrapper for a version-dependent native iOS WebView
	/// </summary>
	public interface INativeWebView
	{
		// Native properties
		bool CanGoBack { get; }
		bool CanGoForward { get; }

		// Native methods
		void GoBack();
		void GoForward();
		void LoadRequest(NSUrlRequest request);
		void StopLoading();
		void Reload();
		void LoadHtmlString(string s, NSUrl baseUrl);
		void SetScrollingEnabled(bool isScrollingEnabled);

		void RegisterNavigationEvents(WebView xamlWebView);
		Task<string> EvaluateJavascriptAsync(CancellationToken ct, string javascript);
	}
}
