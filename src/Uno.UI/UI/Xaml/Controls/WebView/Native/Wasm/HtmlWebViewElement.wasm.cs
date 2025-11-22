#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation;
using Uno.UI.Xaml.Controls;
using static __Microsoft.UI.Xaml.Controls.NativeWebView;

namespace Microsoft.UI.Xaml.Controls;

internal class HtmlWebViewElement : UIElement, INativeWebView, INativeWebViewCookieManager
{
	private CoreWebView2 _coreWebView;

	public HtmlWebViewElement(CoreWebView2 coreWebView) : base("iframe")
	{
		_coreWebView = coreWebView;

		SetAttribute("background-color", "transparent");

		IFrameLoaded += OnNavigationCompleted;
	}

	private event EventHandler IFrameLoaded
	{
		add => RegisterEventHandler("load", value, GenericEventHandlers.RaiseEventHandler);
		remove => UnregisterEventHandler("load", value, GenericEventHandlers.RaiseEventHandler);
	}

	public string DocumentTitle => NativeMethods.GetDocumentTitle(HtmlId) ?? "";

	private void OnNavigationCompleted(object? sender, EventArgs e)
	{
		if (_coreWebView is null)
		{
			return;
		}

		var uriString = this.GetAttribute("src");
		Uri uri = CoreWebView2.BlankUri;
		if (!string.IsNullOrEmpty(uriString))
		{
			uri = new Uri(uriString);
		}

		_coreWebView.OnDocumentTitleChanged();
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => Task.FromResult(NativeMethods.ExecuteScript(HtmlId, script));

	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => Task.FromResult<string?>("");

	private void ScheduleNavigationStarting(string? url, Action loadAction)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationStarting(url, out var cancel);

			if (!cancel)
			{
				loadAction?.Invoke();
			}
		});
	}

	public void ProcessNavigation(Uri uri)
	{
		var uriString = uri.OriginalString;
		ScheduleNavigationStarting(uriString, () => this.SetAttribute("src", uriString));
		OnNavigationCompleted(this, EventArgs.Empty);
	}

	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(null, () => this.SetAttribute("srcdoc", html));
		OnNavigationCompleted(this, EventArgs.Empty);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{
	}

	public void Reload() => NativeMethods.Reload(HtmlId);

	public void Stop() => NativeMethods.Stop(HtmlId);

	public void GoBack() => NativeMethods.GoBack(HtmlId);

	public void GoForward() => NativeMethods.GoForward(HtmlId);

	public void SetScrollingEnabled(bool isScrollingEnabled) { }

	// Cookie Management Implementation
	// Note: WebAssembly has limited cookie access due to browser security (CORS).
	// Only same-origin cookies can be accessed via document.cookie.
	Task<IReadOnlyList<CoreWebView2Cookie>> INativeWebViewCookieManager.GetCookiesAsync(string uri)
	{
		var cookies = new List<CoreWebView2Cookie>();
		
		try
		{
			var cookieString = NativeMethods.GetDocumentCookie();
			if (!string.IsNullOrEmpty(cookieString))
			{
				var cookiePairs = cookieString.Split(';');
				var parsedUri = new Uri(uri);
				var domain = parsedUri.Host;
				var path = parsedUri.AbsolutePath;

				foreach (var cookiePair in cookiePairs)
				{
					var trimmedPair = cookiePair.Trim();
					var equalIndex = trimmedPair.IndexOf('=');
					if (equalIndex > 0)
					{
						var name = trimmedPair.Substring(0, equalIndex);
						var value = trimmedPair.Substring(equalIndex + 1);
						var cookie = new CoreWebView2Cookie(name, value, domain, path);
						cookies.Add(cookie);
					}
				}
			}
		}
		catch
		{
			// Cross-origin cookie access is restricted by browsers
		}

		return Task.FromResult<IReadOnlyList<CoreWebView2Cookie>>(cookies);
	}

	void INativeWebViewCookieManager.AddOrUpdateCookie(CoreWebView2Cookie cookie)
	{
		try
		{
			var cookieString = BuildCookieString(cookie);
			NativeMethods.SetDocumentCookie(cookieString);
		}
		catch
		{
			// Cross-origin cookie setting is restricted by browsers
		}
	}

	void INativeWebViewCookieManager.DeleteCookie(CoreWebView2Cookie cookie)
	{
		try
		{
			// Set cookie with expired date to delete it
			var expiredCookie = $"{cookie.Name}=; Domain={cookie.Domain}; Path={cookie.Path}; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
			NativeMethods.SetDocumentCookie(expiredCookie);
		}
		catch
		{
			// Cross-origin cookie deletion is restricted by browsers
		}
	}

	void INativeWebViewCookieManager.DeleteCookies(string name, string uri)
	{
		try
		{
			var parsedUri = new Uri(uri);
			var domain = parsedUri.Host;
			var path = parsedUri.AbsolutePath;
			var expiredCookie = $"{name}=; Domain={domain}; Path={path}; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
			NativeMethods.SetDocumentCookie(expiredCookie);
		}
		catch
		{
			// Cross-origin cookie deletion is restricted by browsers
		}
	}

	void INativeWebViewCookieManager.DeleteCookiesWithDomainAndPath(string name, string domain, string path)
	{
		try
		{
			var expiredCookie = $"{name}=; Domain={domain}; Path={path}; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
			NativeMethods.SetDocumentCookie(expiredCookie);
		}
		catch
		{
			// Cross-origin cookie deletion is restricted by browsers
		}
	}

	void INativeWebViewCookieManager.DeleteAllCookies()
	{
		try
		{
			// Get all cookies and delete them one by one
			var cookieString = NativeMethods.GetDocumentCookie();
			if (!string.IsNullOrEmpty(cookieString))
			{
				var cookiePairs = cookieString.Split(';');
				foreach (var cookiePair in cookiePairs)
				{
					var trimmedPair = cookiePair.Trim();
					var equalIndex = trimmedPair.IndexOf('=');
					if (equalIndex > 0)
					{
						var name = trimmedPair.Substring(0, equalIndex);
						var expiredCookie = $"{name}=; Expires=Thu, 01 Jan 1970 00:00:00 GMT";
						NativeMethods.SetDocumentCookie(expiredCookie);
					}
				}
			}
		}
		catch
		{
			// Cross-origin cookie deletion is restricted by browsers
		}
	}

	private string BuildCookieString(CoreWebView2Cookie cookie)
	{
		var parts = new System.Collections.Generic.List<string>
		{
			$"{cookie.Name}={cookie.Value}"
		};

		if (!string.IsNullOrEmpty(cookie.Domain))
		{
			parts.Add($"Domain={cookie.Domain}");
		}

		if (!string.IsNullOrEmpty(cookie.Path))
		{
			parts.Add($"Path={cookie.Path}");
		}

		if (!cookie.IsSession)
		{
			// Convert from seconds since epoch to RFC 2822 format
			var expiresDate = DateTimeOffset.FromUnixTimeSeconds((long)cookie.Expires);
			parts.Add($"Expires={expiresDate:R}");
		}

		if (cookie.IsSecure)
		{
			parts.Add("Secure");
		}

		if (cookie.IsHttpOnly)
		{
			parts.Add("HttpOnly");
		}

		if (cookie.SameSite != CoreWebView2CookieSameSiteKind.None)
		{
			parts.Add($"SameSite={cookie.SameSite}");
		}

		return string.Join("; ", parts);
	}

}
